/*
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace KyubEditor.Screenshot
{
    public class ScreenShooterPrefs
    {
        private const string HOME_FOLDER_PREF_KEY = "KyubEditor.Screenshot.HomeFolder.";
        private const string HOME_FOLDER = "Assets";
        private const string HOME_FOLDER_HINT = "Change this setting to the new location of the \"EditorScreenshotAPI\" folder if you move it around in your project.";

        private static string _homeFolder = null;
        public static string HomeFolder
        {
            get
            {
                if (_homeFolder == null)
                {
                    var mainPath = ScriptsFolder;
                    if (mainPath.Contains("Packages"))
                        mainPath = HOME_FOLDER;
                    _homeFolder = mainPath;
                }
                return _homeFolder;
            }
        }

        private static string _scriptsFolder = null;
        public static string ScriptsFolder
        {
            get
            {
                if (_scriptsFolder == null)
                    _scriptsFolder = GetMainScriptsFolderPath();
                return _scriptsFolder;
            }
        }

        public static string GetMainScriptsFolderPath()
        {
            var packagePath = "Packages/com.kyub.editorscreenshot";
            //Try discover if script is inside Package or in AssetFolder
            var fullPackagePath = System.IO.Path.GetFullPath(packagePath).Replace("\\", "/");
            if (!System.IO.Directory.Exists(fullPackagePath))
                fullPackagePath = "";

            string[] files = System.IO.Directory.GetFiles(string.IsNullOrEmpty(fullPackagePath) ? Application.dataPath : fullPackagePath, "ScreenShooterPrefs.cs", System.IO.SearchOption.AllDirectories);

            var folderPath = files.Length > 0 ? files[0].Replace("\\", "/") : "";
            if (!string.IsNullOrEmpty(folderPath))
            {
                var keyFolderPath = "/EditorScreenshotAPI/";
                if (folderPath.Contains(keyFolderPath))
                    folderPath = folderPath.Split(new string[] { keyFolderPath }, System.StringSplitOptions.None)[0] + keyFolderPath;
                else
                    folderPath = fullPackagePath;

                if (string.IsNullOrEmpty(fullPackagePath))
                    folderPath = folderPath.Replace(Application.dataPath, "Assets");
                else
                    folderPath = folderPath.Replace(fullPackagePath, packagePath); //Support new Package Manager file system
            }

            return folderPath;
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

/*#if !UNITY_2018_3_OR_NEWER
        [PreferenceItem(AssetInfo.NAME)]
        public static void EditorPreferences()
        {
            EditorGUILayout.HelpBox(HOME_FOLDER_HINT, MessageType.Info);
            EditorGUILayout.Separator();
            HomeFolder.Draw();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Version " + AssetInfo.VERSION, EditorStyles.centeredGreyMiniLabel);
        }
#endif*/

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static string ProjectName
        {
            get
            {
                var s = Application.dataPath.Split('/');
                var p = s[s.Length - 2];
                return p;
            }
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        public abstract class EditorPrefsItem<T>
        {
            public string Key;
            public string Label;
            public T DefaultValue;

            protected EditorPrefsItem(string key, string label, T defaultValue)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException("key");
                }

                Key = key;
                Label = label;
                DefaultValue = defaultValue;
            }

            public abstract T Value { get; set; }
            public abstract void Draw();

            public static implicit operator T(EditorPrefsItem<T> s)
            {
                return s.Value;
            }
        }

        public class EditorPrefsString : EditorPrefsItem<string>
        {
            public EditorPrefsString(string key, string label, string defaultValue)
                : base(key, label, defaultValue)
            {
            }

            public override string Value
            {
                get { return EditorPrefs.GetString(Key, DefaultValue); }
                set { EditorPrefs.SetString(Key, value); }
            }

            public override void Draw()
            {
                EditorGUIUtility.labelWidth = 100f;
                Value = EditorGUILayout.TextField(Label, Value);
            }
        }
    }
}

#endif