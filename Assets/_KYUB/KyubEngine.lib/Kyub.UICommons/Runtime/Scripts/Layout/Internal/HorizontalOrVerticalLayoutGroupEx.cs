using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
    //Class that control IMaxLayoutElement
    public interface IMaxLayoutGroup : ILayoutGroup
    { }

    /// <summary>
    /// Abstract base class for HorizontalLayoutGroup and VerticalLayoutGroup to generalize common functionality.
    /// </summary>
    [ExecuteAlways]
    public abstract class HorizontalOrVerticalLayoutGroupEx : HorizontalOrVerticalLayoutGroup, IMaxLayoutGroup
    {
        public enum ForceExpandMode { Inflate, KeepSizeWhenChildControl }

        [SerializeField, Tooltip("- Inflate will act exactly like old Horizontal/Vertical LayoutGroup changing child flexible size to 1.\n" +
            "- KeepSizeWhenChildControl will prevent change flexible size when controlled by child, but will expand spacement between elements. ")]
        ForceExpandMode m_ForceExpandMode = ForceExpandMode.Inflate;
        [SerializeField]
        bool m_ReverseOrder = false;


        [SerializeField]
        float m_MaxInnerWidth = -1;
        [SerializeField]
        float m_MaxInnerHeight = -1;
        [SerializeField]
        TextAnchor m_InnerAlign = TextAnchor.MiddleCenter;

        public bool reverseOrder { get { return m_ReverseOrder; } set { SetProperty(ref m_ReverseOrder, value); } }
        public ForceExpandMode forceExpandMode { get { return m_ForceExpandMode; } set { SetProperty(ref m_ForceExpandMode, value); } }

        public TextAnchor innerAlign { get { return m_InnerAlign; } set { SetProperty(ref m_InnerAlign, value); } }
        public float maxInnerWidth { get { return m_MaxInnerWidth; } set { SetProperty(ref m_MaxInnerWidth, value); } }
        public float maxInnerHeight { get { return m_MaxInnerHeight; } set { SetProperty(ref m_MaxInnerHeight, value); } }

        Dictionary<int, List<int>> m_AxisPreFilterIndexes = new Dictionary<int, List<int>>() { { 0, new List<int>() }, { 1, new List<int>() } };

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            if (m_ReverseOrder)
                rectChildren.Reverse();
        }

        /// <summary>
        /// Calculate the layout element properties for this layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected virtual new void CalcAlongAxis(int axis, bool isVertical)
        {
            float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

            float totalMin = combinedPadding;
            float totalPreferred = combinedPadding;
            float totalFlexible = 0;

            //Pick PreFilter of MaxLayout
            List<int> preFilter = m_AxisPreFilterIndexes[axis];
            if (preFilter == null)
            {
                preFilter = new List<int>();
                m_AxisPreFilterIndexes[axis] = preFilter;
            }
            else
                preFilter.Clear();

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
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
                    totalMin = Mathf.Max(min + combinedPadding, totalMin);
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
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
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

        /// <summary>
        /// Set the positions and sizes of the child layout elements for the given axis.
        /// </summary>
        /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected virtual new void SetChildrenAlongAxis(int axis, bool isVertical)
        {
            float size = rectTransform.rect.size[axis];
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);

            //Apply Inner Offset
            var innerOffset = GetInnerOffsetOnAxis(axis, size);
            size -= (innerOffset.x + innerOffset.y);

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            if (alongOtherAxis)
            {
                float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible, max;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                    //Apply max value
                    if (max >= 0 && requiredSpace > max)
                        requiredSpace = max;

                    float startOffset = GetStartOffset(axis, requiredSpace * scaleFactor);
                    startOffset += innerOffset.x;
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
                float pos = (axis == 0 ? padding.left : padding.top);
                float itemFlexibleMultiplier = 0;
                float surplusSpace = size - GetTotalPreferredSize(axis);

                var totalFlexibleSize = GetTotalFlexibleSize(axis);
                var useFlexibleSpacing = false;
                if (surplusSpace > 0)
                {
                    if (totalFlexibleSize == 0)
                    {
                        if (!controlSize || !childForceExpandSize)
                            pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                        else
                            useFlexibleSpacing = true;
                    }
                    else if (totalFlexibleSize > 0)
                        itemFlexibleMultiplier = surplusSpace / totalFlexibleSize;
                }

                //Apply InnerOffset to Pos
                pos += innerOffset.x;

                float minMaxLerp = 0;
                if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

                var currentSpacing = useFlexibleSpacing && rectChildren.Count - 1  > 0? Mathf.Max(spacing, surplusSpace/(rectChildren.Count-1)) : spacing;

                var remainingSurplusPerChilden = GetRemainingSurplusPerChildren(m_AxisPreFilterIndexes, rectChildren, axis, controlSize, childForceExpandSize, minMaxLerp, itemFlexibleMultiplier, totalFlexibleSize);

                for (int i = 0; i < rectChildren.Count; i++)
                {
                    RectTransform child = rectChildren[i];
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

        protected virtual Vector2 GetInnerOffsetOnAxis(int axis, float size)
        {
            float maxInner = axis == 0 ? m_MaxInnerWidth : m_MaxInnerHeight;
            float innerAlignment = (axis == 0 ? (int)innerAlign % 3 : (int)innerAlign / 3) * 0.5f;

            var deltaSize = size > maxInner && maxInner >= 0? (size - maxInner) : 0;
            float innerMin = deltaSize >= 0 ? innerAlignment * deltaSize : 0;
            float innerMax = deltaSize >= 0 ? deltaSize - innerMin : 0;

            return new Vector2(innerMin, innerMax);
        }

        protected virtual float GetRemainingSurplusPerChildren(Dictionary<int, List<int>> axisPreFilterIndexes, List<RectTransform> rectChildren, int axis, bool controlSize, bool childForceExpandSize, float minMaxLerp, float itemFlexibleMultiplier, float totalFlexibleSize)
        {
            List<int> preFilterIndexes = axisPreFilterIndexes[axis];
            var totalChildsCount = rectChildren.Count;

            if (totalFlexibleSize > 0 && totalChildsCount > preFilterIndexes.Count)
            {
                float othersTotalFlexible = totalFlexibleSize;
                float remainingSurplus = 0;
                for (int i = 0; i < preFilterIndexes.Count; i++)
                {
                    RectTransform child = rectChildren[preFilterIndexes[i]];
                    float min, preferred, flexible, max;

                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);

                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    //We will only expand if childcontrol size is false or
                    float desiredChildSize = childSize + (flexible * itemFlexibleMultiplier);

                    if (max >= 0 && desiredChildSize > max)
                    {
                        if (flexible > 0)
                            othersTotalFlexible -= flexible;
                        remainingSurplus += (desiredChildSize - max);
                    }
                }

                if (remainingSurplus > 0)
                {
                    //Get Normalized Surplus
                    remainingSurplus = (remainingSurplus / othersTotalFlexible);
                }
                return remainingSurplus;
            }
            return 0;
        }

        protected virtual void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand,
            out float min, out float preferred, out float flexible)
        {
            float max;
            GetChildSizes(child, axis, controlSize, childForceExpand, out min, out preferred, out flexible, out max);
        }

        protected virtual void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand,
            out float min, out float preferred, out float flexible, out float max)
        {
            if (!controlSize)
            {
                max = -1;
                min = child.sizeDelta[axis];
                preferred = min;
                flexible = 0;
                
            }
            else
            {
                max = LayoutUtilityEx.GetMaxSize(child, axis, -1);
                min = LayoutUtility.GetMinSize(child, axis);
                preferred = LayoutUtility.GetPreferredSize(child, axis);
                flexible = LayoutUtility.GetLayoutProperty(child, (element) => { return axis == 0 ? element.flexibleWidth : element.flexibleHeight; }, -1);

                //Clamp Preferred based in max
                if (max >= 0 && preferred > max)
                    preferred = max;
            }

            //Prevent expand when child control size
            if (childForceExpand && (m_ForceExpandMode == ForceExpandMode.Inflate || !controlSize))
                flexible = Mathf.Max(flexible, 1);

            //Prevent negative values on return
            flexible = Mathf.Max(flexible, 0);
        }
    }
}
