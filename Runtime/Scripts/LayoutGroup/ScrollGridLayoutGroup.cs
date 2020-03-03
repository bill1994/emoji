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

        protected override void OnPotentialScrollValueChanged(Vector2 p_delta)
        {
            var v_contentSize = new Vector2(GetLocalWidth(Content), GetLocalHeight(Content));
            var v_currentDeltaDistance = new Vector2(Mathf.Abs(_lastScrollValue.x - p_delta.x) * v_contentSize.x, Mathf.Abs(_lastScrollValue.y - p_delta.y) * v_contentSize.y);
            if (v_currentDeltaDistance.x < m_scrollMinDeltaDistanceToCallEvent && v_currentDeltaDistance.y < m_scrollMinDeltaDistanceToCallEvent)
                return;

            _lastScrollValue = p_delta;

            if (OnScrollValueChanged != null)
                OnScrollValueChanged.Invoke(p_delta);

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

        protected override void ReloadAll_Internal(bool p_fullRecalc)
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
                        var v_element = m_elements[i];
                        Reload(v_element, i);
                    }
                }
            }
            //Reload current Elements
            for (int i = _cachedMinMaxIndex.x; i <= _cachedMinMaxIndex.y; i++)
            {
                var v_element = m_elements.Count > i && i >= 0 ? m_elements[i] : null;
                //in 'p_fullRecalc == false' we only reload elements that was not previous loaded in last roll
                if (i >= 0 && i < m_elements.Count && v_element != null &&
                    (p_fullRecalc || i < _lastFrameVisibleElementIndexes.x || i > _lastFrameVisibleElementIndexes.y ||
                    (v_element.transform.parent != Content && !IsOutOfConstraintBounds(i)))
                   )
                {
                    Reload(v_element, i);
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
                bool v_isVertical = IsVertical();
                //Begin calculate Cache location
                float initialPadding = v_isVertical ? (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom ? m_padding.top : m_padding.bottom) : (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight ? m_padding.left : m_padding.right);
                float v_currentAccumulatedLocation = initialPadding;
                //Recalculate Cached Constraint Properties
                _cachedConstraintCount = GetCurrentConstraintCount();
                _cachedConstraintSize = GetCurrentConstraintSize();
                _cachedConstraintCellSize = GetCurrentConstraintCellSize();
                _cachedAlignOffset = GetAlignOffset();

                var v_invalidIndexCounter = 0;
                //Recalculate Layout Positions
                for (int i = 0; i < _scrollElementsCachedSize.Count; i++)
                {
                    //We must recalcula index of scroll element to use
                    var v_index = Mathf.Max(0, i - v_invalidIndexCounter);
                    float v_currentSize = 0;
                    float v_spacing = 0;

                    //We only calculate the element size when we change the line (mod(i) == 0)
                    var v_isFirstInConstraint = (v_index % _cachedConstraintCount) == 0;
                    _elementsLayoutPosition[i] = v_isFirstInConstraint ? v_currentAccumulatedLocation : _elementsLayoutPosition[i - 1];

                    //We only calculate the element size when we change the line (mod(i) == 0) and when element is active
                    var v_elementGameObject = v_index >= 0 && m_elements.Count > v_index ? m_elements[v_index] : null;
                    var v_validElement = v_elementGameObject == null || v_elementGameObject.activeSelf;
                    if (v_validElement)
                    {
                        if (v_isFirstInConstraint)
                        {
                            v_currentSize = v_isVertical ? m_cellSize.y : m_cellSize.x;
                            v_spacing = m_spacing;
                        }
                        v_currentAccumulatedLocation += (v_currentSize + v_spacing);
                    }
                    else
                        v_invalidIndexCounter++;
                }
                //Recalculate Total Content Size
                float v_contentSize = 0;
                if (v_isVertical)
                    v_contentSize = (m_padding.top + m_padding.bottom - initialPadding) + v_currentAccumulatedLocation;
                else
                    v_contentSize = (m_padding.left + m_padding.right - initialPadding) + v_currentAccumulatedLocation;
                SetContentSize(v_contentSize);
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
                    var v_parentWidth = GetLocalWidth(Content.parent as RectTransform);
                    var v_newWidth = Mathf.Max(
                        m_padding.left + m_padding.right + ((m_cellSize.x + m_constraintSpacing) * m_defaultConstraintCount) - m_constraintSpacing,
                        v_parentWidth);
                    ScrollRect.horizontal = ScrollRect != null && Mathf.Abs(v_newWidth - v_parentWidth) > SIZE_ERROR;
                    //Apply size larger than content.parent size
                    if (ScrollRect.horizontal)
                        SetLocalWidth(Content, v_newWidth);
                    else
                        base.ApplyContentConstraintSize();

                }
                else
                {
                    var v_parentHeight = GetLocalHeight(Content.parent as RectTransform);
                    var v_newHeight = Mathf.Max(
                        m_padding.top + m_padding.bottom + ((m_cellSize.y + m_constraintSpacing) * m_defaultConstraintCount) - m_constraintSpacing,
                        v_parentHeight);
                    ScrollRect.vertical = ScrollRect != null && Mathf.Abs(v_newHeight - v_parentHeight) > SIZE_ERROR;
                    if (ScrollRect.vertical)
                        SetLocalHeight(Content, v_newHeight);
                    else
                        base.ApplyContentConstraintSize();
                }
            }
            else
                base.ApplyContentConstraintSize();
        }

        protected override void Reload(GameObject p_obj, int p_indexReload)
        {
            if (p_obj == null) return;

            Vector3 v_elementPosition = GetElementPosition(p_indexReload);
            //Unregister or Register Index
            if (Application.isPlaying)
            {
                //Check if index is out of bounds or position is out of constraint bounds
                var v_isOutOfBounds = p_indexReload < _cachedMinMaxIndex.x || p_indexReload > _cachedMinMaxIndex.y || IsOutOfConstraintBounds(v_elementPosition);
                if (v_isOutOfBounds)
                    UnregisterVisibleElement(p_indexReload);
                else
                    RegisterVisibleElement(p_indexReload);
                if (Application.isEditor)
                    p_obj.transform.name = "[" + p_indexReload + "] " + System.Text.RegularExpressions.Regex.Replace(p_obj.transform.name, @"^\[[0-9]+\] ", "");
            }

            //apply element local position
            p_obj.transform.localPosition = v_elementPosition;

            var v_fitter = p_obj.GetComponent<ContentSizeFitter>();
            if (v_fitter != null)
                v_fitter.enabled = false;
            //Recalculate Item Size
            if (Content != null)
            {
                RectTransform v_rectTransform = p_obj.transform as RectTransform;
                float v_itemSize = GetCachedElementSize(p_indexReload);
                if (v_rectTransform != null)
                {
                    //var v_layout = v_rectTransform.GetComponent<LayoutElement>();
                    v_rectTransform.pivot = Content.pivot;
                    if (IsVertical())
                    {
                        v_itemSize = m_cellSize.y;
                        if (Mathf.Abs(GetLocalHeight(v_rectTransform) - v_itemSize) > SIZE_ERROR)
                            SetLocalHeight(v_rectTransform, v_itemSize);

                        //SetLayoutElementPreferredSize(v_layout, new Vector2(-1, v_itemSize));
                        //Apply Padding Horizontal
                        if (Mathf.Abs(GetLocalWidth(v_rectTransform) - _cachedConstraintCellSize) > SIZE_ERROR)
                            SetLocalWidth(v_rectTransform, _cachedConstraintCellSize);
                    }
                    else
                    {
                        v_itemSize = m_cellSize.x;
                        if (Mathf.Abs(GetLocalWidth(v_rectTransform) - v_itemSize) > SIZE_ERROR)
                            SetLocalWidth(v_rectTransform, v_itemSize);

                        //SetLayoutElementPreferredSize(v_layout, new Vector2(v_itemSize, -1));
                        //Apply Padding Vertical
                        if (Mathf.Abs(GetLocalHeight(v_rectTransform) - _cachedConstraintCellSize) > SIZE_ERROR)
                            SetLocalHeight(v_rectTransform, _cachedConstraintCellSize);
                    }
                }
            }
        }

        protected override int GetCurrentIndex()
        {
            //Try Find index based in Layout
            int v_index = _cachedMinMaxIndex.x < 0 ? 0 : _cachedMinMaxIndex.x;
            FixLayoutInconsistencies();

            if (Content != null)
            {
                float v_anchoredPosition = -1;
                if (IsVertical())
                {
                    if (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom)
                    {
                        v_anchoredPosition = Content.anchoredPosition.y;
                    }
                    else
                    {
                        v_anchoredPosition = -Content.anchoredPosition.y;
                    }
                }
                else
                {
                    if (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight)
                    {
                        v_anchoredPosition = -Content.anchoredPosition.x;

                    }
                    else
                    {
                        v_anchoredPosition = Content.anchoredPosition.x;
                    }
                }

                var elementSize = IsVertical() ? m_cellSize.y : m_cellSize.x;
                //Func<int, float> getElementSize = (i) => { return i >= 0 && i < _scrollElementsCachedSize.Count ? _scrollElementsCachedSize[i] : 0; };
                var v_delta = m_spacing < 0 ? -m_spacing : 0; // we must do it to prevent bug when spacing is negative
                //Find current index based in old cachedIndex (Optimized Search)
                if (v_index < _elementsLayoutPosition.Count)
                {
                    var v_initialLoopIndex = _cachedMinMaxIndex.x - (_cachedMinMaxIndex.x % _cachedConstraintCount);
                    if (v_anchoredPosition < (_elementsLayoutPosition[v_index] + v_delta))
                    {
                        for (int i = v_initialLoopIndex; i >= 0; i -= _cachedConstraintCount)
                        {
                            //Search first valid element
                            var elementPosition = (_elementsLayoutPosition[i] + v_delta);
                            if (v_anchoredPosition >= elementPosition)
                            {
                                break;
                            }
                            v_index -= _cachedConstraintCount;
                        }
                    }
                    else
                    {
                        for (int i = v_initialLoopIndex; i < _elementsLayoutPosition.Count; i += _cachedConstraintCount)
                        {
                            //Search first valid element
                            var elementPosition = (_elementsLayoutPosition[i] + v_delta);
                            if (v_anchoredPosition < elementPosition ||
                                v_anchoredPosition < (elementPosition + elementSize))
                            {
                                break;
                            }
                            v_index += _cachedConstraintCount;
                        }
                    }
                }
            }
            v_index = Mathf.Clamp(v_index, 0, m_elements.Count);
            return v_index;
        }

        protected override int GetLastIndex(int currentIndex)
        {
            if (currentIndex < 0)
                currentIndex = GetCurrentIndex();
            var v_lastIndex = currentIndex;
            var v_contentTransform = Viewport != null ? Viewport : (ScrollRect != null ? ScrollRect.transform : this.transform) as RectTransform;

            var v_contentMaxSize = IsVertical() ? GetLocalHeight(v_contentTransform) : GetLocalWidth(v_contentTransform);
            if (Content != null)
            {
                v_contentMaxSize += Mathf.Abs(IsVertical() ? Content.anchoredPosition.y : Content.anchoredPosition.x);
            }

            var v_initialLoopIndex = currentIndex;
            var elementSize = IsVertical() ? m_cellSize.y : m_cellSize.x;
            for (int i = Mathf.Max(0, v_initialLoopIndex); i < _elementsLayoutPosition.Count; i++)
            {
                //Search first invalid element
                if (_elementsLayoutPosition[i] > v_contentMaxSize &&
                    (_elementsLayoutPosition[i] + elementSize) > v_contentMaxSize)
                {
                    //Revert to previous valid element
                    v_lastIndex--;
                    break;
                }
                v_lastIndex++;
            }
            v_lastIndex = Mathf.Clamp(v_lastIndex, currentIndex, m_elements.Count - 1);
            return v_lastIndex;
        }

        protected override Vector3 GetElementPosition(int p_index)
        {
            FixLayoutInconsistencies();
            Vector3 v_position = Vector2.zero;
            var v_cachedLocation = _elementsLayoutPosition.Count > p_index && p_index >= 0 ? _elementsLayoutPosition[p_index] : 0;
            var v_isVertical = IsVertical();
            var v_deltaConstraintIndex = (p_index % _cachedConstraintCount);
            var v_extraConstraintSpacing = (_cachedConstraintCellSize + m_constraintSpacing) * v_deltaConstraintIndex;
            if (v_isVertical)
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom)
                {
                    v_position = new Vector3(v_position.x + m_padding.left + v_extraConstraintSpacing + _cachedAlignOffset, -v_cachedLocation, 0);
                }
                else
                {
                    v_position = new Vector3(v_position.x + m_padding.left + v_extraConstraintSpacing + _cachedAlignOffset, v_cachedLocation, 0);
                }
            }
            else
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight)
                {
                    v_position = new Vector3(v_cachedLocation, v_position.y + m_padding.bottom + v_extraConstraintSpacing + _cachedAlignOffset, 0);
                }
                else
                {
                    v_position = new Vector3(-v_cachedLocation, v_position.y + m_padding.bottom + v_extraConstraintSpacing + _cachedAlignOffset, 0);
                }
            }
            return v_position;
        }

        #endregion

        #region Internal Constraint Helper Functions

        protected virtual bool IsConstraintScrollActive()
        {
            return ScrollRect != null && ScrollRect.vertical && ScrollRect.horizontal;
        }

        protected virtual bool IsOutOfConstraintBounds(int p_index)
        {
            return IsOutOfConstraintBounds(GetElementPosition(p_index));
        }

        protected virtual bool IsOutOfConstraintBounds(Vector3 p_elementPosition)
        {
            if (IsConstraintScrollActive())
            {
                var v_isVertical = IsVertical();
                var v_contentTransform = Viewport != null ? Viewport : (ScrollRect != null ? ScrollRect.transform : this.transform) as RectTransform;
                var v_contentConstraintMaxPosition = v_isVertical ? GetLocalWidth(v_contentTransform) : GetLocalHeight(v_contentTransform);
                if (Content != null)
                    v_contentConstraintMaxPosition += Mathf.Abs(v_isVertical ? Content.anchoredPosition.x : Content.anchoredPosition.y);
                return v_isVertical ? v_contentConstraintMaxPosition < p_elementPosition.x : v_contentConstraintMaxPosition < p_elementPosition.y;
            }
            return false;
        }

        protected virtual int GetCurrentConstraintCount()
        {
            int v_currentConstraintCount = m_defaultConstraintCount;
            //In case of being flexible layout we must calculate the constraint
            if (m_constraintType == GridConstraintTypeEnum.Flexible)
            {
                v_currentConstraintCount = (int)(IsVertical() ?
                    ((m_cellSize.x + m_constraintSpacing) == 0 ? 0 : (GetLocalWidth(Content) - (m_padding.left + m_padding.right) + m_constraintSpacing) / (m_cellSize.x + m_constraintSpacing)) :
                    ((m_cellSize.y + m_constraintSpacing) == 0 ? 0 : (GetLocalHeight(Content) - (m_padding.top + m_padding.bottom) + m_constraintSpacing) / (m_cellSize.y + m_constraintSpacing)));
            }
            return Mathf.Max(1, v_currentConstraintCount);
        }

        protected virtual float GetCurrentConstraintCellSize()
        {
            var v_isVertical = IsVertical();
            if (m_constraintType == GridConstraintTypeEnum.FixedWithFlexibleCells)
            {
                var v_currentConstraintSize = GetCurrentConstraintSize();
                var v_currentConstraint = GetCurrentConstraintCount();
                return (v_currentConstraintSize - m_constraintSpacing * (v_currentConstraint - 1)) / v_currentConstraint;
            }
            else
            {
                return v_isVertical ? m_cellSize.x : m_cellSize.y;
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
            var v_constraintSizeDelta = _cachedConstraintSize - ((_cachedConstraintCellSize + m_constraintSpacing) * _cachedConstraintCount - m_constraintSpacing); // Ex: ContainerSize.Width - CellSize*AmountOfCollumns
            var v_alignOffset = m_constraintAlign == ConstraintAlignEnum.Middle ? v_constraintSizeDelta / 2 : (m_constraintAlign == ConstraintAlignEnum.End ? v_constraintSizeDelta : 0);
            return v_alignOffset;
        }

        #endregion

        #region Layout Elements Function

        public override void CalculateLayoutInputHorizontal()
        {
            TryRecalculateLayout();
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
