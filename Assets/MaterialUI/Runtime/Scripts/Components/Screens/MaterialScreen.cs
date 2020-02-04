//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Material Screen", 100)]
    public class MaterialScreen : MaterialActivity
    {
        #region Private Variables

        [SerializeField]
        private bool m_OptionsControlledByScreenView = true;
        [SerializeField]
        private bool m_DisableWhenNotVisible = true;

        //  Transition In
        [SerializeField]
        private bool m_FadeIn = true;
        [SerializeField]
        private Tween.TweenType m_FadeInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_FadeInAlpha = 0;
        [SerializeField]
        private AnimationCurve m_FadeInCustomCurve = null;
        [SerializeField]
        private bool m_ScaleIn = false;
        [SerializeField]
        private Tween.TweenType m_ScaleInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_ScaleInScale = 0;
        [SerializeField]
        private AnimationCurve m_ScaleInCustomCurve = null;
        [SerializeField]
        private bool m_SlideIn = false;
        [SerializeField]
        private Tween.TweenType m_SlideInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private ScreenView.SlideDirection m_SlideInDirection = ScreenView.SlideDirection.Right;
        [SerializeField]
        private bool m_AutoSlideInAmount = true;
        [SerializeField]
        private float m_SlideInAmount = 0;
        [SerializeField]
        private float m_SlideInPercent = 100f;
        [SerializeField]
        private AnimationCurve m_SlideInCustomCurve = null;
        [SerializeField]
        private bool m_RippleIn = false;
        [SerializeField]
        private Tween.TweenType m_RippleInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private ScreenView.RippleType m_RippleInType = ScreenView.RippleType.MousePosition;
        [SerializeField]
        private Vector2 m_RippleInPosition = Vector2.zero;
        [SerializeField]
        private AnimationCurve m_RippleInCustomCurve = null;

        //  Transition Out
        [SerializeField]
        private bool m_FadeOut = false;
        [SerializeField]
        private Tween.TweenType m_FadeOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_FadeOutAlpha = 0;
        [SerializeField]
        private AnimationCurve m_FadeOutCustomCurve = null;
        [SerializeField]
        private bool m_ScaleOut = false;
        [SerializeField]
        private Tween.TweenType m_ScaleOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_ScaleOutScale = 0;
        [SerializeField]
        private AnimationCurve m_ScaleOutCustomCurve = null;
        [SerializeField]
        private bool m_SlideOut = false;
        [SerializeField]
        private Tween.TweenType m_SlideOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private ScreenView.SlideDirection m_SlideOutDirection = ScreenView.SlideDirection.Left;
        [SerializeField]
        private bool m_AutoSlideOutAmount = true;
        [SerializeField]
        private float m_SlideOutAmount = 0;
        [SerializeField]
        private float m_SlideOutPercent = 100f;
        [SerializeField]
        private AnimationCurve m_SlideOutCustomCurve = null;
        [SerializeField]
        private bool m_RippleOut = false;
        [SerializeField]
        private Tween.TweenType m_RippleOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private ScreenView.RippleType m_RippleOutType = ScreenView.RippleType.MousePosition;
        [SerializeField]
        private Vector2 m_RippleOutPosition = Vector2.zero;
        [SerializeField]
        private AnimationCurve m_RippleOutCustomCurve = null;

        [SerializeField]
        private float m_TransitionDuration = 0.5f;

        private ScreenView m_ScreenView;

        #endregion

        #region Callbacks

        [Header("Callbacks")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_OnScreenEndTransitionIn")]
        public UnityEvent onScreenEndTransitionIn = new UnityEvent();
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_OnScreenEndTransitionOut")]
        private UnityEvent onScreenEndTransitionOut = new UnityEvent();

        #endregion

        #region Properties

        protected EasyFrameAnimator frameAnimator
        {
            get
            {
                if (Application.isPlaying)
                {
                    return this.GetAddComponent<EasyFrameAnimator>();
                }

                return null;
            }
        }

        public bool optionsControlledByScreenView
        {
            get { return screenView != null? m_OptionsControlledByScreenView : false; }
            set { m_OptionsControlledByScreenView = value; }
        }

        public bool disableWhenNotVisible
        {
            get { return m_DisableWhenNotVisible; }
            set { m_DisableWhenNotVisible = value; }
        }

        public bool fadeIn
        {
            get { return optionsControlledByScreenView? screenView.fadeIn : m_FadeIn; }
            set { m_FadeIn = value; }
        }

        public Tween.TweenType fadeInTweenType
        {
            get { return optionsControlledByScreenView ? screenView.fadeInTweenType : m_FadeInTweenType; }
            set { m_FadeInTweenType = value; }
        }

        public float fadeInAlpha
        {
            get { return optionsControlledByScreenView ? screenView.fadeInAlpha : m_FadeInAlpha; }
            set { m_FadeInAlpha = value; }
        }

        public AnimationCurve fadeInCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.fadeInCustomCurve : m_FadeInCustomCurve; }
            set { m_FadeInCustomCurve = value; }
        }

        public bool scaleIn
        {
            get { return optionsControlledByScreenView ? screenView.scaleIn : m_ScaleIn; }
            set { m_ScaleIn = value; }
        }

        public Tween.TweenType scaleInTweenType
        {
            get { return optionsControlledByScreenView ? screenView.scaleInTweenType : m_ScaleInTweenType; }
            set { m_ScaleInTweenType = value; }
        }

        public float scaleInScale
        {
            get { return optionsControlledByScreenView? screenView.scaleInScale : m_ScaleInScale; }
            set { m_ScaleInScale = value; }
        }

        public AnimationCurve scaleInCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.scaleInCustomCurve : m_ScaleInCustomCurve; }
            set { m_ScaleInCustomCurve = value; }
        }

        public bool slideIn
        {
            get { return optionsControlledByScreenView ? screenView.slideIn : m_SlideIn; }
            set { m_SlideIn = value; }
        }

        public Tween.TweenType slideInTweenType
        {
            get { return optionsControlledByScreenView ? screenView.slideInTweenType : m_SlideInTweenType; }
            set { m_SlideInTweenType = value; }
        }

        public ScreenView.SlideDirection slideInDirection
        {
            get { return optionsControlledByScreenView ? screenView.slideInDirection : m_SlideInDirection; }
            set { m_SlideInDirection = value; }
        }

        public bool autoSlideInAmount
        {
            get { return optionsControlledByScreenView ? screenView.autoSlideInAmount : m_AutoSlideInAmount; }
            set { m_AutoSlideInAmount = value; }
        }

        public float slideInAmount
        {
            get { return m_SlideInAmount; }
            set { m_SlideInAmount = value; }
        }

        public float slideInPercent
        {
            get { return optionsControlledByScreenView ? screenView.slideInPercent : m_SlideInPercent; }
            set { m_SlideInPercent = value; }
        }

        public AnimationCurve slideInCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.slideInCustomCurve : m_SlideInCustomCurve; }
            set { m_SlideInCustomCurve = value; }
        }

        public bool rippleIn
        {
            get { return optionsControlledByScreenView ? screenView.rippleIn : m_RippleIn; }
            set { m_RippleIn = value; }
        }

        public Tween.TweenType rippleInTweenType
        {
            get { return optionsControlledByScreenView ? screenView.rippleInTweenType : m_RippleInTweenType; }
            set { m_RippleInTweenType = value; }
        }

        public ScreenView.RippleType rippleInType
        {
            get { return optionsControlledByScreenView ? screenView.rippleInType : m_RippleInType; }
            set { m_RippleInType = value; }
        }

        public Vector2 rippleInPosition
        {
            get { return optionsControlledByScreenView ? screenView.rippleInPosition : m_RippleInPosition; }
            set { m_RippleInPosition = value; }
        }

        public AnimationCurve rippleInCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.rippleInCustomCurve : m_RippleInCustomCurve; }
            set { m_RippleInCustomCurve = value; }
        }

        public bool fadeOut
        {
            get { return optionsControlledByScreenView ? screenView.fadeOut : m_FadeOut; }
            set { m_FadeOut = value; }
        }

        public Tween.TweenType fadeOutTweenType
        {
            get { return optionsControlledByScreenView ? screenView.fadeOutTweenType : m_FadeOutTweenType; }
            set { m_FadeOutTweenType = value; }
        }

        public float fadeOutAlpha
        {
            get { return optionsControlledByScreenView ? screenView.fadeOutAlpha : m_FadeOutAlpha; }
            set { m_FadeOutAlpha = value; }
        }

        public AnimationCurve fadeOutCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.fadeOutCustomCurve : m_FadeOutCustomCurve; }
            set { m_FadeOutCustomCurve = value; }
        }

        public bool scaleOut
        {
            get { return optionsControlledByScreenView ? screenView.scaleOut : m_ScaleOut; }
            set { m_ScaleOut = value; }
        }

        public Tween.TweenType scaleOutTweenType
        {
            get { return optionsControlledByScreenView ? screenView.scaleOutTweenType : m_ScaleOutTweenType; }
            set { m_ScaleOutTweenType = value; }
        }

        public float scaleOutScale
        {
            get { return optionsControlledByScreenView? screenView.scaleOutScale : m_ScaleOutScale; }
            set { m_ScaleOutScale = value; }
        }

        public AnimationCurve scaleOutCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.scaleOutCustomCurve : m_ScaleOutCustomCurve; }
            set { m_ScaleOutCustomCurve = value; }
        }

        public bool slideOut
        {
            get { return optionsControlledByScreenView ? screenView.slideOut : m_SlideOut; }
            set { m_SlideOut = value; }
        }

        public Tween.TweenType slideOutTweenType
        {
            get { return optionsControlledByScreenView ? screenView.slideOutTweenType : m_SlideOutTweenType; }
            set { m_SlideOutTweenType = value; }
        }

        public ScreenView.SlideDirection slideOutDirection
        {
            get { return optionsControlledByScreenView ? screenView.slideOutDirection : m_SlideOutDirection; }
            set { m_SlideOutDirection = value; }
        }

        public bool autoSlideOutAmount
        {
            get { return optionsControlledByScreenView ? screenView.autoSlideOutAmount : m_AutoSlideOutAmount; }
            set { m_AutoSlideOutAmount = value; }
        }

        public float slideOutAmount
        {
            get { return m_SlideOutAmount; }
            set { m_SlideOutAmount = value; }
        }

        public float slideOutPercent
        {
            get { return optionsControlledByScreenView ? screenView.slideOutPercent : m_SlideOutPercent; }
            set { m_SlideOutPercent = value; }
        }

        public AnimationCurve slideOutCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.slideOutCustomCurve : m_SlideOutCustomCurve; }
            set { m_SlideOutCustomCurve = value; }
        }

        public bool rippleOut
        {
            get { return optionsControlledByScreenView ? screenView.rippleOut : m_RippleOut; }
            set { m_RippleOut = value; }
        }

        public Tween.TweenType rippleOutTweenType
        {
            get { return optionsControlledByScreenView ? screenView.rippleOutTweenType : m_RippleOutTweenType; }
            set { m_RippleOutTweenType = value; }
        }

        public ScreenView.RippleType rippleOutType
        {
            get { return optionsControlledByScreenView ? screenView.rippleOutType : m_RippleOutType; }
            set { m_RippleOutType = value; }
        }

        public Vector2 rippleOutPosition
        {
            get { return optionsControlledByScreenView ? screenView.rippleOutPosition : m_RippleOutPosition; }
            set { m_RippleOutPosition = value; }
        }

        public AnimationCurve rippleOutCustomCurve
        {
            get { return optionsControlledByScreenView ? screenView.rippleOutCustomCurve : m_RippleOutCustomCurve; }
            set { m_RippleOutCustomCurve = value; }
        }

        public float transitionDuration
        {
            get { return optionsControlledByScreenView ? screenView.transitionDuration : m_TransitionDuration; }
            set { m_TransitionDuration = value; }
        }

        public ScreenView screenView
        {
            get
            {
                if (m_ScreenView == null)
                {
                    m_ScreenView = GetComponentInParent<ScreenView>();
                    if (m_ScreenView != null)
                        TryRegisterInScreenView();
                }

                return m_ScreenView != null && (m_ScreenView.content == this.transform.parent || 
                    (frameAnimator != null && frameAnimator.ripple != null && frameAnimator.ripple.parent == m_ScreenView.content)) ? 
                    m_ScreenView : 
                    null;
            }
        }

        public int screenIndex
        {
            get { return screenView != null && screenView.materialScreen != null ? screenView.materialScreen.IndexOf(this) : -1; }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            TryRegisterInScreenView();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            InterruptAnimation();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterFrameAnimatorEvents();

            //Prevent Errors while deleting screen
            TryUnregisterInScreenView();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            var newScreenView = GetComponentInParent<ScreenView>();
            newScreenView = newScreenView != null && (newScreenView.content == this.transform.parent || 
                (frameAnimator != null && frameAnimator.ripple != null && frameAnimator.ripple.parent == newScreenView.content)) ? newScreenView : null;

            if (screenView != newScreenView)
            {
                TryUnregisterInScreenView();
                m_ScreenView = newScreenView;
                TryRegisterInScreenView();
            }
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnShow()
        {
            if (onScreenEndTransitionIn != null)
                onScreenEndTransitionIn.Invoke();
        }

        protected virtual void HandleOnHide()
        {
            if (onScreenEndTransitionOut != null)
                onScreenEndTransitionOut.Invoke();
        }

        protected virtual void HandleOnInterrupt(int animationIndex)
        {
            ProcessInterruption(animationIndex, m_DestroyOnHide);
        }

        #endregion

        #region Public Functions

        protected virtual void TryRegisterInScreenView()
        {
            if (screenView != null)
            {
                var v_index = screenView.materialScreen.IndexOf(this);
                if (v_index < 0)
                {
                    screenView.materialScreen.Add(this);
                }
            }
        }

        protected virtual void TryUnregisterInScreenView()
        {
            if (screenView != null)
            {
                var v_index = screenIndex;
                if (screenView.materialScreen.Count > v_index)
                    screenView.materialScreen[v_index] = null;
                screenView.RemoveInvalidScreens();
            }
        }

        public override void Show()
        {
            if (screenView != null)
            {
                screenView.Transition(this);
            }
        }

        public override void Hide()
        {
            Hide(false);
        }

        public virtual void Hide(bool forceHide)
        {
            if (screenView != null)
            {
                if (forceHide || screenView.currentScreen == this)
                {
                    screenView.Back();
                }
            }
        }

        public virtual void HideToNext()
        {
            HideToNext(false);
        }

        public virtual void HideToNext(bool forceHide)
        {
            if (screenView != null)
                HideToScreenIndex(screenView.GetNextScreenIndex());
        }

        public virtual void HideToPrevious()
        {
            HideToPrevious(false);
        }

        public virtual void HideToPrevious(bool forceHide)
        {
            if (screenView != null)
                HideToScreenIndex(screenView.GetPreviousScreenIndex());
        }

        public virtual void HideToScreenIndex(int screenIndexToShow)
        {
            HideToScreenIndex(screenIndexToShow, false);
        }

        public virtual void HideToScreenIndex(int screenIndexToShow, bool forceHide)
        {
            if (screenView != null)
            {
                var selfScreenIndex = screenIndex;
                if (forceHide || selfScreenIndex == screenView.currentScreenIndex)
                {
                    //Add next screen to stack before current screen (so we can call Hide and Destroy)
                    var screen = screenView.PushToScreenStack(screenIndexToShow);
                    if (screen != this)
                    {
                        screenView.RemoveFromScreenStack(selfScreenIndex);
                        if (screenView.currentScreen != null)
                        {
                            Hide(forceHide);
                        }
                    }
                }
            }
        }

        public virtual void HideToScreenWithName(string screenName)
        {
            HideToScreenWithName(screenName, false);
        }

        public virtual void HideToScreenWithName(string screenName, bool forceHide)
        {
            if (screenView != null)
            {
                var selfScreenIndex = screenIndex;
                if (forceHide || selfScreenIndex == screenView.currentScreenIndex)
                {
                    //Add next screen to stack before current screen (so we can call Hide and Destroy)
                    var screen = screenView.PushToScreenStack(screenName);
                    if (screen != this)
                    {
                        screenView.RemoveFromScreenStack(selfScreenIndex);
                        if (screenView.currentScreen != null)
                        {
                            Hide(forceHide);
                        }
                    }
                }
            }
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void SetupFrameAnimator()
        {
            var frameAnimator = this.frameAnimator;
            if (frameAnimator == null)
                return;

            frameAnimator.fadeIn = fadeIn;
            frameAnimator.fadeInTweenType = fadeInTweenType;
            frameAnimator.fadeInAlpha = fadeInAlpha;
            frameAnimator.fadeInCustomCurve = fadeInCustomCurve;

            frameAnimator.scaleIn = scaleIn;
            frameAnimator.scaleInTweenType = scaleInTweenType;
            frameAnimator.scaleInScale = scaleInScale;
            frameAnimator.scaleInCustomCurve = scaleInCustomCurve;

            frameAnimator.slideIn = slideIn;
            frameAnimator.slideInTweenType = slideInTweenType;
            frameAnimator.slideInDirection = slideInDirection;
            frameAnimator.autoSlideInAmount = autoSlideInAmount;
            frameAnimator.slideInAmount = slideInAmount;
            frameAnimator.slideInPercent = slideInPercent;
            frameAnimator.slideInCustomCurve = slideInCustomCurve;

            frameAnimator.rippleIn = rippleIn;
            frameAnimator.rippleInTweenType = rippleInTweenType;
            frameAnimator.rippleInType = rippleInType;
            frameAnimator.rippleInPosition = rippleInPosition;
            frameAnimator.rippleInCustomCurve = rippleInCustomCurve;

            frameAnimator.fadeOut = fadeOut;
            frameAnimator.fadeOutTweenType = fadeOutTweenType;
            frameAnimator.fadeOutAlpha = fadeOutAlpha;
            frameAnimator.fadeOutCustomCurve = fadeOutCustomCurve;

            frameAnimator.scaleOut = scaleOut;
            frameAnimator.scaleOutTweenType = scaleOutTweenType;
            frameAnimator.scaleOutScale = scaleOutScale;
            frameAnimator.scaleOutCustomCurve = scaleOutCustomCurve;

            frameAnimator.slideOut = slideOut;
            frameAnimator.slideOutTweenType = slideOutTweenType;
            frameAnimator.slideOutDirection = slideOutDirection;
            frameAnimator.autoSlideOutAmount = autoSlideOutAmount;
            frameAnimator.slideOutAmount = slideOutAmount;
            frameAnimator.slideOutPercent = slideOutPercent;
            frameAnimator.slideOutCustomCurve = slideOutCustomCurve;

            frameAnimator.rippleOut = rippleOut;
            frameAnimator.rippleOutTweenType = rippleOutTweenType;
            frameAnimator.rippleOutType = rippleOutType;
            frameAnimator.rippleOutPosition = rippleOutPosition;
            frameAnimator.rippleOutCustomCurve = rippleOutCustomCurve;

            frameAnimator.transitionDuration = transitionDuration;

            RegisterFrameAnimatorEvents();
        }

        protected virtual void RegisterFrameAnimatorEvents()
        {
            UnregisterFrameAnimatorEvents();

            if (frameAnimator != null)
                frameAnimator.onEndTransitionIn.AddListener(HandleOnShow);
            if (frameAnimator != null)
                frameAnimator.onEndTransitionOut.AddListener(HandleOnHide);
            if (frameAnimator != null)
                frameAnimator.onInterruptAnimation.AddListener(HandleOnInterrupt);
        }

        protected virtual void UnregisterFrameAnimatorEvents()
        {
            if (frameAnimator != null)
                frameAnimator.onEndTransitionIn.RemoveListener(HandleOnShow);
            if (frameAnimator != null)
                frameAnimator.onEndTransitionOut.RemoveListener(HandleOnHide);
            if (frameAnimator != null)
                frameAnimator.onInterruptAnimation.RemoveListener(HandleOnInterrupt);
        }

        protected internal void TransitionIn()
        {
            if (frameAnimator != null)
            {
                SetupFrameAnimator();
                frameAnimator.TransitionIn();
            }
        }

        protected internal void TransitionOut()
        {
            if (frameAnimator != null)
            {
                SetupFrameAnimator();
                frameAnimator.TransitionOut();
            }
        }

        protected internal void TransitionOutImmediate()
        {
            if (frameAnimator != null)
            {
                SetupFrameAnimator();
                frameAnimator.TransitionOutImmediate();
            }
        }

        protected internal int InterruptAnimation()
        {
            if (frameAnimator != null)
            {
                SetupFrameAnimator();
                UnregisterFrameAnimatorEvents();
                return frameAnimator.InterruptAnimation();
            }

            return -1;
        }

        protected internal void Interrupt(bool canDestroy = false)
        {
            SetupFrameAnimator();
            var processedTransition = frameAnimator != null ? frameAnimator.InterruptAnimation() : -1;

            ProcessInterruption(processedTransition, canDestroy);
        }

        protected internal void ProcessInterruption(int processedTransition, bool canDestroy)
        {
            if (processedTransition > 1)
            {
                if (m_DisableWhenNotVisible)
                {
                    gameObject.SetActive(false);
                }
            }

            var markToDestroy = processedTransition == 2;
            if (Application.isPlaying && markToDestroy && canDestroy)
            {
                GameObject.Destroy(this.gameObject);
            }
        }

        #endregion
    }
}