using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    public abstract class FastLayoutGroup : UIBehaviour, IFastLayoutGroup
    {
        #region Helper Classes

        [System.Serializable]
        public class IntUnityEvent : UnityEvent<int> { };

        public enum ForceExpandMode { Inflate, KeepSizeWhenChildControl }

        #endregion

        #region Private Variables

        [SerializeField] bool m_ReverseOrder = false;

        [SerializeField] protected RectOffset m_Padding = new RectOffset();
        [SerializeField] protected TextAnchor m_ChildAlignment = TextAnchor.UpperLeft;
        [System.NonSerialized] protected RectTransform m_Rect;

        protected DrivenRectTransformTracker m_Tracker;
        protected Vector2 m_TotalMinSize = Vector2.zero;
        protected Vector2 m_TotalPreferredSize = Vector2.zero;
        protected Vector2 m_TotalFlexibleSize = Vector2.zero;

        [System.NonSerialized] protected List<IFastLayoutFeedback> m_Children = new List<IFastLayoutFeedback>();
        protected List<IFastLayoutFeedback> children { get { return m_Children; } }

        protected HashSet<IFastLayoutFeedback> _dirtyChildren = new HashSet<IFastLayoutFeedback>();
        protected DrivenAxis _dirtyAxis = DrivenAxis.None;

        protected float _cachedRectHeight = -1;
        protected float _cachedRectWidth = -1;

        #endregion

        #region Properties

        public bool reverseOrder
        {
            get
            {
                return m_ReverseOrder;
            }
            set
            {
                SetProperty(ref m_ReverseOrder, value);
            }
        }

        /// <summary>
        /// The padding to add around the child layout elements.
        /// </summary>
        public RectOffset padding
        {
            get
            {
                return m_Padding;
            }
            set
            {
                SetProperty(ref m_Padding, value);
            }

        }

        /// <summary>
        /// The alignment to use for the child layout elements in the layout group.
        /// </summary>
        /// <remarks>
        /// If a layout element does not specify a flexible width or height, its child elements many not use the available space within the layout group. In this case, use the alignment settings to specify how to align child elements within their layout group.
        /// </remarks>
        public TextAnchor childAlignment { get { return m_ChildAlignment; } set { SetProperty(ref m_ChildAlignment, value); } }

        public RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        public virtual bool isDirty
        {
            get
            {
                return _dirtyChildren.Count > 0 || isAxisDirty;
            }
            protected set
            {
                if (value)
                {
                    isAxisDirty = true;
                }
                else
                {
                    _dirtyChildren.Clear();
                    isAxisDirty = false;

                }
            }
        }

        public virtual bool isAxisDirty
        {
            get
            {
                return _dirtyAxis.HasFlag(DrivenAxis.Ignore) || (_dirtyAxis & parentControlledAxis) != 0;
            }
            protected set
            {
                if (value)
                {
                    _dirtyAxis = parentControlledAxis | DrivenAxis.Ignore;
                }
                else
                {
                    _dirtyAxis = DrivenAxis.None;

                }
            }
        }

        private bool isRootLayoutGroup
        {
            get
            {
                Transform parent = transform.parent;
                if (parent == null)
                    return true;
                var parentLayout = transform.parent.GetComponent(typeof(ILayoutController));
                return parentLayout == null || !(parentLayout is ILayoutGroup) || !(parentLayout is IFastLayoutGroup);
            }
        }

        protected abstract DrivenAxis parentControlledAxis
        {
            get;
            set;
        }

        protected virtual DrivenAxis childrenControlledAxis
        {
            get
            {
                return ~parentControlledAxis;
            }
            set
            {

            }
        }

        DrivenAxis IFastLayoutGroup.parentControlledAxis
        {
            get
            {
                return parentControlledAxis;
            }
            set
            {
                parentControlledAxis = value;
            }
        }

        DrivenAxis IFastLayoutGroup.childrenControlledAxis
        {
            get
            {
                return childrenControlledAxis;
            }
            set
            {
                childrenControlledAxis = value;
            }
        }

        protected virtual float cachedRectWidth
        {
            get
            {
                return _cachedRectWidth;
            }
            set
            {
                if (_cachedRectWidth == value)
                    return;
                _cachedRectWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        protected virtual float cachedRectHeight
        {
            get
            {
                return _cachedRectHeight;
            }
            set
            {
                if (_cachedRectHeight == value)
                    return;
                _cachedRectHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        #endregion

        #region Constructor

        protected FastLayoutGroup()
        {
            if (m_Padding == null)
                m_Padding = new RectOffset();
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            if (isDirty)
                MarkLayoutForRebuild();
        }

        protected override void OnDisable()
        {
            CancelInvoke();
            m_Tracker.Clear();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            CalculateRectTransformDimensions();

            var isDirty = (_dirtyAxis & parentControlledAxis) != 0;
            //We will only set dirty if value of new rect is smaller than preferred size
            if (isDirty &&
                (_dirtyAxis.HasFlag(DrivenAxis.Vertical) && _cachedRectHeight < m_TotalPreferredSize.y) ||
                (_dirtyAxis.HasFlag(DrivenAxis.Horizontal) && _cachedRectWidth < m_TotalPreferredSize.x))
                isDirty = false;

            //Prevent change size while calculating feedback
            if (isDirty)
                SetDirty();
            else
                _dirtyAxis &= ~(DrivenAxis.Horizontal | DrivenAxis.Vertical);
        }

        protected virtual void OnTransformChildrenChanged()
        {
            ValidateLayoutFeedbacksInChildren();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif
        #endregion

        #region Helper Functions

        protected virtual void CalculateRectTransformDimensions()
        {
            cachedRectWidth = rectTransform.rect.size.x;
            cachedRectHeight = rectTransform.rect.size.y;
        }

        /// <summary>
        /// The min size for the layout group on the given axis.
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <returns>The min size</returns>
        protected virtual float GetTotalMinSize(int axis)
        {
            return m_TotalMinSize[axis];
        }

        /// <summary>
        /// The preferred size for the layout group on the given axis.
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <returns>The preferred size.</returns>
        protected virtual float GetTotalPreferredSize(int axis)
        {
            return m_TotalPreferredSize[axis];
        }

        /// <summary>
        /// The flexible size for the layout group on the given axis.
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <returns>The flexible size</returns>
        protected virtual float GetTotalFlexibleSize(int axis)
        {
            return m_TotalFlexibleSize[axis];
        }

        /// <summary>
        /// Returns the calculated position of the first child layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <param name="requiredSpaceWithoutPadding">The total space required on the given axis for all the layout elements including spacing and excluding padding.</param>
        /// <returns>The position of the first child along the given axis.</returns>
        protected virtual float GetStartOffset(int axis, float requiredSpaceWithoutPadding)
        {
            float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? padding.horizontal : padding.vertical);
            float availableSpace = rectTransform.rect.size[axis];
            float surplusSpace = availableSpace - requiredSpace;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            return (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;
        }

        /// <summary>
        /// Returns the alignment on the specified axis as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.
        /// </summary>
        /// <param name="axis">The axis to get alignment along. 0 is horizontal and 1 is vertical.</param>
        /// <returns>The alignment as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.</returns>
        protected virtual float GetAlignmentOnAxis(int axis)
        {
            if (axis == 0)
                return ((int)childAlignment % 3) * 0.5f;
            else
                return ((int)childAlignment / 3) * 0.5f;
        }

        /// <summary>
        /// Used to set the calculated layout properties for the given axis.
        /// </summary>
        /// <param name="totalMin">The min size for the layout group.</param>
        /// <param name="totalPreferred">The preferred size for the layout group.</param>
        /// <param name="totalFlexible">The flexible size for the layout group.</param>
        /// <param name="axis">The axis to set sizes for. 0 is horizontal and 1 is vertical.</param>
        protected virtual void SetLayoutInputForAxis(float totalMin, float totalPreferred, float totalFlexible, int axis)
        {
            m_TotalMinSize[axis] = totalMin;
            m_TotalPreferredSize[axis] = totalPreferred;
            m_TotalFlexibleSize[axis] = totalFlexible;
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        protected virtual void SetChildAlongAxis(RectTransform rect, int axis, float pos)
        {
            SetChildAlongAxisWithScale(rect, axis, pos, 1.0f);
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        protected virtual void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float scaleFactor)
        {
            SetChildAlongAxisWithScale(rect, axis, pos, scaleFactor, Vector2.up, Vector2.up);
        }

        protected virtual void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, Vector2 anchorMin, Vector2 anchorMax)
        {
            SetChildAlongAxisWithScale(rect, axis, pos, 1.0f, anchorMin, anchorMax);
        }

        protected virtual void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float scaleFactor, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
                return;

            SetChildAlongAxisWithScale(rect, axis, pos, rect.rect.size[axis], scaleFactor, anchorMin, anchorMax);
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        /// <param name="size">The size.</param>
        protected virtual void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size)
        {
            SetChildAlongAxisWithScale(rect, axis, pos, size, 1.0f);
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        /// <param name="size">The size.</param>
        protected virtual void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float size, float scaleFactor)
        {
            SetChildAlongAxisWithScale(rect, axis, pos, size, scaleFactor, Vector2.up, Vector2.up);
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        /// <param name="size">The size.</param>
        protected virtual void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size, Vector2 anchorMin, Vector2 anchorMax)
        {
            SetChildAlongAxisWithScale(rect, axis, pos, size, 1.0f, anchorMin, anchorMax);
        }

        protected virtual void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float size, float scaleFactor, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                (axis == 0 ?
                    (DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX) :
                    (DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY)
                )
            );

            // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
            // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;

            // Freaking insane calculation based in parent rect TopLeft anchor position
            var rectAlign = GetAlignmentOnAxis(axis) * (anchorMax[axis] - anchorMin[axis]);
            Vector2 anchoredPosition = rect.localPosition;

            //Calculate based in self pivot
            anchoredPosition[axis] = (axis == 0) ? (pos + size * rect.pivot[axis] * scaleFactor) : (-pos - size * (1f - rect.pivot[axis]) * scaleFactor);

            //Offset based in parent size and self anchorMin/anchorMax align
            anchoredPosition[axis] += (axis == 0) ? 
                this.rectTransform.rect.xMin - (this.rectTransform.rect.width * rectAlign) : 
                this.rectTransform.rect.yMax + (this.rectTransform.rect.height * rectAlign);
            rect.localPosition = anchoredPosition;

            //Anchor changed so we must apply this size again regardless if size is the same
            Vector2 sizeDelta = rect.rect.size;
            sizeDelta[axis] = size;
            rect.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, size);
        }

        /// <summary>
        /// Helper method used to set a given property if it has changed.
        /// </summary>
        /// <param name="currentValue">A reference to the member value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return;
            currentValue = newValue;
            SetDirty();
        }

        protected virtual bool IsAxisDirty(bool isVertical)
        {
            if (!Application.isPlaying)
                return true;

            bool isAxisDirty = _dirtyAxis.HasFlag(DrivenAxis.Ignore) || (_dirtyAxis & parentControlledAxis & (isVertical ? DrivenAxis.Horizontal : DrivenAxis.Vertical)) != 0;
            return isAxisDirty;
        }

        protected virtual bool IsAxisDirty(int axis)
        {
            return IsAxisDirty(axis != 0);
        }

        #endregion

        #region Dirty Functions

        public virtual void SetDirty()
        {
            SetAxisDirty(DrivenAxis.Horizontal | DrivenAxis.Vertical | DrivenAxis.Ignore);
        }

        protected void SetAxisDirty(DrivenAxis dirtyAxis)
        {
            if (!isDirty)
                MarkLayoutForRebuild();

            _dirtyAxis |= dirtyAxis;
        }

        public virtual void SetElementDirty(IFastLayoutFeedback driven, DrivenAxis dirtyAxis)
        {
            if (driven != null && (dirtyAxis.HasFlag(DrivenAxis.Ignore) || (dirtyAxis & childrenControlledAxis) != 0))
            {
                if (!isDirty)
                    MarkLayoutForRebuild();

                _dirtyChildren.Add(driven);
            }
        }

        protected virtual void MarkLayoutForRebuild()
        {
            if (IsDestroyed() || !IsActive())
                return;

            if (!CanvasUpdateRegistry.IsRebuildingLayout())
            {
                CancelInvoke(nameof(MarkLayoutForRebuild));
                LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
            }
            else if (!IsInvoking(nameof(MarkLayoutForRebuild)))
                Invoke(nameof(MarkLayoutForRebuild), 0);
        }

        #endregion

        #region ILayout Functions

        public virtual float minWidth { get { return GetTotalMinSize(0); } }
        public virtual float preferredWidth { get { return GetTotalPreferredSize(0); } }
        public virtual float flexibleWidth { get { return GetTotalFlexibleSize(0); } }
        public virtual float minHeight { get { return GetTotalMinSize(1); } }
        public virtual float preferredHeight { get { return GetTotalPreferredSize(1); } }
        public virtual float flexibleHeight { get { return GetTotalFlexibleSize(1); } }
        public virtual int layoutPriority { get { return 0; } }

        protected int _initialDirtyIndex = -1;
        public virtual void CalculateLayoutInputHorizontal()
        {
            _initialDirtyIndex = -1;
            m_Children.Clear();
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                var rect = rectTransform.GetChild(i) as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                var layoutFeedbackElement = rect.GetComponent<IFastLayoutFeedback>();
                if (layoutFeedbackElement == null)
                {
                    layoutFeedbackElement = rect.gameObject.AddComponent<FastLayoutFeedback>();
                    layoutFeedbackElement.CalculateLayoutInputHorizontal();
                    layoutFeedbackElement.CalculateLayoutInputVertical();
                }
                else
                {
                    layoutFeedbackElement.CalculateLayoutIgnore();
                }

                if (!layoutFeedbackElement.cachedLayoutIgnore)
                {
                    if (_initialDirtyIndex < 0 && _dirtyChildren.Contains(layoutFeedbackElement))
                        _initialDirtyIndex = m_Children.Count;
                    m_Children.Add(layoutFeedbackElement);
                }
            }
            m_Tracker.Clear();

            if (m_ReverseOrder)
            {
                children.Reverse();
                _initialDirtyIndex = _initialDirtyIndex >= 0 ? m_Children.Count - 1 - _initialDirtyIndex : -1;
            }
        }

        protected virtual void ValidateLayoutFeedbacksInChildren()
        {
            var isRebuildingLayout = CanvasUpdateRegistry.IsRebuildingLayout();
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                var rect = rectTransform.GetChild(i) as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                var layoutFeedbackElement = rect.GetComponent<IFastLayoutFeedback>();
                if (layoutFeedbackElement == null)
                {
                    layoutFeedbackElement = rect.gameObject.AddComponent<FastLayoutFeedback>();
                    if (isRebuildingLayout)
                    {
                        layoutFeedbackElement.CalculateLayoutInputHorizontal();
                        layoutFeedbackElement.CalculateLayoutInputVertical();
                    }
                }
                else if (i < m_Children.Count && layoutFeedbackElement != m_Children[i])
                    SetElementDirty(m_Children[i], DrivenAxis.Ignore);
            }
        }

        public abstract void CalculateLayoutInputVertical();

        // ILayoutController Interface
        public abstract void SetLayoutHorizontal();
        public abstract void SetLayoutVertical();

        #endregion

        #region ILayout Functions Internal

        void ILayoutElement.CalculateLayoutInputHorizontal()
        {
            if (isDirty)
            {
                Debug.Log("Recalculate " + name);
                CalculateLayoutInputHorizontal();
            }
        }

        void ILayoutElement.CalculateLayoutInputVertical()
        {
            if (isDirty)
                CalculateLayoutInputVertical();
        }

        void ILayoutController.SetLayoutHorizontal()
        {
            if (isDirty)
                SetLayoutHorizontal();
        }

        void ILayoutController.SetLayoutVertical()
        {
            if (isDirty)
            {
                SetLayoutVertical();
                isDirty = false;
            }
        }

        #endregion
    }
}
