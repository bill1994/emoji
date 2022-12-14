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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using KyubEditor.Screenshot.Configs;
using KyubEditor.Screenshot.Utils;
using UnityEditor;
using UnityEngine;
using GameViewSizeType = KyubEditor.Screenshot.Utils.GameViewUtil.GameViewSizeType;

namespace KyubEditor.Screenshot
{    
    public class ScreenShooterWindow : EditorWindow
    {
        private UnityEditorInternal.ReorderableList _list;

        private UnityEditorInternal.ReorderableList _camerasList;

        private static bool _isMakingScreenshotsNow;
        private bool _hasErrors;

        private Texture2D _takeButtonIcon;
        private Texture2D _cameraIcon;
        private Texture2D _configsIcon;
        private Texture2D _folderIcon;

        private Texture2D _takeButtonNormal;
        private Texture2D _takeButtonActive;
        private GUIStyle _buttonStyle;

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        [MenuItem("Window/Editor Screen Shooter")]
        protected static void ShowWindow()
        {
            var window = (ScreenShooterWindow) GetWindow(typeof(ScreenShooterWindow));
            window.autoRepaintOnSceneChange = true;
            window.titleContent = new GUIContent("Editor Screen Shooter");
            window.Show();
        }

        /*[MenuItem("Window/Editor Screen Shooter/Take Screenshots &#s")]
        private static void TakeScreenshotOnHotkey()
        {
            EditorCoroutine.Start(TakeScreenshots());
        }*/

        protected void OnEnable()
        {
            _cameraIcon = EditorUtil.GetCameraIcon();
            _configsIcon = EditorUtil.GetConfigsIcon();
            _folderIcon = EditorUtil.GetFolderIcon();
            _takeButtonNormal = EditorUtil.GetButtonNormalTexture();
            _takeButtonActive = EditorUtil.GetButtonActiveTexture();
            _takeButtonIcon = EditorUtil.GetScreenshotsIcon();

            // Reset button style, bcz it can be initialized only on GUI section
            _buttonStyle = null;

            // Init reorderable list if required
            _list = _list ?? ReorderableConfigsList.Create(ScreenShooterSettings.Instance.ScreenshotConfigs, MenuItemHandler);

            _camerasList = new UnityEditorInternal.ReorderableList(ScreenShooterSettings.Instance.Cameras, typeof(Camera), true, false, true, true);
            _camerasList.headerHeight = 0;
            _camerasList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                _camerasList.list[index] = EditorGUI.ObjectField(rect, _camerasList.list[index] as Camera, typeof(Camera), true);
            };
        }

        protected void OnGUI()
        {
            _hasErrors = false;
            GUI.changed = false;
            GUI.enabled = !_isMakingScreenshotsNow;

            Undo.RecordObject(ScreenShooterSettings.Instance, "ScreenShooter settings");

            OnGUICameraInput();            
            OnGUIScreenshotConfigs();
            OnGUISaveFolderInput();
            OnGUITakeButton();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(ScreenShooterSettings.Instance);
                ScreenShooterSettings.Instance.ApplyModificationPropertiesToJson();
            }
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void OnGUICameraInput()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_cameraIcon, GUILayout.Width(24));
            GUILayout.Label("Camera", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            //if (_settings.Camera == null) _settings.Camera = Camera.main;
            _camerasList.DoLayoutList();
            if (ScreenShooterSettings.Instance.Cameras == null || ScreenShooterSettings.Instance.Cameras.Count == 0)
            {
                EditorGUILayout.HelpBox("No cameras selected, Game View buffer will be used instead", MessageType.Info);
            }
            EditorGUILayout.Space();
        }

        private void OnGUIScreenshotConfigs()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_configsIcon, GUILayout.Width(24));
            GUILayout.Label("Screenshots", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            _list.DoLayoutList();
            EditorGUILayout.Space();

            ScreenShooterSettings.Instance.Tag = EditorGUILayout.TextField("Tag", ScreenShooterSettings.Instance.Tag);
            EditorGUILayout.Space();

            ScreenShooterSettings.Instance.AppendTimestamp = EditorGUILayout.Toggle("Timestamp", ScreenShooterSettings.Instance.AppendTimestamp);
            EditorGUILayout.Space();
        }

        private void OnGUISaveFolderInput()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_folderIcon, GUILayout.Width(24));
            GUILayout.Label("Save To", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            ScreenShooterSettings.Instance.SaveFolder = EditorGUILayout.TextField(ScreenShooterSettings.Instance.SaveFolder);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled &= Directory.Exists(ScreenShooterSettings.Instance.SaveFolder);
            if (GUILayout.Button("Show", GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("file://" + Path.GetFullPath(ScreenShooterSettings.Instance.SaveFolder));
            }
            GUI.enabled = !_isMakingScreenshotsNow;

            if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
            {
                ScreenShooterSettings.Instance.SaveFolder = EditorUtility.SaveFolderPanel("Save screenshots to:", ScreenShooterSettings.Instance.SaveFolder, string.Empty);
                GUI.FocusControl("Browse");
            }

            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(ScreenShooterSettings.Instance.SaveFolder) || ScreenShooterSettings.Instance.SaveFolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                EditorGUILayout.HelpBox("Folder path is empty or contains invalid characters.", MessageType.Error);
                _hasErrors = true;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        private void OnGUITakeButton()
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    normal = {background = _takeButtonNormal},
                    active = {background = _takeButtonActive}
                };
            }

            GUI.enabled = !_hasErrors && !_isMakingScreenshotsNow;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(_takeButtonIcon, _buttonStyle, GUILayout.Width(200f)))
            {
                EditorCoroutine.Start(TakeScreenshots());
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        private static IEnumerator TakeScreenshots()
        {
            _isMakingScreenshotsNow = true;
            var currentIndex = GameViewUtil.GetCurrentSizeIndex();

            // Slow down and unpause editor if required
            var paused = EditorApplication.isPaused;
            var timeScale = Time.timeScale;            

            try
            {
                EditorApplication.isPaused = false;
                Time.timeScale = 0.001f;

                var configsCount = ScreenShooterSettings.Instance.ScreenshotConfigs.Count;
                for (var i = 0; i < configsCount; i++)
                {
                    var data = ScreenShooterSettings.Instance.ScreenshotConfigs[i];

                    // Show progress
                    var info = (i + 1) + " / " + configsCount + " - " + data.Name;
                    EditorUtility.DisplayProgressBar("Taking Screenshots", info, (float) (i + 1)/configsCount);

                    // apply custom resolution for game view
                    var sizeType = GameViewSizeType.FixedResolution;
                    var sizeGroupType = GameViewUtil.GetCurrentGroupType();
                    var sizeName = "scr_" + data.Width + "x" + data.Height;

                    if (!GameViewUtil.IsSizeExist(sizeGroupType, sizeName))
                    {
                        GameViewUtil.AddCustomSize(sizeType, sizeGroupType, data.Width, data.Height, sizeName);
                    }

                    var index = GameViewUtil.FindSizeIndex(sizeGroupType, sizeName);
                    GameViewUtil.SetSizeByIndex(index);

                    // add some delay while applying changes
                    var lastFrameTime = EditorApplication.timeSinceStartup;
                    while (EditorApplication.timeSinceStartup - lastFrameTime < 1f) yield return null;
                    GameViewUtil.GetGameViewWindow().Repaint();

                    yield return new WaitForEndOfFrame();

                    ScreenshotUtil.TakeScreenshot(ScreenShooterSettings.Instance, data);

                    yield return new WaitForSecondsRealtime(1f);

                    // just clean it up
                    GameViewUtil.RemoveCustomSize(sizeGroupType, index);
                }                
            }
            finally
            {
                // Restore pause state and time scale
                EditorApplication.isPaused = paused;
                Time.timeScale = timeScale;

                GameViewUtil.SetSizeByIndex(currentIndex);
                EditorUtility.ClearProgressBar();

                _isMakingScreenshotsNow = false;
            }                        
        }
        
        private void MenuItemHandler(object target)
        {
            ScreenShooterSettings.Instance.ScreenshotConfigs.Add(target as ScreenshotConfig);
        }
    }
}

#endif