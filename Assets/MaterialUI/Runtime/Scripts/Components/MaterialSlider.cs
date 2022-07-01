using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace MaterialUI
{
    [ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Material Slider", 100)]
    public class MaterialSlider : SelectableStyleElement<MaterialSlider.SliderStyleProperty>, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
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
        private Graphic m_HandleGraphic;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_PopupText;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_ValueText;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_BackgroundGraphic;

        [SerializeField]
        private RectTransform m_PopupTransform = null;

        [SerializeField]
        private MaterialInputField m_InputField = null;
        [SerializeField]
        private RectTransform m_DotContentTransform = null;
        [SerializeField]
        private RectTransform m_SliderContentTransform = null;
        [SerializeField]
        private VectorImageData m_DotTemplateIcon = null;
        [SerializeField]
        private Graphic[] m_DotGraphics = new Graphic[0];
        [SerializeField]
        private int m_NumberOfDots = 0;

        private Slider m_Slider;
        private CanvasGroup m_CanvasGroup;
        private Canvas m_RootCanvas;
        private bool m_IsSelected;

        private int m_HandleScaleTweener;
        private int m_PopupScaleTweener;
        private int m_PopupTextColorTweener;

        #endregion

        #region Callbacks

        public Slider.SliderEvent m_OnValueChanged = new Slider.SliderEvent();

        #endregion

        #region Properties

        public float value
        {
            get { return slider != null ? slider.value : 0; }
            set
            {
                if (slider != null)
                    slider.value = value;
            }
        }

        public float normalizedValue
        {
            get { return slider != null ? slider.normalizedValue : 0; }
            set
            {
                if (slider != null)
                    slider.normalizedValue = value;
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

        public Slider.SliderEvent onValueChanged 
        { 
            get 
            { 
                return m_OnValueChanged; 
            } 
            set 
            { 
                m_OnValueChanged = value; 
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

        public RectTransform sliderHandleTransform
        {
            get { return slider != null? slider.handleRect : null; }
            set 
            { 
                if(slider != null)
                    slider.handleRect = value; 
            }
        }

        public Graphic handleGraphic
        {
            get { return m_HandleGraphic; }
            set
            {
                if (m_HandleGraphic == value)
                    return;
                m_HandleGraphic = value;
                UpdateColors();
            }
        }

        public RectTransform handleGraphicTransform
        {
            get
            {
                return m_HandleGraphic != null? m_HandleGraphic.rectTransform : null;
            }
        }

        public RectTransform popupTransform
        {
            get { return m_PopupTransform; }
            set { m_PopupTransform = value; }
        }

        public Graphic popupText
        {
            get { return m_PopupText; }
            set { m_PopupText = value; }
        }

        public Graphic valueText
        {
            get { return m_ValueText; }
            set { m_ValueText = value; }
        }

        public MaterialInputField inputField
        {
            get { return m_InputField; }
            set { m_InputField = value; }
        }

        public RectTransform fillRect
        {
            get { return slider != null? slider.fillRect : null; }
            set 
            {
                if(slider != null)
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

        public RectTransform dotContentTransform
        {
            get { return m_DotContentTransform; }
            set { m_DotContentTransform = value; }
        }

        public RectTransform sliderContentTransform
        {
            get { return m_SliderContentTransform; }
            set { m_SliderContentTransform = value; }
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

        public Slider slider
        {
            get
            {
                if (m_Slider == null)
                {
                    m_Slider = GetComponent<Slider>();
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
                if (slider != null)
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
            if(Application.isPlaying)
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
            HandleOnSliderValueChanged(slider.value);
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
            if (m_InputField != null && slider != null)
            {
                if (slider.wholeNumbers)
                {
                    m_InputField.contentType = InputField.ContentType.IntegerNumber;
                }
                else
                {
                    m_InputField.contentType = InputField.ContentType.DecimalNumber;
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
                m_Slider.onValueChanged.RemoveListener(HandleOnSliderValueChanged);

            if (m_InputField != null)
                m_InputField.onEndEdit.RemoveListener(HandleOnInputEnd);

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
                m_Slider.onValueChanged.AddListener(HandleOnSliderValueChanged);

            if (m_InputField != null)
                m_InputField.onEndEdit.AddListener(HandleOnInputEnd);

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
            TweenManager.EndTween(m_HandleScaleTweener);
            TweenManager.EndTween(m_PopupScaleTweener);
            TweenManager.EndTween(m_PopupTextColorTweener);

            if (m_HasPopup)
            {
                if (m_PopupTransform != null)
                {
                    m_PopupScaleTweener = TweenManager.TweenVector3(
                        vector3 =>
                        {
                            if (m_PopupTransform != null)
                                m_PopupTransform.localScale = vector3;
                        },
                        m_PopupTransform.localScale,
                        Vector3.one,
                        m_AnimationDuration,
                        0,
                        null,
                        false,
                        Tween.TweenType.EaseOutSept);
                }
            }

            if (handleGraphicTransform != null)
            {
                var extendedScale = Vector3.one * m_AnimationScale;
                m_HandleScaleTweener = TweenManager.TweenVector3(
                    vector3 =>
                    {
                        if (handleGraphicTransform != null)
                            handleGraphicTransform.localScale = vector3;
                    },
                    handleGraphicTransform.localScale,
                    extendedScale,
                    m_AnimationDuration,
                    0,
                    null,
                    false,
                    Tween.TweenType.SoftEaseOutQuint);
            }

            if (m_PopupText != null)
            {
                m_PopupTextColorTweener = TweenManager.TweenColor(
                    color =>
                    {
                        if (m_PopupText != null)
                            m_PopupText.color = color;
                    },
                    m_PopupText.color,
                    m_PopupText.color.WithAlpha(1f),
                    m_AnimationDuration * 0.66f,
                    m_AnimationDuration * 0.33f);
            }
        }

        protected virtual void AnimateOff()
        {
            TweenManager.EndTween(m_HandleScaleTweener);
            TweenManager.EndTween(m_PopupScaleTweener);
            TweenManager.EndTween(m_PopupTextColorTweener);

            if (m_HasPopup)
            {
                if (m_PopupTransform != null)
                {
                    m_PopupScaleTweener = TweenManager.TweenVector3(
                        vector3 =>
                        {
                            if (m_PopupTransform != null)
                                m_PopupTransform.localScale = vector3;
                        },
                        m_PopupTransform.localScale,
                        Vector3.zero,
                        m_AnimationDuration);
                }
            }

            if (handleGraphicTransform != null)
            {
                m_HandleScaleTweener = TweenManager.TweenVector3(
                    vector3 =>
                    {
                        if(handleGraphicTransform != null)
                            handleGraphicTransform.localScale = vector3;
                    },
                    handleGraphicTransform.localScale,
                    Vector3.one,
                    m_AnimationDuration,
                    0,
                    null,
                    false,
                    Tween.TweenType.EaseOutSept);
            }

            if (m_PopupText != null)
            {
                m_PopupTextColorTweener = TweenManager.TweenColor(
                    color =>
                    {
                        if (m_PopupText != null)
                            m_PopupText.color = color;
                    },
                    m_PopupText.color,
                    m_PopupText.color.WithAlpha(0f),
                    m_AnimationDuration * 0.25f);
            }
        }

        public virtual void UpdateColors()
        {
            var isInteractable = IsInteractable();
            if (m_BackgroundGraphic)
            {
                m_BackgroundGraphic.color = m_BackgroundColor;
            }
            if (m_HandleGraphic)
            {
                m_HandleGraphic.color = isInteractable ? m_EnabledColor : m_DisabledColor;
            }

            var sliderDeltaRange = GetSliderValueRange();
            var sliderRange = (int)(sliderDeltaRange * slider.normalizedValue);
            for (int i = 0; i < m_DotGraphics.Length; i++)
            {
                if (m_DotGraphics[i] == null) continue;

                if (sliderRange >= i)
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

        protected virtual void HandleOnInputEnd(string value)
        {
            if (m_InputField != null && slider != null)
            {
                float floatValue;
                if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out floatValue))
                    slider.value = floatValue;

                m_InputField.text = slider.value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        protected virtual void HandleOnSliderValueChanged(float value)
        {
            if (m_OnValueChanged != null)
                m_OnValueChanged.Invoke(value);

            var popupText = slider.value.ToString("#0.#", System.Globalization.CultureInfo.InvariantCulture);
            var valueText = slider.value.ToString("#0.##", System.Globalization.CultureInfo.InvariantCulture);

            if(m_PopupText != null)
                m_PopupText.SetGraphicText(popupText);

            if (m_ValueText != null)
                m_ValueText.SetGraphicText(valueText);

            if (m_InputField != null)
                m_InputField.text = valueText;

            UpdateColors();
        }

        #endregion

        #region BaseStyleElement Helper Classes

        [System.Serializable]
        public class SliderStyleProperty : StyleProperty
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

            public SliderStyleProperty()
            {
            }

            public SliderStyleProperty(string name, Component target, Color colorEnabled, Color colorDisabled, bool useStyleGraphic)
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
                    var slider = sender as MaterialSlider;
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
 