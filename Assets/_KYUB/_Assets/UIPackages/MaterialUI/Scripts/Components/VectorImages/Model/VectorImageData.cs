//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MaterialUI
{
    [System.Serializable]
    public class VectorImageData
    {
        #region Private Variables

        [SerializeField]
        private Glyph m_Glyph = new Glyph();
        [SerializeField]
        private UnityEngine.Object m_Font = null;

#if UNITY_EDITOR
        bool _canFixFontType = true;
#endif

        #endregion

        #region Public Properties

        public Glyph glyph
        {
            get { return m_Glyph; }
            set { m_Glyph = value; }
        }
        public Font font
        {
            get
            {
#if UNITY_EDITOR
                TryFixFontType();
#endif
                if (m_Font is VectorImageFont)
                    return ((VectorImageFont)m_Font).font;

                return m_Font as Font;
            }
        }

        public TMPro.TMP_FontAsset fontTMPro
        {
            get
            {
#if UNITY_EDITOR
                TryFixFontType();
#endif
                if (m_Font is VectorImageFont)
                    return ((VectorImageFont)m_Font).fontTMPro;

                return m_Font as TMPro.TMP_FontAsset;
            }
        }

        public VectorImageFont vectorFont
        {
            get
            {
#if UNITY_EDITOR
                TryFixFontType();
#endif
                return m_Font as VectorImageFont;
            }
            set
            {
                m_Font = value;
            }
        }

        #endregion

        #region Constructors

        public VectorImageData() { }

        public VectorImageData(Glyph glyph, VectorImageFont vectorFont)
        {
            m_Glyph = glyph;
            if (!m_Glyph.unicode.StartsWith(@"\u"))
            {
                m_Glyph.unicode = @"\u" + m_Glyph.unicode;
            }

            m_Font = vectorFont;
        }

        #endregion

        #region Public Helper Functions

        public bool ContainsData()
        {
            return m_Font != null && m_Glyph != null && !string.IsNullOrEmpty(m_Glyph.name) && !string.IsNullOrEmpty(m_Glyph.unicode);
        }

        #endregion

        #region Editor Functions

#if UNITY_EDITOR
        protected void TryFixFontType()
        {
            if (_canFixFontType && m_Font != null && !(m_Font is VectorImageFont))
            {
                _canFixFontType = false;
                var correctFont = VectorImageManager.GetIconFont(m_Font.name);
                if (correctFont != null)
                    m_Font = correctFont;
            }

        }
#endif

        #endregion

        #region Equal Overrides

        public static bool operator ==(VectorImageData a, VectorImageData b)
        {
            return object.ReferenceEquals(a, b) || 
                (!object.ReferenceEquals(a, null) && 
                 !object.ReferenceEquals(b, null) &&
                 a.m_Font == b.m_Font && 
                 a.m_Glyph != null && b.m_Glyph != null && 
                 a.m_Glyph.name == b.m_Glyph.name && 
                 a.m_Glyph.unicode == b.m_Glyph.unicode);
        }

        public static bool operator !=(VectorImageData a, VectorImageData b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
                return true;
            else
            {
                if (obj is VectorImageData && ((VectorImageData)obj) == this)
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 2081640914;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(m_Glyph == null? m_Glyph.name : "");
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(m_Glyph == null? m_Glyph.unicode : "");
            hashCode = hashCode * -1521134295 + EqualityComparer<UnityEngine.Object>.Default.GetHashCode(m_Font);
            return hashCode;
        }

        #endregion
    }
}