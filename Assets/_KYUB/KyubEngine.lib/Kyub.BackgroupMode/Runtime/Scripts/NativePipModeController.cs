using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub;
using System;

namespace Kyub.BackgroudMode
{
    public class NativePipModeController
    {
        #region Consts

#if UNITY_ANDROID && !UNITY_EDITOR
        const string PLUGIN_CLASS_NAME = "kyub.backgroundmode.BackgroundModeUtils";
#endif

        #endregion

        #region Properties

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

        #endregion

        #region Callback

        public static event Action OnWillEnterInPipMode;

        #endregion

        #region Unity Functions

        protected static void HandleOnApplicationPause(bool isPause)
        {
            if (isPause && s_autoPipMode && IsSupported())
            {
                if (OnWillEnterInPipMode != null)
                    OnWillEnterInPipMode.Invoke();
            }
        }

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
                s_isSupported =  false;
#endif
            }
            return s_isSupported != null && s_isSupported.Value;
        }

        public static bool ActivatePipMode()
        {
            if (IsSupported() && !IsPipModeActive())
            {
                if(OnWillEnterInPipMode != null)
                    OnWillEnterInPipMode.Invoke();
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var pluginClass = new AndroidJavaClass(PLUGIN_CLASS_NAME))
            {
                return pluginClass.CallStatic<bool>("EnterInPipMode");
            }
#else
            return false;
#endif
        }

        public static bool IsPipModeActive()
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

        #endregion

        #region Internal Methods

        protected static void SetAutoPipModeOnPause_Internal(bool supportPipModeOnPause)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var pluginClass = new AndroidJavaClass(PLUGIN_CLASS_NAME))
            {
                pluginClass.CallStatic("SetAutoPipModeOnPause", supportPipModeOnPause);
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            SetAutoPipModeOnPause_Internal(s_autoPipMode);
            ApplicationContext.OnApplicationPause -= HandleOnApplicationPause;
            ApplicationContext.OnApplicationPause += HandleOnApplicationPause;
        }

        #endregion
    }
}
