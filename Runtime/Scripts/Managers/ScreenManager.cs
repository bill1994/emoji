// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using UnityEngine;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Managers/ScreenView Manager")]
    public class ScreenManager : Kyub.Singleton<ScreenManager>
    {
        #region Canvas

        public static RectTransform rectTransform
        {
            get
            {
                return DialogManager.rectTransform;
            }
        }

        public static Canvas parentCanvas
        {
            get
            {
                return DialogManager.parentCanvas;
            }
        }

        #endregion

        #region Custom Screen

        public static void ShowCustomScreenAsync<T>(string screenPrefabPath, Transform parent, System.Action<T> initializeCallback, DialogProgress progressIndicator = null, bool searchForScreensWithSameName = true, float delay = 0.5f) where T : MaterialScreen
        {
            var screenView = parent != null ? parent.GetComponentInParent<ScreenView>() : null;
            ShowCustomScreenAsync(screenPrefabPath, screenView, initializeCallback, progressIndicator, searchForScreensWithSameName, delay);
        }

        public static void ShowCustomScreenAsync<T>(string screenPrefabPath, ScreenView screenView, System.Action<T> initializeCallback, DialogProgress progressIndicator = null, bool searchForScreensWithSameName = true, float delay = 0.5f) where T : MaterialScreen
        {
            T screenWithSameName = null;
            var partialNamePath = screenPrefabPath != null ? System.IO.Path.GetFileName(screenPrefabPath) : string.Empty;
            if (screenView != null)
            {
                if (searchForScreensWithSameName)
                {
                    foreach (var screen in screenView.materialScreen)
                    {
                        if (screen != null && screen is T)
                        {
                            var fullFind = screen.name == screenPrefabPath;
                            var partialFind = !fullFind && screenWithSameName == null && screenPrefabPath != null && screen.name == partialNamePath;
                            if (fullFind || partialFind)
                            {
                                screenWithSameName = screen as T;

                                //We only stop match if screen.name exactly equals screenPrefabPath
                                if (fullFind)
                                    break;
                            }
                        }
                    }
                }
            }

            System.Action<T> internalShowCallback = (screen) =>
            {
                if (screen != null)
                {
                    if (screenView != null)
                    {
                        if (!screenView.materialScreen.Contains(screen))
                            screenView.materialScreen.Add(screen);
                    }
                    //Init
                    if (initializeCallback != null)
                        initializeCallback.Invoke(screen);

                    if (screenView != null)
                        screen.Show();
                    else
                        Debug.Log("Invalid ScreenView");
                }
            };
            //Show Pre-Loaded Screen
            if (screenWithSameName != null)
            {
                //Prevent immediate execution
                System.Action callbackDelayed = () =>
                {
                    internalShowCallback(screenWithSameName);
                };
                Kyub.ApplicationContext.RunOnMainThread(callbackDelayed, 0.01f);
            }

            //Load and show Screen
            else
            {
                DialogProgress currentProgress = progressIndicator;

                System.Action<string, T> internalLoadCallback = (path, dialog) =>
                {
                    //_dialogGenericDialog = dialog;
                    //if (dialog != null)
                    //    dialog.gameObject.SetActive(false);
                    System.Action callbackDelayed = () =>
                    {
                        //Show
                        if (internalShowCallback != null)
                            internalShowCallback.Invoke(dialog);

                        //Hide Progress Indicator
                        currentProgress.Hide();
                    };
                    Kyub.ApplicationContext.RunOnMainThread(callbackDelayed, delay);
                };

                if (currentProgress == null)
                    currentProgress = DialogManager.ShowProgressModalCircular();
                else
                    currentProgress.Show();
                CreateCustomScreenAsync<T>(screenPrefabPath, screenView, internalLoadCallback);
            }
        }

        public static T CreateCustomScreen<T>(string screenPrefabPath, Transform parent) where T : MaterialScreen
        {
            var screenView = parent != null ? parent.GetComponentInParent<ScreenView>() : null;
            return CreateCustomScreen<T>(screenPrefabPath, screenView);
        }

        public static T CreateCustomScreen<T>(string screenPrefabPath, ScreenView screenView) where T : MaterialScreen
        {
            T screen = PrefabManager.InstantiateGameObject(screenPrefabPath, screenView != null ? screenView.transform : Instance.transform, false).GetComponent<T>();
            if (screen != null)
                screen.name = screenPrefabPath;

            return screen;
        }

        public static void CreateCustomScreenAsync<T>(string screenPrefabPath, Transform parent, System.Action<string, T> callback) where T : MaterialScreen
        {
            var screenView = parent != null ? parent.GetComponentInParent<ScreenView>() : null;
            CreateCustomScreenAsync(screenPrefabPath, screenView, callback);
        }

        public static void CreateCustomScreenAsync<T>(string screenPrefabPath, ScreenView screenView, System.Action<string, T> callback) where T : MaterialScreen
        {
            System.Action<string, GameObject> internalCallback = (path, screen) =>
            {
                if (screen != null)
                {
                    screen.name = screenPrefabPath;
                    //screen.gameObject.SetActive(false);
                }

                T assetComponent = null;
                if (screen != null)
                    assetComponent = screen.GetComponent<T>();
                callback(path, assetComponent);
            };
            PrefabManager.InstantiateGameObjectAsync(screenPrefabPath, screenView != null ? screenView.transform : Instance.transform, internalCallback, false);
        }

        #endregion
    }
}
