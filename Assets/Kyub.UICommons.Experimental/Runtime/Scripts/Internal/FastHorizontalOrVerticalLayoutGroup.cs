using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    [ExecuteAlways]
    public abstract class FastHorizontalOrVerticalLayoutGroup : FastLayoutGroup, IMaxLayoutGroup
    {
        #region Private Variables
        [SerializeField, Tooltip("- Inflate will act exactly like old Horizontal/Vertical LayoutGroup changing child flexible size to 1.\n" +
                                 "- KeepSizeWhenChildControl will prevent change flexible size when controlled by child, but will expand spacement between elements. ")]
        ForceExpandMode m_ForceExpandMode = ForceExpandMode.Inflate;

        [SerializeField] protected float m_Spacing = 0;
        [SerializeField] protected bool m_ChildForceExpandWidth = true;
        [SerializeField] protected bool m_ChildForceExpandHeight = true;
        [SerializeField] protected bool m_ChildControlWidth = true;
        [SerializeField] protected bool m_ChildControlHeight = true;
        [SerializeField] protected bool m_ChildScaleWidth = false;
        [SerializeField] protected bool m_ChildScaleHeight = false;

        [SerializeField] protected float m_MaxInnerWidth = -1;
        [SerializeField] protected float m_MaxInnerHeight = -1;
        [SerializeField] protected TextAnchor m_InnerAlign = TextAnchor.MiddleCenter;

        Dictionary<int, List<int>> m_AxisPreFilterIndexes = new Dictionary<int, List<int>>() { { 0, new List<int>() }, { 1, new List<int>() } };

        #endregion

        #region Properties

        public TextAnchor innerAlign { get { return m_InnerAlign; } set { SetProperty(ref m_InnerAlign, value); } }
        public float maxInnerWidth { get { return m_MaxInnerWidth; } set { SetProperty(ref m_MaxInnerWidth, value); } }
        public float maxInnerHeight { get { return m_MaxInnerHeight; } set { SetProperty(ref m_MaxInnerHeight, value); } }

        public float spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

        public bool childForceExpandWidth { get { return m_ChildForceExpandWidth; } set { SetProperty(ref m_ChildForceExpandWidth, value); } }

        public bool childForceExpandHeight { get { return m_ChildForceExpandHeight; } set { SetProperty(ref m_ChildForceExpandHeight, value); } }

        public bool childControlWidth { get { return m_ChildControlWidth; } set { SetProperty(ref m_ChildControlWidth, value); } }

        public bool childControlHeight { get { return m_ChildControlHeight; } set { SetProperty(ref m_ChildControlHeight, value); } }

        public bool childScaleWidth { get { return m_ChildScaleWidth; } set { SetProperty(ref m_ChildScaleWidth, value); } }

        public bool childScaleHeight { get { return m_ChildScaleHeight; } set { SetProperty(ref m_ChildScaleHeight, value); } }

        public ForceExpandMode forceExpandMode { get { return m_ForceExpandMode; } set { SetProperty(ref m_ForceExpandMode, value); } }

        #endregion

        #region Unity Functions

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();

            // For new added components we want these to be set to false,
            // so that the user's sizes won't be overwritten before they
            // have a chance to turn these settings off.
            // However, for existing components that were added before this
            // feature was introduced, we want it to be on be default for
            // backwardds compatibility.
            // Hence their default value is on, but we set to off in reset.
            m_ChildControlWidth = false;
            m_ChildControlHeight = false;
        }
#endif

        #endregion

        #region Helper Functions

        protected virtual void CalcRectTransformChanged(bool isVertical)
        {
            CalculateRectTransformDimensions();

            var isDirty = (_dirtyAxis & parentControlledAxis & (isVertical ? DrivenAxis.Vertical : DrivenAxis.Horizontal)) != 0;
            //We will only set dirty if value of new rect is smaller than preferred size or if flexible size is empty
            if (isDirty &&
                (isVertical && (_cachedRectHeight < m_TotalPreferredSize.y || m_TotalFlexibleSize.y == 0)) ||
                (!isVertical && (_cachedRectWidth < m_TotalPreferredSize.x || m_TotalFlexibleSize.x == 0)))
                isDirty = false;

            //Prevent change size while calculating feedback
            if (isDirty)
                SetDirty();
            else
                _dirtyAxis &= ~(DrivenAxis.Horizontal | DrivenAxis.Vertical);
        }

        /// <summary>
        /// Calculate the layout element properties for this layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected virtual void CalcAlongAxis(int axis, bool isVertical)
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
            for (int i = 0; i < children.Count; i++)
            {
                IFastLayoutFeedback child = children[i];

                if (child == null)
                    return;

                float min, preferred, flexible, max;
                GetChildSizes(children[i], axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);

                //Add this child to be pre-processed before SetLayout (We must pre-process elements with Max Value)
                if (max >= 0 && flexible > 0)
                    preFilter.Add(i);

                if (useScale)
                {
                    float scaleFactor = child.rectTransform != null? child.rectTransform.localScale[axis] : 1.0f;
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

            if (!alongOtherAxis && children.Count > 0)
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
        protected virtual void SetChildrenAlongAxis(int axis, bool isVertical)
        {
            float size = rectTransform.rect.size[axis];
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);

            //Apply Inner Offset
            var innerOffset = GetInnerOffsetOnAxis(axis, size);
            size -= (innerOffset.x + innerOffset.y);

            var isAxisDirty = IsAxisDirty(isVertical);
            bool alongOtherAxis = (isVertical ^ (axis == 1));
            var defaultAnchor = GetDefaultChildAnchor();
            if (alongOtherAxis)
            {
                float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);

                var elements = (isAxisDirty ? (ICollection<IFastLayoutFeedback>)children : _dirtyChildren);
                foreach (IFastLayoutFeedback child in elements)
                {
                    if (child.rectTransform == null || child.rectTransform.parent != this.transform)
                        return;

                    float min, preferred, flexible, max;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);

                    float scaleFactor = useScale && child.rectTransform != null? child.rectTransform.localScale[axis] : 1f;

                    Vector2 anchorMin, anchorMax;
                    GetChildAnchors(child, axis, controlSize, childForceExpandSize, defaultAnchor, out anchorMin, out anchorMax);

                    float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                    //Apply max value
                    if (max >= 0 && requiredSpace > max)
                        requiredSpace = max;

                    float startOffset = GetStartOffset(axis, requiredSpace * scaleFactor);
                    startOffset += innerOffset.x;
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child.rectTransform, axis, startOffset, requiredSpace, scaleFactor, anchorMin, anchorMax);
                    }
                    else
                    {
                        float offsetInCell = (requiredSpace - (axis == 0? child.cachedRectWidth : child.cachedRectHeight)) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child.rectTransform, axis, startOffset + offsetInCell, requiredSpace, scaleFactor, anchorMin, anchorMax);
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

                var currentSpacing = useFlexibleSpacing && children.Count - 1 > 0 ? Mathf.Max(spacing, surplusSpace / (children.Count - 1)) : spacing;

                var remainingSurplusPerChilden = GetRemainingSurplusPerChildren(m_AxisPreFilterIndexes, children, axis, controlSize, childForceExpandSize, minMaxLerp, itemFlexibleMultiplier, totalFlexibleSize);

                int initialIndex = isAxisDirty ? 0 : Mathf.Max(0, _initialDirtyIndex);
                for (int i = 0; i < children.Count; i++)
                {
                    IFastLayoutFeedback child = children[i];
                    float min, preferred, flexible, max;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out max);

                    float scaleFactor = useScale && child.rectTransform != null? child.rectTransform.localScale[axis] : 1f;

                    Vector2 anchorMin, anchorMax;
                    GetChildAnchors(child, axis, controlSize, childForceExpandSize, defaultAnchor, out anchorMin, out anchorMax);

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

                    if (i >= initialIndex)
                    {
                        if (controlSize)
                        {
                            SetChildAlongAxisWithScale(child.rectTransform, axis, pos, childSize, scaleFactor, anchorMin, anchorMax);
                        }
                        else
                        {
                            var sizeDelta = axis == 0 ? child.cachedRectWidth : child.cachedRectHeight;
                            float offsetInCell = (childSize - sizeDelta) * alignmentOnAxis;
                            SetChildAlongAxisWithScale(child.rectTransform, axis, pos + offsetInCell, childSize, scaleFactor, anchorMin, anchorMax);
                        }
                    }
                    pos += childSize * scaleFactor + currentSpacing;
                }
            }
        }

        protected virtual void GetChildAnchors(IFastLayoutFeedback child, int axis, bool controlSize, bool childForceExpand, Vector2 defaultAnchor,
            out Vector2 minAnchor, out Vector2 maxAnchor)
        {
            minAnchor = child != null && child.rectTransform != null? child.rectTransform.anchorMin : defaultAnchor;
            maxAnchor = child != null && child.rectTransform != null ? child.rectTransform.anchorMax : defaultAnchor;

            var drivenAxis = axis == 0 ? DrivenAxis.Horizontal : DrivenAxis.Vertical;
            var expand = !controlSize && (parentControlledAxis & drivenAxis) != 0;
            if (expand)
            {
                minAnchor[axis] = 0;
                maxAnchor[axis] = 1;
            }
            else
            {
                minAnchor[axis] = defaultAnchor[axis];
                maxAnchor[axis] = minAnchor[axis];
            }

            
        }

        protected Vector2 GetDefaultChildAnchor()
        {
            Vector2 anchor;
            if (childAlignment == TextAnchor.LowerLeft)
                anchor = new Vector2(0, 0);
            else if (childAlignment == TextAnchor.LowerRight)
                anchor = new Vector2(1, 0);
            else if (childAlignment == TextAnchor.LowerCenter)
                anchor = new Vector2(0.5f, 0);
            else if (childAlignment == TextAnchor.MiddleLeft)
                anchor = new Vector2(0, 0.5f);
            else if (childAlignment == TextAnchor.MiddleRight)
                anchor = new Vector2(1, 0.5f);
            else if (childAlignment == TextAnchor.MiddleCenter)
                anchor = new Vector2(0.5f, 0.5f);
            else if (childAlignment == TextAnchor.UpperLeft)
                anchor = new Vector2(0, 1);
            else if (childAlignment == TextAnchor.UpperRight)
                anchor = new Vector2(1, 1);
            else
                anchor = new Vector2(0.5f, 1);

            return anchor;
        }

        protected virtual Vector2 GetInnerOffsetOnAxis(int axis, float size)
        {
            float maxInner = axis == 0 ? m_MaxInnerWidth : m_MaxInnerHeight;
            float innerAlignment = (axis == 0 ? (int)innerAlign % 3 : (int)innerAlign / 3) * 0.5f;

            var deltaSize = size > maxInner && maxInner >= 0 ? (size - maxInner) : 0;
            float innerMin = deltaSize >= 0 ? innerAlignment * deltaSize : 0;
            float innerMax = deltaSize >= 0 ? deltaSize - innerMin : 0;

            return new Vector2(innerMin, innerMax);
        }

        protected virtual float GetRemainingSurplusPerChildren(Dictionary<int, List<int>> axisPreFilterIndexes, List<IFastLayoutFeedback> children, int axis, bool controlSize, bool childForceExpandSize, float minMaxLerp, float itemFlexibleMultiplier, float totalFlexibleSize)
        {
            List<int> preFilterIndexes = axisPreFilterIndexes[axis];
            var totalChildsCount = children.Count;

            if (totalFlexibleSize > 0 && totalChildsCount > preFilterIndexes.Count)
            {
                float othersTotalFlexible = totalFlexibleSize;
                float remainingSurplus = 0;
                for (int i = 0; i < preFilterIndexes.Count; i++)
                {
                    IFastLayoutFeedback child = children[preFilterIndexes[i]];
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


        protected virtual void GetChildSizes(IFastLayoutFeedback child, int axis, bool controlSize, bool childForceExpand,
            out float min, out float preferred, out float flexible)
        {
            float max;
            GetChildSizes(child, axis, controlSize, childForceExpand, out min, out preferred, out flexible, out max);
        }

        protected virtual void GetChildSizes(IFastLayoutFeedback child, int axis, bool controlSize, bool childForceExpand,
            out float min, out float preferred, out float flexible, out float max)
        {
            if (!controlSize)
            {
                max = -1;
                min = axis == 0 ? child.cachedRectWidth : child.cachedRectHeight;
                preferred = min;
                flexible = 0;

            }
            else
            {
                max = axis == 0 ? child.cachedMaxWidth : child.cachedMaxHeight;
                min = axis == 0 ? child.cachedMinWidth : child.cachedMinHeight;
                preferred = axis == 0 ? child.cachedPreferredWidth : child.cachedPreferredHeight;
                flexible = axis == 0 ? child.cachedFlexibleWidth : child.cachedFlexibleHeight;

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

        #endregion
    }
}
