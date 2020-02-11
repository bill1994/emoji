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
    public class ScrollLinearLayoutGroup : ScrollLayoutGroup
    {
        #region Overriden Internal Helper Functions

        protected override void RecalculateLayout()
        {
            if (IsFullRecalcRequired())
            {
                var anotherAxis = IsVertical() ? 0 : 1;
                _layoutSize[anotherAxis] = -1;
            }

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
                //Recalculate Layout Positions
                for (int i = 0; i < _scrollElementsCachedSize.Count; i++)
                {
                    float v_currentSize = 0;

                    //Check if element is valid
                    var v_elementGameObject = i >= 0 && m_elements.Count > i ? m_elements[i] : null;
                    var v_validElement = v_elementGameObject == null || v_elementGameObject.activeSelf;
                    if (v_validElement)
                    {
                        v_currentSize = _scrollElementsCachedSize[i] >= 0 ? _scrollElementsCachedSize[i] : GetElementSize(i);
                    }

                    _elementsLayoutPosition[i] = v_currentAccumulatedLocation;
                    v_currentAccumulatedLocation += (v_currentSize + (i != _scrollElementsCachedSize.Count - 1 && v_validElement ? m_spacing : 0));
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

        protected override void Reload(GameObject p_obj, int p_indexReload)
        {
            if (p_obj == null) return;

            //Unregister or Register Index
            if (Application.isPlaying)
            {
                if (p_indexReload < _cachedMinMaxIndex.x || p_indexReload > _cachedMinMaxIndex.y)
                    UnregisterVisibleElement(p_indexReload);
                else
                    RegisterVisibleElement(p_indexReload);
                if(Application.isEditor)
                    p_obj.transform.name = "[" + p_indexReload + "] " + System.Text.RegularExpressions.Regex.Replace(p_obj.transform.name, @"^\[[0-9]+\] ", "");
            }

            Vector3 v_elementPosition = GetElementPosition(p_indexReload);
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
                    var v_layout = v_rectTransform.GetComponent<LayoutElement>();
                    v_rectTransform.pivot = Content.pivot;
                    if (IsVertical())
                    {
                        _layoutSize.x = Mathf.Max(_layoutSize.x, LayoutUtilityEx.GetPreferredWidth(v_rectTransform));
                        if (Mathf.Abs(GetLocalHeight(v_rectTransform) - v_itemSize) > SIZE_ERROR)
                            SetLocalHeight(v_rectTransform, v_itemSize);

                        //SetLayoutElementPreferredSize(v_layout, new Vector2(-1, v_itemSize));

                        //Apply Padding Horizontal
                        var v_width = GetLocalWidth(Content) - (m_padding.left + m_padding.right);
                        if (Mathf.Abs(GetLocalWidth(v_rectTransform) - v_width) > SIZE_ERROR)
                            SetLocalWidth(v_rectTransform, v_width);
                    }
                    else
                    {
                        _layoutSize.y = Mathf.Max(_layoutSize.y, LayoutUtilityEx.GetPreferredHeight(v_rectTransform));
                        if (Mathf.Abs(GetLocalWidth(v_rectTransform) - v_itemSize) > SIZE_ERROR)
                            SetLocalWidth(v_rectTransform, v_itemSize);

                        //SetLayoutElementPreferredSize(v_layout, new Vector2(v_itemSize, -1));

                        //Apply Padding Vertical
                        var v_height = GetLocalHeight(Content) - (m_padding.top + m_padding.bottom);
                        if (Mathf.Abs(GetLocalHeight(v_rectTransform) - v_height) > SIZE_ERROR)
                            SetLocalHeight(v_rectTransform, v_height);
                    }
                }
            }

            if (v_fitter != null)
                v_fitter.enabled = true;
        }

        protected override Vector3 GetElementPosition(int p_index)
        {
            FixLayoutInconsistencies();
            Vector3 v_position = Vector2.zero;
            var v_cachedLocation = _elementsLayoutPosition.Count > p_index && p_index >= 0 ? _elementsLayoutPosition[p_index] : 0;
            var v_isVertical = IsVertical();
            if (v_isVertical)
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom)
                {
                    v_position = new Vector3(v_position.x + m_padding.left, -v_cachedLocation, 0);
                }
                else
                {
                    v_position = new Vector3(v_position.x + m_padding.left, v_cachedLocation, 0);
                }
            }
            else
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight)
                {
                    v_position = new Vector3(v_cachedLocation, v_position.y + m_padding.bottom, 0);
                }
                else
                {
                    v_position = new Vector3(-v_cachedLocation, v_position.y + m_padding.bottom, 0);
                }
            }
            return v_position;
        }

        #endregion

        #region Layout Elements Function

        public override void CalculateLayoutInputHorizontal()
        {
            TryRecalculateLayout();
            var isVertical = IsVertical();
            if(isVertical && !IsFullRecalcRequired())
                CalculateAnotherAxisPreferredSize(isVertical);
            else
                _layoutSize = new Vector2(GetLocalWidth(Content), _layoutSize.y);
        }

        public override void CalculateLayoutInputVertical()
        {
            TryRecalculateLayout();
            var isVertical = IsVertical();
            if (!isVertical && !IsFullRecalcRequired())
                CalculateAnotherAxisPreferredSize(isVertical);
            else
                _layoutSize = new Vector2(_layoutSize.x, GetLocalHeight(Content));
        }

        protected virtual float CalculateAnotherAxisPreferredSize(bool isVertical)
        {
            var anotherAxis = isVertical ? 0 : 1;
            List<GameObject> visibleElements = new List<GameObject>();
            for (int i = _cachedMinMaxIndex.x; i <= _cachedMinMaxIndex.y; i++)
            {
                var elementRectTransform = i >= 0 && m_elements.Count > i && m_elements[i] != null ? m_elements[i].transform as RectTransform : null;
                if (elementRectTransform != null)
                    _layoutSize[anotherAxis] = Mathf.Max(_layoutSize[anotherAxis], LayoutUtilityEx.GetPreferredSize(elementRectTransform, anotherAxis));
            }
            return _layoutSize[anotherAxis];
        }

        #endregion
    }
}
