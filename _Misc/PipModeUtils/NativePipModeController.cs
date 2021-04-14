using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Kyub
{
    public class NativePipModeController
    {
        #region Consts

#if UNITY_ANDROID && !UNITY_EDITOR
        const string PLUGIN_CLASS_NAME = "kyub.backgroundmode.BackgroundModeUtils";
#endif

        #endregion

        #region Helper Classes

#if UNITY_ANDROID && !UNITY_EDITOR
        public class OnPipModeChangedAndroidProxy : AndroidJavaProxy
        {
            Action<bool> _callback = null;

            public OnPipModeChangedAndroidProxy(Action<bool> callback) : base("kyub.backgroundmode.BackgroundModeUtils$OnPipModeChangedListener")
            {
                _callback = callback;
            }

            public void Execute(bool isPipModeOn)
            {
                if (_callback != null)
                    _callback.Invoke(isPipModeOn);
            }
        }
#endif

        #endregion

        #region Properties

        static bool? s_isPipModeActive = null;
        static bool? s_isSupported = null;
        static bool s_autoPipMode = false;

        public static bool AutoPipMode
        {
            get
            {
                return s_autoPipMode;
            }
            set
            {
                if (s_autoPipMode == value)
                    return;

                s_autoPipMode = value;
                SetAutoPipModeOnPause_Internal(s_autoPipMode);
            }
        }

        public static bool IsPipModeActive
        {
            get
            {
                if (s_isPipModeActive == null)
                    s_isPipModeActive = IsPipModeActive_Internal();
                return s_isPipModeActive != null ? s_isPipModeActive.Value : false;
            }
        }

        #endregion

        #region Callback

        public static Action<bool> OnPipModeStateChanged;

        #endregion

        #region Public Methods

        public static bool IsSupported()
        {
            if (s_isSupported == null)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var pluginClass = new AndroidJavaClass(PLUGIN_CLASS_NAME))
                {
                    s_isSupported = pluginClass.CallStatic<bool>("IsPipModeSupported");
                }
#else
                s_isSupported = false;
#endif
            }
            return s_isSupported != null && s_isSupported.Value;
        }

        protected static void SetPipModeChangedUnityCallback(Action<bool> callback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var pluginClass = new AndroidJavaClass(PLUGIN_CLASS_NAME))
            {
                pluginClass.CallStatic("SetPipModeChangedUnityCallback", new OnPipModeChangedAndroidProxy(callback));
            }
#endif
        }

        public static bool ActivatePipMode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var pluginClass = new AndroidJavaClass(PLUGIN_CLASS_NAME))
            {
                return pluginClass.CallStatic<bool>("EnterInPipMode");
            }
#else
            return false;
#endif
        }

        #endregion

        #region Internal Methods

        protected static bool IsPipModeActive_Internal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var pluginClass = new AndroidJavaClass(PLUGIN_CLASS_NAME))
            {
                return pluginClass.CallStatic<bool>("IsInPipMode");
            }
#else
            return false;
#endif
        }

        protected static void SetAutoPipModeOnPause_Internal(bool supportPipModeOnPause)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var pluginClass = new AndroidJavaClass(PLUGIN_CLASS_NAME))
            {
                pluginClass.CallStatic("SetAutoPipModeOnPause", supportPipModeOnPause);
            }
#endif
        }

        #endregion

        #region Receivers

        protected static void HandleOnPipModeChanged_Internal(bool isPipModeActive)
        {
            s_isPipModeActive = isPipModeActive;
            if (OnPipModeStateChanged != null)
                OnPipModeStateChanged.Invoke(isPipModeActive);
        }

        #endregion

        #region Auto-Initialize Methods

        [RuntimeInitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            SetPipModeChangedUnityCallback(HandleOnPipModeChanged_Internal);
            SetAutoPipModeOnPause_Internal(s_autoPipMode);
        }

        #endregion
    }
}
