//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;
using System;

namespace MaterialUI
{
	[AddComponentMenu("MaterialUI/Dialogs/Day Picker Item", 100)]
	public class DialogDatePickerDayItem : MonoBehaviour
	{
        #region Private Variables

        [SerializeField]
		private Graphic m_Text = null;
        [SerializeField]
        private Toggle m_Toggle = null;
        [SerializeField]
        private Graphic m_SelectedImage = null;

		private DateTime m_DateTime;
        private Color m_ToggleOnColor = Color.white;
        private Color m_ToggleOffColor = Color.black;


        #endregion

        #region Public Properties

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

        public DateTime dateTime
        {
            get { return m_DateTime; }
        }

        public Color ToggleOnColor
        {
            get { return m_ToggleOnColor; }
            set { m_ToggleOnColor = value; }
        }

        public Color ToggleOffColor
        {
            get { return m_ToggleOffColor; }
            set { m_ToggleOffColor = value; }
        }

        #endregion

        #region Callbacks

        private Action<DialogDatePickerDayItem, bool> m_OnItemValueChanged;

        #endregion

        #region Helper Functions

        public void Init(DateTime dateTime, Action<DialogDatePickerDayItem, bool> onItemValueChanged)
		{
			m_DateTime = dateTime;
			m_OnItemValueChanged = onItemValueChanged;

			transform.localScale = Vector3.one;

			m_Text.SetGraphicText(m_DateTime.Day.ToString());
		}

		public void UpdateState(DateTime currentDate)
		{
			bool isCurrentMonth = (m_DateTime.Month == currentDate.Month) && !m_DateTime.Equals(default(DateTime));

			toggle.interactable = isCurrentMonth;
			m_Text.gameObject.SetActive(isCurrentMonth);

			if (!isCurrentMonth)
			{
				return;
			}

			bool isToday = m_DateTime.Day == DateTime.Now.Day && m_DateTime.Month == DateTime.Now.Month && m_DateTime.Year == DateTime.Now.Year;
            m_Text.SetGraphicFontStyle(isToday ? FontStyle.Bold : FontStyle.Normal); //TODO: Do not use the unity normal/bold fontStyle, but apply the correct Material font...

            bool toggleState = m_DateTime.Equals(currentDate);

            if (toggleState != toggle.isOn)
			{
				toggle.isOn = toggleState;
                Kyub.Performance.SustainedPerformanceManager.Refresh(this);
			}
        }

		public void OnItemValueChange()
		{
			m_Text.color = toggle.isOn ? m_ToggleOnColor : m_ToggleOffColor;
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);

            if (m_OnItemValueChanged != null)
			{
				m_OnItemValueChanged(this, toggle.isOn);
			}
		}

        #endregion
    }
}