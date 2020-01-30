//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
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

        protected override void ValidateKeyTriggers(MaterialFocusGroup p_materialKeyFocus)
        {
            if (p_materialKeyFocus != null)
            {
                var v_affirmativeTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_affirmativeTrigger.Name = "Return KeyDown";
                v_affirmativeTrigger.Key = KeyCode.Return;
                v_affirmativeTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_affirmativeTrigger.OnCallTrigger, AffirmativeButtonClickedConditional);

                var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_cancelTrigger.Name = "Escape KeyDown";
                v_cancelTrigger.Key = KeyCode.Escape;
                v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, DismissiveButtonClicked);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_affirmativeTrigger, v_cancelTrigger };
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
			m_TitleSection.SetTitle(titleText, icon);
			m_ButtonSection.SetButtons(null, affirmativeButtonText, onDismissiveButtonClicked, dismissiveButtonText);
			m_ButtonSection.SetupButtonLayout(rectTransform);

            if (m_FirstInputField)
            {
                if (firstFieldConfig != null)
                    firstFieldConfig.Apply(m_FirstInputField);
                //m_FirstInputField.customTextValidator = new EmptyTextValidator();
            }
            if (m_SecondInputField)
            {
                if (secondFieldConfig != null)
                    secondFieldConfig.Apply(m_SecondInputField);
                //m_SecondInputField.customTextValidator = new EmptyTextValidator();

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
                    input.characterValidation = (TMPro.TMP_InputField.CharacterValidation)Enum.Parse(typeof(TMPro.TMP_InputField.CharacterValidation), Enum.GetName(typeof(InputField.CharacterValidation), m_ContentType));
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
                InputFieldConfigData v_config = new InputFieldConfigData();
                if (materialInput != null)
                {
                    if (materialInput.inputField is InputField)
                        v_config = materialInput.inputField as InputField;
                    else if (materialInput.inputField is TMPro.TMP_InputField)
                        v_config =  materialInput.inputField as TMPro.TMP_InputField;
                    else if (materialInput.inputField is InputPromptField)
                        v_config =  materialInput.inputField as InputPromptField;

                    v_config.m_HintText = materialInput.hintText;
                }

                return v_config;
            }

            public static implicit operator InputFieldConfigData(InputPromptField input)
            {
                var v_config = new InputFieldConfigData();
                if (input != null)
                {
                    v_config.m_Text = input.text;
                    v_config.m_InputType = input.inputType;
                    v_config.m_LineType = input.lineType;
                    v_config.m_ContentType = input.contentType;
                    v_config.m_CharacterValidation = input.characterValidation;
                    v_config.m_KeyboardType = input.keyboardType;
                    v_config.m_CharacterLimit = input.characterLimit;
                    v_config.m_AsteriskChar = input.asteriskChar;
                    v_config.m_HideMobileInput = input.shouldHideMobileInput;
                    v_config.m_HintText = input.hintText;
                }

                return v_config;
            }

            public static implicit operator InputFieldConfigData(InputField input)
            {
                var v_config = new InputFieldConfigData();
                if (input != null)
                {
                    v_config.m_Text = input.text;
                    v_config.m_InputType = input.inputType;
                    v_config.m_LineType = input.lineType;
                    v_config.m_ContentType = input.contentType;
                    v_config.m_CharacterValidation = input.characterValidation;
                    v_config.m_KeyboardType = input.keyboardType;
                    v_config.m_CharacterLimit = input.characterLimit;
                    v_config.m_AsteriskChar = input.asteriskChar;
                    v_config.m_HideMobileInput = input.shouldHideMobileInput;
                    v_config.m_HintText = input.placeholder != null? input.placeholder.GetGraphicText() : "";
                }

                return v_config;
            }

            public static implicit operator InputFieldConfigData(TMPro.TMP_InputField input)
            {
                var v_config = new InputFieldConfigData();
                if (input != null)
                {
                    v_config.m_Text = input.text;
                    v_config.m_InputType = (InputField.InputType)Enum.ToObject(typeof(InputField.InputType), (int)input.inputType);
                    v_config.m_LineType = (InputField.LineType)Enum.ToObject(typeof(InputField.LineType), (int)input.lineType);
                    v_config.m_ContentType = (InputField.ContentType)Enum.ToObject(typeof(InputField.ContentType), (int)input.contentType);
                    v_config.m_CharacterValidation = (InputField.CharacterValidation)Enum.Parse(typeof(InputField.CharacterValidation), Enum.GetName(typeof(TMPro.TMP_InputField.CharacterValidation), input.characterValidation));
                    v_config.m_KeyboardType = input.keyboardType;
                    v_config.m_CharacterLimit = input.characterLimit;
                    v_config.m_AsteriskChar = input.asteriskChar;
                    v_config.m_HideMobileInput = input.shouldHideMobileInput;
                    v_config.m_HintText = input.placeholder != null ? input.placeholder.GetGraphicText() : "";
                }

                return v_config;
            }

            #endregion
        }

        #endregion
    }
}