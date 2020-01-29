//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Tab Page", 100)]
    public class TabPage : MonoBehaviour
    {
        #region Private Variables

        [SerializeField]
		private bool m_Interactable = true;
        [SerializeField]
        private bool m_DisableWhenNotVisible = true;
        [SerializeField]
		private bool m_ShowDisabledPanel = true;
        [SerializeField]
		private string m_TabName = null;
        [SerializeField]
		private ImageData m_TabIcon = null;
		
		private CanvasGroup m_CanvasGroup;
        private RectTransform m_RectTransform;
        private TabView m_TabView;
		private RectTransform m_DisabledPanel;
		private bool m_LastEnabled = true;

        #endregion

        #region Callbacks

        public UnityEvent OnShow = new UnityEvent();
        public UnityEvent OnHide = new UnityEvent();

        #endregion

        #region Public Properties

        public bool interactable
        {
            get { return m_Interactable; }
            set { m_Interactable = value; }
        }

        public bool disableWhenNotVisible
        {
            get { return m_DisableWhenNotVisible; }
            set { m_DisableWhenNotVisible = value; }
        }

        public bool showDisabledPanel
        {
            get { return m_ShowDisabledPanel; }
            set { m_ShowDisabledPanel = value; }
        }

        public string tabName
        {
            get { return m_TabName; }
            set
            {
                if (m_TabName == value)
                    return;
                m_TabName = value;
                if (Application.isPlaying)
                    tabView.InitializeTabsAndPagesDelayed();
            }
        }

        public ImageData tabIcon
        {
            get { return m_TabIcon; }
            set
            {
                if (m_TabIcon == value)
                    return;
                m_TabIcon = value;
                if (Application.isPlaying)
                    tabView.InitializeTabsAndPagesDelayed();
            }
        }

        private CanvasGroup canvasGroup
        {
            get
            {
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = tabView.tabs[tabView.pages.ToList().IndexOf(this)].GetAddComponent<CanvasGroup>();
                }

                return m_CanvasGroup;
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

        private TabView tabView
        {
            get
            {
                if (m_TabView == null)
                {
                    m_TabView = GetComponentInParent<TabView>();
                }
                return m_TabView;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Update()
        {
            if (m_LastEnabled != m_Interactable)
            {
                m_LastEnabled = m_Interactable;

                if (canvasGroup != null)
                {

                    if (m_LastEnabled)
                    {
                        canvasGroup.blocksRaycasts = true;
                        canvasGroup.alpha = 1f;

						if (m_DisabledPanel != null)
                        {
                            Destroy(m_DisabledPanel.gameObject);
                        }
                    }
                    else
                    {
                        canvasGroup.blocksRaycasts = false;
                        canvasGroup.alpha = 0.15f;

						if (m_ShowDisabledPanel)
						{
	                        m_DisabledPanel = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.disabledPanel, rectTransform).GetComponent<RectTransform>();
	                        m_DisabledPanel.anchoredPosition = Vector2.zero;
	                        m_DisabledPanel.sizeDelta = Vector2.zero;
	                        Color backgroundColor = tabView.GetComponent<Image>().color;
	                        m_DisabledPanel.GetComponent<Image>().color = backgroundColor;
	                        m_DisabledPanel.GetComponentInChildren<Text>().color =
	                            (backgroundColor.r + backgroundColor.g + backgroundColor.b) / 3 > 0.8f
	                                ? MaterialColor.textDark
	                                : MaterialColor.textLight;
						}
                    }
                }
            }
        }

        #endregion

        #region  Helper Functions

        public void DisableIfAllowed()
        {
            if (m_DisableWhenNotVisible)
            {
                if (gameObject.activeSelf)
                    CallOnHide();
                gameObject.SetActive(false);
            }
        }

        public void CallOnShow()
        {
            if (OnShow != null)
                OnShow.Invoke();
        }

        public void CallOnHide()
        {
            if (OnHide != null)
                OnHide.Invoke();
        }

        #endregion
    }
}