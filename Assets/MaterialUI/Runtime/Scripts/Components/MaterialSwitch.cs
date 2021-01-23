﻿//  Copyright 2017 MaterialUI for Unity http://materialunity.com
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
        private RectTransform m_SwitchRectTransform;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_SwitchImage;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_BackImage;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_ShadowImage;
        [SerializeField]
        private RectTransform m_Trail;

        [SerializeField, SerializeStyleProperty]
        private bool m_SlideSwitch = true;
        [SerializeField]
        Slider.Direction m_SlideDirection = Slider.Direction.LeftToRight;

        

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

        public RectTransform trail
        {
            get { return m_Trail == null && m_BackImage != null? m_BackImage.rectTransform : m_Trail; }
            set { m_Trail = value; }
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

        public Slider.Direction slideDirection
        {
            get { return m_SlideDirection; }
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

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            if(enabled && gameObject.activeInHierarchy)
                UpdateColorToggleState(false);
        }

        #endregion

        #region Other Functions

        public override void TurnOn()
        {
            if (switchImage) 
                m_CurrentColor = switchImage.color;
            m_CurrentBackColor = backImage.color;

            base.TurnOn();
        }

        public override void TurnOff()
        {
            if(switchImage) 
                m_CurrentColor = switchImage.color;
            m_CurrentBackColor = backImage.color;

            base.TurnOff();
        }

        protected override void ApplyInteractableOn()
        {
            if(shadowImage != null)
                shadowImage.enabled = true;

            base.ApplyInteractableOn();
        }

        protected override void ApplyInteractableOff()
        {
            if (shadowImage != null)
                shadowImage.enabled = false;

            base.ApplyInteractableOff();
        }

        protected override void AnimOn()
        {
            base.AnimOn();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();

            if (switchImage) 
                switchImage.color = Tween.QuintOut(m_CurrentColor, !canUseDisabledColor || isInteractable ? m_OnColor : m_DisabledColor, m_AnimDeltaTime, m_AnimationDuration);
            if (backImage) 
                backImage.color = Tween.QuintOut(m_CurrentBackColor, !canUseDisabledColor || isInteractable ? backOnColor : backDisabledColor, m_AnimDeltaTime, m_AnimationDuration);

            if (slideSwitch)
            {
                switchRectTransform.anchoredPosition = Tween.SeptSoftOut(switchRectTransform.anchoredPosition, GetSlideAnchoredPosition(true), m_AnimDeltaTime, m_AnimationDuration);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(switchRectTransform);
#endif
            }
        }

        protected override void AnimOff()
        {
            base.AnimOff();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();

            if (switchImage) 
                switchImage.color = Tween.QuintOut(m_CurrentColor, !canUseDisabledColor || isInteractable ? m_OffColor : m_DisabledColor, m_AnimDeltaTime, m_AnimationDuration);
            if (backImage) 
                backImage.color = Tween.QuintOut(m_CurrentBackColor, !canUseDisabledColor || isInteractable ? backOffColor : backDisabledColor, m_AnimDeltaTime, m_AnimationDuration);

            if (slideSwitch)
            {
                switchRectTransform.anchoredPosition = Tween.SeptSoftOut(switchRectTransform.anchoredPosition, GetSlideAnchoredPosition(false), m_AnimDeltaTime, m_AnimationDuration);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(switchRectTransform);
#endif
            }
        }

        protected override void SetOnColor()
        {
            base.SetOnColor();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();

            if (switchImage)
                switchImage.color = !canUseDisabledColor || isInteractable ? m_OnColor : m_DisabledColor;
            if (backImage)
                backImage.color = !canUseDisabledColor || isInteractable ? backOnColor : backDisabledColor;

            if (slideSwitch)
            {
                switchRectTransform.anchoredPosition = GetSlideAnchoredPosition(true);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(switchRectTransform);
#endif
            }
        }

        protected override void SetOffColor()
        {
            base.SetOffColor();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();

            if (switchImage)
                switchImage.color = !canUseDisabledColor || isInteractable ? m_OffColor : m_DisabledColor;
            if (backImage)
                backImage.color = !canUseDisabledColor || isInteractable ? backOffColor : backDisabledColor;

            if (slideSwitch)
            {
                switchRectTransform.anchoredPosition = GetSlideAnchoredPosition(false);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(switchRectTransform);
#endif
            }
        }

        protected Vector2 GetSlideAnchoredPosition(bool isOn)
        {
            if (trail == null)
                return Vector2.zero;
            else if ((m_SlideDirection == Slider.Direction.LeftToRight && isOn) || (m_SlideDirection == Slider.Direction.RightToLeft && !isOn))
                return new Vector2(trail.rect.xMax, 0);
            else if ((m_SlideDirection == Slider.Direction.RightToLeft && isOn) || (m_SlideDirection == Slider.Direction.LeftToRight && !isOn))
                return new Vector2(trail.rect.xMin, 0);
            else if ((m_SlideDirection == Slider.Direction.BottomToTop && isOn) || (m_SlideDirection == Slider.Direction.TopToBottom && !isOn))
                return new Vector2(0, trail.rect.yMax);
            else if ((m_SlideDirection == Slider.Direction.TopToBottom && isOn) || (m_SlideDirection == Slider.Direction.BottomToTop && !isOn))
                return new Vector2(0, trail.rect.yMin);

            return Vector2.zero;
        }

        #endregion
    }
}
