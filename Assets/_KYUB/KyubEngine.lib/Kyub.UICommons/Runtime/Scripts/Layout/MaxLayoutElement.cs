using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
    public enum MaxLayoutMode { Default, ParentPercent, ParentOffset }

    public interface IMaxLayoutElement : ILayoutElement
    { 
        float maxWidth { get; set; }
        float maxHeight { get; set; }

        MaxLayoutMode maxWidthMode { get; set; }
        MaxLayoutMode maxHeightMode { get; set; }

        float GetMaxWidthInDefaultMode();
        float GetMaxHeightInDefaultMode();
    }

    [AddComponentMenu("Kyub UI/Max Layout Element")]
    public class MaxLayoutElement : LayoutElement, IMaxLayoutElement
    {
        
        #region Private Variables

        [SerializeField, Tooltip("* DefaultMode : RectTransform Width/Height\n* ParentPercent : Percent value of parent rect size (0 to 1)\n* ParentOffset : Parent rect size - offset")]
        MaxLayoutMode m_MaxWidthMode = MaxLayoutMode.Default;
        [SerializeField, Tooltip("* DefaultMode : RectTransform Width/Height\n* ParentPercent : Percent value of parent rect size (0 to 1)\n* ParentOffset : Parent rect size - offset")]
        MaxLayoutMode m_MaxHeightMode = MaxLayoutMode.Default;
        [SerializeField]
        protected float m_MaxWidth = -1;
        [SerializeField]
        protected float m_MaxHeight = -1;

        protected float _CurrentPreferredWidth = -1f;
        protected float _CurrentPreferredHeight = -1f;
        protected float _CurrentFlexibleWidth = -1f;
        protected float _CurrentFlexibleHeight = -1f;

        #endregion

        #region Public Properties

        public virtual MaxLayoutMode maxWidthMode
        {
            get
            {
                return m_MaxWidthMode;
            }

            set
            {
                if (m_MaxWidthMode == value)
                    return;
                m_MaxWidthMode = value;
                SetDirty();
            }
        }

        public virtual MaxLayoutMode maxHeightMode
        {
            get
            {
                return m_MaxHeightMode;
            }

            set
            {
                if (m_MaxHeightMode == value)
                    return;
                m_MaxHeightMode = value;
                SetDirty();
            }
        }

        public virtual float maxWidth
        {
            get
            {
                return m_MaxWidth;
            }

            set
            {
                if (m_MaxWidth == value)
                    return;
                m_MaxWidth = value;
                CalculateLayoutInputHorizontal();
                SetDirty();
            }
        }

        public virtual float maxHeight
        {
            get
            {
                return m_MaxHeight;
            }

            set
            {
                if (m_MaxHeight == value)
                    return;
                m_MaxHeight = value;
                CalculateLayoutInputVertical();
                SetDirty();
            }
        }

        public override float preferredWidth
        {
            get
            {
                if (_CurrentPreferredWidth < 0 && basePreferredWidth >= 0)
                    _CurrentPreferredWidth = basePreferredWidth;
                return _CurrentPreferredWidth;
            }

            set
            {
                if (basePreferredWidth == value)
                    return;
                basePreferredWidth = value;
                CalculateLayoutInputHorizontal();
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (_CurrentPreferredHeight < 0 && basePreferredHeight >= 0)
                    _CurrentPreferredHeight = basePreferredHeight;
                return _CurrentPreferredHeight;
            }

            set
            {
                if (basePreferredHeight == value)
                    return;
                basePreferredHeight = value;
                CalculateLayoutInputVertical();
            }
        }

        public override float flexibleWidth
        {
            get
            {
                if (_CurrentFlexibleWidth < 0 && baseFlexibleWidth >= 0)
                    _CurrentFlexibleWidth = baseFlexibleWidth;
                return _CurrentFlexibleWidth;
            }

            set
            {
                if (baseFlexibleWidth == value)
                    return;
                baseFlexibleWidth = value;
                CalculateLayoutInputHorizontal();
            }
        }

        public override float flexibleHeight
        {
            get
            {
                if (_CurrentFlexibleHeight < 0 && baseFlexibleHeight >= 0)
                    _CurrentFlexibleHeight = baseFlexibleHeight;
                return _CurrentFlexibleHeight;
            }

            set
            {
                if (baseFlexibleHeight == value)
                    return;
                baseFlexibleHeight = value;
                CalculateLayoutInputVertical();
            }
        }

        #endregion

        #region Internal Properties

        protected float basePreferredWidth
        {
            get
            {
                return base.preferredWidth;
            }
            set
            {
                if (base.preferredWidth == value)
                    return;
                base.preferredWidth = value;
            }
        }

        protected float basePreferredHeight
        {
            get
            {
                return base.preferredHeight;
            }
            set
            {
                if (base.preferredHeight == value)
                    return;
                base.preferredHeight = value;
            }
        }

        protected float baseFlexibleWidth
        {
            get
            {
                return base.flexibleWidth;
            }
            set
            {
                if (base.flexibleWidth == value)
                    return;
                base.flexibleWidth = value;
            }
        }

        protected float baseFlexibleHeight
        {
            get
            {
                return base.flexibleHeight;
            }
            set
            {
                if (base.flexibleHeight == value)
                    return;
                base.flexibleHeight = value;
            }
        }

        protected float baseMinWidth
        {
            get
            {
                return base.minWidth;
            }
            set
            {
                if (base.minWidth == value)
                    return;
                base.minWidth = value;
            }
        }

        protected float baseMinHeight
        {
            get
            {
                return base.minHeight;
            }
            set
            {
                if (base.minHeight == value)
                    return;
                base.minHeight = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnDisable()
        {
            base.OnDisable();
            CancelInvoke("SetDirty");
        }

        protected override void Start()
        {
            base.Start();
            CalculateLayoutInputHorizontal();
            CalculateLayoutInputVertical();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (IsLayoutDirty())
            {
                Invoke("SetDirty", 0);
            }
        }

        #endregion

        #region Layout Functions

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            var rectTransform = transform as RectTransform;
            _CurrentFlexibleWidth = baseFlexibleWidth;

            if (rectTransform.parent != null && rectTransform.parent.GetComponent<IMaxLayoutGroup>() as Behaviour != null)
            {
                _CurrentPreferredWidth = basePreferredWidth;
                return;
            }
            
            var convertedMaxWidth = GetMaxWidthInDefaultMode();
            _CurrentPreferredWidth = basePreferredWidth > 0 && convertedMaxWidth > 0 ? Mathf.Min(basePreferredWidth, convertedMaxWidth) : basePreferredWidth;
            
            if (rectTransform != null && convertedMaxWidth > 0)
            {
                //Prevent element to have size greater than parent
                var parentTransform = rectTransform.parent as RectTransform;
                float parentRectWidth = parentTransform != null ? parentTransform.rect.width : 0;

                //Discover the true MaxSize to be used in calculations
                var maxWidth = _CurrentPreferredWidth > 0 ? Mathf.Min(_CurrentPreferredWidth, convertedMaxWidth) : convertedMaxWidth;
                maxWidth = Mathf.Max(maxWidth, minWidth);
                maxWidth = Mathf.Min(maxWidth, parentRectWidth);

                _CurrentPreferredWidth = Mathf.Min(parentRectWidth, _CurrentPreferredWidth, maxWidth);

                float rectWidth = Mathf.Max(LayoutUtility.GetFlexibleWidth(rectTransform) > 0 ? rectTransform.rect.width : 0, LayoutUtility.GetMinWidth(rectTransform), LayoutUtility.GetPreferredWidth(rectTransform)); //rectTransform.rect.width;
                //Remove flexible width to force apply preferred size instead (because maxsize is the preferred size)
                if (rectWidth >= maxWidth)
                {
                    _CurrentPreferredWidth = maxWidth;
                    _CurrentFlexibleWidth = _CurrentFlexibleWidth > 0 ? 0 : _CurrentFlexibleWidth;
                }
            }
        }

        public override void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputVertical();
            var rectTransform = transform as RectTransform;
            _CurrentFlexibleHeight = baseFlexibleHeight;

            if (rectTransform.parent != null && rectTransform.parent.GetComponent<IMaxLayoutGroup>() as Behaviour != null)
            {
                _CurrentPreferredHeight = basePreferredHeight;
                return;
            }

            var convertedMaxHeight = GetMaxHeightInDefaultMode();
            _CurrentPreferredHeight = basePreferredHeight > 0 && convertedMaxHeight > 0 ? Mathf.Min(basePreferredHeight, convertedMaxHeight) : basePreferredHeight;

            if (rectTransform != null && convertedMaxHeight > 0)
            {
                //Prevent element to have size greater than parent
                var parentTransform = rectTransform.parent as RectTransform;
                float parentRectHeight = parentTransform != null ? parentTransform.rect.height : 0;

                //Discover the true MaxSize to be used in calculations
                var maxHeight = _CurrentPreferredHeight > 0 ? _CurrentPreferredHeight : convertedMaxHeight;
                maxHeight = Mathf.Max(maxHeight, minHeight);
                maxHeight = Mathf.Min(maxHeight, parentRectHeight);

                _CurrentPreferredHeight = Mathf.Min(parentRectHeight, _CurrentPreferredHeight, maxHeight);

                float rectHeight = Mathf.Max(LayoutUtility.GetFlexibleHeight(rectTransform) > 0 ? rectTransform.rect.height : 0, LayoutUtility.GetMinHeight(rectTransform), LayoutUtility.GetPreferredHeight(rectTransform)); //rectTransform.rect.height;
                //Remove flexible width to force apply preferred size instead (because maxsize is the preferred size)
                if (rectHeight >= maxHeight)
                {
                    _CurrentPreferredHeight = maxHeight;
                    _CurrentFlexibleHeight = _CurrentFlexibleHeight > 0 ? 0 : _CurrentFlexibleHeight;
                }
            }
        }

        public float GetMaxWidthInDefaultMode()
        {
            if (m_MaxWidth > 0 && m_MaxWidthMode != MaxLayoutMode.Default)
            {
                var rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    var parentTransform = rectTransform.parent as RectTransform;
                    var parentRectWidth = parentTransform != null ? parentTransform.rect.width : 0;

                    var width = m_MaxWidthMode == MaxLayoutMode.ParentPercent ?
                        Mathf.Max(0, parentRectWidth * Mathf.Clamp01(m_MaxWidth)) :
                        Mathf.Max(0, parentRectWidth - m_MaxWidth);

                    return width;
                }
            }

            return m_MaxWidth;
        }

        public float GetMaxHeightInDefaultMode()
        {
            if (m_MaxHeight > 0 && m_MaxHeightMode != MaxLayoutMode.Default)
            {
                var rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    var parentTransform = rectTransform.parent as RectTransform;
                    var parentRectHeight = parentTransform != null ? parentTransform.rect.height : 0;

                    var height = m_MaxHeightMode == MaxLayoutMode.ParentPercent ?
                        Mathf.Max(0, parentRectHeight * Mathf.Clamp01(m_MaxHeight)) :
                        Mathf.Max(0, parentRectHeight - m_MaxHeight);

                    return height;
                }
            }

            return m_MaxHeight;
        }

        #endregion

        #region Internal Helper Functions

        protected virtual bool IsLayoutDirty()
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform != null && rectTransform.parent != null && rectTransform.parent.GetComponent<IMaxLayoutGroup>() as Behaviour != null)
            {
                var rectSize = rectTransform.rect.size;
                var maxWidth = Mathf.Max(GetMaxWidthInDefaultMode(), minWidth);
                if (maxWidth > 0 && rectSize.x > maxWidth)
                {
                    return true;
                }
                else
                {
                    var maxHeight = Mathf.Max(GetMaxHeightInDefaultMode(), minHeight);
                    if (maxHeight > 0 && rectSize.y > maxHeight)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
    }
}