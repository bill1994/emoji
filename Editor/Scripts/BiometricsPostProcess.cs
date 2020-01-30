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

#endif