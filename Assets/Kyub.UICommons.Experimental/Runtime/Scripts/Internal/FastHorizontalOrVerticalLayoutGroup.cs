using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    [ExecuteAlways]
    public abstract class FastHorizontalOrVerticalLayoutGroup : FastLayoutGroup
    {
        #region Private Variables
        [SerializeField, Tooltip("- Inflate will act exactly like old Horizontal/Vertical LayoutGroup changing child flexible size to 1.\n" +
                                 "- KeepSizeWhenChildControl will prevent change flexible size when controlled by child, but will expand spacement between elements. ")]
        ForceExpandMode m_ForceExpandMode = ForceExpandMode.KeepSizeWhenChildControl;

        [SerializeField] protected float m_Spacing = 0;
        [SerializeField] protected bool m_ChildForceExpandWidth = true;
        [SerializeField] protected bool m_ChildForceExpandHeight = true;
        [SerializeField] protected bool m_ChildControlWidth = true;
        [SerializeField] protected bool m_ChildControlHeight = true;
        [SerializeField] protected bool m_ChildScaleWidth = false;
        [SerializeField] protected bool m_ChildScaleHeight = false;

        #endregion

        #region Properties

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

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            for (int i = 0; i < children.Count; i++)
            {
                IFastLayoutFeedback child = children[i];

                if (child == null)
                    return;

                float min, preferred, flexible;
                GetChildSizes(children[i], axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

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

                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

                    float scaleFactor = useScale && child.rectTransform != null? child.rectTransform.localScale[axis] : 1f;

                    Vector2 anchorMin, anchorMax;
                    GetChildAnchors(child, axis, controlSize, childForceExpandSize, defaultAnchor, out anchorMin, out anchorMax);

                    float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                    float startOffset = GetStartOffset(axis, requiredSpace * scaleFactor);
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

                if (surplusSpace > 0)
                {
                    if (GetTotalFlexibleSize(axis) == 0)
                        pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                    else if (GetTotalFlexibleSize(axis) > 0)
                        itemFlexibleMultiplier = surplusSpace / GetTotalFlexibleSize(axis);
                }

                float minMaxLerp = 0;
                if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

                int initialIndex = isAxisDirty ? 0 : Mathf.Max(0, _initialDirtyIndex);
                for (int i = 0; i < children.Count; i++)
                {
                    IFastLayoutFeedback child = children[i];
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

                    float scaleFactor = useScale && child.rectTransform != null? child.rectTransform.localScale[axis] : 1f;

                    Vector2 anchorMin, anchorMax;
                    GetChildAnchors(child, axis, controlSize, childForceExpandSize, defaultAnchor, out anchorMin, out anchorMax);

                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    childSize += flexible * itemFlexibleMultiplier;

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
                    pos += childSize * scaleFactor + spacing;
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

        protected virtual void GetChildSizes(IFastLayoutFeedback child, int axis, bool controlSize, bool childForceExpand,
            out float min, out float preferred, out float flexible)
        {
            if (!controlSize)
            {
                min = axis == 0 ? child.cachedRectWidth : child.cachedRectHeight;
                preferred = min;
                flexible = 0;
            }
            else
            {
                min = axis == 0 ? child.cachedMinWidth : child.cachedMinHeight;
                preferred = axis == 0 ? child.cachedPreferredWidth : child.cachedPreferredHeight;
                flexible = axis == 0 ? child.cachedFlexibleWidth : child.cachedFlexibleHeight;
            }

            //Prevent expand when child control size
            if (childForceExpand && (flexible < 0 || !controlSize))
                flexible = Mathf.Max(flexible, 1);

            //Prevent negative values on return
            flexible = Mathf.Max(flexible, 0);
        }

        #endregion
    }
}
