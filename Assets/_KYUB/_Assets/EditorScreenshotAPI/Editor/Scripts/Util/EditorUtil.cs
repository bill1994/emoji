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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KyubEditor.Screenshot.Utils
{
    public static class EditorUtil
    {
        private const string LOAD_ASSET_ERROR_MSG = "Could not load {0}\n" +
                                                    "Did you move the \"ScreenShooter\" folder around in your project? " +
                                                    "Go to \"Preferences -> ScreenShooter\" and update the location of the asset.";

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        static Dictionary<string, Object> packageAssetInstancesCache = new Dictionary<string, Object>();
        public static T LoadFromAsset<T>(string relativePath) where T : UnityEngine.Object
        {
            T asset = null;

            var assetPath = Path.Combine(ScreenShooterPrefs.HomeFolder, relativePath).Replace("\\", "/");
            var fullAssetPath = System.IO.Path.GetFullPath(assetPath);
            
            if (!File.Exists(fullAssetPath))
            {
                var packagePath = Path.Combine(ScreenShooterPrefs.ScriptsFolder, relativePath).Replace("\\", "/");
                var fullPackagePath = System.IO.Path.GetFullPath(packagePath);
                if (File.Exists(fullPackagePath))
                    asset = AssetDatabase.LoadAssetAtPath<T>(packagePath);
            }
            else
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset == null)
                Debug.LogError(string.Format(LOAD_ASSET_ERROR_MSG, assetPath));
            return asset;
        }

        //---------------------------------------------------------------------
        // Textures
        //---------------------------------------------------------------------

        public static Texture2D GetTexture(string filename)
        {
            var skinFolder = (EditorGUIUtility.isProSkin) ? "Professional/" : "Personal/";
            return LoadFromAsset<Texture2D>("Editor/Textures/" + skinFolder + filename);
        }

        public static Texture2D GetCameraIcon()
        {
            return GetTexture("CameraIcon.png");
        }

        public static Texture2D GetConfigsIcon()
        {
            return GetTexture("ConfigsIcon.png");
        }

        public static Texture2D GetFolderIcon()
        {
            return GetTexture("FolderIcon.png");
        }

        public static Texture2D GetScreenshotsIcon()
        {
            return GetTexture("TakeScreenshotsIcon.png");
        }

        public static Texture2D GetButtonNormalTexture()
        {
            return GetTexture("TakeButtonNormal.png");
        }

        public static Texture2D GetButtonActiveTexture()
        {
            return GetTexture("TakeButtonActive.png");
        }
    }
}

#endif