using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialUI
{
    [CreateAssetMenu(fileName = "SafeAreaStyleAsset", menuName = "MaterialUI/Safe Area Style Asset")]
    public class SafeAreaStyleAsset : ScriptableObject
    {
        #region Helper Enums

        public enum FullScreenModeEnum { Default, Disabled, Enabled }

        #endregion

        #region Private Variables

        [SerializeField]
        bool m_enabled = true;
        [SerializeField]
        FullScreenModeEnum m_fullScreenMode = FullScreenModeEnum.Default;

        [Header("Notch Configs")]
        [SerializeField]
        Color m_unsafeContentColor = Color.black;

        [Header("Android Bar Configs")]
        [SerializeField]
        bool m_changeStatusBarColor = false;
        [SerializeField]
        bool m_useLightIconsOnStatusBar = true;
        [SerializeField, Tooltip("Background color of top bar")]
        Color m_statusBarColor = Color.black;
        [Space]
        [SerializeField]
        bool m_changeNavigationBarColor = false;
        [SerializeField]
        bool m_useLightIconsOnNavigationBar = false;
        [SerializeField, Tooltip("Background color of bottom bar")]
        Color m_navigationBarColor = Color.white;

        #endregion

        #region Properties

        public bool Enabled { get { return m_enabled; } set { m_enabled = value; } }
        public FullScreenModeEnum FullScreenMode { get { return m_fullScreenMode; } set { m_fullScreenMode = value; } }
        public Color UnsafeContentColor { get { return m_unsafeContentColor; } set { m_unsafeContentColor = value; } }
        public bool UseLightIconsOnStatusBar { get { return m_useLightIconsOnStatusBar; } set { m_useLightIconsOnStatusBar = value; } }
        public bool UseLightIconsOnNavigationBar { get { return m_useLightIconsOnNavigationBar; } set { m_useLightIconsOnNavigationBar = value; } }
        public Color StatusBarColor { get { return m_statusBarColor; } set { m_statusBarColor = value; } }
        public Color NavigationBarColor { get { return m_navigationBarColor; } set { m_navigationBarColor = value; } }

        public bool ChangeStatusBarColor { get { return m_changeStatusBarColor; } set { m_changeStatusBarColor = value; } }
        public bool ChangeNavigationBarColor { get { return m_changeNavigationBarColor; } set { m_changeNavigationBarColor = value; } }

        #endregion

        #region Public Functions

        public virtual void ApplyStatusBarTheme()
        {
            if (m_enabled)
            {
                if (FullScreenMode != FullScreenModeEnum.Default)
                {
                    var defaultFullScreen = Screen.fullScreen;
                    var isFullScreen = FullScreenMode == FullScreenModeEnum.Default ? defaultFullScreen : (FullScreenMode == FullScreenModeEnum.Enabled ? true : false);
                    if (defaultFullScreen != isFullScreen)
                        Screen.fullScreen = isFullScreen;
                }
#if UNITY_ANDROID
                if (m_changeStatusBarColor)
                    AndroidThemeNativeUtils.SetStatusBarColor(m_statusBarColor, m_useLightIconsOnStatusBar);
                if (m_changeNavigationBarColor)
                    AndroidThemeNativeUtils.SetNavigationBarColor(m_navigationBarColor, m_useLightIconsOnNavigationBar);
#endif
            }
        }

        #endregion
    }
}
