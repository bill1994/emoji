#define FIX_NEW_INPUTSYSTEM_SUPPORT

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
    public partial class TMP_NativeInputField : TMPro.TMP_InputField, INativeInputField
    {
        #region Unity Functions

#if FIX_NEW_INPUTSYSTEM_SUPPORT
        protected override void OnDisable()
        {
            BaseOnDisable();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            BaseOnPointerDown(eventData);
        }
#endif

        #endregion

        #region Base Unity Functions

        protected virtual void BaseOnDisable()
        {
#if FIX_NEW_INPUTSYSTEM_SUPPORT
            // the coroutine will be terminated, so this will ensure it restarts when we are next activated
            BlinkCoroutine = null;

            DeactivateInputField();
            if (m_TextComponent != null)
            {
                m_TextComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
                m_TextComponent.UnregisterDirtyVerticesCallback(UpdateLabel);

                if (m_VerticalScrollbar != null)
                {
                    var onScrollbarValueChangedHandler = (UnityAction<float>)System.Delegate.CreateDelegate(
                        typeof(UnityAction<float>), this, "OnScrollbarValueChange");
                    m_VerticalScrollbar.onValueChanged.RemoveListener(onScrollbarValueChangedHandler);
                }
            }
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            // Clear needs to be called otherwise sync never happens as the object is disabled.
            if (CachedInputRenderer != null)
                CachedInputRenderer.Clear();

            if (m_Mesh != null)
                DestroyImmediate(m_Mesh);

            m_Mesh = null;

            // Unsubscribe to event triggered when text object has been regenerated
            var onTextChangedHandler = (System.Action<Object>)System.Delegate.CreateDelegate(
                typeof(System.Action<Object>), this, "ON_TEXT_CHANGED");

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(onTextChangedHandler);

            BaseSelectableOnDisable();
#else
            base.OnDisable();
#endif
        }

        protected virtual void BaseLateUpdate()
        {
#if FIX_NEW_INPUTSYSTEM_SUPPORT
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

            // Update Scrollbar if needed
            if (IsScrollbarUpdateRequired)
            {
                UpdateScrollbar();
                IsScrollbarUpdateRequired = false;
            }

            // Handle double click to reset / deselect Input Field when ResetOnActivation is false.
            if (!isFocused && SelectionStillActive)
            {
                GameObject selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

                if (selectedObject != null && selectedObject != this.gameObject)
                {
                    if (selectedObject != SelectedObject)
                    {
                        SelectedObject = selectedObject;

                        // Release current selection of the newly selected object is another Input Field
                        if (selectedObject.GetComponent<TMP_InputField>() != null)
                        {
                            // Release selection
                            SelectionStillActive = false;
                            MarkGeometryAsDirty();
                            SelectedObject = null;
                        }
                    }

                    return;
                }

                if (ProcessingEvent != null && ProcessingEvent.rawType == EventType.MouseDown && ProcessingEvent.button == 0)
                {
                    // Check for Double Click
                    bool isDoubleClick = false;
                    float timeStamp = Time.unscaledTime;

                    if (KeyDownStartTime + DoubleClickDelay > timeStamp)
                        isDoubleClick = true;

                    KeyDownStartTime = timeStamp;

                    if (isDoubleClick)
                    {
                        SelectionStillActive = false;

                        MarkGeometryAsDirty();

                        return;
                    }
                }
            }

            //UpdateMaskRegions();

            if (InPlaceEditing() && IsKeyboardUsingEvents() || !isFocused)
            {
                return;
            }

            AssignPositioningIfNeeded();

            if (m_SoftKeyboard == null || m_SoftKeyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                if (m_SoftKeyboard != null)
                {
                    if (!readOnly)
                        text = m_SoftKeyboard.text;

                    if (m_SoftKeyboard.status == TouchScreenKeyboard.Status.LostFocus)
                        SendTouchScreenKeyboardStatusChanged();

                    if (m_SoftKeyboard.status == TouchScreenKeyboard.Status.Canceled)
                    {
                        ReleaseBaseSelection = true;
                        WasCanceled = true;
                        SendTouchScreenKeyboardStatusChanged();
                    }

                    if (m_SoftKeyboard.status == TouchScreenKeyboard.Status.Done)
                    {
                        ReleaseBaseSelection = true;
                        OnSubmit(null);
                        SendTouchScreenKeyboardStatusChanged();
                    }
                }

                OnDeselect(null);
                return;
            }

            string val = m_SoftKeyboard.text;

            if (m_Text != val)
            {
                if (readOnly)
                {
                    m_SoftKeyboard.text = m_Text;
                }
                else
                {
                    m_Text = "";

                    for (int i = 0; i < val.Length; ++i)
                    {
                        char c = val[i];

                        if (c == '\r' || (int)c == 3)
                            c = '\n';

                        if (onValidateInput != null)
                            c = onValidateInput(m_Text, m_Text.Length, c);
                        else if (characterValidation != CharacterValidation.None)
                            c = Validate(m_Text, m_Text.Length, c);

                        if (lineType == LineType.MultiLineSubmit && c == '\n')
                        {
                            m_SoftKeyboard.text = m_Text;

                            OnSubmit(null);
                            OnDeselect(null);
                            return;
                        }

                        if (c != 0)
                            m_Text += c;
                    }

                    if (characterLimit > 0 && m_Text.Length > characterLimit)
                        m_Text = m_Text.Substring(0, characterLimit);

                    UpdateStringPositionFromKeyboard();

                    // Set keyboard text before updating label, as we might have changed it with validation
                    // and update label will take the old value from keyboard if we don't change it here
                    if (m_Text != val)
                        m_SoftKeyboard.text = m_Text;

                    SendOnValueChangedAndUpdateLabel();
                }
            }
            else if (shouldHideMobileInput && Application.platform == RuntimePlatform.Android)
            {
                UpdateStringPositionFromKeyboard();
            }

            if (m_SoftKeyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                if (m_SoftKeyboard.status == TouchScreenKeyboard.Status.Canceled)
                    WasCanceled = true;

                OnDeselect(null);
            }
#else
            base.LateUpdate();
#endif
        }

        protected virtual void BaseOnPointerDown(PointerEventData eventData)
        {
#if FIX_NEW_INPUTSYSTEM_SUPPORT
            if (!MayDrag(eventData))
                return;

            EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            bool hadFocusBefore = AllowInput;
            BaseSelectableOnPointerDown(eventData);

            if (InPlaceEditing() == false)
            {
                if (m_SoftKeyboard == null || !m_SoftKeyboard.active)
                {
                    OnSelect(eventData);
                    return;
                }
            }

            Event.PopEvent(ProcessingEvent);
            bool shift = ProcessingEvent != null && (ProcessingEvent.modifiers & EventModifiers.Shift) != 0;

            // Check for Double Click
            bool isDoubleClick = false;
            float timeStamp = Time.unscaledTime;

            if (PointerDownClickStartTime + DoubleClickDelay > timeStamp)
                isDoubleClick = true;

            PointerDownClickStartTime = timeStamp;

            // Only set caret position if we didn't just get focus now.
            // Otherwise it will overwrite the select all on focus.
            if (hadFocusBefore || !m_OnFocusSelectAll)
            {
                CaretPosition insertionSide;

                int insertionIndex = TMP_TextUtilities.GetCursorIndexFromPosition(m_TextComponent, eventData.position, eventData.pressEventCamera, out insertionSide);

                if (shift)
                {
                    if (m_isRichTextEditingAllowed)
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                    else
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringSelectPositionInternal = insertionIndex == 0
                                ? m_TextComponent.textInfo.characterInfo[0].index
                                : m_TextComponent.textInfo.characterInfo[insertionIndex - 1].index + m_TextComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                }
                else
                {
                    if (m_isRichTextEditingAllowed)
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringPositionInternal = stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringPositionInternal = stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                    else
                    {
                        if (insertionSide == CaretPosition.Left)
                        {
                            stringPositionInternal = stringSelectPositionInternal = insertionIndex == 0
                                ? m_TextComponent.textInfo.characterInfo[0].index
                                : m_TextComponent.textInfo.characterInfo[insertionIndex - 1].index + m_TextComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                        }
                        else if (insertionSide == CaretPosition.Right)
                        {
                            stringPositionInternal = stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                }


                if (isDoubleClick)
                {
                    int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextComponent, eventData.position, eventData.pressEventCamera);

                    if (wordIndex != -1)
                    {
                        // TODO: Should behavior be different if rich text editing is enabled or not?

                        // Select current word
                        caretPositionInternal = m_TextComponent.textInfo.wordInfo[wordIndex].firstCharacterIndex;
                        caretSelectPositionInternal = m_TextComponent.textInfo.wordInfo[wordIndex].lastCharacterIndex + 1;

                        stringPositionInternal = m_TextComponent.textInfo.characterInfo[caretPositionInternal].index;
                        stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].index + m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].stringLength;
                    }
                    else
                    {
                        // Select current character
                        caretPositionInternal = insertionIndex;
                        caretSelectPositionInternal = caretPositionInternal + 1;

                        stringPositionInternal = m_TextComponent.textInfo.characterInfo[insertionIndex].index;
                        stringSelectPositionInternal = stringPositionInternal + m_TextComponent.textInfo.characterInfo[insertionIndex].stringLength;
                    }
                }
                else
                {
                    caretPositionInternal = caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal);
                }

                m_isSelectAll = false;
            }

            UpdateLabel();
            eventData.Use();
#else
            base.OnPointerDown(eventData);
#endif
        }

        #endregion

        #region Compat Public Functions

#if FIX_NEW_INPUTSYSTEM_SUPPORT
        public new void DeactivateInputField(bool clearSelection = false)
        {
            // Not activated do nothing.
            if (!AllowInput)
                return;

            HasDoneFocusTransition = false;
            AllowInput = false;

            if (m_Placeholder != null)
                m_Placeholder.enabled = string.IsNullOrEmpty(m_Text);

            if (m_TextComponent != null && IsInteractable())
            {
                if (WasCanceled && restoreOriginalTextOnEscape)
                    text = OriginalText;

                if (m_SoftKeyboard != null)
                {
                    m_SoftKeyboard.active = false;
                    m_SoftKeyboard = null;
                }

                SelectionStillActive = true;

                if (m_ResetOnDeActivation || ReleaseBaseSelection)
                {
                    //m_StringPosition = m_StringSelectPosition = 0;
                    //m_CaretPosition = m_CaretSelectPosition = 0;
                    //m_TextComponent.rectTransform.localPosition = m_DefaultTransformPosition;

                    //if (caretRectTrans != null)
                    //    caretRectTrans.localPosition = Vector3.zero;

                    SelectionStillActive = false;
                    ReleaseBaseSelection = false;
                    SelectedObject = null;
                }

                SendOnEndEdit();
                SendOnEndTextSelection();

                if (inputSystem != null)
                    inputSystem.imeCompositionMode = IMECompositionMode.Auto;
            }

            MarkGeometryAsDirty();

            // Scrollbar should be updated.
            IsScrollbarUpdateRequired = true;
        }
#endif

        #endregion

        #region Internal Selectable Class Unity Functions

#if FIX_NEW_INPUTSYSTEM_SUPPORT
        protected virtual void BaseSelectableOnDisable()
        {
            s_SelectableCount--;

            // Update the last elements index to be this index
            var currentIndexFieldInfo = typeof(Selectable).GetField("m_CurrentIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (currentIndexFieldInfo != null)
                currentIndexFieldInfo.SetValue(s_Selectables[s_SelectableCount], m_CurrentIndex);

            // Swap the last element and this element
            s_Selectables[m_CurrentIndex] = s_Selectables[s_SelectableCount];

            // null out last element.
            s_Selectables[s_SelectableCount] = null;

            InstantClearState();
        }

        protected virtual void BaseSelectableOnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Selection tracking
            if (IsInteractable() && navigation.mode != Navigation.Mode.None && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            isPointerDown = true;
            EvaluateAndTransitionToSelectionState(eventData);
        }
#endif

        #endregion

        #region Internal Compat Helper Functions

#if FIX_NEW_INPUTSYSTEM_SUPPORT
        protected void UpdateStringPositionFromKeyboard()
        {
            // TODO: Might want to add null check here.
            var selectionRange = m_SoftKeyboard.selection;

            //if (selectionRange.start == 0 && selectionRange.length == 0)
            //    return;

            var selectionStart = selectionRange.start;
            var selectionEnd = selectionRange.end;

            var stringPositionChanged = false;

            if (stringPositionInternal != selectionStart)
            {
                stringPositionChanged = true;
                stringPositionInternal = selectionStart;

                caretPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal);
            }

            if (stringSelectPositionInternal != selectionEnd)
            {
                stringSelectPositionInternal = selectionEnd;
                stringPositionChanged = true;

                caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
            }

            if (stringPositionChanged)
            {
                BlinkStartTime = Time.unscaledTime;

                UpdateLabel();
            }
        }

        protected void SendOnValueChangedAndUpdateLabel()
        {
            UpdateLabel();
            SendOnValueChanged();
        }

        protected void SendOnValueChanged()
        {
            if (onValueChanged != null)
                onValueChanged.Invoke(text);
        }

        protected void MarkGeometryAsDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return;
#endif

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        protected bool IsKeyboardUsingEvents()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.tvOS:
                    return false;
                default:
                    return true;
            }
        }

        protected void UpdateScrollbar()
        {
            // Update Scrollbar
            if (m_VerticalScrollbar)
            {
                float size = m_TextViewport.rect.height / m_TextComponent.preferredHeight;

                IsUpdatingScrollbarValues = true;

                m_VerticalScrollbar.size = size;

                ScrollPosition = m_VerticalScrollbar.value = m_TextComponent.rectTransform.anchoredPosition.y / (m_TextComponent.preferredHeight - m_TextViewport.rect.height);
            }
        }

        protected bool MayDrag(PointerEventData eventData)
        {
            return IsActive() &&
                   IsInteractable() &&
                   eventData.button == PointerEventData.InputButton.Left &&
                   m_TextComponent != null &&
                   (m_SoftKeyboard == null || shouldHideSoftKeyboard || shouldHideMobileInput);
        }

        protected int GetCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_TextComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (m_TextComponent.textInfo.characterInfo[i].index >= stringIndex)
                    return i;
            }

            return count;
        }

        protected bool InPlaceEditing()
        {
            if (TouchKeyboardAllowsInPlaceEditing || (TouchScreenKeyboard.isSupported && (Application.platform == RuntimePlatform.WSAPlayerX86 || Application.platform == RuntimePlatform.WSAPlayerX64 || Application.platform == RuntimePlatform.WSAPlayerARM)))
                return true;

            if (TouchScreenKeyboard.isSupported && shouldHideSoftKeyboard)
                return true;

            if (TouchScreenKeyboard.isSupported && shouldHideSoftKeyboard == false && shouldHideMobileInput == false)
                return false;

            return true;
        }
#endif
        #endregion

        #region Internal Compat Fields

#if FIX_NEW_INPUTSYSTEM_SUPPORT
        System.Reflection.PropertyInfo m_CompositionLengthInfo = null;
        private int compositionLength
        {
            get
            {
                if (m_CompositionLengthInfo == null)
                    m_CompositionLengthInfo = typeof(Selectable).GetProperty("compositionLength", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_CompositionLengthInfo.GetValue(this);
                return value is int ? (int)value : 0;
            }
        }

        protected BaseInput inputSystem
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        System.Reflection.PropertyInfo m_IsPointerDownInfo = null;
        protected bool isPointerDown
        {
            get
            {
                if (m_IsPointerDownInfo == null)
                    m_IsPointerDownInfo = typeof(Selectable).GetProperty("isPointerDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_IsPointerDownInfo.GetValue(this);
                return value is bool ? (bool)value : false;
            }
            set
            {
                if (m_IsPointerDownInfo == null)
                    m_IsPointerDownInfo = typeof(Selectable).GetProperty("isPointerDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_IsPointerDownInfo != null)
                    m_IsPointerDownInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_SelectedObjectInfo = null;
        protected GameObject SelectedObject
        {
            get
            {
                if (m_SelectedObjectInfo == null)
                    m_SelectedObjectInfo = typeof(TMPro.TMP_InputField).GetField("m_SelectedObject", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_SelectedObjectInfo.GetValue(this);
                return value as GameObject;
            }
            set
            {
                if (m_SelectedObjectInfo == null)
                    m_SelectedObjectInfo = typeof(TMPro.TMP_InputField).GetField("m_SelectedObject", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_SelectedObjectInfo != null)
                    m_SelectedObjectInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_HasDoneFocusTransitionInfo = null;
        protected bool HasDoneFocusTransition
        {
            get
            {
                if (m_HasDoneFocusTransitionInfo == null)
                    m_HasDoneFocusTransitionInfo = typeof(TMPro.TMP_InputField).GetField("m_HasDoneFocusTransition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_HasDoneFocusTransitionInfo.GetValue(this);
                return value is bool ? (bool)value : false;
            }
            set
            {
                if (m_HasDoneFocusTransitionInfo == null)
                    m_HasDoneFocusTransitionInfo = typeof(TMPro.TMP_InputField).GetField("m_HasDoneFocusTransition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_HasDoneFocusTransitionInfo != null)
                    m_HasDoneFocusTransitionInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_ReleaseSelectionInfo = null;
        protected bool ReleaseBaseSelection
        {
            get
            {
                if (m_ReleaseSelectionInfo == null)
                    m_ReleaseSelectionInfo = typeof(TMPro.TMP_InputField).GetField("m_ReleaseSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_ReleaseSelectionInfo.GetValue(this);
                return value is bool ? (bool)value : false;
            }
            set
            {
                if (m_ReleaseSelectionInfo == null)
                    m_ReleaseSelectionInfo = typeof(TMPro.TMP_InputField).GetField("m_ReleaseSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_ReleaseSelectionInfo != null)
                    m_ReleaseSelectionInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_SelectionStillActiveInfo = null;
        protected bool SelectionStillActive
        {
            get
            {
                if (m_SelectionStillActiveInfo == null)
                    m_SelectionStillActiveInfo = typeof(TMPro.TMP_InputField).GetField("m_SelectionStillActive", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_SelectionStillActiveInfo.GetValue(this);
                return value is bool ? (bool)value : false;
            }
            set
            {
                if (m_SelectionStillActiveInfo == null)
                    m_SelectionStillActiveInfo = typeof(TMPro.TMP_InputField).GetField("m_SelectionStillActive", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_SelectionStillActiveInfo != null)
                    m_SelectionStillActiveInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_IsScrollbarUpdateRequiredInfo = null;
        protected bool IsScrollbarUpdateRequired
        {
            get
            {
                if (m_IsScrollbarUpdateRequiredInfo == null)
                    m_IsScrollbarUpdateRequiredInfo = typeof(TMPro.TMP_InputField).GetField("m_IsScrollbarUpdateRequired", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_IsScrollbarUpdateRequiredInfo.GetValue(this);
                return value is bool ? (bool)value : false;
            }
            set
            {
                if (m_IsScrollbarUpdateRequiredInfo == null)
                    m_IsScrollbarUpdateRequiredInfo = typeof(TMPro.TMP_InputField).GetField("m_IsUpdatingScrollbarValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_IsScrollbarUpdateRequiredInfo != null)
                    m_IsScrollbarUpdateRequiredInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_IsUpdatingScrollbarValuesInfo = null;
        protected bool IsUpdatingScrollbarValues
        {
            get
            {
                if (m_IsUpdatingScrollbarValuesInfo == null)
                    m_IsUpdatingScrollbarValuesInfo = typeof(TMPro.TMP_InputField).GetField("m_IsUpdatingScrollbarValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_IsUpdatingScrollbarValuesInfo.GetValue(this);
                return value is bool ? (bool)value : false;
            }
            set
            {
                if (m_IsUpdatingScrollbarValuesInfo == null)
                    m_IsUpdatingScrollbarValuesInfo = typeof(TMPro.TMP_InputField).GetField("m_IsUpdatingScrollbarValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_IsUpdatingScrollbarValuesInfo != null)
                    m_IsUpdatingScrollbarValuesInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_BlinkStartTimeInfo = null;
        protected float BlinkStartTime
        {
            get
            {
                if (m_BlinkStartTimeInfo == null)
                    m_BlinkStartTimeInfo = typeof(TMPro.TMP_InputField).GetField("m_BlinkStartTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_BlinkStartTimeInfo.GetValue(this);
                return value is float ? (float)value : 0;
            }
            set
            {
                if (m_BlinkStartTimeInfo == null)
                    m_BlinkStartTimeInfo = typeof(TMPro.TMP_InputField).GetField("m_BlinkStartTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_BlinkStartTimeInfo != null)
                    m_BlinkStartTimeInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_ScrollPositionInfo = null;
        protected float ScrollPosition
        {
            get
            {
                if (m_ScrollPositionInfo == null)
                    m_ScrollPositionInfo = typeof(TMPro.TMP_InputField).GetField("m_ScrollPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_ScrollPositionInfo.GetValue(this);
                return value is float ? (float)value : 0;
            }
            set
            {
                if (m_ScrollPositionInfo == null)
                    m_ScrollPositionInfo = typeof(TMPro.TMP_InputField).GetField("m_ScrollPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_ScrollPositionInfo != null)
                    m_ScrollPositionInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_KeyDownStartTimeInfo = null;
        protected float KeyDownStartTime
        {
            get
            {
                if (m_KeyDownStartTimeInfo == null)
                    m_KeyDownStartTimeInfo = typeof(TMPro.TMP_InputField).GetField("m_KeyDownStartTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_KeyDownStartTimeInfo.GetValue(this);
                return value is float ? (float)value : 0;
            }
            set
            {
                if (m_KeyDownStartTimeInfo == null)
                    m_KeyDownStartTimeInfo = typeof(TMPro.TMP_InputField).GetField("m_KeyDownStartTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_KeyDownStartTimeInfo != null)
                    m_KeyDownStartTimeInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_DoubleClickDelayInfo = null;
        protected float DoubleClickDelay
        {
            get
            {
                if (m_DoubleClickDelayInfo == null)
                    m_DoubleClickDelayInfo = typeof(TMPro.TMP_InputField).GetField("m_DoubleClickDelay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_DoubleClickDelayInfo.GetValue(this);
                return value is float ? (float)value : 0;
            }
            set
            {
                if (m_DoubleClickDelayInfo == null)
                    m_DoubleClickDelayInfo = typeof(TMPro.TMP_InputField).GetField("m_DoubleClickDelay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_DoubleClickDelayInfo != null)
                    m_DoubleClickDelayInfo.SetValue(this, value);
            }
        }

        System.Reflection.FieldInfo m_PointerDownClickStartTimeInfo = null;
        protected float PointerDownClickStartTime
        {
            get
            {
                if (m_PointerDownClickStartTimeInfo == null)
                    m_PointerDownClickStartTimeInfo = typeof(TMPro.TMP_InputField).GetField("m_PointerDownClickStartTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var value = m_PointerDownClickStartTimeInfo.GetValue(this);
                return value is float ? (float)value : 0;
            }
            set
            {
                if (m_PointerDownClickStartTimeInfo == null)
                    m_PointerDownClickStartTimeInfo = typeof(TMPro.TMP_InputField).GetField("m_PointerDownClickStartTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (m_PointerDownClickStartTimeInfo != null)
                    m_PointerDownClickStartTimeInfo.SetValue(this, value);
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
#endif

        #endregion
    }
}
