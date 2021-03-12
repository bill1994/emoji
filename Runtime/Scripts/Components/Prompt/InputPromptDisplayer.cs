using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;
using static MaterialUI.MaterialInputField;

namespace MaterialUI.Internal
{
    public class InputPromptDisplayer : UIBehaviour, ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler, ILayoutElement
    {
        #region Properties

        public InputField.OnChangeEvent onValueChanged
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.onValueChanged : null;
            }
        }

        public InputField.OnChangeEvent onEndEdit
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.onEndEdit : null;
            }
        }

        public UnityEvent onReturnPressed
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.onReturnPressed : null;
            }
        }

        public UnityEvent onPromptSubmit
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.onPromptSubmit : null;
            }
        }

        public DialogPromptAddress customDialogAddress
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.customPromptDialogAddress : null;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField != null)
                    materialInputField.customPromptDialogAddress = value;
            }
        }

        public string hintText
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.hintText : "";
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField != null)
                    materialInputField.hintText = value;
            }
        }

        public InputField.ContentType contentType
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.contentType : InputField.ContentType.Standard;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.contentType == value)
                    return;

                materialInputField.contentType = value;
                EnforceContentType();
            }
        }

        public InputField.CharacterValidation characterValidation
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.characterValidation : InputField.CharacterValidation.None;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.characterValidation == value)
                    return;

                materialInputField.characterValidation = value;
                SetToCustom();
            }
        }

        public InputField.InputType inputType
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.inputType : InputField.InputType.Standard;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.inputType == value)
                    return;

                materialInputField.inputType = value;
                UpdateLabel();
                SetToCustom();
            }
        }

        public InputField.LineType lineType
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.lineType : InputField.LineType.SingleLine;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.lineType == value)
                    return;

                materialInputField.lineType = value;
                SetTextComponentWrapMode();
                SetToCustomIfContentTypeIsNot(InputField.ContentType.Standard, InputField.ContentType.Autocorrected);
            }
        }

        public bool shouldHideMobileInput
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.shouldHideMobileInput : true;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.shouldHideMobileInput == value)
                    return;

                materialInputField.shouldHideMobileInput = value;
            }
        }

        public bool multiLine
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.multiLine : false;
            }
        }

        public char asteriskChar
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.asteriskChar : '•';
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.asteriskChar == value)
                    return;

                materialInputField.asteriskChar = value;
            }
        }

        public string text
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.text : "";
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.text == value)
                    return;

                materialInputField.text = value;
                SendOnValueChangedAndUpdateLabel();
            }
        }

        public float fontSize
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.fontSize : 0;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.fontSize == value)
                    return;

                materialInputField.fontSize = (int)value;
            }
        }

        public Object fontAsset
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.fontAsset : null;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.fontAsset == value)
                    return;

                materialInputField.fontAsset = value;
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
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.characterLimit : 0;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.characterLimit == value)
                    return;

                materialInputField.characterLimit = value;
            }
        }

        public Graphic textComponent
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.inputText : null;
            }
        }

        public TouchScreenKeyboardType keyboardType
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                return materialInputField != null ? materialInputField.keyboardType : TouchScreenKeyboardType.Default;
            }
            set
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                if (materialInputField == null || materialInputField.keyboardType == value)
                    return;

                materialInputField.keyboardType = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();

            if (text == null)
                SetTextWithoutNotify(string.Empty);
            else
            {
                if (textComponent != null)
                {
                    UpdateLabel();
                    if (Application.isPlaying)
                    {
                        textComponent.UnregisterDirtyVerticesCallback(UpdateLabel);
                        textComponent.RegisterDirtyVerticesCallback(UpdateLabel);
                    }
                }
            }
        }

        protected override void OnDisable()
        {
            if (textComponent != null)
                textComponent.UnregisterDirtyVerticesCallback(UpdateLabel);

            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            characterLimit = Mathf.Max(0, characterLimit);

            UpdateLabel();
        }

#endif // if UNITY_EDITOR

        #endregion

        #region UI Unity Functions

        // Trigger all registered callbacks.
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive())
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            OpenPromptDialog();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive())
                return;

            OpenPromptDialog();
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
        }

        #endregion

        #region Helper Functions

        public void ForceLabelUpdate()
        {
            UpdateLabel();
        }

        public void ActivateInputField()
        {
            if (!isFocused)
                OpenPromptDialog();
        }

        public void DeactivateInputField()
        {
            if (isFocused)
                _dialogPrompt.Hide();
        }

        public void SetTextWithoutNotify(string text)
        {
            MaterialInputField materialInputField = GetComponent<MaterialInputField>();
            if (materialInputField != null)
            {
                materialInputField.SetTextWithoutNotify(text);
                UpdateLabel();
            }
        }

        #endregion

        #region Internal Helper Functions

        DialogPrompt _dialogPrompt = null;
        protected virtual void OpenPromptDialog()
        {
            if (!IsActive() || !IsInteractable())
                return;

            var prefabAddress = customDialogAddress == null || customDialogAddress.IsEmpty() || !customDialogAddress.IsResources() ? null : customDialogAddress;
            if (prefabAddress == null)
            {
                OpenDefaultPromptDialog();
            }
            else
            {
                var materialInputField = this.GetComponent<MaterialInputField>();
                var configData = materialInputField != null ? (DialogPrompt.InputFieldConfigData)materialInputField : null;
                DialogManager.ShowCustomDialogAsync<DialogPrompt>(customDialogAddress, (dialog) =>
                {
                    _dialogPrompt = dialog;
                    if (_dialogPrompt != null)
                    {
                        _dialogPrompt.destroyOnHide = true;

                        _dialogPrompt.Initialize(configData,
                            (value) =>
                            {
                                if (this != null)
                                {
                                    value = string.IsNullOrEmpty(value) ? string.Empty : value;
                                    var willChange = text != value;

                                    if (materialInputField != null)
                                        EventSystem.current.SetSelectedGameObject(materialInputField.gameObject);

                                    this.text = value;
                                    if (willChange && onEndEdit != null)
                                        onEndEdit.Invoke(text);
                                    if (onPromptSubmit != null)
                                        onPromptSubmit.Invoke();
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

        public virtual bool IsInteractable()
        {
            var materialInputField = this.GetComponent<MaterialInputField>();
            if (materialInputField != null)
                return materialInputField.IsInteractable();

            return false;
        }

        protected virtual void OpenDefaultPromptDialog()
        {
            if (!IsActive() || !IsInteractable())
                return;

            var materialInputField = this.GetComponent<MaterialInputField>();
            var configData = materialInputField != null ? (DialogPrompt.InputFieldConfigData)materialInputField : null;
            DialogManager.ShowPromptAsync(configData,
                (value) =>
                {
                    if (this != null)
                    {
                        value = string.IsNullOrEmpty(value) ? string.Empty : value;
                        var willChange = text != value;

                        if (materialInputField != null)
                            EventSystem.current.SetSelectedGameObject(materialInputField.gameObject);

                        this.text = value;
                        if (willChange && onEndEdit != null)
                            onEndEdit.Invoke(text);
                        if (onPromptSubmit != null)
                            onPromptSubmit.Invoke();
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

        protected virtual void UpdateLabel()
        {
            MaterialInputField materialInputField = GetComponent<MaterialInputField>();
            if (materialInputField != null)
            {
                materialInputField.ForceLabelUpdate();
            }

            string processed;

            var text = this.text;
            if (text == null)
                text = string.Empty;

            if (inputType == InputField.InputType.Password)
                processed = new string(asteriskChar, text.Length);
            else
                processed = text;

            processed += "\u200B";

            if (textComponent != null)
                textComponent.SetGraphicText(processed);
        }

        protected virtual void SetTextComponentWrapMode()
        {
            SetTextComponentWrapMode(lineType != InputField.LineType.SingleLine);
        }

        protected virtual void SetTextComponentWrapMode(bool value)
        {
            var unityInputField = textComponent as Text;
            var tmpInputField = textComponent as TMP_Text;

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
                        inputType = InputField.InputType.Standard;
                        keyboardType = TouchScreenKeyboardType.Default;
                        characterValidation = InputField.CharacterValidation.None;
                        return;
                    }
                case InputField.ContentType.Autocorrected:
                    {
                        // Don't enforce line type for this content type.
                        inputType = InputField.InputType.AutoCorrect;
                        keyboardType = TouchScreenKeyboardType.Default;
                        characterValidation = InputField.CharacterValidation.None;
                        return;
                    }
                case InputField.ContentType.IntegerNumber:
                    {
                        lineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        inputType = InputField.InputType.Standard;
                        keyboardType = TouchScreenKeyboardType.NumberPad;
                        characterValidation = InputField.CharacterValidation.Integer;
                        return;
                    }
                case InputField.ContentType.DecimalNumber:
                    {
                        lineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        inputType = InputField.InputType.Standard;
                        keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                        characterValidation = InputField.CharacterValidation.Decimal;
                        return;
                    }
                case InputField.ContentType.Alphanumeric:
                    {
                        lineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        inputType = InputField.InputType.Standard;
                        keyboardType = TouchScreenKeyboardType.ASCIICapable;
                        characterValidation = InputField.CharacterValidation.Alphanumeric;
                        return;
                    }
                case InputField.ContentType.Name:
                    {
                        lineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        inputType = InputField.InputType.Standard;
                        keyboardType = TouchScreenKeyboardType.Default;
                        characterValidation = InputField.CharacterValidation.Name;
                        return;
                    }
                case InputField.ContentType.EmailAddress:
                    {
                        lineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        inputType = InputField.InputType.Standard;
                        keyboardType = TouchScreenKeyboardType.EmailAddress;
                        characterValidation = InputField.CharacterValidation.EmailAddress;
                        return;
                    }
                case InputField.ContentType.Password:
                    {
                        lineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        inputType = InputField.InputType.Password;
                        keyboardType = TouchScreenKeyboardType.Default;
                        characterValidation = InputField.CharacterValidation.None;
                        return;
                    }
                case InputField.ContentType.Pin:
                    {
                        lineType = InputField.LineType.SingleLine;
                        SetTextComponentWrapMode(false);
                        inputType = InputField.InputType.Password;
                        keyboardType = TouchScreenKeyboardType.NumberPad;
                        characterValidation = InputField.CharacterValidation.Integer;
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

        #region ILayout Functions

        public virtual float minWidth
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                var unityInputField = materialInputField != null ? materialInputField.inputField as InputField : null;
                var tmpInputField = materialInputField != null ? materialInputField.inputField as TMP_InputField : null;

                return unityInputField != null ? unityInputField.minWidth : (tmpInputField != null ? tmpInputField.minWidth : -1);
            }
        }

        public virtual float preferredWidth
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                var unityInputField = materialInputField != null ? materialInputField.inputField as InputField : null;
                var tmpInputField = materialInputField != null ? materialInputField.inputField as TMP_InputField : null;

                return unityInputField != null ? unityInputField.preferredWidth : (tmpInputField != null ? tmpInputField.preferredWidth : -1);
            }
        }

        public virtual float flexibleWidth
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                var unityInputField = materialInputField != null ? materialInputField.inputField as InputField : null;
                var tmpInputField = materialInputField != null ? materialInputField.inputField as TMP_InputField : null;

                return unityInputField != null ? unityInputField.flexibleWidth : (tmpInputField != null ? tmpInputField.flexibleWidth : -1);
            }
        }

        public virtual float minHeight
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                var unityInputField = materialInputField != null ? materialInputField.inputField as InputField : null;
                var tmpInputField = materialInputField != null ? materialInputField.inputField as TMP_InputField : null;

                return unityInputField != null ? unityInputField.minHeight : (tmpInputField != null ? tmpInputField.minHeight : -1);
            }
        }

        public virtual float preferredHeight
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                var unityInputField = materialInputField != null ? materialInputField.inputField as InputField : null;
                var tmpInputField = materialInputField != null ? materialInputField.inputField as TMP_InputField : null;

                return unityInputField != null ? unityInputField.preferredHeight : (tmpInputField != null ? tmpInputField.preferredHeight : -1);
            }
        }

        public virtual float flexibleHeight
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                var unityInputField = materialInputField != null ? materialInputField.inputField as InputField : null;
                var tmpInputField = materialInputField != null ? materialInputField.inputField as TMP_InputField : null;

                return unityInputField != null ? unityInputField.flexibleHeight : (tmpInputField != null ? tmpInputField.flexibleHeight : -1);
            }
        }

        public virtual int layoutPriority
        {
            get
            {
                MaterialInputField materialInputField = GetComponent<MaterialInputField>();

                var unityInputField = materialInputField != null ? materialInputField.inputField as InputField : null;
                var tmpInputField = materialInputField != null ? materialInputField.inputField as TMP_InputField : null;

                return unityInputField != null ? unityInputField.layoutPriority : (tmpInputField != null ? tmpInputField.layoutPriority : 0);
            }
        }

        public virtual void CalculateLayoutInputHorizontal()
        {
        }

        public virtual void CalculateLayoutInputVertical()
        {
        }

        #endregion
    }
}
