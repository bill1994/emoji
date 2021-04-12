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

        protected void RetrieveStoredKey_Instance(string storedKey, System.Action<AuthResponse> callback, string dialogTitle = null, string dialogSubtitle = null, string cancelText = null)
        {
            if (CredentialsKeyStore.HasKey(storedKey))
            {
                //Create and register delegates
                if (callback != null)
                {
                    System.Action delegateUnregister = null;
                    System.Action<AuthResponse> delegateCallback = (result) =>
                    {
                        delegateUnregister();
                        if (callback != null)
                            callback(result);
                    };
                    delegateUnregister = () =>
                    {
                        OnAuthFailed -= delegateCallback;
                        OnAuthSucess -= delegateCallback;
                    };

                    OnAuthFailed += delegateCallback;
                    OnAuthSucess += delegateCallback;
                }

                if (IsBiometricHardwareAvailable_Native())
                {
                    _processingStoredKey = storedKey;
                    StartFingerprint_Native(dialogTitle, dialogSubtitle, cancelText);
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
	    private static extern void _StartBiometricsAuth (string objectName, string authenticationPopupDescription);

#endif

        int _remainingFailChances = 0;
        protected internal void StartFingerprint_Native(string dialogTitle, string dialogSubtitle, string cancelText)
        {
            if (dialogTitle == null)
                dialogTitle = m_dialogDefaultTitle;

            if (dialogSubtitle == null)
                dialogSubtitle = m_dialogDefaultSubtitle;

            if (cancelText == null)
                cancelText = m_dialogDefaultCancelText;

            dialogTitle = Kyub.Localization.LocaleManager.GetLocalizedText(dialogTitle);
            dialogSubtitle = Kyub.Localization.LocaleManager.GetLocalizedText(dialogSubtitle);
            cancelText = Kyub.Localization.LocaleManager.GetLocalizedText(cancelText);


            var isNativePromptAvailable = IsBiometricPromptAvailable_Native();

            //Native Prompts has infinity chances to fail
            _remainingFailChances = m_maxBiometricsFailure;

            if (!isNativePromptAvailable && !Application.isEditor && Application.isMobilePlatform)
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
                            fingerprintDialog.Initialize(dialogTitle, dialogSubtitle, null, cancelText);
                        }
                    };

                    //Instantiate Dialog
                    MaterialUI.DialogManager.ShowModalCustomDialogAsync<Kyub.Credentials.UI.DialogFingerprint>(m_fingerprintDialogPath, onInitializeDelegate);
                }
                else
                {
                    _activeFingerprintDialog.Initialize(dialogTitle, dialogSubtitle);
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
                        _biometricsBridge.Call("startBiometricsAuth", this.gameObject.name, dialogTitle, "", dialogSubtitle, cancelText);
                    }
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            _remainingFailChances = 1;
            _StartBiometricsAuth(this.gameObject.name, dialogSubtitle);
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
        protected internal void OnAuthenticationReturn(string error)
        {
            bool success = string.IsNullOrEmpty(error);
            _remainingFailChances--;
            if (success || _remainingFailChances <= 0)
            {
                _remainingFailChances = 0;
                if (!success)
                    CancelFingerprint_Native();
                else
                    OnAuthenticationBridgeDidFinish(error);
            }
            else
            {
                //StartFingerprint_Native(_lastDialogTitle);
                if (OnRemainingFailChancesChanged != null)
                    OnRemainingFailChancesChanged(_remainingFailChances);
            }
        }

        //this is the true return function (after all chances over, or sucess == true)
        protected internal void OnAuthenticationBridgeDidFinish(string error)
        {
            _remainingFailChances = 0;
            bool success = string.IsNullOrEmpty(error);
            if (success && !CredentialsKeyStore.HasKey(_processingStoredKey))
            {
                error = INVALID_CREDENTIALS;
                success = false;
            }

            if (success)
            {
                var credentialValueStr = CredentialsKeyStore.GetString(_processingStoredKey);
                //Sucess
                if (!string.IsNullOrEmpty(credentialValueStr))
                {
                    if (OnAuthSucess != null)
                        OnAuthSucess(new AuthResponse(_processingStoredKey, credentialValueStr, null));
                }
                //Failed to retrieve credentials
                else
                {
                    error = INVALID_CREDENTIALS;
                    success = false;
                }
            }

            if (!success)
            {
                Debug.Log("Auth Error: " + error);
                if (OnAuthFailed != null)
                    OnAuthFailed(new AuthResponse(_processingStoredKey, null, error));
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

        public static void Load(string key, System.Action<AuthResponse> callback, string dialogTitle = null, string dialogSubtitle = null, string cancelText = null)
        {
            if (IsBiometricHardwareAvailable())
            {
                Instance.RetrieveStoredKey_Instance(key, callback, dialogTitle, dialogSubtitle, cancelText);
            }
            else
            {
                Instance._processingStoredKey = key;
                if (callback != null)
                {
                    System.Action delegateUnregister = null;
                    System.Action<AuthResponse> delegateCallback = (result) =>
                    {
                        delegateUnregister();
                        if (callback != null)
                            callback(result);
                    };
                    delegateUnregister = () =>
                    {
                        OnAuthFailed -= delegateCallback;
                        OnAuthSucess -= delegateCallback;
                    };

                    OnAuthFailed += delegateCallback;
                    OnAuthSucess += delegateCallback;
                }
                Instance.OnAuthenticationBridgeDidFinish(null);
            }
        }

        public static void Load<T>(string key, System.Action<AuthResponse, T> callbackWithCastedData, string dialogTitle = null, string dialogSubtitle = null, string cancelText = null)
        {
            System.Action<AuthResponse> delegateCallbackWithCastedData = null;
            if (callbackWithCastedData != null)
            {
                delegateCallbackWithCastedData = (response) =>
                {
                    if (callbackWithCastedData != null)
                        callbackWithCastedData(response, response.GetCastedData<T>());
                };
            }
            Load(key, delegateCallbackWithCastedData, dialogTitle, dialogSubtitle, cancelText);
        }

        public static bool Save<T>(string key, T credentialData)
        {
            var sucess = CredentialsKeyStore.SetCredential<T>(key, credentialData);
            if (sucess)
                CredentialsKeyStore.Save();

            return sucess;
        }

        public static bool HasKey(string key)
        {
            return CredentialsKeyStore.HasKey(key);
        }

        public static void Delete(string key)
        {
            CredentialsKeyStore.DeleteKey(key);
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

            public AuthResponse(string key, string data, string error)
            {
                m_requestedKey = key;
                m_data = data;
                m_error = error;
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