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


//Prevent iOS 13 DarkMode to change StatusBar Text Color
namespace KyubEditor.Credentials
{
    public class SafeAreaPostProcessor
    {
		const string KEY_UIUSER = "UIUserInterfaceStyle";
        static string KEY_UIUSER_DESCRIPTION = "Light";

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

                // Change UIUserInterfaceStyle Type
                rootDict.SetString(KEY_UIUSER, KEY_UIUSER_DESCRIPTION);
       
                // Write to file
                File.WriteAllText(plistPath, plist.WriteToString());
            }
        }
    }
}

#endif