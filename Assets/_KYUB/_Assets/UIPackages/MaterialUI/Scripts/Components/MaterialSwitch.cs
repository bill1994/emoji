//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    //[ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Material Switch", 100)]
    public class MaterialSwitch : ToggleBase
    {
        #region Private Variables

        [SerializeField, SerializeStyleProperty]
        private Color m_BackOnColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        private Color m_BackOffColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        private Color m_BackDisabledColor = Color.black;

        [SerializeField, SerializeStyleProperty]
        private Graphic m_SwitchImage;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_BackImage;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_ShadowImage;

        [SerializeField, SerializeStyleProperty]
        private bool m_SlideSwitch = true;

        private RectTransform m_SwitchRectTransform;

        private float m_CurrentSwitchPosition;
        private Color m_CurrentBackColor;

        #endregion

        #region Properties

        public Color backOnColor
        {
            get { return m_BackOnColor; }
            set { m_BackOnColor = value; }
        }

        public Color backOffColor
        {
            get { return m_BackOffColor; }
            set { m_BackOffColor = value; }
        }

        public Color backDisabledColor
        {
            get { return m_BackDisabledColor; }
            set { m_BackDisabledColor = value; }
        }
        public Graphic switchImage
        {
            get { return m_SwitchImage; }
            set { m_SwitchImage = value; }
        }

        public Graphic backImage
        {
            get { return m_BackImage; }
            set { m_BackImage = value; }
        }

        public Graphic shadowImage
        {
            get { return m_ShadowImage; }
            set { m_ShadowImage = value; }
        }

        public bool slideSwitch
        {
            get { return m_SlideSwitch; }
        }

        public RectTransform switchRectTransform
        {
            get
            {
                if (m_SwitchRectTransform == null)
                {
                    if (m_SwitchImage != null)
                    {
                        m_SwitchRectTransform = (RectTransform)m_SwitchImage.transform;
                    }
                }
                return m_SwitchRectTransform;
            }
        }

        #endregion

        #region Unity Functions

#if UNITY_EDITOR

        protected override void OnValidateDelayed()
        {
            base.OnValidateDelayed();

            if (m_Interactable)
            {
                if (m_SwitchImage) m_SwitchImage.color = isOn ? m_OnColor : m_OffColor;
                if (m_BackImage) m_BackImage.color = isOn ? m_BackOnColor : m_BackOffColor;
            }
            else
            {
                if (m_SwitchImage) m_SwitchImage.color = m_DisabledColor;
                if (m_BackImage) m_BackImage.color = m_BackDisabledColor;
            }
        }
#endif

        #endregion

        #region Other Functions

        public override void TurnOn()
        {
            m_CurrentSwitchPosition = switchRectTransform.anchoredPosition.x;
            if (switchImage) m_CurrentColor = switchImage.color;
            m_CurrentBackColor = backImage.color;

            base.TurnOn();
        }

        public override void TurnOnInstant()
        {
            base.TurnOnInstant();

            if (interactable)
            {
                if (switchImage) switchImage.color = m_OnColor;
                if (backImage) backImage.color = backOnColor;
            }

            if(slideSwitch) switchRectTransform.anchoredPosition = new Vector2(8f, 0f);
        }

        public override void TurnOff()
        {
            m_CurrentSwitchPosition = switchRectTransform.anchoredPosition.x;
            if(switchImage) m_CurrentColor = switchImage.color;
            m_CurrentBackColor = backImage.color;

            base.TurnOff();
        }

        public override void TurnOffInstant()
        {
            base.TurnOffInstant();

            if (interactable)
            {
                if (switchImage) switchImage.color = m_OffColor;
                if (backImage) backImage.color = backOffColor;
            }

            if (slideSwitch) switchRectTransform.anchoredPosition = new Vector2(-8f, 0f);
        }

        protected override void ApplyInteractableOn()
        {
            if (isOn)
            {
                if (switchImage) switchImage.color = m_OnColor;
                if (backImage) backImage.color = backOnColor;
            }
            else
            {
                if (switchImage) switchImage.color = m_OffColor;
                if (backImage) backImage.color = backOffColor;
            }

            shadowImage.enabled = true;

            base.ApplyInteractableOn();
        }

        protected override void ApplyInteractableOff()
        {
            if(switchImage) switchImage.color = m_DisabledColor;
            if(backImage) backImage.color = backDisabledColor;

            shadowImage.enabled = false;

            base.ApplyInteractableOff();
        }

        protected override void AnimOn()
        {
            base.AnimOn();

            if (switchImage) switchImage.color = Tween.QuintOut(m_CurrentColor, m_OnColor, m_AnimDeltaTime, m_AnimationDuration);
            if (backImage) backImage.color = Tween.QuintOut(m_CurrentBackColor, backOnColor, m_AnimDeltaTime, m_AnimationDuration);

            if (slideSwitch) switchRectTransform.anchoredPosition = Tween.SeptSoftOut(new Vector2(m_CurrentSwitchPosition, 0f), new Vector2(8f, 0f), m_AnimDeltaTime, m_AnimationDuration);
        }

        protected override void AnimOnComplete()
        {
            base.AnimOnComplete();

            if (switchImage) switchImage.color = m_OnColor;
            if (backImage) backImage.color = backOnColor;

            if (slideSwitch) switchRectTransform.anchoredPosition = new Vector2(8f, 0f);
        }

        protected override void AnimOff()
        {
            base.AnimOff();

            switchImage.color = Tween.QuintOut(m_CurrentColor, m_OffColor, m_AnimDeltaTime, m_AnimationDuration);
            backImage.color = Tween.QuintOut(m_CurrentBackColor, backOffColor, m_AnimDeltaTime, m_AnimationDuration);

            if (slideSwitch) switchRectTransform.anchoredPosition = Tween.SeptSoftOut(new Vector2(m_CurrentSwitchPosition, 0f), new Vector2(-8f, 0f), m_AnimDeltaTime, m_AnimationDuration);
        }

        protected override void AnimOffComplete()
        {
            base.AnimOffComplete();

            switchImage.color = m_OffColor;
            backImage.color = backOffColor;

            if (slideSwitch) switchRectTransform.anchoredPosition = new Vector2(-8f, 0f);
        }

        #endregion
    }
}
