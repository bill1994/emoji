// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.UI;
using System;

namespace MaterialUI
{
	[AddComponentMenu("MaterialUI/Dialogs/Month Picker Item", 100)]
	public class DialogDatePickerMonthItem : MonoBehaviour
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

        private Action<DialogDatePickerMonthItem, bool> m_OnItemValueChanged;

        #endregion

        #region Helper Functions

        public void Init(DateTime dateTime, System.Globalization.CultureInfo currentCulture, Action<DialogDatePickerMonthItem, bool> onItemValueChanged)
		{
			m_DateTime = dateTime;
			m_OnItemValueChanged = onItemValueChanged;

			transform.localScale = Vector3.one;

            var monthName = m_DateTime.ToString("MMM", currentCulture);
            if (monthName.Length > 1)
                monthName = char.ToUpper(monthName[0]) + monthName.Substring(1);

            m_Text.SetGraphicText(monthName);
		}

		public void UpdateState(DateTime currentDate)
		{
			bool isCurrentMonth = (m_DateTime.Month == currentDate.Month) && !m_DateTime.Equals(default(DateTime));

            m_Text.SetGraphicFontStyle(isCurrentMonth && m_DateTime.Year == currentDate.Year ? FontStyle.Bold : FontStyle.Normal); //TODO: Do not use the unity normal/bold fontStyle, but apply the correct Material font...

            bool toggleState = m_DateTime.Month == currentDate.Month && m_DateTime.Year == currentDate.Year;

            // Only sets if true, so the toggle group handles the rest
            if ((!toggle.group || toggleState) && toggleState != toggle.isOn)
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