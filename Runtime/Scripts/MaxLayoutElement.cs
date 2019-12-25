using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
    [AddComponentMenu("Kyub UI/Max Layout Element")]
    public class MaxLayoutElement : LayoutElement
    {
        #region Private Variables

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

        protected override void Start()
        {
            base.Start();
            CalculateLayoutInputHorizontal();
            CalculateLayoutInputVertical();
        }

        #endregion

        #region Layout Functions

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            var rectTransform = transform as RectTransform;

            _CurrentPreferredWidth = basePreferredWidth > 0 && m_MaxWidth > 0 ? Mathf.Min(basePreferredWidth, m_MaxWidth) : basePreferredWidth;
            _CurrentFlexibleWidth = baseFlexibleWidth;

            if (rectTransform != null && m_MaxWidth > 0)
            {
                //Prevent element to have size greater than parent
                var parentTransform = rectTransform.parent as RectTransform;
                float parentRectWidth = parentTransform != null ? parentTransform.rect.width : 0;

                //Discover the true MaxSize to be used in calculations
                var maxWidth = _CurrentPreferredWidth > 0 ? Mathf.Min(_CurrentPreferredWidth, m_MaxWidth) : m_MaxWidth;
                maxWidth = Mathf.Max(maxWidth, minWidth);
                maxWidth = Mathf.Min(maxWidth, parentRectWidth);

                _CurrentPreferredWidth = Mathf.Min(parentRectWidth, _CurrentPreferredWidth, maxWidth);

                float rectWidth = Mathf.Max(LayoutUtility.GetFlexibleWidth(rectTransform) > 0?  rectTransform.rect.width : 0, LayoutUtility.GetMinWidth(rectTransform), LayoutUtility.GetPreferredWidth(rectTransform)); //rectTransform.rect.width;
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

            _CurrentPreferredHeight = basePreferredHeight > 0 && m_MaxHeight > 0 ? Mathf.Min(basePreferredHeight, m_MaxHeight) : basePreferredHeight;
            _CurrentFlexibleHeight = baseFlexibleHeight;

            if (rectTransform != null && m_MaxHeight > 0)
            {
                //Prevent element to have size greater than parent
                var parentTransform = rectTransform.parent as RectTransform;
                float parentRectHeight = parentTransform != null ? parentTransform.rect.height : 0;

                //Discover the true MaxSize to be used in calculations
                var maxHeight = _CurrentPreferredHeight > 0 ? _CurrentPreferredHeight : m_MaxHeight;
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

        #endregion

        #region Internal Helper Functions

        protected virtual bool CheckMaxSize()
        {
            return CheckMaxSize(true);
        }

        protected virtual bool CheckMaxSize(bool p_forceCalculate)
        {
            //CancelInvoke("CheckMaxSize");
            var changed = false;
            var rectTransform = transform as RectTransform;
            var rectSize = rectTransform != null ? rectTransform.rect.size : Vector2.zero;

            var maxWidth = Mathf.Max(m_MaxWidth, minWidth);
            var maxHeight = Mathf.Max(m_MaxHeight, minHeight);
            
            if (rectTransform != null && (maxWidth > 0 || maxHeight > 0))
            {
                var widthIsDirty = p_forceCalculate || rectSize.x > maxWidth;
                var heightIsDirty = p_forceCalculate || rectSize.y > maxHeight;

                if (widthIsDirty)
                {
                    var oldPreferredWidth = _CurrentPreferredWidth;
                    var oldFlexibleWidth = _CurrentFlexibleWidth;
                    CalculateLayoutInputHorizontal();
                    changed = changed || oldPreferredWidth != _CurrentPreferredWidth || oldFlexibleWidth != _CurrentFlexibleWidth;
                }
                if (heightIsDirty)
                {
                    var oldPreferredHeight = _CurrentPreferredHeight;
                    var oldFlexibleHeight= _CurrentFlexibleHeight;

                    CalculateLayoutInputVertical();
                    changed = changed || oldPreferredHeight != _CurrentPreferredHeight || oldFlexibleHeight != _CurrentFlexibleHeight;
                }
            }
            return changed;
        }

        #endregion
    }
}
