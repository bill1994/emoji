﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kyub.Localization;
using TMPro;

namespace Kyub.Localization.UI
{
    public class TMP_LocaleTextUGUI : TextMeshProUGUI
    {
        #region Private Fields

        [SerializeField, Tooltip("Force monospace character with (value)em.\nRequire RichText active")]
        protected float m_monospaceDistEm = 0;
        [SerializeField]
        protected bool m_isLocalized = true;
        [Tooltip("support use of:\n *<locale>...</locale> to localize part of text instead of the full text.\n" +
                                " *<skiplocale> in begining of the text force ignore locale in this object (will clear locale tags before return).\n" +
                                " *<localeparam=number>...</localeparam> replace the text by {number} before localize.\n" +
                                "ex: 'my text <localeparam=15>parameter value</localeparam>' will be localized as 'my text {15}' and after the value {15} will be replaced back")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_supportLocaleTag")]
        protected bool m_supportLocaleRichTextTags = false;

        protected bool _localeParsingRequired = true;

        protected string _lastLocalizedLanguage = "";

        #endregion

        #region Properties

        public float MonospaceDistEm
        {
            get
            {
                return m_monospaceDistEm;
            }
            set
            {
                if (m_monospaceDistEm == value)
                    return;
                m_monospaceDistEm = value;
                SetVerticesDirty();
            }
        }

        public bool SupportLocaleRichTextTags
        {
            get
            {
                return m_supportLocaleRichTextTags;
            }
            set
            {
                if (m_supportLocaleRichTextTags == value)
                    return;
                m_supportLocaleRichTextTags = value;
                if (m_isLocalized)
                    SetVerticesDirty();
            }
        }

        public bool IsLocalized
        {
            get
            {
                return m_isLocalized;
            }
            set
            {
                if (m_isLocalized == value)
                    return;
                m_isLocalized = value;
                SetVerticesDirty();
            }
        }

#if TMP_1_4_0_OR_NEWER
        protected System.Reflection.FieldInfo _isInputParsingRequired_Field = null;
#endif
        protected internal bool IsInputParsingRequired_Internal
        {
            get
            {
#if TMP_1_4_0_OR_NEWER
                if (_isInputParsingRequired_Field == null)
                    _isInputParsingRequired_Field = typeof(TMP_Text).GetField("m_isInputParsingRequired", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_isInputParsingRequired_Field != null)
                    return (bool)_isInputParsingRequired_Field.GetValue(this);
                else
                    return false;
#else
                return m_isInputParsingRequired;
#endif
            }
            protected set
            {
#if TMP_1_4_0_OR_NEWER
                if (_isInputParsingRequired_Field == null)
                    _isInputParsingRequired_Field = typeof(TMP_Text).GetField("m_isInputParsingRequired", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_isInputParsingRequired_Field != null)
                    _isInputParsingRequired_Field.SetValue(this, value);
#else
                m_isInputParsingRequired = value;
#endif
            }
        }

#if TMP_1_4_0_OR_NEWER
        protected enum TextInputSources { Text = 0, SetText = 1, SetCharArray = 2, String = 3 };
        protected System.Reflection.FieldInfo _inputSource_Field = null;
        protected System.Type _textInputSources_Type = null;
#endif
        protected TextInputSources InputSource_Internal
        {
            get
            {
#if TMP_1_4_0_OR_NEWER
                if (_inputSource_Field == null)
                    _inputSource_Field = typeof(TMP_Text).GetField("m_inputSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_inputSource_Field != null)
                    return (TextInputSources)System.Enum.ToObject(typeof(TextInputSources), (int)_inputSource_Field.GetValue(this));
                else
                    return TextInputSources.Text;
#else
                return m_inputSource;
#endif
            }
            set
            {
#if TMP_1_4_0_OR_NEWER

                if (_inputSource_Field == null)
                    _inputSource_Field = typeof(TMP_Text).GetField("m_inputSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (_inputSource_Field != null)
                {
                    //Pick the Type of internal enum to set back in TMP_Text
                    if (_textInputSources_Type == null)
                        _textInputSources_Type = typeof(TMP_Text).GetNestedType("TextInputSources", System.Reflection.BindingFlags.NonPublic);
                    if (_textInputSources_Type != null)
                    {
                        _inputSource_Field.SetValue(this, System.Enum.ToObject(_textInputSources_Type, (int)value));
                    }
                }

#else
                m_inputSource = value;
#endif
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            RegisterEvents();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            UnregisterEvents();
            base.OnDisable();
        }

        #endregion

        #region Locale Parser Functions

        protected virtual bool ParseInputTextAndLocalizeTags()
        {
            _localeParsingRequired = false;

            //Only parse when richtext active (we need the <sprite=index> tag)
            if (LocaleManager.InstanceExists() || !Application.isPlaying || (m_supportLocaleRichTextTags && m_isRichText && !m_isLocalized) || (m_monospaceDistEm != 0 && m_isRichText))
            {
                var parsedLocale = false;
                var oldText = m_text;

                if (IsInputParsingRequired_Internal)
                    m_text = m_text != null ? m_text.Replace("\n", "\\n").Replace("\r", "") : null;

                parsedLocale = !Application.isPlaying || (m_supportLocaleRichTextTags && m_isRichText && !m_isLocalized) ?
                    TryClearLocaleTags(m_text, out m_text) :
                    TryGetLocalizedText(m_text, out m_text);

                if (m_monospaceDistEm != 0)
                    ApplyMonoSpacingValues(m_text, out m_text);

                _localeParsingRequired = false;
                IsInputParsingRequired_Internal = false;
                InputSource_Internal = TextInputSources.Text;

                ParseInputText();

                _localeParsingRequired = false;
                IsInputParsingRequired_Internal = false;

                //Debug.Log("ParseInputTextAndEmojiCharSequence");
                //We must revert the original text because we dont want to permanently change the text
                m_text = oldText;

#if !TMP_2_1_0_PREVIEW_10_OR_NEWER
                m_isCalculateSizeRequired = true;
#endif

                return parsedLocale;
            }

            return false;
        }

        protected virtual bool ApplyMonoSpacingValues(string text, out string outText)
        {
            bool sucess = false;

            if (m_monospaceDistEm != 0)
            {
                outText = "<mspace=" + m_monospaceDistEm.ToString(System.Globalization.CultureInfo.InvariantCulture) + "em>" + text;
                sucess = true;
            }
            else
                outText = text;

            return sucess;
        }

        protected bool TryClearLocaleTags(string text, out string outText)
        {
            bool sucess = false;

            if (m_supportLocaleRichTextTags && m_isRichText)
            {
                outText = Kyub.RegexUtils.BulkReplace(text, LocaleManager.s_localeClearTagsDict);
                if (text != outText)
                    sucess = true;
            }
            else
                outText = text;

            return sucess;
        }

        protected bool TryGetLocalizedText(string text, out string localizedValue)
        {
            bool sucess = false;
            if (m_isLocalized && !string.IsNullOrEmpty(text) && LocaleManager.InstanceExists())
            {
                sucess = LocaleManager.TryGetLocalizedText(text, out localizedValue, m_supportLocaleRichTextTags && m_isRichText);

                //Return Key value if localization is empty
                if (!sucess)
                    localizedValue = text;

                _lastLocalizedLanguage = LocaleManager.Instance.CurrentLanguage;
            }
            else
                localizedValue = text != null ? text : "";

            return sucess;
        }

        #endregion

        #region Text Overriden Functions

        public override void SetVerticesDirty()
        {
            //In textmeshpro 1.4 the parameter "m_isInputParsingRequired" changed to internal, so, to dont use reflection i changed to "m_havePropertiesChanged" parameter
            if (IsInputParsingRequired_Internal)
            {
                _localeParsingRequired = m_isLocalized || (m_supportLocaleRichTextTags && m_isRichText) || (m_monospaceDistEm != 0 && m_isRichText);
            }
            base.SetVerticesDirty();
        }

        public override void Rebuild(CanvasUpdate update)
        {
            if (this == null && enabled && gameObject.activeInHierarchy) return;

            if (_localeParsingRequired)
                ParseInputTextAndLocalizeTags();

            base.Rebuild(update);
        }

        public override string GetParsedText()
        {
            if (_localeParsingRequired)
                ParseInputTextAndLocalizeTags();

            return base.GetParsedText();
        }

        public override TMP_TextInfo GetTextInfo(string text)
        {
            if (!Application.isPlaying || (m_supportLocaleRichTextTags && m_isRichText && !m_isLocalized))
                TryClearLocaleTags(text, out text);
            else
                TryGetLocalizedText(text, out text);
            return base.GetTextInfo(text);
        }

#if TMP_2_1_0_PREVIEW_8_OR_NEWER
        protected override Vector2 CalculatePreferredValues(ref float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing, bool isWordWrappingEnabled)
        {
            if (_localeParsingRequired)
                ParseInputTextAndLocalizeTags();

            return base.CalculatePreferredValues(ref defaultFontSize, marginSize, ignoreTextAutoSizing, isWordWrappingEnabled);
        }
#elif TMP_2_1_0_PREVIEW_3_OR_NEWER
        protected override Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing, bool isWordWrappingEnabled)
        {
            if (_localeParsingRequired)
                ParseInputTextAndLocalizeTags();

            return base.CalculatePreferredValues(defaultFontSize, marginSize, ignoreTextAutoSizing, isWordWrappingEnabled);
        }
#else
        protected override Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing)
        {
            if (_localeParsingRequired)
                ParseInputTextAndLocalizeTags();

            return base.CalculatePreferredValues(defaultFontSize, marginSize, ignoreTextAutoSizing);
        }
#endif

        #endregion

        #region Layout Overriden Functions

#if TMP_2_1_3_OR_NEWER
        public override float preferredWidth { get { m_preferredWidth = GetPreferredWidth(); return m_preferredWidth; } }
        public override float preferredHeight { get { m_preferredHeight = GetPreferredHeight(); return m_preferredHeight; } }

        //NOTE: In Version 2.1.3 or newer we detected an incorrect behaviour in GetPreferredWidth() and GetPreferredHeight() Logic.
        //Fixed this logic creating a "new" version of this funcion that override old behaviour to the correct one
        protected new float GetPreferredWidth()
        {
            if (TMP_Settings.instance == null) return 0;

            // Return cached preferred height if already computed
            if (!m_isPreferredWidthDirty)
                return m_preferredWidth;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            // Set Margins to Infinity
            Vector2 margin = k_LargePositiveVector2;

            if (IsInputParsingRequired_Internal || m_isTextTruncated)
            {
                m_isCalculatingPreferredValues = true;
                ParseInputText();

                _localeParsingRequired = m_isLocalized || (m_supportLocaleRichTextTags && m_isRichText) || (m_monospaceDistEm != 0 && m_isRichText);
                if (_localeParsingRequired)
                    ParseInputTextAndLocalizeTags();
            }

            m_AutoSizeIterationCount = 0;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, false, false).x;

            m_isPreferredWidthDirty = false;

            //Debug.Log("GetPreferredWidth() called on Object ID: " + GetInstanceID() + " on frame: " + Time.frameCount + ". Returning width of " + preferredWidth);

            return preferredWidth;
        }

        protected new float GetPreferredHeight()
        {
            if (TMP_Settings.instance == null) return 0;

            // Return cached preferred height if already computed
            if (!m_isPreferredHeightDirty)
                return m_preferredHeight;

            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Reset auto sizing point size bounds
            m_minFontSize = m_fontSizeMin;
            m_maxFontSize = m_fontSizeMax;
            m_charWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_marginWidth != 0 ? m_marginWidth : k_LargePositiveFloat, k_LargePositiveFloat);

            if (IsInputParsingRequired_Internal || m_isTextTruncated)
            {
                m_isCalculatingPreferredValues = true;
                ParseInputText();

                _localeParsingRequired = m_isLocalized || (m_supportLocaleRichTextTags && m_isRichText) || (m_monospaceDistEm != 0 && m_isRichText);
                if (_localeParsingRequired)
                    ParseInputTextAndLocalizeTags();
            }

            // Reset Text Auto Size iteration tracking.
            m_IsAutoSizePointSizeSet = false;
            m_AutoSizeIterationCount = 0;

            // The CalculatePreferredValues function is potentially called repeatedly when text auto size is enabled.
            // This is a revised implementation to remove the use of recursion which could potentially result in stack overflow issues.
            float preferredHeight = 0;

            while (m_IsAutoSizePointSizeSet == false)
            {
                preferredHeight = CalculatePreferredValues(ref fontSize, margin, m_enableAutoSizing, m_enableWordWrapping).y;
                m_AutoSizeIterationCount += 1;
            }

            m_isPreferredHeightDirty = false;

            //Debug.Log("GetPreferredHeight() called on Object ID: " + GetInstanceID() + " on frame: " + Time.frameCount +". Returning height of " + preferredHeight);

            return preferredHeight;
        }
#endif

        #endregion

        #region Register Functions

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();

            LocaleManager.OnLocalizeChanged += HandleOnLocalize;
        }

        protected virtual void UnregisterEvents()
        {
            LocaleManager.OnLocalizeChanged -= HandleOnLocalize;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnLocalize(bool forceReapply)
        {
            if (LocaleManager.InstanceExists() && m_isLocalized && (forceReapply || !string.Equals(_lastLocalizedLanguage, LocaleManager.Instance.CurrentLanguage)))
            {
                //Invalidate Text
                SetText(m_text);
            }
        }

        #endregion
    }
}
