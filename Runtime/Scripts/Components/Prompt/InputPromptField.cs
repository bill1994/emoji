using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

namespace MaterialUI
{
    public class InputPromptField : Selectable, IPointerClickHandler, ISubmitHandler
    {
        #region Private Variables

        [SerializeField]
        Graphic m_TextComponent = null;
        [SerializeField]
        [TextArea(3, 10)]
        protected string m_Text = string.Empty;

        [Header("Prompt Dialog Settings")]
        [SerializeField]
        string m_CustomDialogPath = "";

        [Header("Input Field Settings")]
        [SerializeField]
        int m_CharacterLimit = 0;
        [SerializeField]
        private InputField.ContentType m_ContentType = InputField.ContentType.Standard;
        [SerializeField]
        private InputField.InputType m_InputType = InputField.InputType.Standard;
        [SerializeField]
        private InputField.LineType m_LineType = InputField.LineType.SingleLine;
        [SerializeField]
        private InputField.CharacterValidation m_CharacterValidation = InputField.CharacterValidation.None;
        [SerializeField]
        TouchScreenKeyboardType m_KeyboardType = TouchScreenKeyboardType.Default;

        [Header("Control Settings")]
        [SerializeField]
        private bool m_HideMobileInput = false;

        [Header("Password Special Settings")]
        [SerializeField]
        char m_AsteriskChar = '•';

        #endregion

        #region Callbacks

        [Header("Callbacks")]
        public InputField.OnChangeEvent onValueChanged = new InputField.OnChangeEvent();
        public InputField.SubmitEvent onEndEdit = new InputField.SubmitEvent();
        public UnityEvent OnReturnPressed = new UnityEvent();

        #endregion

        #region Properties

        public string customDialogPath
        {
            get
            {
                return m_CustomDialogPath;
            }
            set
            {
                if (m_CustomDialogPath == value)
                    return;
                m_CustomDialogPath = value;
            }
        }

        public string hintText
        {
            get
            {
                MaterialInputField v_materialInputField = GetComponent<MaterialInputField>();

                return v_materialInputField != null ? v_materialInputField.hintText : "";
            }
            set
            {
                MaterialInputField v_materialInputField = GetComponent<MaterialInputField>();

                if (v_materialInputField != null)
                    v_materialInputField.hintText = value;
            }
        }

        public InputField.ContentType contentType
        {
            get
            {
                return m_ContentType;
            }
            set
            {
                if (m_ContentType == value)
                    return;
                m_ContentType = value;
                EnforceContentType();
            }
        }

        public InputField.CharacterValidation characterValidation
        {
            get
            {
                return m_CharacterValidation;
            }
            set
            {
                if (m_CharacterValidation == value)
                    return;
                m_CharacterValidation = value;
                SetToCustom();
            }
        }

        public InputField.InputType inputType
        {
            get
            {
                return m_InputType;
            }
            set
            {
                if (m_InputType == value)
                    return;
                m_InputType = value;
                UpdateLabel();
                SetToCustom();
            }
        }

        public InputField.LineType lineType
        {
            get
            {
                return m_LineType;
            }
            set
            {
                if (m_LineType == value)
                    return;
                m_LineType = value;
                SetTextComponentWrapMode();
                SetToCustomIfContentTypeIsNot(InputField.ContentType.Standard, InputField.ContentType.Autocorrected);
            }
        }

        public bool shouldHideMobileInput
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
                        return m_HideMobileInput;
                }

                return true;
            }
            set
            {
                if (m_HideMobileInput == value)
                    return;
                m_HideMobileInput = value;
            }
        }

        public bool multiLine
        {
            get { return m_LineType == InputField.LineType.MultiLineNewline || lineType == InputField.LineType.MultiLineSubmit; }
        }

        public char asteriskChar
        {
            get
            {
                return m_AsteriskChar;
            }

            set
            {
                m_AsteriskChar = value;
            }
        }

        public string text
        {
            get
            {
                return m_Text;
            }
            set
            {
                value = value == null ? string.Empty : value;
                if (m_Text == value)
                    return;

                m_Text = value;
                SendOnValueChangedAndUpdateLabel();
            }
        }

        public float fontSize
        {
            get
            {
                if (m_TextComponent is Text)
                    return (m_TextComponent as Text).fontSize;
                if (m_TextComponent is TMP_Text)
                    return (m_TextComponent as TMP_Text).fontSize;
                return 0;
            }
            set
            {
                var unityText = m_TextComponent as Text;
                var tmpText = m_TextComponent as TMP_Text;

                if (unityText != null)
                    unityText.fontSize = (int)value;
                else if (tmpText != null)
                    tmpText.fontSize = value;
            }
        }

        public Object fontAsset
        {
            get
            { if (m_TextComponent is Text)
                    return (m_TextComponent as Text).font;
                if (m_TextComponent is TMP_Text)
                    return (m_TextComponent as TMP_Text).font;
                return null;
            }
            set
            {
                var unityText = m_TextComponent as Text;
                var tmpText = m_TextComponent as TMP_Text;

                if (unityText != null && (value is Font || value == (UnityEngine.Object)null))
                    unityText.font = value as Font;
                else if (tmpText != null && (value is TMP_FontAsset || value == (UnityEngine.Object)null))
                    tmpText.font = value as TMP_FontAsset;
            }
        }

        public bool isFocused
        {
            get
            {
                return _dialogPrompt != null;
            }
        }

        public int characterLimit
        {
            get
            {
                return m_CharacterLimit;
            }
            set
            {
                if (m_CharacterLimit == value)
                    return;
                m_CharacterLimit = value;
            }
        }

        public Graphic textComponent
        {
            get
            {
                return m_TextComponent;
            }
            set
            {
                if (m_TextComponent == value)
                    return;
                m_TextComponent = value;
            }
        }

        public TouchScreenKeyboardType keyboardType
        {
            get
            {
                return m_KeyboardType;
            }
            set
            {
                if (m_KeyboardType == value)
                    return;
                m_KeyboardType = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_Text == null)
                m_Text = string.Empty;
            if (m_TextComponent != null)
            {
                UpdateLabel();
                if (Application.isPlaying)
                {
                    m_TextComponent.UnregisterDirtyVerticesCallback(UpdateLabel);
                    m_TextComponent.RegisterDirtyVerticesCallback(UpdateLabel);
                }
            }
        }

        protected override void OnDisable()
        {
            if (m_TextComponent != null)
                m_TextComponent.UnregisterDirtyVerticesCallback(UpdateLabel);

            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_CharacterLimit = Mathf.Max(0, m_CharacterLimit);

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateLabel();
        }

#endif // if UNITY_EDITOR

        #endregion

        #region UI Unity Functions

        // Trigger all registered callbacks.
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            OpenPromptDialog();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            OpenPromptDialog();
        }

        #endregion

        #region Helper Functions

        public void ForceLabelUpdate()
        {
            UpdateLabel();
        }

        public void ActivateInputField()
        {
            if(!isFocused)
                OpenPromptDialog();
        }

        public void DeactivateInputField()
        {
            if (isFocused)
                _dialogPrompt.Hide();
        }

        public void SetTextWithoutNotify(string text)
        {
            m_Text = string.IsNullOrEmpty(text)? string.Empty : text;
            UpdateLabel();
        }

        #endregion

        #region Internal Helper Functions

        DialogPrompt _dialogPrompt = null;
        protected virtual void OpenPromptDialog()
        {
            if (!IsActive() || !IsInteractable())
                return;

            if (string.IsNullOrEmpty(m_CustomDialogPath))
            {
                OpenDefaultPromptDialog();
            }
            else
            {
                DialogManager.ShowCustomDialogAsync<DialogPrompt>(m_CustomDialogPath, (dialog) =>
                {
                    _dialogPrompt = dialog;
                    if (_dialogPrompt != null)
                    {
                        _dialogPrompt.destroyOnHide = true;
                        _dialogPrompt.Initialize(this,
                            (value) =>
                            {
                                if (this != null)
                                {
                                    value = string.IsNullOrEmpty(value) ? string.Empty : value;
                                    var willChange = m_Text != value;

                                    this.text = value;
                                    if (willChange && onEndEdit != null)
                                        onEndEdit.Invoke(m_Text);
                                    if (OnReturnPressed != null)
                                        OnReturnPressed.Invoke();
                                }
                            },
                            "OK",
                            this.hintText,
                            null,
                            null,
                            "Cancel");
                    }
                    else
                        OpenDefaultPromptDialog();
                });
            }
        }

        protected virtual void OpenDefaultPromptDialog()
        {
            if (!IsActive() || !IsInteractable())
                return;

            DialogManager.ShowPromptAsync(this,
                (value) =>
                {
                    if (this != null)
                    {
                        this.text = value;
                        if (OnReturnPressed != null)
                            OnReturnPressed.Invoke();
                    }
                },
                "OK",
                this.hintText,
                null,
                null,
                "Cancel",
                (dialog) =>
                {
                    _dialogPrompt = dialog;
                    if (_dialogPrompt != null)
                        _dialogPrompt.destroyOnHide = true;
                });
        }

        protected virtual void SetToCustomIfContentTypeIsNot(params InputField.ContentType[] allowedContentTypes)
        {
            if (contentType == InputField.ContentType.Custom)
                return;

            for (int i = 0; i < allowedContentTypes.Length; i++)
                if (contentType == allowedContentTypes[i])
                    return;

            contentType = InputField.ContentType.Custom;
        }

        protected virtual void SetToCustom()
        {
            if (contentType == InputField.ContentType.Custom)
                return;

            contentType = InputField.ContentType.Custom;
        }

        protected virtual void SendOnValueChangedAndUpdateLabel()
        {
            SendOnValueChanged();
            UpdateLabel();
        }

        protected virtual void SendOnValueChanged()
        {
            if (onValueChanged != null)
                onValueChanged.Invoke(text);
        }

        protected virtual void UpdateLabel ()
        {
            string processed;
            if (m_Text == null)
                m_Text = string.Empty;

            if (inputType == InputField.InputType.Password)
                processed = new string(asteriskChar, m_Text.Length);
            else
                processed = m_Text;

            processed += "\u200B";

            if (m_TextComponent != null)
                m_TextComponent.SetGraphicText(processed);
        }

        protected virtual void SetTextComponentWrapMode()
        {
            SetTextComponentWrapMode(m_LineType != InputField.LineType.SingleLine);
        }

        protected virtual void SetTextComponentWrapMode(bool value)
        {
            var unityInputField = m_TextComponent as Text;
            var tmpInputField = m_TextComponent as TMP_Text;

            if (!value)
            {
                if (unityInputField != null)
                    unityInputField.horizontalOverflow = HorizontalWrapMode.Overflow;
                else
                    tmpInputField.enableWordWrapping = false;
            }
            else
            {
                if (unityInputField != null)
                    unityInputField.horizontalOverflow = HorizontalWrapMode.Wrap;
                else
                    tmpInputField.enableWordWrapping = true;
            }
        }

        protected void EnforceContentType()
        {
            switch (contentType)
            {
                case InputField.ContentType.Standard:
                    {
                        // Don't enforce line type for this content type.
                        m_InputType = InputField.InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = InputField.CharacterValidation.None;
                        return;
                    }
                case InputField.ContentType.Autocorrected:
                    {
                        // Don't enforce line type for this content type.
                        m_InputType = InputField.InputType.AutoCorrect;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = InputField.CharacterValidation.None;
                        return;
                    }
                case InputField.ContentType.IntegerNumber:
                    {
                        m_LineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        m_InputType = InputField.InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                        m_CharacterValidation = InputField.CharacterValidation.Integer;
                        return;
                    }
                case InputField.ContentType.DecimalNumber:
                    {
                        m_LineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        m_InputType = InputField.InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                        m_CharacterValidation = InputField.CharacterValidation.Decimal;
                        return;
                    }
                case InputField.ContentType.Alphanumeric:
                    {
                        m_LineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        m_InputType = InputField.InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.ASCIICapable;
                        m_CharacterValidation = InputField.CharacterValidation.Alphanumeric;
                        return;
                    }
                case InputField.ContentType.Name:
                    {
                        m_LineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        m_InputType = InputField.InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = InputField.CharacterValidation.Name;
                        return;
                    }
                case InputField.ContentType.EmailAddress:
                    {
                        m_LineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        m_InputType = InputField.InputType.Standard;
                        m_KeyboardType = TouchScreenKeyboardType.EmailAddress;
                        m_CharacterValidation = InputField.CharacterValidation.EmailAddress;
                        return;
                    }
                case InputField.ContentType.Password:
                    {
                        m_LineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        m_InputType = InputField.InputType.Password;
                        m_KeyboardType = TouchScreenKeyboardType.Default;
                        m_CharacterValidation = InputField.CharacterValidation.None;
                        return;
                    }
                case InputField.ContentType.Pin:
                    {
                        m_LineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        m_InputType = InputField.InputType.Password;
                        m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                        m_CharacterValidation = InputField.CharacterValidation.Integer;
                        return;
                    }
                default:
                    {
                        // Includes Custom type. Nothing should be enforced.
                        return;
                    }

            }
        }

        #endregion
    }
}
