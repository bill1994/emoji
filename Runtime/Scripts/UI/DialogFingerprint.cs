using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kyub.Credentials;
using System;
using MaterialUI;

namespace Kyub.Credentials.UI
{
    public class DialogFingerprint : MaterialDialogCompat
    {
        #region Private Variables

        [Header("Main Fingerprint Components")]
        [SerializeField]
        private Graphic m_title = null;
        [SerializeField]
        private Graphic m_subTitle = null;
        [SerializeField]
        private DialogTitleSection m_fingerPrintSession = new DialogTitleSection();
        [SerializeField]
        private MaterialButton m_buttonDismiss = null;

        [Header("Visual Fingerprint Fields")]
        [SerializeField]
        float m_animateSessionDuration = 2.0f;
        [Space]
        [SerializeField]
        FingerprintSessionStyle m_sessionActiveData = new FingerprintSessionStyle();
        [SerializeField]
        FingerprintSessionStyle m_sessionInactiveData = new FingerprintSessionStyle();

        int _sessionInactiveTween = -1;
        System.Action _onDismisseButtonClicked = null;

        #endregion

        #region Properties

        public float AnimateSessionDuration
        {
            get
            {
                return m_animateSessionDuration;
            }

            set
            {
                m_animateSessionDuration = value;
            }
        }

        public Graphic Title
        {
            get
            {
                return m_title;
            }

            set
            {
                m_title = value;
            }
        }

        public Graphic SubTitle
        {
            get
            {
                return m_subTitle;
            }

            set
            {
                m_subTitle = value;
            }
        }

        public DialogTitleSection FingerPrintSession
        {
            get
            {
                return m_fingerPrintSession;
            }

            set
            {
                m_fingerPrintSession = value;
            }
        }

        public MaterialButton ButtonDismiss
        {
            get
            {
                return m_buttonDismiss;
            }

            set
            {
                m_buttonDismiss = value;
            }
        }

        public FingerprintSessionStyle SessionActiveData
        {
            get
            {
                return m_sessionActiveData;
            }

            set
            {
                m_sessionActiveData = value;
            }
        }

        public FingerprintSessionStyle SessionInactiveData
        {
            get
            {
                return m_sessionInactiveData;
            }

            set
            {
                m_sessionInactiveData = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            SetFingerprintSessionActive(true);
            RegisterEvents();
        }

        protected override void OnDisable()
        {
            base.OnEnable();
            UnregisterEvents();
        }

        #endregion

        #region Public Functions

        public void Initialize(string p_subtitleText, System.Action onDismissiveButtonClicked = null, string p_dismissiveButtonText = "CANCEL")
        {
            Initialize(m_title != null ? m_title.GetGraphicText() : "", p_subtitleText, onDismissiveButtonClicked, p_dismissiveButtonText);
        }

        public void Initialize(string p_titleText, string p_subtitleText, System.Action onDismissiveButtonClicked = null, string p_dismissiveButtonText = "CANCEL")
        {
            if (m_title != null)
                m_title.SetGraphicText(p_titleText);
            if (m_subTitle != null)
                m_subTitle.SetGraphicText(p_subtitleText);
            if (m_buttonDismiss != null && m_buttonDismiss.text != null)
                m_buttonDismiss.text.SetGraphicText(p_dismissiveButtonText);
            _onDismisseButtonClicked = onDismissiveButtonClicked;

            AnimateFingerprintSessionActive(true);

            //Initialize();

            //Prevent bugs when fingerprint submit error before finish loading dialog
            if (BiometricCredentialsManager.Instance.RemainingBiometricsFailure <= 0)
                DismissiveButtonClicked();
        }

        public void AnimateFingerprintSessionActive(bool p_active)
        {
            TweenManager.EndTween(_sessionInactiveTween);
            if (p_active)
                SetFingerprintSessionActive(true);
            else
            {
                SetFingerprintSessionActive(false);
                _sessionInactiveTween = TweenManager.TweenFloat((value) => { }, 0, 1, m_animateSessionDuration, 0,
                    () =>
                    {
                        if (this != null)
                            SetFingerprintSessionActive(true);
                    });
            }
        }

        public void DismissiveButtonClicked()
        {
            if (_onDismisseButtonClicked != null)
                _onDismisseButtonClicked();
            //Emit Cancel
            BiometricCredentialsManager.Cancel();
            DisposeWindow();
        }

        #endregion

        #region Helper Functions

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (BiometricCredentialsManager.Instance != null)
            {
                BiometricCredentialsManager.OnRemainingFailChancesChanged += HandleOnRemainingFailChancesChanged;
                BiometricCredentialsManager.OnAuthFailed += HandleOnAuthFailed;
                BiometricCredentialsManager.OnAuthSucess += HandleOnAuthSucess;
            }
        }

        protected virtual void UnregisterEvents()
        {
            if (BiometricCredentialsManager.Instance != null)
            {
                BiometricCredentialsManager.OnRemainingFailChancesChanged -= HandleOnRemainingFailChancesChanged;
                BiometricCredentialsManager.OnAuthFailed -= HandleOnAuthFailed;
                BiometricCredentialsManager.OnAuthSucess -= HandleOnAuthSucess;
            }
        }

        protected override void ValidateKeyTriggers(MaterialFocusGroup p_materialKeyFocus)
        {
            if (p_materialKeyFocus != null)
            {
                var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_cancelTrigger.Name = "Escape KeyDown";
                v_cancelTrigger.Key = KeyCode.Escape;
                v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, DismissiveButtonClicked);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_cancelTrigger };
            }
        }

        protected void SetFingerprintSessionActive(bool p_active)
        {
            TweenManager.EndTween(_sessionInactiveTween);
            var v_sessionData = p_active ? m_sessionActiveData : m_sessionInactiveData;
            m_fingerPrintSession.SetTitle(v_sessionData.Text, new ImageData(v_sessionData.Icon));

            if (m_fingerPrintSession.text != null)
                m_fingerPrintSession.text.color = v_sessionData.TextColor;
            if (m_fingerPrintSession.vectorImageData != null)
                m_fingerPrintSession.vectorImageData.color = v_sessionData.IconColor;
            if (m_fingerPrintSession.sprite != null)
                m_fingerPrintSession.sprite.color = v_sessionData.IconColor;
        }

        protected virtual void DisposeWindow()
        {
            _onDismisseButtonClicked = null;
            UnregisterEvents();
            Hide();
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnAuthSucess(BiometricCredentialsManager.AuthResponse p_response)
        {
            SetFingerprintSessionActive(true);
            DisposeWindow();
        }

        protected virtual void HandleOnAuthFailed(BiometricCredentialsManager.AuthResponse p_response)
        {
            SetFingerprintSessionActive(false);
            DisposeWindow();
        }

        private void HandleOnRemainingFailChancesChanged(int p_remainingChances)
        {
            AnimateFingerprintSessionActive(false);
        }

        #endregion

        #region Helper Data

        [System.Serializable]
        public class FingerprintSessionStyle
        {
            #region Private Variables

            [SerializeField]
            VectorImageData m_icon = null;
            [SerializeField]
            string m_text = "";
            [SerializeField]
            Color m_iconColor = MaterialColor.teal500;
            [SerializeField]
            Color m_textColor = MaterialColor.grey500;


            #endregion

            #region Public Properties

            public VectorImageData Icon
            {
                get
                {
                    return m_icon;
                }

                set
                {
                    m_icon = value;
                }
            }

            public string Text
            {
                get
                {
                    return m_text;
                }

                set
                {
                    m_text = value;
                }
            }

            public Color IconColor
            {
                get
                {
                    return m_iconColor;
                }

                set
                {
                    m_iconColor = value;
                }
            }

            public Color TextColor
            {
                get
                {
                    return m_textColor;
                }

                set
                {
                    m_textColor = value;
                }
            }

            #endregion
        }

        #endregion
    }
}
