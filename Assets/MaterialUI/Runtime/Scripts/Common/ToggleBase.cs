// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Toggle;

namespace MaterialUI
{
    [ExecuteInEditMode]
    public class ToggleBase : SelectableStyleElement<ToggleBase.ToggleBaseStyleProperty>, IPointerUpHandler, IPointerExitHandler
    {
        #region Private Variables

        [SerializeField, SerializeStyleProperty]
        protected Color m_OnColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected Color m_OffColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected Color m_DisabledColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected bool m_ChangeGraphicColor = true;
        [SerializeField, SerializeStyleProperty]
        protected Color m_GraphicOnColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected Color m_GraphicOffColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected Color m_GraphicDisabledColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected bool m_ChangeRippleColor = true;
        [SerializeField, SerializeStyleProperty]
        protected Color m_RippleOnColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected Color m_RippleOffColor = Color.black;

        [UnityEngine.Serialization.FormerlySerializedAs("m_ItemText")]
        [SerializeField, SerializeStyleProperty]
        protected internal Graphic m_Graphic;

        [SerializeField, SerializeStyleProperty]
        protected float m_AnimationDuration = 0.25f;

        [SerializeField]
        protected bool m_ToggleGraphic = false;
        [SerializeField]
        protected string m_ToggleOnLabel = null;
        [SerializeField]
        protected string m_ToggleOffLabel = null;
        [SerializeField]
        protected ImageData m_ToggleOnIcon = null;
        [SerializeField]
        protected ImageData m_ToggleOffIcon = null;

        [SerializeField]
        protected bool m_Interactable = true;

        [SerializeField]
        protected bool m_ForceUseDisableColor = false;

        [SerializeField, Tooltip("Try find a MaterialToggleGroup in Parent if group is null")]
        bool m_AutoRegisterInParentGroup = true;
        [SerializeField]
        MaterialToggleGroup m_Group = null;
        //[SerializeField]
        //protected ImageData m_Icon = null;
        //[SerializeField]
        //protected string m_Label = null;

        [SerializeField, HideInInspector]
        protected bool m_LastToggleState = false;

        protected CanvasGroup m_CanvasGroup = null;
        protected MaterialRipple m_MaterialRipple;
        protected Toggle m_Toggle;
        protected Color m_CurrentColor;
        protected Color m_CurrentGraphicColor;
        protected int m_AnimState;
        protected float m_AnimStartTime;
        protected float m_AnimDeltaTime;

        protected bool _callOnValueChangedByClick = false;

        //protected VectorImageData m_LastIconVectorImageData;
        //protected Sprite m_LastIconSprite;
        //protected string m_LastLabelText;

        #endregion

        #region Callbacks

        public UnityEvent onToggleOn = new UnityEvent();
        public UnityEvent onToggleOff = new UnityEvent();

        [Space]
        public ToggleEvent OnValueChangedByClick = new ToggleEvent();
        
        #endregion

        #region Properties

        public bool forceUseDisableColor
        {
            get
            {
                return m_ForceUseDisableColor;
            }
            set
            {
                if (m_ForceUseDisableColor == value)
                    return;
                m_ForceUseDisableColor = value;

                UpdateGraphicToggleState();
                ApplyCanvasGroupChanged();
            }
        }

        public UnityEngine.UI.Toggle.ToggleEvent OnValueChanged
        {
            get
            {
                return toggle != null ? toggle.onValueChanged : null;
            }
        }

        public bool autoRegisterInParentGroup
        {
            get
            {
                return m_AutoRegisterInParentGroup;
            }
            set
            {
                if (m_AutoRegisterInParentGroup == value)
                    return;
                m_AutoRegisterInParentGroup = value;

                if (m_Group == null && m_AutoRegisterInParentGroup && enabled && gameObject.activeInHierarchy && Application.isPlaying)
                    group = GetComponentInParent<MaterialToggleGroup>();
            }
        }

        public bool isOn
        {
            get
            {
                return toggle != null ? toggle.isOn : false;
            }
            set
            {
                if (toggle == null || toggle.isOn == value)
                    return;
                SetIsOnInternal(value, true);
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

        public Color onColor
        {
            get { return m_OnColor; }
            set { m_OnColor = value; }
        }

        public Color offColor
        {
            get { return m_OffColor; }
            set { m_OffColor = value; }
        }

        public Color disabledColor
        {
            get { return m_DisabledColor; }
            set { m_DisabledColor = value; }
        }

        public bool changeGraphicColor
        {
            get { return m_ChangeGraphicColor; }
            set { m_ChangeGraphicColor = value; }
        }

        public Color graphicOnColor
        {
            get { return m_GraphicOnColor; }
            set { m_GraphicOnColor = value; }
        }

        public Color graphicOffColor
        {
            get { return m_GraphicOffColor; }
            set { m_GraphicOffColor = value; }
        }

        public Color graphicDisabledColor
        {
            get { return m_GraphicDisabledColor; }
            set { m_GraphicDisabledColor = value; }
        }

        public bool changeRippleColor
        {
            get { return m_ChangeRippleColor; }
            set { m_ChangeRippleColor = value; }
        }

        public Color rippleOnColor
        {
            get { return m_RippleOnColor; }
            set { m_RippleOnColor = value; }
        }

        public Color rippleOffColor
        {
            get { return m_RippleOffColor; }
            set { m_RippleOffColor = value; }
        }

        public Graphic graphic
        {
            get { return m_Graphic; }
            set { m_Graphic = value; }
        }

        public bool toggleGraphic
        {
            get { return m_ToggleGraphic; }
            set
            {
                if (m_ToggleGraphic == value)
                    return;
                m_ToggleGraphic = value;

                if (m_ToggleGraphic)
                {
                    labelText = isOn ? m_ToggleOnLabel : m_ToggleOffLabel;
                    icon = isOn ? m_ToggleOnIcon : m_ToggleOffIcon;
                }
            }
        }

        public string toggleOnLabel
        {
            get { return m_ToggleOnLabel; }
            set
            {
                if (m_ToggleOnLabel == value)
                    return;
                m_ToggleOnLabel = value;

                if (m_ToggleGraphic && isOn)
                    labelText = value;
            }
        }

        public string toggleOffLabel
        {
            get { return m_ToggleOffLabel; }
            set
            {
                if (m_ToggleOffLabel == value)
                    return;
                m_ToggleOffLabel = value;

                if (m_ToggleGraphic && !isOn)
                    labelText = value;
            }
        }

        public ImageData toggleOnIcon
        {
            get { return m_ToggleOnIcon; }
            set
            {
                if (m_ToggleOnIcon == value)
                    return;
                m_ToggleOnIcon = value;

                if (m_ToggleGraphic && isOn)
                    icon = value;
            }
        }

        public ImageData toggleOffIcon
        {
            get { return m_ToggleOffIcon; }
            set
            {
                if (m_ToggleOffIcon == value)
                    return;
                m_ToggleOffIcon = value;

                if (m_ToggleGraphic && !isOn)
                    icon = value;
            }
        }

        public MaterialRipple materialRipple
        {
            get { return m_MaterialRipple; }
            set { m_MaterialRipple = value; }
        }

        public virtual Toggle toggle
        {
            get
            {
                if (!m_Toggle)
                {
                    m_Toggle = gameObject.GetComponent<Toggle>();
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

        public string labelText
        {
            get
            {
                if (m_Graphic == null) return null;

                return m_Graphic.GetGraphicText();
            }

            set
            {
                if (m_Graphic == null) return;

                m_Graphic.SetGraphicText(value);

                if (m_ToggleGraphic)
                {
                    if (isOn)
                        m_ToggleOnLabel = value;
                    else
                        m_ToggleOffLabel = value;
                }
            }
        }

        public ImageData icon
        {
            get
            {
                if (m_Graphic == null) return null;

                return m_Graphic.GetImageData();
            }

            set
            {
                if (m_Graphic == null) return;

                m_Graphic.SetImageData(value);

                if (m_ToggleGraphic)
                {
                    if (isOn)
                        m_ToggleOnIcon = value;
                    else
                        m_ToggleOffIcon = value;
                }
            }
        }

        private CanvasGroup canvasGroup
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

        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                if (m_Interactable != value || value != toggle.interactable)
                {
                    m_Interactable = value;
                    toggle.interactable = value;

                    if (value && IsInteractable())
                    {
                        ApplyInteractableOn();
                    }
                    else
                    {
                        ApplyInteractableOff();
                    }
                    UpdateGraphicToggleState();
                    ApplyCanvasGroupChanged();
                }
            }
        }

        public MaterialToggleGroup group
        {
            get { return m_Group; }
            set
            {
                if (m_Group == value)
                    return;

                var oldGroup = m_Group;
                if (oldGroup != null)
                    oldGroup.UnregisterToggle(this);

                m_Group = value;
                if (m_Group != null && enabled && gameObject.activeInHierarchy && Application.isPlaying)
                {
                    if (!m_Group.allowSwitchOff && toggle != null)
                        m_Toggle.enabled = !isOn;
                    m_Group.RegisterToggle(this);
                }

                //Unity ToggleGroup not supported
                if (toggle != null && m_Toggle.group != null && m_Group != null)
                    m_Toggle.group = null;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            if (!Application.isPlaying)
                return;

            if (toggle != null)
                RegisterEvents();
            base.Awake();
        }

        protected override void OnEnable()
        {
            if (Application.isPlaying)
            {
                if (m_AutoRegisterInParentGroup && m_Group == null)
                    group = GetComponentInParent<MaterialToggleGroup>();
                else if (m_Group != null)
                    m_Group.RegisterToggle(this);
            }

            base.OnEnable();
            materialRipple = gameObject.GetComponent<MaterialRipple>();
            if (_started)
            {
                UpdateGraphicToggleState();
                UpdateColorToggleState(false);
            }
        }

        protected bool _started = false;
        protected override void Start()
        {
            if (!Application.isPlaying)
                return;

            base.Start();
            _started = true;

            RegisterEvents();
            UpdateGraphicToggleState();
            UpdateColorToggleState(false);
        }

        protected override void OnDisable()
        {
            _callOnValueChangedByClick = false;
            //Only Unregister if self object disabled
            if (m_Group != null && m_Group.gameObject.activeInHierarchy && Application.isPlaying)
                m_Group.UnregisterToggle(this);

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            //Force Unregister OnDestroy
            if (m_Group != null && Application.isPlaying)
                m_Group.UnregisterToggle(this);

            UnregisterEvents();
            base.OnDestroy();
        }

        protected virtual void Update()
        {
            //This Part of code will handle with isOn changed when using "SetIsOnWithoutNotify" and when value changed using Inspector
            if (m_Toggle != null && m_LastToggleState != m_Toggle.isOn)
            {
                m_LastToggleState = m_Toggle.isOn;

                //Force ensure that this toggle state is valid when group was used
                EnsureGroupValidState(true, false);

                if (m_LastToggleState)
                {
                    TurnOnInstant();
                }
                else
                {
                    TurnOffInstant();
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    OnValidate();
#endif
                return;
            }


            if (m_AnimState > 0)
            {
                if (m_AnimStartTime < 0)
                    m_AnimStartTime = Time.realtimeSinceStartup;
                m_AnimDeltaTime = Time.realtimeSinceStartup - m_AnimStartTime;
            }

            if (m_AnimState == 1)
            {
                if (m_AnimDeltaTime <= m_AnimationDuration)
                {
                    AnimOn();
                }
                else
                {
                    AnimOnComplete();
                }
            }
            else if (m_AnimState == 2)
            {
                if (m_AnimDeltaTime <= m_AnimationDuration)
                {
                    AnimOff();
                }
                else
                {
                    AnimOffComplete();
                }
            }


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
                    ApplyCanvasGroupChanged();

                    UpdateColorToggleState(false);
                    UpdateGraphicToggleState();
                }
                finally
                {
                    _isChangingCanvasGroup = false;
                }
            }
        }

#if UNITY_EDITOR

        public void EditorValidate()
        {
            OnValidate();
        }

        protected override void OnValidateDelayed()
        {
            base.OnValidateDelayed();

            if (!Application.isPlaying)
            {
                if (m_Toggle == null)
                    m_Toggle = GetComponent<Toggle>();
            }

            if (m_Toggle != null && m_Toggle.group != null && m_Group != null)
                m_Toggle.group = null;

            ApplyCanvasGroupChanged();
            UpdateColorToggleState(false);
            UpdateGraphicToggleState();
        }
#endif

        #endregion

        #region Unity Input Functions

        public virtual void OnPointerUp(PointerEventData pointerEventData)
        {
            if (pointerEventData.button != PointerEventData.InputButton.Left)
                return;

            if (IsActive() && (m_Toggle != null && m_Toggle.IsInteractable()))
            {
                _callOnValueChangedByClick = true;
            }
            else
            {
                _callOnValueChangedByClick = false;
            }
        }
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            _callOnValueChangedByClick = false;
        }

        #endregion

        #region Helper Functions

        public virtual bool CanUseDisabledColor()
        {
            return m_ForceUseDisableColor || canvasGroup == null || !canvasGroup.enabled;
        }

        protected virtual void SetIsOnInternal(bool value, bool sendCallback)
        {
            //Force Remember last state when we send the callback. This is usefull to prevent unnecessary calculations
            if (sendCallback)
                m_LastToggleState = isOn;

            if (Application.isPlaying && m_Toggle != null &&
                (m_Group == null || m_Group.CanToggleValueChange(this, value)))
            {
                var oldValue = m_Toggle.isOn;

                if (!sendCallback)
                    m_Toggle.SetIsOnWithoutNotify(value);
                else
                    m_Toggle.isOn = value;

                if (m_Group != null)
                    m_Group.NotifyToggleValueChanged(this, sendCallback);
            }
        }

        public virtual void SetIsOnWithoutNotify(bool value)
        {
            if (m_Toggle.isOn != value)
                SetIsOnInternal(value, false);
        }

        public virtual void UpdateColorToggleState(bool canAnimate)
        {
            if (m_Toggle != null)
            {
                if (m_Toggle.isOn)
                {
                    if (canAnimate)
                        TurnOn();
                    else
                        TurnOnInstant();
                }
                else
                {
                    if (canAnimate)
                        TurnOff();
                    else
                        TurnOffInstant();
                }
            }
            bool interactable = IsInteractable();
            if (interactable)
                ApplyInteractableOn();
            else
                ApplyInteractableOff();
        }

        public override void RefreshVisualStyles(bool canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(canAnimate, m_AnimationDuration);
        }

        protected override bool OnLoadStyles(StyleData styleData)
        {
            var sucess = base.OnLoadStyles(styleData);
            if (sucess)
            {
                UpdateColorToggleState(false);
                UpdateGraphicToggleState();
            }
            return sucess;
        }

        public void UnregisterEvents()
        {
            if (m_Toggle != null)
                m_Toggle.onValueChanged.RemoveListener(HandleOnToggleChanged);
        }

        public void RegisterEvents()
        {
            UnregisterEvents();
            if (m_Toggle != null)
                m_Toggle.onValueChanged.AddListener(HandleOnToggleChanged);
        }

        public void Toggle()
        {
            if (m_Toggle.isOn)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }

        protected void UpdateGraphicToggleState()
        {
            UpdateIconDataType();

            if (m_Graphic == null || m_Toggle == null || !m_ToggleGraphic) return;

            if (m_Graphic is Image || m_Graphic is IVectorImage)
            {
                m_Graphic.SetImageData(m_Toggle.isOn ? m_ToggleOnIcon : m_ToggleOffIcon);
            }
            else
            {
                m_Graphic.SetGraphicText(m_Toggle.isOn ? m_ToggleOnLabel : m_ToggleOffLabel, m_Toggle.isOn ? m_ToggleOffLabel : m_ToggleOnLabel);
            }
        }

        protected void UpdateIconDataType()
        {
            if (m_Graphic == null) return;

            if (m_Graphic is Image)
            {
                m_ToggleOnIcon.imageDataType = ImageDataType.Sprite;
                m_ToggleOffIcon.imageDataType = ImageDataType.Sprite;
                //m_Icon.imageDataType = ImageDataType.Sprite;
            }
            else if (m_Graphic is IVectorImage)
            {
                m_ToggleOnIcon.imageDataType = ImageDataType.VectorImage;
                m_ToggleOffIcon.imageDataType = ImageDataType.VectorImage;
                //m_Icon.imageDataType = ImageDataType.VectorImage;
            }
        }

        public virtual void TurnOn()
        {
            RefreshVisualStyles();
            if (m_Graphic)
            {
                m_CurrentGraphicColor = m_Graphic.color;
            }

            m_AnimDeltaTime = 0;
            AnimOn();
            m_AnimStartTime = -1;
            m_AnimState = 1;

            UpdateGraphicToggleState();
        }

        public virtual void TurnOnInstant()
        {
            SetOnColor();
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);

            UpdateGraphicToggleState();
        }

        public virtual void TurnOff()
        {
            RefreshVisualStyles();
            if (m_Graphic)
            {
                m_CurrentGraphicColor = m_Graphic.color;
            }

            m_AnimDeltaTime = 0;
            AnimOff();
            m_AnimStartTime = -1;
            m_AnimState = 2;

            UpdateGraphicToggleState();
        }

        public virtual void TurnOffInstant()
        {
            SetOffColor();
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);

            UpdateGraphicToggleState();
        }

        protected virtual void ApplyInteractableOn()
        {
            if (m_Toggle != null)
            {
                if (m_Toggle.isOn)
                {
                    SetOnColor();
                }
                else
                {
                    SetOffColor();
                }
            }

            if (canvasGroup != null)
            {
                if (!canvasGroup.blocksRaycasts)
                    canvasGroup.blocksRaycasts = true;
                if (!canvasGroup.interactable)
                    canvasGroup.interactable = true;
            }
        }

        protected virtual void ApplyInteractableOff()
        {
            RefreshVisualStyles(false);

            if (m_Toggle != null)
            {
                if (m_Toggle.isOn)
                {
                    SetOnColor();
                }
                else
                {
                    SetOffColor();
                }
            }

            if (canvasGroup != null)
            {
                if (canvasGroup.blocksRaycasts)
                    canvasGroup.blocksRaycasts = false;
                if (canvasGroup.interactable)
                    canvasGroup.interactable = false;
            }

#if UNITY_EDITOR
            OnValidate();
#endif
        }

        protected virtual void AnimOn()
        {
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            if (m_Graphic && changeGraphicColor)
            {
                var isInteractable = IsInteractable();
                var canUseDisabledColor = CanUseDisabledColor();
                m_Graphic.color = Tween.QuintSoftOut(m_CurrentGraphicColor, !canUseDisabledColor || isInteractable ? m_GraphicOnColor : m_GraphicDisabledColor, m_AnimDeltaTime, m_AnimationDuration);
            }
            if (m_ChangeRippleColor && m_MaterialRipple != null)
            {
                materialRipple.rippleData.Color = m_RippleOnColor;
            }
        }

        protected virtual void AnimOnComplete()
        {
            SetOnColor();

            m_AnimState = 0;
        }

        protected virtual void AnimOff()
        {
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            if (m_Graphic && m_ChangeGraphicColor)
            {
                m_Graphic.color = Tween.QuintSoftOut(m_CurrentGraphicColor, !CanUseDisabledColor() || IsInteractable() ? m_GraphicOffColor : m_GraphicDisabledColor, m_AnimDeltaTime, m_AnimationDuration * 0.75f);
            }
            if (m_ChangeRippleColor && m_MaterialRipple != null)
            {
                materialRipple.rippleData.Color = m_RippleOffColor;
            }
        }

        protected virtual void AnimOffComplete()
        {
            SetOffColor();

            m_AnimState = 0;
        }

        protected virtual void SetOnColor()
        {
            RefreshVisualStyles(false);
            if (m_Graphic && m_ChangeGraphicColor)
            {
                m_Graphic.color = !CanUseDisabledColor() || IsInteractable() ? m_GraphicOnColor : m_GraphicDisabledColor;
            }
            if (materialRipple && m_ChangeRippleColor)
            {
                materialRipple.rippleData.Color = m_RippleOnColor;
            }
        }

        protected virtual void SetOffColor()
        {
            RefreshVisualStyles(false);
            if (m_Graphic && m_ChangeGraphicColor)
            {
                m_Graphic.color = !CanUseDisabledColor() || IsInteractable() ? m_GraphicOffColor : m_GraphicDisabledColor;
            }
            if (materialRipple && m_ChangeRippleColor)
            {
                materialRipple.rippleData.Color = m_RippleOffColor;
            }
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

        protected virtual void ApplyCanvasGroupChanged()
        {
            bool interactable = IsInteractable();

            if (canvasGroup != null)
            {
                if (canvasGroup.blocksRaycasts != m_Interactable)
                    canvasGroup.blocksRaycasts = m_Interactable;
                if (canvasGroup.interactable != m_Interactable)
                    canvasGroup.interactable = m_Interactable;

                var alpha = interactable || m_ForceUseDisableColor ? 1f : 0.5f;
                if (canvasGroup.alpha != alpha)
                    canvasGroup.alpha = alpha;
            }
        }

        protected internal virtual void ApplyGroupAllowSwitchOff()
        {
            if (Application.isPlaying)
            {
                if (m_Toggle != null && m_Group != null && !m_Group.allowSwitchOff && m_Group.IsActiveAndEnabledInHierarchy())
                    m_Toggle.enabled = !isOn;
                else if (m_Toggle != null)
                    m_Toggle.enabled = true;
            }
        }

        public virtual void EnsureGroupValidState(bool force, bool sendCallback)
        {
            //Validate Toggle State when setted by m_Toggle instead isOn
            if (m_Group != null && Application.isPlaying)
            {
                ApplyGroupAllowSwitchOff();

                if (isOn || (m_Group.allowSwitchOff && this == m_Group.GetCurrentSelectedToggle(false)))
                    m_Group.NotifyToggleValueChanged(this, true);

                if (m_LastToggleState != isOn || force)
                {
                    m_LastToggleState = isOn;
                    m_Group.EnsureValidState(sendCallback);
                }
            }
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnToggleChanged(bool value)
        {
            Toggle();

            var callback = value ? onToggleOn : onToggleOff;
            if (callback != null)
                callback.Invoke();

            if (_callOnValueChangedByClick)
            {
                _callOnValueChangedByClick = false;
                if (OnValueChangedByClick != null)
                    OnValueChangedByClick.Invoke(value);
            }

            EnsureGroupValidState(false, true);
        }

        #endregion

        #region BaseStyleElement Helper Classes

        [System.Serializable]
        public class ToggleBaseStyleProperty : StyleProperty
        {
            public enum ColorModeEnum { Default, ForceUseDisableColor, None }

            #region Private Variables

            [SerializeField, SerializeStyleProperty]
            protected ColorModeEnum m_colorMode = ColorModeEnum.Default;
            [SerializeField, SerializeStyleProperty]
            protected Color m_colorOn = Color.white;
            [SerializeField, SerializeStyleProperty]
            protected Color m_colorOff = Color.gray;
            [SerializeField, SerializeStyleProperty]
            protected Color m_colorDisabled = Color.white;

            #endregion

            #region Public Properties

            public ColorModeEnum ColorMode
            {
                get
                {
                    return m_colorMode;
                }

                set
                {
                    m_colorMode = value;
                }
            }

            public Color ColorOn
            {
                get
                {
                    return m_colorOn;
                }

                set
                {
                    m_colorOn = value;
                }
            }

            public Color ColorOff
            {
                get
                {
                    return m_colorOff;
                }

                set
                {
                    m_colorOff = value;
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

            public ToggleBaseStyleProperty()
            {
            }

            public ToggleBaseStyleProperty(string name, Component target, Color colorOn, Color colorOff, Color colorDisabled, bool useStyleGraphic)
            {
                m_target = target != null ? target.transform : null;
                m_name = name;
                m_colorOn = colorOn;
                m_colorOff = colorOff;
                m_colorDisabled = colorDisabled;
                m_useStyleGraphic = useStyleGraphic;
            }

            #endregion

            #region Helper Functions

            public override void Tween(BaseStyleElement sender, bool canAnimate, float animationDuration)
            {
                TweenManager.EndTween(_tweenId);

                var graphic = GetTarget<Graphic>();
                if (graphic != null && m_colorMode != ColorModeEnum.None)
                {
                    var toggleBase = sender as ToggleBase;
                    var isActive = toggleBase != null ? toggleBase.toggle.isOn : true;
                    var isInteractable = toggleBase != null ? toggleBase.IsInteractable() : true;
                    var canUseDisabledColor = m_colorMode != ColorModeEnum.ForceUseDisableColor && toggleBase != null ? toggleBase.CanUseDisabledColor() : true;

                    var endColor = !canUseDisabledColor || isInteractable ? (isActive ? m_colorOn : m_colorOff) : m_colorDisabled;
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
                                animationDuration,
                                0,
                                null,
                                false,
                                MaterialUI.Tween.TweenType.SoftEaseOutQuint

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