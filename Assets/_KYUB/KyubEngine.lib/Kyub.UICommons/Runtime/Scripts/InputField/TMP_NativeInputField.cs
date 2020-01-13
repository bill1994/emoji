using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using MobileInputNativePlugin;
using TMPro;

namespace Kyub.UI
{
    public class TMP_NativeInputField : TMPro.TMP_InputField, INativeInputField
    {
        #region Private Variables

        [SerializeField]
        RectTransform m_PanContent = null;

        #endregion

        #region Public Properties

        public RectTransform panContent
        {
            get
            {
                if (m_PanContent == null)
                    return this.transform as RectTransform;
                return m_PanContent;
            }
            set
            {
                if (m_PanContent == value)
                    return;
                m_PanContent = value;
            }
        }

        #endregion

        #region Callbacks

        [Header("Native Input Field Callbacks")]
        public UnityEvent OnReturnPressed = new UnityEvent();

        #endregion

        #region Constructors

        public TMP_NativeInputField()
        {
            asteriskChar = '•';
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            CheckAsteriskChar();
            MobileInputBehaviour v_nativeBox = GetComponent<MobileInputBehaviour>();

            if (MobileInputBehaviour.IsSupported())
            {
                if (v_nativeBox == null && Application.isPlaying)
                    v_nativeBox = gameObject.AddComponent<MobileInputBehaviour>();
            }
            //Not Supported Platform
            else
            {
                if (Application.isPlaying)
                {
                    if (v_nativeBox != null)
                    {

                        Debug.LogWarning("[TMP_NativeInputField] Not Supported Platform (sender " + name + ")");
                        GameObject.Destroy(v_nativeBox);
                    }
                }
            }

            //Activate native edit box
            if (v_nativeBox != null)
            {
                v_nativeBox.hideFlags = HideFlags.None;
                if (enabled && !v_nativeBox.enabled)
                    v_nativeBox.enabled = true;
            }
            RegisterEvents();

            //Update Simbling Index
            if (CachedInputRenderer != null && m_TextComponent != null)
            {
                CachedInputRenderer.transform.SetSiblingIndex(m_TextComponent.transform.GetSiblingIndex());
                AssignPositioningIfNeeded();
            }
        }

        protected override void Start()
        {
            base.Start();

            //Update Simbling Index
            if (CachedInputRenderer != null && m_TextComponent != null)
            {
                CachedInputRenderer.transform.SetSiblingIndex(m_TextComponent.transform.GetSiblingIndex());
                AssignPositioningIfNeeded();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            var v_nativeBox = GetComponent<MobileInputBehaviour>();
            if (v_nativeBox != null)
                v_nativeBox.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInInspector;
            UnregisterEvents();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            HasSelection = true;
            EvaluateAndTransitionToSelectionState(eventData);
            SendOnFocus();
            ActivateInputField();
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            //Get keycode pressed before consume event
            if (IsNativeKeyboardSupported())
            {
                var v_oldShouldActivateNextUpdate = ShouldActivateNextUpdate;
                //Prevent Unity Keyboard Activation when call base.OnUpdateSelected(eventData);
                if (v_oldShouldActivateNextUpdate)
                {
                    ShouldActivateNextUpdate = false;
                    SafeActivateInputFieldInternal();
                }
            }
            BaseOnUpdateSelected(eventData);
        }

        protected virtual void BaseOnUpdateSelected(BaseEventData eventData)
        {
            // Only activate if we are not already activated.
            if (ShouldActivateNextUpdate)
            {
                if (!isFocused)
                {
                    SafeActivateInputFieldInternal();
                    ShouldActivateNextUpdate = false;
                    return;
                }

                // Reset as we are already activated.
                ShouldActivateNextUpdate = false;
            }

            if (!isFocused)
                return;

            bool consumedEvent = false;
            while (Event.PopEvent(ProcessingEvent))
            {
                if (ProcessingEvent.rawType == EventType.KeyDown)
                {
                    consumedEvent = true;
                    var v_shouldBreak = false;
                    var shouldContinue = KeyPressed(ProcessingEvent);
                    if (shouldContinue == EditState.Finish)
                    {
                        DeactivateInputField();
                        v_shouldBreak = true;
                    }
                    //Extra feature to check KeyPress Down in non supported platforms
                    CheckReturnPressedNonSupportedPlatforms(ProcessingEvent);

                    //Break loop if needed
                    if (v_shouldBreak)
                        break;
                }
            }

            if (consumedEvent)
                UpdateLabel();

            eventData.Use();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!isFocused && BlinkCoroutine != null)
            {
                StopCoroutine(BlinkCoroutine);
                BlinkCoroutine = null;
            }

            CheckAsteriskChar();
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Force update text in Native Keyboard
        /// </summary>
        /// <param name="p_text"></param>
        public bool SetTextNative(string p_text)
        {
            var v_nativeBox = GetComponent<MobileInputBehaviour>();
            if (v_nativeBox != null)
            {
                v_nativeBox.Text = p_text;
                return true;
            }
            else
                this.text = p_text;
            return false;
        }

        /// <summary>
        /// Call this functions if you want to update native label font and color (after change this in input field)
        /// </summary>
        public void RecreateKeyboard()
        {
            var v_nativeBox = GetComponent<MobileInputBehaviour>();
            if (v_nativeBox != null)
                v_nativeBox.RecreateNativeEdit();
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnReturnPressed()
        {
            if (OnReturnPressed != null)
                OnReturnPressed.Invoke();
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void CheckAsteriskChar()
        {
#if UNITY_IOS
            //The security character in IOS is locked to 'Black Circle', so we must reflect this in Unity.
            //Black Circle Char! (not Bullet)
            if (Application.isPlaying && asteriskChar != '●')
                asteriskChar = '●';
#elif UNITY_ANDROID
            //The security character in Android is locked to 'Buller', so we must reflect this in Unity.
            //Bullet Char
            if (Application.isPlaying && asteriskChar != '•')
                asteriskChar = '•';  
#endif
        }

        protected void AssignPositioningIfNeeded()
        {
            var caretRectTrans = CachedInputRenderer != null ? CachedInputRenderer.GetComponent<RectTransform>() : null;
            if (m_TextComponent != null && caretRectTrans != null &&
                (caretRectTrans.localPosition != m_TextComponent.rectTransform.localPosition ||
                 caretRectTrans.localRotation != m_TextComponent.rectTransform.localRotation ||
                 caretRectTrans.localScale != m_TextComponent.rectTransform.localScale ||
                 caretRectTrans.anchorMin != m_TextComponent.rectTransform.anchorMin ||
                 caretRectTrans.anchorMax != m_TextComponent.rectTransform.anchorMax ||
                 caretRectTrans.anchoredPosition != m_TextComponent.rectTransform.anchoredPosition ||
                 caretRectTrans.sizeDelta != m_TextComponent.rectTransform.sizeDelta ||
                 caretRectTrans.pivot != m_TextComponent.rectTransform.pivot ||
                 caretRectTrans.offsetMin != m_TextComponent.rectTransform.offsetMin ||
                 caretRectTrans.offsetMax != m_TextComponent.rectTransform.offsetMax))
            {
                caretRectTrans.localPosition = m_TextComponent.rectTransform.localPosition;
                caretRectTrans.localRotation = m_TextComponent.rectTransform.localRotation;
                caretRectTrans.localScale = m_TextComponent.rectTransform.localScale;
                caretRectTrans.anchorMin = m_TextComponent.rectTransform.anchorMin;
                caretRectTrans.anchorMax = m_TextComponent.rectTransform.anchorMax;
                caretRectTrans.anchoredPosition = m_TextComponent.rectTransform.anchoredPosition;
                caretRectTrans.sizeDelta = m_TextComponent.rectTransform.sizeDelta;
                caretRectTrans.pivot = m_TextComponent.rectTransform.pivot;
                caretRectTrans.offsetMin = m_TextComponent.rectTransform.offsetMin;
                caretRectTrans.offsetMax = m_TextComponent.rectTransform.offsetMax;

                // Get updated world corners of viewport.
                //m_TextViewport.GetLocalCorners(m_ViewportCorners);
            }
        }

        // Change the button to the correct state
        System.Reflection.MethodInfo m_EvaluateAndTransitionToSelectionStateInfo = null;
        protected void EvaluateAndTransitionToSelectionState(BaseEventData eventData)
        {
            if (m_EvaluateAndTransitionToSelectionStateInfo == null)
                m_EvaluateAndTransitionToSelectionStateInfo = typeof(Selectable).GetMethod("EvaluateAndTransitionToSelectionState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (m_EvaluateAndTransitionToSelectionStateInfo != null)
            {
                var parameters = m_EvaluateAndTransitionToSelectionStateInfo.GetParameters();
                if (parameters != null && parameters.Length == 1)
                    m_EvaluateAndTransitionToSelectionStateInfo.Invoke(this, new object[] { eventData });
                else if (parameters == null || parameters.Length == 0)
                    m_EvaluateAndTransitionToSelectionStateInfo.Invoke(this, null);
            }
        }

        protected virtual void CheckReturnPressedNonSupportedPlatforms(Event p_event)
        {
            if (!TouchScreenKeyboard.isSupported &&
                (p_event != null && p_event.isKey && p_event.rawType == EventType.KeyDown))
            {
                var v_returnPressed = p_event.keyCode == KeyCode.Return;
                //var v_tabPressed = p_event.keyCode == KeyCode.Tab;
                if ((lineType != LineType.MultiLineNewline && v_returnPressed) ||
                    (lineType == LineType.MultiLineNewline && p_event.shift && v_returnPressed))
                {
                    HandleOnReturnPressed();
                }
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (IsNativeKeyboardSupported())
            {
                var v_nativeBox = GetComponent<MobileInputBehaviour>();
                if (v_nativeBox != null && v_nativeBox.OnReturnPressedEvent != null)
                    v_nativeBox.OnReturnPressedEvent.AddListener(HandleOnReturnPressed);
            }
        }

        protected virtual void UnregisterEvents()
        {
            if (IsNativeKeyboardSupported())
            {
                var v_nativeBox = GetComponent<MobileInputBehaviour>();
                if (v_nativeBox != null && v_nativeBox.OnReturnPressedEvent != null)
                    v_nativeBox.OnReturnPressedEvent.RemoveListener(HandleOnReturnPressed);
            }
        }

        protected bool IsUnityKeyboardSupported()
        {
            var v_isSupported = TouchScreenKeyboard.isSupported && (!shouldHideMobileInput || !MobileInputBehaviour.IsSupported());
            return v_isSupported;
        }

        protected bool IsNativeKeyboardSupported()
        {
            var v_isSupported = TouchScreenKeyboard.isSupported && shouldHideMobileInput && MobileInputBehaviour.IsSupported();
            return v_isSupported;
        }

        #endregion

        #region Internal Important Functions

        public new void ActivateInputField()
        {
            if (m_TextComponent == null || m_TextComponent.font == null || !IsActive() || !IsInteractable())
                return;

            if (IsNativeKeyboardSupported())
            {
                var v_nativeBox = GetComponent<MobileInputBehaviour>();
                if (v_nativeBox != null)
                {
                    v_nativeBox.Text = m_Text;
                    v_nativeBox.SetVisibleAndFocus(true);
                }
                ShouldActivateNextUpdate = false;
                AllowInput = true;
            }
            else
            {
                base.ActivateInputField();
            }
        }

        private void SafeActivateInputFieldInternal()
        {
            if (IsUnityKeyboardSupported())
            {
                if (Input.touchSupported)
                {
                    TouchScreenKeyboard.hideInput = shouldHideMobileInput;
                }
#if UNITY_2018_3_OR_NEWER
                    m_SoftKeyboard = inputType == InputType.Password ?
#else
                m_Keyboard = inputType == InputType.Password ?
#endif
                    TouchScreenKeyboard.Open(m_Text, keyboardType, false, multiLine, true) :
                TouchScreenKeyboard.Open(m_Text, keyboardType, inputType == InputType.AutoCorrect, multiLine);
            }
            else if (IsNativeKeyboardSupported())
            {
                var v_nativeBox = GetComponent<MobileInputBehaviour>();
                if (v_nativeBox != null)
                {
                    v_nativeBox.Text = m_Text;
                    v_nativeBox.SetVisibleAndFocus(true);
                }
            }
            else
            {
                Input.imeCompositionMode = IMECompositionMode.On;
                OnFocus();
            }

            AllowInput = true;
            OriginalText = text;
            WasCanceled = false;
            SetCaretVisible();
            UpdateLabel();
        }

        System.Reflection.MethodInfo m_SetCaretVisibleInfo = null;
        protected void SetCaretVisible()
        {
            if (m_SetCaretVisibleInfo == null)
                m_SetCaretVisibleInfo = typeof(TMPro.TMP_InputField).GetMethod("SetCaretVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (m_SetCaretVisibleInfo != null)
                m_SetCaretVisibleInfo.Invoke(this, null);
        }

#if UNITY_2019_2_OR_NEWER
        public void DeactivateInputField()
        {
            base.DeactivateInputField();
        }
#endif

        #endregion

        #region Internal Important Fields

        System.Reflection.FieldInfo m_BlinkCoroutineInfo = null;
        protected Coroutine BlinkCoroutine
        {
            get
            {
                if (m_BlinkCoroutineInfo == null)
                    m_BlinkCoroutineInfo = typeof(TMPro.TMP_InputField).GetField("m_BlinkCoroutine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return m_BlinkCoroutineInfo.GetValue(this) as Coroutine;
            }
            set
            {
                if (m_BlinkCoroutineInfo == null)
                    m_BlinkCoroutineInfo = typeof(TMPro.TMP_InputField).GetField("m_BlinkCoroutine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_BlinkCoroutineInfo != null)
                    m_BlinkCoroutineInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_CachedInputRendererInfo = null;
        protected CanvasRenderer CachedInputRenderer
        {
            get
            {
                if (m_CachedInputRendererInfo == null)
                    m_CachedInputRendererInfo = typeof(TMPro.TMP_InputField).GetField("m_CachedInputRenderer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return m_CachedInputRendererInfo.GetValue(this) as CanvasRenderer;
            }
            set
            {
                if (m_CachedInputRendererInfo == null)
                    m_CachedInputRendererInfo = typeof(TMPro.TMP_InputField).GetField("m_CachedInputRenderer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_CachedInputRendererInfo != null)
                    m_CachedInputRendererInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_ProcessingEventInfo = null;
        protected Event ProcessingEvent
        {
            get
            {
                if (m_ProcessingEventInfo == null)
                    m_ProcessingEventInfo = typeof(TMPro.TMP_InputField).GetField("m_ProcessingEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (Event)m_ProcessingEventInfo.GetValue(this);
            }
            set
            {
                if (m_ProcessingEventInfo == null)
                    m_ProcessingEventInfo = typeof(TMPro.TMP_InputField).GetField("m_ProcessingEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_ProcessingEventInfo != null)
                    m_ProcessingEventInfo.SetValue(this, value);
            }
        }


        System.Reflection.FieldInfo m_ShouldActivateNextUpdateInfo = null;
        protected bool ShouldActivateNextUpdate
        {
            get
            {
                if (m_ShouldActivateNextUpdateInfo == null)
                    m_ShouldActivateNextUpdateInfo = typeof(TMPro.TMP_InputField).GetField("m_ShouldActivateNextUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (bool)m_ShouldActivateNextUpdateInfo.GetValue(this);
            }
            set
            {
                if (m_ShouldActivateNextUpdateInfo == null)
                    m_ShouldActivateNextUpdateInfo = typeof(TMPro.TMP_InputField).GetField("m_ShouldActivateNextUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_ShouldActivateNextUpdateInfo != null)
                    m_ShouldActivateNextUpdateInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_AllowInputInfo = null;
        protected bool AllowInput
        {
            get
            {
                return isFocused;
            }
            set
            {
                if (m_AllowInputInfo == null)
                    m_AllowInputInfo = typeof(TMPro.TMP_InputField).GetField("m_AllowInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_AllowInputInfo != null)
                    m_AllowInputInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_OriginalTextInfo = null;
        protected string OriginalText
        {
            get
            {
                if (m_OriginalTextInfo == null)
                    m_OriginalTextInfo = typeof(TMPro.TMP_InputField).GetField("m_OriginalText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (string)m_OriginalTextInfo.GetValue(this);
            }
            set
            {
                if (m_OriginalTextInfo == null)
                    m_OriginalTextInfo = typeof(TMPro.TMP_InputField).GetField("m_OriginalText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_OriginalTextInfo != null)
                    m_OriginalTextInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_WasCanceledInfo = null;
        protected bool WasCanceled
        {
            get
            {
                if (m_WasCanceledInfo == null)
                    m_WasCanceledInfo = typeof(TMPro.TMP_InputField).GetField("m_WasCanceled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (bool)m_WasCanceledInfo.GetValue(this);
            }
            set
            {
                if (m_WasCanceledInfo == null)
                    m_WasCanceledInfo = typeof(TMPro.TMP_InputField).GetField("m_WasCanceled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_WasCanceledInfo != null)
                    m_WasCanceledInfo.SetValue(this, value);
            }
        }

        protected bool HasTextSelection
        {
            get
            {
                return this.caretPositionInternal != this.caretSelectPositionInternal;
            }
        }

        System.Reflection.PropertyInfo m_HasSelectionInfo = null;
        protected bool HasSelection
        {
            get
            {
                if (m_HasSelectionInfo == null)
                    m_HasSelectionInfo = typeof(Selectable).GetProperty("hasSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (bool)m_HasSelectionInfo.GetValue(this, null);
            }
            set
            {
                if (m_HasSelectionInfo == null)
                    m_HasSelectionInfo = typeof(Selectable).GetProperty("hasSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_HasSelectionInfo != null)
                    m_HasSelectionInfo.SetValue(this, value, null);
            }
        }

        #endregion

        #region INativeInputField Extra Implementations

        UnityEvent<string> INativeInputField.onValueChanged
        {
            get
            {
                return onValueChanged;
            }
        }

        UnityEvent<string> INativeInputField.onEndEdit
        {
            get
            {
                return onEndEdit;
            }
        }

        UnityEvent INativeInputField.onReturnPressed
        {
            get
            {
                return OnReturnPressed;
            }
        }

        Graphic INativeInputField.textComponent
        {
            get
            {
                return textComponent;
            }
        }

        #endregion
    }
}