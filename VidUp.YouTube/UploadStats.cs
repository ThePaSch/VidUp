﻿#region

using System;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;

#endregion

namespace Drexel.VidUp.Youtube
{
    public class UploadStats
    {
        private bool finished;

        private DateTime currentUploadStart = DateTime.MinValue;

        //sum of uploaded bytes of all finished uploads in this session.
        private long sessionBytesUploadedFinishedUploads;

        //bytes already sent of current upload when upload started.
        private long currentUploadBytesResumed;

        //total remaining bytes to upload of upload list, either with or without uploads to resume, depending on setting. Changes when upload list (add or remove uploads or change status of upload) changes.
        private long sessionTotalBytesToUploadRemaining;

        //total file size in bytes of uploads to be uploaded of upload list, either with or without uploads to resume, depending on setting. Changes when upload list (add or remove uploads or change status of upload) changes.
        private long sessionTotalBytesToUploadFullFilesize;

        //total bytes to upload left
        private long currentTotalBytesLeftRemaining;

        private long currentUploadSpeedInBytesPerSecond;

        private Upload upload;
        

        public float ProgressPercentage
        { 
            get
            {
                if(finished)
                {
                    return 1;
                }
                else
                {
                    return (this.sessionBytesUploadedFinishedUploads + (this.upload.BytesSent - this.currentUploadBytesResumed)) / (float)this.sessionTotalBytesToUploadRemaining;
                }
            } 
        }

        public TimeSpan CurrentFileTimeLeft 
        { 
            get
            {
                if (this.currentUploadSpeedInBytesPerSecond > 0)
                {
                    float seconds = (this.upload.FileLength - this.upload.BytesSent) / (float) this.currentUploadSpeedInBytesPerSecond;
                    return TimeSpan.FromSeconds(seconds);
                }

                return TimeSpan.MinValue;
            }
        }
        public int CurrentFileMbLeft
        {
            get
            {
                return (int)((float)(this.upload.FileLength - this.upload.BytesSent) / Constants.ByteMegaByteFactor);
            }
        }
        public int CurrentFilePercent
        { 
            get
            {
                return (int)((float)this.upload.BytesSent / this.upload.FileLength * 100);
            }
        }

        public int TotalMBLeft
        {
            get
            {
                return (int)((float)this.sessionTotalBytesToUploadFullFilesize / Constants.ByteMegaByteFactor);
            }

        }

        public TimeSpan TotalTimeLeft
        {
            get
            {
                if (this.currentUploadSpeedInBytesPerSecond > 0)
                {
                    float seconds = this.currentTotalBytesLeftRemaining / (float) this.currentUploadSpeedInBytesPerSecond;
                    return TimeSpan.FromSeconds(seconds);
                }

                return TimeSpan.MinValue;
            }
                
        }

        public long CurrentTotalBytesLeftRemaining
        {
            set
            {
                this.currentTotalBytesLeftRemaining = value;
            }
        }
        public long CurrentSpeedInBytesPerSecond
        {
            set
            {
                this.currentUploadSpeedInBytesPerSecond = value;
            }
        }

        public long CurrentSpeedInKiloBytesPerSecond
        { 
            get
            {
                return (long)(this.currentUploadSpeedInBytesPerSecond / 1024f);
            }
        }

        public UploadStats()
        {
        }

        public void UploadsChanged(long sessionTotalBytesToUploadFullFileSize, long sessionTotalBytesToUploadRemaining)
        {
            this.sessionBytesUploadedFinishedUploads = 0;
            this.sessionTotalBytesToUploadRemaining = sessionTotalBytesToUploadRemaining;
            this.sessionTotalBytesToUploadFullFilesize = sessionTotalBytesToUploadFullFileSize;
            this.currentTotalBytesLeftRemaining = sessionTotalBytesToUploadRemaining;
        }

        public void InitializeNewUpload(Upload upload, long currentTotalBytesLeftRemaining)
        {
            this.currentUploadStart = DateTime.Now;
            this.upload = upload;
            this.currentUploadBytesResumed = this.upload.BytesSent;
            this.currentTotalBytesLeftRemaining = currentTotalBytesLeftRemaining;
        }

        public void FinishUpload()
        {
            this.sessionBytesUploadedFinishedUploads += this.upload.FileLength - this.currentUploadBytesResumed;
        }

        public void AllFinished()
        {
            this.finished = true;
        }
    }
}
