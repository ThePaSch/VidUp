﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Drexel.VidUp.Business;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#endregion

namespace Drexel.VidUp.JSON
{
    //todo: maybe make class not static...
    public class JsonSerialization
    {

        private static string serializationFolder;

        private static string uploadListFilePath;
        private static string templateListFilePath;
        private static string allUploadsFilePath;
        public static UploadList UploadList;
        public static TemplateList TemplateList;

        public static string SerializationFolder
        {
            set
            {
                JsonSerialization.serializationFolder = value;
            }
            get
            {
                return JsonSerialization.serializationFolder;
            }
        }
        public static void Initialize()
        {
            JsonSerialization.uploadListFilePath = Path.Combine(JsonSerialization.serializationFolder, "uploadlist.json");
            JsonSerialization.templateListFilePath = Path.Combine(JsonSerialization.serializationFolder, "templatelist.json");
            JsonSerialization.allUploadsFilePath = Path.Combine(JsonSerialization.serializationFolder, "uploads.json");
        }
        public static void DeserializeUploadList()
        {
            if (!File.Exists(JsonSerialization.uploadListFilePath))
            {
                return;
            }

            JsonSerializer serializer = new JsonSerializer();

            //this converters returns existing Upload objects.
            serializer.Converters.Add(new UploadGuidStringConverter());

            using (StreamReader sr = new StreamReader(uploadListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeSerializationRepository.UploadList = serializer.Deserialize<UploadList>(reader);
            }

            DeSerializationRepository.UploadList.SetEventListeners();
        }

        public static void Deserialize()
        {
            JsonSerialization.DeserializeAllUploads();
            JsonSerialization.DeserializeTemplateList();
            JsonSerialization.DeserializeUploadList();
        }

        private static void DeserializeAllUploads()
        {
            if (!File.Exists(JsonSerialization.allUploadsFilePath))
            {
                return;
            }
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new TemplateGuidStringConverter());

            using (StreamReader sr = new StreamReader(JsonSerialization.allUploadsFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeSerializationRepository.AllUploads = serializer.Deserialize<List<Upload>>(reader);
            }

            foreach (Upload upload in DeSerializationRepository.AllUploads)
            {
                if (upload.UploadStatus == UplStatus.Uploading)
                {
                    Type myType = typeof(Upload);
                    FieldInfo myFieldInfo = myType.GetField("uploadStatus", BindingFlags.NonPublic | BindingFlags.Instance);
                    myFieldInfo.SetValue(upload, UplStatus.Stopped);
                }
            }
        }

        public static void DeserializeTemplateList()
        {
            if(!File.Exists(JsonSerialization.templateListFilePath))
            {
                return;
            }

            List<Template> list;
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new UploadGuidStringConverter());

            //serializer.Converters.Add(new MonthRelativeCombinationConverter());

            using (StreamReader sr = new StreamReader(templateListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                list = serializer.Deserialize<List<Template>>(reader);
            }

            //todo: maybe move to ondeserializing attribute method on template or templatelist
            foreach (Template template in list)
            {
                foreach (Upload upload in template.Uploads)
                {
                    Type myType = typeof(Upload);
                    FieldInfo myFieldInfo = myType.GetField("template", BindingFlags.NonPublic | BindingFlags.Instance);
                    myFieldInfo.SetValue(upload, template);
                }
            }

            DeSerializationRepository.Templates = list;
        }
        public static void SerializeAllUploads()
        {
            List<Upload> allUploads = new List<Upload>();
            foreach (Upload upload in JsonSerialization.UploadList)
            {
                allUploads.Add(upload);
            }

            foreach (Template template in JsonSerialization.TemplateList)
            {
                foreach(Upload upload in template.Uploads)
                {
                    if(!allUploads.Contains(upload))
                    {
                        allUploads.Add(upload);
                    }
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new TemplateGuidStringConverter());
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(JsonSerialization.allUploadsFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, allUploads);
            }
        }

        //on UploadList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public static void SerializeUploadList()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(JsonSerialization.uploadListFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, JsonSerialization.UploadList);
            }
        }

        //on TemplateList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public static void SerializeTemplateList()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Formatting = Formatting.Indented;
            serializer.Converters.Add(new UploadGuidStringConverter());

            using (StreamWriter sw = new StreamWriter(templateListFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, JsonSerialization.TemplateList.GetReadonlyTemplateList());
            }
        }
    }
}
