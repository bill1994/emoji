using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace Kyub.Extensions
{
    public static class StringExtensions
    {
        #region Public Functions

        public static string SplitCamelCase(this string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.CultureInvariant).Trim();
        }

        public static string ReplaceFirst(this string p_text, string p_oldText, string p_newText)
        {
            try
            {
                p_oldText = p_oldText != null ? p_oldText : "";
                p_newText = p_newText != null ? p_newText : "";
                p_text = p_text != null ? p_text : "";

                int v_pos = string.IsNullOrEmpty(p_oldText) ? -1 : p_text.IndexOf(p_oldText);
                if (v_pos < 0)
                    return p_text;
                string v_result = p_text.Substring(0, v_pos) + p_newText + p_text.Substring(v_pos + p_oldText.Length);
                return v_result;
            }
            catch { }
            return p_text;
        }

        public static string ReplaceLast(this string p_text, string p_oldText, string p_newText)
        {
            try
            {
                p_oldText = p_oldText != null ? p_oldText : "";
                p_newText = p_newText != null ? p_newText : "";
                p_text = p_text != null ? p_text : "";

                int v_pos = string.IsNullOrEmpty(p_oldText) ? -1 : p_text.LastIndexOf(p_oldText);
                if (v_pos < 0)
                    return p_text;

                string v_result = p_text.Remove(v_pos, p_oldText.Length).Insert(v_pos, p_newText);
                return v_result;
            }
            catch { }
            return p_text;
        }

        public const string REGEX_STRING_URL1 = @"((ht|f)tp(s?)\:\/\/)?www[.][0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?";
        public const string REGEX_STRING_URL2 = @"((ht|f)tp(s?)\:\/\/)[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?";
        public static bool IsUrl(this string p_string)
        {
            if (!string.IsNullOrEmpty(p_string))
            {
                if (Regex.IsMatch(p_string, REGEX_STRING_URL1) || Regex.IsMatch(p_string, REGEX_STRING_URL2))
                    return true;
            }
            return false;
        }

        public static string ToCamelCase(this string p_input)
        {
            if (p_input == null || p_input.Length < 2)
                return p_input;

            string[] words = p_input.Split(
                new char[] { },
                System.StringSplitOptions.RemoveEmptyEntries);

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var word in words)
            {
                var result = word.ToLower().FirstCharToUpper();
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(" ");
                stringBuilder.Append(result);
            }

            return stringBuilder.ToString();
        }

        public static string FirstCharToUpper(this string p_input)
        {
            if (p_input == null)
                return "";

            if (p_input.Length > 1)
                return char.ToUpper(p_input[0]) + p_input.Substring(1);

            return p_input.ToUpper();
        }

        public static bool IsEmail(this string emailAddress)
        {
            bool isValid = s_validEmailRegex.IsMatch(emailAddress);

            return isValid;
        }

        public static string RemoveDiacritics(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        #endregion

        #region Internal Helper Functions

        static Regex s_validEmailRegex = CreateValidEmailRegex();
        private static Regex CreateValidEmailRegex()
        {
            string validEmailPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

            return new Regex(validEmailPattern, RegexOptions.IgnoreCase);
        }

        #endregion
    }
}
