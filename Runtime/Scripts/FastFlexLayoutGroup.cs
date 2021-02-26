using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    public class FastFlexLayoutGroup : FastHorizontalOrVerticalLayoutGroup
    {
        #region Helper Classes

        [System.Serializable]
        protected internal class LinearGroup
        {
            [SerializeField] private Vector2 m_Size = Vector2.zero;
            [SerializeField] private Vector2 m_Position = Vector2.zero;
            [SerializeField] private List<IFastLayoutFeedback> m_Children = new List<IFastLayoutFeedback>();
            [SerializeField] private Vector2 m_TotalMinSize = Vector2.zero;
            [SerializeField] private Vector2 m_TotalPreferredSize = Vector2.zero;
            [SerializeField] private Vector2 m_TotalFlexibleSize = Vector2.zero;

            public Vector2 position
            {
                get
                {
                    return m_Position;
                }
                set
                {
                    if (m_Position == value)
                        return;
                    m_Position = value;
                }
            }

            public Vector2 size
            {
                get
                {
                    return m_Size;
                }
                set
                {
                    if (m_Size == value)
                        return;
                    m_Size = value;
                }
            }

            public List<IFastLayoutFeedback> Children
            {
                get
                {
                    if (m_Children == null)
                        m_Children = new List<IFastLayoutFeedback>();
                    return m_Children;
                }
                set
                {
                    if (m_Children == value)
                        return;
                    m_Children = value;
                }
            }

            public float GetTotalMinSize(int axis)
            {
                return m_TotalMinSize[axis];
            }

            public float GetTotalPreferredSize(int axis)
            {
                return m_TotalPreferredSize[axis];
            }

            public float GetTotalFlexibleSize(int axis)
            {
                return m_TotalFlexibleSize[axis];
            }

            public void SetLayoutInputForAxis(float totalMin, float totalPreferred, float totalFlexible, int axis)
            {
                m_TotalMinSize[axis] = totalMin;
                m_TotalPreferredSize[axis] = totalPreferred;
                m_TotalFlexibleSize[axis] = totalFlexible;
            }

            public float GetStartOffset(int axis, float requiredSpaceWithoutPadding, float alignmentOnAxis)
            {
                float requiredSpace = requiredSpaceWithoutPadding;
                float availableSpace = size[axis];
                float surplusSpace = availableSpace - requiredSpace;
                return surplusSpace * alignmentOnAxis;
            }

        }

        #endregion

        #region Private Variables

        [SerializeField]
        bool m_IsVertical = false;

        [SerializeField]
        float m_SpacingBetween = 0;

        List<LinearGroup> _Groups = new List<LinearGroup>();

        #endregion

        #region Public Properties

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

        public float spacingBetween
        {
            get
            {
                return m_SpacingBetween;
            }
            set
            {
                if (m_SpacingBetween == value)
                    return;
                m_SpacingBetween = value;
                SetDirty();
            }
        }

        #endregion

        #region Constructos

        protected FastFlexLayoutGroup()
        { }

        #endregion

        #region Unity Functions

        protected override void OnRectTransformDimensionsChange()
        {
            CalcRectTransformChanged(isVertical);
        }

        #endregion

        #region Overriden Properties

        protected override void CalcRectTransformChanged(bool isVertical)
        {
            CalculateRectTransformDimensions();

            var isDirty = _dirtyAxis != DrivenAxis.None;

            //We will only set dirty if value of new rect is smaller than preferred size or if flexible size is empty
            if (isDirty &&
                (!isVertical && _dirtyAxis.HasFlag(DrivenAxis.Vertical) && !_dirtyAxis.HasFlag(DrivenAxis.Horizontal) && (_cachedRectHeight < m_TotalPreferredSize.y || m_TotalFlexibleSize.y == 0)) ||
                (isVertical && !_dirtyAxis.HasFlag(DrivenAxis.Vertical) && _dirtyAxis.HasFlag(DrivenAxis.Horizontal) && (_cachedRectWidth < m_TotalPreferredSize.x || m_TotalFlexibleSize.x == 0)))
                isDirty = false;

            //Prevent change size while calculating feedback
            if (isDirty)
                SetDirty();
            else
                _dirtyAxis &= ~(DrivenAxis.Horizontal | DrivenAxis.Vertical);
        }

        public override void SetElementDirty(IFastLayoutFeedback driven, DrivenAxis dirtyAxis)
        {
            if (driven != null && (dirtyAxis.HasFlag(DrivenAxis.Ignore) || (dirtyAxis & childrenControlledAxis) != 0))
            {
                if (!isDirty)
                    MarkLayoutForRebuild();

                //Special case when added element will affect other elements size
                if (dirtyAxis.HasFlag(DrivenAxis.Ignore) &&
                    (m_TotalFlexibleSize.y > 0 || m_TotalFlexibleSize.x > 0))
                {
                    isAxisDirty = true;
                }

                _dirtyChildren.Add(driven);
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            _Groups = BuildChildrenGroups(isVertical);
            CalcAlongAxis(0, m_IsVertical);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1, m_IsVertical);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0, m_IsVertical);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1, m_IsVertical);
        }

        #endregion

        #region Internal Helper Functions

        protected virtual List<LinearGroup> BuildChildrenGroups(bool isVertical)
        {
            List<LinearGroup> rectChildrenPerGroup = new List<LinearGroup>();
            int axis = isVertical ? 1 : 0;

            float size = rectTransform.rect.size[axis];
            float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);

            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);

            float startPos = (axis == 0 ? padding.left : padding.top);
            float itemFlexibleMultiplier = 0;
            float surplusSpace = size - GetTotalPreferredSize(axis);

            float pos = startPos;

            LinearGroup currentGroup = new LinearGroup();
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                float min, preferred, flexible;
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                float scaleFactor = useScale ? child.rectTransform.localScale[axis] : 1f;

                float childSize = Mathf.Max(min, preferred);
                childSize += flexible * itemFlexibleMultiplier;

                //Fit in current group
                if (pos + childSize <= (startPos + innerSize))
                {
                    currentGroup.Children.Add(child);
                    pos += childSize * scaleFactor + spacing;

                    //Last iteraction so we must force add currentgroup to groups
                    if (i == children.Count - 1)
                        rectChildrenPerGroup.Add(currentGroup);
                }
                //Dont fit
                else
                {
                    //We finished this group, add it in groups list
                    rectChildrenPerGroup.Add(currentGroup);

                    //Create a new group
                    var newGroup = new LinearGroup();
                    pos = startPos;

                    //We cant skup this case, child must be added in this group if no other was added before
                    if (currentGroup.Children.Count == 0)
                    {
                        currentGroup.Children.Add(child);
                    }
                    else
                    {
                        newGroup.Children.Add(child);
                        pos = childSize * scaleFactor + spacing;
                    }

                    currentGroup = newGroup;

                    //Last iteraction so we must force add currentgroup to groups
                    if (i == children.Count - 1 && newGroup.Children.Count > 0)
                        rectChildrenPerGroup.Add(newGroup);
                }
            }

            return rectChildrenPerGroup;
        }

        protected new void CalcAlongAxis(int axis, bool isVertical)
        {
            float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

            float totalMin = combinedPadding;
            float totalPreferred = combinedPadding;
            float totalFlexible = 0;

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            var axisSpacing = alongOtherAxis ? spacingBetween : spacing;
            for (int i = 0; i < _Groups.Count; i++)
            {
                LinearGroup group = _Groups[i];
                CalcAlongGroupAxis(axis, isVertical, group);

                //float min = group.GetTotalMinSize(axis);
                float preferred = group.GetTotalPreferredSize(axis);
                float flexible = group.GetTotalFlexibleSize(axis);

                //TODO: FIX MIN SIZE - We removed TotalMin size as a bug found in our logic
                //totalMin += min + axisSpacing;
                totalPreferred += preferred + axisSpacing;

                // Increment flexible size with element's flexible size.
                totalFlexible += flexible;
            }

            if (!alongOtherAxis && children.Count > 0)
            {
                //TODO: FIX MIN SIZE - We removed TotalMin size as a bug found in our logic
                //totalMin -= axisSpacing;
                totalPreferred -= axisSpacing;
            }
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        protected void CalcAlongGroupAxis(int axis, bool isVertical, LinearGroup group)
        {
            if (group == null)
                return;

            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

            float totalMin = 0;
            float totalPreferred = 0;
            float totalFlexible = 0;

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            for (int i = 0; i < group.Children.Count; i++)
            {
                var child = group.Children[i];
                float min, preferred, flexible;
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

                if (useScale)
                {
                    float scaleFactor = child.rectTransform.localScale[axis];
                    min *= scaleFactor;
                    preferred *= scaleFactor;
                    flexible *= scaleFactor;
                }

                if (alongOtherAxis)
                {
                    totalMin = Mathf.Max(min, totalMin);
                    totalPreferred = Mathf.Max(preferred, totalPreferred);
                    totalFlexible = Mathf.Max(flexible, totalFlexible);
                }
                else
                {
                    totalMin += min + spacing;
                    totalPreferred += preferred + spacing;

                    // Increment flexible size with element's flexible size.
                    totalFlexible += flexible;
                }
            }

            if (!alongOtherAxis && children.Count > 0)
            {
                totalMin -= spacing;
                totalPreferred -= spacing;
            }
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            group.SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        protected new void SetChildrenAlongAxis(int axis, bool isVertical)
        {
            float size = rectTransform.rect.size[axis];
            float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
            bool alongOtherAxis = (isVertical ^ (axis == 1));

            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

            //Calculate initial position
            float pos = (axis == 0 ? padding.left : padding.top);
            float surplusSpace = size - GetTotalPreferredSize(axis);

            if (surplusSpace > 0 && alongOtherAxis)
            {
                if (GetTotalFlexibleSize(axis) == 0)
                {
                    if (!controlSize || !childForceExpandSize)
                    {
                        pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                    }
                }
            }

            for (int i = 0; i < _Groups.Count; i++)
            {
                LinearGroup group = _Groups[i];

                var groupSize = group.size;
                groupSize[axis] = alongOtherAxis ? Mathf.Max(group.GetTotalMinSize(axis), group.GetTotalPreferredSize(axis)) : innerSize;
                group.size = groupSize;

                var groupPosition = group.position;
                groupPosition[axis] = pos;
                group.position = groupPosition;

                SetChildrenAlongGroupAxis(axis, isVertical, group);

                if (alongOtherAxis)
                    pos += groupSize[axis] + spacingBetween;
            }
        }
        protected void SetChildrenAlongGroupAxis(int axis, bool isVertical, LinearGroup group)
        {
            float size = group.size[axis];
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            bool alongOtherAxis = (isVertical ^ (axis == 1));

            if (alongOtherAxis)
            {
                float pos = group.position[axis];
                for (int i = 0; i < group.Children.Count; i++)
                {
                    var child = group.Children[i];
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.rectTransform.localScale[axis] : 1f;

                    float requiredSpace = Mathf.Clamp(size, min, flexible > 0 ? size : preferred);
                    float startOffset = pos + group.GetStartOffset(axis, requiredSpace * scaleFactor, alignmentOnAxis);
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child.rectTransform, axis, startOffset, requiredSpace, scaleFactor);
                    }
                    else
                    {
                        float offsetInCell = (requiredSpace - (axis == 0 ? child.cachedRectWidth : child.cachedRectHeight)) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child.rectTransform, axis, startOffset + offsetInCell, requiredSpace, scaleFactor);
                    }
                }
            }
            else
            {
                float pos = group.position[axis];
                float itemFlexibleMultiplier = 0;
                float surplusSpace = size - group.GetTotalPreferredSize(axis);

                var useFlexibleSpacing = false;
                if (surplusSpace > 0)
                {
                    if (group.GetTotalFlexibleSize(axis) == 0)
                    {
                        if (!controlSize || !childForceExpandSize)
                        {
                            pos += group.GetStartOffset(axis, group.GetTotalPreferredSize(axis), alignmentOnAxis);
                        }
                        else
                            useFlexibleSpacing = true;
                    }
                    else if (group.GetTotalFlexibleSize(axis) > 0)
                        itemFlexibleMultiplier = surplusSpace / group.GetTotalFlexibleSize(axis);
                }

                float minMaxLerp = 0;
                if (group.GetTotalMinSize(axis) != group.GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - group.GetTotalMinSize(axis)) / (group.GetTotalPreferredSize(axis) - group.GetTotalMinSize(axis)));

                var currentSpacing = useFlexibleSpacing && group.Children.Count - 1 > 0 ? Mathf.Max(spacing, surplusSpace / (group.Children.Count - 1)) : spacing;
                for (int i = 0; i < group.Children.Count; i++)
                {
                    var child = group.Children[i];
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.rectTransform.localScale[axis] : 1f;

                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    childSize += flexible * itemFlexibleMultiplier;
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child.rectTransform, axis, pos, childSize, scaleFactor);
                    }
                    else
                    {
                        float offsetInCell = (childSize - (axis == 0 ? child.cachedRectWidth : child.cachedRectHeight)) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child.rectTransform, axis, pos + offsetInCell, childSize, scaleFactor);
                    }
                    pos += childSize * scaleFactor + currentSpacing;
                }
            }
        }

        #endregion
    }
}
