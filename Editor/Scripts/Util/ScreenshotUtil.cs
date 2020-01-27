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
using System.Collections.Generic;
using System.IO;
using KyubEditor.Screenshot.Configs;
using UnityEngine;

namespace KyubEditor.Screenshot.Utils
{
    public static class ScreenshotUtil
    {
        public static void TakeScreenshot(ScreenShooterSettings settings, ScreenshotConfig config)
        {
            var suffix = settings.AppendTimestamp ? "." + DateTime.Now.ToString("yyyyMMddHHmmssfff") : "";
            TakeScreenshot(settings.Cameras, settings.SaveFolder, settings.Tag, suffix, config);
        }

        public static void TakeScreenshot(IList<Camera> cameras, string folderName, string prefix, string suffix, ScreenshotConfig screenshotConfig)
        {
            Texture2D scrTexture = null;
            scrTexture = new Texture2D(screenshotConfig.Width, screenshotConfig.Height, TextureFormat.RGB24, false);

            if (cameras != null && cameras.Count > 0)
            {
                //scrTexture = new Texture2D(screenshotConfig.Width, screenshotConfig.Height, TextureFormat.RGB24, false);
                var scrRenderTexture = new RenderTexture(scrTexture.width, scrTexture.height, 24);

                foreach (var camera in cameras)
                {
                    var camRenderTexture = camera.targetTexture;
                    camera.targetTexture = scrRenderTexture;
                    camera.Render();
                    camera.targetTexture = camRenderTexture;
                }

                RenderTexture.active = scrRenderTexture;
                scrTexture.ReadPixels(new Rect(0, 0, scrTexture.width, scrTexture.height), 0, 0);
                scrTexture.Apply();

                SaveTextureAsFile(scrTexture, folderName, prefix, suffix, screenshotConfig);
            }
            else
            {
                var imageFilePath = GetScreenshotPath(folderName, prefix, suffix, screenshotConfig);

                //float scale = Mathf.Max(1, screenshotConfig.Width / Screen.width, screenshotConfig.Height / Screen.height);
                ScreenCapture.CaptureScreenshot(imageFilePath);

                Debug.Log("Image saved to: " + imageFilePath);
            }
        }

        public static void SaveTextureAsFile(Texture2D texture, string folder, string prefix, string suffix, ScreenshotConfig screenshotConfig)
        {
            byte[] bytes;

            switch (screenshotConfig.Type)
            {
                case ScreenshotConfig.Format.PNG:
                    bytes = texture.EncodeToPNG();
                    break;
                case ScreenshotConfig.Format.JPG:
                    bytes = texture.EncodeToJPG();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var imageFilePath = GetScreenshotPath(folder, prefix, suffix, screenshotConfig);

            // ReSharper disable once PossibleNullReferenceException
            File.WriteAllBytes(imageFilePath, bytes);

            Debug.Log("Image saved to: " + imageFilePath);
        }

        private static string MakeValidFileName(string name)
        {
            var invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        private static string GetScreenshotPath(string folder, string prefix, string suffix, ScreenshotConfig screenshotConfig)
        {
            string extension;

            switch (screenshotConfig.Type)
            {
                case ScreenshotConfig.Format.PNG:
                    extension = ".png";
                    break;
                case ScreenshotConfig.Format.JPG:
                    extension = ".jpg";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var fileName = prefix + screenshotConfig.Name + "." + screenshotConfig.Width + "x" + screenshotConfig.Height + suffix;
            var imageFilePath = folder + "/" + MakeValidFileName(fileName + extension);

            // ReSharper disable once PossibleNullReferenceException
            (new FileInfo(imageFilePath)).Directory.Create();

            return imageFilePath;
        }
    }
}

#endif