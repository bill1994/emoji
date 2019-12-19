using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

namespace Kyub
{
    public class PortraitMobileFixer : MonoBehaviour
    {
        protected virtual void Awake()
        {
            if (Application.isMobilePlatform)
                Screen.fullScreen = false;
            //WSetupAndroidTheme(ToARGB(Color.black), ToARGB(Color.black));
        }

        /*protected virtual void Start()
        {
            //if (Application.isMobilePlatform)
            //{
            //DisableFullScreen();
            //UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneWasLoaded;
            //Screen.fullScreen = false;
            //}
        }

        protected virtual void OnDestroy()
        {
            //UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneWasLoaded;
        }

        private void OnSceneWasLoaded(Scene arg0, LoadSceneMode arg1)
        {
            //DisableFullScreen();
        }

        private static void DisableFullScreen()
        {
            Screen.fullScreen = false;
    #if UNITY_ANDROID && !UNITY_EDITOR
            RunOnAndroidUiThread(DisableFullScreenInternal);
    #endif
        }

        public void CallSetupAndroidTheme()
        {
            DisableFullScreen();
            //SetupAndroidTheme(ToARGB(Color.black), ToARGB(Color.black));
        }

        public static void SetupAndroidTheme(int primaryARGB, int darkARGB, string label = null)
        {
    #if UNITY_ANDROID && !UNITY_EDITOR
                label = label??Application.productName;
                Screen.fullScreen = false;
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
                    using (var decor = windowObject.Call<AndroidJavaObject>("getDecorView"))
                    {
                        decor.Call("setSystemUiVisibility", flagNotFullscreen);
                    }
                    int sdkInt = new AndroidJavaClass ("android.os.Build$VERSION").GetStatic<int> ("SDK_INT");
                    int lollipop = 21;
                    if (sdkInt > lollipop) {
                        windowObject.Call ("setStatusBarColor", darkARGB);
                        string myName = activity.Call<string> ("getPackageName");
                        AndroidJavaObject packageManager = activity.Call<AndroidJavaObject> ("getPackageManager");
                        AndroidJavaObject drawable = packageManager.Call<AndroidJavaObject> ("getApplicationIcon", myName);
                        AndroidJavaObject taskDescription = new AndroidJavaObject ("android.app.ActivityManager$TaskDescription", label, drawable.Call<AndroidJavaObject> ("getBitmap"), primaryARGB);
                        activity.Call ("setTaskDescription", taskDescription);
                    }
                }));
    #endif
        }

        public static int ToARGB(Color color)
        {
            Color32 c = (Color32)color;
            byte[] b = new byte[] { c.b, c.g, c.r, c.a };
            return System.BitConverter.ToInt32(b, 0);
        }

    #if UNITY_ANDROID && !UNITY_EDITOR

        private static void RunOnAndroidUiThread(Action target) {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(target));
                }
            }
        }

        private static void DisableFullScreenInternal() 
        {
            int v_flagFullScreen = 0x00000400;
            int v_flagNotFullScreen = 0x00000800;
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) 
            {
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) 
                {
                    using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                    {
                        using (var decor = window.Call<AndroidJavaObject>("getDecorView"))
                        {
                            AndroidJavaClass layoutParamsClass = new AndroidJavaClass ("android.view.WindowManager$LayoutParams");
                            int flagDisableFullscreen = layoutParamsClass.GetStatic<int> ("SYSTEM_UI_FLAG_VISIBLE");
                            decor.Call("setSystemUiVisibility", flagDisableFullscreen);
                        }
                        //window.Call("clearFlags", v_flagFullScreen);
                        //window.Call("addFlags", v_flagNotFullScreen);
                        //window.Call("setFlags", 2048, 2048);
                    }
                    //activity.Call("requestWindowFeature", 1);
                }
            }
        }

    #endif*/
    }
}