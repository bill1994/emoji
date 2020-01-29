//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Kyub.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Checkbox Option", 100)]
    public class DialogCheckboxOption : DialogClickableOption
    {
        #region Private Variables

        [SerializeField]
		private Graphic m_ItemText = null;
        [SerializeField]
        private Graphic m_ItemIcon = null;
        [SerializeField]
		private ToggleBase m_ItemCheckbox = null;

        #endregion

        #region Public Properties

        public Graphic itemText
        {
            get { return m_ItemText; }
        }

        public Graphic itemIcon
        {
            get { return m_ItemIcon; }
        }

        public ToggleBase itemCheckbox
        {
            get { return m_ItemCheckbox; }
        }

        public RectTransform rectTransform
        {
            get
            {
                return transform as RectTransform;
            }
        }

        #endregion

        #region Reload Functions

        protected override void ApplyReload(ScrollDataView.ReloadEventArgs oldArgs, ScrollDataView.ReloadEventArgs newArgs)
        {
            BaseDialogList dialog = DataView != null? DataView.GetComponentInParent<BaseDialogList>() : null;
            if (dialog != null)
            {
                OptionData option = Data as OptionData;
                if (m_ItemText != null)
                    m_ItemText.SetGraphicText(option != null ? option.text : "");
                if (m_ItemIcon != null)
                {
                    m_ItemIcon.SetImageData(option != null ? option.imageData : null);
                    m_ItemIcon.gameObject.SetActive(m_ItemIcon.GetImageData() != null && m_ItemIcon.GetImageData().ContainsData(true));
                }
                if (m_ItemCheckbox != null)
                    m_ItemCheckbox.isOn = dialog.IsDataIndexSelected(DataIndex);
            }
        }

        #endregion
    }
}