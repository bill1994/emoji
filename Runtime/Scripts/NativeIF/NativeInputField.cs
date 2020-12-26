using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Kyub.Internal.NativeInputPlugin;

namespace Kyub.UI
{
    public class NativeInputField : InputField, INativeInputField
    {
        #region Private Variables

        [SerializeField]
        RectTransform m_PanContent = null;
        [SerializeField]
        RectTransform m_TextViewport = null;

        [SerializeField]
        protected bool m_ShowDoneButton = true;
        [SerializeField]
        protected bool m_ShowClearButton = false;
        [SerializeField]
        protected MobileInputBehaviour.ReturnKeyType m_ReturnKeyType = MobileInputBehaviour.ReturnKeyType.Done;

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

        public RectTransform textViewport
        {
            get
            {
                if (m_TextViewport == null && m_TextComponent != null)
                    m_TextViewport = m_TextComponent.rectTransform;
                return m_TextViewport;
            }
            set
            {
                if (m_TextViewport == value)
                    return;
                m_TextViewport = value;
            }
        }

        public bool showDoneButton
        {
            get
            {
                return m_ShowDoneButton;
            }
            set
            {
                if (m_ShowDoneButton == value)
                    return;
                m_ShowDoneButton = value;
                ApplyNativeKeyboardParams();
            }
        }

        public bool showClearButton
        {
            get
            {
                return m_ShowClearButton;
            }
            set
            {
                if (m_ShowClearButton == value)
                    return;
                m_ShowClearButton = value;
                ApplyNativeKeyboardParams();
            }
        }

        public MobileInputBehaviour.ReturnKeyType returnKeyType
        {
            get
            {
                return m_ReturnKeyType;
            }
            set
            {
                if (m_ReturnKeyType == value)
                    return;
                m_ReturnKeyType = value;
                ApplyNativeKeyboardParams();
            }
        }

        #endregion

        #region Callbacks

        [Header("Native Input Field Callbacks")]
        public UnityEvent OnReturnPressed = new UnityEvent();

        #endregion

        #region Constructors

        public NativeInputField()
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

                        Debug.LogWarning("[NativeInputField] Not Supported Platform (sender " + name + ")");
                        GameObject.Destroy(nativeBox);
                    }
                }
            }

            ApplyNativeKeyboardParams();
            //Activate native edit box
            if (nativeBox != null)
            {
                nativeBox.hideFlags = HideFlags.None;
                if (enabled && !nativeBox.enabled)
                    nativeBox.enabled = true;
            }
            RegisterEvents();
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

            //We can only activate inputfield if not raised by pointerdown event as we want to only activate inputfield in OnPointerClick
            var pointerEventData = eventData as PointerEventData;
            if (pointerEventData == null || !pointerEventData.eligibleForClick)
                ActivateInputField();
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!isFocused && transform.parent != null)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.beginDragHandler);
            else
                base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (!isFocused && transform.parent != null)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.dragHandler);
            else
                base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (!isFocused && transform.parent != null)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.endDragHandler);
            else
                base.OnEndDrag(eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            //Check if this component is inside another "DraagHandler" (Probably an ScrollRect).
            //In this case, cancel the click is eventData.dragging is true
            if (eventData.dragging &&
                this.transform.parent != null)
            {
                var dragHandler = this.transform.parent.GetComponentInParent<IBeginDragHandler>();
                if ((dragHandler as Component) != null)
                    return;
            }

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
            //Prevent call base.ActivateInputFieldInternal() as we can't override this function
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

            base.LateUpdate();
            CheckAsteriskChar();
        }

        #endregion

        #region Helper Functions

        protected virtual void ApplyNativeKeyboardParams()
        {
            MobileInputBehaviour nativeBox = GetComponent<MobileInputBehaviour>();
            if (nativeBox != null)
            {
                var changed = m_ReturnKeyType != nativeBox.ReturnKey ||
                    m_ShowClearButton != nativeBox.IsWithClearButton ||
                    m_ShowDoneButton != nativeBox.IsWithDoneButton;

                nativeBox.ReturnKey = m_ReturnKeyType;
                nativeBox.IsWithClearButton = m_ShowClearButton;
                nativeBox.IsWithDoneButton = m_ShowDoneButton;

                if (changed)
                    MarkToRecreateKeyboard();
            }
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

        public void MarkToRecreateKeyboard()
        {
            var nativeBox = GetComponent<MobileInputBehaviour>();
            if (nativeBox != null)
                nativeBox.MarkToRecreateNativeEdit();
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

            int startPos = caretPositionInternal;
            int endPos = caretSelectPositionInternal;

            // Ensure startPos is always less then endPos to make the code simpler
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
            if (m_Keyboard != null && InPlaceEditing())
            {
                m_Keyboard.text = m_Text;
            }
        }

        protected virtual bool InPlaceEditing()
        {
            return !TouchScreenKeyboard.isSupported || TouchKeyboardAllowsInPlaceEditing;
        }

        protected virtual void Delete()
        {
            if (readOnly)
                return;

            if (caretPositionInternal == caretSelectPositionInternal)
                return;

            if (caretPositionInternal < caretSelectPositionInternal)
            {
                m_Text = text.Substring(0, caretPositionInternal) + text.Substring(caretSelectPositionInternal, text.Length - caretSelectPositionInternal);
                caretSelectPositionInternal = caretPositionInternal;
            }
            else
            {
                m_Text = text.Substring(0, caretSelectPositionInternal) + text.Substring(caretPositionInternal, text.Length - caretPositionInternal);
                caretPositionInternal = caretSelectPositionInternal;
            }
        }

        protected virtual void SendOnValueChangedAndUpdateLabel()
        {
            UpdateLabel();
            SendOnValueChanged();
        }

        protected virtual void SendOnValueChanged()
        {
            UISystemProfilerApi.AddMarker("InputField.value", this);
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
            //The security character in Android is locked to 'Buller', so we must reflect this in Unity.
            //Bullet Char
            if (Application.isPlaying && asteriskChar != '•')
                asteriskChar = '•';  
#endif
        }

        protected virtual void CheckReturnPressedNonSupportedPlatforms(Event action)
        {
            if (!TouchScreenKeyboard.isSupported &&
                (action != null && action.isKey && action.rawType == EventType.KeyDown))
            {
                var returnPressed = action.keyCode == KeyCode.Return;
                //var tabPressed = event.keyCode == KeyCode.Tab;
                if ((lineType != LineType.MultiLineNewline && returnPressed) ||
                    (lineType == LineType.MultiLineNewline && action.shift && returnPressed))
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

        #region Internal Rebuild Geometry Funtions

        public override void Rebuild(CanvasUpdate update)
        {
            if (update != CanvasUpdate.LatePreRender)
                return;
            this.UpdateGeometry();
        }

        protected void UpdateGeometry()
        {
            if (!Application.isPlaying || !this.shouldHideMobileInput)
                return;
            if (this.CachedInputRenderer == null && this.m_TextComponent != null)
            {
                GameObject gameObject = new GameObject(this.transform.name + " Input Caret");
                gameObject.hideFlags = HideFlags.DontSave;
                gameObject.transform.SetParent(this.m_TextComponent.transform.parent);
                gameObject.transform.SetSiblingIndex(m_TextComponent.transform.GetSiblingIndex());
                gameObject.layer = this.gameObject.layer;
                this.CaretRectTrans = gameObject.AddComponent<RectTransform>();
                this.CachedInputRenderer = gameObject.AddComponent<CanvasRenderer>();
                this.CachedInputRenderer.SetMaterial(Graphic.defaultGraphicMaterial, (Texture)Texture2D.whiteTexture);
                gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                this.AssignPositioningIfNeeded();
            }
            if (this.CachedInputRenderer == null)
                return;
            this.OnFillVBO(this.mesh);
            this.CachedInputRenderer.SetMesh(this.mesh);
        }

        System.Reflection.MethodInfo m_AssignPositioningIfNeededInfo = null;
        protected void AssignPositioningIfNeeded()
        {
            if (m_AssignPositioningIfNeededInfo == null)
                m_AssignPositioningIfNeededInfo = typeof(InputField).GetMethod("AssignPositioningIfNeeded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (m_AssignPositioningIfNeededInfo != null)
                m_AssignPositioningIfNeededInfo.Invoke(this, null);
        }

        System.Reflection.MethodInfo m_OnFillVBOInfo = null;
        protected void OnFillVBO(Mesh vbo)
        {
            if (this.isFocused && !this.HasTextSelection)
            {
                using (VertexHelper vbo1 = new VertexHelper())
                {
                    Rect rect = this.m_TextComponent.rectTransform.rect;
                    Vector2 size = rect.size;
                    Vector2 textAnchorPivot = Text.GetTextAnchorPivot(this.m_TextComponent.alignment);
                    Vector2 zero = Vector2.zero;
                    zero.x = Mathf.Lerp(rect.xMin, rect.xMax, textAnchorPivot.x);
                    zero.y = Mathf.Lerp(rect.yMin, rect.yMax, textAnchorPivot.y);
                    Vector2 roundingOffset = this.m_TextComponent.PixelAdjustPoint(zero) - zero + Vector2.Scale(size, textAnchorPivot);
                    roundingOffset.x = roundingOffset.x - Mathf.Floor(0.5f + roundingOffset.x);
                    roundingOffset.y = roundingOffset.y - Mathf.Floor(0.5f + roundingOffset.y);
                    this.GenerateCaret(vbo1, roundingOffset);
                    vbo1.FillMesh(vbo);
                }
            }
            else
            {
                if (m_OnFillVBOInfo == null)
                    m_OnFillVBOInfo = typeof(InputField).GetMethod("OnFillVBO", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (m_OnFillVBOInfo != null)
                    m_OnFillVBOInfo.Invoke(this, new object[] { vbo });
            }
        }

        #endregion

        #region Caret Important Functions

        protected void GenerateCaret(VertexHelper vbo, Vector2 roundingOffset)
        {
            if (!this.m_CaretVisible)
                return;
            if (this.m_CursorVerts == null)
                this.CreateCursorVerts();
            float caretWidth = (float)this.caretWidth;
            int charPos = Mathf.Max(0, this.caretPositionInternal - this.m_DrawStart);
            TextGenerator cachedTextGenerator = this.m_TextComponent.cachedTextGenerator;
            if (cachedTextGenerator == null || cachedTextGenerator.lineCount == 0)
                return;
            Vector2 zero = Vector2.zero;
            if (charPos < cachedTextGenerator.characters.Count)
            {
                UICharInfo character = cachedTextGenerator.characters[charPos];
                zero.x = character.cursorPos.x;
            }
            zero.x /= this.m_TextComponent.pixelsPerUnit;
            if ((double)zero.x > (double)this.m_TextComponent.rectTransform.rect.xMax)
                zero.x = this.m_TextComponent.rectTransform.rect.xMax;
            int characterLine = this.DetermineCharacterLine(charPos, cachedTextGenerator);
            zero.y = cachedTextGenerator.lines[characterLine].topY / this.m_TextComponent.pixelsPerUnit;
            float num = (float)cachedTextGenerator.lines[characterLine].height / this.m_TextComponent.pixelsPerUnit;
            for (int index = 0; index < this.m_CursorVerts.Length; ++index)
                this.m_CursorVerts[index].color = (Color32)this.caretColor;
            this.m_CursorVerts[0].position = new Vector3(zero.x, zero.y - num, 0.0f);
            this.m_CursorVerts[1].position = new Vector3(zero.x + caretWidth, zero.y - num, 0.0f);
            this.m_CursorVerts[2].position = new Vector3(zero.x + caretWidth, zero.y, 0.0f);
            this.m_CursorVerts[3].position = new Vector3(zero.x, zero.y, 0.0f);
            if (roundingOffset != Vector2.zero)
            {
                for (int index = 0; index < this.m_CursorVerts.Length; ++index)
                {
                    UIVertex cursorVert = this.m_CursorVerts[index];
                    cursorVert.position.x += roundingOffset.x;
                    cursorVert.position.y += roundingOffset.y;
                }
            }
            vbo.AddUIVertexQuad(this.m_CursorVerts);
            int height = Screen.height;
            zero.y = (float)height - zero.y;
            input.compositionCursorPos = zero;
        }

        protected void CreateCursorVerts()
        {
            this.m_CursorVerts = new UIVertex[4];
            for (int index = 0; index < this.m_CursorVerts.Length; ++index)
            {
                this.m_CursorVerts[index] = UIVertex.simpleVert;
                this.m_CursorVerts[index].uv0 = Vector2.zero;
            }
        }

        protected int DetermineCharacterLine(int charPos, TextGenerator generator)
        {
            for (int index = 0; index < generator.lineCount - 1; ++index)
            {
                if (generator.lines[index + 1].startCharIdx > charPos)
                    return index;
            }
            return generator.lineCount - 1;
        }

        #endregion

        #region Internal Important Functions

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

        private void SafeActivateInputFieldInternal()
        {
            if (IsUnityKeyboardSupported())
            {
                if (input.touchSupported)
                {
                    TouchScreenKeyboard.hideInput = shouldHideMobileInput;
                }

                m_Keyboard = (inputType == InputType.Password) ?
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
                input.imeCompositionMode = IMECompositionMode.On;
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
                m_SetCaretVisibleInfo = typeof(InputField).GetMethod("SetCaretVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (m_SetCaretVisibleInfo != null)
                m_SetCaretVisibleInfo.Invoke(this, null);
        }

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

        protected BaseInput input
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        System.Reflection.FieldInfo m_CachedInputRendererInfo = null;
        protected CanvasRenderer CachedInputRenderer
        {
            get
            {
                if (m_CachedInputRendererInfo == null)
                    m_CachedInputRendererInfo = typeof(InputField).GetField("m_CachedInputRenderer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return m_CachedInputRendererInfo.GetValue(this) as CanvasRenderer;
            }
            set
            {
                if (m_CachedInputRendererInfo == null)
                    m_CachedInputRendererInfo = typeof(InputField).GetField("m_CachedInputRenderer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_CachedInputRendererInfo != null)
                    m_CachedInputRendererInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_CaretRectTransInfo = null;
        protected RectTransform CaretRectTrans
        {
            get
            {
                if (m_CaretRectTransInfo == null)
                    m_CaretRectTransInfo = typeof(InputField).GetField("caretRectTrans", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return m_CaretRectTransInfo.GetValue(this) as RectTransform;
            }
            set
            {
                if (m_CaretRectTransInfo == null)
                    m_CaretRectTransInfo = typeof(InputField).GetField("caretRectTrans", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_CaretRectTransInfo != null)
                    m_CaretRectTransInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_ProcessingEventInfo = null;
        protected Event ProcessingEvent
        {
            get
            {
                if (m_ProcessingEventInfo == null)
                    m_ProcessingEventInfo = typeof(InputField).GetField("m_ProcessingEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (Event)m_ProcessingEventInfo.GetValue(this);
            }
            set
            {
                if (m_ProcessingEventInfo == null)
                    m_ProcessingEventInfo = typeof(InputField).GetField("m_ProcessingEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
                    m_ShouldActivateNextUpdateInfo = typeof(InputField).GetField("m_ShouldActivateNextUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (bool)m_ShouldActivateNextUpdateInfo.GetValue(this);
            }
            set
            {
                if (m_ShouldActivateNextUpdateInfo == null)
                    m_ShouldActivateNextUpdateInfo = typeof(InputField).GetField("m_ShouldActivateNextUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
                    m_AllowInputInfo = typeof(InputField).GetField("m_AllowInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
                    m_OriginalTextInfo = typeof(InputField).GetField("m_OriginalText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (string)m_OriginalTextInfo.GetValue(this);
            }
            set
            {
                if (m_OriginalTextInfo == null)
                    m_OriginalTextInfo = typeof(InputField).GetField("m_OriginalText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
                    m_WasCanceledInfo = typeof(InputField).GetField("m_WasCanceled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (bool)m_WasCanceledInfo.GetValue(this);
            }
            set
            {
                if (m_WasCanceledInfo == null)
                    m_WasCanceledInfo = typeof(InputField).GetField("m_WasCanceled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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