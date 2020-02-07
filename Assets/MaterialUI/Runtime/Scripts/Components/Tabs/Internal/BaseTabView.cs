//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.


using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

namespace MaterialUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [ExecuteInEditMode]
    public abstract class BaseTabView : UIBehaviour
    {
        #region Private Variables

        [SerializeField]
        protected bool m_AnimateTabs = true;
        [Space]
        [SerializeField]
        protected RectTransform m_TabsContainer = null;
        [SerializeField]
        protected int m_CurrentPage = 0;
        [SerializeField]
        protected TabItem m_TabItemTemplate = null;
        [SerializeField]
        protected RectTransform m_PagesContainer = null;
        [SerializeField]
        protected RectTransform m_PagesRect = null;
        [SerializeField]
        protected RectTransform m_Indicator = null;
        [Space]
        [SerializeField]
        protected bool m_CanScrollBetweenTabs = true;

        protected ScrollRect m_PagesScrollRect;
        protected TabItem[] m_Tabs;
        protected RectTransform m_RectTransform;

        protected int m_IndicatorSizeTweener;
        protected int m_IndicatorTweener;
        protected int m_TabsContainerTweener;
        protected int m_PagesContainerTweener;

        protected Vector2 m_PageSize;
        protected Canvas _RootCanvas;

        #endregion

        #region Public Properties

        public RectTransform tabsContainer
        {
            get { return m_TabsContainer; }
            set { m_TabsContainer = value; }
        }

        public abstract List<TabPage> pages
        {
            get;
            set;
        }

        public int currentPage
        {
            get { return m_CurrentPage; }
            set
            {
                if (m_CurrentPage == value)
                    return;
                if (Application.isPlaying)
                    SetPage(value);
                else
                    m_CurrentPage = value;
            }
        }

        public TabItem tabItemTemplate
        {
            get { return m_TabItemTemplate; }
            set { m_TabItemTemplate = value; }
        }

        public RectTransform pagesContainer
        {
            get { return m_PagesContainer; }
            set { m_PagesContainer = value; }
        }

        public RectTransform pagesRect
        {
            get { return m_PagesRect; }
            set { m_PagesRect = value; }
        }

        public RectTransform indicator
        {
            get { return m_Indicator; }
            set { m_Indicator = value; }
        }

        public ScrollRect pagesScrollRect
        {
            get
            {
                if (m_PagesScrollRect == null)
                {
                    m_PagesScrollRect = m_PagesRect.GetComponent<ScrollRect>();
                }
                return m_PagesScrollRect;
            }
        }

        public TabItem[] tabs
        {
            get { return m_Tabs; }
        }

        public bool canScrollBetweenTabs
        {
            get { return m_CanScrollBetweenTabs; }
            set
            {
                m_CanScrollBetweenTabs = value;
                pagesScrollRect.enabled = value;
                OverscrollConfig overscroll = pagesScrollRect.GetComponent<OverscrollConfig>();
                if (overscroll != null)
                {
                    overscroll.enabled = value;
                }
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    m_RectTransform = (RectTransform)transform;
                }
                return m_RectTransform;
            }
        }

        public bool animateTabs
        {
            get
            {
                return m_AnimateTabs;
            }

            set
            {
                m_AnimateTabs = value;
            }
        }

        public Canvas rootCanvas
        {
            get
            {
                if (_RootCanvas == null)
                {
                    _RootCanvas = transform.GetRootCanvas();
                }
                return _RootCanvas;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started && Application.isPlaying)
                InitializeTabsAndPagesDelayed();

#if UNITY_EDITOR
            Selection.selectionChanged -= OnValidate;
            Selection.selectionChanged += OnValidate;
#endif
        }

        protected bool _started = false;
        protected override void Start()
        {
            if (Application.isPlaying)
            {
                _started = true;
                InitializeTabsAndPages();

                var scaler = rootCanvas != null ? rootCanvas.GetComponent<MaterialCanvasScaler>() : null;
                if (scaler != null)
                    scaler.onCanvasAreaChanged.AddListener(OnCanvasAreaChanged);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CancelInvoke();
#if UNITY_EDITOR
            Selection.selectionChanged -= OnValidate;
#endif
        }

        protected virtual void OnCanvasAreaChanged(bool scaleChanged, bool orientationChanged)
        {
            if (Application.isPlaying)
            {
                InitializeTabsAndPages();
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (Application.isPlaying)
            {
                InitializeTabsAndPagesDelayed();
            }
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (Application.isPlaying)
            {
                InitializeTabsAndPagesDelayed();
            }
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            if (Application.isPlaying)
            {
                InitializeTabsAndPagesDelayed();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying)
            {
                var scaler = rootCanvas != null ? rootCanvas.GetComponent<MaterialCanvasScaler>() : null;
                if (scaler != null)
                    scaler.onCanvasAreaChanged.RemoveListener(OnCanvasAreaChanged);
            }
        }

        #endregion

        #region Initialize Methods

        public virtual void InitializeTabs()
        {
            if (this == null || m_TabsContainer == null || !Application.isPlaying)
                return;

            var contentSizeFitter = m_TabsContainer.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
                contentSizeFitter.enabled = true;

            //Initialize TabContainer
            m_TabsContainer.anchorMin = Vector2.zero;
            m_TabsContainer.anchorMax = new Vector2(0, 1);

            SetupTabSize();
            InstantiateTabsFromTemplate();

            var barParent = m_TabsContainer.parent as RectTransform;
            //Setup Size
            var tabContainerLayoutElement = m_TabsContainer.GetComponent<LayoutElement>();
            if (tabContainerLayoutElement)
                tabContainerLayoutElement.minWidth = barParent.GetProperSize().x;//Mathf.Max(barWidth, tabContainerLayoutElement.minWidth, barParent != null? barParent.GetProperSize().x : 0);

            //Configure Overscroll
            OverscrollConfig overscrollConfig = m_TabsContainer.parent.GetComponent<OverscrollConfig>();
            if (overscrollConfig != null)
                overscrollConfig.Setup();

            //Fix Indicator size in next cycle
            InitializeIndicatorDelayed();
        }

        protected void InitializeIndicatorDelayed()
        {
            CancelInvoke("InitializeIndicator");
            Invoke("InitializeIndicator", 0.05f);
        }

        protected virtual void InitializeIndicator()
        {
            if (this == null || m_Indicator == null || !Application.isPlaying)
                return;

            m_Indicator.anchorMin = new Vector2(0, 0);
            m_Indicator.anchorMax = new Vector2(0, 0);
            //m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_ForceSameTabSize ? m_TabWidth : m_Tabs[m_CurrentPage].rectTransform.GetProperSize().x);
            m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_Tabs[m_CurrentPage].rectTransform.GetProperSize().x);

            if (m_Indicator != null && m_Indicator.transform.parent == m_TabItemTemplate.transform.parent)
                m_Indicator.transform.SetAsLastSibling();

            TweenIndicator(m_CurrentPage, false);
        }

        protected virtual void SetupTabSize()
        {
            m_TabItemTemplate.gameObject.SetActive(false);
        }

        protected virtual void InstantiateTabsFromTemplate()
        {
            var tabs = new List<TabItem>(m_Tabs != null ? m_Tabs : new TabItem[pages.Count]);
            m_Tabs = new TabItem[pages.Count];

            for (int i = 0; i < pages.Count; i++)
            {
                TabItem tab = tabs.Count > i ? tabs[i] : null;
                if (tab == null)
                    tab = Instantiate(m_TabItemTemplate.gameObject).GetComponent<TabItem>();

                tab.gameObject.SetActive(true);
                tab.rectTransform.SetParent(m_TabItemTemplate.transform.parent);

                tab.rectTransform.localScale = Vector3.one;
                tab.rectTransform.localEulerAngles = Vector3.zero;
                tab.rectTransform.localPosition = new Vector3(tab.rectTransform.localPosition.x, tab.rectTransform.localPosition.y, 0f);

                tab.id = i;

                if (!string.IsNullOrEmpty(pages[i].tabName))
                {
                    tab.name = pages[i].tabName;
                    tab.labelText = tab.name;

                    /*if (tab.graphic != null)
                    {
                        tab.graphic.SetGraphicText(tab.name);
                    }*/
                }
                else
                {
                    tab.name = "Tab " + i;
                    tab.labelText = "";
                    /*if (tab.graphic != null)
                    {
                        tab.graphic.enabled = false;
                    }*/
                }

                tab.SetupGraphic(pages[i].tabIcon.imageDataType);

                if (tab.itemIcon != null)
                {
                    if (pages[i].tabIcon != null)
                    {
                        tab.itemIcon.SetImageData(pages[i].tabIcon);
                    }
                    else
                    {
                        tab.itemIcon.enabled = false;
                    }
                }

                m_Tabs[i] = tab;
            }

            //Destroy extra tabs
            for (int i = pages.Count; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab != null)
                {
                    if (Application.isPlaying)
                        GameObject.Destroy(tab.gameObject);
                    else
                        GameObject.DestroyImmediate(tab.gameObject);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        public virtual void InitializePages()
        {
            if (this == null || m_PagesRect == null || !Application.isPlaying)
                return;

            if (pages.Count > 0)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    pages[i].gameObject.SetActive(true);
                }
            }

            m_PageSize = m_PagesRect.GetProperSize();

            for (int i = 0; i < pages.Count; i++)
            {
                RectTransform page = pages[i].rectTransform;

                page.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, i * m_PageSize.x, m_PageSize.x);
                page.anchorMin = Vector2.zero;
                page.anchorMax = new Vector2(0, 1);
                page.sizeDelta = new Vector2(page.sizeDelta.x, 0);
            }

            m_PagesContainer.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, m_PageSize.x * pages.Count);
            m_PagesContainer.anchorMin = Vector2.zero;
            m_PagesContainer.anchorMax = new Vector2(0, 1);
            m_PagesContainer.sizeDelta = new Vector2(m_PagesContainer.sizeDelta.x, 0);

            OverscrollConfig overscrollConfig = m_PagesRect.GetComponent<OverscrollConfig>();

            if (overscrollConfig != null)
            {
                overscrollConfig.Setup();
            }

            SetPage(m_CurrentPage, false);
        }

        public void InitializeTabsAndPagesDelayed()
        {
            CancelInvoke("InitializeTabsAndPages");
            Invoke("InitializeTabsAndPages", 0);
        }

        public virtual void InitializeTabsAndPages()
        {
            if (this == null || !Application.isPlaying)
                return;

            InitializeTabs();
            InitializePages();
        }

        #endregion

        #region Helper Functions

        public void SetPage(int index)
        {
            SetPage(index, animateTabs);
        }

        public virtual void SetPage(int index, bool animate)
        {
            index = Mathf.Clamp(index, 0, pages.Count - 1);

            TweenIndicator(index, animate);
            TweenTabsContainer(index, animate);
            TweenPagesContainer(index, animate);

            if (m_Tabs == null)
                m_Tabs = new TabItem[0];

            for (int i = 0; i < m_Tabs.Length; i++)
            {
                if (m_Tabs[i] != null)
                {
                    //Call Events
                    m_Tabs[i].isOn = i == index;
                }
            }
        }

        private void TweenPagesContainer(int index, bool animate = true)
        {
            if (m_PagesContainer == null)
                return;

            if (pages == null)
                pages = new List<TabPage>();

            for (int i = 0; i < pages.Count; i++)
            {
                int smaller = Mathf.Min(m_CurrentPage, index);
                int bigger = Mathf.Max(m_CurrentPage, index);

                //if (i >= smaller - 1 && i <= bigger + 1)
                if (i == smaller || i == bigger)
                {
                    pages[i].gameObject.SetActive(true);
                }
                else
                {
                    pages[i].DisableIfAllowed();
                }
            }

            float targetPosition = -(index * m_PageSize.x);

            targetPosition = Mathf.Clamp(targetPosition, -(pages.Count * m_PageSize.x), 0);

            TweenManager.EndTween(m_PagesContainerTweener);

            m_CurrentPage = index;

            if (animate)
            {
                m_PagesContainerTweener =
                    TweenManager.TweenVector2(vector2 => m_PagesContainer.anchoredPosition = vector2,
                        m_PagesContainer.anchoredPosition, new Vector2(targetPosition, 0), 0.5f, 0, OnPagesTweenEnd);
            }
            else
            {
                m_PagesContainer.anchoredPosition = new Vector2(targetPosition, 0);
                OnPagesTweenEnd();
            }
        }

        protected virtual void TweenTabsContainer(int index, bool animate = true)
        {
            if (m_TabsContainer == null)
                return;

            var useSameSizeCalculation = /*m_ForceSameTabSize ||*/ m_Tabs.Length <= index || index < 0 || m_Tabs[index] == null;

            float targetPosition = 0;
            if (useSameSizeCalculation)
            {
                targetPosition += rectTransform.GetProperSize().x / 2;

                targetPosition = Mathf.Clamp(targetPosition, -LayoutUtility.GetPreferredWidth(m_TabsContainer) + rectTransform.GetProperSize().x, 0);
            }
            else
                targetPosition = m_Tabs[index].rectTransform.anchoredPosition.x + (m_Tabs[index].rectTransform.GetProperSize().x / 2);

            TweenManager.EndTween(m_TabsContainerTweener);

            if (animate)
            {
                m_TabsContainerTweener = TweenManager.TweenVector2(
                    vector2 => m_TabsContainer.anchoredPosition = vector2, m_TabsContainer.anchoredPosition,
                    new Vector2(targetPosition, 0), 0.5f);
            }
            else
            {
                m_TabsContainer.anchoredPosition = new Vector2(targetPosition, 0);
            }
        }

        protected virtual void TweenIndicator(int targetTab, bool animate = true)
        {
            if (m_Indicator == null)
                return;

            float targetPosition = /*m_ForceSameTabSize ||*/ m_Tabs.Length <= targetTab || targetTab < 0 || m_Tabs[targetTab] == null ?
                0 :
                m_Tabs[targetTab].rectTransform.anchoredPosition.x - (m_Tabs[targetTab].rectTransform.GetProperSize().x / 2);

            float targetSize = /*m_ForceSameTabSize ||*/ m_Tabs.Length <= targetTab || targetTab < 0 || m_Tabs[targetTab] == null ?
                0 :
                m_Tabs[targetTab].rectTransform.GetProperSize().x;

            TweenManager.EndTween(m_IndicatorSizeTweener);
            TweenManager.EndTween(m_IndicatorTweener);

            if (animate)
            {
                //if (!m_ForceSameTabSize)
                //    m_IndicatorSizeTweener = TweenManager.TweenFloat(value => m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value), m_Indicator.GetProperSize().x, targetSize, 0.5f);
                m_IndicatorTweener = TweenManager.TweenVector2(vector2 =>
                {
                    if (m_Indicator != null)
                        m_Indicator.anchoredPosition = vector2;
                }, 
                m_Indicator.anchoredPosition, new Vector2(targetPosition, 0), 0.5f);
            }
            else
            {
                //if (!m_ForceSameTabSize)
                //    m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize);
                m_Indicator.anchoredPosition = new Vector2(targetPosition, 0);
            }
        }

        public virtual void TabItemPointerDown(int id)
        {
            TweenManager.EndTween(m_TabsContainerTweener);
        }

        public virtual void TabPagePointerUp(float delta)
        {
            if (m_CanScrollBetweenTabs)
            {
                pagesScrollRect.velocity = Vector2.zero;

                if (Mathf.Abs(delta) < 1)
                {
                    SetPage(NearestPage(), true);
                }
                else
                {
                    if (delta < 0)
                    {
                        SetPage(NearestPage(1), true);
                    }
                    else
                    {
                        SetPage(NearestPage(-1), true);
                    }
                }
            }
        }

        protected virtual int NearestPage(int direction = 0)
        {
            float currentPosition = -m_PagesContainer.anchoredPosition.x;

            if (direction < 0)
            {
                return Mathf.FloorToInt(currentPosition / m_PageSize.x);
            }

            if (direction > 0)
            {
                return Mathf.CeilToInt(currentPosition / m_PageSize.x);
            }

            return Mathf.RoundToInt(currentPosition / m_PageSize.x);
        }

        public virtual void TabPageDrag()
        {
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            if (m_CanScrollBetweenTabs)
            {
                if (TweenManager.TweenIsActive(m_PagesContainerTweener))
                {
                    TweenManager.EndTween(m_PagesContainerTweener);

                    m_CurrentPage = NearestPage();

                    OnPagesTweenEnd();

                    TweenIndicator(m_CurrentPage);
                }

                TweenManager.EndTween(m_IndicatorTweener);

                if (m_Indicator != null)
                {
                    float normalizedPagesContainerPosition = -m_PagesContainer.anchoredPosition.x / (m_PageSize.x * pages.Count);
                    /*if (m_ForceSameTabSize)
                    {
                        m_Indicator.anchoredPosition = new Vector2((m_TabWidth * m_Tabs.Length) * normalizedPagesContainerPosition, 0);
                    }
                    else*/
                    {
                        m_Indicator.anchoredPosition = new Vector2(rectTransform.GetProperSize().x * normalizedPagesContainerPosition, 0);
                    }
                }
            }
        }

        #endregion

        #region Receivers

        protected virtual void OnPagesTweenEnd()
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (i == m_CurrentPage)
                //if (i >= m_CurrentPage - 1 && i <= m_CurrentPage + 1)
                {
                    pages[i].gameObject.SetActive(true);
                    pages[i].CallOnShow();
                }
                else
                {
                    pages[i].DisableIfAllowed();
                }
            }
        }

        #endregion
    }
}