//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Date Picker", 1)]
    public class DialogDatePicker : MaterialDialogCompat
    {
        #region Private Variables

        [SerializeField, SerializeStyleProperty]
        private Graphic m_TextDate = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_TextMonth = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_TextYear = null;
        [SerializeField, SerializeStyleProperty]
        private Image m_Header = null;

        [SerializeField]
        private CanvasGroup m_CalendarCanvasGroup = null;
        [SerializeField]
        private CanvasGroup m_YearCanvasGroup = null;
        [SerializeField]
		private DialogDatePickerYearList m_DatePickerYearList = null;

        [Space]
		[SerializeField]
		private Graphic[] m_DatePickerDayTexts = null;
        [SerializeField]
        protected DialogDatePickerDayItem[] m_DatePickerDayItems = null;

        [Space]
        [SerializeField]
        protected DialogDatePickerMonthItem[] m_DatePickerMonthItems = null;

        [SerializeField]
        private string m_DateFormatPattern = "ddd, MMM dd";

        private DateTime m_CurrentDate;

        private DayOfWeek m_DayOfWeek = DayOfWeek.Sunday;

        private CultureInfo m_CultureInfo;

        private Action<DateTime> m_OnAffirmativeClicked;
		private Action m_OnDismissiveClicked;

		private Vector2 m_InitialSize;

        #endregion

        #region Public Properties

        public Graphic textDate
        {
            get { return m_TextDate; }
        }

        public Graphic textMonth
        {
            get { return m_TextMonth; }
        }

        public Graphic textYear
        {
            get { return m_TextYear; }
        }

        public Image header
        {
            get { return m_Header; }
            set { m_Header = value; }
        }

        public DateTime currentDate
        {
            get { return m_CurrentDate; }
            set
            {
                SetDate(value);
            }
        }

        public DayOfWeek dayOfWeek
        {
            get { return m_DayOfWeek; }
        }

        public CultureInfo cultureInfo
        {
            get { return m_CultureInfo; }
        }

        public string dateFormatPattern
        {
            get { return m_DateFormatPattern; }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
		{
            base.Awake();
			m_InitialSize = rectTransform.sizeDelta;
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
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, OnButtonCancelClicked);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_affirmativeTrigger, v_cancelTrigger };
            }
        }

        public void Initialize(int year, int month, int day, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor)
        {
            SetDate(new DateTime(year, month, day));
            OnDateClicked();

            // Callbacks
            m_OnAffirmativeClicked = onAffirmativeClicked;
			m_OnDismissiveClicked = onDismissiveClicked;

            SetColor(accentColor);

			//Initialize();
			rectTransform.sizeDelta = m_InitialSize;
        }

        public void SetColor(Color accentColor)
        {
            if(m_Header != null)
            {
                m_Header.color = accentColor;
            }

            if(m_DatePickerYearList != null)
            {
                m_DatePickerYearList.SetColor(accentColor);
            }

            if(m_DatePickerDayItems != null)
            {
                for (int i = 0; i < m_DatePickerDayItems.Length; i++)
                {
                    m_DatePickerDayItems[i].selectedImage.color = accentColor;
                }
            }

            if (m_DatePickerMonthItems != null)
            {
                for (int i = 0; i < m_DatePickerMonthItems.Length; i++)
                {
                    m_DatePickerMonthItems[i].selectedImage.color = accentColor;
                }
            }
        }

        public virtual void SetDate(DateTime date)
        {
            m_CurrentDate = date;
			UpdateDaysText();
            UpdateDateList(GetMonthDateList(m_CurrentDate.Year, m_CurrentDate.Month));
            UpdateYearMonthsDateList();

            UpdateDatesText();
        }

        public void SetYear(int year)
        {
            DateTime newDate = default(DateTime);
            if (!DateTime.IsLeapYear(year) && m_CurrentDate.Month == 2 && m_CurrentDate.Day == 29)
            {
                newDate = new DateTime(year, m_CurrentDate.Month, 28);
            }
            else
            {
                newDate = new DateTime(year, m_CurrentDate.Month, m_CurrentDate.Day);
            }

            SetDate(newDate);

            OnDateClicked();
        }

        public void SetCultureInfo(CultureInfo cultureInfo)
        {
            m_CultureInfo = cultureInfo;

            if (m_CultureInfo == null)
            {
                m_CultureInfo = new CultureInfo("en-US");
            }

            SetDate(m_CurrentDate);
        }

        public void SetDayOfWeek(DayOfWeek dayOfWeek, CultureInfo cultureInfo)
		{
			m_DayOfWeek = dayOfWeek;
            SetCultureInfo(cultureInfo);
        }

		public void SetDateFormatPattern(string dateFormatPattern)
		{
			m_DateFormatPattern = dateFormatPattern;
			UpdateDatesText();
		}

        private void UpdateDateList(List<DateTime> dateTime)
        {
            for (int i = 0; i < m_DatePickerDayItems.Length; i++)
            {
                DateTime date = (i < dateTime.Count) ? dateTime[i] : default(DateTime);
                m_DatePickerDayItems[i].Init(date, OnDayItemValueChanged);

                m_DatePickerDayItems[i].UpdateState(m_CurrentDate);
            }
        }

        private void UpdateYearMonthsDateList()
        {
            m_CultureInfo = cultureInfo;

            if (m_CultureInfo == null)
            {
                m_CultureInfo = new CultureInfo("en-US");
            }

            var v_monthsDate = GetYearMonthsDateList(m_CurrentDate.Year);
            for (int i = 0; i < m_DatePickerMonthItems.Length; i++)
            {
                DateTime date = (i < v_monthsDate.Count) ? v_monthsDate[i] : default(DateTime);
                m_DatePickerMonthItems[i].Init(date, m_CultureInfo, OnMonthItemValueChanged);

                m_DatePickerMonthItems[i].UpdateState(m_CurrentDate);
            }
        }

        private void UpdateDatesText()
        {
            if (m_CultureInfo == null)
            {
                m_CultureInfo = System.Globalization.CultureInfo.CurrentCulture;
            }

            if (m_TextMonth != null)
            {
                m_TextMonth.SetGraphicText(m_CurrentDate.ToString("MMMMM yyyy", m_CultureInfo));
            }

            if(m_TextYear != null)
            {
                m_TextYear.SetGraphicText(m_CurrentDate.ToString("yyyy"));
            }

            if(m_TextDate != null)
            {
                m_TextDate.SetGraphicText(GetFormattedDate(m_CurrentDate));
            }
        }

		private void UpdateDaysText()
		{
            if (m_CultureInfo == null)
            {
                m_CultureInfo = new CultureInfo("en-US");
            }

            for (int i = 0; i < 7; i++)
			{
                if (m_DatePickerDayTexts.Length > i)
                {
                    int day = ((int)m_DayOfWeek + i) % 7;
                    m_DatePickerDayTexts[i].SetGraphicText(m_CultureInfo.DateTimeFormat.GetDayName((DayOfWeek)day).Substring(0, 1).ToUpper());
                }
			}
		}

        private string GetFormattedDate(DateTime date)
        {
            return date.ToString(m_DateFormatPattern, m_CultureInfo);
        }

        private List<DateTime> GetMonthDateList(int year, int month)
        {
            List<DateTime> dateList = new List<DateTime>();

            DateTime firstDate = new DateTime(year, month, 1);
            while (firstDate.DayOfWeek != m_DayOfWeek)
            {
                firstDate = firstDate.AddDays(-1);
            }

            int lastDayInMonth = DateTime.DaysInMonth(year, month);
            while (firstDate.Day != lastDayInMonth || firstDate.Month != month)
            {
                dateList.Add(firstDate);
                firstDate = firstDate.AddDays(1);
            }

            dateList.Add(firstDate);

            return dateList;
        }

        private List<DateTime> GetYearMonthsDateList(int year)
        {
            List<DateTime> monthsList = new List<DateTime>();

            for (int i = 1; i <= 12; i++)
            {
                monthsList.Add(new DateTime(year, i, 1));
            }

            return monthsList;
        }

        #endregion

        #region Receivers

        protected virtual void OnDayItemValueChanged(DialogDatePickerDayItem dayItem, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            m_CurrentDate = dayItem.dateTime;
            UpdateDatesText();
        }

        protected virtual void OnMonthItemValueChanged(DialogDatePickerMonthItem monthItem, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            m_CurrentDate = monthItem.dateTime;
            UpdateDatesText();
        }

        public void OnPreviousMonthClicked()
        {
            DateTime date = m_CurrentDate;
            date = date.AddMonths(-1);
            SetDate(new DateTime(date.Year, date.Month, 1));
        }

        public void OnNextMonthClicked()
        {
            DateTime date = m_CurrentDate;
            date = date.AddMonths(1);
            SetDate(new DateTime(date.Year, date.Month, 1));
        }

        public void OnYearClicked()
        {
            m_YearCanvasGroup.gameObject.SetActive(true);  // HACK: to disable the big list of GameObjects to avoid lag during dialog movement
            TweenManager.TweenFloat(f => m_CalendarCanvasGroup.alpha = f, m_CalendarCanvasGroup.alpha, 0f, 0.5f);
            TweenManager.TweenFloat(f => m_YearCanvasGroup.alpha = f, m_YearCanvasGroup.alpha, 1f, 0.5f, 0.01f);  // HACK: 0.01f of delay, because if not, the animation is not played

            m_DatePickerYearList.CenterToSelectedYear(m_CurrentDate.Year);

            m_CalendarCanvasGroup.blocksRaycasts = false;
            m_YearCanvasGroup.blocksRaycasts = true;

            m_TextDate.color = new Color(m_TextDate.color.r, m_TextDate.color.g, m_TextDate.color.b, 0.5f);
            m_TextYear.color = new Color(m_TextYear.color.r, m_TextYear.color.g, m_TextYear.color.b, 1.0f);
        }

        public void OnDateClicked()
        {
            if (m_CalendarCanvasGroup != null)
            {
                TweenManager.TweenFloat(f => m_CalendarCanvasGroup.alpha = f, m_CalendarCanvasGroup.alpha, 1f, 0.5f);
                m_CalendarCanvasGroup.blocksRaycasts = true;
            }

            if (m_YearCanvasGroup != null)
            {
                TweenManager.TweenFloat(f => m_YearCanvasGroup.alpha = f, m_YearCanvasGroup.alpha, 0f, 0.5f, 0.01f, callback: () =>
                {
                    m_YearCanvasGroup.gameObject.SetActive(false); // HACK: to disable the big list of GameObjects to avoid lag during dialog movement
                });

                m_YearCanvasGroup.blocksRaycasts = false;
            }

            if(m_TextDate != null)
            {
                m_TextDate.color = new Color(m_TextDate.color.r, m_TextDate.color.g, m_TextDate.color.b, 1.0f);
            }

            if(m_TextYear != null)
            {
                m_TextYear.color = new Color(m_TextYear.color.r, m_TextYear.color.g, m_TextYear.color.b, 0.5f);
            }
        }

        public void OnButtonOkClicked()
        {
            if (m_OnAffirmativeClicked != null)
            {
                m_OnAffirmativeClicked(m_CurrentDate);
            }

            Hide();
        }

		public void OnButtonCancelClicked()
		{
			if (m_OnDismissiveClicked != null)
			{
				m_OnDismissiveClicked();
			}

			Hide();
		}

        #endregion
    }
}