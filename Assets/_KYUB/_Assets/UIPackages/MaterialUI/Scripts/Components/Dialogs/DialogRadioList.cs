//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Radio List", 1)]
    public class DialogRadioList : MaterialDialogCompat
    {
        [SerializeField]
        private DialogTitleSection m_TitleSection = null;
        public DialogTitleSection titleSection
        {
            get { return m_TitleSection; }
            set { m_TitleSection = value; }
        }

        [SerializeField]
        private DialogButtonSection m_ButtonSection = null;
        public DialogButtonSection buttonSection
        {
            get { return m_ButtonSection; }
            set { m_ButtonSection = value; }
        }

        [SerializeField]
        private MaterialToggleGroup m_ToggleGroup = null;
        public MaterialToggleGroup toggleGroup
        {
            get { return m_ToggleGroup; }
            set { m_ToggleGroup = value; }
        }

        private List<DialogCheckboxOption> m_SelectionItems;
        public List<DialogCheckboxOption> selectionItems
        {
            get { return m_SelectionItems; }
            protected set { m_SelectionItems = value; }
        }

		private int m_SelectedIndex;
        public int selectedIndex
        {
            get { return m_SelectedIndex; }
            protected set { m_SelectedIndex = value; }
        }

        private string[] m_OptionList;
        public string[] optionList
        {
            get { return m_OptionList; }
            set { m_OptionList = value; }
        }

		private Action<int> m_OnAffirmativeButtonClicked;
		public Action<int> onAffirmativeButtonClicked
		{
			get { return m_OnAffirmativeButtonClicked; }
			set { m_OnAffirmativeButtonClicked = value; }
		}

        private Action<int> m_onItemClick;
        public Action<int> onItemClick
        {
            get { return m_onItemClick; }
            set { m_onItemClick = value; }
        }

        [SerializeField]
        private GameObject m_OptionTemplate = null;
        protected GameObject optionTemplate
        {
            get { return m_OptionTemplate; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OverscrollConfig overscrollConfig = GetComponentInChildren<OverscrollConfig>();

            if (overscrollConfig != null)
            {
                overscrollConfig.Setup();
            }
        }

		public virtual void Initialize(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart)
        {
            ClearRadioList();

            m_OptionList = options;
            m_SelectionItems = new List<DialogCheckboxOption>();
            
            for (int i = 0; i < m_OptionList.Length; i++)
            {
                m_SelectionItems.Add(CreateSelectionItem(i));
            }

			if (selectedIndexStart < 0) selectedIndexStart = 0;
			if (selectedIndexStart >= m_SelectionItems.Count) selectedIndexStart = m_SelectionItems.Count - 1;
			m_SelectionItems[selectedIndexStart].itemCheckbox.toggle.isOn = true;
			m_SelectedIndex = selectedIndexStart;

            m_OptionTemplate.SetActive(false);

            if(m_TitleSection != null)
                m_TitleSection.SetTitle(titleText, icon);

            if (m_ButtonSection != null)
            {
                m_ButtonSection.SetButtons(null, affirmativeButtonText, onDismissiveButtonClicked, dismissiveButtonText);
                m_ButtonSection.SetupButtonLayout(rectTransform);
            }

			m_OnAffirmativeButtonClicked = onAffirmativeButtonClicked;

            float availableHeight = DialogManager.rectTransform.rect.height;

            if (m_TitleSection != null && m_TitleSection.text != null)
            {
                LayoutGroup textAreaRectTransform = m_TitleSection.text.transform.parent.GetComponent<LayoutGroup>();

                if (textAreaRectTransform.gameObject.activeSelf)
                {
                    textAreaRectTransform.CalculateLayoutInputVertical();
                    availableHeight -= textAreaRectTransform.preferredHeight;
                }
            }

            /*if(AutoSize && m_ListScrollLayoutElement != null)
            {
                m_ListScrollLayoutElement.maxHeight = availableHeight - 98f;
            }*/

            //Initialize();
        }

        protected virtual DialogCheckboxOption CreateSelectionItem(int i)
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

            option.itemCheckbox.group = m_ToggleGroup;
            option.itemCheckbox.isOn = false;
            option.gameObject.SetActive(true);

            return option;
        }

        public virtual void OnItemClick(int index)
        {
            Toggle toggle = m_SelectionItems[index].itemCheckbox.toggle;
            toggle.isOn = !toggle.isOn;
            m_SelectedIndex = index;

            if(onItemClick != null)
            {
                onItemClick(index);
            }
        }

        public virtual void AffirmativeButtonClicked()
        {
			m_OnAffirmativeButtonClicked.InvokeIfNotNull(m_SelectedIndex);
            Hide();
        }

        public virtual void DismissiveButtonClicked()
        {
            if (m_ButtonSection != null)
            {
                m_ButtonSection.OnDismissiveButtonClicked();
            }

            Hide();
		}

        public virtual void ClearRadioList()
        {
            if (m_SelectionItems == null) return;

            foreach(DialogCheckboxOption option in m_SelectionItems)
            {
                Destroy(option.gameObject);
            }

            m_SelectionItems.Clear();
        }

		/*void Update()
		{
			if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
			{
				AffirmativeButtonClicked();
			}
			else if (Input.GetKeyDown(KeyCode.Escape))
			{
				DismissiveButtonClicked();
			}
		}*/
    }
}