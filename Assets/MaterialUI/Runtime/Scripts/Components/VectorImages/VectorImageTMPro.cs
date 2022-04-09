// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaterialUI
{
    //[ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Vector Image (TextMeshPro)", 50)]
    public class VectorImageTMPro : TextMeshProUGUI, IVectorImage, ILayoutIgnorer
    {
        #region Private Variables

        [SerializeField]
        private float m_Size = 48;
        //[SerializeField]
        //private float m_ScaledSize = 0;
        [SerializeField]
        private VectorImageSizeMode m_SizeMode = VectorImageSizeMode.MatchMin;
        [SerializeField]
        private VectorImageData m_VectorImageData = new VectorImageData();
        [SerializeField, Tooltip("Keep in layout calculation when VectorImageData is Empty?"), UnityEngine.Serialization.FormerlySerializedAs("m_KeepSizeWhenEmpty")]
        bool m_IncludeInLayoutWhenEmpty = true;

        private Canvas _RootCanvas = null;
        //private float _LocalScaleFactor = 1f;
        //private DrivenRectTransformTracker m_Tracker = new DrivenRectTransformTracker();

        #endregion

        #region Public Properties

        public bool includeInLayoutWhenEmpty
        {
            get { return m_IncludeInLayoutWhenEmpty; }
            set
            {
                m_IncludeInLayoutWhenEmpty = value;
                RefreshScale();
            }
        }

        public float size
        {
            get { return m_Size; }
            set
            {
                m_Size = value;
                RefreshScale();
            }
        }

        /*public float scaledSize
        {
            get { return m_ScaledSize; }
        }*/

        public VectorImageSizeMode sizeMode
        {
            get { return m_SizeMode; }
            set
            {
                m_SizeMode = value;
                //m_Tracker.Clear();
                RefreshScale();
                SetLayoutDirty();
            }
        }

        public Canvas rootCanvas
        {
            get
            {
                if (_RootCanvas == null)
                {
                    _RootCanvas = transform.GetRootCanvas();
                }
                return _RootCanvas;
            }
        }

        public VectorImageData vectorImageData
        {
            get { return m_VectorImageData; }
            set
            {
                m_VectorImageData = value;
                UpdateFontAndText();

                RefreshScale();

#if UNITY_EDITOR
                EditorUtility.SetDirty(gameObject);
#endif
            }
        }

        public TMP_FontAsset glyphFont
        {
            get
            {
                return vectorImageData != null ? vectorImageData.fontTMPro : null;
            }
            set
            {
                if (vectorImageData == null || vectorImageData.fontTMPro == value)
                    return;

                vectorImageData.vectorFont = (VectorImageFont)value;
                UpdateFontAndText();
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
                UpdateFontAndText();
            }
        }


        public new bool isUsingBold
        {
            get
            {
                return (fontStyle & FontStyles.Bold) == FontStyles.Bold;

            }
            set
            {
                if (value)
                    fontStyle |= FontStyles.Bold;
                else
                    fontStyle &= (~FontStyles.Bold);
            }
        }

        #endregion

        #region Public Functions

        public void Refresh()
        {
            UpdateFontAndText();
            RefreshScale();

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }

        public bool SupportUnityFont()
        {
            return false;
        }

        public bool SupportTMProFont()
        {
            return true;
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();

            alignment = TextAlignmentOptions.Center;
            overflowMode = TextOverflowModes.Overflow;

            UpdateFontAndText();
            RefreshScale();

            SetAllDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            //m_Tracker.Clear();
        }

        protected override void Start()
        {
            alignment = TextAlignmentOptions.Center;
            overflowMode = TextOverflowModes.Overflow;

            UpdateFontAndText();

            SetAllDirty();
            UpdateMaterial();
            UpdateGeometry();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            //m_Tracker.Clear();
            RefreshScale();
            base.OnValidate();
            SetLayoutDirty();

            UpdateFontAndText();
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

        #region Internal Helper Functions

        private void UpdateFontAndText()
        {
            font = vectorImageData != null ? vectorImageData.fontTMPro : null;
            text = vectorImageData != null ? IconDecoder.Decode(vectorImageData.glyph.unicode) : string.Empty;
        }

        private void RefreshScale()
        {
#if UNITY_EDITOR
            //if (gameObject.IsPrefabInstance()) return;
            if (!gameObject.scene.IsValid()) return;
#endif

            if (rootCanvas == null) // When instantiating the icon for the first time
            {
                //m_ScaledSize = size;
                fontSize = Mathf.RoundToInt(size);
                return;
            }

            if (!enabled) return;

            if (size == 0 && sizeMode == VectorImageSizeMode.Manual)
            {
                //m_ScaledSize = 0;
                fontSize = 0;
                return;
            }

            float tempSize = size;

            if (sizeMode == VectorImageSizeMode.Manual)
            {
                //m_ScaledSize = tempSize * rootCanvas.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchWidth)
            {
                tempSize = rectTransform.rect.width;
                //m_ScaledSize = rectTransform.rect.width;
                //tempSize = m_ScaledSize;
                //m_ScaledSize *= rootCanvas.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchHeight)
            {
                tempSize = rectTransform.rect.height;
                //m_ScaledSize = rectTransform.rect.height;
                //tempSize = m_ScaledSize;
                //m_ScaledSize *= rootCanvas.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchMin)
            {
                Vector2 tempVector2 = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
                tempSize = Mathf.Min(tempVector2.x, tempVector2.y);

                //m_ScaledSize = Mathf.Min(tempVector2.x, tempVector2.y);
                //tempSize = m_ScaledSize;
                //m_ScaledSize *= rootCanvas.scaleFactor;
            }
            else if (sizeMode == VectorImageSizeMode.MatchMax)
            {
                Vector2 tempVector2 = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
                tempSize = Mathf.Max(tempVector2.x, tempVector2.y);

                //m_ScaledSize = Mathf.Max(tempVector2.x, tempVector2.y);
                //tempSize = m_ScaledSize;
                //m_ScaledSize *= rootCanvas.scaleFactor;
            }

            /*if (m_ScaledSize > 500)
            {
                m_LocalScaleFactor = m_ScaledSize / 500;
            }
            else
            {
                m_LocalScaleFactor = 1f;
            }*/

            //tempSize *= m_LocalScaleFactor;

            fontSize = Mathf.RoundToInt(tempSize);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif

            //m_LocalScaleFactor *= (size / Mathf.Max(size));

            
            /*if (!float.IsInfinity(m_LocalScaleFactor) && !float.IsNaN(m_LocalScaleFactor) && new Vector3(m_LocalScaleFactor, m_LocalScaleFactor, m_LocalScaleFactor) != rectTransform.localScale)
            {
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.Scale);
                rectTransform.localScale = new Vector3(m_LocalScaleFactor, m_LocalScaleFactor, m_LocalScaleFactor);
            }*/
        }

        #endregion

        #region Layout

        public new void CalculateLayoutInputHorizontal()
        {
            CacheIgnorers();
            base.CalculateLayoutInputHorizontal();
            RefreshScale();
        }

        public new void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputVertical();
            RefreshScale();
        }

        public override float preferredWidth { get { return size; } }
        public new float minWidth { get { return -1; } }
        public new float flexibleWidth { get { return -1; } }
        public override float preferredHeight { get { return size; } }
        public new float minHeight { get { return -1; } }
        public new float flexibleHeight { get { return -1; } }
        public override bool raycastTarget { get { return ignoreLayout ? false : base.raycastTarget; } set { base.raycastTarget = value; } }
        public virtual bool ignoreLayout
        {
            get
            {
                if (_cachedIgnorers == null)
                    CacheIgnorers();

                var requireIgnoreLayout = !m_IncludeInLayoutWhenEmpty && (m_VectorImageData == null || !m_VectorImageData.ContainsData());

                //Fix bug with LayoutGroup Implementation
                if (!requireIgnoreLayout)
                {
                    var ignorer = _cachedIgnorers.Find(a => ((Component)a != null) && a.ignoreLayout);
                    requireIgnoreLayout = ignorer != null;
                }
                return requireIgnoreLayout;
            }
        }

        [System.NonSerialized]
        List<ILayoutIgnorer> _cachedIgnorers = null;
        protected virtual void CacheIgnorers()
        {
            //Cache ignorers in this GameObject
            if (_cachedIgnorers == null)
                _cachedIgnorers = new List<ILayoutIgnorer>();
            else
                _cachedIgnorers.Clear();

            if (this != null)
            {
                GetComponents<ILayoutIgnorer>(_cachedIgnorers);
                var index = _cachedIgnorers.IndexOf(this);
                if (index >= 0)
                    _cachedIgnorers.RemoveAt(index);
            }
        }


        #endregion
    }
}