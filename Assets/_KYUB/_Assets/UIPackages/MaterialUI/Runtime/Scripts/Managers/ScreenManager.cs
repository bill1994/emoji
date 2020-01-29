//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Managers/ScreenView Manager")]
    public class ScreenManager : MonoBehaviour
    {
        #region Instance

        /// <summary>
        /// The instance in the scene.
        /// </summary>
        private static ScreenManager m_Instance;
        /// <summary>
        /// The instance in the scene.
        /// If null, automatically creates one in the scene.
        /// </summary>
        private static ScreenManager instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new GameObject("ScreenManager").AddComponent<ScreenManager>();
                    m_Instance.InitScreenViewSystem();
                }

                return m_Instance;
            }
        }

        #endregion

        #region Canvas

        /// <summary>
        /// The parent canvas.
        /// </summary>
        [SerializeField]
        private Canvas m_ParentCanvas = null;

        /// <summary>
        /// The rect transform of the manager.
        /// </summary>
        private RectTransform m_RectTransform;
        /// <summary>
        /// The rect transform of the manager.
        /// If null, automatically gets the attached RectTransform.
        /// </summary>
        public static RectTransform rectTransform
        {
            get
            {
                if (instance != null && m_Instance.m_RectTransform == null)
                {
                    if (m_Instance.m_ParentCanvas == null)
                        m_Instance.InitScreenViewSystem();

                    if (m_Instance.m_ParentCanvas != null)
                    {
                        CanvasSafeArea safeArea = m_Instance.m_ParentCanvas.GetComponent<CanvasSafeArea>();
                        instance.m_RectTransform = safeArea != null && safeArea.Content != null ? safeArea.Content : m_Instance.m_ParentCanvas.transform as RectTransform;
                    }
                }

                return instance.m_RectTransform;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            if (!m_Instance)
            {
                m_Instance = this;
                m_Instance.InitScreenViewSystem();
            }
            else if (m_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_Instance == this)
                m_Instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            if (m_Instance == this)
                m_Instance = null;
        }

        #endregion

        #region Init Functions

        private void InitScreenViewSystem()
        {
            m_RectTransform = gameObject.GetAddComponent<RectTransform>();

            if (m_ParentCanvas == null)
                m_ParentCanvas = FindObjectOfType<Canvas>().transform.GetRootCanvas();

            if (m_ParentCanvas != null)
            {
                CanvasSafeArea safeArea = m_ParentCanvas.GetComponent<CanvasSafeArea>();
                transform.SetParent(safeArea != null && safeArea.Content != null ? safeArea.Content : m_ParentCanvas.transform, false);
            }
            transform.localScale = Vector3.one;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
        }

        #endregion

        #region Custom Screen

        public static void ShowCustomScreenAsync<T>(string screenPrefabPath, Transform parent, System.Action<T> initializeCallback, DialogProgress progressIndicator = null, bool p_searchForScreensWithSameName = true) where T : MaterialScreen
        {
            T screenWithSameName = null;
            var screenView = parent != null ? parent.GetComponentInParent<ScreenView>() : null;
            if (screenView != null)
            {
                if (p_searchForScreensWithSameName)
                {
                    foreach (var screen in screenView.materialScreen)
                    {
                        if (screen != null && screen is T && screen.name == screenPrefabPath)
                        {
                            screenWithSameName = screen as T;
                            break;
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
                internalShowCallback(screenWithSameName);
            }

            //Load and show Screen
            else
            {
                DialogProgress currentProgress = progressIndicator;

                System.Action<string, T> internalLoadCallback = (path, dialog) =>
                {
                    //_dialogGenericDialog = dialog;
                    if (dialog != null)
                        dialog.gameObject.SetActive(false);
                    System.Action callbackDelayed = () =>
                    {
                        //Show
                        if (internalShowCallback != null)
                            internalShowCallback.Invoke(dialog);

                        //Hide Progress Indicator
                        currentProgress.Hide();
                    };
                    Kyub.DelayedFunctionUtils.CallFunction(callbackDelayed, 0.5f);
                };

                if (currentProgress == null)
                    currentProgress = DialogManager.ShowProgressModalCircular();
                else
                    currentProgress.Show();
                CreateCustomScreenAsync<T>(screenPrefabPath, parent, internalLoadCallback);
            }
        }

        public static T CreateCustomScreen<T>(string screenPrefabPath, Transform parent) where T : MaterialScreen
        {
            var screenView = parent != null ? parent.GetComponentInParent<ScreenView>() : null;
            T screen = PrefabManager.InstantiateGameObject(screenPrefabPath, screenView != null ? screenView.transform : instance.transform).GetComponent<T>();
            if (screen != null)
                screen.name = screenPrefabPath;

            return screen;
        }

        public static void CreateCustomScreenAsync<T>(string screenPrefabPath, Transform parent, System.Action<string, T> callback) where T : MaterialScreen
        {
            var screenView = parent != null ? parent.GetComponentInParent<ScreenView>() : null;
            System.Action<string, GameObject> internalCallback = (path, screen) =>
            {
                if (screen != null)
                {
                    screen.name = screenPrefabPath;
                    screen.gameObject.SetActive(false);
                }

                T assetComponent = null;
                if (screen != null)
                    assetComponent = screen.GetComponent<T>();
                callback(path, assetComponent);
            };
            PrefabManager.InstantiateGameObjectAsync(screenPrefabPath, screenView != null ? screenView.transform : instance.transform, internalCallback);
        }

        #endregion
    }
}
