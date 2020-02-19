
/// Credit Danny Goodayle 
/// Sourced from - http://www.justapixel.co.uk/radial-layouts-nice-and-simple-in-unity3ds-ui-system/
/// Updated by ddreaper - removed dependency on a custom ScrollRect script. Now implements drag interfaces and standard Scroll Rect.
/// Chid Layout fix by John Hattan - enables an options 

/*
Radial Layout Group by Just a Pixel (Danny Goodayle) - http://www.justapixel.co.uk
Copyright (c) 2015
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
    [AddComponentMenu("Kyub UI/Radial Layout Group")]
    public class RadialLayoutGroup : LayoutGroup
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
            //Get active children
            List<RectTransform> activeChildren = new List<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = (RectTransform)transform.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                    activeChildren.Add(child);
            }

            float maxAngle = m_MaxAngle == m_MinAngle ? m_MinAngle + 360 : m_MaxAngle;
            float offsetAngle = transform.childCount == 0 ? 0 : (maxAngle - m_MinAngle) / activeChildren.Count;

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
                radius = (size - paddingSize) / 2.0f ;

            //We must offset content to middle
            pos += (preferredSize - (axis == 0 ? padding.horizontal : padding.vertical)) / 2;

            float currentAngle = m_StartAngle;
            for (int i = 0; i < activeChildren.Count; i++)
            {
                RectTransform child = (RectTransform)transform.GetChild(i);
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
                    currentPos -= (isVertical ? child.sizeDelta.y : child.sizeDelta.x)/2.0f;
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

            
            float preferredSize = (radius* 2) + paddingSize;

            SetLayoutInputForAxis(-1, preferredSize, -1, axis);
        }

        #endregion
    }
}
