//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

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
		private ToggleBase m_ItemCheckbox = null;
        
		private RectTransform m_RectTransform;

        #endregion

        #region Public Properties

        public Graphic itemText
        {
            get { return m_ItemText; }
        }

        public ToggleBase itemCheckbox
        {
            get { return m_ItemCheckbox; }
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