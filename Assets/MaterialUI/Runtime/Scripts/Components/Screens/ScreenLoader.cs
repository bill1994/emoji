using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    public class ScreenLoader : MonoBehaviour
    {
        #region Private Variables

        [SerializeField]
        protected float m_delay = 0.5f;
        [SerializeField]
        protected ScreenView m_screenView = null;
        [SerializeField]
        protected bool m_tryFindRootView = true;

        ScreenView _cachedScreenView = null;
        #endregion

        #region Public Properties

        public float Delay
        {
            get
            {
                return m_delay;
            }
            set
            {
                m_delay = value;
            }
        }

        #endregion

        #region Public Properties

        public bool TryFindRootView
        {
            get
            {
                return m_tryFindRootView;
            }
            set
            {
                if (m_tryFindRootView == value)
                    return;
                m_tryFindRootView = value;
                _cachedScreenView = null;
            }
        }

        public ScreenView ScreenView
        {
            get
            {
                if (this != null && m_screenView == null)
                {
                    if (_cachedScreenView == null)
                    {
                        if (m_tryFindRootView)
                        {
                            var screenviews = GetComponentsInParent<ScreenView>();
                            _cachedScreenView = screenviews.Length > 0 ? screenviews[screenviews.Length - 1] : null;
                        }
                        else
                            _cachedScreenView = GetComponentInParent<ScreenView>();
                    }
                    return _cachedScreenView;
                }
                else
                    _cachedScreenView = null;

                return m_screenView;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnDisable()
        {
            _cachedScreenView = null;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _cachedScreenView = null;
        }
#endif

        #endregion

        #region Load Screen

        public void ForceLoadAndShowScreen(string screenResourcesName)
        {
            LoadAndShowScreen_Internal(screenResourcesName, false, false, null);
        }

        public void ForceLoadAndShowScreen(string screenResourcesName, System.Action<MaterialScreen> onStackScreen)
        {
            LoadAndShowScreen_Internal(screenResourcesName, false, false, onStackScreen);
        }

        public void ForceLoadAndReplaceScreen(string screenResourcesName)
        {
            LoadAndShowScreen_Internal(screenResourcesName, false, true, null);
        }

        public void ForceLoadAndReplaceScreen(string screenResourcesName, System.Action<MaterialScreen> onStackScreen)
        {
            LoadAndShowScreen_Internal(screenResourcesName, false, true, onStackScreen);
        }

        public void LoadAndShowScreen(string screenResourcesName)
        {
            LoadAndShowScreen_Internal(screenResourcesName, true, false, null);
        }

        public void LoadAndShowScreen(string screenResourcesName, System.Action<MaterialScreen> onStackScreen)
        {
            LoadAndShowScreen_Internal(screenResourcesName, true, false, onStackScreen);
        }

        public void LoadAndReplaceScreen(string screenResourcesName)
        {
            LoadAndShowScreen_Internal(screenResourcesName, true, true, null);
        }

        public void LoadAndReplaceScreen(string screenResourcesName, System.Action<MaterialScreen> onStackScreen)
        {
            LoadAndShowScreen_Internal(screenResourcesName, true, true, onStackScreen);
        }

        protected void LoadAndShowScreen_Internal(string screenResourcesName, bool searchForScreensWithSameName, bool removePreviousStack, System.Action<MaterialScreen> onStackScreen)
        {
            if (ScreenView == null)
            {
                Debug.LogError("ScreenView is null");
                return;
            }
            else
            {
                MaterialScreen screenWithSameName = searchForScreensWithSameName ? ScreenView.GetScreenWithName(screenResourcesName) : null;
                if (screenWithSameName == null)
                {
                    var lastScreen = ScreenView.currentScreen;
                    ScreenManager.ShowCustomScreenAsync<MaterialScreen>(screenResourcesName,
                        ScreenView != null ? ScreenView.transform : null,
                        (screen) =>
                        {
                            if (this == null)
                                return;

                            if (screen != null && screen != lastScreen && removePreviousStack)
                            {
                                ScreenView.RemoveFromScreenStack(lastScreen);
                            }
                            if (onStackScreen != null) onStackScreen(screen);
                        },
                        null,
                        false,
                        m_delay);
                }
                else
                {
                    if (ScreenView.currentScreen != screenWithSameName)
                    {
                        var lastScreen = ScreenView.currentScreen;
                        if (removePreviousStack)
                        {
                            ScreenView.RemoveFromScreenStack(lastScreen);
                        }
                        screenWithSameName.Show();
                    }
                    else
                    {
                        //Debug.Log("Screen already instantiated");
                        return;
                    }

                    if (onStackScreen != null) onStackScreen(screenWithSameName);
                }
            }
        }

        #endregion
    }
}
