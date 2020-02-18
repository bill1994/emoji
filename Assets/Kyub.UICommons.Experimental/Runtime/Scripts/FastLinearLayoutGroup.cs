using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    public class FastLinearLayoutGroup : FastHorizontalOrVerticalLayoutGroup
    {
        #region Private Variables

        [SerializeField]
        bool m_IsVertical = true;

        #endregion

        #region Public Properties

        public bool isVertical
        {
            get
            {
                return m_IsVertical;
            }
            set
            {
                if (m_IsVertical == value)
                    return;
                m_IsVertical = value;
                SetDirty();
            }
        }

        protected override DrivenAxis parentControlledAxis
        {
            get
            {
                var driven = (childForceExpandHeight ? DrivenAxis.Vertical : DrivenAxis.None) |
                    (childForceExpandWidth ? DrivenAxis.Horizontal : DrivenAxis.None);

                return driven;
            }
            set { }
        }

        protected override DrivenAxis childrenControlledAxis
        {
            get
            {
                return (childControlHeight ? DrivenAxis.Vertical : DrivenAxis.None) | 
                    (childControlWidth ? DrivenAxis.Horizontal : DrivenAxis.None);
            }
            set
            {

            }
        }

        #endregion

        #region Constructos

        protected FastLinearLayoutGroup()
        { }

        #endregion

        #region Unity Functions

        protected override void OnRectTransformDimensionsChange()
        {
            CalcRectTransformChanged(isVertical);
        }

        #endregion

        #region Overriden Properties

        public override void SetElementDirty(IFastLayoutFeedback driven, DrivenAxis dirtyAxis)
        {
            if (driven != null && (dirtyAxis.HasFlag(DrivenAxis.Ignore) || (dirtyAxis & childrenControlledAxis) != 0))
            {
                if (!isDirty)
                    MarkLayoutForRebuild();

                //Special case when added element will affect other elements size
                if (dirtyAxis.HasFlag(DrivenAxis.Ignore) && 
                    ((isVertical && m_TotalFlexibleSize.y > 0) ||
                    (!isVertical && m_TotalFlexibleSize.x > 0)))
                {
                    isAxisDirty = true;
                }

                _dirtyChildren.Add(driven);
            }
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0, m_IsVertical);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1, m_IsVertical);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0, m_IsVertical);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1, m_IsVertical);
        }

        #endregion
    }
}
