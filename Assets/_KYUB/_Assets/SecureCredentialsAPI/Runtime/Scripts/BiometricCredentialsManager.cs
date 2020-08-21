using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Kyub.Credentials
{
    public class BiometricCredentialsManager : Singleton<BiometricCredentialsManager>
    {
        #region Error Messages

        const string INVALID_CREDENTIALS = "Failed to retrieve credentials from CredentialsKeyStore";
        const string FINGERPRINT_NOT_AVAILABLE = "Fingerprint Hardware not available.";

        #endregion

        #region Static Events

        public static event System.Action OnAuthStarted;
        public static event System.Action<AuthResponse> OnAuthSucess;
        public static event System.Action<AuthResponse> OnAuthFailed;
        public static event System.Action<int> OnRemainingFailChancesChanged;

        #endregion

        #region Private Variables

        [SerializeField]
        string m_fingerprintDialogPath = "CustomUI/DialogFingerprint";
        [Space]
        [SerializeField, Range(1, 5)]
        int m_maxBiometricsFailure = 5;
        [SerializeField]
        string m_dialogDefaultTitle = "Touch ID";
        [SerializeField]
        string m_dialogDefaultSubtitle = "Confirm fingerprint to continue";
        [SerializeField]
        string m_dialogDefaultCancelText = "Cancel";
        [Space]
        [SerializeField, Tooltip("Legacy Mode will force use internal Unity Popup instead native  (SDK < 28)")]
        bool m_useLegacyMode = true;

        string _processingStoredKey = null;
        UI.DialogFingerprint _activeFingerprintDialog = null;

        #endregion

        #region Properties

        public string FingerprintDialogPath
        {
            get
            {
                return m_fingerprintDialogPath;
            }

            set
            {
                if (m_fingerprintDialogPath == value)
                    return;
                m_fingerprintDialogPath = value;
                _activeFingerprintDialog = null;
            }
        }

        public int MaxBiometricsFailure
        {
            get
            {
                return m_maxBiometricsFailure;
            }

            set
            {
                m_maxBiometricsFailure = value;
            }
        }

        public string DialogDefaultTitle
        {
            get
            {
                return m_dialogDefaultTitle;
            }

            set
            {
                m_dialogDefaultTitle = value;
            }
        }

        public string DialogDefaultSubtitle
        {
            get
            {
                return m_dialogDefaultSubtitle;
            }

            set
            {
                m_dialogDefaultSubtitle = value;
            }
        }

        public string DialogDefaultCancelText
        {
            get
            {
                return m_dialogDefaultCancelText;
            }

            set
            {
                m_dialogDefaultCancelText = value;
            }
        }

        public bool UseLegacyMode
        {
            get
            {
                return m_useLegacyMode;
            }

            set
            {
                m_useLegacyMode = value;
            }
        }

        public int RemainingBiometricsFailure
        {
            get
            {
                return _remainingFailChances;
            }
        }

        #endregion

        #region Internal Helper Functions (Instance)

        protected void RetrieveStoredKey_Instance(string p_storedKey, System.Action<AuthResponse> callback, string p_dialogTitle = null, string p_dialogSubtitle = null, string p_cancelText = null)
        {
            if (CredentialsKeyStore.HasKey(p_storedKey))
            {
                //Create and register delegates
                if (callback != null)
                {
                    System.Action v_delegateUnregister = null;
                    System.Action<AuthResponse> v_delegateCallback = (result) =>
                    {
                        v_delegateUnregister();
                        if (callback != null)
                            callback(result);
                    };
                    v_delegateUnregister = () =>
                    {
                        OnAuthFailed -= v_delegateCallback;
                        OnAuthSucess -= v_delegateCallback;
                    };

                    OnAuthFailed += v_delegateCallback;
                    OnAuthSucess += v_delegateCallback;
                }

                if (IsBiometricHardwareAvailable_Native())
                {
                    _processingStoredKey = p_storedKey;
                    StartFingerprint_Native(p_dialogTitle, p_dialogSubtitle, p_cancelText);
                    if (OnAuthStarted != null)
                        OnAuthStarted();
                }
                //Failed to retrieve fingerprint (permission failed, or fingerprint sensor not available)
                else
                {
                    OnAuthenticationBridgeDidFinish(FINGERPRINT_NOT_AVAILABLE);
                }
            }
            else
            {
                OnAuthenticationBridgeDidFinish(INVALID_CREDENTIALS);
            }
        }

        #endregion

        #region Native Functions (Instance)

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _biometricsBridge = null;
        private AndroidJavaObject _activityContext = null;
        private string UNITY_PLAYER_CLASS = "com.unity3d.player.UnityPlayer";
        private string BIOMETRICS_BRIDGE_CLASS = "com.kyub.biometricsauthlibrary.Bridge";

#elif UNITY_IOS && !UNITY_EDITOR
	    [DllImport ("__Internal")]
	    private static extern void _StartBiometricsAuth (string p_objectName, string p_authenticationPopupDescription);

#endif

        int _remainingFailChances = 0;
        protected internal void StartFingerprint_Native(string p_dialogTitle, string p_dialogSubtitle, string p_cancelText)
        {
            if (p_dialogTitle == null)
                p_dialogTitle = m_dialogDefaultTitle;

            if (p_dialogSubtitle == null)
                p_dialogSubtitle = m_dialogDefaultSubtitle;

            if (p_cancelText == null)
                p_cancelText = m_dialogDefaultCancelText;

            p_dialogTitle = Kyub.Localization.LocaleManager.GetLocalizedText(p_dialogTitle);
            p_dialogSubtitle = Kyub.Localization.LocaleManager.GetLocalizedText(p_dialogSubtitle);
            p_cancelText = Kyub.Localization.LocaleManager.GetLocalizedText(p_cancelText);


            var v_isNativePromptAvailable = IsBiometricPromptAvailable_Native();

            //Native Prompts has infinity chances to fail
            _remainingFailChances = m_maxBiometricsFailure;

            if (!v_isNativePromptAvailable && !Application.isEditor && Application.isMobilePlatform)
            {
                //Show custom dialogs
                if (_activeFingerprintDialog == null)
                {
                    //Prepare Initialize Method
                    System.Action<Kyub.Credentials.UI.DialogFingerprint> onInitializeDelegate = (fingerprintDialog) =>
                    {
                        if (fingerprintDialog != null)
                        {
                            _activeFingerprintDialog = fingerprintDialog;
                            fingerprintDialog.Initialize(p_dialogTitle, p_dialogSubtitle, null, p_cancelText);
                        }
                    };

                    //Instantiate Dialog
                    MaterialUI.DialogManager.ShowModalCustomDialogAsync<Kyub.Credentials.UI.DialogFingerprint>(m_fingerprintDialogPath, onInitializeDelegate);
                }
                else
                {
                    _activeFingerprintDialog.Initialize(p_dialogTitle, p_dialogSubtitle);
                    _activeFingerprintDialog.ShowModal();
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass activityClass = new AndroidJavaClass(UNITY_PLAYER_CLASS))
            {
                _activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                using (AndroidJavaClass pluginClass = new AndroidJavaClass(BIOMETRICS_BRIDGE_CLASS))
                {
                    if (pluginClass != null)
                    {
                        _biometricsBridge = pluginClass.CallStatic<AndroidJavaObject>("instance");
                        _biometricsBridge.Call("setContext", _activityContext);
                        _biometricsBridge.Call("setLegacyMode", m_useLegacyMode);
                        _biometricsBridge.Call("startBiometricsAuth", this.gameObject.name, p_dialogTitle, "", p_dialogSubtitle, p_cancelText);
                    }
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            _remainingFailChances = 1;
            _StartBiometricsAuth(this.gameObject.name, p_dialogSubtitle);
#endif
        }

        protected internal bool IsBiometricHardwareAvailable_Native()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int sdkVersion = new AndroidJavaClass ("android.os.Build$VERSION").GetStatic<int> ("SDK_INT");
            int marshmallowVersion = 23;
            if(sdkVersion >= marshmallowVersion)
            {
                using (AndroidJavaClass activityClass = new AndroidJavaClass(UNITY_PLAYER_CLASS))
                {
                    _activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                    using (AndroidJavaClass pluginClass = new AndroidJavaClass(BIOMETRICS_BRIDGE_CLASS))
                    {
                        if (pluginClass != null)
                        {
                            _biometricsBridge = pluginClass.CallStatic<AndroidJavaObject>("instance");
                            _biometricsBridge.Call("setContext", _activityContext);
                            _biometricsBridge.Call("setLegacyMode", m_useLegacyMode);
                            bool available = _biometricsBridge.Call<bool>("isBiometricHardwareAvailable");
                            return available;
                        }
                    }
                }
            }
            return false;
#elif UNITY_IOS && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        protected internal bool IsBiometricPromptAvailable_Native()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int sdkVersion = new AndroidJavaClass ("android.os.Build$VERSION").GetStatic<int> ("SDK_INT");
            int marshmallowVersion = 23;
            if (sdkVersion >= marshmallowVersion)
            {
                using (AndroidJavaClass pluginClass = new AndroidJavaClass(BIOMETRICS_BRIDGE_CLASS))
                {
                    if (pluginClass != null)
                    {
                        _biometricsBridge = pluginClass.CallStatic<AndroidJavaObject>("instance");
                        _biometricsBridge.Call("setLegacyMode", m_useLegacyMode);
                        bool available = _biometricsBridge.Call<bool>("isBiometricPromptEnabled");
                        return available;
                    }
                }
            }
            return false;
#elif UNITY_IOS && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        protected internal void CancelFingerprint_Native()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass activityClass = new AndroidJavaClass(UNITY_PLAYER_CLASS))
            {
                _activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                using (AndroidJavaClass pluginClass = new AndroidJavaClass(BIOMETRICS_BRIDGE_CLASS))
                {
                    if (pluginClass != null)
                    {
                        _biometricsBridge = pluginClass.CallStatic<AndroidJavaObject>("instance");
                        _biometricsBridge.Call("cancelBiometricsAuth");
                    }
                }
            }
#endif
        }

        #endregion

        #region Receivers

        //this functions is used to reduce amount of remaining chances in callback
        protected internal void OnAuthenticationReturn(string p_error)
        {
            bool v_success = string.IsNullOrEmpty(p_error);
            _remainingFailChances--;
            if (v_success || _remainingFailChances <= 0)
            {
                _remainingFailChances = 0;
                if (!v_success)
                    CancelFingerprint_Native();
                else
                    OnAuthenticationBridgeDidFinish(p_error);
            }
            else
            {
                //StartFingerprint_Native(_lastDialogTitle);
                if (OnRemainingFailChancesChanged != null)
                    OnRemainingFailChancesChanged(_remainingFailChances);
            }
        }

        //this is the true return function (after all chances over, or sucess == true)
        protected internal void OnAuthenticationBridgeDidFinish(string p_error)
        {
            _remainingFailChances = 0;
            bool v_success = string.IsNullOrEmpty(p_error);
            if (v_success && !CredentialsKeyStore.HasKey(_processingStoredKey))
            {
                p_error = INVALID_CREDENTIALS;
                v_success = false;
            }

            if (v_success)
            {
                var v_credentialValueStr = CredentialsKeyStore.GetString(_processingStoredKey);
                //Sucess
                if (!string.IsNullOrEmpty(v_credentialValueStr))
                {
                    if (OnAuthSucess != null)
                        OnAuthSucess(new AuthResponse(_processingStoredKey, v_credentialValueStr, null));
                }
                //Failed to retrieve credentials
                else
                {
                    p_error = INVALID_CREDENTIALS;
                    v_success = false;
                }
            }

            if (!v_success)
            {
                Debug.Log("Auth Error: " + p_error);
                if (OnAuthFailed != null)
                    OnAuthFailed(new AuthResponse(_processingStoredKey, null, p_error));
            }

            _processingStoredKey = null;
        }

        #endregion

        #region Public Functions (Static)

        public static bool IsBiometricHardwareAvailable()
        {
            return Instance.IsBiometricHardwareAvailable_Native();
        }

        public static bool Cancel()
        {
            if (Instance.RemainingBiometricsFailure > 0)
            {
                Instance.CancelFingerprint_Native();
                return true;
            }
            return false;
        }

        public static void Load(string p_key, System.Action<AuthResponse> callback, string p_dialogTitle = null, string p_dialogSubtitle = null, string p_cancelText = null)
        {
            if (IsBiometricHardwareAvailable())
            {
                Instance.RetrieveStoredKey_Instance(p_key, callback, p_dialogTitle, p_dialogSubtitle, p_cancelText);
            }
            else
            {
                Instance._processingStoredKey = p_key;
                if (callback != null)
                {
                    System.Action v_delegateUnregister = null;
                    System.Action<AuthResponse> v_delegateCallback = (result) =>
                    {
                        v_delegateUnregister();
                        if (callback != null)
                            callback(result);
                    };
                    v_delegateUnregister = () =>
                    {
                        OnAuthFailed -= v_delegateCallback;
                        OnAuthSucess -= v_delegateCallback;
                    };

                    OnAuthFailed += v_delegateCallback;
                    OnAuthSucess += v_delegateCallback;
                }
                Instance.OnAuthenticationBridgeDidFinish(null);
            }
        }

        public static void Load<T>(string p_key, System.Action<AuthResponse, T> callbackWithCastedData, string p_dialogTitle = null, string p_dialogSubtitle = null, string p_cancelText = null)
        {
            System.Action<AuthResponse> v_delegateCallbackWithCastedData = null;
            if (callbackWithCastedData != null)
            {
                v_delegateCallbackWithCastedData = (response) =>
                {
                    if (callbackWithCastedData != null)
                        callbackWithCastedData(response, response.GetCastedData<T>());
                };
            }
            Load(p_key, v_delegateCallbackWithCastedData, p_dialogTitle, p_dialogSubtitle, p_cancelText);
        }

        public static bool Save<T>(string p_key, T p_credentialData)
        {
            var v_sucess = CredentialsKeyStore.SetCredential<T>(p_key, p_credentialData);
            if (v_sucess)
                CredentialsKeyStore.Save();

            return v_sucess;
        }

        public static bool HasKey(string p_key)
        {
            return CredentialsKeyStore.HasKey(p_key);
        }

        public static void Delete(string p_key)
        {
            CredentialsKeyStore.DeleteKey(p_key);
            CredentialsKeyStore.Save();
        }

        public static void DeleteAll()
        {
            CredentialsKeyStore.DeleteAll();
        }

        #endregion

        #region Helper Classes

        [System.Serializable]
        public class AuthResponse
        {
            #region Private Variables

            [SerializeField]
            string m_requestedKey = "";
            [SerializeField]
            string m_error = null;
            [SerializeField]
            string m_data = null;

            #endregion

            #region Public Properties

            public string Key
            {
                get
                {
                    return m_requestedKey;
                }

                set
                {
                    m_requestedKey = value;
                }
            }

            public string Error
            {
                get
                {
                    return m_error;
                }

                set
                {
                    m_error = value;
                }
            }

            public string Data
            {
                get
                {
                    return m_data;
                }

                set
                {
                    m_data = value;
                }
            }

            #endregion

            #region Constructors

            public AuthResponse(string p_key, string p_data, string p_error)
            {
                m_requestedKey = p_key;
                m_data = p_data;
                m_error = p_error;
            }

            #endregion

            #region Helper Functions

            public T GetCastedData<T>()
            {
                try
                {
                    return !string.IsNullOrEmpty(m_data) ? SerializationUtils.FromJson<T>(m_data) : default(T);
                }
                catch { }
                return default(T);
            }

            public bool IsSucess()
            {
                return !string.IsNullOrEmpty(m_data) && !string.IsNullOrEmpty(m_requestedKey) && string.IsNullOrEmpty(m_error);
            }

            #endregion
        }

        #endregion
    }
}