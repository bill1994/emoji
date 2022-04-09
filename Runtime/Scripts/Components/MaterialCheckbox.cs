// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

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

        protected override void AnimOn()
        {
            base.AnimOn();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();
            if (checkImage != null)
                checkImage.color = Tween.QuintOut(m_CurrentColor, !canUseDisabledColor || isInteractable ? onColor : disabledColor, m_AnimDeltaTime, m_AnimationDuration);
            if(frameImage != null)
                frameImage.color = Tween.QuintOut(m_CurrentFrameColor, !canUseDisabledColor || isInteractable ? onColor : disabledColor, m_AnimDeltaTime, m_AnimationDuration);

            float tempSize = Tween.QuintOut(m_CurrentCheckSize, m_AnimationSize, m_AnimDeltaTime, m_AnimationDuration);

            if(checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(tempSize, tempSize);
        }

        protected override void AnimOff()
        {
            base.AnimOff();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();
            if (checkImage != null)
                checkImage.color = Tween.QuintOut(m_CurrentColor, !canUseDisabledColor || isInteractable ? offColor : disabledColor, m_AnimDeltaTime, m_AnimationDuration);
            if (frameImage != null)
                frameImage.color = Tween.QuintOut(m_CurrentFrameColor, !canUseDisabledColor || isInteractable ? offColor : disabledColor, m_AnimDeltaTime, m_AnimationDuration);

            float tempSize = Tween.QuintOut(m_CurrentCheckSize, 0, m_AnimDeltaTime, m_AnimationDuration);

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(tempSize, tempSize);
        }

        protected override void SetOnColor()
        {
            base.SetOnColor();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();

            if (checkImage != null)
                checkImage.color = !canUseDisabledColor || isInteractable ? onColor : disabledColor;
            if (frameImage != null)
                frameImage.color = !canUseDisabledColor || isInteractable ? onColor : disabledColor;

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(m_AnimationSize, m_AnimationSize);
        }

        protected override void SetOffColor()
        {
            base.SetOffColor();

            var canUseDisabledColor = CanUseDisabledColor();
            var isInteractable = IsInteractable();
            if (checkImage != null)
                checkImage.color = !canUseDisabledColor || isInteractable ? offColor : disabledColor;
            if (frameImage != null)
                frameImage.color = !canUseDisabledColor || isInteractable ? offColor : disabledColor;

            if (checkRectTransform != null)
                checkRectTransform.sizeDelta = new Vector2(0, 0);
        }

        #endregion
    }
}
