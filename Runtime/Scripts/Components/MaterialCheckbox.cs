//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    //[ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Toggles/Material Checkbox", 100)]
    public class MaterialCheckbox : ToggleBase
    {
        #region Private Variables

        [SerializeField]
        int m_AnimationSize = 24;
        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_DotImage")]
        private Graphic m_CheckImage;
        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_RingImage")]
        private Graphic m_FrameImage;

        private RectTransform m_CheckRectTransform;

        private float m_CurrentCheckSize;
        private Color m_CurrentFrameColor;

        #endregion

        #region Properties

        public Graphic checkImage
        {
            get { return m_CheckImage; }
            set { m_CheckImage = value; }
        }
        
        public int animationSize
        {
            get { return m_AnimationSize; }
            set { m_AnimationSize = value; }
        }

        public Graphic frameImage
        {
            get { return m_FrameImage; }
            set { m_FrameImage = value; }
        }

        public RectTransform checkRectTransform
        {
            get
            {
                if (m_CheckRectTransform == null)
                {
                    if (m_CheckImage != null)
                    {
                        m_CheckRectTransform = (RectTransform)m_CheckImage.transform;
                    }
                }
                return m_CheckRectTransform;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            if (checkImage)
            {
                m_CheckRectTransform = checkImage.GetComponent<RectTransform>();
            }
        }

        protected override void Start()
        {
            base.Start();
        }

        #endregion

        #region Other Functions

        public override void TurnOn()
        {
            if (checkImage != null)
            {
                m_CurrentCheckSize = checkImage.rectTransform.sizeDelta.x;
                m_CurrentColor = checkImage.color;
            }
            if (frameImage != null)
                m_CurrentFrameColor = frameImage.color;

            base.TurnOn();
        }


        public override void TurnOnInstant()
        {
            base.TurnOnInstant();
            if (m_Toggle != null)
            {
                if (m_Toggle.interactable)
                {
                    AnimOnComplete();
                }
            }

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(m_AnimationSize, m_AnimationSize);
        }

        public override void TurnOff()
        {
            if (checkImage != null)
            {
                m_CurrentCheckSize = checkImage.rectTransform.sizeDelta.x;
                m_CurrentColor = checkImage.color;
            }
            if (frameImage != null)
                m_CurrentFrameColor = frameImage.color;

            base.TurnOff();
        }

        public override void TurnOffInstant()
        {
            base.TurnOffInstant();

            if (m_Toggle != null)
            {
                if (m_Toggle.interactable)
                {
                    AnimOffComplete();
                }
            }

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = Vector2.zero;
        }

        protected override void ApplyInteractableOn()
        {
            base.ApplyInteractableOn();

            if (m_Toggle != null)
            {
                if (m_Toggle.isOn)
                {
                    AnimOnComplete();
                }
                else
                {
                    AnimOffComplete();
                }
            }
        }

        protected override void ApplyInteractableOff()
        {
            base.ApplyInteractableOff();

            if(checkImage != null)
                checkImage.color = disabledColor;
            if(frameImage != null)
                frameImage.color = disabledColor;
        }

        protected override void AnimOn()
        {
            base.AnimOn();

            if(checkImage != null)
                checkImage.color = Tween.QuintOut(m_CurrentColor, onColor, m_AnimDeltaTime, m_AnimationDuration);
            if(frameImage != null)
                frameImage.color = Tween.QuintOut(m_CurrentFrameColor, onColor, m_AnimDeltaTime, m_AnimationDuration);

            float tempSize = Tween.QuintOut(m_CurrentCheckSize, m_AnimationSize, m_AnimDeltaTime, m_AnimationDuration);

            if(checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(tempSize, tempSize);
        }

        protected override void AnimOnComplete()
        {
            base.AnimOnComplete();

            if(checkImage != null)
                checkImage.color = onColor;
            if (frameImage != null)
                frameImage.color = onColor;

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(m_AnimationSize, m_AnimationSize);
        }

        protected override void AnimOff()
        {
            base.AnimOff();

            if (checkImage != null)
                checkImage.color = Tween.QuintOut(m_CurrentColor, offColor, m_AnimDeltaTime, m_AnimationDuration);
            if (frameImage != null)
                frameImage.color = Tween.QuintOut(m_CurrentFrameColor, offColor, m_AnimDeltaTime, m_AnimationDuration);

            float tempSize = Tween.QuintOut(m_CurrentCheckSize, 0, m_AnimDeltaTime, m_AnimationDuration);

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(tempSize, tempSize);
        }

        protected override void AnimOffComplete()
        {
            base.AnimOffComplete();

            if (checkImage != null)
                checkImage.color = offColor;
            if (frameImage != null)
                frameImage.color = offColor;

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(0, 0);
        }

        #endregion
    }
}
