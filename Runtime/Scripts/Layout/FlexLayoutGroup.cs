using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
    [AddComponentMenu("Kyub UI/Flex Layout Group")]
    public class FlexLayoutGroup : HorizontalOrVerticalLayoutGroupEx
    {
        #region Helper Classes

        [System.Serializable]
        protected internal class LinearGroup
        {
            [SerializeField] private Vector2 m_Size = Vector2.zero;
            [SerializeField] private Vector2 m_Position = Vector2.zero;
            [SerializeField] private List<RectTransform> m_RectChildren = new List<RectTransform>();
            [SerializeField] private Vector2 m_TotalMinSize = Vector2.zero;
            [SerializeField] private Vector2 m_TotalPreferredSize = Vector2.zero;
            [SerializeField] private Vector2 m_TotalFlexibleSize = Vector2.zero;

            private Dictionary<int, List<int>> m_AxisPreFilterIndexes = new Dictionary<int, List<int>>() { { 0, new List<int>() }, { 1, new List<int>() } };

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

            public List<RectTransform> RectChildren
            {
                get
                {
                    if (m_RectChildren == null)
                        m_RectChildren = new List<RectTransform>();
                    return m_RectChildren;
                }
                set
                {
                    if (m_RectChildren == value)
                        return;
                    m_RectChildren = value;
                }
            }

            public Dictionary<int, List<int>> AxisPreFilterIndexes
            {
                get
                {
                    return m_AxisPreFilterIndexes;
                }
                set
                {
                    if (m_AxisPreFilterIndexes == value)
                        return;
                    m_AxisPreFilterIndexes = value;
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

        protected FlexLayoutGroup()
        { }

        #endregion

        #region Overriden Properties

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
            //float surplusSpace = size - GetTotalPreferredSize(axis);

            float pos = startPos;

            LinearGroup currentGroup = new LinearGroup();
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                float min, preferred, flexible, max;
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);
                float scaleFactor = useScale ? child.localScale[axis] : 1f;

                float childSize = Mathf.Max(min, preferred);
                childSize += flexible * itemFlexibleMultiplier;

                //Fit in current group
                if (pos + childSize <= (startPos + innerSize))
                {
                    currentGroup.RectChildren.Add(child);
                    pos += childSize * scaleFactor + spacing;

                    //Last iteraction so we must force add currentgroup to groups
                    if (i == rectChildren.Count - 1)
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
                    if (currentGroup.RectChildren.Count == 0)
                    {
                        currentGroup.RectChildren.Add(child);
                    }
                    else
                    {
                        newGroup.RectChildren.Add(child);
                        pos = childSize * scaleFactor + spacing;
                    }

                    currentGroup = newGroup;

                    //Last iteraction so we must force add currentgroup to groups
                    if (i == rectChildren.Count - 1 && newGroup.RectChildren.Count > 0)
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

            if (!alongOtherAxis && rectChildren.Count > 0)
            {
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

            //Pick PreFilter of MaxLayout
            List<int> preFilter = group.AxisPreFilterIndexes[axis];
            if (preFilter == null)
            {
                preFilter = new List<int>();
                group.AxisPreFilterIndexes[axis] = preFilter;
            }
            else
                preFilter.Clear();

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            for (int i = 0; i < group.RectChildren.Count; i++)
            {
                RectTransform child = group.RectChildren[i];
                float min, preferred, flexible, max;
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);

                //Add this child to be pre-processed before SetLayout (We must pre-process elements with Max Value)
                if (max >= 0 && flexible > 0)
                    preFilter.Add(i);

                if (useScale)
                {
                    float scaleFactor = child.localScale[axis];
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

            if (!alongOtherAxis && rectChildren.Count > 0)
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

            //Apply Inner Offset
            var innerOffset = GetInnerOffsetOnAxis(axis, size);
            size -= (innerOffset.x + innerOffset.y);

            float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
            bool alongOtherAxis = (isVertical ^ (axis == 1));

            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

            //Calculate initial position
            float pos = (axis == 0 ? padding.left : padding.top);
            float surplusSpace = size - GetTotalPreferredSize(axis);

            float itemFlexibleMultiplier = 0;
            var totalFlexibleSize = GetTotalFlexibleSize(axis);
            if (surplusSpace > 0 && alongOtherAxis)
            {
                if (totalFlexibleSize == 0)
                {
                    if (!controlSize || !childForceExpandSize)
                        pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                }
                else if (totalFlexibleSize > 0)
                    itemFlexibleMultiplier = surplusSpace / totalFlexibleSize;
            }

            //Apply InnerOffset to Pos
            pos += innerOffset.x;

            for (int i = 0; i < _Groups.Count; i++)
            {
                LinearGroup group = _Groups[i];

                var groupSize = group.size;
                groupSize[axis] = alongOtherAxis ? Mathf.Max(group.GetTotalMinSize(axis), group.GetTotalPreferredSize(axis)) : innerSize;
                if (alongOtherAxis && itemFlexibleMultiplier > 0)
                    groupSize[axis] += (group.GetTotalFlexibleSize(axis) * itemFlexibleMultiplier);
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
                for (int i = 0; i < group.RectChildren.Count; i++)
                {
                    RectTransform child = group.RectChildren[i];
                    float min, preferred, flexible, max;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float requiredSpace = Mathf.Clamp(size, min, flexible > 0 ? size : preferred);
                    //Apply max value
                    if (max >= 0 && requiredSpace > max)
                        requiredSpace = max;

                    float startOffset = pos + group.GetStartOffset(axis, requiredSpace * scaleFactor, alignmentOnAxis);
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child, axis, startOffset, requiredSpace, scaleFactor);
                    }
                    else
                    {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, startOffset + offsetInCell, scaleFactor);
                    }
                }
            }
            else
            {
                float pos = group.position[axis];
                float itemFlexibleMultiplier = 0;
                float surplusSpace = size - group.GetTotalPreferredSize(axis);

                var totalFlexibleSize = group.GetTotalFlexibleSize(axis);
                var useFlexibleSpacing = false;
                if (surplusSpace > 0)
                {
                    if (totalFlexibleSize == 0)
                    {
                        if (!controlSize || !childForceExpandSize)
                        {
                            pos += group.GetStartOffset(axis, group.GetTotalPreferredSize(axis), alignmentOnAxis);
                        }
                        else
                            useFlexibleSpacing = true;
                    }
                    else if (totalFlexibleSize > 0)
                        itemFlexibleMultiplier = surplusSpace / totalFlexibleSize;
                }

                float minMaxLerp = 0;
                if (group.GetTotalMinSize(axis) != group.GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - group.GetTotalMinSize(axis)) / (group.GetTotalPreferredSize(axis) - group.GetTotalMinSize(axis)));

                var currentSpacing = useFlexibleSpacing && group.RectChildren.Count - 1 > 0 ? Mathf.Max(spacing, surplusSpace / (group.RectChildren.Count - 1)) : spacing;

                var remainingSurplusPerChilden = GetRemainingSurplusPerChildren(group.AxisPreFilterIndexes, group.RectChildren, axis, controlSize, childForceExpandSize, minMaxLerp, itemFlexibleMultiplier, totalFlexibleSize);

                for (int i = 0; i < group.RectChildren.Count; i++)
                {
                    RectTransform child = group.RectChildren[i];
                    float min, preferred, flexible, max;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    //We will only expand if childcontrol size is false
                    float desiredChildSize = childSize + (flexible * itemFlexibleMultiplier);
                    if (max >= 0 && desiredChildSize > max)
                    {
                        childSize = max;
                    }
                    else
                    {
                        //Non MaxElements contains RemainingSurplusPerChildren calculated in GetRemainingSurplusPerChildren() Before Loop begins (Aka Pre-Filter)
                        childSize = desiredChildSize + (flexible * remainingSurplusPerChilden);
                    }
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child, axis, pos, childSize, scaleFactor);
                    }
                    else
                    {
                        float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, pos + offsetInCell, scaleFactor);
                    }
                    pos += childSize * scaleFactor + currentSpacing;
                }
            }
        }

        #endregion
    }
}
