﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Youtube.Data;
using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubeUpload
    {
        private static string uploadEndpoint = "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status";
        private static string thumbnailEndpoint = "https://www.googleapis.com/upload/youtube/v3/thumbnails/set";
        private static string playlistItemsEndpoint = "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet";
        
        private static ThrottledBufferedStream stream = null;
        private static int uploadChunkSizeInBytes = 40 * 262144; // 10 MegaByte, The chunk size must be a multiple of 256 KiloByte
        private static TimeSpan twoSeconds = new TimeSpan(0, 0, 2);
        public static long MaxUploadInBytesPerSecond
        {
            set
            {
                ThrottledBufferedStream stream = YoutubeUpload.stream;
                if (stream != null)
                {
                    stream.MaximumBytesPerSecond = value;
                }
            }
        }

        public static async Task<UploadResult> Upload(Upload upload, long maxUploadInBytesPerSecond, Action<YoutubeUploadStats> updateUploadProgress, Func<bool> stopUpload)
        {
            upload.UploadErrorMessage = string.Empty;

            UploadResult result = new UploadResult()
            {
                ThumbnailSuccessFull = false,
                PlaylistSuccessFull = false,
                VideoId = string.Empty
            };

            if(!File.Exists(upload.FilePath))
            {
                upload.UploadErrorMessage = "File does not exist.";
                return result;
            }

            try
            {
                string range = null;
                if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
                {
                    await YoutubeUpload.requestNewUpload(upload);
                }
                else
                {
                    range = await YoutubeUpload.getRange(upload);
                }

                long uploadByteIndex = 0;
                if (!string.IsNullOrWhiteSpace(range))
                {
                    string[] parts = range.Split('-');
                    uploadByteIndex = Convert.ToInt64(parts[1]) + 1;
                }

                upload.BytesSent = uploadByteIndex;
                long initialBytesSent = uploadByteIndex;

                using (FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond))
                {
                    inputStream.Position = uploadByteIndex;
                    YoutubeUpload.stream = inputStream;
                    long totalBytesRead = 0;

                    long fileLength = upload.FileLength;
                    HttpWebRequest request = null;

                    DateTime lastStatUpdate = DateTime.Now;

                    while (fileLength > totalBytesRead + initialBytesSent)
                    {
                        request = null;
                        if (uploadByteIndex > 0 || fileLength > YoutubeUpload.uploadChunkSizeInBytes)
                        {
                            request = await HttpWebRequestCreator.CreateAuthenticatedResumeHttpWebRequest(upload.ResumableSessionUri, "PUT", fileLength, uploadByteIndex, YoutubeUpload.uploadChunkSizeInBytes);
                        }
                        else
                        {
                            request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(upload.ResumableSessionUri, "PUT", upload.FilePath);
                        }

                        int chunkBytesSent = 0;
                        using (Stream dataStream = await request.GetRequestStreamAsync())
                        {
                            //very small buffer increases CPU load >= 10kByte seems OK.
                            byte[] buffer = new byte[10 * 1024];
                            int bytesRead;
                            YoutubeUploadStats stats = new YoutubeUploadStats();



                            int readLength = buffer.Length;
                            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, readLength)) != 0)
                            {
                                if (stopUpload != null && stopUpload())
                                {
                                    request.Abort();
                                    upload.UploadStatus = UplStatus.Stopped;
                                    break;
                                }

                                await dataStream.WriteAsync(buffer, 0, bytesRead);
                                
                                totalBytesRead += bytesRead;
                                chunkBytesSent += bytesRead;

                                if (chunkBytesSent + buffer.Length > YoutubeUpload.uploadChunkSizeInBytes)
                                {
                                    readLength = YoutubeUpload.uploadChunkSizeInBytes - chunkBytesSent;

                                    if (readLength == 0)
                                    {
                                        uploadByteIndex += YoutubeUpload.uploadChunkSizeInBytes;
                                    }
                                }

                                if (DateTime.Now - lastStatUpdate > YoutubeUpload.twoSeconds)
                                {
                                    stats.BytesSent = totalBytesRead;
                                    stats.CurrentSpeedInBytesPerSecond = inputStream.CurrentSpeedInBytesPerSecond;
                                    upload.BytesSent = initialBytesSent + totalBytesRead;
                                    updateUploadProgress(stats);
                                    lastStatUpdate = DateTime.Now;
                                }
                            }

                            try
                            {
                                if (upload.UploadStatus != UplStatus.Stopped)
                                {
                                    using (HttpWebResponse httpResponse =
                                        (HttpWebResponse) await request.GetResponseAsync())
                                    {
                                        upload.BytesSent = upload.FileLength;
                                        YoutubeUpload.stream = null;

                                        using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                                        {
                                            var definition = new {Id = ""};
                                            var response =
                                                JsonConvert.DeserializeAnonymousType(await reader.ReadToEndAsync(),
                                                    definition);
                                            result.VideoId = response.Id;
                                        }
                                    }
                                }
                            }
                            catch (WebException e)
                            {
                                if (e.Response == null)
                                {
                                    throw;
                                }

                                using (HttpWebResponse httpResponse = e.Response as HttpWebResponse)
                                {
                                    if (httpResponse == null)
                                    {
                                        throw;
                                    }

                                    if ((int) httpResponse.StatusCode != 308)
                                    {
                                        throw;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                if (upload.UploadStatus != UplStatus.Stopped)
                {
                    if (e.Response != null)
                    {
                        using(e.Response)
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage =
                                $"Video upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                    }
                }

                return result;
            }
            catch(Exception e)
            {
                upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                return result;
            }

            result.ThumbnailSuccessFull = await YoutubeUpload.addThumbnail(upload, result.VideoId);
            result.PlaylistSuccessFull = await YoutubeUpload.addToPlaylist(upload, result.VideoId);

            return result;
        }

        private static async Task<string> getRange(Upload upload)
        {
            HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedResumeInformationHttpWebRequest(upload.ResumableSessionUri, "PUT", upload.FilePath);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    throw;
                }

                using (HttpWebResponse httpResponse = e.Response as HttpWebResponse)
                {
                    if (httpResponse == null)
                    {
                        throw;
                    }

                    if ((int) httpResponse.StatusCode != 308)
                    {
                        throw;
                    }

                    return httpResponse.Headers["Range"];
                }
            }

            throw new InvalidOperationException("Http status code 308 expected for ResumeInformationHttpWebRequest");
        }

        private static async Task requestNewUpload(Upload upload)
        {
            YoutubeVideoRequest video = new YoutubeVideoRequest();

            video.VideoSnippet = new YoutubeVideoSnippet();
            video.VideoSnippet.Title = upload.YtTitle;
            video.VideoSnippet.Description = upload.Description;
            video.VideoSnippet.Tags = (upload.Tags != null ? upload.Tags : new List<string>()).ToArray();

            video.Status = new YoutubeStatus();
            video.Status.Privacy = upload.Visibility.ToString().ToLower(); // "unlisted", "private" or "public"

            if (upload.PublishAt.Date != DateTime.MinValue.Date)
            {
                video.Status.PublishAt = upload.PublishAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz");
            }

            string content = JsonConvert.SerializeObject(video);
            var jsonBytes = Encoding.UTF8.GetBytes(content);

            FileInfo info = new FileInfo(upload.FilePath);
            //request upload session/uri
            HttpWebRequest request =
                await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                    YoutubeUpload.uploadEndpoint, "POST", jsonBytes, "application/json; charset=utf-8");

            //slug header adds original video file name to youtube studio, lambda filters to valid chars (ascii >=32 and <=255)
            string httpHeaderCompatibleString = new String(Path.GetFileName(upload.FilePath).Where(c =>
            {
                char ch = (char) ((uint) byte.MaxValue & (uint) c);
                if ((ch >= ' ' || ch == '\t') && ch != '\x007F')
                {
                    return true;
                }

                return false;
            }).ToArray());

            request.Headers.Add("Slug", httpHeaderCompatibleString);
            request.Headers.Add("X-Upload-Content-Length", info.Length.ToString());
            request.Headers.Add("X-Upload-Content-Type", "video/*");


            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                upload.ResumableSessionUri = response.Headers["Location"];
            }
        }


        private static async Task<bool> addThumbnail(Upload upload, string videoId)
        {
            if (!string.IsNullOrWhiteSpace(upload.ThumbnailFilePath) && File.Exists(upload.ThumbnailFilePath))
            {
                try
                {
                    HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                            string.Format("{0}?videoId={1}", YoutubeUpload.thumbnailEndpoint, videoId), "POST", upload.ThumbnailFilePath);

                        using (FileStream inputStream = new FileStream(upload.ThumbnailFilePath, FileMode.Open))
                        using (Stream dataStream = await request.GetRequestStreamAsync())
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await dataStream.WriteAsync(buffer, 0, bytesRead);
                            }
                        }

                        using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                        {
                        }
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        using(e.Response)
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage += $"Thumbnail upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage += $"Thumbnail upload failed: {e.ToString()}";
                    }

                    return false;
                }
                catch (Exception e)
                {
                    upload.UploadErrorMessage = $"Thumbnail upload failed: {e.ToString()}";
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> addToPlaylist(Upload upload, string videoId)
        {
            if (!string.IsNullOrWhiteSpace(upload.PlaylistId))
            {
                try
                {
                    YoutubePlaylistItemRequest playlistItem = new YoutubePlaylistItemRequest();

                    playlistItem.PlaylistSnippet = new YoutubePlaylistItemSnippet();
                    playlistItem.PlaylistSnippet.PlaylistId = upload.PlaylistId;
                    playlistItem.PlaylistSnippet.ResourceId = new VideoResource();
                    playlistItem.PlaylistSnippet.ResourceId.VideoId = videoId;

                    string content = JsonConvert.SerializeObject(playlistItem);
                    var jsonBytes = Encoding.UTF8.GetBytes(content);

                    FileInfo info = new FileInfo(upload.FilePath);
                    //request upload session/uri
                    HttpWebRequest request =
                        await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                            YoutubeUpload.playlistItemsEndpoint, "POST", jsonBytes, "application/json; charset=utf-8");

                    using (Stream dataStream = await request.GetRequestStreamAsync())
                    {
                        dataStream.Write(jsonBytes, 0, jsonBytes.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                    }
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        using (e.Response)
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage += $"Playlist addition failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage += $"Playlist addition failed: {e.ToString()}";
                    }

                    return false;
                }
                catch (Exception e)
                {
                    upload.UploadErrorMessage = $"Playlist addition failed: {e.ToString()}";
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
