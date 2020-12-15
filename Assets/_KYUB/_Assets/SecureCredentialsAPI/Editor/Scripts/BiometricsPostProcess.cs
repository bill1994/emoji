#if UNITY_IPHONE && UNITY_EDITOR

using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.Text;
using UnityEditor.iOS.Xcode.Extensions;
using System.Collections.Generic;


//Adds required NSFaceIDUsageDescription to the iOS project plist (required in iOS 11)
namespace KyubEditor.Credentials
{
    public class BiometricsPostProcessor
    {
		const string KEY_FACEID = "NSFaceIDUsageDescription";
        static string KEY_FACEID_DESCRIPTION = "Access to FaceId is required in order to auto-login into app.";

        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
 
            if (buildTarget == BuildTarget.iOS) 
            {
                // Get plist
                string plistPath = pathToBuiltProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
       
                // Get root
                PlistElementDict rootDict = plist.root;
       
                // Change NSFaceId Description
                rootDict.SetString(KEY_FACEID, KEY_FACEID_DESCRIPTION);
       
                // Write to file
                File.WriteAllText(plistPath, plist.WriteToString());
            }
        }
    }
}

#elif UNITY_EDITOR && UNITY_ANDROID

using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;

namespace KyubEditor.Credentials
{
    public class BiometricsPostProcessor : IPostGenerateGradleAndroidProject
    {
        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            var androidManifest = new AndroidManifest(GetManifestPath(basePath));

            androidManifest.SetUseFeatureHardwareFingerprint();
            androidManifest.SetPermissionBiometric();
            androidManifest.SetPermissionFingerPrint();

            androidManifest.Save();
        }

        public int callbackOrder { get { return 1; } }

        private string _manifestFilePath;

        private string GetManifestPath(string basePath)
        {
            if (string.IsNullOrEmpty(_manifestFilePath))
            {
                var pathBuilder = new StringBuilder(basePath);
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
                _manifestFilePath = pathBuilder.ToString();
            }
            return _manifestFilePath;
        }
    }

    class AndroidXmlDocument : XmlDocument
    {
        private string m_Path;
        protected XmlNamespaceManager nsMgr;
        public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
        public AndroidXmlDocument(string path)
        {
            m_Path = path;
            using (var reader = new XmlTextReader(m_Path))
            {
                reader.Read();
                Load(reader);
            }
            nsMgr = new XmlNamespaceManager(NameTable);
            nsMgr.AddNamespace("android", AndroidXmlNamespace);
        }

        public string Save()
        {
            return SaveAs(m_Path);
        }

        public string SaveAs(string path)
        {
            using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
            {
                writer.Formatting = Formatting.Indented;
                Save(writer);
            }
            return path;
        }
    }

    internal class AndroidManifest : AndroidXmlDocument
    {
        private readonly XmlElement ApplicationElement;

        public AndroidManifest(string path) : base(path)
        {
            ApplicationElement = SelectSingleNode("/manifest/application") as XmlElement;
        }

        private XmlAttribute CreateAndroidAttribute(string key, string value)
        {
            XmlAttribute attr = CreateAttribute("android", key, AndroidXmlNamespace);
            attr.Value = value;
            return attr;
        }

        internal XmlNode GetActivityWithLaunchIntent()
        {
            return SelectSingleNode("/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and " +
                    "intent-filter/category/@android:name='android.intent.category.LAUNCHER']", nsMgr);
        }

        internal void SetApplicationTheme(string appTheme)
        {
            ApplicationElement.Attributes.Append(CreateAndroidAttribute("theme", appTheme));
        }

        internal void SetStartingActivityName(string activityName)
        {
            GetActivityWithLaunchIntent().Attributes.Append(CreateAndroidAttribute("name", activityName));
        }

        internal void SetPermissionBiometric()
        {
            var manifest = SelectSingleNode("/manifest");
            XmlElement child = CreateElement("uses-permission");
            manifest.AppendChild(child);
            XmlAttribute newAttribute = CreateAndroidAttribute("name", "android.permission.USE_BIOMETRIC");
            child.Attributes.Append(newAttribute);
        }

        internal void SetPermissionFingerPrint()
        {
            var manifest = SelectSingleNode("/manifest");
            XmlElement child = CreateElement("uses-permission");
            manifest.AppendChild(child);
            XmlAttribute newAttribute = CreateAndroidAttribute("name", "android.permission.USE_FINGERPRINT");
            child.Attributes.Append(newAttribute);
        }

        internal void SetUseFeatureHardwareFingerprint()
        {
            var manifest = SelectSingleNode("/manifest");
            XmlElement child = CreateElement("uses-feature");
            manifest.AppendChild(child);
            XmlAttribute newAttribute = CreateAndroidAttribute("name", "android.hardware.fingerprint");
            child.Attributes.Append(newAttribute);
            newAttribute = CreateAndroidAttribute("required", "false");
            child.Attributes.Append(newAttribute);
        }
    }
}
#endif