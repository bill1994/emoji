//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Kyub.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Simple Option", 100)]
    public class DialogSimpleOption : DialogClickableOption
    {
        #region Private Variables

        [SerializeField]
        private Graphic m_ItemIcon = null;
        [SerializeField]
        private Graphic m_ItemText = null;
        [SerializeField]
        private MaterialRipple m_ItemRipple = null;

        #endregion

        #region Public Properties

        public Graphic itemText
        {
            get { return m_ItemText; }
            set { m_ItemText = value; }
        }

        public Graphic itemIcon
        {
            get { return m_ItemIcon; }
            set { m_ItemIcon = value; }
        }

        public MaterialRipple itemRipple
        {
            get { return m_ItemRipple; }
            set { m_ItemRipple = value; }
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
            BaseDialogList dialog = DataView != null ? DataView.GetComponentInParent<BaseDialogList>() : null;
            if (dialog != null)
            {
                OptionData option = Data as OptionData;
                if (m_ItemText != null)
                    m_ItemText.SetGraphicText(option != null ? option.text : "");
                if (m_ItemIcon != null)
                {
                    m_ItemIcon.SetImage(option != null ? option.imageData : null);
                    m_ItemIcon.gameObject.SetActive(m_ItemIcon.GetImageData() != null && m_ItemIcon.GetImageData().ContainsData(true));
                }
            }
        }

        #endregion
    }
}