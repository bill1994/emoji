using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialDateTimePicker : StyleElement<MaterialStylePanel.PanelStyleProperty>
    {
        #region Helper Classes

        public enum DateTimeMode { Date, Time, DateTime, Month }

        [System.Serializable]
        public class DateUnityEvent : UnityEvent<System.DateTime> { }
        [System.Serializable]
        public class StrUnityEvent : UnityEvent<string> { }

        #endregion

        #region Static Vars

        static DialogDatePicker s_DatePicker = null;
        static DialogTimePicker s_TimePicker = null;

        #endregion

        #region Private Variables

        [SerializeField]
        protected DateTimeMode m_PickerMode = DateTimeMode.Date;
        [Space]
        [SerializeField, Tooltip("Use this to return current string date format\n-'d' will display dd/MM/aaaa ,\n-'t' will display HH:mm ,\n-'g' will display full date and time")]
        protected string m_DateFormat = "d";
        [SerializeField]
        protected string m_CultureInfo = "current";

        [Space]
        [SerializeField]
        protected string m_CurrentFormattedDate = "";
        [Space]
        [SerializeField, SerializeStyleProperty]
        protected Color m_DialogColor = MaterialColor.teal500;
        [Space]
        [SerializeField, SerializeStyleProperty]
        protected MaterialButton m_Button = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_TextComponent = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_HintTextComponent = null;

        #endregion

        #region Callback

        [Header("Callbacks")]
        public UnityEvent OnCancelCallback;
        public DateUnityEvent OnDateTimeChangedCallback;
        public StrUnityEvent OnFormattedDateTimeChangedCallback;

        #endregion

        #region Public Properties

        public Color dialogColor
        {
            get
            {
                return m_DialogColor;
            }

            set
            {
                m_DialogColor = value;
            }
        }

        public Color textComponentColor
        {
            get
            {
                return m_TextComponent != null? m_TextComponent.color : Color.black;
            }

            set
            {
                if (m_TextComponent != null && m_TextComponent.color != value)
                    m_TextComponent.color = value;
            }
        }

        public Color hintComponentColor
        {
            get
            {
                return m_HintTextComponent != null ? m_HintTextComponent.color : Color.black;
            }

            set
            {
                if (m_HintTextComponent != null && m_HintTextComponent.color != value)
                    m_HintTextComponent.color = value;
            }
        }

        public MaterialButton button
        {
            get
            {
                return m_Button;
            }

            set
            {
                if (m_Button == value)
                    return;

                UnregisterEvents();
                m_Button = value;
                if (enabled && gameObject.activeInHierarchy && gameObject.activeSelf)
                    RegisterEvents();
            }
        }

        public string CurrentFormattedDate
        {
            get
            {
                return m_CurrentFormattedDate;
            }

            set
            {
                if (m_CurrentFormattedDate == value)
                    return;

                //We only call full callbacks if value is valid
                System.DateTime date = System.DateTime.MinValue;
                if (TryParseDate(value, m_DateFormat, GetCultureInfo(), out date))
                    HandleOnChangeDateTime(date);
                else
                {
                    //Only call FormattedDate callback
                    m_CurrentFormattedDate = value;
                    UpdateTextState();
                    if (OnFormattedDateTimeChangedCallback != null)
                        OnFormattedDateTimeChangedCallback.Invoke(m_CurrentFormattedDate);

                }
            }
        }

        public string DateFormat
        {
            get
            {
                return m_DateFormat;
            }

            set
            {
                var v_value = string.IsNullOrEmpty(value) ? "" : value;
                if (m_DateFormat == v_value)
                    return;

                m_CurrentFormattedDate = HasValidDate()? GetCurrentDate().ToString(v_value, GetCultureInfo()) : "";
                m_DateFormat = v_value;
                HandleOnChangeDateTime(m_CurrentFormattedDate);
            }
        }

        public string CultureInfo
        {
            get
            {
                return m_CultureInfo;
            }

            set
            {
                var v_value = string.IsNullOrEmpty(value) ? "" : value;
                if (m_CultureInfo == v_value)
                    return;
                m_CurrentFormattedDate = HasValidDate() ? GetCurrentDate().ToString(v_value, GetCultureInfo()) : "";
                m_CultureInfo = v_value;
                HandleOnChangeDateTime(m_CurrentFormattedDate);
            }
        }

        public DateTimeMode PickerMode
        {
            get
            {
                return m_PickerMode;
            }

            set
            {
                m_PickerMode = value;
            }
        }

        public Graphic textComponent
        {
            get
            {
                return m_TextComponent;
            }

            set
            {
                m_TextComponent = value;
            }
        }

        public Graphic hintTextComponent
        {
            get
            {
                return m_HintTextComponent;
            }

            set
            {
                m_HintTextComponent = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            RegisterEvents();
            base.OnEnable();
        }

        protected override void Start()
        {
            base.Start();
            Init();
        }

        protected override void OnDisable()
        {
            UnregisterEvents();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if(Application.isPlaying)
                HandleOnChangeDateTime(m_CurrentFormattedDate);
        }

#endif

        #endregion

        #region Public Helper Functions

        public void SetPickerMode(int pickerMode)
        {
            var mode = (DateTimeMode)pickerMode;
            if (mode != DateTimeMode.Date &&
                mode != DateTimeMode.Time &&
                mode != DateTimeMode.DateTime &&
                mode != DateTimeMode.Month)
            {
                mode = DateTimeMode.Date;
            }

            PickerMode = mode;
        }

        public bool HasValidDate()
        {
            return IsValidDate(m_CurrentFormattedDate, m_DateFormat, GetCultureInfo());
        }

        public void ShowPicker()
        {
            System.DateTime date = GetCurrentDate();

            var isTimePicker = m_PickerMode == DateTimeMode.Time || m_PickerMode == DateTimeMode.DateTime;
            var isDatePicker = m_PickerMode == DateTimeMode.Date || m_PickerMode == DateTimeMode.DateTime;
            var isMonthPicker = m_PickerMode == DateTimeMode.Month;

            if (isDatePicker || isMonthPicker)
            {
                //We must call changeTime after change date (if supported)
                System.Action<System.DateTime> onChangeDate = (pickedDate) =>
                {
                    HandleOnChangeDateKeepingLastTime(pickedDate);

                    pickedDate = GetCurrentDate();
                    if (isTimePicker)
                    {
                        if (s_TimePicker == null)
                        {
                            DialogManager.ShowTimePickerAsync(pickedDate, HandleOnChangeTimeKeepingLastDate, m_DialogColor, (dialog) =>
                            {
                                s_TimePicker = dialog;
                                //dialog.destroyOnHide = false;

                            });
                        }
                        else
                        {
                            //s_TimePicker.destroyOnHide = false;
                            s_TimePicker.Initialize(pickedDate, HandleOnChangeTimeKeepingLastDate, m_DialogColor);
                            s_TimePicker.Show();
                        }
                    }
                };

                //Show DatePicker
                if (s_DatePicker == null)
                {
                    if (isMonthPicker)
                    {
                        DialogManager.ShowMonthPickerAsync(date.Year, date.Month, onChangeDate, HandleOnDismiss, m_DialogColor, (dialog) =>
                        {
                            dialog.SetCultureInfo(GetCultureInfo());
                            s_DatePicker = dialog;
                        });
                    }
                    else
                    {
                        DialogManager.ShowDatePickerAsync(date.Year, date.Month, date.Day, onChangeDate, HandleOnDismiss, m_DialogColor, (dialog) =>
                        {
                            dialog.SetCultureInfo(GetCultureInfo());
                            s_DatePicker = dialog;
                        });
                    }
                }
                else
                {
                    s_DatePicker.Initialize(date.Year, date.Month, date.Day, onChangeDate, HandleOnDismiss, m_DialogColor);
                    s_DatePicker.SetCultureInfo(GetCultureInfo());
                    s_DatePicker.Show();
                }
            }

            if (isTimePicker && m_PickerMode != DateTimeMode.DateTime)
            {
                if (isTimePicker)
                {
                    if (s_TimePicker == null)
                    {
                        DialogManager.ShowTimePickerAsync(date, HandleOnChangeDateTime, m_DialogColor, (dialog) =>
                        {
                            s_TimePicker = dialog;
                            //dialog.destroyOnHide = false;
                        });
                    }
                    else
                    {
                        //s_TimePicker.destroyOnHide = false;
                        s_TimePicker.Initialize(date, HandleOnChangeDateTime, m_DialogColor);
                        s_TimePicker.Show();
                    }
                }
            }
            
        }

        public void DismissPicker()
        {
            if (s_DatePicker != null && s_DatePicker.gameObject.activeSelf)
                s_DatePicker.Hide();
            if (s_TimePicker != null && s_TimePicker.gameObject.activeSelf)
                s_TimePicker.Hide();

            HandleOnDismiss();
        }

        public void SetCurrentDate(System.DateTime p_value)
        {
            HandleOnChangeDateTime(p_value);
        }

        public System.DateTime GetCurrentDate()
        {
            return ParseDate(m_CurrentFormattedDate, m_DateFormat, GetCultureInfo());
        }

        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0);
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void Init()
        {
            //Call ChangeDateTime Initial Callback
            m_CurrentFormattedDate = m_CurrentFormattedDate.Trim();
            if (m_CurrentFormattedDate == "*" || string.Equals(m_CurrentFormattedDate, "now", System.StringComparison.CurrentCultureIgnoreCase))
            {
                HandleOnChangeDateTime(System.DateTime.Now);
            }
            else if (string.Equals(m_CurrentFormattedDate, "utcnow", System.StringComparison.CurrentCultureIgnoreCase))
            {
                HandleOnChangeDateTime(System.DateTime.UtcNow);
            }
            else if (string.Equals(m_CurrentFormattedDate, "min", System.StringComparison.CurrentCultureIgnoreCase))
            {
                HandleOnChangeDateTime(System.DateTime.MinValue);
            }
            else
            {
                HandleOnChangeDateTime(m_CurrentFormattedDate);
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (m_Button != null && m_Button.buttonObject != null)
                m_Button.buttonObject.onClick.AddListener(ShowPicker);
        }

        protected virtual void UnregisterEvents()
        {
            if (m_Button != null && m_Button.buttonObject != null)
                m_Button.buttonObject.onClick.RemoveListener(ShowPicker);
        }

        protected virtual void UpdateTextState()
        {
            bool isValidData = IsValidDate(m_CurrentFormattedDate, m_DateFormat, GetCultureInfo());
            if (m_TextComponent != null)
            {
                bool textActive = m_HintTextComponent == null || isValidData;
                m_TextComponent.SetGraphicText(m_CurrentFormattedDate);
                m_TextComponent.enabled = textActive;
            }
            if (m_HintTextComponent != null)
            {
                bool hintActive = m_TextComponent != null && !isValidData;
                m_HintTextComponent.enabled = hintActive;
            }
        }

        public System.Globalization.CultureInfo GetCultureInfo()
        {
            if (m_CultureInfo == null)
                m_CultureInfo = "";

            if (string.IsNullOrEmpty(m_CultureInfo) ||
                string.Equals(m_CultureInfo, "invariant", System.StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(m_CultureInfo, "invariantculture", System.StringComparison.CurrentCultureIgnoreCase))
            {
                return System.Globalization.CultureInfo.InvariantCulture;
            }
            else if (string.Equals(m_CultureInfo, "*", System.StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(m_CultureInfo, "current", System.StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(m_CultureInfo, "currentculture", System.StringComparison.CurrentCultureIgnoreCase))
            {
                return System.Globalization.CultureInfo.CurrentCulture;
            }
            else
            {
                try
                {
                    return new System.Globalization.CultureInfo(m_CultureInfo, false);
                }
                catch
                {
                    //Culture not found, revert to empty
                    Debug.Log("Culture '" + m_CultureInfo + "' not found (sender: " + name + ")");
                    m_CultureInfo = "";
                }
            }
            return System.Globalization.CultureInfo.InvariantCulture;
        }

        #endregion

        #region Receivers

        public virtual void HandleOnChangeTimeKeepingLastDate(System.DateTime date)
        {
            var currentDate = GetCurrentDate();

            date = new System.DateTime(currentDate.Year, currentDate.Month, currentDate.Day, date.Hour, date.Minute, date.Second, date.Millisecond);
            HandleOnChangeDateTime(date);
        }

        public virtual void HandleOnChangeDateKeepingLastTime(System.DateTime date)
        {
            var currentDate = GetCurrentDate();

            date = new System.DateTime(date.Year, date.Month, date.Day, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond);
            HandleOnChangeDateTime(date);
        }

        public virtual void HandleOnChangeDateTime(System.DateTime date)
        {
            HandleOnChangeDateTime(date.ToString(m_DateFormat, GetCultureInfo()));
        }

        public virtual void HandleOnChangeDateTime(string dateFormatted)
        {
            System.DateTime date = System.DateTime.Now;
            TryParseDate(dateFormatted, m_DateFormat, GetCultureInfo(), out date);
            m_CurrentFormattedDate = dateFormatted;

            UpdateTextState();
            if (OnDateTimeChangedCallback != null)
                OnDateTimeChangedCallback.Invoke(date);
            if (OnFormattedDateTimeChangedCallback != null)
                OnFormattedDateTimeChangedCallback.Invoke(m_CurrentFormattedDate);
        }

        public virtual void HandleOnDismiss()
        {
            if (OnCancelCallback != null)
                OnCancelCallback.Invoke();
        }

        #endregion

        #region Static Helper Functions

        public static bool IsValidDate(string dateFormatted, string format, System.Globalization.CultureInfo cultureInfo)
        {
            System.DateTime date = System.DateTime.MinValue;
            return TryParseDate(dateFormatted, format, cultureInfo, out date);
        }

        public static System.DateTime ParseDate(string dateStr, string format, System.Globalization.CultureInfo cultureInfo)
        {
            System.DateTime date = System.DateTime.Now;
            TryParseDate(dateStr, format, cultureInfo, out date);
            return date;
        }

        public static bool TryParseDate(string dateStr, string format, System.Globalization.CultureInfo cultureInfo, out System.DateTime date)
        {
            date = System.DateTime.Now;
            if (!string.IsNullOrEmpty(dateStr))
            {
                if (!System.DateTime.TryParseExact(dateStr, format, cultureInfo, System.Globalization.DateTimeStyles.None, out date))
                    return System.DateTime.TryParse(dateStr, cultureInfo, System.Globalization.DateTimeStyles.None, out date);
                else
                    return true;
            }
            return false;
        }

        #endregion

    }
}
