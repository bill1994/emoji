using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Kyub.Extensions;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class BetterGridLayoutGroup : GridLayoutGroup
    {
        #region Private Variables

        [SerializeField]
        RectTransform m_flexibleContentToExpand = null;

        #endregion

        #region Public Properties

        public RectTransform FlexibleContentToExpand
        {
            get
            {
                return m_flexibleContentToExpand;
            }
            set
            {
                if (m_flexibleContentToExpand == value)
                    return;
                m_flexibleContentToExpand = value;
            }
        }

        #endregion

        #region Helper Functions

        protected virtual void CalculateDynamicContraint()
        {
            if (FlexibleContentToExpand != null && constraint == Constraint.Flexible)
            {
                var v_localRect = FlexibleContentToExpand.GetLocalRect();
                var v_group = FlexibleContentToExpand.GetComponent<LayoutGroup>();
                if (v_group != null)
                {
                    v_localRect.width -= v_group.padding.left + v_group.padding.right;
                }
                var v_localMin = transform.InverseTransformPoint(FlexibleContentToExpand.TransformPoint(v_localRect.min));
                var v_localMax = transform.InverseTransformPoint(FlexibleContentToExpand.TransformPoint(v_localRect.max));
                v_localRect = Rect.MinMaxRect(v_localMin.x, v_localMin.y, v_localMax.x, v_localMax.y);
                int v_constraint = (int)((v_localRect.size.x - (padding.left + padding.right) + spacing.x) / (cellSize.x + spacing.x));
                v_constraint = Mathf.Min(base.rectChildren.Count , v_constraint);

                var v_oldConstraint = constraintCount;
                constraintCount = v_constraint;
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            CalculateDynamicContraint();
            var v_oldContraint = constraint;
            if (FlexibleContentToExpand != null)
                this.m_Constraint = Constraint.FixedColumnCount;
            base.CalculateLayoutInputHorizontal();
            this.m_Constraint = v_oldContraint;
        }

        public override void CalculateLayoutInputVertical()
        {
            CalculateDynamicContraint();
            var v_oldContraint = constraint;
            if (FlexibleContentToExpand != null)
                this.m_Constraint = Constraint.FixedColumnCount;
            base.CalculateLayoutInputVertical();
            this.m_Constraint = v_oldContraint;
        }

        #endregion
    }
}
