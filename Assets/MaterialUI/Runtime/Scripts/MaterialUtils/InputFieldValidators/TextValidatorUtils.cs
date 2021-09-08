//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine.UI;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Globalization;

namespace MaterialUI
{
    /// <summary>
    /// Base class for a Text validator.
    /// </summary>
    public class BaseTextValidator
    {
        #region Private Variables

        protected MaterialInputField m_MaterialInputField;

        #endregion

        #region Public Properties

        public virtual MaterialInputField materialInputField { get { return m_MaterialInputField; } }

        #endregion

        #region Public Functions

        public virtual bool IsTextValid()
        {
            return true;
        }

        /// <summary>
        /// Initializes the validator for the specified material input field.
        /// </summary>
        /// <param name="materialInputField">The material input field.</param>
        public virtual void Init(MaterialInputField materialInputField)
        {
            Dispose();
            m_MaterialInputField = materialInputField;
        }

        public virtual void Dispose()
        {
            m_MaterialInputField = null;
        }

        #endregion
    }

    public class BaseAutoFormatTextValidator : BaseTextValidator
    {
        #region Public Functions

        public override void Init(MaterialInputField materialInputField)
        {
            Dispose();

            m_MaterialInputField = materialInputField;

            RegisterEvents();
        }

        public override void Dispose()
        {
            UnregisterEvents();
            _formatTextCoroutine = null;
            if (_formatTextCoroutine != null)
                m_MaterialInputField.StopCoroutine(_formatTextCoroutine);
            base.Dispose();
        }

        public virtual bool FormatText()
        {
            return false;
        }

        #endregion

        #region Helper Functions

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (Application.isPlaying && m_MaterialInputField != null)
            {
                if (TouchScreenKeyboard.isSupported)
                {
                    if (m_MaterialInputField.onEndEdit != null)
                        m_MaterialInputField.onEndEdit.AddListener(HandleonValueChanged);
                }
                else
                {
                    if (m_MaterialInputField.onValueChanged != null)
                        m_MaterialInputField.onValueChanged.AddListener(HandleonValueChanged);
                }
            }
        }

        protected virtual void UnregisterEvents()
        {
            if (m_MaterialInputField != null)
            {
                if (TouchScreenKeyboard.isSupported)
                {
                    if (m_MaterialInputField.onEndEdit != null)
                        m_MaterialInputField.onEndEdit.RemoveListener(HandleonValueChanged);
                }
                else
                {
                    if (m_MaterialInputField.onValueChanged != null)
                        m_MaterialInputField.onValueChanged.RemoveListener(HandleonValueChanged);
                }
            }
        }

        #endregion

        #region Events

        Coroutine _formatTextCoroutine = null;
        protected virtual void HandleonValueChanged(string text)
        {
            if (m_MaterialInputField != null)
            {
                if (_formatTextCoroutine != null)
                    m_MaterialInputField.StopCoroutine(_formatTextCoroutine);

                if (m_MaterialInputField.enabled && m_MaterialInputField.gameObject.activeInHierarchy)
                {
                    _formatTextCoroutine = m_MaterialInputField.StartCoroutine(FormatTextDelayedRoutine());
                }
                else
                {
                    _formatTextCoroutine = null;
                    UnregisterEvents();
                    FormatText();
                    IsTextValid();
                    RegisterEvents();
                }
            }
        }

        protected IEnumerator FormatTextDelayedRoutine()
        {
            yield return null;
            _formatTextCoroutine = null;
            UnregisterEvents();
            FormatText();
            RegisterEvents();
        }

        #endregion
    }

    /// <summary>
    /// Text validator that checks for empty text.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class EmptyTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialUI.EmptyTextValidator"/> class.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        public EmptyTextValidator(string errorMessage = "Can't be empty")
        {
            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the text contains characters, otherwise false.</returns>
        public override bool IsTextValid()
        {
            if (string.IsNullOrEmpty(m_MaterialInputField.text))
            {
                if (m_MaterialInputField != null && m_MaterialInputField.validationText != null)
                    m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
                return false;
            }

            return true;
        }

        public virtual ITextValidator Clone()
        {
            return new EmptyTextValidator(m_ErrorMessage);
        }
    }

    /// <summary>
    /// Text validator that checks for emails.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class EmailTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialUI.EmailTextValidator"/> class with a custom error message.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        public EmailTextValidator(string errorMessage = "Format is invalid")
        {
            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the text appears to be an email, otherwise false.</returns>
        public override bool IsTextValid()
        {
            Regex regex = new Regex(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*" + "@" + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
            Match match = regex.Match(m_MaterialInputField.text);
            if (!match.Success)
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
                return false;
            }

            return true;
        }

        public virtual ITextValidator Clone()
        {
            return new EmailTextValidator(m_ErrorMessage);
        }
    }

    /// <summary>
    /// Text validator that checks for names.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class NameTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// The minimum length.
        /// </summary>
        private int m_MinimumLength = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="NameTextValidator"/> class.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		public NameTextValidator(string errorMessage = "Format is invalid")
        {
            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameTextValidator"/> class.
        /// </summary>
		/// <param name="minimumLength">The minimum length.</param>
		/// <param name="errorMessage">The error message.</param>
		public NameTextValidator(int minimumLength, string errorMessage = "Format is invalid")
        {
            m_MinimumLength = minimumLength;
            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the text appears to be a name, otherwise false.</returns>
        public override bool IsTextValid()
        {
            if (m_MaterialInputField.text.Length < m_MinimumLength)
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
                return false;
            }

            Regex regex = new Regex(@"^\p{L}+(\s+\p{L}+)*$");
            Match match = regex.Match(m_MaterialInputField.text);
            if (!match.Success)
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
                return false;
            }

            return true;
        }

        public virtual ITextValidator Clone()
        {
            return new NameTextValidator(m_MinimumLength, m_ErrorMessage);
        }
    }

    /// <summary>
    /// Text validator that checks for minimum length.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class MinLengthTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// The minimum length
        /// </summary>
        private int m_MinimumLength = 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordTextValidator"/> class.
        /// </summary>
        /// <param name="minimumLength">The minimum length.</param>
		/// <param name="errorMessage">The error message.</param>
		public MinLengthTextValidator(int minimumLength, string errorMessage = "Require at least {0} characters")
        {
            m_MinimumLength = minimumLength;

            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the text is at least the length of or longer than m_MinimumLength, otherwise false.</returns>
        public override bool IsTextValid()
        {
            if (m_MaterialInputField.text.Length < m_MinimumLength)
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage, m_MinimumLength);
                return false;
            }

            return true;
        }

        public virtual ITextValidator Clone()
        {
            return new MinLengthTextValidator(m_MinimumLength, m_ErrorMessage);
        }
    }

    /// <summary>
    /// Text validator that checks for maximum length.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class MaxLengthTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// The minimum length
        /// </summary>
        private int m_MaximumLength = 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordTextValidator"/> class.
        /// </summary>
        /// <param name="minimumLength">The minimum length.</param>
        /// <param name="errorMessage">The error message.</param>
        public MaxLengthTextValidator(int maximumLength, string errorMessage)
        {
            m_MaximumLength = maximumLength;

            if (string.IsNullOrEmpty(errorMessage))
            {
                m_ErrorMessage = "Limited to " + m_MaximumLength + " characters";
            }
            else
            {
                m_ErrorMessage = errorMessage;
            }
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the text is lower than m_MaximumLength, otherwise false.</returns>
        public override bool IsTextValid()
        {
            if (m_MaterialInputField.text.Length > m_MaximumLength)
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
                return false;
            }

            return true;
        }

        public virtual ITextValidator Clone()
        {
            return new MaxLengthTextValidator(m_MaximumLength, m_ErrorMessage);
        }
    }

    /// <summary>
    /// Text validator that checks two passwords to see if they are the same.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class SamePasswordTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// The original password input field
        /// </summary>
        private MaterialInputField m_OriginalPasswordInputField;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamePasswordTextValidator"/> class.
        /// </summary>
		/// <param name="originalPasswordInputField">The original password input field.</param>
		/// <param name="errorMessage">The error message.</param>
		public SamePasswordTextValidator(MaterialInputField originalPasswordInputField, string errorMessage = "Passwords are different!")
        {
            m_OriginalPasswordInputField = originalPasswordInputField;
            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the text in the two fields match, otherwise false.</returns>
        public override bool IsTextValid()
        {

            if (!m_MaterialInputField.text.Equals(m_OriginalPasswordInputField.text))
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
                return false;
            }

            return true;
        }

        public virtual ITextValidator Clone()
        {
            return new SamePasswordTextValidator(m_OriginalPasswordInputField, m_ErrorMessage);
        }
    }

    /// <summary>
    /// Text validator that checks for existing directories.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class DirectoryExistTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialUI.DirectoryExistTextValidator"/> class.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        public DirectoryExistTextValidator(string errorMessage = "This directory does not exist")
        {
            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the directory given by the text value exists, otherwise false.</returns>
        public override bool IsTextValid()
        {
            bool directoryExists = Directory.Exists(m_MaterialInputField.text);
            if (!directoryExists)
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
            }

            return directoryExists;
        }

        public virtual ITextValidator Clone()
        {
            return new DirectoryExistTextValidator(m_ErrorMessage);
        }
    }

    /// <summary>
    /// Text validator that checks for existing files.
    /// </summary>
    /// <seealso cref="MaterialUI.BaseTextValidator" />
    /// <seealso cref="MaterialUI.ITextValidator" />
    public class FileExistTextValidator : BaseTextValidator, ITextValidator
    {
        /// <summary>
        /// The error message.
        /// </summary>
        private string m_ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialUI.DirectoryExistTextValidator"/> class.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        public FileExistTextValidator(string errorMessage = "This file does not exist")
        {
            m_ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the filepath given by the text value exists, otherwise false.</returns>
        public override bool IsTextValid()
        {
            bool fileExists = File.Exists(m_MaterialInputField.text);
            if (!fileExists)
            {
                m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
            }

            return fileExists;
        }
        public virtual ITextValidator Clone()
        {
            return new FileExistTextValidator(m_ErrorMessage);
        }
    }

    public class DecimalValidator : CustomFormatValidator
    {
        public const string DEFAULT_DECIMAL_INCLUSIVE_VALIDATOR_ERROR_MSG = "Value must be a number between '<localeparam=0>{0}</localeparam>' and '<localeparam=1>{1}</localeparam>' (Inclusive)";

        protected double _minValue = double.MinValue;
        protected double _maxValue = double.MaxValue;
        protected bool _isMinInclusive = true;
        protected bool _isMaxInclusive = true;
        protected System.IFormatProvider _formatProvider = null;
        protected const string DECIMAL_VALIDATOR = @"^[-+]?\d+([.,]\d+)?$";

        public DecimalValidator(string error = DEFAULT_DECIMAL_INCLUSIVE_VALIDATOR_ERROR_MSG) : this(true, double.MinValue, double.MaxValue, error)
        {
        }

        public DecimalValidator(bool isInclusive, double minValue = double.MinValue, double maxValue = double.MaxValue, string error = DEFAULT_DECIMAL_INCLUSIVE_VALIDATOR_ERROR_MSG) :
            this(null, isInclusive, minValue, maxValue, error)
        {
        }

        public DecimalValidator(bool isMinInclusive, bool isMaxInclusive, double minValue = double.MinValue, double maxValue = double.MaxValue, string error = DEFAULT_DECIMAL_INCLUSIVE_VALIDATOR_ERROR_MSG) :
            this(null, isMinInclusive, isMaxInclusive, minValue, maxValue, error)
        {
        }

        public DecimalValidator(System.IFormatProvider format, bool isInclusive, double minValue = double.MinValue, double maxValue = double.MaxValue, string error = DEFAULT_DECIMAL_INCLUSIVE_VALIDATOR_ERROR_MSG) :
            this(format, isInclusive, isInclusive, minValue, maxValue, error)
        {
        }

        public DecimalValidator(System.IFormatProvider format, bool isMinInclusive, bool isMaxInclusive, double minValue = double.MinValue, double maxValue = double.MaxValue, string error = DEFAULT_DECIMAL_INCLUSIVE_VALIDATOR_ERROR_MSG) : base(DECIMAL_VALIDATOR, string.Format(error, minValue.ToString("0.0"), maxValue.ToString("0.0")))
        {
            _isMinInclusive = isMinInclusive;
            _isMaxInclusive = isMaxInclusive;
            _minValue = minValue;
            _maxValue = maxValue;

            //Swap values if Min > Max
            if (_minValue > _maxValue)
            {
                var tempMin = _minValue;
                _minValue = _maxValue;
                _maxValue = tempMin;
            }
            _formatProvider = format == null ? CultureInfo.InvariantCulture : format;
        }

        public override bool IsTextValid()
        {
            var isValid = base.IsTextValid();

            if (isValid)
            {
                double parsedValue;
                0                isValid = m_MaterialInputField != null &&
                                    double.TryParse(m_MaterialInputField.text, NumberStyles.Float, _formatProvider, out parsedValue) &&
                                    ((_isMinInclusive && parsedValue == _minValue) || parsedValue > _minValue) &&
                                    ((_isMaxInclusive && parsedValue == _maxValue) || parsedValue < _maxValue);
            }
            if (!isValid)
            {
                if (m_MaterialInputField != null && m_MaterialInputField.validationText != null)
                    m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
            }
            return isValid;
        }

        public override ITextValidator Clone()
        {
            return new DecimalValidator(_formatProvider, _isMinInclusive, _isMaxInclusive, _minValue, _maxValue, m_ErrorMessage);
        }
    }

    public class DateFormatValidator : CustomFormatValidator
    {
        protected System.IFormatProvider _formatProvider = null;

        public DateFormatValidator(System.IFormatProvider format, string error = "Date format is invalid") : base("", error)
        {
            _formatProvider = format == null ? CultureInfo.InvariantCulture : format;
        }

        public DateFormatValidator(string cultureInfoName, string error = "Date format is invalid") : base("", error)
        {
            _formatProvider = CultureInfo.GetCultureInfo(cultureInfoName);
        }

        public override bool IsTextValid()
        {
            var isValid = base.IsTextValid();
            if (isValid)
            {
                var text = m_MaterialInputField != null ? m_MaterialInputField.text : "";
                System.DateTime result;
                return System.DateTime.TryParse(text, _formatProvider, System.Globalization.DateTimeStyles.None, out result);
            }
            return isValid;
        }

        public override ITextValidator Clone()
        {
            return new DateFormatValidator(_formatProvider, m_ErrorMessage);
        }
    }

    public class DateMinMaxFormatValidator : DateFormatValidator
    {
        public const string DEFAULT_DATE_INCLUSIVE_VALIDATOR_ERROR_MSG = "Value must be a date between '<localeparam=0>{0}</localeparam>' and '<localeparam=1>{1}</localeparam>' (Inclusive)";

        protected bool _isMinInclusive = true;
        protected bool _isMaxInclusive = true;
        protected System.DateTime _minValue = System.DateTime.MinValue;
        protected System.DateTime _maxValue = System.DateTime.MaxValue;
        protected string _formatExact;

        public DateMinMaxFormatValidator(bool isMinInclusive, bool isMaxInclusive, System.DateTime minValue, System.DateTime maxValue, string cultureInfoName, string error = DEFAULT_DATE_INCLUSIVE_VALIDATOR_ERROR_MSG) :
            this(isMinInclusive, isMaxInclusive, minValue, maxValue, CultureInfo.GetCultureInfo(cultureInfoName), error)
        {
        }

        public DateMinMaxFormatValidator(bool isMinInclusive, bool isMaxInclusive, System.DateTime minValue, System.DateTime maxValue, string formatExact, string cultureInfoName, string error = DEFAULT_DATE_INCLUSIVE_VALIDATOR_ERROR_MSG) :
            this(isMinInclusive, isMaxInclusive, minValue, maxValue, formatExact, CultureInfo.GetCultureInfo(cultureInfoName), error)
        {
        }

        public DateMinMaxFormatValidator(bool isMinInclusive, bool isMaxInclusive, System.DateTime minValue, System.DateTime maxValue, System.IFormatProvider format, string error = DEFAULT_DATE_INCLUSIVE_VALIDATOR_ERROR_MSG) : this(isMinInclusive, isMaxInclusive, minValue, maxValue, null, format, error)
        {
        }

        public DateMinMaxFormatValidator(bool isMinInclusive, bool isMaxInclusive, System.DateTime minValue, System.DateTime maxValue, string formatExact, System.IFormatProvider format, string error = DEFAULT_DATE_INCLUSIVE_VALIDATOR_ERROR_MSG) : base(format, string.Format(error, minValue.ToString("d", format), maxValue.ToString("d", format)))
        {
            _isMinInclusive = isMinInclusive;
            _isMaxInclusive = isMaxInclusive;
            _minValue = minValue;
            _maxValue = maxValue;

            //Swap values if Min > Max
            if (_minValue > _maxValue)
            {
                var tempMin = _minValue;
                _minValue = _maxValue;
                _maxValue = tempMin;
            }
            _formatProvider = format == null ? CultureInfo.InvariantCulture : format;
            _formatExact = formatExact;
        }

        public override bool IsTextValid()
        {
            bool isValid = false;
            var text = m_MaterialInputField != null ? m_MaterialInputField.text : "";

            System.DateTime result = System.DateTime.MinValue;
            //Added support to TryParse Exact
            if (!string.IsNullOrEmpty(_formatExact))
                isValid = System.DateTime.TryParseExact(text, _formatExact, _formatProvider, System.Globalization.DateTimeStyles.None, out result);

            isValid = isValid || System.DateTime.TryParse(text, _formatProvider, System.Globalization.DateTimeStyles.None, out result);

            if (isValid)
            {
                isValid = m_MaterialInputField != null &&
                    ((_isMinInclusive && result == _minValue) || result > _minValue) &&
                    ((_isMaxInclusive && result == _maxValue) || result < _maxValue);
            }
            if (!isValid)
            {
                if (m_MaterialInputField != null && m_MaterialInputField.validationText != null)
                    m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
            }
            return isValid;
        }

        public override ITextValidator Clone()
        {
            return new DateMinMaxFormatValidator(_isMinInclusive, _isMaxInclusive, _minValue, _maxValue, _formatProvider, m_ErrorMessage);
        }
    }

    public class StrongPasswordValidator : CustomFormatValidator
    {
        const string STRONG_PASS_VALIDATOR = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,15}$";

        public StrongPasswordValidator(string error = "Passwords must be strong.") : base(STRONG_PASS_VALIDATOR, error)
        {
        }

        public override ITextValidator Clone()
        {
            return new StrongPasswordValidator(m_ErrorMessage);
        }
    }

    public abstract class CustomFormatValidator : BaseAutoFormatTextValidator, IAutoFormatTextValidator
    {
        #region Private Fields

        protected string m_ErrorMessage;
        protected string m_ValidatorFormat = "";

        public CustomFormatValidator(string validatorFormat, string errorMessage = "Format is invalid")
        {
            m_ErrorMessage = errorMessage;
            m_ValidatorFormat = validatorFormat == null ? string.Empty : validatorFormat;
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns>True if the filepath given by the text value exists, otherwise false.</returns>
        public override bool IsTextValid()
        {
            if (!string.IsNullOrEmpty(m_ValidatorFormat))
            {
                Regex regex = new Regex(m_ValidatorFormat);
                Match match = regex.Match(m_MaterialInputField.text);
                if (!match.Success)
                {
                    m_MaterialInputField.validationText.SetGraphicText(m_ErrorMessage);
                    return false;
                }
            }

            return true;
        }

        public override bool FormatText()
        {
            var lastValidIndex = m_MaterialInputField != null ? m_MaterialInputField.caretPosition : -1;
            var formattedText = m_MaterialInputField == null || string.IsNullOrEmpty(m_MaterialInputField.text) ? string.Empty : CalculateFormattedText(m_MaterialInputField.text, ref lastValidIndex);
            if (m_MaterialInputField != null && formattedText != m_MaterialInputField.text)
            {
                UnregisterEvents();
                m_MaterialInputField.text = formattedText;
                m_MaterialInputField.caretPosition = Mathf.Clamp(lastValidIndex, 0, formattedText.Length);
                RegisterEvents();

                return true;
            }
            return false;
        }

        public abstract ITextValidator Clone();

        #endregion

        #region Mask Functions

        protected virtual string CalculateFormattedText(string text, ref int caretPosition)
        {
            return text;
        }

        protected string ApplyMask(string text, string mask, char[] specialChars, char[] validTextChars, ref int caretPosition)
        {
            return ApplyMask(text, mask, specialChars != null ? new string(specialChars) : string.Empty, validTextChars != null ? new string(validTextChars) : string.Empty, ref caretPosition);
        }

        protected string ApplyMask(string text, string mask, string maskSpecialChars, string validTextChars, ref int caretPosition)
        {
            if (text == null)
                text = string.Empty;
            if (string.IsNullOrEmpty(mask))
                return text;

            char[] maskChars = mask.ToCharArray();

            //Calculate valid text chars nad converted caret position
            List<char> textChars = new List<char>();
            var lastValidIndex = -1;
            var convertedCaretPosition = -1;
            for (int i = 0; i < text.Length; i++)
            {
                char sh = text[i];

                if (string.IsNullOrEmpty(validTextChars) || Regex.IsMatch(sh.ToString(), "[" + validTextChars + "]"))
                {
                    textChars.Add(sh);
                    lastValidIndex = textChars.Count - 1;
                }
                if (caretPosition - 1 == i)
                    convertedCaretPosition = lastValidIndex;
            }

            //Empty Text
            if (textChars.Count == 0)
            {
                caretPosition = 0;
                return "";
            }

            //Run on all elements of the mark
            for (int i = 0, j = 0; i < maskChars.Length; i++)
            {
                char ch = maskChars[i];

                if (string.IsNullOrEmpty(maskSpecialChars) || Regex.IsMatch(ch.ToString(), "[" + maskSpecialChars + "]"))
                {
                    if (j < textChars.Count)
                    {

                        char sh = textChars[j]; //j < textChars.Count ? textChars[j] : maskChars[i];

                        if (convertedCaretPosition == j)
                            caretPosition = i + 1;

                        //Replace character of original text to the mask char text
                        maskChars[i] = sh;
                        j++;
                        continue;
                    }
                }

                //Amount of special chars invalid
                if (j >= textChars.Count)
                {
                    var subConvertedText = i <= 0 ? string.Empty : new string(maskChars, 0, i);
                    caretPosition = Mathf.Clamp(caretPosition, 0, subConvertedText.Length);
                    return subConvertedText;
                }
            }

            //Finished
            var convertedText = new string(maskChars).Trim();
            caretPosition = Mathf.Clamp(caretPosition, 0, convertedText.Length);

            return convertedText;
        }

        #endregion
    }
}