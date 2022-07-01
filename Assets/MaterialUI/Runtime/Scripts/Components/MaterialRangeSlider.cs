using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Kyub.UI;
using static UnityEngine.UI.Slider;
using static Kyub.UI.RangeSlider;

namespace MaterialUI
{
    [ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Material Range Slider", 100)]
    public class MaterialRangeSlider : SelectableStyleElement<MaterialRangeSlider.RangeSliderStyleProperty>, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region Private Variables

        [SerializeField]
        private bool m_Interactable = true;
        [SerializeField]
        float m_AnimationScale = 1.25f;
        [SerializeField, SerializeStyleProperty]
        float m_AnimationDuration = 0.25f;
        [SerializeField]
        private bool m_HasPopup = true;
        [SerializeField]
        private bool m_HasDots = false;

        [SerializeField, SerializeStyleProperty]
        private Color m_EnabledColor;
        [SerializeField, SerializeStyleProperty]
        private Color m_DisabledColor;
        [SerializeField, SerializeStyleProperty]
        private Color m_BackgroundColor;

        [SerializeField, SerializeStyleProperty]
        private Graphic m_HandleLowGraphic;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_PopupLowText;
        [SerializeField]
        private RectTransform m_PopupLowTransform = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_ValueLowText;
        [SerializeField]
        private MaterialInputField m_InputFieldLow = null;

        [SerializeField, SerializeStyleProperty]
        private Graphic m_HandleHighGraphic;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_PopupHighText;
        [SerializeField]
        private RectTransform m_PopupHighTransform = null;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_ValueHighText;
        [SerializeField]
        private MaterialInputField m_InputFieldHigh = null;

        [SerializeField, SerializeStyleProperty]
        private Graphic m_BackgroundGraphic;

        [SerializeField]
        private RectTransform m_SliderContentTransform = null;
        [SerializeField]
        private RectTransform m_DotContentTransform = null;
        [SerializeField]
        private VectorImageData m_DotTemplateIcon = null;
        [SerializeField]
        private Graphic[] m_DotGraphics = new Graphic[0];
        [SerializeField]
        private int m_NumberOfDots = 0;

        private RangeSlider m_Slider;
        private CanvasGroup m_CanvasGroup;
        private Canvas m_RootCanvas;
        private bool m_IsSelected;

        private int m_HandleLowScaleTweener;
        private int m_PopupLowScaleTweener;
        private int m_PopupLowTextColorTweener;

        private int m_HandleHighScaleTweener;
        private int m_PopupHighScaleTweener;
        private int m_PopupHighTextColorTweener;

        #endregion

        #region Callbacks

        public RangeSliderEvent m_OnValueChanged = new RangeSliderEvent();
        [SerializeField]
        private SliderEvent m_OnLowValueChanged = new SliderEvent();
        [SerializeField]
        private SliderEvent m_OnHighValueChanged = new SliderEvent();

        #endregion

        #region Properties

        public RangeSliderEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        public SliderEvent onLowValueChanged { get { return m_OnLowValueChanged; } set { m_OnLowValueChanged = value; } }

        public SliderEvent onHighValueChanged { get { return m_OnHighValueChanged; } set { m_OnHighValueChanged = value; } }

        public float lowValue
        {
            get { return slider != null ? slider.lowValue : 0; }
            set
            {
                if (slider != null)
                    slider.lowValue = value;
            }
        }

        public float highValue
        {
            get { return slider != null ? slider.highValue : 0; }
            set
            {
                if (slider != null)
                    slider.highValue = value;
            }
        }

        public float normalizedLowValue
        {
            get { return slider != null ? slider.normalizedLowValue : 0; }
            set
            {
                if (slider != null)
                    slider.normalizedLowValue = value;
            }
        }

        public float normalizedHighValue
        {
            get { return slider != null ? slider.normalizedHighValue : 0; }
            set
            {
                if (slider != null)
                    slider.normalizedHighValue = value;
            }
        }

        public float maxValue
        {
            get { return slider != null ? slider.maxValue : 0; }
            set
            {
                if (slider != null)
                    slider.maxValue = value;
            }
        }

        public float minValue
        {
            get { return slider != null ? slider.minValue : 0; }
            set
            {
                if (slider != null)
                    slider.minValue = value;
            }
        }

        public bool wholeNumbers
        {
            get { return slider != null ? slider.wholeNumbers : false; }
            set
            {
                if (slider != null)
                    slider.wholeNumbers = value;
            }
        }

        public float animationScale
        {
            get
            {
                return m_AnimationScale;
            }

            set
            {
                m_AnimationScale = value;
            }
        }

        public float animationDuration
        {
            get
            {
                return m_AnimationDuration;
            }

            set
            {
                m_AnimationDuration = value;
            }
        }

        public bool hasPopup
        {
            get { return m_HasPopup; }
            set { m_HasPopup = value; }
        }

        public bool hasDots
        {
            get { return m_HasDots; }
            set { m_HasDots = value; }
        }

        public Color enabledColor
        {
            get { return m_EnabledColor; }
            set
            {
                if (m_EnabledColor == value)
                    return;
                m_EnabledColor = value;

                UpdateColors();
            }
        }

        public Color disabledColor
        {
            get { return m_DisabledColor; }
            set
            {
                if (m_DisabledColor == value)
                    return;
                m_DisabledColor = value;

                UpdateColors();
            }
        }

        public Color backgroundColor
        {
            get
            {
                if (m_BackgroundGraphic != null)
                {
                    if (m_BackgroundGraphic.color != m_BackgroundColor)
                    {
                        m_BackgroundColor = m_BackgroundGraphic.color;
                    }
                }
                return m_BackgroundColor;
            }
            set
            {
                if (m_BackgroundColor == value)
                    return;
                m_BackgroundColor = value;
                UpdateColors();
            }
        }

        public RectTransform sliderLowHandleTransform
        {
            get { return slider != null ? slider.lowHandleRect : null; }
            set
            {
                if (slider != null)
                    slider.lowHandleRect = value;
            }
        }

        public RectTransform sliderHighHandleTransform
        {
            get { return slider != null ? slider.highHandleRect : null; }
            set
            {
                if (slider != null)
                    slider.highHandleRect = value;
            }
        }

        public Graphic handleLowGraphic
        {
            get { return m_HandleLowGraphic; }
            set
            {
                if (m_HandleLowGraphic == value)
                    return;
                m_HandleLowGraphic = value;
                UpdateColors();
            }
        }

        public Graphic handleHighGraphic
        {
            get { return m_HandleHighGraphic; }
            set
            {
                if (m_HandleHighGraphic == value)
                    return;
                m_HandleHighGraphic = value;
                UpdateColors();
            }
        }

        public RectTransform handleLowGraphicTransform
        {
            get
            {
                return m_HandleLowGraphic != null ? m_HandleLowGraphic.rectTransform : null;
            }
        }

        public RectTransform handleHighGraphicTransform
        {
            get
            {
                return m_HandleHighGraphic != null ? m_HandleHighGraphic.rectTransform : null;
            }
        }

        public RectTransform popupLowTransform
        {
            get { return m_PopupLowTransform; }
            set { m_PopupLowTransform = value; }
        }

        public RectTransform popupHighTransform
        {
            get { return m_PopupHighTransform; }
            set { m_PopupHighTransform = value; }
        }

        public Graphic popupLowText
        {
            get { return m_PopupLowText; }
            set { m_PopupLowText = value; }
        }

        public Graphic popupHighText
        {
            get { return m_PopupHighText; }
            set { m_PopupHighText = value; }
        }

        public Graphic valueLowText
        {
            get { return m_ValueLowText; }
            set { m_ValueLowText = value; }
        }

        public Graphic valueHighText
        {
            get { return m_ValueHighText; }
            set { m_ValueHighText = value; }
        }

        public MaterialInputField inputFieldLow
        {
            get { return m_InputFieldLow; }
            set { m_InputFieldLow = value; }
        }

        public MaterialInputField inputFieldHigh
        {
            get { return m_InputFieldHigh; }
            set { m_InputFieldHigh = value; }
        }

        public RectTransform fillRect
        {
            get { return slider != null ? slider.fillRect : null; }
            set
            {
                if (slider != null)
                    slider.fillRect = value;
            }
        }

        public Graphic backgroundGraphic
        {
            get { return m_BackgroundGraphic; }
            set
            {
                if (m_BackgroundGraphic == value)
                    return;
                m_BackgroundGraphic = value;
                UpdateColors();
            }
        }

        public RectTransform sliderContentTransform
        {
            get { return m_SliderContentTransform; }
            set { m_SliderContentTransform = value; }
        }

        public RectTransform dotContentTransform
        {
            get { return m_DotContentTransform; }
            set { m_DotContentTransform = value; }
        }

        public RectTransform rectTransform
        {
            get { return transform as RectTransform; }
        }

        [SerializeStyleProperty]
        public VectorImageData dotTemplateIcon
        {
            get { return m_DotTemplateIcon; }
            set { m_DotTemplateIcon = value; }
        }

        public RangeSlider slider
        {
            get
            {
                if (m_Slider == null)
                {
                    m_Slider = GetComponent<RangeSlider>();
                    if (m_Slider != null)
                        RegisterEvents();
                }
                return m_Slider;
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = GetComponent<CanvasGroup>();
                }
                return m_CanvasGroup;
            }
        }

        public Canvas rootCanvas
        {
            get
            {
                if (m_RootCanvas == null)
                {
                    m_RootCanvas = transform.GetRootCanvas();
                }
                return m_RootCanvas;
            }
        }

        public bool isSelected
        {
            get { return m_IsSelected; }
        }

        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                if(slider != null)
                    slider.interactable = value;

                if (canvasGroup != null)
                {
                    canvasGroup.interactable = value;
                    canvasGroup.blocksRaycasts = value;
                }
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterEvents();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterEvents();
            CancelInvoke();
            if (Application.isPlaying)
                AnimateOff();
        }

        protected override void Start()
        {
            base.Start();
            ValidateContent();
        }

        protected virtual void Update()
        {
            ValidateContent();
        }

        protected virtual void OnCanvasChanged(bool scaleChanged, bool orientationChanged)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            HandleOnSliderValueChanged(new Vector2(slider.lowValue, slider.highValue));
        }

        protected bool _isChangingCanvasGroup = false;
        protected override void OnCanvasGroupChanged()
        {
            if (!_isChangingCanvasGroup && !Kyub.Performance.SustainedPerformanceManager.IsSettingLowPerformance)
            {
                try
                {
                    _isChangingCanvasGroup = true;
                    base.OnCanvasGroupChanged();
                    UpdateColors();
                }
                finally
                {
                    _isChangingCanvasGroup = false;
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidateDelayed()
        {
            LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
            ValidateContent();
            UpdateColors();
        }
#endif

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInvoking("AnimateOn"))
                Invoke("AnimateOn", 0);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInvoking("AnimateOff"))
                Invoke("AnimateOff", 0);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            SnapTo();
            AnimateOn();
            m_IsSelected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            AnimateOff();
            m_IsSelected = false;
        }

        #endregion

        #region Other Functions

        protected virtual void ValidateContent()
        {
            if (slider != null)
            {
                if (slider.wholeNumbers)
                {
                    if(m_InputFieldLow)
                        m_InputFieldLow.contentType = InputField.ContentType.IntegerNumber;
                    if (m_InputFieldHigh)
                        m_InputFieldHigh.contentType = InputField.ContentType.IntegerNumber;
                }
                else
                {
                    if (m_InputFieldLow)
                        m_InputFieldLow.contentType = InputField.ContentType.DecimalNumber;
                    if (m_InputFieldHigh)
                        m_InputFieldHigh.contentType = InputField.ContentType.DecimalNumber;
                }
            }

            //Force Upgrade Dots
            if (slider != null && slider.wholeNumbers && m_HasDots)
            {
                if (m_NumberOfDots != GetSliderValueRange())
                {
                    RebuildDots();
                }
            }
            else if (m_NumberOfDots > 0)
            {
                DestroyDots();
            }
        }

        protected virtual void UnregisterEvents()
        {
            if (m_Slider != null)
            {
                m_Slider.onValueChanged.RemoveListener(HandleOnSliderValueChanged);
                m_Slider.onLowValueChanged.RemoveListener(HandleOnSliderLowValueChanged);
                m_Slider.onHighValueChanged.RemoveListener(HandleOnSliderHighValueChanged);
            }

            if (m_InputFieldLow != null)
                m_InputFieldLow.onEndEdit.RemoveListener(HandleOnInputLowEnd);
            if (m_InputFieldHigh != null)
                m_InputFieldHigh.onEndEdit.RemoveListener(HandleOnInputHighEnd);

            var scaler = rootCanvas != null ? rootCanvas.GetComponent<MaterialCanvasScaler>() : null;
            if (scaler != null)
            {
                scaler.onCanvasAreaChanged.RemoveListener(OnCanvasChanged);
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (m_Slider != null)
            {
                m_Slider.onValueChanged.AddListener(HandleOnSliderValueChanged);
                m_Slider.onLowValueChanged.AddListener(HandleOnSliderLowValueChanged);
                m_Slider.onHighValueChanged.AddListener(HandleOnSliderHighValueChanged);
            }

            if (m_InputFieldLow != null)
                m_InputFieldLow.onEndEdit.AddListener(HandleOnInputLowEnd);
            if (m_InputFieldHigh != null)
                m_InputFieldHigh.onEndEdit.AddListener(HandleOnInputHighEnd);

            var scaler = rootCanvas != null ? rootCanvas.GetComponent<MaterialCanvasScaler>() : null;
            if (scaler != null)
            {
                scaler.onCanvasAreaChanged.AddListener(OnCanvasChanged);
            }
        }

        protected virtual void DestroyDots()
        {
            for (int i = 0; i < m_DotGraphics.Length; i++)
            {
                if (m_DotGraphics[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(m_DotGraphics[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(m_DotGraphics[i].gameObject);
                    }
                }
            }

            m_NumberOfDots = -1;

            m_DotGraphics = new Graphic[0];
        }

        protected virtual void RebuildDots()
        {
            if (m_DotContentTransform != null)
            {
                m_NumberOfDots = GetSliderValueRange();
                float dotDistance = 1 / (float)m_NumberOfDots;

                var previousDots = m_DotGraphics;
                m_DotGraphics = new Graphic[m_NumberOfDots + 1];

                for (int i = 0; i < m_DotGraphics.Length; i++)
                {
                    m_DotGraphics[i] = previousDots != null && previousDots.Length > i ? previousDots[i] : null;
                    if (m_DotGraphics[i] == null)
                        m_DotGraphics[i] = CreateDot();
                    m_DotGraphics[i].rectTransform.SetAnchorX(dotDistance * i, dotDistance * i);
                }

                UpdateColors();
            }
            else
            {
                DestroyDots();
            }
        }

        protected virtual int GetSliderValueRange()
        {
            return Mathf.RoundToInt(slider.maxValue - slider.minValue);
        }

        private Graphic CreateDot()
        {
            RectTransform dot = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.sliderDot, m_DotContentTransform).GetComponent<RectTransform>();
            dot.anchoredPosition = Vector2.zero;
            dot.anchoredPosition = new Vector2(0f, 0.5f);
            return dot.GetComponent<Graphic>();
        }

        protected virtual void AnimateOn()
        {
            TweenManager.EndTween(m_HandleLowScaleTweener);
            TweenManager.EndTween(m_PopupLowScaleTweener);
            TweenManager.EndTween(m_PopupLowTextColorTweener);

            TweenManager.EndTween(m_HandleHighScaleTweener);
            TweenManager.EndTween(m_PopupHighScaleTweener);
            TweenManager.EndTween(m_PopupHighTextColorTweener);

            if(slider != null)
                CheckIfRequireInvert(slider.lowHandleRect, slider.highHandleRect);

            var interactionState = slider != null? slider.currentInteractionState : RangeSlider.InteractionStateEnum.None;

            if (interactionState == RangeSlider.InteractionStateEnum.Low)
            {
                if (m_HasPopup)
                {
                    if (m_PopupLowTransform != null)
                    {
                        var lowTransform = m_PopupLowTransform;
                        m_PopupLowScaleTweener = TweenManager.TweenVector3(
                            vector3 =>
                            {
                                if (lowTransform != null)
                                    lowTransform.localScale = vector3;
                            },
                            lowTransform.localScale,
                            Vector3.one,
                            m_AnimationDuration,
                            0,
                            null,
                            false,
                            Tween.TweenType.EaseOutSept);
                    }
                }

                if (handleLowGraphicTransform != null)
                {
                    var extendedScale = Vector3.one * m_AnimationScale;
                    var lowTransform = handleLowGraphicTransform;
                    m_HandleHighScaleTweener = TweenManager.TweenVector3(
                        vector3 =>
                        {
                            if (lowTransform != null)
                                lowTransform.localScale = vector3;
                        },
                        lowTransform.localScale,
                        extendedScale,
                        m_AnimationDuration,
                        0,
                        null,
                        false,
                        Tween.TweenType.SoftEaseOutQuint);
                }

                if (m_PopupLowText != null)
                {
                    var lowTransformText = m_PopupLowText;
                    m_PopupLowTextColorTweener = TweenManager.TweenColor(
                        color =>
                        {
                            if (lowTransformText != null)
                                lowTransformText.color = color;
                        },
                        lowTransformText.color,
                        lowTransformText.color.WithAlpha(1f),
                        m_AnimationDuration * 0.66f,
                        m_AnimationDuration * 0.33f);
                }
            }
            else if (interactionState == RangeSlider.InteractionStateEnum.High)
            {
                if (m_HasPopup)
                {
                    if (m_PopupHighTransform != null)
                    {
                        var highTransform = m_PopupHighTransform;
                        m_PopupHighScaleTweener = TweenManager.TweenVector3(
                            vector3 =>
                            {
                                if (highTransform != null)
                                    highTransform.localScale = vector3;
                            },
                            highTransform.localScale,
                            Vector3.one,
                            m_AnimationDuration,
                            0,
                            null,
                            false,
                            Tween.TweenType.EaseOutSept);
                    }
                }

                if (handleHighGraphicTransform != null)
                {
                    var extendedScale = Vector3.one * m_AnimationScale;
                    var highTransform = handleHighGraphicTransform;
                    m_HandleHighScaleTweener = TweenManager.TweenVector3(
                        vector3 =>
                        {
                            if (highTransform != null)
                                highTransform.localScale = vector3;
                        },
                        highTransform.localScale,
                        extendedScale,
                        m_AnimationDuration,
                        0,
                        null,
                        false,
                        Tween.TweenType.SoftEaseOutQuint);
                }

                if (m_PopupHighText != null)
                {
                    var highTransformText = m_PopupHighText;
                    m_PopupHighTextColorTweener = TweenManager.TweenColor(
                        color =>
                        {
                            if (highTransformText != null)
                                highTransformText.color = color;
                        },
                        highTransformText.color,
                        highTransformText.color.WithAlpha(1f),
                        m_AnimationDuration * 0.66f,
                        m_AnimationDuration * 0.33f);
                }
            }
        }

        protected virtual void AnimateOff()
        {
            TweenManager.EndTween(m_HandleLowScaleTweener);
            TweenManager.EndTween(m_PopupLowScaleTweener);
            TweenManager.EndTween(m_PopupLowTextColorTweener);

            TweenManager.EndTween(m_HandleHighScaleTweener);
            TweenManager.EndTween(m_PopupHighScaleTweener);
            TweenManager.EndTween(m_PopupHighTextColorTweener);

            if (slider != null)
                CheckIfRequireInvert(slider.lowHandleRect, slider.highHandleRect);

            if (m_HasPopup)
            {
                if (m_PopupLowTransform != null)
                {
                    var lowTransform = m_PopupLowTransform;
                    m_PopupLowScaleTweener = TweenManager.TweenVector3(
                        vector3 =>
                        {
                            if (lowTransform != null)
                                lowTransform.localScale = vector3;
                        },
                        lowTransform.localScale,
                        Vector3.zero,
                        m_AnimationDuration);
                }
            }

            if (handleLowGraphicTransform != null)
            {
                var lowTransform = handleLowGraphicTransform;
                m_HandleLowScaleTweener = TweenManager.TweenVector3(
                    vector3 =>
                    {
                        if(lowTransform != null)
                            lowTransform.localScale = vector3;
                    },
                    lowTransform.localScale,
                    Vector3.one,
                    m_AnimationDuration,
                    0,
                    null,
                    false,
                    Tween.TweenType.EaseOutSept);
            }

            if (m_PopupLowText != null)
            {
                var lowTransformText = m_PopupLowText;
                m_PopupLowTextColorTweener = TweenManager.TweenColor(
                    color =>
                    {
                        if (lowTransformText != null)
                            lowTransformText.color = color;
                    },
                    lowTransformText.color,
                    lowTransformText.color.WithAlpha(0f),
                    m_AnimationDuration * 0.25f);
            }

            if (m_HasPopup)
            {
                if (m_PopupHighTransform != null)
                {
                    var highTransform = m_PopupHighTransform;
                    m_PopupHighScaleTweener = TweenManager.TweenVector3(
                        vector3 =>
                        {
                            if (highTransform != null)
                                highTransform.localScale = vector3;
                        },
                        highTransform.localScale,
                        Vector3.zero,
                        m_AnimationDuration);
                }
            }

            if (handleHighGraphicTransform != null)
            {
                var highTransform = handleHighGraphicTransform;
                m_HandleHighScaleTweener = TweenManager.TweenVector3(
                    vector3 =>
                    {
                        if(highTransform != null)
                            highTransform.localScale = vector3;
                    },
                    highTransform.localScale,
                    Vector3.one,
                    m_AnimationDuration,
                    0,
                    null,
                    false,
                    Tween.TweenType.EaseOutSept);
            }

            if (m_PopupHighText != null)
            {
                var highTransformText = m_PopupHighText;
                m_PopupHighTextColorTweener = TweenManager.TweenColor(
                    color =>
                    {
                        if (highTransformText != null)
                            highTransformText.color = color;
                    },
                    highTransformText.color,
                    highTransformText.color.WithAlpha(0f),
                    m_AnimationDuration * 0.25f);
            }
        }

        protected virtual void CheckIfRequireInvert(Transform lowTransformParent, Transform highTransformParent)
        {
            var newHandleGraphic = m_HandleHighGraphic;
            var newPopupText = m_PopupHighText;
            var newPopupTransform = m_PopupHighTransform;
            var newValueText = m_ValueHighText;

            if (m_HandleHighGraphic != null && m_HandleHighGraphic.transform.IsChildOf(lowTransformParent))
                m_HandleHighGraphic = m_HandleLowGraphic;
            if (m_PopupHighText != null && m_PopupHighText.transform.IsChildOf(lowTransformParent))
                m_PopupHighText = m_PopupLowText;
            if (m_PopupHighTransform != null && m_PopupHighTransform.transform.IsChildOf(lowTransformParent))
                m_PopupHighTransform = m_PopupLowTransform;
            if (m_ValueHighText != null && m_ValueHighText.transform.IsChildOf(lowTransformParent))
                m_ValueHighText = m_ValueLowText;

            if (m_HandleLowGraphic != null && m_HandleLowGraphic.transform.IsChildOf(highTransformParent))
                m_HandleLowGraphic = newHandleGraphic;
            if (m_PopupLowText != null && m_PopupLowText.transform.IsChildOf(highTransformParent))
                m_PopupLowText = newPopupText;
            if (m_PopupLowTransform != null && m_PopupLowTransform.transform.IsChildOf(highTransformParent))
                m_PopupLowTransform = newPopupTransform;
            if (m_ValueLowText != null && m_ValueLowText.transform.IsChildOf(highTransformParent))
                m_ValueLowText = newValueText;
        }

        public virtual void UpdateColors()
        {
            var isInteractable = IsInteractable();
            if (m_BackgroundGraphic)
            {
                m_BackgroundGraphic.color = m_BackgroundColor;
            }
            if (m_HandleLowGraphic)
            {
                m_HandleLowGraphic.color = isInteractable ? m_EnabledColor : m_DisabledColor;
            }
            if (m_HandleHighGraphic)
            {
                m_HandleHighGraphic.color = isInteractable ? m_EnabledColor : m_DisabledColor;
            }

            var sliderDeltaRange = GetSliderValueRange();
            var sliderRangeLow = (int)(sliderDeltaRange * slider.normalizedLowValue);
            var sliderRangeHigh = (int)(sliderDeltaRange * slider.normalizedHighValue);

            for (int i = 0; i < m_DotGraphics.Length; i++)
            {
                if (m_DotGraphics[i] == null) continue;

                if (i >= sliderRangeLow  && i <= sliderRangeHigh)
                {
                    m_DotGraphics[i].color = isInteractable ? m_EnabledColor : m_DisabledColor;
                }
                else
                {
                    m_DotGraphics[i].color = m_BackgroundColor;
                }
            }
            RefreshVisualStyles(false);
        }

        public override void RefreshVisualStyles(bool canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(canAnimate, m_AnimationDuration);
        }

        public virtual bool IsInteractable()
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
            return interactable;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnInputLowEnd(string value)
        {
            if (m_InputFieldLow != null && slider != null)
            {
                float floatValue;
                if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out floatValue))
                    slider.lowValue = floatValue;

                m_InputFieldLow.text = slider.lowValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        protected virtual void HandleOnInputHighEnd(string value)
        {
            if (m_InputFieldHigh != null && slider != null)
            {
                float floatValue;
                if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out floatValue))
                    slider.highValue = floatValue;

                m_InputFieldHigh.text = slider.highValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        protected virtual void HandleOnSliderValueChanged(Vector2 value)
        {
            if (m_OnValueChanged != null)
                m_OnValueChanged.Invoke(value);

            if (slider != null)
                CheckIfRequireInvert(slider.lowHandleRect, slider.highHandleRect);

            var popupLowText = slider.lowValue.ToString("#0.#", System.Globalization.CultureInfo.InvariantCulture);
            var valueLowText = slider.lowValue.ToString("#0.##", System.Globalization.CultureInfo.InvariantCulture);

            if (m_PopupLowText != null)
                m_PopupLowText.SetGraphicText(popupLowText);

            if (m_ValueLowText != null)
                m_ValueLowText.SetGraphicText(valueLowText);

            if (m_InputFieldLow != null)
                m_InputFieldLow.text = valueLowText;

            var popupHighText = slider.highValue.ToString("#0.#", System.Globalization.CultureInfo.InvariantCulture);
            var valueHighText = slider.highValue.ToString("#0.##", System.Globalization.CultureInfo.InvariantCulture);

            if (m_PopupHighText != null)
                m_PopupHighText.SetGraphicText(popupHighText);

            if (m_ValueHighText != null)
                m_ValueHighText.SetGraphicText(valueHighText);

            if (m_InputFieldHigh != null)
                m_InputFieldHigh.text = valueHighText;

            UpdateColors();
        }

        protected virtual void HandleOnSliderHighValueChanged(float low)
        {
            if (m_OnLowValueChanged != null)
                m_OnLowValueChanged.Invoke(low);
        }

        protected virtual void HandleOnSliderLowValueChanged(float high)
        {
            if (m_OnHighValueChanged != null)
                m_OnHighValueChanged.Invoke(high);
        }

        #endregion

        #region BaseStyleElement Helper Classes

        [System.Serializable]
        public class RangeSliderStyleProperty : StyleProperty
        {
            #region Private Variables

            [SerializeField, SerializeStyleProperty]
            protected Color m_colorEnabled = Color.white;
            [SerializeField, SerializeStyleProperty]
            protected Color m_colorDisabled = Color.gray;

            #endregion

            #region Public Properties

            public Color ColorEnabled
            {
                get
                {
                    return m_colorEnabled;
                }

                set
                {
                    m_colorEnabled = value;
                }
            }


            public Color ColorDisabled
            {
                get
                {
                    return m_colorDisabled;
                }

                set
                {
                    m_colorDisabled = value;
                }
            }

            #endregion

            #region Constructor

            public RangeSliderStyleProperty()
            {
            }

            public RangeSliderStyleProperty(string name, Component target, Color colorEnabled, Color colorDisabled, bool useStyleGraphic)
            {
                m_target = target != null ? target.transform : null;
                m_name = name;
                m_colorEnabled = colorEnabled;
                m_colorDisabled = colorDisabled;
                m_useStyleGraphic = useStyleGraphic;
            }

            #endregion

            #region Helper Functions

            public override void Tween(BaseStyleElement sender, bool canAnimate, float animationDuration)
            {
                TweenManager.EndTween(_tweenId);

                var graphic = GetTarget<Graphic>();
                if (graphic != null)
                {
                    var slider = sender as MaterialRangeSlider;
                    var isInteractable = slider != null ? slider.IsInteractable() : true;

                    var endColor = !isInteractable ? m_colorDisabled : m_colorEnabled;
                    if (canAnimate && Application.isPlaying)
                    {
                        _tweenId = TweenManager.TweenColor(
                                (color) =>
                                {
                                    if (graphic != null)
                                        graphic.color = color;
                                },
                                graphic.color,
                                endColor,
                                animationDuration
                            );
                    }
                    else
                    {
                        graphic.color = endColor;
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
