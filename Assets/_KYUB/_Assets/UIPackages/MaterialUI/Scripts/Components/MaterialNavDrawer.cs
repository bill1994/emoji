//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Material Nav Drawer", 100)]
    public class MaterialNavDrawer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Private Variables

        [SerializeField]
        protected bool m_UseFocusGroup = true;
        [SerializeField]
        protected bool m_leftToRight = true;
        [SerializeField]
        protected Image m_BackgroundImage = null;
        [SerializeField]
        protected Image m_ShadowImage = null;
        [SerializeField]
        protected GameObject m_PanelLayer = null;
        [SerializeField]
        protected bool m_DarkenBackground = true;
        [SerializeField]
        protected float m_DragDeltaTolerance = 0.5f;
        [SerializeField]
        protected bool m_DragToOpenOrClose = true;
        [SerializeField]
        protected bool m_TapBackgroundToClose = true;
        [SerializeField]
        protected bool m_OpenOnStart = false;
        [SerializeField]
        protected float m_AnimationDuration = 0.5f;

        protected Canvas _RootCanvas;

        protected float m_MaxPosition;
        protected float m_MinPosition;

        protected RectTransform m_RectTransform;
        protected GameObject m_BackgroundGameObject;
        protected RectTransform m_BackgroundRectTransform;
        protected CanvasGroup m_BackgroundCanvasGroup;
        protected GameObject m_ShadowGameObject;
        protected CanvasGroup m_ShadowCanvasGroup;

        protected byte m_AnimState;
        protected float m_AnimStartTime;
        protected float m_AnimDeltaTime;

        protected Vector2 m_CurrentPos;
        protected float m_CurrentBackgroundAlpha;
        protected float m_CurrentShadowAlpha;
        protected Vector2 m_TempVector2;

        #endregion

        #region Callbacks

        public UnityEvent OnOpenEnd;
        public UnityEvent OnCloseEnd;

        #endregion

        #region Public Properties

        public bool useFocusGroup
        {
            get { return m_UseFocusGroup; }
            set { m_UseFocusGroup = value; }
        }

        public bool leftToRight
        {
            get { return m_leftToRight; }
            set { m_leftToRight = value; }
        }

        public Image backgroundImage
        {
            get { return m_BackgroundImage; }
            set { m_BackgroundImage = value; }
        }

        public Image shadowImage
        {
            get { return m_ShadowImage; }
            set { m_ShadowImage = value; }
        }

        public GameObject panelLayer
        {
            get { return m_PanelLayer; }
            set { m_PanelLayer = value; }
        }

        public bool darkenBackground
        {
            get { return m_DarkenBackground; }
            set { m_DarkenBackground = value; }
        }

        public float dragDeltaTolerance
        {
            get { return m_DragDeltaTolerance; }
            set { m_DragDeltaTolerance = value; }
        }

        public bool dragToOpenOrClose
        {
            get { return m_DragToOpenOrClose; }
            set { m_DragToOpenOrClose = value; }
        }

        public bool tapBackgroundToClose
        {
            get { return m_TapBackgroundToClose; }
            set { m_TapBackgroundToClose = value; }
        }

        public bool openOnStart
        {
            get { return m_OpenOnStart; }
            set { m_OpenOnStart = value; }
        }

        public float animationDuration
        {
            get { return m_AnimationDuration; }
            set { m_AnimationDuration = value; }
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

        protected virtual void Awake()
        {
            m_RectTransform = gameObject.GetComponent<RectTransform>();
            m_BackgroundRectTransform = m_BackgroundImage.GetComponent<RectTransform>();
            m_BackgroundCanvasGroup = m_BackgroundImage.GetComponent<CanvasGroup>();
            m_ShadowCanvasGroup = m_ShadowImage.GetComponent<CanvasGroup>();
        }

        protected virtual void Start()
        {
            m_MaxPosition = (m_leftToRight ? 1 : -1) * m_RectTransform.rect.width / 2;
            m_MinPosition = -m_MaxPosition;

            RefreshBackgroundSize();

            m_BackgroundGameObject = m_BackgroundImage.gameObject;
            m_ShadowGameObject = m_ShadowImage.gameObject;

            if (m_OpenOnStart)
            {
                Open();
            }
            else
            {
                m_BackgroundGameObject.SetActive(false);
                m_ShadowGameObject.SetActive(false);
                m_PanelLayer.SetActive(false);
            }
        }

        protected virtual void Update()
        {
            if (m_AnimState > 0)
            {
                if (m_AnimStartTime < 0)
                    m_AnimStartTime = Time.realtimeSinceStartup;
                Kyub.Performance.SustainedPerformanceManager.Refresh(this);

            }

            if (m_AnimState == 1)
            {
                m_AnimDeltaTime = Time.realtimeSinceStartup - m_AnimStartTime;

                if (m_AnimDeltaTime <= m_AnimationDuration)
                {
                    m_RectTransform.anchoredPosition = Tween.QuintOut(m_CurrentPos, new Vector2(m_MaxPosition, m_RectTransform.anchoredPosition.y), m_AnimDeltaTime, m_AnimationDuration);

                    if (m_DarkenBackground)
                    {
                        m_BackgroundCanvasGroup.alpha = Tween.QuintOut(m_CurrentBackgroundAlpha, 1f, m_AnimDeltaTime, m_AnimationDuration);
                    }

                    m_ShadowCanvasGroup.alpha = Tween.QuintIn(m_CurrentShadowAlpha, 1f, m_AnimDeltaTime, m_AnimationDuration / 2f);
                }
                else
                {
                    m_RectTransform.anchoredPosition = new Vector2(m_MaxPosition, m_RectTransform.anchoredPosition.y);
                    if (m_DarkenBackground)
                    {
                        m_BackgroundCanvasGroup.alpha = 1f;
                    }
                    m_AnimState = 0;
                    HandleOnOpenEnd();
                }
            }
            else if (m_AnimState == 2)
            {
                m_AnimDeltaTime = Time.realtimeSinceStartup - m_AnimStartTime;

                if (m_AnimDeltaTime <= m_AnimationDuration)
                {
                    m_RectTransform.anchoredPosition = Tween.QuintOut(m_CurrentPos, new Vector2(m_MinPosition, m_RectTransform.anchoredPosition.y), m_AnimDeltaTime, m_AnimationDuration);

                    if (m_DarkenBackground)
                    {
                        m_BackgroundCanvasGroup.alpha = Tween.QuintOut(m_CurrentBackgroundAlpha, 0f, m_AnimDeltaTime, m_AnimationDuration);
                    }

                    m_ShadowCanvasGroup.alpha = Tween.QuintIn(m_CurrentShadowAlpha, 0f, m_AnimDeltaTime, m_AnimationDuration);
                }
                else
                {
                    m_RectTransform.anchoredPosition = new Vector2(m_MinPosition, m_RectTransform.anchoredPosition.y);
                    if (m_DarkenBackground)
                    {
                        m_BackgroundCanvasGroup.alpha = 0f;
                    }

                    m_BackgroundGameObject.SetActive(false);
                    m_ShadowGameObject.SetActive(false);
                    m_PanelLayer.SetActive(false);

                    m_AnimState = 0;
                    HandleOnCloseEnd();
                }
            }

            m_RectTransform.anchoredPosition = new Vector2(Mathf.Clamp(m_RectTransform.anchoredPosition.x, Mathf.Min(m_MinPosition, m_MaxPosition), Mathf.Max(m_MinPosition, m_MaxPosition)), m_RectTransform.anchoredPosition.y);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            RefreshBackgroundSize();
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);

            m_AnimState = 0;

            m_BackgroundGameObject.SetActive(true);
            m_ShadowGameObject.SetActive(true);
            m_PanelLayer.SetActive(true);
        }

        public void OnDrag(PointerEventData data)
        {
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            m_TempVector2 = m_RectTransform.anchoredPosition;
            m_TempVector2.x += data.delta.x / rootCanvas.scaleFactor;

            m_RectTransform.anchoredPosition = m_TempVector2;

            if (m_DarkenBackground)
            {
                m_BackgroundCanvasGroup.alpha = 1 - (m_MaxPosition - m_RectTransform.anchoredPosition.x) / (m_MaxPosition - m_MinPosition);
            }

            m_ShadowCanvasGroup.alpha = 1 - (m_MaxPosition - m_RectTransform.anchoredPosition.x) / ((m_MaxPosition - m_MinPosition) * 2);
        }

        public void OnEndDrag(PointerEventData data)
        {
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            if (m_DragToOpenOrClose)
            {
                if (Mathf.Abs(data.delta.x) >= m_DragDeltaTolerance)
                {
                    if (data.delta.x > m_DragDeltaTolerance)
                    {
                        if (m_leftToRight)
                            Open();
                        else
                            Close();
                    }
                    else
                    {
                        if (m_leftToRight)
                            Close();
                        else
                            Open();
                    }
                }
                else
                {
                    if ((m_RectTransform.anchoredPosition.x - m_MinPosition) >
                        (m_MaxPosition - m_RectTransform.anchoredPosition.x))
                    {
                        if (m_leftToRight)
                            Open();
                        else
                            Close();
                    }
                    else
                    {
                        if (m_leftToRight)
                            Close();
                        else
                            Open();
                    }
                }
            }
        }

        #endregion

        #region Helper Functions

        public void BackgroundTap()
        {
            if (m_TapBackgroundToClose)
            {
                Close();
            }
        }

        protected virtual void InitializeFocusGroup()
        {
            if (m_PanelLayer != null)
            {
                var v_materialKeyFocus = m_PanelLayer.GetComponent<MaterialFocusGroup>();
                if (m_UseFocusGroup && v_materialKeyFocus == null)
                {
                    v_materialKeyFocus = m_PanelLayer.AddComponent<MaterialFocusGroup>();

                    var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                    v_cancelTrigger.Name = "Escape KeyDown";
                    v_cancelTrigger.Key = KeyCode.Escape;
                    v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
#if UNITY_EDITOR
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(v_cancelTrigger.OnCallTrigger, Close);
#else
                    v_cancelTrigger.OnCallTrigger.AddListener(Close);
#endif

                    v_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_cancelTrigger };
                }

                if (v_materialKeyFocus != null)
                {
                    v_materialKeyFocus.enabled = m_UseFocusGroup;
                }
            }
        }

        public void Open()
        {
            InitializeFocusGroup();
            RefreshBackgroundSize();
            m_BackgroundGameObject.SetActive(true);
            m_ShadowGameObject.SetActive(true);
            m_PanelLayer.SetActive(true);
            m_CurrentPos = m_RectTransform.anchoredPosition;
            m_CurrentBackgroundAlpha = m_BackgroundCanvasGroup.alpha;
            m_CurrentShadowAlpha = m_ShadowCanvasGroup.alpha;
            m_BackgroundCanvasGroup.blocksRaycasts = true;
            m_AnimStartTime = -1;
            m_AnimState = 1;
        }

        public void Close()
        {
            m_CurrentPos = m_RectTransform.anchoredPosition;
            m_CurrentBackgroundAlpha = m_BackgroundCanvasGroup.alpha;
            m_CurrentShadowAlpha = m_ShadowCanvasGroup.alpha;
            m_BackgroundCanvasGroup.blocksRaycasts = false;
            m_AnimStartTime = -1;
            m_AnimState = 2;
        }

        private void RefreshBackgroundSize()
        {
            m_BackgroundRectTransform.sizeDelta = new Vector2((Screen.width / rootCanvas.scaleFactor) + 1f, m_BackgroundRectTransform.sizeDelta.y);
            //MaterialActivity.Inflate(m_BackgroundRectTransform, true);
        }

        #endregion


        #region Receivers

        protected virtual void HandleOnOpenEnd()
        {
            if (OnOpenEnd != null)
                OnOpenEnd.Invoke();
        }

        protected virtual void HandleOnCloseEnd()
        {
            if (OnCloseEnd != null)
                OnCloseEnd.Invoke();
        }

        #endregion

    }
}