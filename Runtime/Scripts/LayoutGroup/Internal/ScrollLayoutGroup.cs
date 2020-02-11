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
    public abstract class ScrollLayoutGroup : UIBehaviour, IEnumerable<GameObject>, ILayoutElement, ICanvasElement, ILayoutGroup
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

        #endregion

        #region Private Variables

        [Header("Elements Fields")]
        [SerializeField]
        protected bool m_disableNonVisibleElements = true;
        [SerializeField]
        protected bool m_autoPickElements = true;
        [Space]
        [SerializeField, Tooltip("force extra visible elements (before/after screen)")]
        Vector2Int m_extraVisibleElements = new Vector2Int(0, 0);
        [SerializeField, Tooltip("In deep hierarchys SetParent contains a huge impact in performance. This property will try prevent recalculate amount of visible elements")]
        bool m_optimizeDeepHierarchy = true;
        [Space]
        [SerializeField]
        protected List<GameObject> m_elements = new List<GameObject>();

        [Header("Layout Fields")]
        [Space]
        [SerializeField]
        int m_startingSibling = 0;
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
        int m_minContentSize = -1;
        [SerializeField]
        bool m_autoMinContentSize = false;
        [SerializeField]
        MinContentSizeAlign m_minContentSizeAlign = MinContentSizeAlign.SameAsScrollDirection;

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

        public Vector2Int ExtraVisibleElements
        {
            get
            {
                return m_extraVisibleElements;
            }
            set
            {
                if (m_extraVisibleElements == value)
                    return;
                m_extraVisibleElements = value;
                SetCachedElementsLayoutDirty(true);
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

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterScrollRectEvents();
            Invoke("RevertInvisibleElementsToMainContent", 0.01f);

            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
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
            //TryRecalculateLayout();
        }

        protected Vector2 _lastScrollValue = new Vector2(-1, -1);
        protected virtual void OnPotentialScrollValueChanged(Vector2 p_delta)
        {
            var v_contentSize = new Vector2(GetLocalWidth(Content), GetLocalHeight(Content));
            var v_currentDeltaDistance = new Vector2(Mathf.Abs(_lastScrollValue.x - p_delta.x) * v_contentSize.x, Mathf.Abs(_lastScrollValue.y - p_delta.y) * v_contentSize.y);
            if (v_currentDeltaDistance.x < m_scrollMinDeltaDistanceToCallEvent && v_currentDeltaDistance.y < m_scrollMinDeltaDistanceToCallEvent)
                return;

            _lastScrollValue = p_delta;

            if (OnScrollValueChanged != null)
                OnScrollValueChanged.Invoke(p_delta);

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
                SetCachedElementsLayoutDirty(true);
        }

        Vector2 _oldDimension = new Vector2(-1, -1);
        protected override void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying)
            {
                base.OnRectTransformDimensionsChange();
                var v_newDimension = new Vector2(GetLocalWidth(this.transform as RectTransform), GetLocalHeight(this.transform as RectTransform));
                if ((v_newDimension.x == 0 || Mathf.Abs(_oldDimension.x - v_newDimension.x) > Mathf.Abs(_oldDimension.x * 0.01f)) ||
                    (v_newDimension.y == 0 || Mathf.Abs(_oldDimension.y - v_newDimension.y) > Mathf.Abs(_oldDimension.y * 0.01f)))
                {
                    _oldDimension = v_newDimension;
                    SetCachedElementsLayoutDirty();
                }
            }
            else
                SetCachedElementsLayoutDirty();
        }

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

        public void ReplaceElements(IList<GameObject> p_elements)
        {
            _objectsToSendToInvisibleContentParent.Clear();
            m_elements = p_elements != null ? new List<GameObject>(p_elements) : new List<GameObject>();
            _objectsToSendToInvisibleContentParent.Clear();
            SetCachedElementsLayoutDirty(true);
            if (OnAllElementsReplaced != null)
                OnAllElementsReplaced.Invoke();
        }

        public bool AddRangeElements(IList<GameObject> p_elements)
        {
            return InsertRangeElements(ElementsCount, p_elements);
        }

        public bool InsertRangeElements(int p_index, IList<GameObject> p_elements)
        {
            if (p_elements != null && p_elements.Count > 0)
            {
                p_index = Mathf.Clamp(p_index, 0, m_elements.Count - 1);
                var v_currentIndex = p_index;
                List<int> v_addIndexes = new List<int>();
                foreach (var v_element in p_elements)
                {
                    m_elements.Insert(v_currentIndex, v_element);
                    v_addIndexes.Add(v_currentIndex);
                    v_currentIndex++;
                }
                SetCachedElementsLayoutDirty(true);
                if (OnElementsAdded != null)
                    OnElementsAdded.Invoke(v_addIndexes.ToArray());
                return true;
            }
            return false;
        }

        public void AddElement(GameObject p_element)
        {
            InsertElement(ElementsCount, p_element);
        }

        public int InsertElement(int p_index, GameObject p_element)
        {
            p_index = Mathf.Clamp(p_index, 0, m_elements.Count - 1);
            m_elements.Insert(p_index, p_element);
            SetCachedElementsLayoutDirty(true);
            if (OnElementsAdded != null)
                OnElementsAdded.Invoke(new int[] { p_index });
            return p_index;
        }

        public bool RemoveElementAt(int p_index)
        {
            if (p_index >= 0 && p_index < m_elements.Count)
            {
                if (m_elements[p_index] != null)
                    _objectsToSendToInvisibleContentParent.Remove(m_elements[p_index]);
                m_elements.RemoveAt(p_index);
                SetCachedElementsLayoutDirty(true);
                if (OnElementsRemoved != null)
                    OnElementsRemoved.Invoke(new int[] { p_index });
                return true;
            }
            return false;
        }

        public bool RemoveElement(GameObject p_object)
        {
            var v_index = FindElementIndex(p_object);
            return RemoveElementAt(v_index);
        }

        public int FindElementIndex(GameObject p_object)
        {
            return m_elements.IndexOf(p_object);
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
            List<GameObject> v_visibleElements = new List<GameObject>();
            for (int i = _cachedMinMaxIndex.x; i <= _cachedMinMaxIndex.y; i++)
            {
                if (i >= 0 && m_elements.Count > i && m_elements[i] != null)
                    v_visibleElements.Add(m_elements[i]);
            }
            return v_visibleElements.ToArray();
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

        public virtual void SetCachedElementSize(int p_index, float p_itemSize)
        {
            FixLayoutInconsistencies();
            if (_scrollElementsCachedSize.Count > p_index && p_index >= 0)
            {
                var v_itemSize = _scrollElementsCachedSize[p_index];
                if (v_itemSize != p_itemSize)
                {
                    v_itemSize = p_itemSize;
                    _scrollElementsCachedSize[p_index] = v_itemSize;
                    SetCachedElementsLayoutDirty();
                    if (OnElementCachedSizeChanged != null)
                        OnElementCachedSizeChanged.Invoke(p_index);
                }
            }
        }

        public float GetCachedElementPosition(int p_index)
        {
            return _elementsLayoutPosition.Count > p_index && p_index >= 0 ? _elementsLayoutPosition[p_index] : 0;
        }

        public float GetCachedElementSize(int p_index)
        {
            FixLayoutInconsistencies();
            float v_itemSize = -1;
            if (_scrollElementsCachedSize.Count > p_index && p_index >= 0)
            {
                v_itemSize = _scrollElementsCachedSize[p_index];
                _scrollElementsCachedSize[p_index] = v_itemSize;
            }
            var v_elementTransform = p_index >= 0 && m_elements.Count > p_index && m_elements[p_index] != null ? m_elements[p_index].transform as RectTransform : null;
            var v_elementSize = v_elementTransform != null ? (IsVertical() ? GetLocalHeight(v_elementTransform) : GetLocalWidth(v_elementTransform)) : 0;
            return v_itemSize >= 0 ? v_itemSize : v_elementSize;
        }

        public virtual void SetCachedElementsLayoutDirty(bool p_performFullRecalc = false)
        {
            if (p_performFullRecalc)
            {
                _lastFrameVisibleElementIndexes = new Vector2Int(-1, -1);
                _cachedMinMaxIndex = new Vector2Int(-1, -1);
            }
            _layoutDirty = true;
            LayoutRebuilder.MarkLayoutForRebuild(this.transform as RectTransform)
                ;
        }

        public virtual void TryRecalculateLayout(bool p_force = false)
        {
            if (_elementsLayoutPosition.Count != _scrollElementsCachedSize.Count || _scrollElementsCachedSize.Count != m_elements.Count)
                _layoutDirty = true;
            if (_layoutDirty)
            {
                _layoutDirty = false;
                if (_cachedMinMaxIndex.x >= m_elements.Count || _cachedMinMaxIndex.y >= m_elements.Count)
                    _cachedMinMaxIndex = new Vector2Int(-1, -1);
                RecalculateLayout();

                var v_contentSize = GetContentSize();
                if ((IsVertical() && Mathf.Abs(Content.localPosition.y) > v_contentSize) ||
                    (!IsVertical() && Mathf.Abs(Content.localPosition.x) > v_contentSize)
                   )
                {
                    RecalculateAfterDragRebuild();
                }
                else
                {
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

        protected void RecalculateAfterDragRebuild()
        {
            if (ScrollRect != null)
                ScrollRect.Rebuild(CanvasUpdate.PostLayout);
            _lastFrameVisibleElementIndexes = new Vector2Int(-1, -1);
            _cachedMinMaxIndex = new Vector2Int(-1, -1);
            Invoke("RecalculateLayout", 0.1f);
        }

        //Element RectTransform Size
        protected float GetElementSize(int p_index)
        {
            var v_elementTransform = p_index >= 0 && m_elements.Count > p_index && m_elements[p_index] != null ? m_elements[p_index].transform : null;
            return CalculateElementSize(v_elementTransform, IsVertical());
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
                    var v_value = GetElementSize(_elementsLayoutPosition.Count);
                    _elementsLayoutPosition.Add(v_value);
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

        protected virtual float SetContentSize(float p_size)
        {
            bool v_isVertical = IsVertical();

            var v_minContentSize = m_autoMinContentSize ? GetParentContentSize() : m_minContentSize;
            bool v_sizeChanged = true;
            var v_newSize = Mathf.Max(0, v_minContentSize, p_size);
            if (Mathf.Abs(v_newSize - p_size) <= SIZE_ERROR)
            {
                v_sizeChanged = false;
                v_newSize = p_size;
            }

            //Recalculate Total Content Size
            if (v_isVertical)
                SetLocalHeight(Content, v_newSize);
            else
                SetLocalWidth(Content, v_newSize);

            //Recalculate every Element Layout based in Delta Diff
            if (v_sizeChanged)
            {
                float v_deltaDiff = 0;
                //Recalculate Delta Diff based in align
                if (m_minContentSizeAlign == MinContentSizeAlign.Middle)
                    v_deltaDiff = Mathf.Max(0, v_newSize - p_size) / 2.0f;
                else if (m_minContentSizeAlign == MinContentSizeAlign.InverseScrollDirection)
                    v_deltaDiff = Mathf.Max(0, v_newSize - p_size);

                if (v_deltaDiff > SIZE_ERROR)
                {
                    //Apply DeltaDiff in any Element Position
                    for (int i = 0; i < _scrollElementsCachedSize.Count; i++)
                    {
                        _elementsLayoutPosition[i] += v_deltaDiff;
                    }
                }
            }
            return v_newSize;
        }

        #endregion

        #region Internal Helper Functions

        protected virtual bool IsElementVisible(GameObject p_element)
        {
            return p_element != null && p_element.transform.parent == Content;
        }

        protected bool IsElementVisible(int p_index)
        {
            return m_elements != null && m_elements.Count > p_index && p_index >= 0 ? IsElementVisible(m_elements[p_index]) : false;
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
                var v_group = Content.GetComponent<LayoutGroup>();
                if (v_group != null)
                    v_group.enabled = false;
                //var v_fitter = Content.GetComponent<ContentSizeFitter>();
                //if (v_fitter != null)
                //    v_fitter.enabled = false;
            }
        }

        protected virtual void UpdateContentPivotAndAnchor()
        {
            var v_content = Content;
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
                        var v_element = m_elements[i];
                        if (v_element != null)
                        {
                            if (v_element.transform.parent != Content)
                                v_element.transform.SetParent(Content, false);
                            v_element.transform.SetSiblingIndex(i + m_startingSibling);
                        }
                    }

                    //Now we must pick non-mapped objects (Templates?)
                    for (int i = 0; i < _invisibleElementsContent.childCount; i++)
                    {
                        var v_element = _invisibleElementsContent.GetChild(i);
                        if (v_element != null)
                        {
                            if (v_element.transform.parent != Content)
                                v_element.transform.SetParent(Content, false);
                            v_element.transform.SetSiblingIndex(i);
                        }
                    }
                }
            }
        }

        protected virtual void TrySetupInvisibleContent()
        {
            if (_invisibleElementsContent == null && Application.isPlaying)
            {
                var v_invisibleContainerObj = new GameObject("[AUTO_GEN] Invisible Content");
                v_invisibleContainerObj.transform.SetParent(this.transform);
                v_invisibleContainerObj.transform.localPosition = Vector3.zero;
                v_invisibleContainerObj.transform.localScale = Vector3.one;
                v_invisibleContainerObj.transform.localRotation = Quaternion.identity;
                var v_canvas = v_invisibleContainerObj.GetComponent<Canvas>();
                if (v_canvas == null)
                    v_canvas = v_invisibleContainerObj.AddComponent<Canvas>();
                v_canvas.enabled = false;

                var v_layoutElement = v_invisibleContainerObj.GetComponent<LayoutElement>();
                if (v_layoutElement == null)
                    v_layoutElement = v_invisibleContainerObj.AddComponent<LayoutElement>();
                v_layoutElement.ignoreLayout = true;

                _invisibleElementsContent = v_invisibleContainerObj.transform as RectTransform;
                if (_invisibleElementsContent == null)
                    _invisibleElementsContent = v_invisibleContainerObj.AddComponent<RectTransform>();
            }
            if (_invisibleElementsContent != null && _invisibleElementsContent.gameObject.activeSelf != !m_disableNonVisibleElements)
                _invisibleElementsContent.gameObject.SetActive(!m_disableNonVisibleElements);
        }

        protected virtual Vector2Int CalculateSafeCachedMinMax()
        {
            var cachedNewMinMaxIndex = new Vector2Int(GetCurrentIndex(), GetLastIndex());

            if (OptimizeDeepHierarchy)
            {
                var deltaOld = Mathf.Abs(_cachedMinMaxIndex.y - _cachedMinMaxIndex.x);
                var deltaNew = Mathf.Abs(cachedNewMinMaxIndex.y - cachedNewMinMaxIndex.x);
                if (deltaNew < deltaOld)
                {
                    int deltaExtra = deltaOld - deltaNew;
                    int amountBefore = deltaExtra / 2;
                    int amountAfter = deltaExtra - amountBefore;

                    if (cachedNewMinMaxIndex.x - amountBefore < 0)
                    {
                        int invalidBeforeAmount = Mathf.Abs(cachedNewMinMaxIndex.x - amountBefore);
                        amountBefore -= invalidBeforeAmount;
                        amountAfter += invalidBeforeAmount;
                    }
                    else if (cachedNewMinMaxIndex.y + amountAfter > m_elements.Count - 1)
                    {
                        int invalidAfterAmount = Mathf.Abs((cachedNewMinMaxIndex.y + amountAfter) - (m_elements.Count - 1));
                        amountAfter -= invalidAfterAmount;
                        amountBefore += invalidAfterAmount;
                    }
                    cachedNewMinMaxIndex.x -= amountBefore;
                    cachedNewMinMaxIndex.y += amountAfter;

                    //Prevent unknown behaviours
                    cachedNewMinMaxIndex.x = Mathf.Clamp(cachedNewMinMaxIndex.x, 0, m_elements.Count - 1);
                    cachedNewMinMaxIndex.y = Mathf.Clamp(cachedNewMinMaxIndex.y, 0, m_elements.Count - 1);
                }
            }
            return cachedNewMinMaxIndex;
        }

        protected virtual void ReloadAll_Internal(bool p_fullRecalc)
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
                        var v_element = m_elements[i];
                        Reload(v_element, i);
                    }
                }
            }
            //Reload current Elements
            for (int i = _cachedMinMaxIndex.x; i <= _cachedMinMaxIndex.y; i++)
            {
                if (i >= 0 && i < m_elements.Count && m_elements[i] != null &&
                    (p_fullRecalc || i < _lastFrameVisibleElementIndexes.x || i > _lastFrameVisibleElementIndexes.y) // in 'p_fullRecalc == false' we only reload elements that was not previous loaded in last roll
                   )
                {
                    var v_element = m_elements[i];
                    Reload(v_element, i);
                }
            }
            _lastFrameVisibleElementIndexes = _cachedMinMaxIndex;
            if (OnVisibleElementsChanged != null)
                OnVisibleElementsChanged.Invoke(_cachedMinMaxIndex);
        }

        protected internal const float SIZE_ERROR = 0.01f;
        protected abstract void Reload(GameObject p_obj, int p_indexReload);

        /*protected virtual void SetLayoutElementPreferredSize(LayoutElement p_layout, Vector2 p_preferredSize)
        {
            if (p_layout != null)
            {
                var v_fitter = p_layout.GetComponent<ContentSizeFitter>();
                if(v_fitter == null)
                    v_fitter = GetComponent<ContentSizeFitter>();
                if (v_fitter != null && v_fitter.enabled)
                    p_preferredSize = new Vector2(-1, -1);
                if (p_layout.preferredWidth >= 0)
                    p_layout.preferredWidth = p_preferredSize.x;
                if (p_layout.preferredHeight >= 0)
                    p_layout.preferredHeight = p_preferredSize.y;
            }
        }*/

        protected internal HashSet<GameObject> _objectsToSendToInvisibleContentParent = new HashSet<GameObject>();
        protected virtual void TrySendObjectsToInvisibleContentParent()
        {
            if (_invisibleElementsContent != null)
            {
                foreach (var v_object in _objectsToSendToInvisibleContentParent)
                {
                    if (v_object != null)
                        v_object.transform.SetParent(_invisibleElementsContent, false);
                }
            }
            _objectsToSendToInvisibleContentParent.Clear();
        }

        protected virtual void RegisterVisibleElement(int p_index)
        {
            GameObject v_object = p_index >= 0 && m_elements.Count > p_index ? m_elements[p_index] : null;
            if (v_object != null)
            {
                if (Content != null)
                {
                    var v_rectTransform = v_object.transform as RectTransform;
                    //Setup pivots
                    if (v_rectTransform != null)
                    {
                        v_rectTransform.pivot = Content.pivot;
                        v_rectTransform.anchorMin = Content.anchorMin;
                        v_rectTransform.anchorMax = Content.anchorMax;
                    }
                    v_object.transform.SetParent(Content, false);
                    v_object.transform.SetSiblingIndex(Mathf.Clamp(p_index - _cachedMinMaxIndex.x + m_startingSibling, 0, Content.childCount - 1));
                }
                _objectsToSendToInvisibleContentParent.Remove(v_object);
                //if (!v_object.activeSelf)
                //    v_object.SetActive(true);
                if (v_object.activeSelf && OnElementBecameVisible != null)
                    OnElementBecameVisible.Invoke(v_object, p_index);
            }
        }

        protected virtual void UnregisterVisibleElement(int p_index)
        {
            GameObject v_object = p_index >= 0 && m_elements.Count > p_index ? m_elements[p_index] : null;
            if (v_object != null)
            {
                if (_invisibleElementsContent != null && Application.isPlaying)
                {
                    _objectsToSendToInvisibleContentParent.Add(v_object);
                    //v_object.transform.SetParent(_invisibleElementsContent, false);
                }
                if (OnElementBecameInvisible != null)
                    OnElementBecameInvisible.Invoke(v_object, p_index);
            }
        }

        protected virtual void ForcePickElements()
        {
            if (Content != null)
            {
                List<GameObject> v_filledObjects = new List<GameObject>();
                foreach (var v_obj in Content)
                {
                    var v_transform = v_obj as Transform;
                    if (v_transform != null)
                    {
                        var v_layoutElement = v_transform.GetComponent<LayoutElement>();
                        if (v_layoutElement == null || !v_layoutElement.ignoreLayout)
                            v_filledObjects.Add(v_transform.gameObject);
                    }

                }
                ReplaceElements(v_filledObjects);
            }
            else
                ClearElements();

            _layoutDirty = false;
        }

        protected virtual int GetCurrentIndex()
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

                var v_delta = m_spacing < 0 ? -m_spacing : 0; // we must do it to prevent bug when spacing is negative
                //Find current index based in old cachedIndex (Optimized Search)
                if (v_index < _elementsLayoutPosition.Count)
                {
                    var v_initialLoopIndex = _cachedMinMaxIndex.x;
                    if (v_anchoredPosition < (_elementsLayoutPosition[v_index] + v_delta))
                    {
                        for (int i = Mathf.Max(0, v_initialLoopIndex); i >= 0; i--)
                        {
                            if (v_anchoredPosition >= (_elementsLayoutPosition[i] + v_delta))
                            {
                                break;
                            }
                            v_index--;
                        }
                    }
                    else
                    {
                        for (int i = Mathf.Max(0, v_initialLoopIndex); i < _elementsLayoutPosition.Count; i++)
                        {
                            if (v_anchoredPosition < (_elementsLayoutPosition[i] + v_delta))
                            {
                                if (i != v_initialLoopIndex)
                                    v_index--;
                                break;
                            }
                            v_index++;
                        }
                    }
                }
            }
            v_index = Mathf.Clamp(v_index - m_extraVisibleElements.x, 0, m_elements.Count - 1);
            return v_index;
        }

        protected virtual int GetLastIndex()
        {
            var v_currentIndex = GetCurrentIndex();
            var v_lastIndex = v_currentIndex;
            var v_contentTransform = Viewport != null ? Viewport : (ScrollRect != null ? ScrollRect.transform : this.transform) as RectTransform;

            var v_contentMaxSize = IsVertical() ? GetLocalHeight(v_contentTransform) : GetLocalWidth(v_contentTransform);
            if (Content != null)
            {
                v_contentMaxSize += Mathf.Abs(IsVertical() ? Content.anchoredPosition.y : Content.anchoredPosition.x);
            }

            var v_initialLoopIndex = v_currentIndex;
            for (int i = Mathf.Max(0, v_initialLoopIndex); i < _elementsLayoutPosition.Count; i++)
            {
                if (_elementsLayoutPosition[i] > v_contentMaxSize)
                {
                    break;
                }
                v_lastIndex++;
            }
            v_lastIndex = Mathf.Clamp(v_lastIndex + m_extraVisibleElements.y, v_currentIndex, m_elements.Count - 1);
            return v_lastIndex;
        }

        protected abstract Vector3 GetElementPosition(int p_index);

        public virtual float GetContentSize()
        {
            return Content != null ? (IsVertical() ? GetLocalHeight(Content) : GetLocalWidth(Content)) : 0;
        }

        public float GetParentContentSize()
        {
            var v_target = Viewport != null ? Viewport : (ScrollRect != null ? ScrollRect.transform as RectTransform : null);
            return IsVertical() ? GetLocalHeight(v_target) : GetLocalWidth(v_target);
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

            internal ScrollLayoutGroupEnumerator(ScrollLayoutGroup p_outer)
            {
                this.m_outer = p_outer;
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

        public static T GetComponentInParent<T>(Component p_component, bool p_includeInactive)
        {
            if (p_component != null)
            {
                if (p_includeInactive)
                {
                    var v_return = p_component.GetComponent<T>();
                    if (v_return == null)
                    {
                        var v_parent = p_component.transform.parent;
                        if (v_parent != null)
                            v_return = GetComponentInParent<T>(v_parent, p_includeInactive);
                    }
                    return v_return;
                }
                else
                {
                    return p_component.GetComponentInParent<T>();
                }
            }
            return default(T);
        }

        public static Vector2 GetLocalSize(RectTransform p_rectTransform)
        {
            if (p_rectTransform != null)
                return p_rectTransform.rect.size;
            return Vector2.zero;
        }

        public static float GetLocalWidth(RectTransform p_rectTransform)
        {
            if (p_rectTransform != null)
                return p_rectTransform.rect.width;
            return 0;
        }

        public static float GetLocalHeight(RectTransform p_rectTransform)
        {
            if (p_rectTransform != null)
                return p_rectTransform.rect.height;
            return 0;
        }

        public static void SetLocalSize(RectTransform p_rectTransform, Vector2 p_newSize)
        {
            Vector2 v_oldSize = p_rectTransform.rect.size;
            Vector2 v_deltaSize = p_newSize - v_oldSize;
            p_rectTransform.offsetMin = p_rectTransform.offsetMin - new Vector2(v_deltaSize.x * p_rectTransform.pivot.x, v_deltaSize.y * p_rectTransform.pivot.y);
            p_rectTransform.offsetMax = p_rectTransform.offsetMax + new Vector2(v_deltaSize.x * (1f - p_rectTransform.pivot.x), v_deltaSize.y * (1f - p_rectTransform.pivot.y));
        }

        public static void SetLocalWidth(RectTransform p_rectTransform, float p_newSize)
        {
            SetLocalSize(p_rectTransform, new Vector2(p_newSize, p_rectTransform.rect.size.y));
            LayoutRebuilder.MarkLayoutForRebuild(p_rectTransform);
        }

        public static void SetLocalHeight(RectTransform p_rectTransform, float p_newSize)
        {
            SetLocalSize(p_rectTransform, new Vector2(p_rectTransform.rect.size.x, p_newSize));
            LayoutRebuilder.MarkLayoutForRebuild(p_rectTransform);
        }

        public static float CalculateElementSize(Component p_object, bool p_isVerticalLayout)
        {
            var v_elementTransform = p_object != null ? p_object.transform as RectTransform : null;
            var ignoreLayouts = v_elementTransform != null ? v_elementTransform.GetComponents<ILayoutIgnorer>() : null;
            if (ignoreLayouts != null)
            {
                foreach (var ignoreLayout in ignoreLayouts)
                {
                    if (ignoreLayout.ignoreLayout)
                    {
                        float v_elementSize = v_elementTransform != null ? (p_isVerticalLayout ? GetLocalHeight(v_elementTransform) : GetLocalWidth(v_elementTransform)) : 100;
                        return v_elementSize;
                    }
                }
            }

            float preferredSize = LayoutUtilityEx.GetPreferredSize(v_elementTransform, p_isVerticalLayout ? 1 : 0);
            //v_elementSize = Mathf.Max(preferredSize, v_elementSize);

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
            TryRecalculateLayout();
            _layoutSize = new Vector2(GetLocalWidth(Content), _layoutSize.y);
        }

        public virtual void CalculateLayoutInputVertical()
        {
            TryRecalculateLayout();
            _layoutSize = new Vector2(_layoutSize.x, GetLocalHeight(Content));
        }

        public void SetLayoutHorizontal()
        {
            TryRecalculateLayout();
        }

        public void SetLayoutVertical()
        {
            TryRecalculateLayout();
        }

        #endregion

        #region ICanvas Rebuild Element (Editor Only)

        void ICanvasElement.Rebuild(CanvasUpdate executing)
        {
            if (executing != CanvasUpdate.PostLayout)
                return;

            if (!Application.isPlaying)
                SetCachedElementsLayoutDirty();
        }

        void ICanvasElement.LayoutComplete()
        {
        }

        void ICanvasElement.GraphicUpdateComplete()
        {
        }

        #endregion

    }
}