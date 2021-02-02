using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    //[ExecuteInEditMode]
    public class ScrollGridLayoutGroup : ScrollLayoutGroup
    {
        #region Helper Classes

        public enum GridConstraintTypeEnum
        {
            Fixed,
            FixedWithFlexibleCells,
            Flexible
        }

        public enum ConstraintAlignEnum
        {
            Begin,
            Middle,
            End
        }

        #endregion

        #region Private Variables

        [Header("Grid Layout Properties")]
        [SerializeField]
        protected GridConstraintTypeEnum m_constraintType = GridConstraintTypeEnum.Flexible;
        [SerializeField]
        protected ConstraintAlignEnum m_constraintAlign = ConstraintAlignEnum.Middle;
        [SerializeField]
        protected Vector2 m_cellSize = new Vector2(100f, 100f);
        [SerializeField]
        protected int m_defaultConstraintCount = 2; //Used to defined amount of rows or collums
        [SerializeField]
        protected float m_constraintSpacing = 0;

        //Allign Grid Caches
        protected float _cachedAlignOffset = 0;
        protected float _cachedConstraintCellSize = 0;
        protected float _cachedConstraintSize = 0;
        protected int _cachedConstraintCount = 0;

        #endregion

        #region Public Properties

        public float CachedConstraintSize
        {
            get
            {
                return _cachedConstraintSize;
            }
        }

        public float CachedConstraintCellSize
        {
            get
            {
                return _cachedConstraintCellSize;
            }
        }

        public int CachedConstraintCount
        {
            get
            {
                return _cachedConstraintCount;
            }
        }

        public Vector2 CellSize
        {
            get
            {
                return m_cellSize;
            }
            set
            {
                if (m_cellSize == value)
                    return;
                m_cellSize = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public GridConstraintTypeEnum ConstraintType
        {
            get
            {
                return m_constraintType;
            }
            set
            {
                if (m_constraintType == value)
                    return;
                m_constraintType = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public ConstraintAlignEnum ConstraintAlign
        {
            get
            {
                return m_constraintAlign;
            }
            set
            {
                if (m_constraintAlign == value)
                    return;
                m_constraintAlign = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public int DefaultConstraintCount
        {
            get
            {
                return m_defaultConstraintCount;
            }
            set
            {
                if (m_defaultConstraintCount == value)
                    return;
                m_defaultConstraintCount = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public float ConstraintSpacing
        {
            get
            {
                return m_constraintSpacing;
            }
            set
            {
                if (m_constraintSpacing == value)
                    return;
                m_constraintSpacing = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (Mathf.Abs(_cachedConstraintSize - GetCurrentConstraintSize()) > SIZE_ERROR)
                SetCachedElementsLayoutDirty(true);
        }

        protected override void OnPotentialScrollValueChanged(Vector2 delta)
        {
            var contentSize = new Vector2(GetLocalWidth(Content), GetLocalHeight(Content));
            var currentDeltaDistance = new Vector2(Mathf.Abs(_lastScrollValue.x - delta.x) * contentSize.x, Mathf.Abs(_lastScrollValue.y - delta.y) * contentSize.y);
            if (currentDeltaDistance.x < m_scrollMinDeltaDistanceToCallEvent && currentDeltaDistance.y < m_scrollMinDeltaDistanceToCallEvent)
                return;

            _lastScrollValue = delta;

            if (OnScrollValueChanged != null)
                OnScrollValueChanged.Invoke(delta);

            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            if (m_elements == null)
                return;

            //Special case that horizontal and vertical scroll enabled
            var checkIndex = CalculateSafeCachedMinMax();
            if (_cachedMinMaxIndex.x != checkIndex.x || _cachedMinMaxIndex.y != checkIndex.y || IsConstraintScrollActive())
                FastReloadAll();
            else
            {
                return;
            }
        }

        #endregion

        #region Overriden Internal Helper Functions

        protected override void ReloadAll_Internal(bool fullRecalc)
        {
            if (m_elements == null)
                return;

            _cachedMinMaxIndex = CalculateSafeCachedMinMax();
            if (OnBeforeChangeVisibleElements != null)
                OnBeforeChangeVisibleElements.Invoke(_lastFrameVisibleElementIndexes);
            if (_lastFrameVisibleElementIndexes != _cachedMinMaxIndex)
            {
                if (_lastFrameVisibleElementIndexes.x < 0 || _lastFrameVisibleElementIndexes.y < 0)
                    _lastFrameVisibleElementIndexes = new Vector2Int(0, Mathf.Max(0, m_elements.Count - 1));
                //Unload not visible elements
                for (int i = _lastFrameVisibleElementIndexes.x; i <= _lastFrameVisibleElementIndexes.y; i++)
                {
                    if (i >= 0 && i < m_elements.Count && m_elements[i] != null && (i < _cachedMinMaxIndex.x || i > _cachedMinMaxIndex.y))
                    {
                        var element = m_elements[i];
                        Reload(element, i);
                    }
                }
            }
            //Reload current Elements
            for (int i = _cachedMinMaxIndex.x; i <= _cachedMinMaxIndex.y; i++)
            {
                var element = m_elements.Count > i && i >= 0 ? m_elements[i] : null;
                //in 'fullRecalc == false' we only reload elements that was not previous loaded in last roll
                if (i >= 0 && i < m_elements.Count && element != null &&
                    (fullRecalc || i < _lastFrameVisibleElementIndexes.x || i > _lastFrameVisibleElementIndexes.y ||
                    (element.transform.parent != Content && !IsOutOfConstraintBounds(i)))
                   )
                {
                    Reload(element, i);
                }
            }
            _lastFrameVisibleElementIndexes = _cachedMinMaxIndex;
            if (OnVisibleElementsChanged != null)
                OnVisibleElementsChanged.Invoke(_cachedMinMaxIndex);
        }

        protected override void RecalculateLayout()
        {
            if (Content != null)
            {
                UpdateContentPivotAndAnchor();
                if (m_autoPickElements && !Application.isPlaying)
                    ForcePickElements();
                FixLayoutInconsistencies();
                ApplyContentConstraintSize();
                bool isVertical = IsVertical();
                //Begin calculate Cache location
                float initialPadding = isVertical ? (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom ? m_padding.top : m_padding.bottom) : (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight ? m_padding.left : m_padding.right);
                float currentAccumulatedLocation = initialPadding;
                //Recalculate Cached Constraint Properties
                _cachedConstraintCount = GetCurrentConstraintCount();
                _cachedConstraintSize = GetCurrentConstraintSize();
                _cachedConstraintCellSize = GetCurrentConstraintCellSize();
                _cachedAlignOffset = GetAlignOffset();

                var invalidIndexCounter = 0;
                //Recalculate Layout Positions
                for (int i = 0; i < _scrollElementsCachedSize.Count; i++)
                {
                    //We must recalcula index of scroll element to use
                    var index = Mathf.Max(0, i - invalidIndexCounter);
                    float currentSize = 0;
                    float spacing = 0;

                    //We only calculate the element size when we change the line (mod(i) == 0)
                    var isFirstInConstraint = (index % _cachedConstraintCount) == 0;
                    _elementsLayoutPosition[i] = isFirstInConstraint ? currentAccumulatedLocation : _elementsLayoutPosition[i - 1];

                    //We only calculate the element size when we change the line (mod(i) == 0) and when element is active
                    var elementGameObject = index >= 0 && m_elements.Count > index ? m_elements[index] : null;
                    var validElement = elementGameObject == null || elementGameObject.activeSelf;
                    if (validElement)
                    {
                        if (isFirstInConstraint)
                        {
                            currentSize = isVertical ? m_cellSize.y : m_cellSize.x;
                            spacing = m_spacing;
                        }
                        currentAccumulatedLocation += (currentSize + spacing);
                    }
                    else
                        invalidIndexCounter++;
                }
                //Recalculate Total Content Size
                float contentSize = 0;
                if (isVertical)
                    contentSize = (m_padding.top + m_padding.bottom - initialPadding) + currentAccumulatedLocation;
                else
                    contentSize = (m_padding.left + m_padding.right - initialPadding) + currentAccumulatedLocation;
                SetContentSize(contentSize);
                ReloadAll();
                if (OnReloadCachedElementsLayout != null)
                    OnReloadCachedElementsLayout.Invoke();
            }
        }

        protected override void ApplyContentConstraintSize()
        {
            //Recalculate Total Content Size
            if (ScrollRect != null && m_constraintType == GridConstraintTypeEnum.Fixed)
            {
                //Special case when we can activate vertical and horizontal scroll
                if (IsVertical())
                {
                    var parentWidth = GetLocalWidth(Content.parent as RectTransform);
                    var newWidth = Mathf.Max(
                        m_padding.left + m_padding.right + ((m_cellSize.x + m_constraintSpacing) * m_defaultConstraintCount) - m_constraintSpacing,
                        parentWidth);
                    ScrollRect.horizontal = ScrollRect != null && Mathf.Abs(newWidth - parentWidth) > SIZE_ERROR;
                    //Apply size larger than content.parent size
                    if (ScrollRect.horizontal)
                        SetLocalWidth(Content, newWidth);
                    else
                        base.ApplyContentConstraintSize();

                }
                else
                {
                    var parentHeight = GetLocalHeight(Content.parent as RectTransform);
                    var newHeight = Mathf.Max(
                        m_padding.top + m_padding.bottom + ((m_cellSize.y + m_constraintSpacing) * m_defaultConstraintCount) - m_constraintSpacing,
                        parentHeight);
                    ScrollRect.vertical = ScrollRect != null && Mathf.Abs(newHeight - parentHeight) > SIZE_ERROR;
                    if (ScrollRect.vertical)
                        SetLocalHeight(Content, newHeight);
                    else
                        base.ApplyContentConstraintSize();
                }
            }
            else
                base.ApplyContentConstraintSize();
        }

        protected override void Reload(GameObject obj, int indexReload)
        {
            if (obj == null) return;

            Vector3 elementPosition = GetElementPosition(indexReload);
            //Unregister or Register Index
            if (Application.isPlaying)
            {
                //Check if index is out of bounds or position is out of constraint bounds
                var isOutOfBounds = indexReload < _cachedMinMaxIndex.x || indexReload > _cachedMinMaxIndex.y || IsOutOfConstraintBounds(elementPosition);
                if (isOutOfBounds)
                    UnregisterVisibleElement(indexReload);
                else
                    RegisterVisibleElement(indexReload);
                if (Application.isEditor)
                    obj.transform.name = "[" + indexReload + "] " + System.Text.RegularExpressions.Regex.Replace(obj.transform.name, @"^\[[0-9]+\] ", "");
            }

            //apply element local position
            obj.transform.localPosition = elementPosition;

            var fitter = obj.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;
            //Recalculate Item Size
            if (Content != null)
            {
                RectTransform rectTransform = obj.transform as RectTransform;
                float itemSize = GetCachedElementSize(indexReload);
                if (rectTransform != null)
                {
                    //var layout = rectTransform.GetComponent<LayoutElement>();
                    rectTransform.pivot = Content.pivot;
                    if (IsVertical())
                    {
                        itemSize = m_cellSize.y;
                        if (Mathf.Abs(GetLocalHeight(rectTransform) - itemSize) > SIZE_ERROR)
                            SetLocalHeight(rectTransform, itemSize);

                        //SetLayoutElementPreferredSize(layout, new Vector2(-1, itemSize));
                        //Apply Padding Horizontal
                        if (Mathf.Abs(GetLocalWidth(rectTransform) - _cachedConstraintCellSize) > SIZE_ERROR)
                            SetLocalWidth(rectTransform, _cachedConstraintCellSize);
                    }
                    else
                    {
                        itemSize = m_cellSize.x;
                        if (Mathf.Abs(GetLocalWidth(rectTransform) - itemSize) > SIZE_ERROR)
                            SetLocalWidth(rectTransform, itemSize);

                        //SetLayoutElementPreferredSize(layout, new Vector2(itemSize, -1));
                        //Apply Padding Vertical
                        if (Mathf.Abs(GetLocalHeight(rectTransform) - _cachedConstraintCellSize) > SIZE_ERROR)
                            SetLocalHeight(rectTransform, _cachedConstraintCellSize);
                    }
                }
            }
        }

        protected override int GetCurrentIndex()
        {
            //Try Find index based in Layout
            int index = _cachedMinMaxIndex.x < 0 ? 0 : _cachedMinMaxIndex.x;
            FixLayoutInconsistencies();

            if (Content != null)
            {
                float anchoredPosition = -1;
                if (IsVertical())
                {
                    if (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom)
                    {
                        anchoredPosition = Content.anchoredPosition.y;
                    }
                    else
                    {
                        anchoredPosition = -Content.anchoredPosition.y;
                    }
                }
                else
                {
                    if (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight)
                    {
                        anchoredPosition = -Content.anchoredPosition.x;

                    }
                    else
                    {
                        anchoredPosition = Content.anchoredPosition.x;
                    }
                }

                var elementSize = IsVertical() ? m_cellSize.y : m_cellSize.x;
                //Func<int, float> getElementSize = (i) => { return i >= 0 && i < _scrollElementsCachedSize.Count ? _scrollElementsCachedSize[i] : 0; };
                var delta = m_spacing < 0 ? -m_spacing : 0; // we must do it to prevent bug when spacing is negative
                //Find current index based in old cachedIndex (Optimized Search)
                if (index < _elementsLayoutPosition.Count)
                {
                    var initialLoopIndex = _cachedMinMaxIndex.x - (_cachedMinMaxIndex.x % _cachedConstraintCount);
                    if (anchoredPosition < (_elementsLayoutPosition[index] + delta))
                    {
                        for (int i = initialLoopIndex; i >= 0; i -= _cachedConstraintCount)
                        {
                            //Search first valid element
                            var elementPosition = (_elementsLayoutPosition[i] + delta);
                            if (anchoredPosition >= elementPosition)
                            {
                                break;
                            }
                            index -= _cachedConstraintCount;
                        }
                    }
                    else
                    {
                        for (int i = initialLoopIndex; i < _elementsLayoutPosition.Count; i += _cachedConstraintCount)
                        {
                            //Search first valid element
                            var elementPosition = (_elementsLayoutPosition[i] + delta);
                            if (anchoredPosition < elementPosition ||
                                anchoredPosition < (elementPosition + elementSize))
                            {
                                break;
                            }
                            index += _cachedConstraintCount;
                        }
                    }
                }
            }
            index = Mathf.Clamp(index, 0, m_elements.Count);
            return index;
        }

        protected override int GetLastIndex(int currentIndex)
        {
            if (currentIndex < 0)
                currentIndex = GetCurrentIndex();
            var lastIndex = currentIndex;
            var contentTransform = Viewport != null ? Viewport : (ScrollRect != null ? ScrollRect.transform : this.transform) as RectTransform;

            var contentMaxSize = IsVertical() ? GetLocalHeight(contentTransform) : GetLocalWidth(contentTransform);
            if (Content != null)
            {
                contentMaxSize += Mathf.Abs(IsVertical() ? Content.anchoredPosition.y : Content.anchoredPosition.x);
            }

            var initialLoopIndex = currentIndex;
            var elementSize = IsVertical() ? m_cellSize.y : m_cellSize.x;
            for (int i = Mathf.Max(0, initialLoopIndex); i < _elementsLayoutPosition.Count; i++)
            {
                //Search first invalid element
                if (_elementsLayoutPosition[i] > contentMaxSize &&
                    (_elementsLayoutPosition[i] + elementSize) > contentMaxSize)
                {
                    //Revert to previous valid element
                    lastIndex--;
                    break;
                }
                lastIndex++;
            }
            lastIndex = Mathf.Clamp(lastIndex, currentIndex, m_elements.Count - 1);
            return lastIndex;
        }

        protected override Vector3 GetElementPosition(int index)
        {
            FixLayoutInconsistencies();
            Vector3 position = Vector2.zero;
            var cachedLocation = _elementsLayoutPosition.Count > index && index >= 0 ? _elementsLayoutPosition[index] : 0;
            var isVertical = IsVertical();
            var deltaConstraintIndex = (index % _cachedConstraintCount);
            var extraConstraintSpacing = (_cachedConstraintCellSize + m_constraintSpacing) * deltaConstraintIndex;
            if (isVertical)
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom)
                {
                    position = new Vector3(position.x + m_padding.left + extraConstraintSpacing + _cachedAlignOffset, -cachedLocation, 0);
                }
                else
                {
                    position = new Vector3(position.x + m_padding.left + extraConstraintSpacing + _cachedAlignOffset, cachedLocation, 0);
                }
            }
            else
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight)
                {
                    position = new Vector3(cachedLocation, position.y + m_padding.bottom + extraConstraintSpacing + _cachedAlignOffset, 0);
                }
                else
                {
                    position = new Vector3(-cachedLocation, position.y + m_padding.bottom + extraConstraintSpacing + _cachedAlignOffset, 0);
                }
            }
            return position;
        }

        #endregion

        #region Internal Constraint Helper Functions

        protected virtual bool IsConstraintScrollActive()
        {
            return ScrollRect != null && ScrollRect.vertical && ScrollRect.horizontal;
        }

        protected virtual bool IsOutOfConstraintBounds(int index)
        {
            return IsOutOfConstraintBounds(GetElementPosition(index));
        }

        protected virtual bool IsOutOfConstraintBounds(Vector3 elementPosition)
        {
            if (IsConstraintScrollActive())
            {
                var isVertical = IsVertical();
                var contentTransform = Viewport != null ? Viewport : (ScrollRect != null ? ScrollRect.transform : this.transform) as RectTransform;
                var contentConstraintMaxPosition = isVertical ? GetLocalWidth(contentTransform) : GetLocalHeight(contentTransform);
                if (Content != null)
                    contentConstraintMaxPosition += Mathf.Abs(isVertical ? Content.anchoredPosition.x : Content.anchoredPosition.y);
                return isVertical ? contentConstraintMaxPosition < elementPosition.x : contentConstraintMaxPosition < elementPosition.y;
            }
            return false;
        }

        protected virtual int GetCurrentConstraintCount()
        {
            int currentConstraintCount = m_defaultConstraintCount;
            //In case of being flexible layout we must calculate the constraint
            if (m_constraintType == GridConstraintTypeEnum.Flexible)
            {
                currentConstraintCount = (int)(IsVertical() ?
                    ((m_cellSize.x + m_constraintSpacing) == 0 ? 0 : (GetLocalWidth(Content) - (m_padding.left + m_padding.right) + m_constraintSpacing) / (m_cellSize.x + m_constraintSpacing)) :
                    ((m_cellSize.y + m_constraintSpacing) == 0 ? 0 : (GetLocalHeight(Content) - (m_padding.top + m_padding.bottom) + m_constraintSpacing) / (m_cellSize.y + m_constraintSpacing)));
            }
            return Mathf.Max(1, currentConstraintCount);
        }

        protected virtual float GetCurrentConstraintCellSize()
        {
            var isVertical = IsVertical();
            if (m_constraintType == GridConstraintTypeEnum.FixedWithFlexibleCells)
            {
                var currentConstraintSize = GetCurrentConstraintSize();
                var currentConstraint = GetCurrentConstraintCount();
                return (currentConstraintSize - m_constraintSpacing * (currentConstraint - 1)) / currentConstraint;
            }
            else
            {
                return isVertical ? m_cellSize.x : m_cellSize.y;
            }
        }

        protected virtual float GetCurrentConstraintSize()
        {
            if (IsVertical())
                return GetLocalWidth(Content) - (m_padding.left + m_padding.right);
            return GetLocalHeight(Content) - (m_padding.top + m_padding.bottom);
        }

        protected virtual float GetAlignOffset()
        {
            var constraintSizeDelta = _cachedConstraintSize - ((_cachedConstraintCellSize + m_constraintSpacing) * _cachedConstraintCount - m_constraintSpacing); // Ex: ContainerSize.Width - CellSize*AmountOfCollumns
            var alignOffset = m_constraintAlign == ConstraintAlignEnum.Middle ? constraintSizeDelta / 2 : (m_constraintAlign == ConstraintAlignEnum.End ? constraintSizeDelta : 0);
            return alignOffset;
        }

        #endregion

        #region Layout Elements Function

        public override void CalculateLayoutInputHorizontal()
        {
            //TryRecalculateLayout();
            var isVertical = IsVertical();
            if (isVertical)
            {
                if (!IsFullRecalcRequired())
                    CalculateAnotherAxisPreferredSize(isVertical);
                else
                    _layoutSize = new Vector2(_cachedConstraintSize, _layoutSize.y);
            }
            else
                _layoutSize = new Vector2(GetLocalWidth(Content), _layoutSize.y);
        }

        public override void CalculateLayoutInputVertical()
        {
            //TryRecalculateLayout();
            var isVertical = IsVertical();
            if (!isVertical)
            {
                if (!IsFullRecalcRequired())
                    CalculateAnotherAxisPreferredSize(isVertical);
                else
                    _layoutSize = new Vector2(_layoutSize.x, _cachedConstraintSize);
            }
            else
                _layoutSize = new Vector2(_layoutSize.x, GetLocalHeight(Content));
        }

        protected virtual float CalculateAnotherAxisPreferredSize(bool isVertical)
        {
            _cachedConstraintCount = GetCurrentConstraintCount();
            _cachedConstraintSize = GetCurrentConstraintSize();
            _cachedConstraintCellSize = GetCurrentConstraintCellSize();

            var anotherAxis = isVertical ? 0 : 1;
            _layoutSize[anotherAxis] = _cachedConstraintSize;

            return _layoutSize[anotherAxis];
        }

        #endregion


    }
}
