//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Prompt", 1)]
    public class DialogPrompt : MaterialDialogCompat
    {
        #region Private Variables

        [SerializeField]
        private DialogTitleSection m_TitleSection = new DialogTitleSection();
        [SerializeField]
        private DialogButtonSection m_ButtonSection = new DialogButtonSection();
        [SerializeField]
        private MaterialInputField m_FirstInputField = null;
        [SerializeField]
        private MaterialInputField m_SecondInputField = null;

        #endregion

        #region Public Properties

        public DialogTitleSection titleSection
        {
            get { return m_TitleSection; }
            set { m_TitleSection = value; }
        }

        public DialogButtonSection buttonSection
        {
            get { return m_ButtonSection; }
            set { m_ButtonSection = value; }
        }

        public MaterialInputField firstInputField
        {
            get { return m_FirstInputField; }
        }

        public MaterialInputField secondInputField
        {
            get { return m_SecondInputField; }
        }

        #endregion

        #region Callbacks

        private Action<string> m_OnAffirmativeOneButtonClicked;
        public Action<string> onAffirmativeOneButtonClicked
        {
            get { return m_OnAffirmativeOneButtonClicked; }
            set { m_OnAffirmativeOneButtonClicked = value; }
        }

        private Action<string, string> m_OnAffirmativeTwoButtonClicked;
        public Action<string, string> onAffirmativeTwoButtonClicked
        {
            get { return m_OnAffirmativeTwoButtonClicked; }
            set { m_OnAffirmativeTwoButtonClicked = value; }
        }

        #endregion

        #region Helper Functions

        protected override void ValidateKeyTriggers(MaterialFocusGroup materialKeyFocus)
        {
            if (materialKeyFocus != null)
            {
                var affirmativeTrigger = new MaterialFocusGroup.KeyTriggerData();
                affirmativeTrigger.Name = "Return KeyDown";
                affirmativeTrigger.Key = KeyCode.Return;
                affirmativeTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(affirmativeTrigger.OnCallTrigger, AffirmativeButtonClickedConditional);

                var cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                cancelTrigger.Name = "Escape KeyDown";
                cancelTrigger.Key = KeyCode.Escape;
                cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(cancelTrigger.OnCallTrigger, DismissiveButtonClicked);

                materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { affirmativeTrigger, cancelTrigger };
            }
        }

        public void Initialize(string firstFieldName, Action<string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            var firstFieldConfig = new InputFieldConfigData() { hintText = firstFieldName };
            Initialize(firstFieldConfig, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
        }

        public void Initialize(InputFieldConfigData firstFieldConfig, Action<string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            m_OnAffirmativeOneButtonClicked = onAffirmativeButtonClicked;
            CommonInitialize(firstFieldConfig, null, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
        }

        public void Initialize(InputFieldConfigData firstFieldConfig, InputFieldConfigData secondFieldConfig, Action<string, string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            m_OnAffirmativeTwoButtonClicked = onAffirmativeButtonClicked;
            CommonInitialize(firstFieldConfig, secondFieldConfig, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
        }

        public void Initialize(string firstFieldName, string secondFieldName, Action<string, string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            var firstFieldConfig = new InputFieldConfigData() { hintText = firstFieldName };
            var secondFieldConfig = secondFieldName == null ? null : new InputFieldConfigData() { hintText = secondFieldName };
            Initialize(firstFieldConfig, secondFieldConfig, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
        }

        private void CommonInitialize(InputFieldConfigData firstFieldConfig, InputFieldConfigData secondFieldConfig, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            if (m_TitleSection != null)
                m_TitleSection.SetTitle(titleText, icon);

            if (m_ButtonSection != null)
            {
                m_ButtonSection.SetButtons(null, affirmativeButtonText, onDismissiveButtonClicked, dismissiveButtonText);
                m_ButtonSection.SetupButtonLayout(rectTransform);
            }

            if (m_FirstInputField)
            {
                if (firstFieldConfig != null)
                    firstFieldConfig.Apply(m_FirstInputField);
                m_FirstInputField.inputPromptDisplayOption = (MaterialInputField.InputPromptDisplayMode)0;
            }
            if (m_SecondInputField)
            {
                if (secondFieldConfig != null)
                    secondFieldConfig.Apply(m_SecondInputField);

                m_SecondInputField.inputPromptDisplayOption = (MaterialInputField.InputPromptDisplayMode)0;
                m_SecondInputField.gameObject.SetActive(secondFieldConfig != null);
            }

            UpdateAffirmativeButtonState();

            //Initialize();
        }

        protected virtual void AffirmativeButtonClickedConditional()
        {
            if ((m_FirstInputField == null || !m_FirstInputField.isFocused || !m_FirstInputField.multiLine) &&
                (m_SecondInputField == null || !m_SecondInputField.isFocused || !m_SecondInputField.multiLine))
                AffirmativeButtonClicked();
        }

        public virtual void AffirmativeButtonClicked()
        {
            if (m_OnAffirmativeOneButtonClicked != null && m_FirstInputField)
            {
                m_OnAffirmativeOneButtonClicked(m_FirstInputField.text);
            }

            if (m_OnAffirmativeTwoButtonClicked != null && m_FirstInputField)
            {
                m_OnAffirmativeTwoButtonClicked(m_FirstInputField.text, m_SecondInputField.text);
            }

            Hide();
        }

        public void DismissiveButtonClicked()
        {
            if (m_ButtonSection != null)
                m_ButtonSection.OnDismissiveButtonClicked();
            Hide();
        }

        public virtual void UpdateAffirmativeButtonState()
        {
            bool isButtonInteractable = true;

            if (m_FirstInputField && m_FirstInputField.customTextValidator != null)
            {
                isButtonInteractable = m_FirstInputField.customTextValidator.IsTextValid();
            }

            if (m_SecondInputField && m_SecondInputField.gameObject.activeSelf)
            {
                if (m_SecondInputField.customTextValidator != null)
                {
                    isButtonInteractable &= m_SecondInputField.customTextValidator.IsTextValid();
                }
            }

            if (m_ButtonSection != null && m_ButtonSection.affirmativeButton != null)
                m_ButtonSection.affirmativeButton.interactable = isButtonInteractable;
        }

        #endregion

        #region Receivers

        public override void OnActivityEndShow()
        {
            base.OnActivityEndShow();

            if (firstInputField) firstInputField.inputField.Select();
        }

        #endregion

        #region Helper Classes

        public class InputFieldConfigData
        {
            #region Private Variables

            [SerializeField]
            private string m_Text = string.Empty;
            [SerializeField]
            private string m_HintText = string.Empty;
            [SerializeField]
            private InputField.InputType m_InputType = InputField.InputType.Standard;
            [SerializeField]
            private InputField.LineType m_LineType = InputField.LineType.SingleLine;
            [SerializeField]
            private TouchScreenKeyboardType m_KeyboardType = TouchScreenKeyboardType.Default;
            [SerializeField]
            private InputField.ContentType m_ContentType = InputField.ContentType.Standard;
            [SerializeField]
            private InputField.CharacterValidation m_CharacterValidation = InputField.CharacterValidation.None;
            [SerializeField]
            private int m_CharacterLimit = 0;
            [SerializeField]
            private char m_AsteriskChar = '•';
            [SerializeField]
            private bool m_HideMobileInput = false;
            [SerializeField]
            ITextValidator m_CustomTextValidator = null;

            [SerializeField]
            Dictionary<string, object> m_extraProperties = new Dictionary<string, object>();

            #endregion

            #region Properties

            public string text
            {
                get { return m_Text; }
                set { m_Text = value; }
            }

            public string hintText
            {
                get { return m_HintText; }
                set { m_HintText = value; }
            }

            public InputField.InputType inputType
            {
                get { return m_InputType; }
                set { m_InputType = value; }
            }

            public InputField.LineType lineType
            {
                get { return m_LineType; }
                set { m_LineType = value; }
            }

            public TouchScreenKeyboardType keyboardType
            {
                get { return m_KeyboardType; }
                set { m_KeyboardType = value; }
            }

            public InputField.ContentType contentType
            {
                get { return m_ContentType; }
                set { m_ContentType = value; }
            }

            public InputField.CharacterValidation characterValidation
            {
                get { return m_CharacterValidation; }
                set { m_CharacterValidation = value; }
            }

            public int characterLimit
            {
                get { return m_CharacterLimit; }
                set { m_CharacterLimit = value; }
            }

            public char asteriskChar
            {
                get { return m_AsteriskChar; }
                set { m_AsteriskChar = value; }
            }

            public bool hideMobileInput
            {
                get { return m_HideMobileInput; }
                set { m_HideMobileInput = value; }
            }

            public Dictionary<string, object> extraProperties
            {
                get { return m_extraProperties; }
                set { m_extraProperties = value; }
            }

            public ITextValidator customTextValidator
            {
                get { return m_CustomTextValidator; }
                set { m_CustomTextValidator = value; }
            }

            #endregion

            #region Helper Functions

            public void Apply(MaterialInputField materialInput)
            {
                if (materialInput != null)
                {
                    if (materialInput.inputField is InputField)
                        Apply_Internal(materialInput.inputField as InputField);
                    else if (materialInput.inputField is TMPro.TMP_InputField)
                        Apply_Internal(materialInput.inputField as TMPro.TMP_InputField);
                    else if (materialInput.inputField is InputPromptField)
                        Apply_Internal(materialInput.inputField as InputPromptField);
                    materialInput.hintText = m_HintText;
                    materialInput.customTextValidator = m_CustomTextValidator;

                    //Try Apply Extra Properties
                    if (m_extraProperties != null)
                    {
                        var stylesToApplyDict = materialInput.ExtraStylePropertiesMap;

                        foreach (var pair in stylesToApplyDict)
                        {
                            var key = pair.Key;
                            var style = pair.Value;

                            object data;
                            if (!string.IsNullOrEmpty(key) && m_extraProperties.TryGetValue(key, out data))
                            {
                                StyleUtils.ApplyGraphicData(style.Target, data);
                            }
                        }
                    }
                }
            }

            protected void Apply_Internal(InputPromptField input)
            {
                if (input != null)
                {
                    input.text = m_Text;
                    input.inputType = m_InputType;
                    input.lineType = m_LineType;
                    input.contentType = m_ContentType;
                    input.characterValidation = m_CharacterValidation;
                    input.keyboardType = m_KeyboardType;
                    input.characterLimit = m_CharacterLimit;
                    input.asteriskChar = m_AsteriskChar;
                    input.shouldHideMobileInput = m_HideMobileInput;
                    input.hintText = m_HintText;
                }
            }

            public void Apply_Internal(InputField input)
            {
                if (input != null)
                {
                    input.text = m_Text;
                    input.inputType = m_InputType;
                    input.lineType = m_LineType;
                    input.contentType = m_ContentType;
                    input.characterValidation = m_CharacterValidation;
                    input.keyboardType = m_KeyboardType;
                    input.characterLimit = m_CharacterLimit;
                    input.asteriskChar = m_AsteriskChar;
                    input.shouldHideMobileInput = m_HideMobileInput;
                    if (input.placeholder != null)
                        input.placeholder.SetGraphicText(m_HintText);
                }
            }

            public void Apply_Internal(TMPro.TMP_InputField input)
            {
                if (input != null)
                {
                    input.text = m_Text;
                    input.inputType = (TMPro.TMP_InputField.InputType)Enum.ToObject(typeof(TMPro.TMP_InputField.InputType), (int)m_InputType);
                    input.lineType = (TMPro.TMP_InputField.LineType)Enum.ToObject(typeof(TMPro.TMP_InputField.LineType), (int)m_LineType);
                    input.contentType = (TMPro.TMP_InputField.ContentType)Enum.ToObject(typeof(TMPro.TMP_InputField.ContentType), (int)m_ContentType);
                    input.characterValidation = (TMPro.TMP_InputField.CharacterValidation)Enum.Parse(typeof(TMPro.TMP_InputField.CharacterValidation), Enum.GetName(typeof(InputField.CharacterValidation), m_CharacterValidation));
                    input.keyboardType = m_KeyboardType;
                    input.characterLimit = m_CharacterLimit;
                    input.asteriskChar = m_AsteriskChar;
                    input.shouldHideMobileInput = m_HideMobileInput;
                    if (input.placeholder != null)
                        input.placeholder.SetGraphicText(m_HintText);
                }
            }

            #endregion

            #region Conversors

            public static implicit operator InputFieldConfigData(MaterialInputField materialInput)
            {
                InputFieldConfigData config = new InputFieldConfigData();
                if (materialInput != null)
                {
                    if (materialInput.inputField is InputField)
                        config = materialInput.inputField as InputField;
                    else if (materialInput.inputField is TMPro.TMP_InputField)
                        config = materialInput.inputField as TMPro.TMP_InputField;
                    else if (materialInput.inputField is InputPromptField)
                        config = materialInput.inputField as InputPromptField;

                    config.m_HintText = materialInput.hintText;
                    config.m_CustomTextValidator = materialInput.customTextValidator != null ? materialInput.customTextValidator.Clone() : null;

                    var stylesDict = materialInput.ExtraStylePropertiesMap;

                    config.m_extraProperties = new Dictionary<string, object>();
                    foreach (var pair in stylesDict)
                    {
                        var key = pair.Key;
                        var style = pair.Value;

                        if (!string.IsNullOrEmpty(key) && style != null && style.Target != null)
                        {
                            var data = StyleUtils.GetGraphicData(style.Target);
                            config.m_extraProperties[key] = data;
                        }
                    }
                }

                return config;
            }

            public static implicit operator InputFieldConfigData(InputPromptField input)
            {
                var config = new InputFieldConfigData();
                if (input != null)
                {
                    config.m_Text = input.text;
                    config.m_InputType = input.inputType;
                    config.m_LineType = input.lineType;
                    config.m_ContentType = input.contentType;
                    config.m_CharacterValidation = input.characterValidation;
                    config.m_KeyboardType = input.keyboardType;
                    config.m_CharacterLimit = input.characterLimit;
                    config.m_AsteriskChar = input.asteriskChar;
                    config.m_HideMobileInput = input.shouldHideMobileInput;
                    config.m_HintText = input.hintText;
                }

                return config;
            }

            public static implicit operator InputFieldConfigData(InputField input)
            {
                var config = new InputFieldConfigData();
                if (input != null)
                {
                    config.m_Text = input.text;
                    config.m_InputType = input.inputType;
                    config.m_LineType = input.lineType;
                    config.m_ContentType = input.contentType;
                    config.m_CharacterValidation = input.characterValidation;
                    config.m_KeyboardType = input.keyboardType;
                    config.m_CharacterLimit = input.characterLimit;
                    config.m_AsteriskChar = input.asteriskChar;
                    config.m_HideMobileInput = input.shouldHideMobileInput;
                    config.m_HintText = input.placeholder != null ? input.placeholder.GetGraphicText() : "";
                }

                return config;
            }

            public static implicit operator InputFieldConfigData(TMPro.TMP_InputField input)
            {
                var config = new InputFieldConfigData();
                if (input != null)
                {
                    config.m_Text = input.text;
                    config.m_InputType = (InputField.InputType)Enum.ToObject(typeof(InputField.InputType), (int)input.inputType);
                    config.m_LineType = (InputField.LineType)Enum.ToObject(typeof(InputField.LineType), (int)input.lineType);
                    config.m_ContentType = (InputField.ContentType)Enum.ToObject(typeof(InputField.ContentType), (int)input.contentType);
                    config.m_CharacterValidation = (InputField.CharacterValidation)Enum.Parse(typeof(InputField.CharacterValidation), Enum.GetName(typeof(TMPro.TMP_InputField.CharacterValidation), input.characterValidation));
                    config.m_KeyboardType = input.keyboardType;
                    config.m_CharacterLimit = input.characterLimit;
                    config.m_AsteriskChar = input.asteriskChar;
                    config.m_HideMobileInput = input.shouldHideMobileInput;
                    config.m_HintText = input.placeholder != null ? input.placeholder.GetGraphicText() : "";
                }

                return config;
            }

            #endregion
        }

        #endregion
    }
}