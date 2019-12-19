using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    public class EasyFrameAnimator : AbstractTweenBehaviour
    {
        #region Helper Structs

        [System.Serializable]
        public class IntUnityEvent : UnityEvent<int> { }

        public struct RectTransformValues
        {
            //public Vector3 localPosition { get; set; }
            //public Vector3 localScale { get; set; }
            //public Quaternion localRotation { get; set; }
            public Vector2 sizeDelta { get; set; }
            public Vector2 anchorMin { get; set; }
            public Vector2 anchorMax { get; set; }
            public Vector2 pivot { get; set; }
            public bool? contentSizeFitterEnabled { get; set; }

            public void FromTransform(RectTransform rectTransform)
            {
                if (rectTransform != null)
                {
                    //localPosition = rectTransform.localPosition;
                    //localScale = rectTransform.localScale;
                    //localRotation = rectTransform.localRotation;
                    anchorMin = rectTransform.anchorMin;
                    anchorMax = rectTransform.anchorMax;
                    pivot = rectTransform.pivot;
                    sizeDelta = rectTransform.sizeDelta;

                    var fitter = rectTransform.GetComponent<ContentSizeFitter>();
                    contentSizeFitterEnabled = fitter != null ? (bool?)fitter.enabled : null;
                }
            }

            public void ToTransform(RectTransform rectTransform)
            {
                if (rectTransform != null)
                {
                    //rectTransform.localPosition = localPosition;
                    //rectTransform.localScale = localScale;
                    //rectTransform.localRotation = localRotation;
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    rectTransform.pivot = pivot;
                    rectTransform.sizeDelta = sizeDelta;

                    var fitter = rectTransform.GetComponent<ContentSizeFitter>();
                    if (fitter != null && contentSizeFitterEnabled != null && contentSizeFitterEnabled.HasValue)
                        fitter.enabled = contentSizeFitterEnabled.Value;
                }
            }
        }

        #endregion

        #region Private Variables

        //  Transition In
        [SerializeField]
        private bool m_FadeIn = false;
        [SerializeField]
        private Tween.TweenType m_FadeInTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_FadeInAlpha = 0;
        [SerializeField]
        private AnimationCurve m_FadeInCustomCurve = null;
        [SerializeField]
        private bool m_ScaleIn = false;
        [SerializeField]
        private Tween.TweenType m_ScaleInTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_ScaleInScale = 0;
        [SerializeField]
        private AnimationCurve m_ScaleInCustomCurve = null;
        [SerializeField]
        private bool m_SlideIn = false;
        [SerializeField]
        private Tween.TweenType m_SlideInTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
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
        private Tween.TweenType m_RippleInTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
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
        private Tween.TweenType m_FadeOutTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_FadeOutAlpha = 0;
        [SerializeField]
        private AnimationCurve m_FadeOutCustomCurve = null;
        [SerializeField]
        private bool m_ScaleOut = false;
        [SerializeField]
        private Tween.TweenType m_ScaleOutTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_ScaleOutScale = 0;
        [SerializeField]
        private AnimationCurve m_ScaleOutCustomCurve = null;
        [SerializeField]
        private bool m_SlideOut = false;
        [SerializeField]
        private Tween.TweenType m_SlideOutTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
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
        private Tween.TweenType m_RippleOutTweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private ScreenView.RippleType m_RippleOutType = ScreenView.RippleType.MousePosition;
        [SerializeField]
        private Vector2 m_RippleOutPosition = Vector2.zero;
        [SerializeField]
        private AnimationCurve m_RippleOutCustomCurve = null;

        [SerializeField]
        private float m_TransitionDuration = 0.5f;

        private RectTransform _Ripple;
        private CanvasGroup _CanvasGroup;


        private int _IsTransitioning = 0;
        private float _TransitionCurrentTime;

        private RectTransformValues _TempRippleAnchors;
        private Vector2 _TempRippleSize;
        private Vector3 _TempRippleScale;
        private Vector3 _TargetRipplePos;
        private Vector3 _CurrentRipplePos;

        private Vector3 _TempScreenPos;
        private Vector3 _SlideScreenPos;

        #endregion

        #region Callbacks

        [Header("Callbacks")]
        [SerializeField]
        public UnityEvent onEndTransitionIn = new UnityEvent();
        [SerializeField]
        public UnityEvent onEndTransitionOut = new UnityEvent();
        [SerializeField]
        public IntUnityEvent onInterruptAnimation = new IntUnityEvent();

        #endregion

        #region Properties

        public bool fadeIn
        {
            get { return m_FadeIn; }
            set { m_FadeIn = value; }
        }

        public Tween.TweenType fadeInTweenType
        {
            get { return m_FadeInTweenType; }
            set { m_FadeInTweenType = value; }
        }

        public float fadeInAlpha
        {
            get { return m_FadeInAlpha; }
            set { m_FadeInAlpha = value; }
        }

        public AnimationCurve fadeInCustomCurve
        {
            get { return m_FadeInCustomCurve; }
            set { m_FadeInCustomCurve = value; }
        }

        public bool scaleIn
        {
            get { return m_ScaleIn; }
            set { m_ScaleIn = value; }
        }

        public Tween.TweenType scaleInTweenType
        {
            get { return m_ScaleInTweenType; }
            set { m_ScaleInTweenType = value; }
        }

        public float scaleInScale
        {
            get { return m_ScaleInScale; }
            set { m_ScaleInScale = value; }
        }

        public AnimationCurve scaleInCustomCurve
        {
            get { return m_ScaleInCustomCurve; }
            set { m_ScaleInCustomCurve = value; }
        }

        public bool slideIn
        {
            get { return m_SlideIn; }
            set { m_SlideIn = value; }
        }

        public Tween.TweenType slideInTweenType
        {
            get { return m_SlideInTweenType; }
            set { m_SlideInTweenType = value; }
        }

        public ScreenView.SlideDirection slideInDirection
        {
            get { return m_SlideInDirection; }
            set { m_SlideInDirection = value; }
        }

        public bool autoSlideInAmount
        {
            get { return m_AutoSlideInAmount; }
            set { m_AutoSlideInAmount = value; }
        }

        public float slideInAmount
        {
            get { return m_SlideInAmount; }
            set { m_SlideInAmount = value; }
        }

        public float slideInPercent
        {
            get { return m_SlideInPercent; }
            set { m_SlideInPercent = value; }
        }

        public AnimationCurve slideInCustomCurve
        {
            get { return m_SlideInCustomCurve; }
            set { m_SlideInCustomCurve = value; }
        }

        public bool rippleIn
        {
            get { return m_RippleIn; }
            set { m_RippleIn = value; }
        }

        public Tween.TweenType rippleInTweenType
        {
            get { return m_RippleInTweenType; }
            set { m_RippleInTweenType = value; }
        }

        public ScreenView.RippleType rippleInType
        {
            get { return m_RippleInType; }
            set { m_RippleInType = value; }
        }

        public Vector2 rippleInPosition
        {
            get { return m_RippleInPosition; }
            set { m_RippleInPosition = value; }
        }

        public AnimationCurve rippleInCustomCurve
        {
            get { return m_RippleInCustomCurve; }
            set { m_RippleInCustomCurve = value; }
        }

        public bool fadeOut
        {
            get { return m_FadeOut; }
            set { m_FadeOut = value; }
        }

        public Tween.TweenType fadeOutTweenType
        {
            get { return m_FadeOutTweenType; }
            set { m_FadeOutTweenType = value; }
        }

        public float fadeOutAlpha
        {
            get { return m_FadeOutAlpha; }
            set { m_FadeOutAlpha = value; }
        }

        public AnimationCurve fadeOutCustomCurve
        {
            get { return m_FadeOutCustomCurve; }
            set { m_FadeOutCustomCurve = value; }
        }

        public bool scaleOut
        {
            get { return m_ScaleOut; }
            set { m_ScaleOut = value; }
        }

        public Tween.TweenType scaleOutTweenType
        {
            get { return m_ScaleOutTweenType; }
            set { m_ScaleOutTweenType = value; }
        }

        public float scaleOutScale
        {
            get { return m_ScaleOutScale; }
            set { m_ScaleOutScale = value; }
        }

        public AnimationCurve scaleOutCustomCurve
        {
            get { return m_ScaleOutCustomCurve; }
            set { m_ScaleOutCustomCurve = value; }
        }

        public bool slideOut
        {
            get { return m_SlideOut; }
            set { m_SlideOut = value; }
        }

        public Tween.TweenType slideOutTweenType
        {
            get { return m_SlideOutTweenType; }
            set { m_SlideOutTweenType = value; }
        }

        public ScreenView.SlideDirection slideOutDirection
        {
            get { return m_SlideOutDirection; }
            set { m_SlideOutDirection = value; }
        }

        public bool autoSlideOutAmount
        {
            get { return m_AutoSlideOutAmount; }
            set { m_AutoSlideOutAmount = value; }
        }

        public float slideOutAmount
        {
            get { return m_SlideOutAmount; }
            set { m_SlideOutAmount = value; }
        }

        public float slideOutPercent
        {
            get { return m_SlideOutPercent; }
            set { m_SlideOutPercent = value; }
        }

        public AnimationCurve slideOutCustomCurve
        {
            get { return m_SlideOutCustomCurve; }
            set { m_SlideOutCustomCurve = value; }
        }

        public bool rippleOut
        {
            get { return m_RippleOut; }
            set { m_RippleOut = value; }
        }

        public Tween.TweenType rippleOutTweenType
        {
            get { return m_RippleOutTweenType; }
            set { m_RippleOutTweenType = value; }
        }

        public ScreenView.RippleType rippleOutType
        {
            get { return m_RippleOutType; }
            set { m_RippleOutType = value; }
        }

        public Vector2 rippleOutPosition
        {
            get { return m_RippleOutPosition; }
            set { m_RippleOutPosition = value; }
        }

        public AnimationCurve rippleOutCustomCurve
        {
            get { return m_RippleOutCustomCurve; }
            set { m_RippleOutCustomCurve = value; }
        }

        public float transitionDuration
        {
            get { return m_TransitionDuration; }
            set { m_TransitionDuration = value; }
        }

        public float transitionCurrentTime
        {
            get { return _TransitionCurrentTime; }
            set { _TransitionCurrentTime = value; }
        }

        private RectTransform ripple
        {
            get
            {
                if (_Ripple == null)
                {
                    _Ripple = new GameObject("Ripple Mask", typeof(VectorImageTMPro)).GetComponent<RectTransform>();
                    _Ripple.GetComponent<VectorImageTMPro>().vectorImageData = MaterialUIIconHelper.GetIcon("circle").vectorImageData;
                    _Ripple.SetParent(rectTransform.parent);
                    _Ripple.localScale = Vector3.one;
                    _Ripple.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                    _Ripple.sizeDelta = Vector2.zero;
                    _Ripple.position = GetRipplePosition();
                    _Ripple.localRotation = Quaternion.identity;
                    _Ripple.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                }
                return _Ripple;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                return gameObject.transform as RectTransform;
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (gameObject != null)
                {
                    _CanvasGroup = gameObject.GetAddComponent<CanvasGroup>();
                    _CanvasGroup.blocksRaycasts = true;
                    _CanvasGroup.interactable = true;
                    //m_CanvasGroup.ignoreParentGroups = true;
                }
                return _CanvasGroup;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnDisable()
        {
            InterruptAnimation();
        }

        protected virtual void Update()
        {
            if (_IsTransitioning > 0)
            {
                Kyub.Performance.SustainedPerformanceManager.Refresh(rectTransform);
                if (_TransitionCurrentTime < 0)
                    _TransitionCurrentTime = 0;
                _TransitionCurrentTime += Time.smoothDeltaTime;

                if (transitionCurrentTime <= transitionDuration)
                {
                    if (_IsTransitioning == 1)
                    {
                        if (rippleIn)
                        {
                            Vector3 tempVector3 = _Ripple.position;
                            tempVector3.x = MaterialUI.Tween.Evaluate(rippleInTweenType, _CurrentRipplePos.x, _TargetRipplePos.x, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            tempVector3.y = MaterialUI.Tween.Evaluate(rippleInTweenType, _CurrentRipplePos.y, _TargetRipplePos.y, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            tempVector3.z = MaterialUI.Tween.Evaluate(rippleInTweenType, _CurrentRipplePos.z, _TargetRipplePos.z, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            _Ripple.position = tempVector3;

                            Vector2 tempVector2 = _Ripple.sizeDelta;
                            tempVector2.x = MaterialUI.Tween.Evaluate(rippleInTweenType, 0, _TempRippleSize.x, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            tempVector2.y = MaterialUI.Tween.Evaluate(rippleInTweenType, 0, _TempRippleSize.y, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            _Ripple.sizeDelta = tempVector2;

                            rectTransform.position = _TempScreenPos;

                            rectTransform.localScale = new Vector3(_TempRippleScale.x / ripple.localScale.x, _TempRippleScale.y / ripple.localScale.y, _TempRippleScale.z / ripple.localScale.z);
                        }
                        if (fadeIn)
                        {
                            canvasGroup.alpha = MaterialUI.Tween.Evaluate(fadeInTweenType, fadeInAlpha, 1f, _TransitionCurrentTime,
                                transitionDuration, fadeInCustomCurve);
                        }
                        if (scaleIn)
                        {
                            Vector3 tempVector3 = rectTransform.localScale;
                            tempVector3.x = MaterialUI.Tween.Evaluate(scaleInTweenType, scaleInScale, 1f, _TransitionCurrentTime,
                                transitionDuration, scaleInCustomCurve);
                            tempVector3.y = tempVector3.x;
                            tempVector3.z = tempVector3.x;
                            rectTransform.localScale = tempVector3;
                        }
                        if (slideIn)
                        {
                            Vector3 tempVector3 = rectTransform.position;
                            tempVector3.x = MaterialUI.Tween.Evaluate(slideInTweenType, _SlideScreenPos.x, _TempScreenPos.x, _TransitionCurrentTime,
                                transitionDuration, slideInCustomCurve);
                            tempVector3.y = MaterialUI.Tween.Evaluate(slideInTweenType, _SlideScreenPos.y, _TempScreenPos.y, _TransitionCurrentTime,
                                transitionDuration, slideInCustomCurve);
                            tempVector3.z = MaterialUI.Tween.Evaluate(slideInTweenType, _SlideScreenPos.z, _TempScreenPos.z, _TransitionCurrentTime,
                                transitionDuration, slideInCustomCurve);
                            rectTransform.position = tempVector3;
                        }
                    }
                    else if (_IsTransitioning == 2)
                    {
                        if (rippleOut)
                        {
                            Vector3 tempVector3 = _Ripple.position;
                            tempVector3.x = MaterialUI.Tween.Evaluate(rippleInTweenType, _CurrentRipplePos.x, _TargetRipplePos.x, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            tempVector3.y = MaterialUI.Tween.Evaluate(rippleInTweenType, _CurrentRipplePos.y, _TargetRipplePos.y, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            tempVector3.z = MaterialUI.Tween.Evaluate(rippleInTweenType, _CurrentRipplePos.z, _TargetRipplePos.z, _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            _Ripple.position = tempVector3;

                            Vector2 tempVector2 = _Ripple.sizeDelta;
                            tempVector2.x = MaterialUI.Tween.Evaluate(rippleInTweenType, _TempRippleSize.x, 0,
                                _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            tempVector2.y = MaterialUI.Tween.Evaluate(rippleInTweenType, _TempRippleSize.y, 0,
                                _TransitionCurrentTime, m_TransitionDuration, rippleInCustomCurve);
                            _Ripple.sizeDelta = tempVector2;

                            rectTransform.position = _TempScreenPos;

                            rectTransform.localScale = new Vector3(_TempRippleScale.x / ripple.localScale.x, _TempRippleScale.y / ripple.localScale.y, _TempRippleScale.z / ripple.localScale.z);
                        }
                        if (fadeOut)
                        {
                            canvasGroup.alpha = MaterialUI.Tween.Evaluate(fadeOutTweenType, 1f, fadeOutAlpha,
                                _TransitionCurrentTime, transitionDuration, fadeOutCustomCurve);
                        }
                        if (scaleOut)
                        {
                            Vector3 tempVector3 = rectTransform.localScale;
                            tempVector3.x = MaterialUI.Tween.Evaluate(scaleOutTweenType, 1f, scaleOutScale, _TransitionCurrentTime,
                                transitionDuration, scaleOutCustomCurve);
                            tempVector3.y = tempVector3.x;
                            tempVector3.z = tempVector3.x;
                            rectTransform.localScale = tempVector3;
                        }
                        if (slideOut)
                        {
                            Vector3 tempVector3 = rectTransform.position;
                            tempVector3.x = MaterialUI.Tween.Evaluate(slideOutTweenType, _TempScreenPos.x, _SlideScreenPos.x,
                                _TransitionCurrentTime, transitionDuration, slideOutCustomCurve);
                            tempVector3.y = MaterialUI.Tween.Evaluate(slideOutTweenType, _TempScreenPos.y, _SlideScreenPos.y, _TransitionCurrentTime,
                                transitionDuration, slideOutCustomCurve);
                            tempVector3.z = MaterialUI.Tween.Evaluate(slideOutTweenType, _TempScreenPos.z, _SlideScreenPos.z, _TransitionCurrentTime,
                                transitionDuration, slideOutCustomCurve);
                            rectTransform.position = tempVector3;
                        }
                    }
                }
                else
                    InterruptAnimation();
            }
        }

        #endregion

        #region Public Functions

        public virtual void TransitionIn()
        {
            //CheckValues();

            //InterruptAnimation();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (gameObject != null)
                gameObject.SetActive(true);

            _TempScreenPos = rectTransform.position;

            if (rippleIn)
            {
                SetupRipple();
                ripple.SetSiblingIndex(rectTransform.GetSiblingIndex());
                Vector2 tempSize = rectTransform.GetProperSize();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _TempRippleScale = rectTransform.localScale;
                rectTransform.SetParent(ripple, true);
                rectTransform.sizeDelta = tempSize;
            }
            if (fadeIn)
            {
                canvasGroup.alpha = fadeInAlpha;
            }
            if (scaleIn)
            {
                rectTransform.localScale = new Vector3(scaleInScale, scaleInScale, scaleInScale);
            }
            if (slideIn)
            {
                if (autoSlideInAmount)
                {
                    var rectTransformParent = rectTransform.parent as RectTransform;

                    //Create Rect To Compare Distance
                    var parentRect = rectTransformParent.rect;
                    var selfRect = rectTransform.rect;

                    Vector2 selfLocalMin = rectTransformParent.InverseTransformDirection(rectTransformParent.TransformDirection(new Vector2(selfRect.xMin, selfRect.yMin)));
                    Vector2 selfLocalMax = rectTransformParent.InverseTransformDirection(rectTransformParent.TransformDirection(new Vector2(selfRect.xMax, selfRect.yMax)));
                    selfRect = Rect.MinMaxRect(selfLocalMin.x, selfLocalMin.y, selfLocalMax.x, selfLocalMax.y);

                    if (slideInDirection == ScreenView.SlideDirection.Up)
                        slideInAmount = Mathf.Abs(selfRect.yMin - parentRect.yMax);
                    else if (slideInDirection == ScreenView.SlideDirection.Down)
                        slideInAmount = Mathf.Abs(selfRect.yMax - parentRect.yMin);
                    else if (slideInDirection == ScreenView.SlideDirection.Left)
                        slideInAmount = Mathf.Abs(selfRect.xMax - parentRect.xMin);
                    else
                        slideInAmount = Mathf.Abs(selfRect.xMin - parentRect.xMax);

                    /*bool isVertical = (slideInDirection == ScreenView.SlideDirection.Up ||
                                       slideInDirection == ScreenView.SlideDirection.Down);

                    if (isVertical)
                    {

                        slideInAmount = rectTransform.localScale.y * rectTransform.GetProperSize().y * slideInPercent * 0.01f;

                        if (parentRect != null)
                            slideInAmount += Mathf.Abs(slideInAmount - (rectTransformParent.localScale.y * rectTransformParent.GetProperSize().y * slideInPercent * 0.01f));
                    }
                    else
                    {
                        slideInAmount = rectTransform.localScale.x * rectTransform.GetProperSize().x * slideInPercent * 0.01f;

                        if (rectTransformParent != null)
                            slideInAmount += Mathf.Abs(slideInAmount - (rectTransformParent.localScale.x * rectTransformParent.GetProperSize().x * slideInPercent * 0.01f));
                    }*/
                }

                var localRectPosition = rectTransform.localPosition;
                switch (slideInDirection)
                {
                    case ScreenView.SlideDirection.Left:
                        _SlideScreenPos = new Vector2(localRectPosition.x - slideInAmount, localRectPosition.y);
                        break;
                    case ScreenView.SlideDirection.Right:
                        _SlideScreenPos = new Vector2(localRectPosition.x + slideInAmount, localRectPosition.y);
                        break;
                    case ScreenView.SlideDirection.Up:
                        _SlideScreenPos = new Vector2(localRectPosition.x, localRectPosition.y + slideInAmount);
                        break;
                    case ScreenView.SlideDirection.Down:
                        _SlideScreenPos = new Vector2(localRectPosition.x, localRectPosition.y - slideInAmount);
                        break;
                }
                if (rectTransform.parent != null)
                    _SlideScreenPos = rectTransform.parent.TransformPoint(_SlideScreenPos);
                rectTransform.position = _SlideScreenPos;
            }

            enabled = true;
            _IsTransitioning = 1;
            _TransitionCurrentTime = -1;  //Time.realtimeSinceStartup;
        }

        public virtual void TransitionOut()
        {
            //CheckValues();

            //InterruptAnimation();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            _TempScreenPos = rectTransform.position;

            if (rippleOut)
            {
                SetupRipple();
                _TempRippleSize = GetRippleTargetSize();
                ripple.sizeDelta = _TempRippleSize;
                ripple.anchoredPosition = Vector2.zero;
                ripple.SetSiblingIndex(rectTransform.GetSiblingIndex());
                Vector2 tempSize = rectTransform.GetProperSize();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _TempRippleScale = rectTransform.localScale;
                rectTransform.SetParent(ripple, true);
                rectTransform.sizeDelta = tempSize;
            }
            if (fadeOut)
            {
                canvasGroup.alpha = 1f;
            }
            if (scaleOut)
            {
                rectTransform.localScale = new Vector3(1f, 1f, 1f);
            }
            if (slideOut)
            {
                if (autoSlideOutAmount)
                {
                    var rectTransformParent = rectTransform.parent as RectTransform;

                    //Create Rect To Compare Distance
                    var parentRect = rectTransformParent.rect;
                    var selfRect = rectTransform.rect;

                    Vector2 selfLocalMin = rectTransformParent.InverseTransformDirection(rectTransformParent.TransformDirection(new Vector2(selfRect.xMin, selfRect.yMin)));
                    Vector2 selfLocalMax = rectTransformParent.InverseTransformDirection(rectTransformParent.TransformDirection(new Vector2(selfRect.xMax, selfRect.yMax)));
                    selfRect = Rect.MinMaxRect(selfLocalMin.x, selfLocalMin.y, selfLocalMax.x, selfLocalMax.y);

                    if (slideInDirection == ScreenView.SlideDirection.Up)
                        slideOutAmount = Mathf.Abs(selfRect.yMin - parentRect.yMax);
                    else if (slideInDirection == ScreenView.SlideDirection.Down)
                        slideOutAmount = Mathf.Abs(selfRect.yMax - parentRect.yMin);
                    else if (slideInDirection == ScreenView.SlideDirection.Left)
                        slideOutAmount = Mathf.Abs(selfRect.xMax - parentRect.xMin);
                    else
                        slideOutAmount = Mathf.Abs(selfRect.xMin - parentRect.xMax);

                    /*bool isVertical = (slideOutDirection == ScreenView.SlideDirection.Up ||
                                       slideOutDirection == ScreenView.SlideDirection.Down);

                    if (isVertical)
                    {
                        slideOutAmount = rectTransform.localScale.y * rectTransform.GetProperSize().y * slideInPercent * 0.01f;

                        if (rectTransformParent != null)
                            slideOutAmount += Mathf.Abs(slideOutAmount - (rectTransformParent.localScale.y * rectTransformParent.GetProperSize().y * slideInPercent * 0.01f));
                    }
                    else
                    {
                        slideOutAmount = rectTransform.localScale.x * rectTransform.GetProperSize().x * slideInPercent * 0.01f;

                        if (rectTransformParent != null)
                            slideOutAmount += Mathf.Abs(slideOutAmount - (rectTransformParent.localScale.x * rectTransformParent.GetProperSize().x * slideInPercent * 0.01f));
                    }*/
                }

                var localRectPosition = rectTransform.localPosition;
                switch (slideOutDirection)
                {
                    case ScreenView.SlideDirection.Left:
                        _SlideScreenPos = new Vector2(localRectPosition.x - slideOutAmount, localRectPosition.y);
                        break;
                    case ScreenView.SlideDirection.Right:
                        _SlideScreenPos = new Vector2(localRectPosition.x + slideOutAmount, localRectPosition.y);
                        break;
                    case ScreenView.SlideDirection.Up:
                        _SlideScreenPos = new Vector2(localRectPosition.x, localRectPosition.y + slideOutAmount);
                        break;
                    case ScreenView.SlideDirection.Down:
                        _SlideScreenPos = new Vector2(localRectPosition.x, localRectPosition.y - slideOutAmount);
                        break;
                }
                if (rectTransform.parent != null)
                    _SlideScreenPos = rectTransform.parent.TransformPoint(_SlideScreenPos);
            }

            enabled = true;
            _IsTransitioning = 2;
            _TransitionCurrentTime = -1;
        }

        public virtual void TransitionOutImmediate()
        {
            InterruptAnimation();
            _IsTransitioning = 3;
            _TransitionCurrentTime = -1;
        }

        public override void Tween(string tag, System.Action<string> callback)
        {
            if (string.IsNullOrEmpty(tag))
            {
                if (callback != null)
                    callback.Invoke(tag);

                return;
            }

            UnityAction internalCallback = null;
            tag = tag.ToLower();
            if (tag.Contains("show") || tag.Contains("transitionin"))
            {
                internalCallback = callback == null ? (UnityAction)null : () =>
                {
                    if (onEndTransitionIn != null)
                        onEndTransitionIn.RemoveListener(internalCallback);
                    if (callback != null)
                        callback(tag);
                };
                //Register In Callback
                if (internalCallback != null)
                    onEndTransitionIn.AddListener(internalCallback);
                TransitionIn();
                return;
            }
            else if (tag.Contains("hide") || tag.Contains("transitionout"))
            {
                internalCallback = callback == null ? (UnityAction)null : () =>
                {
                    if (onEndTransitionOut != null)
                        onEndTransitionOut.RemoveListener(internalCallback);
                    if (callback != null)
                        callback(tag);
                };
                //Register Out Callback
                if (internalCallback != null)
                    onEndTransitionOut.AddListener(internalCallback);
                TransitionOut();
                return;
            }
            //Auto Invoke Callback because requested tag does not exist

            if (callback != null)
                callback.Invoke(tag);

        }

        #endregion

        #region Receivers

        protected virtual void HandleOnShow()
        {
            if (onEndTransitionIn != null)
                onEndTransitionIn.Invoke();
        }

        protected virtual void HandleOnHide()
        {
            if (onEndTransitionOut != null)
                onEndTransitionOut.Invoke();
        }

        #endregion

        #region Internal Helper Functions

        protected void SetupRipple()
        {
            ripple.sizeDelta = Vector2.zero;
            _CurrentRipplePos = GetRipplePosition();
            _TargetRipplePos = GetRippleTargetPosition();
            _TempRippleSize = GetRippleTargetSize();
            ripple.gameObject.SetActive(true);

            _TempRippleAnchors.FromTransform(rectTransform);
            var fitter = rectTransform.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;
        }

        protected Vector2 GetRipplePosition()
        {
            switch (m_RippleInType)
            {
                case ScreenView.RippleType.Manual:
                    return m_RippleInPosition;

                case ScreenView.RippleType.Center:
                    Vector3 rectPosition = rectTransform.GetPositionRegardlessOfPivot();
                    //return rectPosition;
                    return new Vector2(rectPosition.x + rectTransform.sizeDelta.x * 0.5f, rectPosition.y + rectTransform.sizeDelta.y * 0.5f);

                default:
                    return Input.mousePosition;
            }
        }

        protected Vector2 GetRippleTargetSize()
        {
            Vector2 size = rectTransform.GetProperSize();

            size.x *= size.x;
            size.y *= size.y;

            size.x = Mathf.Sqrt(size.x + size.y);
            size.y = size.x;

            return size;
        }

        protected Vector3 GetRippleTargetPosition()
        {
            return rectTransform.GetPositionRegardlessOfPivot();
        }

        protected internal int InterruptAnimation()
        {
            int v_processedTransition = _IsTransitioning;
            if (_IsTransitioning == 1)
            {
                if (rippleIn)
                {
                    if (gameObject != null && gameObject.activeInHierarchy)
                        rectTransform.SetParent(ripple.parent, true);
                    rectTransform.position = _TempScreenPos;

                    _TempRippleAnchors.ToTransform(rectTransform);
                    //rectTransform.anchorMin = Vector2.zero;
                    //rectTransform.anchorMax = Vector2.one;
                    //rectTransform.sizeDelta = Vector2.zero;
                    //rectTransform.anchoredPosition = Vector2.zero;
                    ripple.gameObject.SetActive(false);
                }
                if (fadeIn)
                {
                    canvasGroup.alpha = 1f;
                }
                if (scaleIn)
                {
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                }
                if (slideIn)
                {
                    rectTransform.position = _TempScreenPos;
                }
                HandleOnShow();
            }
            else if (_IsTransitioning == 2)
            {
                if (rippleOut)
                {
                    if (gameObject != null && gameObject.activeInHierarchy)
                        rectTransform.SetParent(ripple.parent, true);
                    rectTransform.position = _TempScreenPos;

                    _TempRippleAnchors.ToTransform(rectTransform);
                    //rectTransform.anchorMin = Vector2.zero;
                    //rectTransform.anchorMax = Vector2.one;
                    //rectTransform.sizeDelta = Vector2.zero;
                    //rectTransform.anchoredPosition = Vector2.zero;
                    ripple.gameObject.SetActive(false);
                }
                if (fadeOut)
                {
                    canvasGroup.alpha = 1f;
                }
                if (scaleOut)
                {
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                }
                if (slideOut)
                {
                    rectTransform.position = _TempScreenPos;
                }
                HandleOnHide();
            }

            if (_IsTransitioning > 0)
            {
                _IsTransitioning = 0;
                enabled = false;
                onInterruptAnimation.InvokeIfNotNull(v_processedTransition);
            }

            return v_processedTransition;
        }

        #endregion
    }
}