using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MaterialUI
{
    [CreateAssetMenu(fileName = "VectorImageFont", menuName = "MaterialUI/Vector Image Font")]
    public class VectorImageFont : ScriptableObject
    {
        #region Consts

        public const string VECTOR_FONT_SUFIX = "_VectorFont";

        #endregion

        #region Private Variables

        [SerializeField]
        Font m_Font = null;
        [SerializeField]
        TMPro.TMP_FontAsset m_FontTMPro = null;
        [SerializeField, HideInInspector]
        VectorImageSet m_IconSet = new VectorImageSet();

#if UNITY_EDITOR
        [SerializeField]
        TextAsset m_ImageSetTextAsset = null;
#endif

#if UNITY_EDITOR
        [System.NonSerialized] bool _isFirstValidate = true;
        [System.NonSerialized] Font _previousFont = null;
        [System.NonSerialized] TMPro.TMP_FontAsset _previousFontTmpPro = null;
        [System.NonSerialized] TextAsset _previousImageSetTextAsset = null;
#endif

        #endregion

        #region Properties

        public Font font
        {
            get
            {
                return m_Font;
            }
        }

        public TMP_FontAsset fontTMPro
        {
            get
            {
                return m_FontTMPro;
            }
        }

        public string FontName
        {
            get
            {
                if (m_Font != null)
                    return m_Font.name;
                else if (m_FontTMPro != null)
                {
#if UNITY_2018_3_OR_NEWER
                    return m_FontTMPro.faceInfo.familyName;
#else
                    return m_FontTMPro.fontInfo.Name;
#endif
                }
                else
                    return name.Replace(VECTOR_FONT_SUFIX, "");
            }
        }

        public VectorImageSet iconSet
        {
            get
            {
                return m_IconSet;
            }
        }

        #endregion

        #region Unity Functions

#if UNITY_EDITOR

        protected virtual void OnValidate()
        {
            //Force pick references on first validate (prevent recalculate every time that unity compiles)
            if (_isFirstValidate)
            {
                _isFirstValidate = false;
                _previousFont = m_Font;
                _previousFontTmpPro = m_FontTMPro;
                _previousImageSetTextAsset = m_ImageSetTextAsset;
            }


            //Validate Name
            if (_previousFontTmpPro != null || _previousFont != null)
            {
                var fontName = _previousFont != null ? _previousFont.name :
#if UNITY_2018_3_OR_NEWER
                    (_previousFontTmpPro != null ? _previousFontTmpPro.faceInfo.familyName :
#else
                    (_previousFontTmpPro != null ? _previousFontTmpPro.fontInfo.Name :
#endif
                    name);
                if (!fontName.EndsWith(VECTOR_FONT_SUFIX))
                    fontName += VECTOR_FONT_SUFIX;
                if (fontName != name)
                {
                    name = fontName;
                    var path = UnityEditor.AssetDatabase.GetAssetPath(this);
                    if (!string.IsNullOrEmpty(path))
                    {
                        UnityEditor.AssetDatabase.RenameAsset(path, name);
                        UnityEditor.AssetDatabase.Refresh();
                    }
                }
            }

            //Try recalculate if invalid
            var v_fontChaged = false;
            if (_previousFont != m_Font || _previousFontTmpPro != m_FontTMPro)
            {
                _previousFont = m_Font;
                _previousFontTmpPro = m_FontTMPro;
                v_fontChaged = true;
            }
            if (_previousImageSetTextAsset != m_ImageSetTextAsset || (m_ImageSetTextAsset == null && v_fontChaged))
            {
                _previousImageSetTextAsset = m_ImageSetTextAsset;
                RecreateIconSet();
            }
        }
#endif

        #endregion

        #region Public Functions

        public Glyph GetGlyphByName(string iconName)
        {
            if (iconName == null)
                iconName = "";
            iconName = iconName.ToLower();
            var unicodeIconName = !iconName.StartsWith(@"\u") ? (@"\u" + iconName) : iconName;
            foreach (var glyph in m_IconSet.iconGlyphList)
            {
                var glyphName = glyph.name.ToLower();
                if (glyphName.Equals(iconName) || glyphName.Equals(unicodeIconName))
                    return glyph;
            }
            return null;
        }

        public Glyph GetGlyphByUnicode(string unicode)
        {
            unicode = !unicode.StartsWith(@"\u") ? (@"\u" + unicode) : unicode;
            foreach (var glyph in m_IconSet.iconGlyphList)
            {

                var glyphUnicode = !glyph.unicode.StartsWith(@"\u") ? (@"\u" + glyph.unicode.ToLower()) : glyph.unicode.ToLower();
                if (glyphUnicode.Equals(unicode))
                    return glyph;
            }
            return null;
        }

        public bool SupportTMProFont()
        {
            return m_FontTMPro != null;
        }

        public bool SupportUnityFont()
        {
            return m_Font != null;
        }

        #endregion

        #region Editor Helper Functions

#if UNITY_EDITOR
        [ContextMenu("RecreateIconSet")]
        protected virtual void RecreateIconSet()
        {
            m_IconSet = new VectorImageSet();
            var v_needAutoCreate = true;
            try
            {
                if (m_ImageSetTextAsset != null)
                {
                    JsonUtility.FromJsonOverwrite(m_ImageSetTextAsset.text, m_IconSet);
                    v_needAutoCreate = false;
                }
            }
            catch { }

            if (v_needAutoCreate)
            {
                if (m_Font != null)
                {
                    CharacterInfo[] v_charInfos = PickAllCharInfos(m_Font);
                    foreach (var v_charInfo in v_charInfos)
                    {
                        var unicode = v_charInfo.index.ToString();
                        if (!unicode.StartsWith(@"\u"))
                            unicode = @"\u" + unicode;

                        m_IconSet.iconGlyphList.Add(new Glyph(unicode, unicode, true));
                    }
                }
                else if (m_FontTMPro != null)
                {
                    HashSet<uint> v_addedGlyphs = new HashSet<uint>();
                    List<TMP_FontAsset> v_fontsToCheckIcons = new List<TMP_FontAsset>();
                    v_fontsToCheckIcons.Add(m_FontTMPro);

                    for (int i = 0; i < v_fontsToCheckIcons.Count; i++)
                    {
                        var currentFont = v_fontsToCheckIcons[i];
#if UNITY_2018_3_OR_NEWER
                        foreach (var v_pair in currentFont.characterLookupTable)
#else
                        foreach (var v_pair in currentFont.characterDictionary)
#endif
                        {
                            var keyUInt = (uint)v_pair.Key;
                            if (!v_addedGlyphs.Contains(keyUInt))
                            {
                                v_addedGlyphs.Add(keyUInt);
                                var unicode = v_pair.Key.ToString();
                                if (!unicode.StartsWith(@"\u"))
                                    unicode = @"\u" + unicode;

                                m_IconSet.iconGlyphList.Add(new Glyph(unicode, unicode, true));
                            }
                        }
#if UNITY_2018_3_OR_NEWER
                        foreach (var v_fallbackFont in currentFont.fallbackFontAssetTable)
#else
                        foreach (var v_fallbackFont in currentFont.fallbackFontAssets)
#endif
                        {
                            if (v_fallbackFont != null && !v_fontsToCheckIcons.Contains(v_fallbackFont))
                                v_fontsToCheckIcons.Add(v_fallbackFont);
                        }
                    }
                }
            }
        }

        public static CharacterInfo[] PickAllCharInfos(Font p_font)
        {
            CharacterInfo[] v_chars = new CharacterInfo[0];
            if (p_font != null)
            {
                UnityEditor.TrueTypeFontImporter v_fontReimporter = null;

                //A GLITCH: Unity's Font.CharacterInfo doesn't work
                //properly on dynamic mode, we need to change it to Unicode first
                if (p_font.dynamic)
                {
                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(p_font);
                    v_fontReimporter = (UnityEditor.TrueTypeFontImporter)UnityEditor.AssetImporter.GetAtPath(assetPath);

                    v_fontReimporter.fontTextureCase = UnityEditor.FontTextureCase.Unicode;
                    v_fontReimporter.SaveAndReimport();
                }

                //Only Non-Dynamic Fonts define the characterInfo array
                v_chars = p_font.characterInfo;

                // Change back to dynamic font
                if (v_fontReimporter != null)
                {
                    v_fontReimporter.fontTextureCase = UnityEditor.FontTextureCase.Dynamic;
                    v_fontReimporter.SaveAndReimport();
                }
            }
            return v_chars;
        }
#endif

        #endregion

        #region Conversors

        public static implicit operator Font(VectorImageFont vectorFont)
        {
            return vectorFont != null ? vectorFont.font : null;
        }

        public static implicit operator TMPro.TMP_FontAsset(VectorImageFont vectorFont)
        {
            return vectorFont != null ? vectorFont.fontTMPro : null;
        }

        public static explicit operator VectorImageFont(Font font)
        {
            return new VectorImageFont() { m_Font = font };
        }

        public static explicit operator VectorImageFont(TMPro.TMP_FontAsset fontTMPro)
        {
            return new VectorImageFont() { m_FontTMPro = fontTMPro };
        }

        #endregion
    }
}
