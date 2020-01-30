//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Kyub.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Date Picker Item", 100)]
    public class DialogDatePickerYearItem : DialogClickableOption
    {
        #region Private Variables

        [SerializeField]
        private Graphic m_Text = null;
		[SerializeField]
		private Toggle m_Toggle = null;
		[SerializeField]
		private Graphic m_SelectedImage = null;
		
		private int m_Year;

        #endregion

        #region Public Properties

        public virtual int index { get; set; }

        public Graphic text
        {
            get { return m_Text; }
        }

        public Toggle toggle
        {
            get { return m_Toggle; }
        }

        public Graphic selectedImage
        {
            get { return m_SelectedImage; }
        }

        public int year
        {
            get { return m_Year; }
            set
            {
                m_Year = value;
                text.SetGraphicText(m_Year.ToString());
            }
        }

        #endregion

        #region Helper Functions

        public void UpdateState(int currentYear)
        {
            var toggleState = year == currentYear;
            if (toggleState != toggle.isOn)
            {
                toggle.isOn = toggleState;
                Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            }
        }

        public void OnItemValueChange()
        {
            m_Text.color = toggle.isOn ? Color.white : Color.black;
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
        }

        #endregion
    }
}