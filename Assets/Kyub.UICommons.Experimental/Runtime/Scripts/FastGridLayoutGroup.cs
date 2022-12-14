using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    [ExecuteAlways]
    public class FastGridLayoutGroup : FastLayoutGroup
    {
        #region Helper Classes

        public enum Corner
        {
            /// <summary>
            /// Upper Left corner.
            /// </summary>
            UpperLeft = 0,
            /// <summary>
            /// Upper Right corner.
            /// </summary>
            UpperRight = 1,
            /// <summary>
            /// Lower Left corner.
            /// </summary>
            LowerLeft = 2,
            /// <summary>
            /// Lower Right corner.
            /// </summary>
            LowerRight = 3
        }
        public enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        public enum Constraint
        {
            Flexible = 0,
            FixedColumnCount = 1,
            FixedRowCount = 2
        }

        #endregion

        #region Private Variables

        [SerializeField] protected Corner m_StartCorner = Corner.UpperLeft;
        [SerializeField] protected Axis m_StartAxis = Axis.Horizontal;
        [SerializeField] protected Vector2 m_CellSize = new Vector2(100, 100);
        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
        [SerializeField] protected Constraint m_Constraint = Constraint.Flexible;
        [SerializeField] protected int m_ConstraintCount = 2;

        #endregion

        #region Properties

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
                return DrivenAxis.None;
            }
            set
            {

            }
        }

        public Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }
        public Axis startAxis { get { return m_StartAxis; } set { SetProperty(ref m_StartAxis, value); } }
        public Vector2 cellSize { get { return m_CellSize; } set { SetProperty(ref m_CellSize, value); } }
        public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }
        public Constraint constraint { get { return m_Constraint; } set { SetProperty(ref m_Constraint, value); } }
        public int constraintCount { get { return m_ConstraintCount; } set { SetProperty(ref m_ConstraintCount, Mathf.Max(1, value)); } }

        #endregion

        #region Constructors

        protected FastGridLayoutGroup()
        { }

        #endregion

        #region Unity Functions

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
            constraintCount = constraintCount;
        }

#endif

        #endregion

        #region Overriden Functions

        /// <summary>
        /// Called by the layout system to calculate the horizontal layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            int minColumns = 0;
            int preferredColumns = 0;
            if (m_Constraint == Constraint.FixedColumnCount)
            {
                minColumns = preferredColumns = m_ConstraintCount;
            }
            else if (m_Constraint == Constraint.FixedRowCount)
            {
                minColumns = preferredColumns = Mathf.CeilToInt(children.Count / (float)m_ConstraintCount - 0.001f);
            }
            else
            {
                minColumns = 1;
                preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(children.Count));
            }

            SetLayoutInputForAxis(
                padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x,
                padding.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x,
                -1, 0);
        }

        /// <summary>
        /// Called by the layout system to calculate the vertical layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            int minRows = 0;
            if (m_Constraint == Constraint.FixedColumnCount)
            {
                minRows = Mathf.CeilToInt(children.Count / (float)m_ConstraintCount - 0.001f);
            }
            else if (m_Constraint == Constraint.FixedRowCount)
            {
                minRows = m_ConstraintCount;
            }
            else
            {
                float width = rectTransform.rect.width;
                int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
                minRows = Mathf.CeilToInt(children.Count / (float)cellCountX);
            }

            float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;
            SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
        }

        private void SetCellsAlongAxis(int axis)
        {
            // Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
            // and only vertical values when invoked for the vertical axis.
            // However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
            // Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
            // and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.

            if (axis == 0)
            {
                // Only set the sizes when invoked for horizontal axis, not the positions.
                for (int i = 0; i < children.Count; i++)
                {
                    var rect = children[i].rectTransform;

                    m_Tracker.Add(this, rect,
                        DrivenTransformProperties.Anchors |
                        DrivenTransformProperties.AnchoredPosition |
                        DrivenTransformProperties.SizeDelta);

                    rect.anchorMin = Vector2.up;
                    rect.anchorMax = Vector2.up;
                    rect.sizeDelta = cellSize;
                }
                return;
            }

            float width = rectTransform.rect.size.x;
            float height = rectTransform.rect.size.y;

            int cellCountX = 1;
            int cellCountY = 1;
            if (m_Constraint == Constraint.FixedColumnCount)
            {
                cellCountX = m_ConstraintCount;

                if (children.Count > cellCountX)
                    cellCountY = children.Count / cellCountX + (children.Count % cellCountX > 0 ? 1 : 0);
            }
            else if (m_Constraint == Constraint.FixedRowCount)
            {
                cellCountY = m_ConstraintCount;

                if (children.Count > cellCountY)
                    cellCountX = children.Count / cellCountY + (children.Count % cellCountY > 0 ? 1 : 0);
            }
            else
            {
                if (cellSize.x + spacing.x <= 0)
                    cellCountX = int.MaxValue;
                else
                    cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

                if (cellSize.y + spacing.y <= 0)
                    cellCountY = int.MaxValue;
                else
                    cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
            }

            int cornerX = (int)startCorner % 2;
            int cornerY = (int)startCorner / 2;

            int cellsPerMainAxis, actualCellCountX, actualCellCountY;
            if (startAxis == Axis.Horizontal)
            {
                cellsPerMainAxis = cellCountX;
                actualCellCountX = Mathf.Clamp(cellCountX, 1, children.Count);
                actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(children.Count / (float)cellsPerMainAxis));
            }
            else
            {
                cellsPerMainAxis = cellCountY;
                actualCellCountY = Mathf.Clamp(cellCountY, 1, children.Count);
                actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(children.Count / (float)cellsPerMainAxis));
            }

            Vector2 requiredSpace = new Vector2(
                actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
                actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
            );
            Vector2 startOffset = new Vector2(
                GetStartOffset(0, requiredSpace.x),
                GetStartOffset(1, requiredSpace.y)
            );

            for (int i = 0; i < children.Count; i++)
            {
                int positionX;
                int positionY;
                if (startAxis == Axis.Horizontal)
                {
                    positionX = i % cellsPerMainAxis;
                    positionY = i / cellsPerMainAxis;
                }
                else
                {
                    positionX = i / cellsPerMainAxis;
                    positionY = i % cellsPerMainAxis;
                }

                if (cornerX == 1)
                    positionX = actualCellCountX - 1 - positionX;
                if (cornerY == 1)
                    positionY = actualCellCountY - 1 - positionY;

                SetChildAlongAxis(children[i].rectTransform, 0, startOffset.x + (cellSize[0] + spacing[0]) * positionX, cellSize[0]);
                SetChildAlongAxis(children[i].rectTransform, 1, startOffset.y + (cellSize[1] + spacing[1]) * positionY, cellSize[1]);
            }
        }

        #endregion
    }
}
