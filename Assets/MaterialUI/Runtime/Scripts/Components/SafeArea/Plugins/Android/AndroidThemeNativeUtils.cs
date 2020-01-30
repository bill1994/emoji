#if UNITY_ANDROID

using UnityEngine;

namespace MaterialUI
{
    public class AndroidThemeNativeUtils
    {
        #region Consts

        public const int SDK_ANDROID_LOLLIPOP = 21;
        public const int SDK_ANDROID_M = 23;

        public const int SYSTEM_UI_FLAG_LIGHT_STATUS_BAR = 8192;
        public const int SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR = 16;

        public const int SYSTEM_UI_FLAG_VISIBLE = 0;
        public const int SYSTEM_UI_FLAG_LOW_PROFILE = 1;
        public const int SYSTEM_UI_FLAG_HIDE_NAVIGATION = 2;
        public const int SYSTEM_UI_FLAG_FULLSCREEN = 4;

        public const int SYSTEM_UI_FLAG_LAYOUT_STABLE = 256;
        public const int SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION = 512;
        public const int SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN = 1024;
        public const int SYSTEM_UI_FLAG_IMMERSIVE = 2048;
        public const int SYSTEM_UI_FLAG_IMMERSIVE_STICKY = 4096;

        public const int FLAG_LAYOUT_NO_LIMITS = 512;
        public const int FLAG_FULLSCREEN = 1024;
        public const int FLAG_FORCE_NOT_FULLSCREEN = 2048;
        public const int FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS = -2147483648;
        public const int FLAG_TRANSLUCENT_STATUS = 67108864;
        public const int FLAG_TRANSLUCENT_NAVIGATION = 134217728;

        public const int ID_ANDROID_CONTENT = 16908290;

        #endregion

        #region Main Objects

        public static AndroidJavaObject GetActivity()
        {
            if (Application.isEditor)
                return null;

            AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            return activity;
        }

        public static AndroidJavaObject GetApplicationContext(AndroidJavaObject activity)
        {
            if (Application.isEditor)
                return null;

            return activity.Call<AndroidJavaObject>("getApplicationContext");
        }

        public static AndroidJavaObject GetWindow(AndroidJavaObject activity)
        {
            if (Application.isEditor)
                return null;

            return activity.Call<AndroidJavaObject>("getWindow");
        }

        public static AndroidJavaObject GetPackageManager(AndroidJavaObject activity)
        {
            if (Application.isEditor)
                return null;

            return activity.Call<AndroidJavaObject>("getPackageManager");
        }

        public static AndroidJavaObject GetDecorView(AndroidJavaObject window)
        {
            if (Application.isEditor)
                return null;

            return window.Call<AndroidJavaObject>("getDecorView");
        }

        #endregion

        #region Display Metrics

        public static AndroidJavaObject GetResources(AndroidJavaObject applicationContext)
        {
            if (Application.isEditor)
                return null;

            return applicationContext.Call<AndroidJavaObject>("getResources");
        }

        public static AndroidJavaObject GetDisplayMetrics(AndroidJavaObject resources)
        {
            if (Application.isEditor)
                return null;

            return resources.Call<AndroidJavaObject>("getDisplayMetrics");
        }

        public static float GetScreenDensity(AndroidJavaObject displayMetrics)
        {
            if (Application.isEditor)
                return 1;

            return displayMetrics.Get<float>("density");
        }

        #endregion

        #region Task Description Functions

        public static string GetPackageName(AndroidJavaObject activity)
        {
            if (Application.isEditor || activity == null)
                return null;

            string packageName = activity.Call<string>("getPackageName");
            return packageName;
        }

        public static AndroidJavaObject GetApplicationIcon(AndroidJavaObject packageManager, string packageName)
        {
            if (Application.isEditor || packageManager == null)
                return null;

            return packageManager.Call<AndroidJavaObject>("getApplicationIcon", packageName).Call<AndroidJavaObject>("getBitmap");
        }

        public static void SetTaskDescriptionIcon(AndroidJavaObject activity, Color iconColor)
        {
            if (Application.isEditor || activity == null)
                return;

            var label = string.Empty;
            var packageName = GetPackageName(activity);
            var packageManager = GetPackageManager(activity);
            var icon = GetApplicationIcon(packageManager, packageName);
            AndroidJavaObject taskDescription = new AndroidJavaObject("android.app.ActivityManager$TaskDescription", label, icon, ToAndroidARGB(iconColor));
            activity.Call("setTaskDescription", taskDescription);
        }

        #endregion

        #region System UI Visibility Functions

        public static void ClearSystemUiVisibilityFlags(AndroidJavaObject decorView, int flags)
        {
            if (Application.isEditor || decorView == null)
                return;

            var currentFlags = GetSystemUiVisibilityFlags(decorView);
            currentFlags &= ~flags;
            SetSystemUiVisibilityFlags(decorView, currentFlags);
        }

        public static void AddSystemUiVisibilityFlags(AndroidJavaObject decorView, int flags)
        {
            if (Application.isEditor || decorView == null)
                return;

            var currentFlags = GetSystemUiVisibilityFlags(decorView);
            currentFlags |= flags;
            SetSystemUiVisibilityFlags(decorView, currentFlags);
        }

        public static void SetSystemUiVisibilityFlags(AndroidJavaObject decorView, int flags)
        {
            if (Application.isEditor || decorView == null)
                return;

            decorView.Call("setSystemUiVisibility", flags);
        }

        public static int GetSystemUiVisibilityFlags(AndroidJavaObject decorView)
        {
            if (Application.isEditor || decorView == null)
                return -1;

            return decorView.Call<int>("getSystemUiVisibility");
        }

        #endregion

        #region Window Functions

        public static int GetWindowFlagConstValue(string flagName)
        {
            if (Application.isEditor)
                return -1;

            AndroidJavaClass layoutParamsClass = new AndroidJavaClass("android.view.WindowManager$LayoutParams");
            return layoutParamsClass.GetStatic<int>(flagName);
        }

        protected static int GetWindowFlags(AndroidJavaObject window)
        {
            if (Application.isEditor || window == null)
                return -1;

            return window.Call<AndroidJavaObject>("getAttributes").Get<int>("flags");
        }

        protected static void AddWindowFlags(AndroidJavaObject window, int flags)
        {
            if (Application.isEditor || window == null)
                return;

            window.Call("addFlags", flags);
        }

        protected static void ClearWindowFlags(AndroidJavaObject window, int flags)
        {
            if (Application.isEditor || window == null)
                return;

            window.Call("clearFlags", flags);
        }

        #endregion

        #region Activity Functions

        public static void RunOnUIThread(AndroidJavaObject activity, System.Action action)
        {
            if (Application.isEditor || activity == null)
                return;

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                if (action != null)
                    action.Invoke();
            }));
        }

        #endregion

        #region Native Bars Functions

        static bool IsStatusBarVisible_Internal(AndroidJavaObject decorView)
        {
            if (Application.isEditor || decorView == null)
                return false;


            var rectangle = new AndroidJavaObject("android.graphics.Rect");
            int statusBarHeight = rectangle.Get<int>("top");
            return statusBarHeight != 0;
        }

        public static bool IsStatusBarActive(AndroidJavaObject window)
        {
            if (Application.isEditor || window == null)
                return false;

            var decorView = GetDecorView(window);
            //if (IsStatusBarVisible_Internal(decorView))
            //{
                var decorViewFlags = GetSystemUiVisibilityFlags(decorView);
                var isFullScreen = (decorViewFlags & SYSTEM_UI_FLAG_FULLSCREEN) == SYSTEM_UI_FLAG_FULLSCREEN;
                if (!isFullScreen)
                {
                    var windowFlags = GetWindowFlags(window);
                    return (decorViewFlags & SYSTEM_UI_FLAG_VISIBLE) == SYSTEM_UI_FLAG_VISIBLE &&
                        (windowFlags & FLAG_FORCE_NOT_FULLSCREEN) == FLAG_FORCE_NOT_FULLSCREEN;
                }
            //}

            return false;
        }

        public static bool IsNavigationBarActive(AndroidJavaObject window)
        {
            if (Application.isEditor || window == null)
                return false;

            var decorView = GetDecorView(window);

            var decorViewFlags = GetSystemUiVisibilityFlags(decorView);
            var isFullScreen = (decorViewFlags & SYSTEM_UI_FLAG_FULLSCREEN) == SYSTEM_UI_FLAG_FULLSCREEN;
            if (!isFullScreen)
            {
                var windowFlags = GetWindowFlags(window);
                var isNavigationHidden = (decorViewFlags & SYSTEM_UI_FLAG_HIDE_NAVIGATION) == SYSTEM_UI_FLAG_HIDE_NAVIGATION;
                return !isNavigationHidden && 
                    ((decorViewFlags & SYSTEM_UI_FLAG_VISIBLE) == SYSTEM_UI_FLAG_VISIBLE ||
                    (windowFlags & FLAG_FORCE_NOT_FULLSCREEN) == FLAG_FORCE_NOT_FULLSCREEN);
            }

            return false;
        }

        public static bool IsViewBehindBars(AndroidJavaObject window)
        {
            if (Application.isEditor || window == null)
                return true;

            var windowFlags = GetWindowFlags(window);

            var decorView = GetDecorView(window);
            var decorViewFlags = GetSystemUiVisibilityFlags(decorView);
            return (decorViewFlags & SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN) == SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN || 
                (windowFlags & FLAG_LAYOUT_NO_LIMITS) == FLAG_LAYOUT_NO_LIMITS;
        }

        public static float GetNavigationBarHeight(AndroidJavaObject activity)
        {
            if (Application.isEditor || activity == null)
                return 0;

            var context = GetApplicationContext(activity);
            var resources = GetResources(context);
            var displayMetrics = GetDisplayMetrics(resources);
            
            float height = 0;
            int resourceId = resources.Call<int>("getIdentifier", "navigation_bar_height", "dimen", "android");
            if (resourceId > 0)
            {
                height = resources.Call<int>("getDimensionPixelSize", resourceId);
            }
            else
            {
                var density = GetScreenDensity(displayMetrics);
                height = Mathf.Ceil(48 * density);
            }

            return height;
        }

        public static float GetStatusBarHeight(AndroidJavaObject activity)
        {
            if (Application.isEditor || activity == null)
                return 0;

            var context = GetApplicationContext(activity);
            var resources = GetResources(context);
            var displayMetrics = GetDisplayMetrics(resources);
            
            float height = 0;
            int resourceId = resources.Call<int>("getIdentifier", "status_bar_height", "dimen", "android");
            if (resourceId > 0)
            {
                height = resources.Call<int>("getDimensionPixelSize", resourceId);
            }
            else
            {
                var density = GetScreenDensity(displayMetrics);
                int sdkInt = AndroidThemeNativeUtils.GetSdkVersion();
                height = Mathf.Ceil(sdkInt >= SDK_ANDROID_M? 24 : 25 * density);
            }

            return height;
        }

        public static Color GetNavigationBarColor(Color color)
        {
            if (Application.isEditor)
                return Color.clear;

            var sdkVersion = GetSdkVersion();
            if (sdkVersion < SDK_ANDROID_M)
                return Color.clear;

            var activity = GetActivity();
            var intColor = GetWindow(activity).Call<int>("getNavigationBarColor");

            return FromAndroidARGB(intColor);
        }

        public static Color GetStatusBarColor(Color color)
        {
            if (Application.isEditor)
                return Color.clear;

            var sdkVersion = GetSdkVersion();
            if (sdkVersion < SDK_ANDROID_LOLLIPOP)
                return Color.clear;

            var activity = GetActivity();
            var intColor = GetWindow(activity).Call<int>("getStatusBarColor");

            return FromAndroidARGB(intColor);
        }

        public static void SetStatusBarColor(Color color, bool isWhiteIcons)
        {
            if (Application.isEditor)
                return;

            var sdkVersion = GetSdkVersion();
            if (sdkVersion < SDK_ANDROID_LOLLIPOP)
                return;

            var activity = GetActivity();
            RunOnUIThread(activity, () =>
            {
                var window = GetWindow(activity);
                SetStatusBarTranslucentActive(window, false);
                AddWindowFlags(window, FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);

                window.Call("setStatusBarColor", ToAndroidARGB(color));

                //Android M
                if (sdkVersion >= SDK_ANDROID_M)
                {
                    var decorView = GetDecorView(window);
                    //ClearSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LAYOUT_STABLE);
                    //ClearSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN);
                    if (isWhiteIcons)
                        ClearSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LIGHT_STATUS_BAR);
                    else
                        AddSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LIGHT_STATUS_BAR);
                }
                else
                {
                    SetTaskDescriptionIcon(activity, isWhiteIcons ? Color.white : Color.black);
                }
            });
        }

        public static void SetNavigationBarColor(Color color, bool isWhiteIcons)
        {
            if (Application.isEditor)
                return;

            var sdkVersion = GetSdkVersion();
            if (sdkVersion < SDK_ANDROID_M) //Android Lollipop
                return;

            var activity = GetActivity();
            RunOnUIThread(activity, () =>
            {
                var window = GetWindow(activity);
                SetNavigationBarTranslucentActive(window, false);
                AddWindowFlags(window, FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);

                window.Call("setNavigationBarColor", ToAndroidARGB(color));

                //Android M
                var decorView = GetDecorView(window);
                //ClearSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LAYOUT_STABLE);
                //ClearSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN);
                if (isWhiteIcons)
                    ClearSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR);
                else
                    AddSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR);

            });
        }

        public static void SetStatusBarTranslucentActive(AndroidJavaObject window, bool isOn)
        {
            if (Application.isEditor || window == null)
                return;

            if (isOn)
            {
                AddWindowFlags(window, FLAG_TRANSLUCENT_STATUS);
                AddWindowFlags(window, FLAG_FULLSCREEN);
                ClearWindowFlags(window, FLAG_FORCE_NOT_FULLSCREEN);
            }
            else
            {
                ClearWindowFlags(window, FLAG_TRANSLUCENT_STATUS);
                ClearWindowFlags(window, FLAG_FULLSCREEN);
                AddWindowFlags(window, FLAG_FORCE_NOT_FULLSCREEN);
            }
        }

        protected static void SetNavigationBarTranslucentActive(AndroidJavaObject window, bool isOn)
        {
            if (Application.isEditor || window == null)
                return;

            var decorView = GetDecorView(window);
            if (isOn)
            {
                AddWindowFlags(window, FLAG_TRANSLUCENT_NAVIGATION);
                AddSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION);
            }
            else
            {
                ClearWindowFlags(window, FLAG_TRANSLUCENT_NAVIGATION);
                ClearSystemUiVisibilityFlags(decorView, SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION);
            }
        }

        #endregion

        #region Other Functions

        public static int GetSdkVersion()
        {
            if (Application.isEditor)
                return -1;

            int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            return sdkInt;
        }

        public static int ToAndroidARGB(Color color)
        {
            Color32 c = (Color32)color;
            byte[] b = new byte[] { c.b, c.g, c.r, c.a };

            return System.BitConverter.ToInt32(b, 0);
        }

        public static Color FromAndroidARGB(int colorARGBInt)
        {
            var bytes = System.BitConverter.GetBytes(colorARGBInt);

            byte colorB = bytes.Length > 0 ? bytes[0] : (byte)0;
            byte colorG = bytes.Length > 1 ? bytes[1] : (byte)0;
            byte colorR = bytes.Length > 2 ? bytes[2] : (byte)0;
            byte colorA = bytes.Length > 3 ? bytes[3] : (byte)1;
            Color32 color = new Color32(colorR, colorG, colorB, colorA);

            return color;
        }

        #endregion
    }
}

#endif