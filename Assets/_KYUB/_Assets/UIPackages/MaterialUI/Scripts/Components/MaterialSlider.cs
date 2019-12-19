//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

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
    public class MaterialSlider : StyleElement<MaterialSlider.SliderStyleProperty>, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler, ILayoutGroup, ILayoutElement
    {
        #region Private Variables

        [SerializeField, SerializeStyleProperty]
        float m_AnimationDuration = 0.25f;
        [SerializeField]
        private bool m_HasPopup = true;
        [SerializeField]
        private bool m_HasDots = true;

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
        private RectTransform m_SliderHandleTransform = null;
        [SerializeField]
        private RectTransform m_PopupTransform = null;

        [SerializeField]
        private MaterialInputField m_InputField = null;
        [SerializeField]
        private RectTransform m_FillTransform = null;
        [SerializeField]
        private RectTransform m_LeftContentTransform = null;
        [SerializeField]
        private RectTransform m_RightContentTransform = null;
        [SerializeField]
        private RectTransform m_SliderContentTransform = null;
        [SerializeField]
        private RectTransform m_RectTransform = null;
        [SerializeField]
        private VectorImageData m_DotTemplateIcon = null;
        [SerializeField]
        private Graphic[] m_DotGraphics = new Graphic[0];
        [SerializeField]
        private int m_NumberOfDots = 0;
        [SerializeField]
        private bool m_HasManualPreferredWidth = false;
        [SerializeField]
        private float m_ManualPreferredWidth = 200f;
        [SerializeField]
        private bool m_Interactable = true;
        [SerializeField]
        private bool m_LowLeftDisabledOpacity = false;
        [SerializeField]
        private bool m_LowRightDisabledOpacity = false;
        [SerializeField]
        private CanvasGroup m_LeftCanvasGroup = null;
        [SerializeField]
        private CanvasGroup m_RightCanvasGroup = null;

        private RectTransform m_HandleGraphicTransform;
        private RectTransform m_FillAreaTransform;
        private Slider m_Slider;
        private CanvasGroup m_CanvasGroup;
        private Canvas m_RootCanvas;
        private bool m_IsSelected;

        private int m_HandleSizeTweener;
        private int m_PopupScaleTweener;
        private int m_HandleAnchorMinTweener;
        private int m_HandleAnchorMaxTweener;
        private int m_HandlePositionYTweener;
        private int m_PopupTextColorTweener;

        private DrivenRectTransformTracker m_Tracker = new DrivenRectTransformTracker();

        private float m_Width;
        private float m_Height;
        private float m_LeftWidth;
        private float m_RightWidth;
        private float m_LastSliderValue;
        private float m_CurrentInputValue;

        private MaterialUIScaler scaler;

        #endregion

        #region Properties

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
                m_EnabledColor = value;

                if (m_HandleGraphic)
                {
                    m_HandleGraphic.color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                }

                for (int i = 0; i < m_DotGraphics.Length; i++)
                {
                    if (m_DotGraphics[i] == null) continue;

                    if (slider.value > i)
                    {
                        m_DotGraphics[i].color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                    }
                    else
                    {
                        m_DotGraphics[i].color = m_BackgroundColor;
                    }
                }
            }
        }

        public Color disabledColor
        {
            get { return m_DisabledColor; }
            set
            {
                m_DisabledColor = value;

                if (m_HandleGraphic)
                {
                    m_HandleGraphic.color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                }

                for (int i = 0; i < m_DotGraphics.Length; i++)
                {
                    if (m_DotGraphics[i] == null) continue;

                    if (slider.value > i)
                    {
                        m_DotGraphics[i].color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                    }
                    else
                    {
                        m_DotGraphics[i].color = m_BackgroundColor;
                    }
                }
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
                m_BackgroundColor = value;
                if (m_BackgroundGraphic != null)
                {
                    m_BackgroundGraphic.color = m_BackgroundColor;
                }
            }
        }

        public RectTransform sliderHandleTransform
        {
            get { return m_SliderHandleTransform; }
            set { m_SliderHandleTransform = value; }
        }

        public Graphic handleGraphic
        {
            get { return m_HandleGraphic; }
            set
            {
                m_HandleGraphic = value;
                if (m_HandleGraphic)
                {
                    m_HandleGraphic.color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                }
            }
        }

        public RectTransform handleGraphicTransform
        {
            get
            {
                if (m_HandleGraphicTransform == null)
                {
                    if (m_HandleGraphic != null)
                    {
                        m_HandleGraphicTransform = m_HandleGraphic.rectTransform;
                    }
                }
                return m_HandleGraphicTransform;
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

        public RectTransform fillTransform
        {
            get { return m_FillTransform; }
            set { m_FillTransform = value; }
        }

        public Graphic backgroundGraphic
        {
            get { return m_BackgroundGraphic; }
            set
            {
                m_BackgroundGraphic = value;
                if (m_BackgroundGraphic != null)
                {
                    m_BackgroundGraphic.color = m_BackgroundColor;
                }
            }
        }

        public RectTransform leftContentTransform
        {
            get { return m_LeftContentTransform; }
            set { m_LeftContentTransform = value; }
        }

        public RectTransform rightContentTransform
        {
            get { return m_RightContentTransform; }
            set { m_RightContentTransform = value; }
        }

        public RectTransform sliderContentTransform
        {
            get { return m_SliderContentTransform; }
            set { m_SliderContentTransform = value; }
        }

        public RectTransform rectTransform
        {
            get { return m_RectTransform; }
            set { m_RectTransform = value; }
        }

        [SerializeStyleProperty]
        public VectorImageData dotTemplateIcon
        {
            get { return m_DotTemplateIcon; }
            set { m_DotTemplateIcon = value; }
        }

        public RectTransform fillAreaTransform
        {
            get
            {
                if (m_FillAreaTransform == null)
                {
                    if (m_FillTransform != null)
                    {
                        m_FillAreaTransform = m_FillTransform.parent as RectTransform;
                    }
                }

                return m_FillAreaTransform;
            }
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

        public bool hasManualPreferredWidth
        {
            get { return m_HasManualPreferredWidth; }
            set
            {
                m_HasManualPreferredWidth = value;
                CalculateLayoutInputHorizontal();
                SetLayoutHorizontal();
            }
        }

        public float manualPreferredWidth
        {
            get { return m_ManualPreferredWidth; }
            set
            {
                m_ManualPreferredWidth = value;
                CalculateLayoutInputHorizontal();
                SetLayoutHorizontal();
            }
        }

        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                slider.interactable = value;
                canvasGroup.interactable = value;
                canvasGroup.blocksRaycasts = value;
                if (m_InputField)
                {
                    m_InputField.GetComponent<MaterialInputField>().interactable = value;
                }
            }
        }
        public bool lowLeftDisabledOpacity
        {
            get { return m_LowLeftDisabledOpacity; }
            set
            {
                m_LowLeftDisabledOpacity = value;

                if (m_LeftContentTransform)
                {
                    leftCanvasGroup.alpha = m_LowLeftDisabledOpacity ? (m_Interactable ? 1f : 0.5f) : 1f;
                }
            }
        }

        public bool lowRightDisabledOpacity
        {
            get { return m_LowRightDisabledOpacity; }
            set
            {
                m_LowRightDisabledOpacity = value;

                if (m_RightContentTransform)
                {
                    rightCanvasGroup.alpha = m_LowRightDisabledOpacity ? (m_Interactable ? 1f : 0.5f) : 1f;
                }
            }
        }

        public CanvasGroup leftCanvasGroup
        {
            get
            {
                if (m_LeftCanvasGroup == null)
                {
                    if (m_LeftContentTransform != null)
                    {
                        m_LeftCanvasGroup = m_LeftContentTransform.gameObject.GetAddComponent<CanvasGroup>();
                    }
                }
                return m_LeftCanvasGroup;
            }
        }

        public CanvasGroup rightCanvasGroup
        {
            get
            {
                if (m_RightCanvasGroup == null)
                {
                    if (m_RightContentTransform != null)
                    {
                        m_RightCanvasGroup = m_RightContentTransform.gameObject.GetAddComponent<CanvasGroup>();
                    }
                }
                return m_RightCanvasGroup;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            if(slider != null)
                RegisterEvents();
            base.Awake();
        }

        protected override void OnEnable()
        {
            SetTracker();

            scaler = MaterialUIScaler.GetRootScaler(transform);

            if (scaler != null)
            {
                scaler.onCanvasAreaChanged.AddListener(OnCanvasChanged);
            }
        }

        protected override void OnDisable()
        {
            if (scaler != null)
            {
                scaler.onCanvasAreaChanged.RemoveListener(OnCanvasChanged);
            }

            m_Tracker.Clear();
        }

        private void OnCanvasChanged(bool scaleChanged, bool orientationChanged)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            OnSliderValueChanged(slider.value);
        }

        protected override void Start()
        {
            if (!Application.isPlaying)
            {
                SetTracker();
            }

            if (m_InputField != null)
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

#if UNITY_EDITOR
            m_LastSliderValue = slider.value;
#endif
        }

        protected override void OnDestroy()
        {
            UnregisterEvents();
            base.OnDestroy();
        }

        protected virtual void Update()
        {
            if (TweenManager.TweenIsActive(m_HandleAnchorMinTweener))
            {
                m_FillTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, fillAreaTransform.rect.width * handleGraphicTransform.anchorMin.x);
            }

            if (m_InputField != null)
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

            if (slider.wholeNumbers && m_HasDots)
            {
                if (m_NumberOfDots != SliderValueRange())
                {
                    RebuildDots();
                }
                for (int i = 0; i < m_DotGraphics.Length; i++)
                {
                    if (slider.value > i)
                    {
                        m_DotGraphics[i].color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                    }
                    else
                    {
                        m_DotGraphics[i].color = m_BackgroundColor;
                    }
                }
            }
            else
            {
                DestroyDots();
            }

            if (m_HandleGraphic)
            {
                m_HandleGraphic.color = m_Interactable ? m_EnabledColor : m_DisabledColor;
            }

#if UNITY_EDITOR
            if (Application.isPlaying) return;

            OnSliderValueChanged(m_LastSliderValue);
            m_LastSliderValue = slider.value;
#endif
        }

#if UNITY_EDITOR
        protected override void OnValidateDelayed()
        {
            LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            SetLayoutHorizontal();
        }
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            SetLayoutHorizontal();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateOn();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateOff();
        }

        public void OnSelect(BaseEventData eventData)
        {
            AnimateOn();
            m_IsSelected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            AnimateOff();
            m_IsSelected = false;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnSliderValueChanged(float value)
        {
            OnSliderValueChanged(value);
        }

        #endregion

        #region Other Functions

        public void UnregisterEvents()
        {
            if (m_Slider != null)
                m_Slider.onValueChanged.RemoveListener(HandleOnSliderValueChanged);
        }

        public void RegisterEvents()
        {
            UnregisterEvents();
            if (m_Slider != null)
                m_Slider.onValueChanged.AddListener(HandleOnSliderValueChanged);
        }

        private void SetTracker()
        {
            m_Tracker.Clear();
            m_Tracker.Add(this, m_SliderContentTransform, DrivenTransformProperties.AnchorMinX);
            m_Tracker.Add(this, m_SliderContentTransform, DrivenTransformProperties.AnchorMaxX);
            m_Tracker.Add(this, m_SliderContentTransform, DrivenTransformProperties.AnchoredPositionX);
            m_Tracker.Add(this, m_SliderContentTransform, DrivenTransformProperties.SizeDeltaX);
        }

        private void DestroyDots()
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

        private void RebuildDots()
        {
            DestroyDots();

            m_NumberOfDots = SliderValueRange();
            float dotDistance = 1 / (float)m_NumberOfDots;

            m_DotGraphics = new Graphic[m_NumberOfDots + 1];

            for (int i = 0; i < m_DotGraphics.Length; i++)
            {
                m_DotGraphics[i] = CreateDot();
                m_DotGraphics[i].rectTransform.SetAnchorX(dotDistance * i, dotDistance * i);

                if (slider.value > i)
                {
                    m_DotGraphics[i].color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                }
                else
                {
                    m_DotGraphics[i].color = m_BackgroundColor;
                }
            }
        }

        private int SliderValueRange()
        {
            return Mathf.RoundToInt(slider.maxValue - slider.minValue);
        }

        private Graphic CreateDot()
        {
            RectTransform dot = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.sliderDot, m_SliderContentTransform).GetComponent<RectTransform>();
            dot.SetSiblingIndex(1);
            dot.anchoredPosition = Vector2.zero;
            dot.anchoredPosition = new Vector2(0f, 0.5f);
            return dot.GetComponent<Graphic>();
        }

        private void AnimateOn()
        {
            TweenManager.EndTween(m_HandleSizeTweener);
            TweenManager.EndTween(m_PopupScaleTweener);
            TweenManager.EndTween(m_HandlePositionYTweener);
            TweenManager.EndTween(m_PopupTextColorTweener);

            if (m_HasPopup)
            {
                m_HandleSizeTweener = TweenManager.TweenVector2(vector2 => handleGraphicTransform.sizeDelta = vector2,
                    handleGraphicTransform.sizeDelta, new Vector2(38, 38), m_AnimationDuration, 0f, null, false,
                    Tween.TweenType.SoftEaseOutQuint);

                m_HandlePositionYTweener = TweenManager.TweenFloat(
                    f => m_HandleGraphicTransform.anchoredPosition = new Vector2(m_HandleGraphicTransform.anchoredPosition.x, f),
                        m_HandleGraphicTransform.anchoredPosition.y, slider.wholeNumbers && m_HasDots ? 36 : 30,
                        m_AnimationDuration, 0, null, false, Tween.TweenType.EaseOutSept);

                m_PopupScaleTweener = TweenManager.TweenVector3(vector3 => m_PopupTransform.localScale = vector3,
                    m_PopupTransform.localScale, Vector3.one, m_AnimationDuration, 0, null, false, Tween.TweenType.EaseOutSept);
            }
            else
            {
                m_HandleSizeTweener = TweenManager.TweenVector2(vector2 => handleGraphicTransform.sizeDelta = vector2,
                    handleGraphicTransform.sizeDelta, new Vector2(24, 24), m_AnimationDuration, 0, null, false, Tween.TweenType.SoftEaseOutQuint);
            }

            m_PopupTextColorTweener = TweenManager.TweenColor(color => m_PopupText.color = color, () => m_PopupText.color,
               () => m_PopupText.color.WithAlpha(1f), m_AnimationDuration * 0.66f, m_AnimationDuration * 0.33f);
        }

        private void AnimateOff()
        {
            TweenManager.EndTween(m_HandleSizeTweener);
            TweenManager.EndTween(m_PopupScaleTweener);
            TweenManager.EndTween(m_HandlePositionYTweener);
            TweenManager.EndTween(m_PopupTextColorTweener);

            if (m_HasPopup)
            {
                m_HandlePositionYTweener =
                    TweenManager.TweenFloat(
                        f => m_HandleGraphicTransform.anchoredPosition = new Vector2(m_HandleGraphicTransform.anchoredPosition.x, f),
                        m_HandleGraphicTransform.anchoredPosition.y,
                        MaterialUIScaler.GetRootScaler(transform).canvas.pixelPerfect ? 1f : 0f, m_AnimationDuration, 0, null, false, Tween.TweenType.EaseOutCubed);

                m_PopupScaleTweener = TweenManager.TweenVector3(vector3 => m_PopupTransform.localScale = vector3,
                    m_PopupTransform.localScale, Vector3.zero, m_AnimationDuration);
            }

            m_HandleSizeTweener = TweenManager.TweenVector2(vector2 => handleGraphicTransform.sizeDelta = vector2,
                    handleGraphicTransform.sizeDelta, new Vector2(16, 16), m_AnimationDuration, 0, null, false,
                    Tween.TweenType.EaseOutSept);

            m_PopupTextColorTweener = TweenManager.TweenColor(color => m_PopupText.color = color, m_PopupText.color,
                    m_PopupText.color.WithAlpha(0f), m_AnimationDuration * 0.25f);
        }

        public void OnInputChange(string value)
        {
            float floatValue;
            if (float.TryParse(value, out floatValue))
            {
                m_CurrentInputValue = floatValue;
                if (floatValue >= slider.minValue && floatValue <= slider.maxValue)
                {
                    slider.value = floatValue;
                }
            }
        }

        public void OnInputEnd()
        {
            if (m_InputField != null)
            {
                slider.value = m_CurrentInputValue;
                m_InputField.text = slider.value.ToString();
            }
        }

        public void OnSliderValueChanged(float value)
        {
            TweenManager.EndTween(m_HandleAnchorMinTweener);
            TweenManager.EndTween(m_HandleAnchorMaxTweener);

            if (slider.wholeNumbers && SliderValueRange() < 100 && Application.isPlaying)
            {
                m_HandleAnchorMinTweener = TweenManager.TweenFloat(
                        f => handleGraphicTransform.anchorMin = new Vector2(f, handleGraphicTransform.anchorMin.y),
                        handleGraphicTransform.anchorMin.x, m_Slider.handleRect.anchorMin.x, m_AnimationDuration * 0.5f,
                        0, null, false, Tween.TweenType.EaseOutSept);

                m_HandleAnchorMaxTweener = TweenManager.TweenFloat(
                        f => handleGraphicTransform.anchorMax = new Vector2(f, handleGraphicTransform.anchorMax.y),
                        handleGraphicTransform.anchorMax.x, m_Slider.handleRect.anchorMax.x, m_AnimationDuration * 0.5f,
                        0, null, false, Tween.TweenType.EaseOutSept);
            }
            else
            {
                Vector2 anchor = handleGraphicTransform.anchorMin;
                anchor.x = m_Slider.handleRect.anchorMin.x;
                handleGraphicTransform.anchorMin = anchor;

                anchor = handleGraphicTransform.anchorMax;
                anchor.x = m_Slider.handleRect.anchorMax.x;
                handleGraphicTransform.anchorMax = anchor;
                handleGraphicTransform.anchoredPosition = new Vector2(0f, handleGraphicTransform.anchoredPosition.y);
            }

            m_FillTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, fillAreaTransform.rect.width * m_HandleGraphicTransform.anchorMin.x);

            m_PopupText.SetGraphicText(slider.value.ToString("#0.#"));

            if (m_ValueText != null)
            {
                m_ValueText.SetGraphicText(slider.value.ToString("#0.##"));
            }

            if (m_InputField != null)
            {
                m_InputField.text = slider.value.ToString("#0.##");
            }
        }

        public void OnBeforeValidate()
        {
            if (m_BackgroundGraphic)
            {
                m_BackgroundColor = m_BackgroundGraphic.color;
            }
        }

        public void UpdateColors()
        {
            if (m_BackgroundGraphic)
            {
                m_BackgroundGraphic.color = m_BackgroundColor;
            }
            if (m_HandleGraphic)
            {
                m_HandleGraphic.color = m_Interactable ? m_EnabledColor : m_DisabledColor;
            }

            for (int i = 0; i < m_DotGraphics.Length; i++)
            {
                if (m_DotGraphics[i] == null) continue;

                if (slider.value > i)
                {
                    m_DotGraphics[i].color = m_Interactable ? m_EnabledColor : m_DisabledColor;
                }
                else
                {
                    m_DotGraphics[i].color = m_BackgroundColor;
                }
            }

            if (m_LeftContentTransform)
            {
                leftCanvasGroup.alpha = m_LowLeftDisabledOpacity ? (m_Interactable ? 1f : 0.5f) : 1f;
            }
            if (m_RightContentTransform)
            {
                rightCanvasGroup.alpha = m_LowRightDisabledOpacity ? (m_Interactable ? 1f : 0.5f) : 1f;
            }
        }

        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, m_AnimationDuration);
        }

        #endregion

        #region Layout Functions

        public void CalculateLayoutInputHorizontal()
        {
            if (m_LeftContentTransform)
            {
                ILayoutElement[] leftElements = m_LeftContentTransform.GetComponentsInChildren<ILayoutElement>();

                leftElements = leftElements.Reverse().ToArray();

                for (int i = 0; i < leftElements.Length; i++)
                {
                    leftElements[i].CalculateLayoutInputHorizontal();
                }

                m_LeftWidth = LayoutUtility.GetPreferredWidth(m_LeftContentTransform) + 16;
            }

            if (m_RightContentTransform)
            {
                ILayoutElement[] rightElements = m_RightContentTransform.GetComponentsInChildren<ILayoutElement>();

                rightElements = rightElements.Reverse().ToArray();

                for (int i = 0; i < rightElements.Length; i++)
                {
                    rightElements[i].CalculateLayoutInputHorizontal();
                }

                m_RightWidth = LayoutUtility.GetPreferredWidth(m_RightContentTransform) + 16;
            }
            else
            {
                m_RightWidth = 0f;
            }

            m_Width = Mathf.Max(m_ManualPreferredWidth, m_LeftWidth + m_RightWidth + ((slider.wholeNumbers && m_HasDots) ? 6f : 0f));
        }

        /// <summary>
        /// Sets the layout horizontal.
        /// </summary>
        public void SetLayoutHorizontal()
        {
            SetTracker();
            if (m_LeftContentTransform)
            {
                ILayoutController[] leftControllers = m_LeftContentTransform.GetComponentsInChildren<ILayoutController>();

                for (int i = 0; i < leftControllers.Length; i++)
                {
                    leftControllers[i].SetLayoutHorizontal();
                }
            }

            if (m_RightContentTransform)
            {
                ILayoutController[] rightControllers = m_RightContentTransform.GetComponentsInChildren<ILayoutController>();

                for (int i = 0; i < rightControllers.Length; i++)
                {
                    rightControllers[i].SetLayoutHorizontal();
                }
            }

            m_SliderContentTransform.anchorMin = new Vector2(0, m_SliderContentTransform.anchorMin.y);
            m_SliderContentTransform.anchorMax = new Vector2(1, m_SliderContentTransform.anchorMax.y);

            m_SliderContentTransform.anchoredPosition = new Vector2(m_LeftWidth + ((slider.wholeNumbers && m_HasDots) ? 3f : 0f), m_SliderContentTransform.anchoredPosition.y);
            m_SliderContentTransform.sizeDelta = new Vector2(-(m_LeftWidth + m_RightWidth) - ((slider.wholeNumbers && m_HasDots) ? 6f : 0f), m_SliderContentTransform.sizeDelta.y);
        }

        public void CalculateLayoutInputVertical()
        {
            float leftHeight = 0;
            float rightHeight = 0;

            if (m_LeftContentTransform)
            {
                ILayoutElement[] elements = m_LeftContentTransform.GetComponentsInChildren<ILayoutElement>();
                elements = elements.Reverse().ToArray();
                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i].CalculateLayoutInputVertical();
                }

                leftHeight = LayoutUtility.GetPreferredHeight(m_LeftContentTransform);
            }

            if (m_RightContentTransform)
            {
                ILayoutElement[] elements = m_RightContentTransform.GetComponentsInChildren<ILayoutElement>();
                elements = elements.Reverse().ToArray();
                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i].CalculateLayoutInputVertical();
                }

                rightHeight = LayoutUtility.GetPreferredHeight(m_RightContentTransform);
            }

            m_Height = Mathf.Max(LayoutUtility.GetPreferredHeight(m_SliderContentTransform), Mathf.Max(leftHeight, rightHeight));
            m_Height = Mathf.Max(m_Height, 24f);
        }

        public void SetLayoutVertical()
        {
            if (m_LeftContentTransform)
            {
                ILayoutController[] controllers = m_LeftContentTransform.GetComponentsInChildren<ILayoutController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    controllers[i].SetLayoutVertical();
                }
            }

            if (m_RightContentTransform)
            {
                ILayoutController[] controllers = m_RightContentTransform.GetComponentsInChildren<ILayoutController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    controllers[i].SetLayoutVertical();
                }
            }

            if (rootCanvas != null && !m_IsSelected)
            {
                Vector2 tempVector2 = m_HandleGraphic.rectTransform.anchoredPosition;
                tempVector2.y = (rootCanvas.pixelPerfect) ? 1f : 0f;
                m_HandleGraphic.rectTransform.anchoredPosition = tempVector2;
            }
        }

        public float minWidth
        {
            get { return -1; }
        }

        public float preferredWidth
        {
            get { return m_HasManualPreferredWidth ? m_Width : -1; }
        }

        public float flexibleWidth
        {
            get { return -1; }
        }

        public float minHeight
        {
            get { return -1; }
        }

        public float preferredHeight
        {
            get { return m_Height; }
        }

        public float flexibleHeight
        {
            get { return -1; }
        }

        public int layoutPriority
        {
            get { return -1; }
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

            public SliderStyleProperty(string p_name, Component p_target, Color p_colorEnabled, Color p_colorDisabled, bool p_useStyleGraphic)
            {
                m_target = p_target != null ? p_target.transform : null;
                m_name = p_name;
                m_colorEnabled = p_colorEnabled;
                m_colorDisabled = p_colorDisabled;
                m_useStyleGraphic = p_useStyleGraphic;
            }

            #endregion

            #region Helper Functions

            public override void Tween(BaseStyleElement p_sender, bool p_canAnimate, float p_animationDuration)
            {
                TweenManager.EndTween(_tweenId);

                var v_graphic = GetTarget<Graphic>();
                if (v_graphic != null)
                {
                    var v_slider = p_sender as MaterialSlider;
                    var v_isInteractable = v_slider != null ? v_slider.m_Interactable : true;

                    var v_endColor = !v_isInteractable ? m_colorDisabled : m_colorEnabled;
                    if (p_canAnimate && Application.isPlaying)
                    {
                        _tweenId = TweenManager.TweenColor(
                                (color) =>
                                {
                                    if (v_graphic != null)
                                        v_graphic.color = color;
                                },
                                v_graphic.color,
                                v_endColor,
                                p_animationDuration
                            );
                    }
                    else
                    {
                        v_graphic.color = v_endColor;
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
 