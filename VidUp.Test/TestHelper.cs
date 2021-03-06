﻿#region

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

#endregion

namespace Drexel.VidUp.Test
{
    public static class TestHelpers
    {
        public static void CopyTestConfigAndSetCurrentSettings(string fullQualifiedClassName, string testMethodName)
        {
            TestHelpers.getConfigSource(fullQualifiedClassName, testMethodName, out string configSource, out string fullTestName);
            if (string.IsNullOrWhiteSpace(configSource))
            {
                configSource = fullTestName;
            }

            CurrentSettings.StorageFolder = Path.Combine(BaseSettings.StorageFolder, fullTestName);
            Directory.CreateDirectory(CurrentSettings.StorageFolder);

            TestHelpers.copyFile(configSource, CurrentSettings.StorageFolder, "templatelist.json");
            TestHelpers.copyFile(configSource, CurrentSettings.StorageFolder, "uploadlist.json");
            TestHelpers.copyFile(configSource, CurrentSettings.StorageFolder, "uploads.json");
        }

        private static void copyFile(string fullTestName, string testStorageFolder, string fileName)
        {
            string templateListJsonFilePath =
                Path.Combine(TestContext.CurrentContext.TestDirectory, "TestConfigs", fullTestName, fileName);
            if (File.Exists(templateListJsonFilePath))
            {
                string json = File.ReadAllText(templateListJsonFilePath);
                json = json.Replace("{BaseDir}", TestContext.CurrentContext.TestDirectory.Replace("\\", "\\\\"));
                File.WriteAllText(Path.Combine(testStorageFolder, fileName), json);
            }
        }

        private static void getConfigSource(string fullQualifiedClassName, string testMethodName, out string configSource, out string fullTestName)
        {
            fullTestName = TestHelpers.GetTestName(fullQualifiedClassName, testMethodName);

            configSource = null;
            MethodBase method = Type.GetType(fullQualifiedClassName).GetMethod(testMethodName);
            ConfigSourceAttribute[] attributes =
                (ConfigSourceAttribute[])method.GetCustomAttributes(typeof(ConfigSourceAttribute), true);
            if (attributes.Length > 0)
            {
                ConfigSourceAttribute attribute = attributes[0];
                configSource = attribute.Source;
            }
        }

        public static string GetTestName(string fullQualifiedClassName, string testMethodName)
        {
            int startIndex = fullQualifiedClassName.LastIndexOf('.') + 1;
            string className = fullQualifiedClassName.Substring(startIndex,
                fullQualifiedClassName.Length - startIndex);
            return string.Format("{0}_{1}", className, testMethodName);
        }
    }
}
