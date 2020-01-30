//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace MaterialUI
{
	[AddComponentMenu("MaterialUI/Dialogs/Time Picker", 1)]
	public class DialogTimePicker : MaterialDialogCompat
	{
        #region Private Variables

        [SerializeField, SerializeStyleProperty]
		private Image m_ClockNeedle = null;
        [SerializeField]
        private Graphic[] m_ClockTextArray = null;

        [SerializeField, SerializeStyleProperty]
        private Graphic m_TextAM = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_TextPM = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_TextHours = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_TextMinutes = null;

        [SerializeField, SerializeStyleProperty]
        private Image m_Header = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_NeedleCenter = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_NeedleEnd = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_MaskedText = null;

        [SerializeField]
        private MaterialButton m_AffirmativeButton = null;
        [SerializeField]
        private MaterialButton m_DismissiveButton = null;

        [SerializeField]
        private DialogTimePickerClock m_timePickerClock = null;

        private int m_CurrentHour;
        private int m_CurrentMinute;
        private bool m_IsAM;
        private bool m_IsHoursSelected;

        private float m_NeedleAngle;

        private Graphic m_ClosestText;

        #endregion

        #region Callbacks

        private Action<DateTime> m_OnAffirmativeClicked;

        #endregion

        #region Properties

        public Action<DateTime> onAffirmativeClicked
        {
            get { return m_OnAffirmativeClicked; }
            set { m_OnAffirmativeClicked = value; }
        }

        public MaterialButton affirmativeButton
        {
            get { return m_AffirmativeButton; }
            set { m_AffirmativeButton = value; }
        }

        public MaterialButton dismissiveButton
        {
            get { return m_DismissiveButton; }
            set { m_DismissiveButton = value; }
        }

        public Image clockNeedle
        {
            get { return m_ClockNeedle; }
        }

        public Graphic[] clockTextArray
        {
            get { return m_ClockTextArray; }
        }

        public Graphic textAM
        {
            get { return m_TextAM; }
        }

        public Graphic textPM
        {
            get { return m_TextPM; }
        }

        public Graphic textHours
        {
            get { return m_TextHours; }
        }

        public Graphic textMinutes
        {
            get { return m_TextMinutes; }
        }

        public int currentHour
        {
            get { return m_CurrentHour; }
            set
            {
                SetHours(value);
            }
        }

        public int currentMinute
        {
            get { return m_CurrentMinute; }
            set
            {
                SetMinutes(value);
            }
        }

        public bool isAM
        {
            get { return m_IsAM; }
            set
            {
                if (value)
                {
                    OnAMClicked();
                }
                else
                {
                    OnPMClicked();
                }
            }
        }

        public bool isHoursSelected
        {
            get { return m_IsHoursSelected; }
            set
            {
                if (value)
                {
                    OnHoursClicked();
                }
                else
                {
                    OnMinutesClicked();
                }
            }
        }

        public Image header
		{
			get { return m_Header; }
			set { m_Header = value; }
		}

		public Graphic needleCenter
		{
			get { return m_NeedleCenter; }
			set { m_NeedleCenter = value; }
		}

		public Graphic needleEnd
		{
			get { return m_NeedleEnd; }
			set { m_NeedleEnd = value; }
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
                MaterialActivity.AddEventListener(v_affirmativeTrigger.OnCallTrigger, OnButtonOkClicked);

                var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_cancelTrigger.Name = "Escape KeyDown";
                v_cancelTrigger.Key = KeyCode.Escape;
                v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, Hide);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_affirmativeTrigger, v_cancelTrigger };
            }
        }

        public void Initialize(DateTime time, Action<DateTime> onAffirmativeClicked, Color accentColor)
		{
			this.currentHour = time.Hour % 12;
			this.currentMinute = time.Minute;
			this.isAM = time.Hour >= 12 ? false : true;
			this.onAffirmativeClicked = onAffirmativeClicked;

			Vector2 initialSize = rectTransform.sizeDelta;
			//Initialize();
			rectTransform.sizeDelta = initialSize;

			SetColor(accentColor);
		}

		public override void Show()
		{
            if (onShow != null)
                onShow.AddListener(InitTimePickerClock);
			base.Show();
		}

        public override void Hide()
        {
            if(onShow != null)
                onShow.RemoveListener(InitTimePickerClock);
            base.Hide();
        }

        protected void InitTimePickerClock()
        {
            if(m_timePickerClock != null)
                m_timePickerClock.Init();
        }

        public void SetColor(Color accentColor)
		{
			m_ClockNeedle.color = accentColor;
			m_NeedleCenter.color = accentColor;
			m_NeedleEnd.color = accentColor;
			m_Header.color = accentColor;
			m_AffirmativeButton.materialRipple.rippleData.Color = accentColor;
			m_DismissiveButton.materialRipple.rippleData.Color = accentColor;
		}

		void Update()
		{
			m_ClockNeedle.transform.localRotation = Quaternion.Slerp(m_ClockNeedle.transform.localRotation, Quaternion.Euler(new Vector3(0, 0, m_NeedleAngle)), Time.deltaTime * 20f);

			int hour = GetHourFromAngle(m_ClockNeedle.transform.eulerAngles.z) - 1;
			if (hour == -1)
			{
				hour = 11;
			}
			m_ClosestText = m_ClockTextArray[hour];
			m_MaskedText.transform.position = m_ClosestText.transform.position;
			m_MaskedText.transform.rotation = m_ClosestText.transform.rotation;
            m_MaskedText.SetGraphicText(m_ClosestText.GetGraphicText());
		}

		private float GetAngleFromHour(int hour)
		{
			return -m_CurrentHour * 30 + 90;
		}

		private int GetHourFromAngle(float angle)
		{
			float approximateHour = -(angle - 90) / 30;
			int hour = Mathf.RoundToInt(approximateHour);

			if (hour < 0) hour += 12;
			return hour;
		}

		private float GetAngleFromMinute(int minute)
		{
			return -m_CurrentMinute / 5.0f * 30 + 90;
		}

		private int GetMinuteFromAngle(float angle)
		{
			float approximateMinute = -(angle - 90) / 30 * 5.0f;
			int minute = Mathf.RoundToInt(approximateMinute);

			if (minute < 0) minute += 60;
			return minute;
		}

		public void SetAngle(float angle)
		{
			if (m_IsHoursSelected)
			{
				SetHours(GetHourFromAngle(angle));
			}
			else
			{
				SetMinutes(GetMinuteFromAngle(angle));
			}
		}

		private void SetHours(int hour, bool updateClock = true)
		{
			if (hour <= 0) hour = 12;
			if (hour > 12) hour = 12;

			m_CurrentHour = hour;
            textHours.SetGraphicText(hour.ToString("0"));

			if (updateClock)
			{
				SelectHours();
				UpdateNeedle();
			}
		}

		private void SetMinutes(int minute, bool updateClock = true)
		{
			if (minute < 0) minute = 0;
			if (minute > 60) minute = 60;

			m_CurrentMinute = minute;
            m_TextMinutes.SetGraphicText(minute.ToString("00"));

			if (updateClock)
			{
				SelectMinutes();
				UpdateNeedle();
			}
		}

		public void SetTime(int hour, int minute)
		{
			SetHours(hour);
			SetMinutes(minute);
		}

		private void UpdateNeedle()
		{
			float rotation = m_IsHoursSelected ? GetAngleFromHour(m_CurrentHour) : GetAngleFromMinute(m_CurrentMinute);
			UpdateNeedleAngle(rotation);
		}

		private void UpdateNeedleAngle(float angle)
		{
			m_NeedleAngle = angle;
		}

		private void UpdateClockTextArray()
		{
			for (int i = 0; i < m_ClockTextArray.Length; i++)
			{
				if (m_IsHoursSelected)
				{
					int number = (i + 1);
                    m_ClockTextArray[i].SetGraphicText(number.ToString("0"));
				}
				else
				{
					int number = (i + 1) * 5;
					number = number % 60;
                    m_ClockTextArray[i].SetGraphicText(number.ToString("00"));
				}
			}

			UpdateNeedle();
		}

		public void OnAMClicked()
		{
			m_IsAM = true;
			m_TextAM.color = new Color(m_TextAM.color.r, m_TextAM.color.g, m_TextAM.color.b, 1.0f);
			textPM.color = new Color(textPM.color.r, textPM.color.g, textPM.color.b, 0.5f);
		}

		public void OnPMClicked()
		{
			m_IsAM = false;
			textPM.color = new Color(textPM.color.r, textPM.color.g, textPM.color.b, 1.0f);
			m_TextAM.color = new Color(m_TextAM.color.r, m_TextAM.color.g, m_TextAM.color.b, 0.5f);
		}

		public void OnHoursClicked()
		{
			SelectHours();
		}

		private void SelectHours()
		{
			textHours.color = new Color(textHours.color.r, textHours.color.g, textHours.color.b, 1.0f);
			m_TextMinutes.color = new Color(m_TextMinutes.color.r, m_TextMinutes.color.g, m_TextMinutes.color.b, 0.5f);

			m_IsHoursSelected = true;
			UpdateClockTextArray();
		}

		public void OnMinutesClicked()
		{
			SelectMinutes();
		}

		private void SelectMinutes()
		{
			m_TextMinutes.color = new Color(m_TextMinutes.color.r, m_TextMinutes.color.g, m_TextMinutes.color.b, 1.0f);
			textHours.color = new Color(textHours.color.r, textHours.color.g, textHours.color.b, 0.5f);

			m_IsHoursSelected = false;
			UpdateClockTextArray();
		}

		public void OnButtonOkClicked()
		{
			if (m_OnAffirmativeClicked != null)
			{
				DateTime date = DateTime.MinValue.AddHours(m_CurrentHour).AddMinutes(m_CurrentMinute);
				if (!m_IsAM && m_CurrentHour == 12) { } // If it's both PM and 12, we do nothing
				else if (!m_IsAM)
				{
					date = date.AddHours(12);
				}
				else if (m_CurrentHour == 12)
				{
					date = date.AddHours(-12);
				}

				m_OnAffirmativeClicked(date);
			}

			Hide();
		}

        #endregion
    }
}