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
                bool isVertical = IsVertical();
                //Begin calculate Cache location
                float initialPadding = isVertical ? (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom ? m_padding.top : m_padding.bottom) : (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight ? m_padding.left : m_padding.right);
                float currentAccumulatedLocation = initialPadding;
                //Recalculate Layout Positions
                for (int i = 0; i < _scrollElementsCachedSize.Count; i++)
                {
                    float currentSize = 0;

                    //Check if element is valid
                    var elementGameObject = i >= 0 && m_elements.Count > i ? m_elements[i] : null;
                    var validElement = elementGameObject == null || elementGameObject.activeSelf;
                    if (validElement)
                    {
                        currentSize = _scrollElementsCachedSize[i] >= 0 ? _scrollElementsCachedSize[i] : GetElementSize(i);
                    }

                    _elementsLayoutPosition[i] = currentAccumulatedLocation;
                    currentAccumulatedLocation += (currentSize + (i != _scrollElementsCachedSize.Count - 1 && validElement ? m_spacing : 0));
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

        protected override void Reload(GameObject obj, int indexReload)
        {
            if (obj == null) return;

            //Unregister or Register Index
            if (Application.isPlaying)
            {
                if (indexReload < _cachedMinMaxIndex.x || indexReload > _cachedMinMaxIndex.y)
                    UnregisterVisibleElement(indexReload);
                else
                    RegisterVisibleElement(indexReload);
                if (Application.isEditor)
                    obj.transform.name = "[" + indexReload + "] " + System.Text.RegularExpressions.Regex.Replace(obj.transform.name, @"^\[[0-9]+\] ", "");
            }

            Vector3 elementPosition = GetElementPosition(indexReload);
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
                        _layoutSize.x = Mathf.Max(_layoutSize.x, LayoutUtilityEx.GetPreferredWidth(rectTransform));
                        if (Mathf.Abs(GetLocalHeight(rectTransform) - itemSize) > SIZE_ERROR)
                            SetLocalHeight(rectTransform, itemSize);

                        //SetLayoutElementPreferredSize(layout, new Vector2(-1, itemSize));

                        //Apply Padding Horizontal
                        var width = GetLocalWidth(Content) - (m_padding.left + m_padding.right);
                        if (Mathf.Abs(GetLocalWidth(rectTransform) - width) > SIZE_ERROR)
                            SetLocalWidth(rectTransform, width);
                    }
                    else
                    {
                        _layoutSize.y = Mathf.Max(_layoutSize.y, LayoutUtilityEx.GetPreferredHeight(rectTransform));
                        if (Mathf.Abs(GetLocalWidth(rectTransform) - itemSize) > SIZE_ERROR)
                            SetLocalWidth(rectTransform, itemSize);

                        //SetLayoutElementPreferredSize(layout, new Vector2(itemSize, -1));

                        //Apply Padding Vertical
                        var height = GetLocalHeight(Content) - (m_padding.top + m_padding.bottom);
                        if (Mathf.Abs(GetLocalHeight(rectTransform) - height) > SIZE_ERROR)
                            SetLocalHeight(rectTransform, height);
                    }
                }
            }

            if (fitter != null)
                fitter.enabled = true;
        }

        protected override Vector3 GetElementPosition(int index)
        {
            FixLayoutInconsistencies();
            Vector3 position = Vector2.zero;
            var cachedLocation = _elementsLayoutPosition.Count > index && index >= 0 ? _elementsLayoutPosition[index] : 0;
            var isVertical = IsVertical();
            if (isVertical)
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.TopToBottom)
                {
                    position = new Vector3(position.x + m_padding.left, -cachedLocation, 0);
                }
                else
                {
                    position = new Vector3(position.x + m_padding.left, cachedLocation, 0);
                }
            }
            else
            {
                if (m_scrollAxis == ScrollDirectionTypeEnum.LeftToRight)
                {
                    position = new Vector3(cachedLocation, position.y + m_padding.bottom, 0);
                }
                else
                {
                    position = new Vector3(-cachedLocation, position.y + m_padding.bottom, 0);
                }
            }
            return position;
        }

        #endregion

        #region Layout Elements Function

        public override void CalculateLayoutInputHorizontal()
        {
            //TryRecalculateLayout();
            var isVertical = IsVertical();
            if (isVertical && !IsFullRecalcRequired())
                CalculateAnotherAxisPreferredSize(isVertical);
            else
                _layoutSize = new Vector2(GetLocalWidth(Content), _layoutSize.y);
        }

        public override void CalculateLayoutInputVertical()
        {
            //TryRecalculateLayout();
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
