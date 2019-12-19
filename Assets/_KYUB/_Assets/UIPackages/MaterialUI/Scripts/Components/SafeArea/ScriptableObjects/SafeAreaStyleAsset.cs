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

        [Header("Android SDK >= 23 Configs")]
        [SerializeField]
        bool m_useLightIconsOnStatusBar = true;
        [SerializeField]
        bool m_useLightIconsOnNavigationBar = false;

        [Header("Android SDK < 23 Configs")]
        [SerializeField, Tooltip("Pré API 23 Only")]
        Color m_iconsColor = Color.white;

        [Header("All Android SDKs Configs")]
        [SerializeField, Tooltip("Background color of top Bar")]
        Color m_statusBarColor = Color.black;
        [SerializeField, Tooltip("Background color of bottom Bar")]
        Color m_navigationBarColor = Color.white;

        #endregion

        #region Properties

        public bool Enabled { get { return m_enabled; } set { m_enabled = value; } }
        public FullScreenModeEnum FullScreenMode { get { return m_fullScreenMode; } set { m_fullScreenMode = value; } }
        public Color UnsafeContentColor { get { return m_unsafeContentColor; } set { m_unsafeContentColor = value; } }
        public bool UseLightIconsOnStatusBar { get { return m_useLightIconsOnStatusBar; } set { m_useLightIconsOnStatusBar = value; } }
        public bool UseLightIconsOnNavigationBar { get { return m_useLightIconsOnNavigationBar; } set { m_useLightIconsOnNavigationBar = value; } }
        public Color IconsColor { get { return m_iconsColor; } set { m_iconsColor = value; } }
        public Color StatusBarColor { get { return m_statusBarColor; } set { m_statusBarColor = value; } }
        public Color NavigationBarColor { get { return m_navigationBarColor; } set { m_navigationBarColor = value; } }

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
                SetupAndroidTheme(m_iconsColor, m_statusBarColor, m_navigationBarColor, m_useLightIconsOnStatusBar, m_useLightIconsOnNavigationBar);
            }
        }

        #endregion

        #region Theme Utils (Static)

        public static void SetupAndroidTheme(Color p_iconsColor, Color p_statusBarColor, Color p_navigationBarColor, bool p_useLightIconsOnStatusBar, bool p_useLightIconsOnNatigationBar, string p_label = null)
        {
            SetupAndroidTheme_Internal(ToAndroidARGB(p_iconsColor), ToAndroidARGB(p_statusBarColor), ToAndroidARGB(p_navigationBarColor), p_useLightIconsOnStatusBar, p_useLightIconsOnNatigationBar, p_label);
        }

        static void SetupAndroidTheme_Internal(int p_iconsColorInt, int p_statusBarColorInt, int p_navigationBarColorInt, bool p_useLightIconsOnStatusBar, bool p_useLightIconsOnNatigationBar, string p_label)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            p_label = p_label ?? Application.productName;
            AndroidJavaObject activity = new AndroidJavaClass ("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject> ("currentActivity");
            activity.Call ("runOnUiThread", new AndroidJavaRunnable (() => {
                AndroidJavaClass layoutParamsClass = new AndroidJavaClass ("android.view.WindowManager$LayoutParams");
                int flagFullscreen = layoutParamsClass.GetStatic<int> ("FLAG_FULLSCREEN");
                int flagNotFullscreen = layoutParamsClass.GetStatic<int> ("FLAG_FORCE_NOT_FULLSCREEN");
                int flagDrawsSystemBarBackgrounds = layoutParamsClass.GetStatic<int> ("FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS");
                AndroidJavaObject windowObject = activity.Call<AndroidJavaObject> ("getWindow");
                windowObject.Call ("clearFlags", flagFullscreen);
                windowObject.Call ("addFlags", flagNotFullscreen);
                windowObject.Call ("addFlags", flagDrawsSystemBarBackgrounds);
                int sdkInt = new AndroidJavaClass ("android.os.Build$VERSION").GetStatic<int> ("SDK_INT");
                int lollipop = 21;
                if (sdkInt > lollipop) 
                {
                    windowObject.Call ("setStatusBarColor", p_statusBarColorInt);
                    windowObject.Call ("setNavigationBarColor", p_navigationBarColorInt);
                    string myName = activity.Call<string> ("getPackageName");
                    AndroidJavaObject packageManager = activity.Call<AndroidJavaObject> ("getPackageManager");
                    AndroidJavaObject drawable = packageManager.Call<AndroidJavaObject> ("getApplicationIcon", myName);
                    AndroidJavaObject taskDescription = new AndroidJavaObject ("android.app.ActivityManager$TaskDescription", p_label, drawable.Call<AndroidJavaObject> ("getBitmap"), p_iconsColorInt);
                    activity.Call ("setTaskDescription", taskDescription);

                    
                    AndroidJavaObject decorViewObject = windowObject.Call<AndroidJavaObject>("getDecorView");
                    if (decorViewObject != null)
                    {
                        int flag_SYSTEM_UI_FLAG_LIGHT_STATUS_‌​BAR = 8192;
                        int flag_SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR = 16;

                        var v_flagsToActivate = (p_useLightIconsOnStatusBar ? 0 : flag_SYSTEM_UI_FLAG_LIGHT_STATUS_‌​BAR) |
                                                (p_useLightIconsOnNatigationBar ? 0 : flag_SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR);

                        //var v_flagsToDeactivate = (p_useLightIconsOnStatusBar ? flag_SYSTEM_UI_FLAG_LIGHT_STATUS_‌​BAR : 0) |
                        //                        (p_useLightIconsOnNatigationBar ? flag_SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR : 0);

                        //int v_currentUIDecorFlags = decorViewObject.Call<int>("getSystemUiVisibility");
                        //v_currentUIDecorFlags = ~v_flagsToDeactivate;
                        //v_currentUIDecorFlags |= v_flagsToActivate;
                        decorViewObject.Call("setSystemUiVisibility", v_flagsToActivate);
                    }
                }
            }));
#endif
        }

        static int ToAndroidARGB(Color p_color)
        {
            Color32 c = (Color32)p_color;
            byte[] b = new byte[] { c.b, c.g, c.r, c.a };
            return System.BitConverter.ToInt32(b, 0);
        }

        #endregion
    }
}
