//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dropdown Trigger", 100)]
    public class DropdownTrigger : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        #region Private Variables

        [SerializeField]
        private MaterialDropdown m_Dropdown = null;
        [SerializeField]
        private int m_Index = 0;

        #endregion

        #region Public Properties

        public MaterialDropdown dropdown
        {
            get { return m_Dropdown; }
            set { m_Dropdown = value; }
        }

        public int index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }

        #endregion

        #region Unity Functions

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dropdown != null)
            {
                dropdown.Select(m_Index);
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (dropdown != null)
            {
                dropdown.Select(m_Index, true);
            }
        }

        #endregion
    }
}