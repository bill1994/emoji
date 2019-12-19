//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Checkbox List", 1)]
    public class DialogCheckboxList : MaterialDialogCompat
    {
        #region Helper Classes

        public delegate void OptionSelectedEvent(int i);

        #endregion

        #region Private Variables

        [SerializeField]
        private DialogTitleSection m_TitleSection = null;
        [SerializeField]
        private DialogButtonSection m_ButtonSection = null;
        [SerializeField]
        private GameObject m_OptionTemplate = null;

        private List<DialogCheckboxOption> m_SelectionItems;
        private bool[] m_SelectedIndexes;
        private string[] m_OptionList;

        #endregion

        #region Callbacks

        private Action<bool[]> m_OnAffirmativeButtonClicked;
        public OptionSelectedEvent onOptionSelected;

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

        public List<DialogCheckboxOption> selectionItems
        {
            get { return m_SelectionItems; }
        }

        public bool[] selectedIndexes
        {
            get { return m_SelectedIndexes; }
            set { m_SelectedIndexes = value; }
        }

        public string[] optionList
        {
            get { return m_OptionList; }
            set { m_OptionList = value; }
        }

        public Action<bool[]> onAffirmativeButtonClicked
        {
            get { return m_OnAffirmativeButtonClicked; }
            set { m_OnAffirmativeButtonClicked = value; }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
		{
            base.OnEnable();
            OverscrollConfig scrollConfig = GetComponentInChildren<OverscrollConfig>();

            if(scrollConfig != null)
            {
                scrollConfig.Setup();
            }
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
                MaterialActivity.AddEventListener(v_affirmativeTrigger.OnCallTrigger, AffirmativeButtonClicked);

                var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_cancelTrigger.Name = "Escape KeyDown";
                v_cancelTrigger.Key = KeyCode.Escape;
                v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, DismissiveButtonClicked);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_affirmativeTrigger, v_cancelTrigger };
            }
        }

        public void Initialize(string[] options, Action<bool[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            m_OptionList = options;
            m_SelectionItems = new List<DialogCheckboxOption>();
            m_SelectedIndexes = new bool[options.Length];

            for (int i = 0; i < m_OptionList.Length; i++)
            {
                m_SelectionItems.Add(CreateSelectionItem(i));
            }

            Destroy(m_OptionTemplate);

            m_TitleSection.SetTitle(titleText, icon);
			m_ButtonSection.SetButtons(null, affirmativeButtonText, onDismissiveButtonClicked, dismissiveButtonText);
            m_ButtonSection.SetupButtonLayout(rectTransform);

			m_OnAffirmativeButtonClicked = onAffirmativeButtonClicked;

            float availableHeight = DialogManager.rectTransform.rect.height;

            LayoutGroup textAreaRectTransform = m_TitleSection.text.transform.parent.GetComponent<LayoutGroup>();

            if (textAreaRectTransform.gameObject.activeSelf)
            {
                textAreaRectTransform.CalculateLayoutInputVertical();
                availableHeight -= textAreaRectTransform.preferredHeight;
            }

            //Initialize();
        }

        private DialogCheckboxOption CreateSelectionItem(int i)
        {
            DialogCheckboxOption option = Instantiate(m_OptionTemplate).GetComponent<DialogCheckboxOption>();
            option.rectTransform.SetParent(m_OptionTemplate.transform.parent);
            option.rectTransform.localScale = Vector3.one;
            option.rectTransform.localEulerAngles = Vector3.zero;
            option.rectTransform.localPosition = new Vector3(option.rectTransform.localPosition.x, option.rectTransform.localPosition.y, 0f);

            Graphic text = option.itemText;

            text.SetGraphicText(m_OptionList[i]);

            option.index = i;
            option.onClickAction += OnItemClick;

            return option;
        }

        public void OnItemClick(int index)
        {
            Toggle toggle = m_SelectionItems[index].itemCheckbox.toggle;
            toggle.isOn = !toggle.isOn;

            m_SelectedIndexes[index] = toggle.isOn;
        }

        public void AffirmativeButtonClicked()
        {
			m_OnAffirmativeButtonClicked.InvokeIfNotNull(m_SelectedIndexes);
            Hide();
        }

        public void DismissiveButtonClicked()
        {
            m_ButtonSection.OnDismissiveButtonClicked();
            Hide();
		}

        #endregion
    }
}