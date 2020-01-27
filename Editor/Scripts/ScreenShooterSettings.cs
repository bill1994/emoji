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

using System.Collections.Generic;
using UnityEngine;
using KyubEditor.Screenshot.Configs;
using KyubEditor.Screenshot.Utils;
using UnityEditor;
using System.IO;
using System;

namespace KyubEditor.Screenshot
{
    public class ScreenShooterSettings : ScriptableObject
    {
        private const string RELATIVE_PATH = "Editor/Data/EditorScreenshotSettings.asset";

        [System.NonSerialized]
        public List<Camera> Cameras = new List<Camera>();

        public List<ScreenshotConfig> ScreenshotConfigs = new List<ScreenshotConfig>();
        public string Tag;
        public bool AppendTimestamp;
        public string SaveFolder;

        //---------------------------------------------------------------------
        // Save to Json Functions Static
        //---------------------------------------------------------------------

        public bool ApplyModificationPropertiesToJson(bool forceCreateFile = false)
        {
            try
            {
                var projectSettingsFilePath = GetProjectSettingsFilePath();

                //Only save as json if is an asset clone
                if (forceCreateFile || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(this)))
                {
                    File.WriteAllText(projectSettingsFilePath, EditorJsonUtility.ToJson(this));
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ScreenShooterSettings] ApplyModificationPropertiesToJson Exception:\n" + e);
            };

            return false;
        }

        //---------------------------------------------------------------------
        // Static
        //---------------------------------------------------------------------

        static ScreenShooterSettings _instance = null;
        public static ScreenShooterSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreateFromProjectSettingsJson();
                }

                return _instance;
            }
        }
        
        public static ScreenShooterSettings LoadOrCreateFromProjectSettingsJson()
        {
            var projectSettingsFilePath = GetProjectSettingsFilePath();
            var asset = EditorUtil.LoadFromAsset<ScreenShooterSettings>(RELATIVE_PATH);

            var forceCreateFile = asset == null ||
                ScreenShooterPrefs.HomeFolder != ScreenShooterPrefs.ScriptsFolder;

            if (asset == null)
                asset = ScriptableObject.CreateInstance<ScreenShooterSettings>();
            //This is a Package
            else if (forceCreateFile)
                asset = ScriptableObject.Instantiate(asset);

            if (forceCreateFile)
                asset.name = Path.GetFileName(RELATIVE_PATH);

            if (File.Exists(projectSettingsFilePath))
            {
                try
                {
                    if (forceCreateFile)
                    {
                        var json = File.ReadAllText(projectSettingsFilePath);
                        EditorJsonUtility.FromJsonOverwrite(json, asset);
                    }
                    //Working on original code
                    else
                    {
                        File.Delete(projectSettingsFilePath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ScreenShooterSettings] LoadOrCreateFromProjectSettingsJson Exception:\n" + e);
                };
            }

            asset.ApplyModificationPropertiesToJson(forceCreateFile);

            return asset;
        }

        private static string GetProjectSettingsFilePath()
        {
            var fileName = System.IO.Path.GetFileName(RELATIVE_PATH);
            var projectSettingsPath = Path.Combine(Application.dataPath.Replace("Assets", "ProjectSettings"), fileName).Replace("\\", "/");
            return projectSettingsPath;
        }
    }
}

#endif