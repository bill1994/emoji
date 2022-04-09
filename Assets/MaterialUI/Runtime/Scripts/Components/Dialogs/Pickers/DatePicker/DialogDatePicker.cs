// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Events;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Date Picker", 1)]
    public class DialogDatePicker : MaterialDialogCompat
    {
        [System.Serializable]
        public class DateTimeUnityEvent : UnityEvent<DateTime> { }

        #region Private Variables

        [SerializeField, SerializeStyleProperty]
        protected Graphic m_TextDate = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_TextMonth = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_TextYear = null;
        [SerializeField, SerializeStyleProperty]
        protected Image m_Header = null;

        [SerializeField]
        protected CanvasGroup m_CalendarCanvasGroup = null;
        [SerializeField]
        protected CanvasGroup m_YearCanvasGroup = null;
        [SerializeField]
        protected DialogDatePickerYearList m_DatePickerYearList = null;

        [Space]
		[SerializeField]
        protected Graphic[] m_DatePickerDayTexts = null;
        [SerializeField]
        protected DialogDatePickerDayItem[] m_DatePickerDayItems = null;

        [Space]
        [SerializeField]
        protected DialogDatePickerMonthItem[] m_DatePickerMonthItems = null;

        [SerializeField]
        protected string m_DateFormatPattern = "ddd, MMM dd";

        protected DateTime _CurrentDate;

        protected DayOfWeek _DayOfWeek = DayOfWeek.Sunday;

        protected CultureInfo _CultureInfo;

        protected Vector2 _InitialSize;

        //Internal Callbacks
        protected Action<DateTime> _OnAffirmativeClicked;
        protected Action _OnDismissiveClicked;


        //Just Used to Call OnDateChangedCallback
        protected DateTime currentDateInternal
        {
            get
            {
                return _CurrentDate;
            }
            set
            {
                if (_CurrentDate == value)
                    return;
                _CurrentDate = value;

                if (OnCurrentDateChangedCallback != null)
                    OnCurrentDateChangedCallback.Invoke(_CurrentDate);
            }
        }

        #endregion

        #region Callbacks

        public DateTimeUnityEvent OnCurrentDateChangedCallback = new DateTimeUnityEvent();

        #endregion

        #region Public Properties

        public Action<DateTime> onAffirmativeClicked
        {
            get { return _OnAffirmativeClicked; }
            set { _OnAffirmativeClicked = value; }
        }

        public Action onDismissiveClicked
        {
            get { return _OnDismissiveClicked; }
            set { _OnDismissiveClicked = value; }
        }

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
            get { return _CurrentDate; }
            set
            {
                SetDate(value);
            }
        }

        public DayOfWeek dayOfWeek
        {
            get { return _DayOfWeek; }
        }

        public CultureInfo cultureInfo
        {
            get { return _CultureInfo; }
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
			_InitialSize = rectTransform.sizeDelta;
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
            _OnAffirmativeClicked = onAffirmativeClicked;
			_OnDismissiveClicked = onDismissiveClicked;

            SetColor(accentColor);

			//Initialize();
			rectTransform.sizeDelta = _InitialSize;
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
            currentDateInternal = date;

            UpdateDaysText();
            UpdateDateList(GetMonthDateList(_CurrentDate.Year, _CurrentDate.Month));
            UpdateYearMonthsDateList();

            UpdateDatesText();
        }

        public void SetYear(int year)
        {
            DateTime newDate = default(DateTime);
            if (!DateTime.IsLeapYear(year) && _CurrentDate.Month == 2 && _CurrentDate.Day == 29)
            {
                newDate = new DateTime(year, _CurrentDate.Month, 28);
            }
            else
            {
                newDate = new DateTime(year, _CurrentDate.Month, _CurrentDate.Day);
            }

            SetDate(newDate);

            OnDateClicked();
        }

        public void SetCultureInfo(CultureInfo cultureInfo)
        {
            _CultureInfo = cultureInfo;

            if (_CultureInfo == null)
            {
                _CultureInfo = new CultureInfo("en-US");
            }

            SetDate(_CurrentDate);
        }

        public void SetDayOfWeek(DayOfWeek dayOfWeek, CultureInfo cultureInfo)
		{
			_DayOfWeek = dayOfWeek;
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

                m_DatePickerDayItems[i].UpdateState(_CurrentDate);
            }
        }

        private void UpdateYearMonthsDateList()
        {
            _CultureInfo = cultureInfo;

            if (_CultureInfo == null)
            {
                _CultureInfo = new CultureInfo("en-US");
            }

            var v_monthsDate = GetYearMonthsDateList(_CurrentDate.Year);
            for (int i = 0; i < m_DatePickerMonthItems.Length; i++)
            {
                DateTime date = (i < v_monthsDate.Count) ? v_monthsDate[i] : default(DateTime);
                m_DatePickerMonthItems[i].Init(date, _CultureInfo, OnMonthItemValueChanged);

                m_DatePickerMonthItems[i].UpdateState(_CurrentDate);
            }
        }

        private void UpdateDatesText()
        {
            if (_CultureInfo == null)
            {
                _CultureInfo = System.Globalization.CultureInfo.CurrentCulture;
            }

            if (m_TextMonth != null)
            {
                m_TextMonth.SetGraphicText(_CurrentDate.ToString("MMMMM yyyy", _CultureInfo));
            }

            if(m_TextYear != null)
            {
                m_TextYear.SetGraphicText(_CurrentDate.ToString("yyyy"));
            }

            if(m_TextDate != null)
            {
                m_TextDate.SetGraphicText(GetFormattedDate(_CurrentDate));
            }
        }

		private void UpdateDaysText()
		{
            if (_CultureInfo == null)
            {
                _CultureInfo = new CultureInfo("en-US");
            }

            for (int i = 0; i < 7; i++)
			{
                if (m_DatePickerDayTexts.Length > i)
                {
                    int day = ((int)_DayOfWeek + i) % 7;
                    m_DatePickerDayTexts[i].SetGraphicText(_CultureInfo.DateTimeFormat.GetDayName((DayOfWeek)day).Substring(0, 1).ToUpper());
                }
			}
		}

        private string GetFormattedDate(DateTime date)
        {
            return date.ToString(m_DateFormatPattern, _CultureInfo);
        }

        private List<DateTime> GetMonthDateList(int year, int month)
        {
            List<DateTime> dateList = new List<DateTime>();

            DateTime firstDate = new DateTime(year, month, 1);
            while (firstDate.DayOfWeek != _DayOfWeek)
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

        public override void OnActivityBeginHide()
        {
            if (_OnDismissiveClicked != null)
                _OnDismissiveClicked.InvokeIfNotNull();

            base.OnActivityBeginHide();
        }

        protected virtual void OnDayItemValueChanged(DialogDatePickerDayItem dayItem, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            currentDateInternal = dayItem.dateTime;
            UpdateDatesText();
        }

        protected virtual void OnMonthItemValueChanged(DialogDatePickerMonthItem monthItem, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            currentDateInternal = monthItem.dateTime;
            UpdateDatesText();
        }

        public void OnPreviousMonthClicked()
        {
            DateTime date = _CurrentDate;
            date = date.AddMonths(-1);
            SetDate(new DateTime(date.Year, date.Month, 1));
        }

        public void OnNextMonthClicked()
        {
            DateTime date = _CurrentDate;
            date = date.AddMonths(1);
            SetDate(new DateTime(date.Year, date.Month, 1));
        }

        public void OnYearClicked()
        {
            m_YearCanvasGroup.gameObject.SetActive(true);  // HACK: to disable the big list of GameObjects to avoid lag during dialog movement
            TweenManager.TweenFloat(f => m_CalendarCanvasGroup.alpha = f, m_CalendarCanvasGroup.alpha, 0f, 0.5f);
            TweenManager.TweenFloat(f => m_YearCanvasGroup.alpha = f, m_YearCanvasGroup.alpha, 1f, 0.5f, 0.01f);  // HACK: 0.01f of delay, because if not, the animation is not played

            m_DatePickerYearList.CenterToSelectedYear(_CurrentDate.Year);

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
            var oldDismissAction = _OnDismissiveClicked;
            _OnDismissiveClicked = null;

            if (_OnAffirmativeClicked != null)
            {
                _OnAffirmativeClicked(_CurrentDate);
            }

            Hide();

            _OnDismissiveClicked = oldDismissAction;
        }

		public void OnButtonCancelClicked()
		{
			Hide();
		}

        #endregion
    }
}