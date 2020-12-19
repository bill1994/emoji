#if UNITY_2018_3_OR_NEWER && !UNITY_2019_1_OR_NEWER
#define TMP_1_4_0_OR_NEWER
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kyub.EmojiSearch.Utilities;

namespace Kyub.EmojiSearch.UI
{
    public class TMP_EmojiTextUGUI : TextMeshProUGUI
    {
        #region Private Fields

        [SerializeField, Tooltip("Force monospace character with (value)em.\nRequire RichText active")]
        protected float m_monospaceDistEm = 0;

        protected bool m_emojiParsingRequired = true;

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

        #region Emoji Parser Functions

        protected virtual bool ParseInputTextAndEmojiCharSequence()
        {
            m_emojiParsingRequired = false;

            //Only parse when richtext active (we need the <sprite=index> tag)
            if (m_isRichText)
            {
                var parsedEmoji = false;
                var oldText = m_text;

                /*//Parse Sprite Chars
                ParseInputText();

                //Parse Emojis
                m_text = CharBufferToString(); //Pick the char unicodes to parse the emojis
                parsedEmoji = TMP_EmojiSearchEngine.ParseEmojiCharSequence(spriteAsset, ref m_text);

                //Apply parsed Emoji to char buffer
                if (parsedEmoji)
                {
                    StringToCharArray(m_text, ref m_char_buffer);
                    SetArraySizes(m_char_buffer);
                }*/

                parsedEmoji = TMP_EmojiSearchEngine.ParseEmojiCharSequence(spriteAsset, ref m_text);

                if (m_monospaceDistEm != 0)
                    ApplyMonoSpacingValues(m_text, out m_text);

                m_emojiParsingRequired = false;
                IsInputParsingRequired_Internal = false;
                InputSource_Internal = TextInputSources.Text;

                ParseInputText();

                m_emojiParsingRequired = false;
                IsInputParsingRequired_Internal = false;

                //Debug.Log("ParseInputTextAndEmojiCharSequence");
                //We must revert the original text because we dont want to permanently change the text
                m_text = oldText;

#if !TMP_2_1_0_PREVIEW_10_OR_NEWER
                m_isCalculateSizeRequired = true;
#endif

                return parsedEmoji;
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

        #endregion

        #region Text Overriden Functions

        public override void SetVerticesDirty()
        {
            //In textmeshpro 1.4 the parameter "m_isInputParsingRequired" changed to internal, so, to dont use reflection i changed to "m_havePropertiesChanged" parameter
            if (IsInputParsingRequired_Internal)
            {
                m_emojiParsingRequired = m_isRichText;
            }
            base.SetVerticesDirty();
        }

        public override void Rebuild(CanvasUpdate update)
        {
            if (this == null && enabled && gameObject.activeInHierarchy) return;

            if (m_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            base.Rebuild(update);
        }

        public override string GetParsedText()
        {
            if (m_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.GetParsedText();
        }

        public override TMP_TextInfo GetTextInfo(string text)
        {
            TMP_EmojiSearchEngine.ParseEmojiCharSequence(spriteAsset, ref text);
            return base.GetTextInfo(text);
        }


#if TMP_2_1_0_PREVIEW_8_OR_NEWER
        protected override Vector2 CalculatePreferredValues(ref float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing, bool isWordWrappingEnabled)
        {
            if (m_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.CalculatePreferredValues(ref defaultFontSize, marginSize, ignoreTextAutoSizing, isWordWrappingEnabled);
        }
#elif TMP_2_1_0_PREVIEW_3_OR_NEWER
        protected override Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing, bool isWordWrappingEnabled)
        {
            if (m_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.CalculatePreferredValues(defaultFontSize, marginSize, ignoreTextAutoSizing, isWordWrappingEnabled);
        }
#else
        protected override Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing)
        {
            if (m_emojiParsingRequired)
                ParseInputTextAndEmojiCharSequence();

            return base.CalculatePreferredValues(defaultFontSize, marginSize, ignoreTextAutoSizing);
        }
#endif

        /*public string CharBufferToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (m_char_buffer != null)
            {
                for (int i = 0; i < m_char_buffer.Length; i++)
                {
                    var intValue = m_char_buffer[i];

                    //Not a surrogate and is valid UTF32 (conditions to use char.ConvertFromUtf32 function)
                    if (intValue > 0x000000 && intValue < 0x10ffff &&
                                        (intValue < 0x00d800 || intValue > 0x00dfff))
                    {
                        var UTF16Surrogate = char.ConvertFromUtf32(intValue);
                        if (!string.IsNullOrEmpty(UTF16Surrogate))
                        {
                            //Add chars into cache (we must include the both char paths in fastLookupPath)
                            foreach (var surrogateChar in UTF16Surrogate)
                            {
                                builder.Append(surrogateChar);
                            }
                        }
                    }
                    //Simple Append
                    else
                    {
                        builder.Append((char)intValue);
                    }
                }
            }

            return builder.ToString();
        }*/

#endregion
    }
}
