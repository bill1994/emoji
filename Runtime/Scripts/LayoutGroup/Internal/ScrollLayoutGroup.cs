using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public abstract class ScrollLayoutGroup : UIBehaviour, IEnumerable<GameObject>, ILayoutElement, ILayoutController
    {
        #region Helper Classes

        [System.Serializable]
        public class ScrollUnityEvent : UnityEvent<Vector2> { }
        [System.Serializable]
        public class IndexRangeUnityEvent : UnityEvent<Vector2Int> { }
        [System.Serializable]
        public class VisibleElementUnityEvent : UnityEvent<GameObject, int> { }

        [System.Serializable]
        public class IntUnityEvent : UnityEvent<int> { }
        [System.Serializable]
        public class IntArrayUnityEvent : UnityEvent<int[]> { }

        public enum ScrollDirectionTypeEnum
        {
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft
        }

        public enum MinContentSizeAlign
        {
            SameAsScrollDirection,
            Middle,
            InverseScrollDirection,
        }

        [System.Serializable]
        public struct ExtraElementsAmount
        {
            [SerializeField]
            private int m_before;
            [SerializeField]
            private int m_after;

            public int Before
            {
                get
                {
                    return m_before;
                }
                set
                {
                    if (m_before == value)
                        return;
                    m_before = value;
                }
            }

            public int After
            {
                get
                {
                    return m_after;
                }
                set
                {
                    if (m_after == value)
                        return;
                    m_after = value;
                }
            }
        }

        #endregion

        #region Private Variables

        [Header("Elements Fields")]
        [SerializeField]
        protected bool m_disableNonVisibleElements = true;
        [SerializeField]
        protected bool m_autoPickElements = true;
        [Space]
        [SerializeField, Tooltip("Minimum amount of visible elements to display. Set -1 to always show all elements")]
        protected int m_minVisibleElements = 0;
        [SerializeField, Tooltip("Amount of elements to awake when scroll reach the end of current visible area")]
        protected int m_elementsToAwakePerStep = 1;
        [SerializeField, Tooltip("Define extra visible elements range. This is useful to set elements After of Before screen range")]
        ExtraElementsAmount m_extraVisibleElementsCount = new ExtraElementsAmount();
        [SerializeField, Tooltip("In deep hierarchys SetParent contains a huge impact in performance. This property will try prevent recalculate amount of visible element, avoiding SetParent and SetSiblingIndex")]
        protected bool m_optimizeDeepHierarchy = true;
        [Space]
        [SerializeField]
        protected List<GameObject> m_elements = new List<GameObject>();

        [Header("Layout Fields")]
        [Space]
        [SerializeField]
        protected int m_startingSibling = 0;
        [Space]
        [SerializeField]
        protected RectOffset m_padding = new RectOffset();
        [SerializeField]
        protected int m_spacing = 0;
        [SerializeField]
        protected ScrollDirectionTypeEnum m_scrollAxis = ScrollDirectionTypeEnum.TopToBottom;// m_scrollAxis scrollview  
        [SerializeField]
        protected ScrollRect m_scrollRect = null;
        [Space]
        [SerializeField]
        protected int m_minContentSize = -1;
        [SerializeField]
        protected bool m_autoMinContentSize = false;
        [SerializeField]
        protected MinContentSizeAlign m_minContentSizeAlign = MinContentSizeAlign.SameAsScrollDirection;

        [Header("Scroll Optimizer Fields")]
        [SerializeField, Range(0, 20), Tooltip("Delta distance Value that will be used while scrolling to call ScrollEvent (in local content space)")]
        protected float m_scrollMinDeltaDistanceToCallEvent = 5.0f;

        protected bool _layoutDirty = true;
        [SerializeField, HideInInspector]
        protected Transform _invisibleElementsContent = null;

        //Cached value of each element of the scrollrect (we will use this in all maths because m_elements can contain nulls so we will use this cache value to build the layout)
        [System.NonSerialized] protected List<float> _scrollElementsCachedSize = new List<float>();
        //Used to cache the location of each item (based in previous items)
        [System.NonSerialized] protected List<float> _elementsLayoutPosition = new List<float>(); //The first position is the true position in Axis Direction (IsVertical? Y position : X Position) and the Y is the percent in Content Width or Height used

        //Visibility Caches
        protected Vector2Int _lastFrameVisibleElementIndexes = new Vector2Int(-1, -1);
        protected Vector2Int _cachedMinMaxIndex = new Vector2Int(-1, -1);

        #endregion

        #region Callbacks

        [Header("Scroll Callcabks")]
        public ScrollUnityEvent OnScrollValueChanged = new ScrollUnityEvent();

        [Header("Visibility Callbacks")]
        public IndexRangeUnityEvent OnVisibleElementsChanged = new IndexRangeUnityEvent();
        public IndexRangeUnityEvent OnBeforeChangeVisibleElements = new IndexRangeUnityEvent();
        public VisibleElementUnityEvent OnElementBecameInvisible = new VisibleElementUnityEvent();
        public VisibleElementUnityEvent OnElementBecameVisible = new VisibleElementUnityEvent();

        [Header("Layout Callbacks")]
        public UnityEvent OnReloadCachedElementsLayout = new UnityEvent();
        public IntUnityEvent OnElementCachedSizeChanged = new IntUnityEvent();

        [Header("Add/Remove Callbacks")]
        public UnityEvent OnAllElementsReplaced = new UnityEvent();
        public IntArrayUnityEvent OnElementsAdded = new IntArrayUnityEvent();
        public IntArrayUnityEvent OnElementsRemoved = new IntArrayUnityEvent();
        public IntUnityEvent OnElementChanged = new IntUnityEvent();

        #endregion

        #region Public Properties

        public bool OptimizeDeepHierarchy
        {
            get
            {
                return m_optimizeDeepHierarchy;
            }
            set
            {
                if (m_optimizeDeepHierarchy == value)
                    return;
                m_optimizeDeepHierarchy = value;
            }
        }

        public RectOffset Padding
        {
            get
            {
                return m_padding;
            }
            set
            {
                if (m_padding == value)
                    return;
                m_padding = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public int Spacing
        {
            get
            {
                return m_spacing;
            }
            set
            {
                if (m_spacing == value)
                    return;
                m_spacing = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public float ScrollMinDeltaDistanceToCallEvent
        {
            get
            {
                return m_scrollMinDeltaDistanceToCallEvent;
            }

            set
            {
                if (m_scrollMinDeltaDistanceToCallEvent == value)
                    return;
                m_scrollMinDeltaDistanceToCallEvent = value;
            }
        }

        public RectTransform Content
        {
            get
            {
                return ScrollRect != null ? m_scrollRect.content : this.transform as RectTransform;
            }
        }

        public RectTransform Viewport
        {
            get
            {
                return ScrollRect != null ? m_scrollRect.viewport : null;
            }
        }

        public ScrollRect ScrollRect
        {
            get
            {
                if (m_scrollRect == null)
                    m_scrollRect = GetComponentInParent<ScrollRect>(this, true);
                return m_scrollRect;
            }
        }

        public bool DisableNonVisibleElements
        {
            get
            {
                return m_disableNonVisibleElements;
            }
            set
            {
                if (m_disableNonVisibleElements == value)
                    return;
                m_disableNonVisibleElements = value;
                TrySetupInvisibleContent();
            }
        }

        public GameObject this[int i]
        {
            get
            {
                return m_elements[i];
            }
            set
            {
                if (m_elements[i] == value)
                    return;
                if (m_elements[i] != null)
                    _objectsToSendToInvisibleContentParent.Remove(m_elements[i]);
                m_elements[i] = value;
                if (OnElementChanged != null)
                    OnElementChanged.Invoke(i);
            }
        }

        public ScrollDirectionTypeEnum ScrollAxis
        {
            get
            {
                return m_scrollAxis;
            }
            set
            {
                if (m_scrollAxis == value)
                    return;
                m_scrollAxis = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public Vector2Int VisibleElementsIndexRange
        {
            get
            {
                if (_cachedMinMaxIndex.x >= m_elements.Count || _cachedMinMaxIndex.y >= m_elements.Count)
                    _cachedMinMaxIndex = new Vector2Int(-1, -1);
                return _cachedMinMaxIndex;
            }
        }

        public int MinVisibleElements
        {
            get
            {
                return m_minVisibleElements;
            }
            set
            {
                if (m_minVisibleElements == value)
                    return;
                m_minVisibleElements = value;
                SetCachedElementsLayoutDirty(true);
            }
        }

        public int ElementsToAwakePerStep
        {
            get
            {
                return m_elementsToAwakePerStep;
            }
            set
            {
                if (m_elementsToAwakePerStep == value)
                    return;
                m_elementsToAwakePerStep = value;
            }
        }


        public ExtraElementsAmount ExtraVisibleElementsCount
        {
            get
            {
                return m_extraVisibleElementsCount;
            }
            set
            {
                if (m_extraVisibleElementsCount.Before == value.Before &&
                    m_extraVisibleElementsCount.After == value.After)
                    return;
                m_extraVisibleElementsCount = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            ForceInitialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterScrollRectEvents();
            RevertInvisibleElementsToMainContent();
            if (!Application.isPlaying)
                Init();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterScrollRectEvents();
            if (!IsInvoking("RevertInvisibleElementsToMainContent"))
                Invoke("RevertInvisibleElementsToMainContent", 0);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_invisibleElementsContent != null)
                GameObject.DestroyImmediate(_invisibleElementsContent.gameObject);
        }

        protected virtual void Update()
        {
            TrySendObjectsToInvisibleContentParent();
            //As we are not an LayoutGroup we must check for layout recalcs in Update
            TryRecalculateLayout();
        }

        protected Vector2 _lastScrollValue = new Vector2(-1, -1);
        protected virtual void OnPotentialScrollValueChanged(Vector2 delta)
        {
            var contentSize = new Vector2(GetLocalWidth(Content), GetLocalHeight(Content));
            var currentDeltaDistance = new Vector2(Mathf.Abs(_lastScrollValue.x - delta.x) * contentSize.x, Mathf.Abs(_lastScrollValue.y - delta.y) * contentSize.y);
            if (currentDeltaDistance.x < m_scrollMinDeltaDistanceToCallEvent && currentDeltaDistance.y < m_scrollMinDeltaDistanceToCallEvent)
                return;

            _lastScrollValue = delta;

            if (OnScrollValueChanged != null)
                OnScrollValueChanged.Invoke(delta);

            if (m_elements == null)
                return;

            var checkIndex = CalculateSafeCachedMinMax();
            if (_cachedMinMaxIndex.x != checkIndex.x || _cachedMinMaxIndex.y != checkIndex.y)
                FastReloadAll();
            else
            {
                return;
            }
        }

        //Used in editor to autopick elements on every change in children transform
        protected virtual void OnTransformChildrenChanged()
        {
            if (m_autoPickElements && !Application.isPlaying)
            {
                if (!IsInvoking("RecalculateLayout"))
                    Invoke("RecalculateLayout", 0);
            }
        }

        Vector2 _oldDimension = new Vector2(-1, -1);
        protected override void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying)
            {
                base.OnRectTransformDimensionsChange();
                var newDimension = new Vector2(GetLocalWidth(this.transform as RectTransform), GetLocalHeight(this.transform as RectTransform));
                if ((newDimension.x == 0 || Mathf.Abs(_oldDimension.x - newDimension.x) > Mathf.Epsilon) ||
                    (newDimension.y == 0 || Mathf.Abs(_oldDimension.y - newDimension.y) > Mathf.Epsilon))
                {
                    _oldDimension = newDimension;
                    SetCachedElementsLayoutDirty();
                }
            }
            else
                SetCachedElementsLayoutDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetCachedElementsLayoutDirty(true);
        }

#endif

        #endregion

        #region Elements Change Function

        public int ElementsCount
        {
            get
            {
                return m_elements.Count;
            }
        }

        public GameObject[] GetElements()
        {
            return m_elements.ToArray();
        }

        public void ClearElements()
        {
            _objectsToSendToInvisibleContentParent.Clear();
            m_elements.Clear();
            ReplaceElements(m_elements);
        }

        public void ReplaceElements(IList<GameObject> elements)
        {
            _objectsToSendToInvisibleContentParent.Clear();
            m_elements = elements != null ? new List<GameObject>(elements) : new List<GameObject>();
            _objectsToSendToInvisibleContentParent.Clear();
            SetCachedElementsLayoutDirty(true);
            if (OnAllElementsReplaced != null)
                OnAllElementsReplaced.Invoke();
        }

        public bool AddRangeElements(IList<GameObject> elements)
        {
            return InsertRangeElements(ElementsCount, elements);
        }

        public bool InsertRangeElements(int index, IList<GameObject> elements)
        {
            if (elements != null && elements.Count > 0)
            {
                index = Mathf.Clamp(index, 0, m_elements.Count - 1);
                var currentIndex = index;
                List<int> addIndexes = new List<int>();
                foreach (var element in elements)
                {
                    m_elements.Insert(currentIndex, element);
                    addIndexes.Add(currentIndex);
                    currentIndex++;
                }
                SetCachedElementsLayoutDirty(true);
                if (OnElementsAdded != null)
                    OnElementsAdded.Invoke(addIndexes.ToArray());
                return true;
            }
            return false;
        }

        public void AddElement(GameObject element)
        {
            InsertElement(ElementsCount, element);
        }

        public int InsertElement(int index, GameObject element)
        {
            index = Mathf.Clamp(index, 0, m_elements.Count - 1);
            m_elements.Insert(index, element);
            SetCachedElementsLayoutDirty(true);
            if (OnElementsAdded != null)
                OnElementsAdded.Invoke(new int[] { index });
            return index;
        }

        public bool RemoveElementAt(int index)
        {
            if (index >= 0 && index < m_elements.Count)
            {
                if (m_elements[index] != null)
                    _objectsToSendToInvisibleContentParent.Remove(m_elements[index]);
                m_elements.RemoveAt(index);
                SetCachedElementsLayoutDirty(true);
                if (OnElementsRemoved != null)
                    OnElementsRemoved.Invoke(new int[] { index });
                return true;
            }
            return false;
        }

        public bool RemoveElement(GameObject element)
        {
            var index = FindElementIndex(element);
            return RemoveElementAt(index);
        }

        public int FindElementIndex(GameObject element)
        {
            return m_elements.IndexOf(element);
        }

        #endregion

        #region Public Helper Functions

        bool _needInit = true;
        public bool ForceInitialize()
        {
            if (_needInit)
            {
                _needInit = false;
                Init();
                return true;
            }
            return false;
        }

        public GameObject[] GetVisibleElements()
        {
            List<GameObject> visibleElements = new List<GameObject>();
            for (int i = _cachedMinMaxIndex.x; i <= _cachedMinMaxIndex.y; i++)
            {
                if (i >= 0 && m_elements.Count > i && m_elements[i] != null)
                    visibleElements.Add(m_elements[i]);
            }
            return visibleElements.ToArray();
        }

        public bool IsVertical()
        {
            return ScrollAxis == ScrollDirectionTypeEnum.TopToBottom || ScrollAxis == ScrollDirectionTypeEnum.BottomToTop;
        }

        public virtual void FullReloadAll()
        {
            _lastFrameVisibleElementIndexes = new Vector2Int(-1, -1);
            ReloadAll_Internal(true);
        }

        public virtual void FastReloadAll()
        {
            ReloadAll_Internal(false);
        }

        public virtual void ReloadAll()
        {
            ReloadAll_Internal(true);
        }

        #endregion

        #region Public Layout Functions

        public virtual void SetCachedElementSize(int index, float itemSize)
        {
            FixLayoutInconsistencies();
            if (_scrollElementsCachedSize.Count > index && index >= 0)
            {
                var elementItemSize = _scrollElementsCachedSize[index];
                if (elementItemSize != itemSize)
                {
                    elementItemSize = itemSize;
                    _scrollElementsCachedSize[index] = itemSize;
                    SetCachedElementsLayoutDirty();
                    if (OnElementCachedSizeChanged != null)
                        OnElementCachedSizeChanged.Invoke(index);
                }
            }
        }

        public float GetCachedElementPosition(int index)
        {
            return _elementsLayoutPosition.Count > index && index >= 0 ? _elementsLayoutPosition[index] : 0;
        }

        public float GetCachedElementSize(int index)
        {
            FixLayoutInconsistencies();
            float itemSize = -1;
            if (_scrollElementsCachedSize.Count > index && index >= 0)
            {
                itemSize = _scrollElementsCachedSize[index];
                _scrollElementsCachedSize[index] = itemSize;
            }
            var elementTransform = index >= 0 && m_elements.Count > index && m_elements[index] != null ? m_elements[index].transform as RectTransform : null;
            var elementSize = elementTransform != null ? (IsVertical() ? GetLocalHeight(elementTransform) : GetLocalWidth(elementTransform)) : 0;
            return itemSize >= 0 ? itemSize : elementSize;
        }

        public virtual void SetCachedElementsLayoutDirty(bool performFullRecalc = false)
        {
            if (performFullRecalc)
            {
                _lastFrameVisibleElementIndexes = new Vector2Int(-1, -1);
                _cachedMinMaxIndex = new Vector2Int(-1, -1);
            }
            if (!_layoutDirty)
            {
                _layoutDirty = true;
                /*if (!CanvasUpdateRegistry.IsRebuildingLayout())
                {
                    MarkLayoutForRebuild();
                }
                else if (!IsInvoking("MarkLayoutForRebuild"))
                    Invoke("MarkLayoutForRebuild", 0);*/
            }

        }

        /*protected void MarkLayoutForRebuild()
        {
            if (this != null)
            {
                CancelInvoke("MarkLayoutForRebuild");
                LayoutRebuilder.MarkLayoutForRebuild(this.transform as RectTransform);
            }
        }*/

        public virtual void TryRecalculateLayout(bool force = false)
        {
            if (_elementsLayoutPosition.Count != _scrollElementsCachedSize.Count || _scrollElementsCachedSize.Count != m_elements.Count)
                _layoutDirty = true;
            if (_layoutDirty)
            {
                _layoutDirty = false;
                if (_cachedMinMaxIndex.x >= m_elements.Count || _cachedMinMaxIndex.y >= m_elements.Count)
                    _cachedMinMaxIndex = new Vector2Int(-1, -1);
                RecalculateLayout();

                var contentSize = GetContentSize();
                if (ScrollRect != null &&
                    ((IsVertical() && Mathf.Abs(Content.anchoredPosition.y) > contentSize) ||
                    (!IsVertical() && Mathf.Abs(Content.anchoredPosition.x) > contentSize))
                   )
                {
                    ScrollRect.Rebuild(CanvasUpdate.PostLayout);
                    SetCachedElementsLayoutDirty();
                }
                else
                {
                    //CancelInvoke("MarkLayoutForRebuild");
                    _layoutDirty = false;
                }
            }
        }

        #endregion

        #region Internal Layout Functions

        protected virtual bool IsFullRecalcRequired()
        {
            return _lastFrameVisibleElementIndexes.x < 0 && _lastFrameVisibleElementIndexes.y < 0 && _cachedMinMaxIndex.x < 0 && _cachedMinMaxIndex.y < 0;
        }

        //Element RectTransform Size
        protected float GetElementSize(int index)
        {
            var elementTransform = index >= 0 && m_elements.Count > index && m_elements[index] != null ? m_elements[index].transform : null;
            return CalculateElementSize(elementTransform, IsVertical());
        }

        protected virtual void FixLayoutInconsistencies()
        {
            //Check if definition of layouts is equal total number of item
            if (m_elements.Count != _scrollElementsCachedSize.Count || _elementsLayoutPosition.Count != _scrollElementsCachedSize.Count)
                SetCachedElementsLayoutDirty();
            while (_scrollElementsCachedSize.Count != m_elements.Count)
            {
                if (_scrollElementsCachedSize.Count > m_elements.Count)
                    _scrollElementsCachedSize.RemoveAt(_scrollElementsCachedSize.Count - 1);
                else
                    _scrollElementsCachedSize.Add(-1);
            }
            while (_elementsLayoutPosition.Count != _scrollElementsCachedSize.Count)
            {
                if (_elementsLayoutPosition.Count > _scrollElementsCachedSize.Count)
                    _elementsLayoutPosition.RemoveAt(_elementsLayoutPosition.Count - 1);
                else
                {
                    var value = GetElementSize(_elementsLayoutPosition.Count);
                    _elementsLayoutPosition.Add(value);
                }
            }
        }

        protected abstract void RecalculateLayout();

        //this function will force Content to have the same size of his parent in Non-ScrollDirection
        protected virtual void ApplyContentConstraintSize()
        {
            if (ScrollRect != null)
            {
                //Recalculate Total Content Size
                if (IsVertical())
                    SetLocalWidth(Content, GetLocalWidth(Viewport != null ? Viewport : ScrollRect.transform as RectTransform));
                else
                    SetLocalHeight(Content, GetLocalHeight(Viewport != null ? Viewport : ScrollRect.transform as RectTransform));
            }
        }

        protected virtual float SetContentSize(float size)
        {
            bool isVertical = IsVertical();

            var minContentSize = m_autoMinContentSize ? GetParentContentSize() : m_minContentSize;
            bool sizeChanged = true;
            var newSize = Mathf.Max(0, minContentSize, size);
            if (Mathf.Abs(newSize - size) <= SIZE_ERROR)
            {
                sizeChanged = false;
                newSize = size;
            }

            //Recalculate Total Content Size
            if (isVertical)
                SetLocalHeight(Content, newSize);
            else
                SetLocalWidth(Content, newSize);

            //Recalculate every Element Layout based in Delta Diff
            if (sizeChanged)
            {
                float deltaDiff = 0;
                //Recalculate Delta Diff based in align
                if (m_minContentSizeAlign == MinContentSizeAlign.Middle)
                    deltaDiff = Mathf.Max(0, newSize - size) / 2.0f;
                else if (m_minContentSizeAlign == MinContentSizeAlign.InverseScrollDirection)
                    deltaDiff = Mathf.Max(0, newSize - size);

                if (deltaDiff > SIZE_ERROR)
                {
                    //Apply DeltaDiff in any Element Position
                    for (int i = 0; i < _scrollElementsCachedSize.Count; i++)
                    {
                        _elementsLayoutPosition[i] += deltaDiff;
                    }
                }
            }
            return newSize;
        }

        #endregion

        #region Internal Helper Functions

        protected virtual bool IsElementVisible(GameObject element)
        {
            return element != null && element.transform.parent == Content;
        }

        protected bool IsElementVisible(int index)
        {
            return m_elements != null && m_elements.Count > index && index >= 0 ? IsElementVisible(m_elements[index]) : false;
        }

        protected virtual void RegisterScrollRectEvents()
        {
            UnregisterScrollRectEvents();
            if (ScrollRect != null)
            {
                if (ScrollRect.onValueChanged != null)
                    ScrollRect.onValueChanged.AddListener(OnPotentialScrollValueChanged);
            }
        }

        protected virtual void UnregisterScrollRectEvents()
        {
            if (ScrollRect != null)
            {
                if (ScrollRect.onValueChanged != null)
                    ScrollRect.onValueChanged.RemoveListener(OnPotentialScrollValueChanged);
            }
        }

        protected virtual void Init()
        {
            if (m_autoPickElements && !Application.isPlaying)
                ForcePickElements();
            TrySetupInvisibleContent();
            TryRecalculateLayout();
            if (Content != null)
            {
                var group = Content.GetComponent<LayoutGroup>();
                if (group != null)
                    group.enabled = false;
                //var fitter = Content.GetComponent<ContentSizeFitter>();
                //if (fitter != null)
                //    fitter.enabled = false;
            }
        }

        protected virtual void UpdateContentPivotAndAnchor()
        {
            var content = Content;
            if (Content != null)
            {
                if (ScrollAxis == ScrollDirectionTypeEnum.TopToBottom)
                {
                    Content.pivot = new Vector2(0, 1);
                    Content.anchorMin = new Vector2(0, 1);
                    Content.anchorMax = new Vector2(1, 1);
                }
                else if (ScrollAxis == ScrollDirectionTypeEnum.BottomToTop)
                {
                    Content.pivot = new Vector2(0, 0);
                    Content.anchorMin = new Vector2(0, 0);
                    Content.anchorMax = new Vector2(1, 0);
                }
                else if (ScrollAxis == ScrollDirectionTypeEnum.LeftToRight)
                {
                    Content.pivot = new Vector2(0, 0);
                    Content.anchorMin = new Vector2(0, 0);
                    Content.anchorMax = new Vector2(0, 1);
                }
                else
                {
                    Content.pivot = new Vector2(1, 0);
                    Content.anchorMin = new Vector2(1, 0);
                    Content.anchorMax = new Vector2(1, 1);
                }
            }
            if (ScrollRect != null)
            {
                ScrollRect.vertical = IsVertical();
                ScrollRect.horizontal = !ScrollRect.vertical;
            }
        }

        protected virtual void RevertInvisibleElementsToMainContent()
        {
            if (this != null)
            {
                SetCachedElementsLayoutDirty(true);
                if (_invisibleElementsContent != null && Content != null)
                {
                    //Pick Elements
                    for (int i = 0; i < m_elements.Count; i++)
                    {
                        var element = m_elements[i];
                        if (element != null)
                        {
                            if (element.transform.parent != Content)
                                element.transform.SetParent(Content, false);
                            element.transform.SetSiblingIndex(i + m_startingSibling);
                        }
                    }

                    //Now we must pick non-mapped objects (Templates?)
                    for (int i = 0; i < _invisibleElementsContent.childCount; i++)
                    {
                        var element = _invisibleElementsContent.GetChild(i);
                        if (element != null)
                        {
                            if (element.transform.parent != Content)
                                element.transform.SetParent(Content, false);
                            element.transform.SetSiblingIndex(i);
                        }
                    }
                }
            }
        }

        protected virtual void TrySetupInvisibleContent()
        {
            if (_invisibleElementsContent == null && Application.isPlaying)
            {
                var invisibleContainerObj = new GameObject("[AUTO_GEN] Invisible Content");
                invisibleContainerObj.transform.SetParent(this.transform);
                invisibleContainerObj.transform.localPosition = Vector3.zero;
                invisibleContainerObj.transform.localScale = Vector3.one;
                invisibleContainerObj.transform.localRotation = Quaternion.identity;
                var canvas = invisibleContainerObj.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = invisibleContainerObj.AddComponent<Canvas>();
                canvas.enabled = false;

                var layoutElement = invisibleContainerObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = invisibleContainerObj.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;

                _invisibleElementsContent = invisibleContainerObj.transform as RectTransform;
                if (_invisibleElementsContent == null)
                    _invisibleElementsContent = invisibleContainerObj.AddComponent<RectTransform>();
            }
            if (_invisibleElementsContent != null && _invisibleElementsContent.gameObject.activeSelf != !m_disableNonVisibleElements)
                _invisibleElementsContent.gameObject.SetActive(!m_disableNonVisibleElements);
        }

        protected int CalculateVisibleElementsCountThisFrame(Vector2Int currentVisibleElements)
        {
            var range = m_minVisibleElements < 0 ? m_elements.Count - 1 : m_minVisibleElements;
            if (m_optimizeDeepHierarchy)
                range = Mathf.Max(range, Math.Abs(_lastFrameVisibleElementIndexes.y + 1 - _lastFrameVisibleElementIndexes.x));

            return Mathf.Max(Math.Abs(currentVisibleElements.y + 1 - currentVisibleElements.x), range);
        }

        protected virtual Vector2Int CalculateSafeCachedMinMax()
        {
            var current = GetCurrentIndex();
            var cachedNewMinMaxIndex = new Vector2Int(
                Mathf.Clamp(current - m_extraVisibleElementsCount.Before, 0, m_elements.Count - 1),
                Mathf.Clamp(GetLastIndex(current) + m_extraVisibleElementsCount.After, 0, m_elements.Count - 1));

            var currentRange = Math.Abs(cachedNewMinMaxIndex.y + 1 - cachedNewMinMaxIndex.x);
            var targetRange = CalculateVisibleElementsCountThisFrame(cachedNewMinMaxIndex);

            //Calculate the targetVisible range based in previous activated elements
            if (currentRange < targetRange)
            {
                var elementsBefore = cachedNewMinMaxIndex.x - Mathf.Max(0, _lastFrameVisibleElementIndexes.x);
                var elementsAfter = Mathf.Max(0, _lastFrameVisibleElementIndexes.y) - cachedNewMinMaxIndex.y;

                //Try prevent bugs when visible elements return a huge amount of elements
                while (elementsBefore + elementsAfter + currentRange > targetRange)
                {
                    if (elementsBefore < elementsAfter)
                        elementsAfter--;
                    else
                        elementsBefore--;
                }

                var elementsExtra = elementsBefore + elementsAfter + currentRange == targetRange ?
                    0 :
                    Mathf.Max(0, targetRange - (elementsAfter + elementsBefore + currentRange));

                //Donate from extra elements to lowest one
                while (elementsExtra > 0)
                {
                    elementsExtra--;
                    if (elementsBefore < elementsAfter)
                        elementsBefore++;
                    else
                        elementsAfter++;
                }

                var elementsToAwakePerStep = Mathf.Max(0, m_elementsToAwakePerStep - 1);
                if (elementsAfter < 0)
                {
                    //Try pick donation from elements before or extra elements
                    while (elementsAfter < elementsToAwakePerStep && elementsBefore > 0)
                    {
                        elementsAfter++;
                        elementsBefore--;
                    }
                }
                else if (elementsBefore < 0)
                {
                    //Try pick donation from elements after or extra elements
                    while (elementsBefore < elementsToAwakePerStep && elementsAfter > 0)
                    {
                        elementsBefore++;
                        elementsAfter--;
                    }
                }

                if (cachedNewMinMaxIndex.x - elementsBefore < 0)
                    elementsAfter += elementsBefore - cachedNewMinMaxIndex.x;
                else if (cachedNewMinMaxIndex.y + elementsAfter > m_elements.Count - 1)
                    elementsBefore += (cachedNewMinMaxIndex.y + elementsBefore) - (m_elements.Count - 1);

                cachedNewMinMaxIndex.x = Mathf.Clamp(cachedNewMinMaxIndex.x - Mathf.Max(0, elementsBefore), 0, m_elements.Count - 1);
                cachedNewMinMaxIndex.y = Mathf.Clamp(cachedNewMinMaxIndex.y + Mathf.Max(0, elementsAfter), 0, m_elements.Count - 1);
            }

            return cachedNewMinMaxIndex;
        }

        protected virtual void ReloadAll_Internal(bool fullRecalc)
        {
            //Unregister events when scroll is not active
            if (GetContentSize() < GetParentContentSize())
                UnregisterScrollRectEvents();
            else if (enabled && gameObject.activeInHierarchy)
                RegisterScrollRectEvents();

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
                if (i >= 0 && i < m_elements.Count && m_elements[i] != null &&
                    (fullRecalc || i < _lastFrameVisibleElementIndexes.x || i > _lastFrameVisibleElementIndexes.y) // in 'fullRecalc == false' we only reload elements that was not previous loaded in last roll
                   )
                {
                    var element = m_elements[i];
                    Reload(element, i);
                }
            }
            _lastFrameVisibleElementIndexes = _cachedMinMaxIndex;
            if (OnVisibleElementsChanged != null)
                OnVisibleElementsChanged.Invoke(_cachedMinMaxIndex);
        }

        protected internal const float SIZE_ERROR = 0.01f;
        protected abstract void Reload(GameObject obj, int indexReload);

        /*protected virtual void SetLayoutElementPreferredSize(LayoutElement layout, Vector2 preferredSize)
        {
            if (layout != null)
            {
                var fitter = layout.GetComponent<ContentSizeFitter>();
                if(fitter == null)
                    fitter = GetComponent<ContentSizeFitter>();
                if (fitter != null && fitter.enabled)
                    preferredSize = new Vector2(-1, -1);
                if (layout.preferredWidth >= 0)
                    layout.preferredWidth = preferredSize.x;
                if (layout.preferredHeight >= 0)
                    layout.preferredHeight = preferredSize.y;
            }
        }*/

        protected internal HashSet<GameObject> _objectsToSendToInvisibleContentParent = new HashSet<GameObject>();
        protected virtual void TrySendObjectsToInvisibleContentParent()
        {
            if (_invisibleElementsContent != null)
            {
                foreach (var element in _objectsToSendToInvisibleContentParent)
                {
                    if (element != null)
                        element.transform.SetParent(_invisibleElementsContent, false);
                }
            }
            _objectsToSendToInvisibleContentParent.Clear();
        }

        protected virtual void RegisterVisibleElement(int index)
        {
            GameObject element = index >= 0 && m_elements.Count > index ? m_elements[index] : null;
            if (element != null)
            {
                if (Content != null)
                {
                    var rectTransform = element.transform as RectTransform;
                    //Setup pivots
                    if (rectTransform != null)
                    {
                        rectTransform.pivot = Content.pivot;
                        rectTransform.anchorMin = Content.anchorMin;
                        rectTransform.anchorMax = Content.anchorMax;
                    }
                    if (element.transform != Content)
                        element.transform.SetParent(Content, false);
                    if (!m_optimizeDeepHierarchy || m_spacing < 0)
                        element.transform.SetSiblingIndex(Mathf.Clamp(index - _cachedMinMaxIndex.x + m_startingSibling, 0, Content.childCount - 1));
                }
                _objectsToSendToInvisibleContentParent.Remove(element);
                //if (!object.activeSelf)
                //    object.SetActive(true);
                if (element.activeSelf && OnElementBecameVisible != null)
                    OnElementBecameVisible.Invoke(element, index);
            }
        }

        protected virtual void UnregisterVisibleElement(int index)
        {
            GameObject element = index >= 0 && m_elements.Count > index ? m_elements[index] : null;
            if (element != null)
            {
                if (_invisibleElementsContent != null && Application.isPlaying)
                {
                    _objectsToSendToInvisibleContentParent.Add(element);
                    //object.transform.SetParent(_invisibleElementsContent, false);
                }
                if (OnElementBecameInvisible != null)
                    OnElementBecameInvisible.Invoke(element, index);
            }
        }

        protected virtual void ForcePickElements()
        {
            if (Content != null)
            {
                List<GameObject> filledObjects = new List<GameObject>();
                foreach (var obj in Content)
                {
                    var transform = obj as Transform;
                    if (transform != null)
                    {
                        var layoutElement = transform.GetComponent<LayoutElement>();
                        if (layoutElement == null || !layoutElement.ignoreLayout)
                            filledObjects.Add(transform.gameObject);
                    }

                }
                ReplaceElements(filledObjects);
            }
            else
                ClearElements();

            _layoutDirty = false;
        }

        protected virtual int GetCurrentIndex()
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

                Func<int, float> getElementSize = (i) => {

                    var isInsideRange = i >= 0 && i < _scrollElementsCachedSize.Count;
                    if (isInsideRange)
                    {
                        if (_scrollElementsCachedSize[i] < 0)
                            _scrollElementsCachedSize[i] = Mathf.Max(0, GetElementSize(i));

                        return _scrollElementsCachedSize[i];
                    }

                    return 0;
                };
                var delta = m_spacing < 0 ? -m_spacing : 0; // we must do it to prevent bug when spacing is negative
                //Find current index based in old cachedIndex (Optimized Search)
                if (index < _elementsLayoutPosition.Count)
                {
                    var initialLoopIndex = _cachedMinMaxIndex.x;
                    if (anchoredPosition < (_elementsLayoutPosition[index] + delta))
                    {
                        for (int i = Mathf.Max(0, initialLoopIndex); i >= 0; i--)
                        {
                            //Search first valid element
                            var elementPosition = (_elementsLayoutPosition[i] + delta);
                            if (anchoredPosition >= elementPosition)
                            {
                                break;
                            }
                            index--;
                        }
                    }
                    else
                    {
                        for (int i = Mathf.Max(0, initialLoopIndex); i < _elementsLayoutPosition.Count; i++)
                        {
                            //Search first valid element
                            var elementPosition = (_elementsLayoutPosition[i] + delta);
                            if (anchoredPosition < elementPosition ||
                                anchoredPosition < (elementPosition + getElementSize(i)))
                            {
                                break;
                            }
                            index++;
                        }
                    }
                }
            }
            index = Mathf.Clamp(index, 0, m_elements.Count - 1);
            return index;
        }

        protected virtual int GetLastIndex(int currentIndex)
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
            Func<int, float> getElementSize = (i) => {

                var isInsideRange = i >= 0 && i < _scrollElementsCachedSize.Count;
                if (isInsideRange)
                {
                    if (_scrollElementsCachedSize[i] < 0)
                        _scrollElementsCachedSize[i] = Mathf.Max(0, GetElementSize(i));

                    return _scrollElementsCachedSize[i];
                }

                return 0;
            };
            for (int i = Mathf.Max(0, initialLoopIndex); i < _elementsLayoutPosition.Count; i++)
            {
                //Search first invalid element
                if (_elementsLayoutPosition[i] > contentMaxSize &&
                    (_elementsLayoutPosition[i] + getElementSize(i)) > contentMaxSize)
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

        protected abstract Vector3 GetElementPosition(int index);

        public virtual float GetContentSize()
        {
            return Content != null ? (IsVertical() ? GetLocalHeight(Content) : GetLocalWidth(Content)) : 0;
        }

        public float GetParentContentSize()
        {
            var target = Viewport != null ? Viewport : (ScrollRect != null ? ScrollRect.transform as RectTransform : null);
            return IsVertical() ? GetLocalHeight(target) : GetLocalWidth(target);
        }

        #endregion

        #region Enumerator

        public IEnumerator<GameObject> GetEnumerator()
        {
            return new ScrollLayoutGroupEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ScrollLayoutGroupEnumerator(this);
        }


        private sealed class ScrollLayoutGroupEnumerator : IEnumerator<GameObject>
        {
            private int m_currentIndex = -1;
            private ScrollLayoutGroup m_outer;

            public GameObject Current
            {
                get
                {
                    return this.m_outer.m_elements != null ? this.m_outer.m_elements[this.m_currentIndex] : null;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            internal ScrollLayoutGroupEnumerator(ScrollLayoutGroup outer)
            {
                this.m_outer = outer;
            }

            public bool MoveNext()
            {
                return ++this.m_currentIndex < (this.m_outer.m_elements != null ? this.m_outer.m_elements.Count : 0);
            }

            public void Reset()
            {
                this.m_currentIndex = -1;
            }

            public void Dispose()
            {
            }
        }

        #endregion

        #region Static Helper Functions

        public static T GetComponentInParent<T>(Component component, bool includeInactive)
        {
            if (component != null)
            {
                if (includeInactive)
                {
                    var result = component.GetComponent<T>();
                    if (result == null)
                    {
                        var parent = component.transform.parent;
                        if (parent != null)
                            result = GetComponentInParent<T>(parent, includeInactive);
                    }
                    return result;
                }
                else
                {
                    return component.GetComponentInParent<T>();
                }
            }
            return default(T);
        }

        public static Vector2 GetLocalSize(RectTransform rectTransform)
        {
            if (rectTransform != null)
                return rectTransform.rect.size;
            return Vector2.zero;
        }

        public static float GetLocalWidth(RectTransform rectTransform)
        {
            if (rectTransform != null)
                return rectTransform.rect.width;
            return 0;
        }

        public static float GetLocalHeight(RectTransform rectTransform)
        {
            if (rectTransform != null)
                return rectTransform.rect.height;
            return 0;
        }

        public static void SetLocalSize(RectTransform rectTransform, Vector2 newSize)
        {
            Vector2 oldSize = rectTransform.rect.size;
            Vector2 deltaSize = newSize - oldSize;
            rectTransform.offsetMin = rectTransform.offsetMin - new Vector2(deltaSize.x * rectTransform.pivot.x, deltaSize.y * rectTransform.pivot.y);
            rectTransform.offsetMax = rectTransform.offsetMax + new Vector2(deltaSize.x * (1f - rectTransform.pivot.x), deltaSize.y * (1f - rectTransform.pivot.y));
        }

        public static void SetLocalWidth(RectTransform rectTransform, float newSize)
        {
            SetLocalSize(rectTransform, new Vector2(newSize, rectTransform.rect.size.y));
            //MarkLayoutForRebuild();
        }

        public static void SetLocalHeight(RectTransform rectTransform, float newSize)
        {
            SetLocalSize(rectTransform, new Vector2(rectTransform.rect.size.x, newSize));
            //MarkLayoutForRebuild();
        }

        public static float CalculateElementSize(Component component, bool isVerticalLayout)
        {
            var elementTransform = component != null ? component.transform as RectTransform : null;
            var ignoreLayouts = elementTransform != null ? elementTransform.GetComponents<ILayoutIgnorer>() : null;
            if (ignoreLayouts != null)
            {
                foreach (var ignoreLayout in ignoreLayouts)
                {
                    if (ignoreLayout.ignoreLayout)
                    {
                        float elementSize = elementTransform != null ? (isVerticalLayout ? GetLocalHeight(elementTransform) : GetLocalWidth(elementTransform)) : 100;
                        return elementSize;
                    }
                }
            }

            float preferredSize = LayoutUtilityEx.GetPreferredSize(elementTransform, isVerticalLayout ? 1 : 0, -1);
            if (preferredSize < 0)
            {
                var elementSize = elementTransform != null ? (isVerticalLayout ? GetLocalHeight(elementTransform) : GetLocalWidth(elementTransform)) : 100;
                return elementSize;
            }

            return preferredSize;
        }

        #endregion

        #region Layout Functions

        public float minWidth
        {
            get
            {
                return -1;
            }
        }

        public float preferredWidth
        {
            get
            {
                if (_layoutSize.x == -1)
                    CalculateLayoutInputHorizontal();
                return _layoutSize.x;
            }
        }

        public float flexibleWidth
        {
            get
            {
                return -1;
            }
        }

        public float minHeight
        {
            get
            {
                return -1;
            }
        }

        public float preferredHeight
        {
            get
            {
                if (_layoutSize.y == -1)
                    CalculateLayoutInputVertical();
                return _layoutSize.y;
            }
        }

        public float flexibleHeight
        {
            get
            {
                return -1;
            }
        }

        public int layoutPriority
        {
            get
            {
                return 1;
            }
        }

        protected Vector2 _layoutSize = new Vector2(-1, -1);
        public virtual void CalculateLayoutInputHorizontal()
        {
            //TryRecalculateLayout();
            _layoutSize = new Vector2(GetLocalWidth(Content), _layoutSize.y);
        }

        public virtual void CalculateLayoutInputVertical()
        {
            //TryRecalculateLayout();
            _layoutSize = new Vector2(_layoutSize.x, GetLocalHeight(Content));
        }

        public virtual void SetLayoutHorizontal()
        {
            //As this way to recalculate layout will handle in problems in PlayMode we need only force execute in EditorMode
#if UNITY_EDITOR
            if(!Application.isPlaying)
                TryRecalculateLayout();
#endif
        }

        public virtual void SetLayoutVertical()
        {
            //As this way to recalculate layout will handle in problems in PlayMode we need only force execute in EditorMode
#if UNITY_EDITOR
            if (!Application.isPlaying)
                TryRecalculateLayout();
#endif
        }

        #endregion

    }
}