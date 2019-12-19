//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    #region Helper Classes

    public enum VectorImageSizeMode
    {
        Manual,
        MatchWidth,
        MatchHeight,
        MatchMin,
        MatchMax
    }

    public interface IVectorImage
    {
        VectorImageSizeMode sizeMode
        {
            get;
            set;
        }

        float size
        {
            get;
            set;
        }

        VectorImageData vectorImageData
        {
            get;
            set;
        }

        GameObject gameObject
        {
            get;
        }

        RectTransform rectTransform
        {
            get;
        }

        string name
        {
            get;
            set;
        }

        bool SupportTMProFont();
        bool SupportUnityFont();
        void Refresh();
    }

    #endregion

    //[ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Vector Image", 50)]
    public class VectorImage : Text, IVectorImage
    {
        #region Private Variables

        [SerializeField]
        private float m_Size = 48;
        [SerializeField]
        private float m_ScaledSize = 0;
        [SerializeField]
        private VectorImageSizeMode m_SizeMode = VectorImageSizeMode.MatchMin;
        [SerializeField]
        private MaterialUIScaler m_MaterialUiScaler = null;
        [SerializeField]
        private VectorImageData m_VectorImageData = new VectorImageData();

        private float m_LocalScaleFactor;
        private DrivenRectTransformTracker m_Tracker = new DrivenRectTransformTracker();

        #endregion

        #region Public Properties

        public float size
        {
            get { return m_Size; }
            set
            {
                m_Size = value;
                RefreshScale();
            }
        }
        public float scaledSize
        {
            get { return m_ScaledSize; }
        }

        public VectorImageSizeMode sizeMode
        {
            get { return m_SizeMode; }
            set
            {
                m_SizeMode = value;
                m_Tracker.Clear();
                RefreshScale();
                SetLayoutDirty();
            }
        }
        public MaterialUIScaler materialUiScaler
        {
            get
            {
                if (m_MaterialUiScaler == null)
                {
                    m_MaterialUiScaler = MaterialUIScaler.GetRootScaler(transform);
                }
                return m_MaterialUiScaler;
            }
        }
        public VectorImageData vectorImageData
        {
            get { return m_VectorImageData; }
            set
            {
                m_VectorImageData = value;
                updateFontAndText();

                RefreshScale();

#if UNITY_EDITOR
                EditorUtility.SetDirty(gameObject);
#endif
            }
        }

        public Font glyphFont
        {
            get
            {
                return vectorImageData != null ? vectorImageData.font : null;
            }
            set
            {
                if (vectorImageData == null || vectorImageData.font == value)
                    return;

                vectorImageData.vectorFont = (VectorImageFont) value;
                updateFontAndText();
            }
        }

        public VectorImageFont glyphVectorFont
        {
            get
            {
                return vectorImageData != null ? vectorImageData.vectorFont : null;
            }
            set
            {
                if (vectorImageData == null || vectorImageData.vectorFont == value)
                    return;

                vectorImageData.vectorFont = value;
                updateFontAndText();
            }
        }


        public bool isUsingBold
        {
            get
            {
                return fontStyle == FontStyle.Bold || fontStyle == FontStyle.BoldAndItalic;

            }
            set
            {
                var oldValue = fontStyle == FontStyle.Bold || fontStyle == FontStyle.BoldAndItalic;
                if (oldValue == value)
                    return;

                if (value)
                {
                    if (fontStyle == FontStyle.Italic)
                        fontStyle = FontStyle.BoldAndItalic;
                    else if (fontStyle == FontStyle.Normal)
                        fontStyle = FontStyle.Bold;
                }
                else
                {
                    if (fontStyle == FontStyle.BoldAndItalic)
                        fontStyle = FontStyle.Italic;
                    else if (fontStyle == FontStyle.Bold)
                        fontStyle = FontStyle.Normal;
                }
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();

            alignment = TextAnchor.MiddleCenter;
            horizontalOverflow = HorizontalWrapMode.Overflow;
            verticalOverflow = VerticalWrapMode.Overflow;

            updateFontAndText();
            RefreshScale();

            SetAllDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_Tracker.Clear();
        }

        protected override void Start()
        {
            alignment = TextAnchor.MiddleCenter;
            horizontalOverflow = HorizontalWrapMode.Overflow;
            verticalOverflow = VerticalWrapMode.Overflow;

            updateFontAndText();

            SetAllDirty();
            UpdateMaterial();
            UpdateGeometry();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_Tracker.Clear();
            RefreshScale();
            base.OnValidate();
            SetLayoutDirty();

            updateFontAndText();
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            RefreshScale();
        }

        #endregion

        #region TextUnicode

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (fontSize == 0)
            {
                toFill.Clear();
                return;
            }

            base.OnPopulateMesh(toFill);
        }

        #endregion

        #region Public Functions

        public void Refresh()
        {
            updateFontAndText();
            RefreshScale();

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }

        public bool SupportUnityFont()
        {
            return true;
        }

        public bool SupportTMProFont()
        {
            return false;
        }

        #endregion

        #region Internal Helper Functions

        private void updateFontAndText()
        {
            if (vectorImageData != null)
            {
                font = vectorImageData.font;
                text = IconDecoder.Decode(vectorImageData.glyph.unicode);//vectorImageData.glyph.unicode;
            }
        }

        private void RefreshScale()
        {
#if UNITY_EDITOR
            //if (gameObject.IsPrefabInstance()) return;
            if (!gameObject.scene.IsValid()) return;
#endif

            if (materialUiScaler == null) // When instantiating the icon for the first time
            {
                return;
            }

            if (!enabled) return;

            if (size == 0 && sizeMode == VectorImageSizeMode.Manual)
            {
                fontSize = 0;
                return;
            }

            float tempSize = size;

            if (sizeMode == VectorImageSizeMode.Manual)
            {
                m_ScaledSize = tempSize * materialUiScaler.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchWidth)
            {
                m_ScaledSize = rectTransform.rect.width;
                tempSize = m_ScaledSize;
                m_ScaledSize *= materialUiScaler.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchHeight)
            {
                m_ScaledSize = rectTransform.rect.height;
                tempSize = m_ScaledSize;
                m_ScaledSize *= materialUiScaler.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchMin)
            {
                Vector2 tempVector2 = new Vector2(rectTransform.rect.width, rectTransform.rect.height);

                m_ScaledSize = Mathf.Min(tempVector2.x, tempVector2.y);
                tempSize = m_ScaledSize;
                m_ScaledSize *= materialUiScaler.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchMax)
            {
                Vector2 tempVector2 = new Vector2(rectTransform.rect.width, rectTransform.rect.height);

                m_ScaledSize = Mathf.Max(tempVector2.x, tempVector2.y);
                tempSize = m_ScaledSize;
                m_ScaledSize *= materialUiScaler.scaleFactor;
            }

            if (m_ScaledSize > 500)
            {
                m_LocalScaleFactor = m_ScaledSize / 500;
            }
            else
            {
                m_LocalScaleFactor = 1f;
            }

            tempSize *= m_LocalScaleFactor;

            fontSize = Mathf.RoundToInt(tempSize);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif

            m_LocalScaleFactor *= (size / Mathf.Max(size));

            if (m_LocalScaleFactor != float.NaN && new Vector3(m_LocalScaleFactor, m_LocalScaleFactor, m_LocalScaleFactor) != rectTransform.localScale)
            {
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.Scale);
                rectTransform.localScale = new Vector3(m_LocalScaleFactor, m_LocalScaleFactor, m_LocalScaleFactor);
            }
        }

        #endregion

        #region Layout Functions

        public override void CalculateLayoutInputHorizontal()
        {
            RefreshScale();
        }

        public override void CalculateLayoutInputVertical()
        {
            RefreshScale();
        }

        public override float preferredWidth { get { return size; } }
        public override float minWidth { get { return -1; } }
        public override float flexibleWidth { get { return -1; } }
        public override float preferredHeight { get { return size; } }
        public override float minHeight { get { return -1; } }
        public override float flexibleHeight { get { return -1; } }

        #endregion
    }
}