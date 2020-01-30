//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Tab Item", 100)]
    public class TabItem : ToggleBase, IPointerDownHandler
    {
        #region Private Variables

        [SerializeField, SerializeStyleProperty]
        protected bool m_ChangeIconColor = false;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_ItemIcon = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_ItemIndex = null;
        [SerializeField]
        protected BaseTabView m_TabView = null;

        private int m_Id;

        #endregion

        #region Constructor

        public TabItem()
        {
            m_ChangeGraphicColor = false;
            m_ChangeRippleColor = false;
        }

        #endregion

        #region Public Properties

        public bool changeIconColor
        {
            get { return m_ChangeIconColor; }
            set { m_ChangeIconColor = value; }
        }

        public override Toggle toggle
        {
            get
            {
                if (!m_Toggle)
                {
                    m_Toggle = gameObject.GetAddComponent<Toggle>();
                    if (m_Toggle != null)
                        RegisterEvents();
                }
                return m_Toggle;
            }
            set
            {
                if (m_Toggle == value)
                    return;

                UnregisterEvents();
                m_Toggle = value;
                RegisterEvents();
            }
        }

        public Graphic itemIcon
        {
            get { return m_ItemIcon; }
            set { m_ItemIcon = value; }
        }

        public BaseTabView tabView
        {
            get
            {
                if (m_TabView == null)
                {
                    m_TabView = GetComponentInParent<TabView>();
                }
                return m_TabView;
            }
            set { m_TabView = value; }
        }
        
        public int id
        {
            get { return m_Id; }
            set
            {
                m_Id = value;
                if (m_ItemIndex != null)
                    m_ItemIndex.SetGraphicText((m_Id + 1).ToString());
            }
        }
        
        public RectTransform rectTransform
        {
            get
            {
                return transform as RectTransform;
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = gameObject.GetComponent<CanvasGroup>();
                }
                return m_CanvasGroup;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            toggle.enabled = !isOn;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (interactable)
            {
                if(tabView)
                    tabView.TabItemPointerDown(id);
            }
        }

        #endregion

        #region Helper Functions

        protected override void HandleOnToggleChanged(bool value)
        {
            if(tabView != null && tabView.currentPage != id && value)
                tabView.SetPage(id);
            base.HandleOnToggleChanged(value);

            toggle.enabled = !value;
        }

        public virtual void SetupGraphic(ImageDataType type)
        {
            var itemIconName = m_ItemIcon != null ? m_ItemIcon.name : "Icon";
            //if (gameObject.GetChildByName<Image>(itemIconName) == null || gameObject.GetChildByName<IVectorImage>(itemIconName) == null) return;

            Graphic otherIcon = null;
            if (type == ImageDataType.Sprite)
            {
                m_ItemIcon = gameObject.GetChildByName<Image>(itemIconName);
                otherIcon = gameObject.GetChildByName<IVectorImage>(itemIconName) as Graphic;
            }
            else
            {
                m_ItemIcon = gameObject.GetChildByName<IVectorImage>(itemIconName) as Graphic;
                otherIcon = gameObject.GetChildByName<Image>(itemIconName);
            }

            if (m_ItemIcon != null)
                m_ItemIcon.gameObject.SetActive(true);
            if (otherIcon != null)
                otherIcon.gameObject.SetActive(false);
        }

        #endregion

        #region Unity Functions

#if UNITY_EDITOR

        protected override void OnValidateDelayed()
        {
            base.OnValidateDelayed();

            if (m_ChangeIconColor)
            {
                if (m_Interactable)
                {
                    if (m_ItemIcon != null)
                        m_ItemIcon.color = isOn ? m_OnColor : m_OffColor;
                }
                else
                {
                    if (m_ItemIcon != null)
                        m_ItemIcon.color = m_DisabledColor;
                }
            }
        }
#endif

        #endregion

        #region Other Functions

        protected override void ApplyCanvasGroupChanged()
        {
            bool interactable = m_Interactable;
            if (interactable)
            {
                var allCanvas = GetComponentsInParent<CanvasGroup>();
                for (int i = 0; i < allCanvas.Length; i++)
                {
                    var canvas = allCanvas[i];

                    interactable = interactable && canvas.interactable;
                    if (!interactable || canvas.ignoreParentGroups)
                        break;
                }
            }

            canvasGroup.blocksRaycasts = m_Interactable;
            canvasGroup.interactable = m_Interactable;
            ApplyCanvasGroupAlpha(false);
        }

        protected void ApplyCanvasGroupAlpha(bool animate)
        {
            if (Application.isPlaying)
            {
                var targetAlpha = (m_TabView != null && !m_ChangeIconColor && !m_ChangeGraphicColor && !isOn) ? (interactable ? 0.5f : 0.15f) : (interactable ? 1f : 0.5f);
                if (animate)
                    canvasGroup.alpha = Tween.QuintOut(canvasGroup.alpha, targetAlpha, m_AnimDeltaTime, m_AnimationDuration);
                else
                    canvasGroup.alpha = targetAlpha;
            }
            else
            {
                canvasGroup.alpha = interactable ? 1f : 0.5f;
            }
        }

        public override void TurnOn()
        {
            if (m_ChangeIconColor)
            {
                if (m_ItemIcon != null)
                    m_CurrentColor = m_ItemIcon.color;
            }
            ApplyCanvasGroupAlpha(false);
            base.TurnOn();
        }

        public override void TurnOnInstant()
        {
            base.TurnOnInstant();

            if (m_ChangeIconColor)
            {
                if (interactable)
                {
                    if (m_ItemIcon != null)
                        m_ItemIcon.color = m_OnColor;
                }
            }
            ApplyCanvasGroupAlpha(false);
        }

        public override void TurnOff()
        {
            if (m_ChangeIconColor)
            {
                if (m_ItemIcon != null)
                    m_CurrentColor = m_ItemIcon.color;
            }

            ApplyCanvasGroupAlpha(false);
            base.TurnOff();
        }

        public override void TurnOffInstant()
        {
            base.TurnOffInstant();

            if (m_ChangeIconColor)
            {
                if (interactable)
                {
                    if (m_ItemIcon != null)
                        m_ItemIcon.color = m_OffColor;
                }
            }
            ApplyCanvasGroupAlpha(false);
        }

        protected override void ApplyInteractableOn()
        {
            if (m_ChangeIconColor)
            {
                if (isOn)
                {
                    if (m_ItemIcon != null)
                        m_ItemIcon.color = m_OnColor;
                }
                else
                {
                    if (m_ItemIcon != null)
                        m_ItemIcon.color = m_OffColor;
                }
            }
            
            base.ApplyInteractableOn();
        }

        protected override void ApplyInteractableOff()
        {
            if (m_ChangeIconColor)
            {
                if (m_ItemIcon != null)
                    m_ItemIcon.color = m_DisabledColor;
            }

            base.ApplyInteractableOff();
        }

        protected override void AnimOn()
        {
            base.AnimOn();

            if (m_ChangeIconColor)
            {
                if (m_ItemIcon != null)
                    m_ItemIcon.color = Tween.QuintOut(m_CurrentColor, m_OnColor, m_AnimDeltaTime, m_AnimationDuration);
            }
        }

        protected override void AnimOnComplete()
        {
            base.AnimOnComplete();

            if (m_ChangeIconColor)
            {
                if (m_ItemIcon != null)
                    m_ItemIcon.color = m_OnColor;
            }
        }

        protected override void AnimOff()
        {
            base.AnimOff();

            if(m_ChangeIconColor && m_ItemIcon != null)
                m_ItemIcon.color = Tween.QuintOut(m_CurrentColor, m_OffColor, m_AnimDeltaTime, m_AnimationDuration);
        }

        protected override void AnimOffComplete()
        {
            base.AnimOffComplete();

            if (m_ChangeIconColor && m_ItemIcon != null)
                m_ItemIcon.color = m_OffColor;
        }

        #endregion
    }
}