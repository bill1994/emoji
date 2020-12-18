using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Kyub.Internal.NativeInputPlugin;
using TMPro;
using Kyub.EventSystems;

namespace Kyub.UI
{
    public partial class TMP_NativeInputField : TMPro.TMP_InputField, INativeInputField
    {
        #region Private Variables

        [Tooltip("Force monospace character with distance (value)em when password selected")]
        [SerializeField]
        protected float m_MonospacePasswordDistEm = 0.5f;
        [SerializeField]
        RectTransform m_PanContent = null;

        #endregion

        #region Public Properties

        public float MonospacePasswordDistEm
        {
            get
            {
                return m_MonospacePasswordDistEm;
            }
            set
            {
                if (m_MonospacePasswordDistEm == value)
                    return;
                m_MonospacePasswordDistEm = value;
            }
        }

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

            MobileInputBehaviour nativeBox = GetComponent<MobileInputBehaviour>();
            if (MobileInputBehaviour.IsSupported())
            {
                if (nativeBox == null && Application.isPlaying)
                    nativeBox = gameObject.AddComponent<MobileInputBehaviour>();
            }
            //Not Supported Platform
            else
            {
                if (Application.isPlaying)
                {
                    if (nativeBox != null)
                    {

                        Debug.LogWarning("[TMP_NativeInputField] Not Supported Platform (sender " + name + ")");
                        GameObject.Destroy(nativeBox);
                    }
                }
            }

            //Activate native edit box
            if (nativeBox != null)
            {
                nativeBox.hideFlags = HideFlags.None;
                if (enabled && !nativeBox.enabled)
                    nativeBox.enabled = true;
            }
            RegisterEvents();

            //Update Simbling Index
            if (CachedInputRenderer != null && m_TextComponent != null)
            {
                CachedInputRenderer.transform.SetSiblingIndex(m_TextComponent.transform.GetSiblingIndex());
                AssignPositioningIfNeeded();
            }

            if (m_TextComponent != null)
            {
                //Unregister base.UpdateLabel
                m_TextComponent.UnregisterDirtyVerticesCallback(base.UpdateLabel);
                //Registe new UpdateLabel
                m_TextComponent.RegisterDirtyVerticesCallback(UpdateLabel);
            }
        }
        protected override void OnDisable()
        {
            BaseOnDisable();
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

            var nativeBox = GetComponent<MobileInputBehaviour>();
            if (nativeBox != null)
                nativeBox.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInInspector;

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
                var oldShouldActivateNextUpdate = ShouldActivateNextUpdate;
                //Prevent Unity Keyboard Activation when call base.OnUpdateSelected(eventData);
                if (oldShouldActivateNextUpdate)
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
                    var shouldBreak = false;
                    var shouldContinue = KeyPressed(ProcessingEvent);
                    if (shouldContinue == EditState.Finish)
                    {
                        DeactivateInputField();
                        shouldBreak = true;
                    }
                    //Extra feature to check KeyPress Down in non supported platforms
                    CheckReturnPressedNonSupportedPlatforms(ProcessingEvent);

                    //Break loop if needed
                    if (shouldBreak)
                        break;
                }
            }

            if (consumedEvent)
                UpdateLabel();

            eventData.Use();
        }

        protected override void LateUpdate()
        {
            BaseLateUpdate();

            if (!isFocused && BlinkCoroutine != null)
            {
                StopCoroutine(BlinkCoroutine);
                BlinkCoroutine = null;
            }

            CheckAsteriskChar();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            //Track font from textComponent
            if (textComponent != null && textComponent.font != fontAsset)
            {
                m_GlobalFontAsset = textComponent.font;
                UnityEditor.EditorUtility.SetDirty(this);
            }
            base.OnValidate();
            UpdateLabel();
        }
#endif

        #endregion

        #region Helper Functions

        protected new void UpdateLabel()
        {
            base.UpdateLabel();
            if (m_MonospacePasswordDistEm != 0 && inputType == InputType.Password &&
                m_TextComponent != null && m_TextComponent.font != null && PreventCallback == false)
            {
                PreventCallback = true;
                //We must support richtext when password with monospace active
                m_TextComponent.richText = true;
                var text = m_TextComponent.text;
                if(ApplyMonoSpacingValues(text, out text))
                {
                    m_TextComponent.text = text;
                }
                PreventCallback = false;
            }
        }

        protected virtual bool ApplyMonoSpacingValues(string text, out string outText)
        {
            bool sucess = false;

            if (m_MonospacePasswordDistEm != 0)
            {
                outText = "<mspace=" + m_MonospacePasswordDistEm.ToString(System.Globalization.CultureInfo.InvariantCulture) + "em>" + text;
                sucess = true;
            }
            else
                outText = text;

            return sucess;
        }

        /// <summary>
        /// Force update text in Native Keyboard
        /// </summary>
        /// <param name="text"></param>
        public bool SetTextNative(string text)
        {
            var nativeBox = GetComponent<MobileInputBehaviour>();
            if (nativeBox != null)
            {
                nativeBox.Text = text;
                return true;
            }
            else
                this.text = text;
            return false;
        }

        /// <summary>
        /// Call this functions if you want to update native label font and color (after change this in input field)
        /// </summary>
        public void RecreateKeyboard()
        {
            var nativeBox = GetComponent<MobileInputBehaviour>();
            if (nativeBox != null)
                nativeBox.RecreateNativeEdit();
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnReturnPressed()
        {
            if (OnReturnPressed != null)
                OnReturnPressed.Invoke();
        }

        #endregion

        #region Clipboard Overriden Functions

        public new virtual void ProcessEvent(Event e)
        {
            KeyPressed(e);
        }

        protected new virtual EditState KeyPressed(Event evt)
        {
            // We must override base event for clipboard actions
            var currentEventModifiers = evt.modifiers;
            bool ctrl = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX ? (currentEventModifiers & EventModifiers.Command) != 0 : (currentEventModifiers & EventModifiers.Control) != 0;
            bool shift = (currentEventModifiers & EventModifiers.Shift) != 0;
            bool alt = (currentEventModifiers & EventModifiers.Alt) != 0;
            bool ctrlOnly = ctrl && !alt && !shift;

            switch (evt.keyCode)
            {
                // Copy
                case KeyCode.C:
                    {
                        if (ctrlOnly)
                        {
                            if (inputType != InputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            return EditState.Continue;
                        }
                        break;
                    }

                // Paste
                case KeyCode.V:
                    {
                        if (ctrlOnly)
                        {
                            if (enabled && gameObject.activeInHierarchy && Application.platform == RuntimePlatform.WebGLPlayer)
                            {
                                //Request clipboard (in WebGL this request is async)
                                Kyub.UI.ClipboardUtility.GetText();

                                //Set clipboard after delay
                                if (!IsInvoking("AppendClipboard"))
                                    Invoke("AppendClipboard", 0.1f);
                            }
                            else
                                AppendClipboard();
                            return EditState.Continue;
                        }
                        break;
                    }

                // Cut
                case KeyCode.X:
                    {
                        if (ctrlOnly)
                        {
                            if (inputType != InputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            Delete();
                            UpdateTouchKeyboardFromEditChanges();
                            SendOnValueChangedAndUpdateLabel();
                            return EditState.Continue;
                        }
                        break;
                    }
            }

            return base.KeyPressed(evt);
        }

        protected virtual void AppendClipboard()
        {
            Append(clipboard);
            UpdateLabel();

            CancelInvoke("AppendClipboard");
        }

        protected virtual string GetSelectedString()
        {
            if (!HasSelection)
                return "";

            int startPos = stringPositionInternal;
            int endPos = stringSelectPositionInternal;

            // Ensure pos is always less then selPos to make the code simpler
            if (startPos > endPos)
            {
                int temp = startPos;
                startPos = endPos;
                endPos = temp;
            }

            return text.Substring(startPos, endPos - startPos);
        }

        protected virtual void UpdateTouchKeyboardFromEditChanges()
        {
            // Update the TouchKeyboard's text from edit changes
            // if in-place editing is allowed
            if (m_SoftKeyboard != null && InPlaceEditing())
            {
                m_SoftKeyboard.text = m_Text;
            }
        }

        protected virtual bool InPlaceEditing()
        {
            if (TouchKeyboardAllowsInPlaceEditing || (TouchScreenKeyboard.isSupported && (Application.platform == RuntimePlatform.WSAPlayerX86 || Application.platform == RuntimePlatform.WSAPlayerX64 || Application.platform == RuntimePlatform.WSAPlayerARM)))
                return true;

            if (TouchScreenKeyboard.isSupported && shouldHideSoftKeyboard)
                return true;

            if (TouchScreenKeyboard.isSupported && shouldHideSoftKeyboard == false && shouldHideMobileInput == false)
                return false;

            return true;
        }

        protected virtual void Delete()
        {
            if (readOnly)
                return;

            if (m_StringPosition == m_StringSelectPosition)
                return;

            if (m_isRichTextEditingAllowed || m_isSelectAll)
            {
                // Handling of Delete when Rich Text is allowed.
                if (m_StringPosition < m_StringSelectPosition)
                {
                    m_Text = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
                    m_StringSelectPosition = m_StringPosition;
                }
                else
                {
                    m_Text = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);
                    m_StringPosition = m_StringSelectPosition;
                }

                m_isSelectAll = false;
            }
            else
            {
                if (m_CaretPosition < m_CaretSelectPosition)
                {
                    m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition].index;
                    m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition - 1].index + m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition - 1].stringLength;

                    m_Text = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);

                    m_StringSelectPosition = m_StringPosition;
                    m_CaretSelectPosition = m_CaretPosition;
                }
                else
                {
                    m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition - 1].index + m_TextComponent.textInfo.characterInfo[m_CaretPosition - 1].stringLength;
                    m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition].index;

                    m_Text = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);

                    m_StringPosition = m_StringSelectPosition;
                    m_CaretPosition = m_CaretSelectPosition;
                }
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        protected virtual void SendOnValueChangedAndUpdateLabel()
        {
            UpdateLabel();
            SendOnValueChanged();
        }

        protected virtual void SendOnValueChanged()
        {
            if (onValueChanged != null)
                onValueChanged.Invoke(text);
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
            //The security character in Android is locked to 'Bullet', so we must reflect this in Unity.
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

        protected virtual void CheckReturnPressedNonSupportedPlatforms(Event eventElement)
        {
            if (!TouchScreenKeyboard.isSupported &&
                (eventElement != null && eventElement.isKey && eventElement.rawType == EventType.KeyDown))
            {
                var returnPressed = eventElement.keyCode == KeyCode.Return;
                //var tabPressed = event.keyCode == KeyCode.Tab;
                if ((lineType != LineType.MultiLineNewline && returnPressed) ||
                    (lineType == LineType.MultiLineNewline && eventElement.shift && returnPressed))
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
                var nativeBox = GetComponent<MobileInputBehaviour>();
                if (nativeBox != null && nativeBox.OnReturnPressedEvent != null)
                    nativeBox.OnReturnPressedEvent.AddListener(HandleOnReturnPressed);
            }
        }

        protected virtual void UnregisterEvents()
        {
            if (IsNativeKeyboardSupported())
            {
                var nativeBox = GetComponent<MobileInputBehaviour>();
                if (nativeBox != null && nativeBox.OnReturnPressedEvent != null)
                    nativeBox.OnReturnPressedEvent.RemoveListener(HandleOnReturnPressed);
            }
        }

        protected bool IsUnityKeyboardSupported()
        {
            var isSupported = TouchScreenKeyboard.isSupported && (!shouldHideMobileInput || !MobileInputBehaviour.IsSupported());
            return isSupported;
        }

        protected bool IsNativeKeyboardSupported()
        {
            var isSupported = TouchScreenKeyboard.isSupported && shouldHideMobileInput && MobileInputBehaviour.IsSupported();
            return isSupported;
        }

        #endregion

        #region Internal Important Functions

        public new void ActivateInputField()
        {
            if (m_TextComponent == null || m_TextComponent.font == null || !IsActive() || !IsInteractable())
                return;

            if (IsNativeKeyboardSupported())
            {
                var nativeBox = GetComponent<MobileInputBehaviour>();
                if (nativeBox != null)
                {
                    nativeBox.Text = m_Text;
                    nativeBox.Show();
                }
                ShouldActivateNextUpdate = false;
                AllowInput = true;
            }
            else
            {
                base.ActivateInputField();
            }
        }

        protected void SafeActivateInputFieldInternal()
        {
            if (IsUnityKeyboardSupported())
            {
                if (InputCompat.touchSupported)
                {
                    TouchScreenKeyboard.hideInput = shouldHideMobileInput;
                }
#if TMP_1_4_0_OR_NEWER
                m_SoftKeyboard = inputType == InputType.Password ?
#else
                m_Keyboard = inputType == InputType.Password ?
#endif
                    TouchScreenKeyboard.Open(m_Text, keyboardType, false, multiLine, true) :
                    TouchScreenKeyboard.Open(m_Text, keyboardType, inputType == InputType.AutoCorrect, multiLine);
            }
            else if (IsNativeKeyboardSupported())
            {
                var nativeBox = GetComponent<MobileInputBehaviour>();
                if (nativeBox != null)
                {
                    nativeBox.Text = m_Text;
                    nativeBox.Show();
                }
            }
            else
            {
                InputCompat.imeCompositionMode = IMECompositionMode.On;
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

#if TMP_1_4_0_OR_NEWER
        public void DeactivateInputField()
        {
            DeactivateInputField(false);
        }
#endif
        #endregion

        #region Internal Important Fields

        protected static string clipboard
        {
            get
            {
                return Kyub.UI.ClipboardUtility.GetText();
            }
            set
            {
                Kyub.UI.ClipboardUtility.SetText(value);
            }
        }

        System.Reflection.FieldInfo m_TouchKeyboardAllowsInPlaceEditingInfo = null;
        protected bool TouchKeyboardAllowsInPlaceEditing
        {
            get
            {
                if (m_TouchKeyboardAllowsInPlaceEditingInfo == null)
                    m_TouchKeyboardAllowsInPlaceEditingInfo = typeof(TMPro.TMP_InputField).GetField("m_TouchKeyboardAllowsInPlaceEditing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_TouchKeyboardAllowsInPlaceEditingInfo.GetValue(this);
                return value is bool ? (bool)value : false;
            }
            set
            {
                if (m_TouchKeyboardAllowsInPlaceEditingInfo == null)
                    m_TouchKeyboardAllowsInPlaceEditingInfo = typeof(TMPro.TMP_InputField).GetField("m_TouchKeyboardAllowsInPlaceEditing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_TouchKeyboardAllowsInPlaceEditingInfo != null)
                    m_TouchKeyboardAllowsInPlaceEditingInfo.SetValue(this, value);
            }
        }

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

        System.Reflection.FieldInfo m_PreventCallbackInfo = null;
        protected bool PreventCallback
        {
            get
            {
                if (m_PreventCallbackInfo == null)
                    m_PreventCallbackInfo = typeof(TMPro.TMP_InputField).GetField("m_PreventCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (bool)m_PreventCallbackInfo.GetValue(this);
            }
            set
            {
                if (m_PreventCallbackInfo == null)
                    m_PreventCallbackInfo = typeof(TMPro.TMP_InputField).GetField("m_PreventCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_PreventCallbackInfo != null)
                    m_PreventCallbackInfo.SetValue(this, value);
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