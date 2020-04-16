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
    [AddComponentMenu("MaterialUI/TabView", 100)]
    public class TabView : BaseTabView
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
        bool m_UseLegacyControlMode = true;
        [SerializeField]
        bool m_ForceSameTabSize = true;
        [Space]
        [SerializeField]
        private List<TabPage> m_Pages = null;

        protected float m_TabWidth;
        protected float m_TabPadding = 12;

        protected bool? _cachedTabChildForceExpand = null;
        protected float? _cachedTabSpacing = null;
#if UNITY_EDITOR
        protected bool? _lastForceSameTabSize = false;
#endif

        #endregion

        #region Public Properties

        public bool useLegacyControlMode
        {
            get
            {
                return m_UseLegacyControlMode;
            }
            set
            {
                if (m_UseLegacyControlMode == value)
                    return;
                m_UseLegacyControlMode = value;
                if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
                    InitializeTabsAndPagesDelayed();
            }
        }

        public override List<TabPage> pages
        {
            get
            {
                if (m_Pages == null)
                    m_Pages = new List<TabPage>();
                return m_Pages;
            }
            set { m_Pages = value; }
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

        public bool forceSameTabSize
        {
            get
            {
                return m_ForceSameTabSize;
            }

            set
            {
                if (m_ForceSameTabSize == value)
                    return;
                m_ForceSameTabSize = value;
                if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
                    InitializeTabsAndPagesDelayed();
            }
        }

        #endregion

        #region Unity Functions

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (enabled && gameObject.activeInHierarchy)
            {
                if (!Application.isPlaying)
                    Invoke("TrackPages", 0);
                else if (_lastForceSameTabSize == null || _lastForceSameTabSize != m_ForceSameTabSize)
                {
                    if (_lastForceSameTabSize != null)
                        InitializeTabsAndPagesDelayed();
                    _lastForceSameTabSize = m_ForceSameTabSize;
                }
            }
        }
#endif

        #endregion

        #region Initialize Methods

        public override void InitializeTabs()
        {
            if (this == null || m_TabsContainer == null || !Application.isPlaying)
                return;

            var contentSizeFitter = m_TabsContainer.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
                contentSizeFitter.enabled = true;

            //Initialize TabContainer
            var layoutGroup = m_TabsContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layoutGroup != null && m_UseLegacyControlMode)
            {
                if (_cachedTabChildForceExpand == null)
                    _cachedTabChildForceExpand = layoutGroup.childForceExpandWidth;
                if (_cachedTabSpacing == null)
                    _cachedTabSpacing = layoutGroup.spacing;
                layoutGroup.childForceExpandWidth = m_ForceSameTabSize ? true : _cachedTabChildForceExpand.Value;
                layoutGroup.spacing = m_ForceSameTabSize ? 0 : _cachedTabSpacing.Value;
            }

            m_TabsContainer.anchorMin = Vector2.zero;
            m_TabsContainer.anchorMax = new Vector2(0, 1);

            SetupTabSize();
            InstantiateTabsFromTemplate();

            float barWidth = m_ForceSameTabSize ? (m_TabWidth * m_Pages.Count()) :
                ((m_TabWidth + (layoutGroup != null ? layoutGroup.spacing : 0)) * m_Pages.Count()) + layoutGroup.padding.horizontal;

            var barParent = m_TabsContainer.parent as RectTransform;
            //Setup Size
            var tabContainerLayoutElement = m_TabsContainer.GetComponent<LayoutElement>();
            if (tabContainerLayoutElement)
                tabContainerLayoutElement.minWidth = barParent.GetProperSize().x;//Mathf.Max(barWidth, tabContainerLayoutElement.minWidth, barParent != null? barParent.GetProperSize().x : 0);
            m_TabsContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barWidth);

            //Configure Overscroll
            OverscrollConfig overscrollConfig = m_TabsContainer.parent.GetComponent<OverscrollConfig>();
            if (overscrollConfig != null)
                overscrollConfig.Setup();

            //Fix Indicator size in next cycle
            InitializeIndicatorDelayed();
        }

        public override void InitializeIndicator()
        {
            if (this == null || m_Indicator == null || m_Tabs == null || !Application.isPlaying)
                return;

            m_Indicator.anchorMin = new Vector2(0, 0);
            m_Indicator.anchorMax = new Vector2(0, 0);

            var currentPage = m_Tabs != null && m_CurrentPage >= 0 && m_CurrentPage < m_Tabs.Length ? m_Tabs[m_CurrentPage] : null;
            m_Indicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_ForceSameTabSize || currentPage == null ? m_TabWidth : m_Tabs[m_CurrentPage].rectTransform.GetProperSize().x);

            if (m_Indicator != null && m_Indicator.transform.parent == m_TabItemTemplate.transform.parent)
                m_Indicator.transform.SetAsLastSibling();

            TweenIndicator(m_CurrentPage, false);
        }

        protected override void SetupTabSize()
        {
            if (this == null || m_TabItemTemplate == null || m_Tabs == null || !Application.isPlaying)
            {
                if (m_TabItemTemplate != null)
                    m_TabItemTemplate.gameObject.SetActive(false);
                m_TabWidth = -1;
                return;
            }

            if (m_ForceSameTabSize)
            {
                float barWidth = rectTransform.GetProperSize().x;

                m_TabWidth = GetMaxTabTextWidth() + (2 * m_TabPadding);
                float combinedWidth = m_TabWidth * m_Pages.Count;
                m_TabItemTemplate.GetComponent<LayoutElement>().minWidth = 72;

                var v_minDelta = 16f;
                if (combinedWidth - barWidth < v_minDelta)
                {
                    m_TabWidth = barWidth / m_Pages.Count;
                }

                m_TabWidth = Mathf.Max(m_TabWidth, LayoutUtility.GetPreferredWidth(m_TabItemTemplate.rectTransform), m_TabItemTemplate.GetComponent<LayoutElement>().minWidth);
            }
            else
            {
                m_TabWidth = -1;
            }
            m_TabItemTemplate.GetComponent<LayoutElement>().preferredWidth = m_TabWidth;
            m_TabItemTemplate.gameObject.SetActive(false);
        }

        #endregion

        #region Helper Functions

        protected virtual float GetMaxTabTextWidth()
        {
            float longestTextWidth = 0;

            if (m_TabItemTemplate.graphic != null)
            {
                var changed = false;
                var originalText = m_TabItemTemplate.graphic.GetGraphicText();
                for (int i = 0; i < m_Pages.Count; i++)
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
                if (changed)
                    m_TabItemTemplate.graphic.SetGraphicText(originalText);
            }

            return longestTextWidth;
        }

        protected override void TweenTabsContainer(int index, bool animate = true)
        {
            if (m_TabsContainer == null || m_Tabs == null)
                return;

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

        protected override void TweenIndicator(int targetTab, bool animate = true)
        {
            if (m_Indicator == null || m_Tabs == null)
                return;

            float targetPosition = m_ForceSameTabSize || m_Tabs.Length <= targetTab || targetTab < 0 || m_Tabs[targetTab] == null ?
                targetTab * m_TabWidth :
                m_Tabs[targetTab].rectTransform.anchoredPosition.x - (m_Tabs[targetTab].rectTransform.GetProperSize().x / 2);

            float targetSize = m_ForceSameTabSize || m_Tabs.Length <= targetTab || targetTab < 0 || m_Tabs[targetTab] == null ?
                m_TabWidth :
                m_Tabs[targetTab].rectTransform.GetProperSize().x;

            TweenManager.EndTween(m_IndicatorSizeTweener);
            TweenManager.EndTween(m_IndicatorTweener);

            if (animate)
            {
                if (!m_ForceSameTabSize)
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

        public override void TabPageDrag()
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
                    float normalizedPagesContainerPosition = -m_PagesContainer.anchoredPosition.x / (m_PageSize.x * m_Pages.Count);
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

            if (m_PagesContainer != null && m_AutoTrackPages && enabled && gameObject.activeInHierarchy)
            {
                TabPage[] tempPages = m_PagesContainer.GetComponentsInChildren<TabPage>(true);

                List<TabPage> ownedTempPages = new List<TabPage>();

                var trackedPagesDirty = false;
                for (int i = 0; i < tempPages.Length; i++)
                {
                    if (tempPages[i].transform.parent == m_PagesContainer.transform)
                    {
                        trackedPagesDirty = trackedPagesDirty || !m_Pages.Contains(tempPages[i]);
                        ownedTempPages.Add(tempPages[i]);
                    }
                }

                if (trackedPagesDirty || m_Pages.Count != ownedTempPages.Count)
                {
                    m_Pages = new List<TabPage>();

                    for (int i = 0; i < ownedTempPages.Count; i++)
                    {
                        m_Pages.Add(ownedTempPages[i]);
                    }
                    EditorUtility.SetDirty(this);
                }
            }

            if (m_OldSelectionObjects != Selection.activeGameObject)
            {
                m_OldSelectionObjects = Selection.activeGameObject;
                m_PagesDirty = true;
            }

            if (m_Pages.Count > 0 && m_PagesDirty)
            {
                m_PagesDirty = false;

                bool pageSelected = false;

                if (m_OnlyShowSelectedPage)
                {
                    for (int i = 0; i < m_Pages.Count; i++)
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