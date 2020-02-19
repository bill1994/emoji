using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    [ExecuteAlways]
    public class FastRadialLayoutGroup : FastLayoutGroup
    {
        #region Private Variables

        [SerializeField]
        private Vector2 m_Radius = Vector2.zero;
        [SerializeField, Range(0f, 360f)]
        private float m_MinAngle = 0;
        [SerializeField, Range(0f, 360f)]
        private float m_MaxAngle = 0;
        [SerializeField, Range(0f, 360f)]
        private float m_StartAngle = 0;

        #endregion

        #region Public Properties

        protected override DrivenAxis parentControlledAxis
        {
            get
            {
                var driven = DrivenAxis.Horizontal | DrivenAxis.Vertical;

                return driven;
            }
            set { }
        }

        protected override DrivenAxis childrenControlledAxis
        {
            get
            {
                return  DrivenAxis.None;
            }
            set
            {

            }
        }

        public float startAngle
        {
            get
            {
                return m_StartAngle;
            }
            set
            {
                if (m_StartAngle == value)
                    return;
                m_StartAngle = value;
            }
        }

        public float maxAngle
        {
            get
            {
                return m_MaxAngle;
            }
            set
            {
                if (m_MaxAngle == value)
                    return;
                m_MaxAngle = value;
            }
        }

        public float minAngle
        {
            get
            {
                return m_MinAngle;
            }
            set
            {
                if (m_MinAngle == value)
                    return;
                m_MinAngle = value;
            }
        }

        public Vector2 radius
        {
            get
            {
                return m_Radius;
            }
            set
            {
                if (m_Radius == value)
                    return;
                m_Radius = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            CalculateRectTransformDimensions();

            var isDirty = (_dirtyAxis & parentControlledAxis) != 0;
            //Prevent change size while calculating feedback
            if (isDirty)
                SetDirty();
            else
                _dirtyAxis &= ~(DrivenAxis.Horizontal | DrivenAxis.Vertical);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif

        #endregion

        #region ILayout Functions

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1);
        }

        #endregion

        #region  Helper Functions

        protected virtual void SetChildrenAlongAxis(int axis)
        {
            var isVertical = axis == 1;

            float maxAngle = m_MaxAngle == m_MinAngle ? m_MinAngle + 360 : m_MaxAngle;
            float offsetAngle = transform.childCount == 0 ? 0 : (maxAngle - m_MinAngle) / children.Count;

            //Calculate Initial Position
            var preferredSize = GetTotalPreferredSize(axis);
            float paddingSize = isVertical ? padding.vertical : padding.horizontal;
            float size = rectTransform.rect.size[axis];
            float pos = (axis == 0 ? padding.left : padding.top);
            float surplusSpace = size - preferredSize;

            if (surplusSpace > 0)
                pos = GetStartOffset(axis, preferredSize - (axis == 0 ? padding.horizontal : padding.vertical));

            //Set radius if value is negative
            var radius = m_Radius[axis];
            if (radius <= 0)
                radius = (size - paddingSize) / 2.0f;

            //We must offset content to middle
            pos += (preferredSize - (axis == 0 ? padding.horizontal : padding.vertical)) / 2;

            float currentAngle = m_StartAngle;
            for (int i = 0; i < children.Count; i++)
            {
                RectTransform child = children[i].rectTransform;
                if (child != null)
                {
                    float currentPos = pos;
                    if (isVertical)
                    {
                        currentPos += Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
                    }
                    else
                    {
                        currentPos += Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius;
                    }
                    //ChildAlongAxis calculatee position relative to top-bottom axis so we must place this position at the middle of the object
                    currentPos -= (isVertical ? child.sizeDelta.y : child.sizeDelta.x) / 2.0f;
                    SetChildAlongAxis(child, axis, currentPos);
                    currentAngle += offsetAngle;
                }
            }
        }

        protected virtual void CalcAlongAxis(int axis)
        {
            bool isVertical = axis == 1;

            //Calculate Size
            float paddingSize = isVertical ? padding.vertical : padding.horizontal;
            float size = rectTransform.rect.size[axis];

            //Set radius if value is negative
            var radius = m_Radius[axis];
            if (radius <= 0)
                radius = (size - paddingSize) / 2.0f;


            float preferredSize = (radius * 2) + paddingSize;

            SetLayoutInputForAxis(-1, preferredSize, -1, axis);
        }

        #endregion
    }
}
