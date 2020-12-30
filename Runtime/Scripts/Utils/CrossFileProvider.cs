using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Async;
using System.Runtime.InteropServices;

namespace Kyub.PickerServices
{
    public static class CrossFileProvider
    {
        #region Public Functions

        public static void ShareFile(string filePath, string text = "", string subject = "")
        {
            var nativeShare = new NativeShare().AddFile(filePath).SetSubject(subject).SetText(text);
            nativeShare.Share();
        }

        public static void ShareFiles(IEnumerable<string> filePaths, string text = "", string subject = "")
        {
            var nativeShare = new NativeShare().SetSubject(subject).SetText(text);
            if (filePaths != null)
            {
                foreach (var path in filePaths)
                {
                    nativeShare.AddFile(path);
                }
            }
            nativeShare.Share();
        }

        public static void ShareMessage(string text, string subject = "")
        {
            var nativeShare = new NativeShare().SetSubject(subject).SetText(text);
            nativeShare.Share();
        }

        public static void OpenFileUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            filePath = filePath.Trim();
            bool requestDefaultOpenUrl = HasValidProtocol(filePath);

            if (!requestDefaultOpenUrl)
            {
#if UNITY_IOS && !UNITY_EDITOR
                if(!iOS_OpenFileUrl(filePath))
                    requestDefaultOpenUrl = true;
#elif UNITY_ANDROID && !UNITY_EDITOR
                Android_OpenFileUrl(filePath);
#else
                requestDefaultOpenUrl = true;
#endif
            }

            if (requestDefaultOpenUrl)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                JS_OpenFileUrl(filePath);
#else
                Application.OpenURL(filePath);
#endif
            }
        }

        public static bool FileExists(string filePath)
        {
#if UNITY_IOS
            System.IO.FileInfo v_info = new System.IO.FileInfo(filePath);
            if (v_info == null ||v_info.Exists == false)
                return false;
            return true;
#elif UNITY_WEBGL && !UNITY_EDITOR
            return HasValidProtocol(filePath);
#else
            return System.IO.File.Exists(filePath);
#endif
        }

        #endregion

        #region Helper Functions

        private static bool HasValidProtocol(string filePath, HashSet<string> supportedProtocols = null, HashSet<string> invalidProtocols = null)
        {
            filePath = filePath != null ? filePath.Trim() : null;
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (supportedProtocols == null)
            {
                supportedProtocols = new HashSet<string>() {
                    "http",
                    "ftp",
                    "www",
                    "tel",
                    "mailto"
                };
            }
            if (invalidProtocols == null)
                invalidProtocols = new HashSet<string>() { "file" };

            foreach (var invalidProtocol in invalidProtocols)
            {
                if (filePath.StartsWith(invalidProtocol))
                    return false;
            }

            foreach (var protocol in supportedProtocols)
            {
                if (filePath.StartsWith(protocol))
                    return true;
            }
            return false;
        }

        #endregion

        #region Android Extern Functions

#if UNITY_ANDROID && !UNITY_EDITOR
        public static void Android_OpenFileUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;


            if (string.IsNullOrEmpty(filePath))
                return;

            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                int ANDROID_SDK_NOUGAT = 24;

                AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");

                //Permission to read URI
                intent.Call<AndroidJavaObject>("addFlags", intent.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));
                intent.Call<AndroidJavaObject>("setAction", intent.GetStatic<string>("ACTION_VIEW"));

                //Get API Android Version
                var apiLevel = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");

                var mimeType = "";
                AndroidJavaObject uri;

                if (apiLevel > ANDROID_SDK_NOUGAT)
                {
                    //New version, need a fileprovider
                    var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                    var fileProvider = new AndroidJavaClass("com.yasirkula.unity.NativeShareContentProvider");
                    var authority = Android_GetAuthority();
                    var file = new AndroidJavaObject("java.io.File", filePath);
                    uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", context, authority, file);
                    mimeType = fileProvider.Call<string>("getType", uri);
                }
                else
                {
                    //Old version using uriClass
                    var uriClass = new AndroidJavaClass("android.net.Uri");
                    var file = new AndroidJavaObject("java.io.File", filePath);
                    uri = uriClass.CallStatic<AndroidJavaObject>("fromFile", file);
                    mimeType = MimeTypeMapping.GetMimeMapping(filePath);
                }

                if (string.IsNullOrEmpty(mimeType))
                    mimeType = "application/octet-stream";

                //Set MimeType
                intent.Call<AndroidJavaObject>("setType", mimeType);
                //Set Uri
                intent.Call<AndroidJavaObject>("setData", uri);

                //start activity
                activity.Call("startActivity", intent);
            }
        }

        private static string Android_GetAuthority(string className = "com.yasirkula.unity.NativeShareContentProvider")
        {
            int PackageManager_GET_PROVIDERS = 8;

            AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            var context = activity.Call<AndroidJavaObject>("getApplicationContext");
            var packageManager = context.Call<AndroidJavaObject>("getPackageManager");

            var packageName = context.Call<string>("getPackageName");
            var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, PackageManager_GET_PROVIDERS);

            var providersUncasted = packageInfo.Get<AndroidJavaObject>("providers");
            AndroidJavaObject[] providers = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(providersUncasted.GetRawObject());


            if (providers != null)
            {
                foreach (var provider in providers)
                {
                    var providerName = provider.Get<string>("name");
                    var providerPackageName = provider.Get<string>("packageName");
                    var providerAuthority = provider.Get<string>("authority");

                    if (!string.IsNullOrEmpty(providerName) && !string.IsNullOrEmpty(providerPackageName) && !string.IsNullOrEmpty(providerAuthority) &&
                        providerName.Equals(className) && providerPackageName.Equals(packageName))
                    {
                        return providerAuthority;
                    }
                }
            }
            return Application.identifier;
        }
#endif

        #endregion

        #region iOS Extern Functions

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern bool iOS_OpenFileUrl(string path);
#endif

        #endregion

        #region WebGL Extern Functions

#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern bool JS_OpenFileUrl(string path);
#endif

        #endregion
    }
}
