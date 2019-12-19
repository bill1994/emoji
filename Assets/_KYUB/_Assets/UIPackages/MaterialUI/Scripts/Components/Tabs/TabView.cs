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

namespace MaterialUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    //[ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/TabView", 100)]
    public class TabView : UIBehaviour
    {
        #region Private Variables

#if UNITY_EDITOR
        [SerializeField]
        private bool m_AutoTrackPages = true;

        [SerializeField]
        private bool m_OnlyShowSelectedPage = true;

        private GameObject m_OldSelectionObjects;

        private bool m_PagesDirty;
#endif

        [SerializeField]
        bool m_ForceSameTabSize = true;
        [Space]
        [SerializeField]
        private RectTransform m_TabsContainer = null;
        [SerializeField]
        private TabPage[] m_Pages = null;
        [SerializeField]
        private int m_CurrentPage = 0;
        [SerializeField]
        private TabItem m_TabItemTemplate = null;
        [SerializeField]
        private RectTransform m_PagesContainer = null;
        [SerializeField]
        private RectTransform m_PagesRect = null;
        [SerializeField]
        private RectTransform m_Indicator = null;
        [Space]
        //[SerializeField]
        //private bool m_LowerUnselectedTabAlpha = true;
        [SerializeField]
        private bool m_CanScrollBetweenTabs = true;

        private ScrollRect m_PagesScrollRect;
        private float m_TabWidth;
        private float m_TabPadding = 12;
        private TabItem[] m_Tabs;
        private RectTransform m_RectTransform;

        private int m_IndicatorSizeTweener;
        private int m_IndicatorTweener;
        private int m_TabsContainerTweener;
        private int m_PagesContainerTweener;

        private Vector2 m_PageSize;
        //private bool m_AlreadyInitialized;

        #endregion

        #region Public Properties

        /*public float shrinkTabsToFitThreshold
        {
            get { return m_ShrinkTabsToFitThreshold; }
            set { m_ShrinkTabsToFitThreshold = value; }
        }

        public bool forceStretchTabsOnLanscape
        {
            get { return m_ForceStretchTabsOnLanscape; }
            set { m_ForceStretchTabsOnLanscape = value; }
        }*/

        public RectTransform tabsContainer
        {
            get { return m_TabsContainer; }
            set { m_TabsContainer = value; }
        }

        public TabPage[] pages
        {
            get { return m_Pages; }
            set { m_Pages = value; }
        }

        public int currentPage
        {
            get { return m_CurrentPage; }
            set { m_CurrentPage = value; }
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

        public float tabWidth
        {
            get { return m_TabWidth; }
        }

        public float tabPadding
        {
            get { return m_TabPadding; }
            set { m_TabPadding = value; }
        }

        public TabItem[] tabs
        {
            get { return m_Tabs; }
        }

        /*public bool lowerUnselectedTabAlpha
        {
            get { return m_LowerUnselectedTabAlpha; }
            set { m_LowerUnselectedTabAlpha = value; }
        }*/

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

        public bool forceSameTabSize
        {
            get
            {
                return m_ForceSameTabSize;
            }

            set
            {
                m_ForceSameTabSize = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnDisable()
        {
            base.OnDisable();
            _initializeTabsAndPagesCoroutine = null;
#if UNITY_EDITOR
            CancelInvoke();
#endif
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started && Application.isPlaying)
                InitializeTabsAndPagesDelayed();
        }

        bool _started = false;
        protected override void Start()
        {
            if (Application.isPlaying)
            {
                _started = true;
                InitializeTabsAndPagesDelayed();

                MaterialUIScaler.GetRootScaler(rectTransform).onCanvasAreaChanged.AddListener((scaleChanged, orientationChanged) =>
                {
                    if (Application.isPlaying)
                    {
                        InitializeTabsAndPagesDelayed();
                    }
                });
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

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (enabled && gameObject.activeInHierarchy)
                Invoke("TrackPages", 0.05f);
        }
#endif

        #endregion

        #region Initialize Methods

        public void InitializeTabs()
        {
            //Initialize TabContainer
            float barWidth = rectTransform.GetProperSize().x;

            m_TabsContainer.GetComponent<LayoutElement>().minWidth = barWidth;

            var contentSizeFitter = m_TabsContainer.GetComponent<ContentSizeFitter>();
            if(contentSizeFitter != null)
                contentSizeFitter.enabled = true;

            if(m_ForceSameTabSize)
                m_TabsContainer.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            m_TabsContainer.anchorMin = Vector2.zero;
            m_TabsContainer.anchorMax = new Vector2(0, 1);

            SetupTabSize();
            InstantiateTabsFromTemplate();
            InitializeIndicator();

            //Configure Overscroll
            OverscrollConfig overscrollConfig = m_TabsContainer.parent.GetComponent<OverscrollConfig>();
            if (overscrollConfig != null)
                overscrollConfig.Setup();

            //m_AlreadyInitialized = true;
        }

        protected void InitializeIndicator()
        {
            m_Indicator.anchorMin = new Vector2(0, 0);
            m_Indicator.anchorMax = new Vector2(0, 0);
            m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_ForceSameTabSize ? m_TabWidth : m_Tabs[m_CurrentPage].rectTransform.GetProperSize().x);

            if (m_Indicator != null && m_Indicator.transform.parent == m_TabItemTemplate.transform.parent)
                m_Indicator.transform.SetAsLastSibling();

            TweenIndicator(m_CurrentPage, true);
        }

        protected void SetupTabSize()
        {
            if (m_ForceSameTabSize)
            {
                float barWidth = rectTransform.GetProperSize().x;

                m_TabWidth = GetMaxTabTextWidth() + (2 * m_TabPadding);
                float combinedWidth = m_TabWidth * m_Pages.Length;
                m_TabItemTemplate.GetComponent<LayoutElement>().minWidth = 72;

                var v_minDelta = 16f;
                if (combinedWidth - barWidth < v_minDelta)
                {
                    m_TabWidth = barWidth / m_Pages.Length;
                }

                m_TabWidth = Mathf.Max(m_TabWidth, m_TabItemTemplate.GetComponent<LayoutElement>().minWidth);
            }
            else
            {
                m_TabWidth = -1;
            }
            m_TabItemTemplate.GetComponent<LayoutElement>().preferredWidth = m_TabWidth;
            m_TabItemTemplate.gameObject.SetActive(false);
        }

        protected void InstantiateTabsFromTemplate()
        {
            var tabs = new List<TabItem>(m_Tabs != null ? m_Tabs : new TabItem[m_Pages.Length]);
            m_Tabs = new TabItem[m_Pages.Length];

            for (int i = 0; i < m_Pages.Length; i++)
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

                if (!string.IsNullOrEmpty(m_Pages[i].tabName))
                {
                    tab.name = m_Pages[i].tabName;
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

                tab.SetupGraphic(m_Pages[i].tabIcon.imageDataType);

                if (tab.itemIcon != null)
                {
                    if (m_Pages[i].tabIcon != null)
                    {
                        tab.itemIcon.SetImage(m_Pages[i].tabIcon);
                    }
                    else
                    {
                        tab.itemIcon.enabled = false;
                    }
                }

                m_Tabs[i] = tab;
            }

            //Destroy extra tabs
            for (int i = m_Pages.Length; i < tabs.Count; i++)
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

        public void InitializePages()
        {
            if (m_Pages.Length > 0)
            {
                for (int i = 0; i < m_Pages.Length; i++)
                {
                    m_Pages[i].gameObject.SetActive(true);
                }
            }

            m_PageSize = m_PagesRect.GetProperSize();

            for (int i = 0; i < m_Pages.Length; i++)
            {
                RectTransform page = m_Pages[i].rectTransform;

                page.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, i * m_PageSize.x, m_PageSize.x);
                page.anchorMin = Vector2.zero;
                page.anchorMax = new Vector2(0, 1);
                page.sizeDelta = new Vector2(page.sizeDelta.x, 0);
            }

            m_PagesContainer.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, m_PageSize.x * m_Pages.Length);
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

        Coroutine _initializeTabsAndPagesCoroutine = null;
        public void InitializeTabsAndPagesDelayed()
        {
            if (enabled && gameObject.activeInHierarchy)
            {
                if (_initializeTabsAndPagesCoroutine == null)
                    _initializeTabsAndPagesCoroutine = StartCoroutine(InitializeTabsAndPagesRoutine());
            }
            else
            {
                if (_initializeTabsAndPagesCoroutine != null)
                {
                    StopCoroutine(_initializeTabsAndPagesCoroutine);
                    _initializeTabsAndPagesCoroutine = null;
                }

                InitializeTabs();
                InitializePages();
            }
        }

        protected IEnumerator InitializeTabsAndPagesRoutine()
        {
            yield return null;
            InitializeTabs();
            yield return null;
            InitializePages();

            _initializeTabsAndPagesCoroutine = null;
        }

        #endregion

        #region Helper Functions

        private float GetMaxTabTextWidth()
        {
            float longestTextWidth = 0;

            if (m_TabItemTemplate.graphic != null)
            {
                var changed = false;
                var originalText = m_TabItemTemplate.graphic.GetGraphicText();
                for (int i = 0; i < m_Pages.Length; i++)
                {
                    ILayoutElement layoutElement = m_TabItemTemplate.graphic as ILayoutElement;
                    if (layoutElement != null)
                    {
                        changed = true;
                        m_TabItemTemplate.graphic.SetGraphicText(m_Pages[i].tabName);
                        layoutElement.CalculateLayoutInputHorizontal();
                        layoutElement.CalculateLayoutInputVertical();
                        longestTextWidth = Mathf.Max(longestTextWidth, layoutElement.preferredWidth);
                    }
                }
                if(changed)
                    m_TabItemTemplate.graphic.SetGraphicText(originalText);
            }

            return longestTextWidth;
        }

        public void SetPage(int index)
        {
            SetPage(index, true);
        }

        public void SetPage(int index, bool animate)
        {
            index = Mathf.Clamp(index, 0, m_Pages.Length - 1);

            TweenIndicator(index, animate);
            TweenTabsContainer(index, animate);
            TweenPagesContainer(index, animate);

            for (int i = 0; i < m_Tabs.Length; i++)
            {
                if (m_Tabs[i] != null)
                {
                    //Call Events
                    m_Tabs[i].isOn = i == index;
                    //Tween
                    /*if (m_LowerUnselectedTabAlpha)
                    {
                        if (animate)
                        {
                            int i1 = i;
                            TweenManager.TweenFloat(
                                f =>
                                {
                                    if (m_Tabs[i1] != null)
                                        m_Tabs[i1].canvasGroup.alpha = f;
                                },
                                () => m_Tabs[i1] != null ? m_Tabs[i1].canvasGroup.alpha : 0,
                                () => m_Pages[i1] != null && m_Pages[i1].interactable ? (i1 == index ? 1f : 0.5f) : 0.15f,
                                0.5f);
                        }
                        else
                        {
                            m_Tabs[i].canvasGroup.alpha = m_Pages[i].interactable ? (i == index ? 1f : 0.5f) : 0.15f;
                        }
                    }*/
                }
            }
        }

        private void TweenPagesContainer(int index, bool animate = true)
        {
            for (int i = 0; i < m_Pages.Length; i++)
            {
                int smaller = Mathf.Min(m_CurrentPage, index);
                int bigger = Mathf.Max(m_CurrentPage, index);

                //if (i >= smaller - 1 && i <= bigger + 1)
                if (i == smaller || i == bigger)
                {
                    m_Pages[i].gameObject.SetActive(true);
                }
                else
                {
                    m_Pages[i].DisableIfAllowed();
                }
            }

            float targetPosition = -(index * m_PageSize.x);

            targetPosition = Mathf.Clamp(targetPosition, -(m_Pages.Length * m_PageSize.x), 0);

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

        private void TweenTabsContainer(int index, bool animate = true)
        {
            var v_useSameSizeCalculation = m_ForceSameTabSize || m_Tabs.Length <= index || index < 0 || m_Tabs[index] == null;

            float targetPosition = 0;
            if (v_useSameSizeCalculation)
            {
                targetPosition = -(index * m_TabWidth);

                targetPosition += rectTransform.GetProperSize().x / 2;
                targetPosition -= m_TabWidth / 2;

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

        private void TweenIndicator(int targetTab, bool animate = true)
        {
            float targetPosition = m_ForceSameTabSize || m_Tabs.Length <= targetTab || targetTab < 0  || m_Tabs[targetTab] == null? 
                targetTab * m_TabWidth : 
                m_Tabs[targetTab].rectTransform.anchoredPosition.x - (m_Tabs[targetTab].rectTransform.GetProperSize().x/2);

            float targetSize = m_ForceSameTabSize || m_Tabs.Length <= targetTab || targetTab < 0 || m_Tabs[targetTab] == null ?
                m_TabWidth :
                m_Tabs[targetTab].rectTransform.GetProperSize().x;

            TweenManager.EndTween(m_IndicatorSizeTweener);
            TweenManager.EndTween(m_IndicatorTweener);

            if (animate)
            {
                if(!m_ForceSameTabSize)
                    m_IndicatorSizeTweener = TweenManager.TweenFloat(value => m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value), m_Indicator.GetProperSize().x, targetSize, 0.5f);
                m_IndicatorTweener = TweenManager.TweenVector2(vector2 => m_Indicator.anchoredPosition = vector2, m_Indicator.anchoredPosition, new Vector2(targetPosition, 0), 0.5f);
            }
            else
            {
                if (!m_ForceSameTabSize)
                    m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize);
                m_Indicator.anchoredPosition = new Vector2(targetPosition, 0);
            }
        }

        public void TabItemPointerDown(int id)
        {
            TweenManager.EndTween(m_TabsContainerTweener);
        }

        public void TabPagePointerUp(float delta)
        {
            if (m_CanScrollBetweenTabs)
            {
                pagesScrollRect.velocity = Vector2.zero;

                if (Mathf.Abs(delta) < 1)
                {
                    SetPage(NearestPage());
                }
                else
                {
                    if (delta < 0)
                    {
                        SetPage(NearestPage(1));
                    }
                    else
                    {
                        SetPage(NearestPage(-1));
                    }
                }
            }
        }

        private int NearestPage(int direction = 0)
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

        public void TabPageDrag()
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

                float normalizedPagesContainerPosition = -m_PagesContainer.anchoredPosition.x / (m_PageSize.x * m_Pages.Length);
                if (m_ForceSameTabSize)
                {
                    m_Indicator.anchoredPosition = new Vector2((m_TabWidth * m_Tabs.Length) * normalizedPagesContainerPosition, 0);
                }
                else
                {
                    m_Indicator.anchoredPosition = new Vector2(rectTransform.GetProperSize().x * normalizedPagesContainerPosition, 0);
                }
            }
        }

        #endregion

        #region Receivers

        private void OnPagesTweenEnd()
        {
            for (int i = 0; i < m_Pages.Length; i++)
            {
                if (i == m_CurrentPage)
                //if (i >= m_CurrentPage - 1 && i <= m_CurrentPage + 1)
                {
                    m_Pages[i].gameObject.SetActive(true);
                    m_Pages[i].CallOnShow();
                }
                else
                {
                    m_Pages[i].DisableIfAllowed();
                }
            }
        }

        #endregion

        #region Editor Functions

#if UNITY_EDITOR
        public void SetPagesDirty()
        {
            m_PagesDirty = true;
        }

        public void TrackPages()
        {
            if (IsDestroyed() || Application.isPlaying) return;

            if (m_AutoTrackPages && enabled && gameObject.activeInHierarchy)
            {
                TabPage[] tempPages = GetComponentsInChildren<TabPage>(true);

                List<TabPage> ownedTempPages = new List<TabPage>();

                for (int i = 0; i < tempPages.Length; i++)
                {
                    if (tempPages[i].transform.parent.parent.parent == transform)
                    {
                        ownedTempPages.Add(tempPages[i]);
                    }
                }

                m_Pages = new TabPage[ownedTempPages.Count];

                for (int i = 0; i < ownedTempPages.Count; i++)
                {
                    m_Pages[i] = ownedTempPages[i];
                }
            }

            if (m_OldSelectionObjects != Selection.activeGameObject)
            {
                m_OldSelectionObjects = Selection.activeGameObject;
                m_PagesDirty = true;
            }

            if (m_Pages.Length > 0 && m_PagesDirty)
            {
                m_PagesDirty = false;

                bool pageSelected = false;

                if (m_OnlyShowSelectedPage)
                {
                    for (int i = 0; i < m_Pages.Length; i++)
                    {
                        if (m_Pages[i] == null) continue;

                        RectTransform[] children = m_Pages[i].GetComponentsInChildren<RectTransform>(true);

                        bool objectSelected = false;

                        for (int j = 0; j < children.Length; j++)
                        {
                            if (Selection.Contains(children[j].gameObject))
                            {
                                if (!m_Pages[i].gameObject.activeSelf)
                                {
                                    m_Pages[i].gameObject.SetActive(true);
                                }
                                pageSelected = true;
                                objectSelected = true;
                            }
                        }
                        if (!objectSelected)
                        {
                            if (m_Pages[i].gameObject.activeSelf)
                            {
                                m_Pages[i].gameObject.SetActive(false);
                            }
                        }
                    }

                    if (!pageSelected && !m_Pages[m_CurrentPage].gameObject.activeSelf)
                    {
                        if (!m_Pages[m_CurrentPage].gameObject.activeSelf)
                        {
                            m_Pages[m_CurrentPage].gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
#endif

        #endregion
    }
}