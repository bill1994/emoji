//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Simple Option", 100)]
    public class DialogSimpleOption : DialogClickableOption
    {
        #region Private Variables

        [SerializeField]
        private Graphic m_ItemText = null;
        [SerializeField]
        private MaterialRipple m_ItemRipple = null;
        private RectTransform m_RectTransform;

        private Graphic m_ItemIcon;

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
                if (m_RectTransform == null)
                {
                    m_RectTransform = transform as RectTransform;
                }

                return m_RectTransform;
            }
        }

        #endregion
    }
}