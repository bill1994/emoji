// ----------------------------------------------------------------------------
// The MIT License
// Based in UnityMobileInput https://github.com/mopsicus/UnityMobileInput
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using Kyub.Internal.NativeInputPlugin.NiceJson;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Kyub.Internal.NativeInputPlugin
{
    public enum NativeInputPluginMaskModeEnum { Manual = 0, Auto = 1 }
    public interface INativeInputField
    {
        char asteriskChar { get; set; }
        string text { get; set; }
        GameObject gameObject { get; }
        Transform transform { get; }
        bool isFocused { get; }
        bool enabled { get; set; }
        int characterLimit { get; set; }
        Graphic placeholder { get; set; }
        Graphic textComponent { get; }
        RectTransform textViewport { get; set; }
        TouchScreenKeyboardType keyboardType { get; set; }

        bool shouldHideMobileInput { get; set; }

        //Used as reference to PAN all screen
        RectTransform panContent { get; set; }

        //Used as clip rect while update native rect position
        RectTransform rectMaskContent { get; set; }

        UnityEvent<string> onValueChanged { get; }
        UnityEvent<string> onEndEdit { get; }
        UnityEvent onReturnPressed { get; }

        bool SetTextNative(string text);
        void RecreateKeyboard();
        void ActivateInputField();
        void DeactivateInputField();
        void ProcessEvent(Event e);
        bool IsDestroyed();

        void ActivateInputWithoutNotify();
    }

    public class MobileInputBehaviour : MobileInputReceiver
    {
        #region Static Helper Functions

        public static bool IsSupported()
        {
            return TouchScreenKeyboard.isSupported && !Application.isEditor && Application.isPlaying;
        }

        #endregion

        #region Helper Classes

        private struct MobileInputConfig
        {
            public bool Multiline;
            public Color TextColor;
            public Color BackgroundColor;
            public string ContentType;
            public string InputType;
            public string KeyboardType;
            public string Font;
            public float FontSize;
            public string Align;
            public string Placeholder;
            public Color PlaceholderColor;
            public int CharacterLimit;
        }

        public enum ReturnKeyType
        {
            Default,
            Next,
            Done,
            Search
        }

        #endregion

        #region Consts

        const string CREATE = "CREATE_EDIT";
        const string REMOVE = "REMOVE_EDIT";
        const string SET_TEXT = "SET_TEXT";
        const string SET_RECT = "SET_RECT";
        const string SET_FOCUS = "SET_FOCUS";
        const string ON_FOCUS = "ON_FOCUS";
        const string ON_UNFOCUS = "ON_UNFOCUS";
        const string SET_VISIBLE = "SET_VISIBLE";
        const string TEXT_CHANGE = "TEXT_CHANGE";
        const string TEXT_END_EDIT = "TEXT_END_EDIT";
        const string ANDROID_KEY_DOWN = "ANDROID_KEY_DOWN";
        const string DONE_KEY_DOWN = "DONE_KEY_DOWN";
        const string RETURN_PRESSED = "RETURN_PRESSED";
        const string READY = "READY";

        #endregion

        #region Fields

        [SerializeField]
        bool m_requireRecreate = false;
        [SerializeField]
        private bool m_isWithDoneButton = true;
        [SerializeField]
        private bool m_isWithClearButton = true;
        [SerializeField]
        private ReturnKeyType m_returnKey = ReturnKeyType.Done;

        public UnityEvent OnReturnPressedEvent = new UnityEvent();

        public Action OnReturnPressed = delegate { };
        public Action<bool> OnFocusChanged = delegate { };

        private bool _isMobileInputCreated = false;
        private INativeInputField _inputObject;
        private Graphic _inputObjectText;

        private bool _isFocusOnCreate;
        private bool _isVisibleOnCreate = false;
        private bool _isVisible = false;

        //int _defaultDelayCount = 5;

        //private Rect _lastRect;

        /*#if (UNITY_IPHONE) && !UNITY_EDITOR
                private bool _cachedStatusBarHidden = iOSStatusBar.iOSStatusBarManager.IsStatusBarHidden();
        #endif*/

        private MobileInputConfig _config;

        #endregion

        #region Properties

        protected BaseInput input
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        public INativeInputField InputField
        {
            get
            {
                return _inputObject;
            }
        }

        public bool Visible
        {
            get { return _isVisible; }
            private set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                _isFocusOnCreate = value;
                _isVisibleOnCreate = value;
                /*#if (UNITY_IPHONE) && !UNITY_EDITOR
                                if (!_cachedStatusBarHidden)
                                {
                                    iOSStatusBar.iOSStatusBarManager.Show(!_isVisible);
                                }
                #endif*/

#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
                if (_isVisible)
                    RegisterInputFieldEvents();
                else
                    UnregisterInputFieldEvents();
#endif
            }
        }

        public bool IsVisibleOnCreate
        {
            get
            {
                return _isVisibleOnCreate;
            }
        }

        public bool IsFocusOnCreate
        {
            get
            {
                return _isFocusOnCreate;
            }
        }

        public string Text
        {
            get
            {
                return _inputObject.text;
            }
            set
            {
                _inputObject.text = value;
                SetTextNative(value);
            }
        }

        public bool IsWithDoneButton
        {
            get
            {
                return m_isWithDoneButton;
            }
            set
            {
                if (m_isWithDoneButton == value)
                    return;
                m_isWithDoneButton = value;
                if (_isMobileInputCreated)
                    MarkToRecreateNativeEdit();
            }
        }
        public bool IsWithClearButton
        {
            get
            {
                return m_isWithClearButton;
            }
            set
            {
                if (m_isWithClearButton == value)
                    return;
                m_isWithClearButton = value;
                if (_isMobileInputCreated)
                    MarkToRecreateNativeEdit();
            }
        }
        public ReturnKeyType ReturnKey
        {
            get
            {
                return m_returnKey;
            }
            set
            {
                if (m_returnKey == value)
                    return;
                m_returnKey = value;
                if (_isMobileInputCreated)
                    MarkToRecreateNativeEdit();
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            _inputObject = this.GetComponent<INativeInputField>();
            if ((UnityEngine.Object)_inputObject == null)
            {
                Debug.LogError(string.Format("No INativeInputField for {0} MobileInput", this.name));
                throw new MissingComponentException();
            }
            _inputObjectText = _inputObject.textComponent;
        }

        protected virtual void OnEnable()
        {
            if (m_requireRecreate && _started)
            {
                m_requireRecreate = false;
                RecreateNativeEdit(Visible);
            }
        }

        protected bool _started = false;
        protected override void Start()
        {
            if (IsSupported())
            {
                _started = true;
                base.Start();

                // Wait until the end of frame before initializing to ensure that Unity UI layout has been built. We used to
                // initialize at Start, but that resulted in an invalid RectTransform position and size on the InputField if it
                // was instantiated at runtime instead of being built in to the scene.
                if (m_requireRecreate)
                    RecreateNativeEdit();
            }
            else
            {
                _isMobileInputCreated = false;
                if (this != null)
                {
                    Debug.LogWarning("[NATIVE EDITBOX] Not Supported Platform (sender " + name + ")");
                    Component.DestroyImmediate(this);
                }
            }
        }

        /// <summary>
        /// Hide native on disable
        /// </summary>
        protected virtual void OnDisable()
        {
            if (_isMobileInputCreated)
            {
                StopCoroutine("SetFocusRoutine");
                this.SetVisible(false, false);
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        protected override void OnDestroy()
        {
            if (IsSupported())
            {
                RemoveNative();
                base.OnDestroy();
            }
        }

        /// <summary>
        /// Handler for app focus lost
        /// </summary>
        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (!_isMobileInputCreated || !this.Visible)
            {
                return;
            }
            this.SetVisibleAndFocus_Internal(hasFocus && _inputObject != null && _inputObject.isFocused, true);
        }

        protected virtual void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            this.UpdateForceKeyeventForAndroid();
#endif

            if (Visible && this._inputObject != null && _isMobileInputCreated)
            {
                SetRectNative(this._inputObject.textViewport, this._inputObject.panContent, this._inputObject.rectMaskContent);
            }
            //Set Visible false when click out of rect
            if (Application.isMobilePlatform && input.touchCount > 0)
            {
                if (Visible && (_inputObject != null && _inputObject.isFocused))
                {
                    var panContent = _inputObject.panContent != null ? _inputObject.panContent : (RectTransform)this.transform;
                    var camera = GetComponentInParent<Canvas>().rootCanvas.worldCamera;

                    for (int i = 0; i < input.touchCount; i++)
                    {
                        var touch = input.GetTouch(i);
                        if (touch.phase == TouchPhase.Began)
                        {

                            var isInside = RectTransformUtility.RectangleContainsScreenPoint(panContent, touch.position, camera);
                            if (!isInside)
                            {
                                SetVisible(false);
                                break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper Functions

        protected virtual void RegisterInputFieldEvents()
        {
            UnregisterInputFieldEvents();
            if (_inputObject != null)
                _inputObject.onValueChanged.AddListener(HandleOnInputValueChanged);
        }

        protected virtual void UnregisterInputFieldEvents()
        {
            if (_inputObject != null)
                _inputObject.onValueChanged.RemoveListener(HandleOnInputValueChanged);
        }

        public void MarkToRecreateNativeEdit()
        {
            m_requireRecreate = true;
        }

        public void RecreateNativeEdit()
        {
            RecreateNativeEdit(Visible);
        }

        public void RecreateNativeEdit(bool isVisible)
        {
            if (!IsSupported())
                return;

            if (_isMobileInputCreated)
                RemoveNative();
            if (enabled && gameObject.activeSelf && gameObject.activeInHierarchy)
            {
                _isVisibleOnCreate = isVisible;
                _isFocusOnCreate = isVisible;

                m_requireRecreate = false;
                StopCoroutine("InitializeOnNextFrame");
                StartCoroutine("InitializeOnNextFrame", isVisible);
            }
            else
            {
                Visible = isVisible;
                m_requireRecreate = true;
            }
        }

        private IEnumerator InitializeOnNextFrame(bool isVisible)
        {
            yield return null;
            this.PrepareNativeEdit();

            _isVisibleOnCreate = isVisible;
            _isFocusOnCreate = isVisible;

            m_requireRecreate = false;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            this.CreateNativeEdit ();
            this.SetTextNative (this._inputObject.text);
#else
            SetVisibleAndFocus_Internal(isVisible, true);
#endif
        }

        public static Rect GetScreenRectFromRectTransform(RectTransform rectTransform, RectTransform clipRectTransform = null, bool invertY = true)
        {
            if (rectTransform == null)
                return Rect.zero;

            Vector3[] corners = new Vector3[4];

            if (clipRectTransform == null)
                rectTransform.GetWorldCorners(corners);

            //Intersect Rect
            else
            {

                Vector2 clipperMin = clipRectTransform.rect.min;
                Vector2 clipperMax = clipRectTransform.rect.max;

                Vector2 rectTransformMin = clipRectTransform.InverseTransformPoint(rectTransform.TransformPoint(rectTransform.rect.min));
                Vector2 rectTransformMax = clipRectTransform.InverseTransformPoint(rectTransform.TransformPoint(rectTransform.rect.max));

                //Intersect element rect with ClipRect
                var intersectRect = Rect.MinMaxRect(Mathf.Clamp(rectTransformMin.x, clipperMin.x, clipperMax.x),
                                                   Mathf.Clamp(rectTransformMin.y, clipperMin.y, clipperMax.y),
                                                   Mathf.Clamp(rectTransformMax.x, clipperMin.x, clipperMax.x),
                                                   Mathf.Clamp(rectTransformMax.y, clipperMin.y, clipperMax.y));

                //Convert interset to world space
                corners[0] = clipRectTransform.TransformPoint(new Vector2(intersectRect.xMin, intersectRect.yMin));
                corners[1] = clipRectTransform.TransformPoint(new Vector2(intersectRect.xMin, intersectRect.yMax));
                corners[2] = clipRectTransform.TransformPoint(new Vector2(intersectRect.xMax, intersectRect.yMax));
                corners[3] = clipRectTransform.TransformPoint(new Vector2(intersectRect.xMax, intersectRect.yMin));
            }

            float xMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;

            var canvas = rectTransform.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : null;
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    camera = null;
                else if (camera == null && canvas.renderMode == RenderMode.WorldSpace)
                    camera = Camera.main;
            }

            for (int i = 0; i < 4; i++)
            {
                // For Canvas mode Screen Space - Overlay there is no Camera; best solution I've found
                // is to use RectTransformUtility.WorldToScreenPoint) with a null camera.
                Vector3 screenCoord = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);

                if (screenCoord.x < xMin)
                    xMin = screenCoord.x;
                if (screenCoord.x > xMax)
                    xMax = screenCoord.x;
                if (screenCoord.y < yMin)
                    yMin = screenCoord.y;
                if (screenCoord.y > yMax)
                    yMax = screenCoord.y;
            }
            Rect result = new Rect(xMin, invertY ? (Screen.height - yMax) : yMax, xMax - xMin, yMax - yMin);
            return result;
        }

        protected void PrepareNativeEdit()
        {
            if (_inputObject != null && !_inputObject.IsDestroyed())
            {
                var inputField = _inputObject as InputField;
                var tmpInputField = _inputObject as TMP_InputField;

                var textObject = _inputObjectText as Text;
                var tmpTextObject = _inputObjectText as TMP_Text;

                Graphic placeHolder = _inputObject.placeholder != null ? _inputObject.placeholder : null;
                _config.Placeholder = placeHolder is Text ? ((Text)placeHolder).text : (placeHolder is TMP_Text ? ((TMP_Text)placeHolder).text : "");
                _config.PlaceholderColor = placeHolder != null ? placeHolder.color : Color.white;
                _config.CharacterLimit = _inputObject.characterLimit;
                _config.Font = textObject != null ? PostScriptNameUtils.GetPostScriptName(textObject.font) :
                    (tmpTextObject != null ? PostScriptNameUtils.GetPostScriptName(tmpTextObject.font) : string.Empty);

                _config.FontSize = GetNativeFontSize();
                _config.TextColor = _inputObjectText.color;
                _config.Align = GetGraphicTextAnchor(_inputObjectText).ToString();
                _config.ContentType = inputField != null ? inputField.contentType.ToString() :
                    (tmpInputField != null ? tmpInputField.contentType.ToString() : "");
                _config.BackgroundColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                _config.Multiline = (inputField != null && inputField.lineType != UnityEngine.UI.InputField.LineType.SingleLine) ||
                                    (tmpInputField != null && tmpInputField.lineType != TMP_InputField.LineType.SingleLine);
                _config.KeyboardType = _inputObject.keyboardType.ToString();
                _config.InputType = inputField != null ? inputField.inputType.ToString() :
                    (tmpInputField != null ? tmpInputField.inputType.ToString() : "");
            }
        }

        protected virtual float GetNativeFontSize()
        {
            var textObject = _inputObjectText as Text;
            var tmpTextObject = _inputObjectText as TMP_Text;
            Rect rect = GetScreenRectFromRectTransform(this._inputObjectText.rectTransform);
            float ratio = rect.height / _inputObjectText.rectTransform.rect.height;
            var fontSize = (textObject != null ? ((float)textObject.fontSize) : (tmpTextObject != null ? tmpTextObject.fontSize : 0)) * ratio;
            return fontSize;
        }

        #region Receivers

        private void OnTextChange(string text)
        {
            if (text == this._inputObject.text)
                return;

            UnregisterInputFieldEvents();
            this._inputObject.text = text;
            if (this._inputObject.onValueChanged != null)
                this._inputObject.onValueChanged.Invoke(text);

            if (this.Visible)
            {
                RegisterInputFieldEvents();
                CheckUnityFieldsVisibility();
            }
        }

        private void OnTextEditEnd(string text)
        {
            UnregisterInputFieldEvents();
            this._inputObject.text = text;
            if (this._inputObject.onEndEdit != null)
                this._inputObject.onEndEdit.Invoke(text);

            if (this.Visible)
                RegisterInputFieldEvents();
            SetVisible(false);
        }

        //Used only to Sync Text value with native value (when someone change inputfield.text with keyboard opened)
        private void HandleOnInputValueChanged(string text)
        {
            SetTextNative(text);
        }

        #endregion

        /// <summary>
        /// Sending data to plugin
        /// </summary>
        /// <param name="data">JSON</param>
        public override void Send(JsonObject data)
        {
            MobileInput.Plugin.StartCoroutine(PluginsMessageRoutine(data));
        }

        /// <summary>
        /// Remove focus, keyboard when app lose focus
        /// </summary>
        public override void Hide()
        {
            this.SetVisibleAndFocus_Internal(false, true);
        }

        /// <summary>
        /// Coroutine for send, so its not freeze main thread
        /// </summary>
        /// <param name="data">JSON</param>
        private IEnumerator PluginsMessageRoutine(JsonObject data)
        {
            yield return null;

            string msg = data["msg"];
            if (msg.Equals(TEXT_CHANGE))
            {
                string text = data["text"];
                this.OnTextChange(text);
            }
            else if (msg.Equals(TEXT_END_EDIT))
            {
                string text = data["text"];
                this.OnTextEditEnd(text);
            }
            else if (msg.Equals(RETURN_PRESSED) || msg.Equals(DONE_KEY_DOWN))
            {
                var isDone = msg.Equals(DONE_KEY_DOWN);
                var inputField = _inputObject as InputField;
                var tmpInputField = _inputObject as TMP_InputField;

                if (isDone ||
                    (inputField != null && (inputField.lineType == UnityEngine.UI.InputField.LineType.MultiLineSubmit || inputField.lineType == UnityEngine.UI.InputField.LineType.SingleLine)) ||
                    (tmpInputField != null && (tmpInputField.lineType == TMP_InputField.LineType.MultiLineSubmit || tmpInputField.lineType == TMP_InputField.LineType.SingleLine)))
                {
                    if (OnReturnPressed != null)
                        OnReturnPressed();

                    SetVisibleAndFocus_Internal(false, true);

                    if (OnReturnPressedEvent != null)
                        OnReturnPressedEvent.Invoke();
                }
                else
                {
                    var text = data["text"] + "\n";
                    SetTextNative(text);
                    this.OnTextEditEnd(text);
                }

            }
            else if (msg.Equals(READY))
            {
                this.Ready();
            }
            else if (msg.Equals(ON_FOCUS))
            {
                /*var isVisible = true;
                if (this.Visible != isVisible)
                {
                    this.Visible = isVisible;
                    CheckUnityFieldsVisibility();
                }*/
                OnFocusChanged(true);
            }
            else if (msg.Equals(ON_UNFOCUS))
            {
                /*var isVisible = false;
                if (this.Visible != isVisible)
                {
                    this.Visible = isVisible;
                    CheckUnityFieldsVisibility();
                }*/
                OnFocusChanged(false);
            }
        }

        /// <summary>
        /// Create native input field
        /// </summary>
        private void CreateNativeEdit()
        {
            Rect rect = GetScreenRectFromRectTransform(this._inputObject.textViewport);
            Rect panContentRect = GetScreenRectFromRectTransform(this._inputObject.panContent);
            JsonObject data = new JsonObject();
            data["msg"] = CREATE;
            data["x"] = rect.x / Screen.width;
            data["y"] = rect.y / Screen.height;
            data["width"] = rect.width / Screen.width;
            data["height"] = rect.height / Screen.height;

            data["pan_content_x"] = panContentRect.x / Screen.width;
            data["pan_content_y"] = panContentRect.y / Screen.height;
            data["pan_content_width"] = panContentRect.width / Screen.width;
            data["pan_content_height"] = panContentRect.height / Screen.height;

            data["character_limit"] = _config.CharacterLimit;
            data["text_color_r"] = _config.TextColor.r;
            data["text_color_g"] = _config.TextColor.g;
            data["text_color_b"] = _config.TextColor.b;
            data["text_color_a"] = _config.TextColor.a;
            data["back_color_r"] = _config.BackgroundColor.r;
            data["back_color_g"] = _config.BackgroundColor.g;
            data["back_color_b"] = _config.BackgroundColor.b;
            data["back_color_a"] = _config.BackgroundColor.a;
            data["font"] = _config.Font;
            data["font_size"] = _config.FontSize;
            data["content_type"] = _config.ContentType;
            data["align"] = _config.Align;
            data["with_done_button"] = this.IsWithDoneButton;
            data["with_clear_button"] = this.IsWithClearButton;
            data["placeholder"] = _config.Placeholder;
            data["placeholder_color_r"] = _config.PlaceholderColor.r;
            data["placeholder_color_g"] = _config.PlaceholderColor.g;
            data["placeholder_color_b"] = _config.PlaceholderColor.b;
            data["placeholder_color_a"] = _config.PlaceholderColor.a;
            data["multiline"] = _config.Multiline;
            data["input_type"] = _config.InputType;
            data["keyboard_type"] = _config.KeyboardType;
            switch (ReturnKey)
            {
                case ReturnKeyType.Next:
                    data["return_key_type"] = "Next";
                    break;
                case ReturnKeyType.Done:
                    data["return_key_type"] = "Done";
                    break;
                case ReturnKeyType.Search:
                    data["return_key_type"] = "Search";
                    break;
                default:
                    data["return_key_type"] = "Default";
                    break;
            }
            Debug.Log("MobileInput CreateNativeEdit " + data.ToJsonString());
            this.Execute(data);
            _isMobileInputCreated = true;
            //Ready();
        }

        void Ready()
        {
            _isMobileInputCreated = true;
            SetVisibleAndFocus_Internal(_isVisibleOnCreate, _isFocusOnCreate, true);

            //SetVisible(_isVisibleOnCreate);
            /*if (_isFocusOnCreate)
            {
                SetFocus(true);
            }*/
        }

        void SetTextNative(string text)
        {
            if (_isMobileInputCreated)
            {
                if (string.IsNullOrEmpty(text))
                    text = string.Empty;
                JsonObject data = new JsonObject();
                data["msg"] = SET_TEXT;
                data["text"] = text;
                this.Execute(data);
            }
        }

        /// <summary>
        /// Remove field
        /// </summary>
        private void RemoveNative()
        {
            if (_isMobileInputCreated)
            {
                _isMobileInputCreated = false;
                JsonObject data = new JsonObject();
                data["msg"] = REMOVE;
                this.Execute(data);
            }
        }

        /// <summary>
        /// Set new size and position
        /// </summary>
        /// <param name="inputRect">RectTransform</param>
        public void SetRectNative(RectTransform inputRect, RectTransform panContent, RectTransform clipperRect)
        {
            if (_isMobileInputCreated)
            {
                Rect rect = GetScreenRectFromRectTransform(inputRect, clipperRect);
                Rect panContentRect = GetScreenRectFromRectTransform(panContent);
                //if (_lastRect == rect)
                //{
                //    return;
                //}
                //_lastRect = rect;

                JsonObject data = new JsonObject();
                data["msg"] = SET_RECT;
                data["x"] = rect.x / Screen.width;
                data["y"] = rect.y / Screen.height;
                data["width"] = rect.width / Screen.width;
                data["height"] = rect.height / Screen.height;

                data["pan_content_x"] = panContentRect.x / Screen.width;
                data["pan_content_y"] = panContentRect.y / Screen.height;
                data["pan_content_width"] = panContentRect.width / Screen.width;
                data["pan_content_height"] = panContentRect.height / Screen.height;

                this.Execute(data);
            }
        }

        /// <summary>
        /// Set focus on field
        /// </summary>
        /// <param name="isFocus">true | false</param>
        public void SetFocus(bool isFocus)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!_isMobileInputCreated) {
                _isFocusOnCreate = isFocus;
                return;
            }
            
            JsonObject data = new JsonObject ();
            data["msg"] = SET_FOCUS;
            data["is_focus"] = isFocus;
            this.Execute (data);
#else
            if (gameObject.activeInHierarchy)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(isFocus ? this.gameObject : null);

                if (_inputObject != null && !_inputObject.IsDestroyed())
                {
                    if (isFocus)
                    {
                        _inputObject.ActivateInputField();
                    }
                    else
                    {
                        _inputObject.DeactivateInputField();
                    }
                }
            }
            else
            {
                _isFocusOnCreate = isFocus;
            }
#endif
        }

        public void Show()
        {
            var isVisible = true;

            //Check if we must force recreate
            if (!m_requireRecreate)
                m_requireRecreate = !_isMobileInputCreated || !Mathf.Approximately(GetNativeFontSize(), _config.FontSize);

            if (m_requireRecreate)
            {
                m_requireRecreate = false;
                RecreateNativeEdit(isVisible);
            }
            else
                SetVisibleAndFocus_Internal(isVisible, false);
        }

        protected void SetVisibleAndFocus_Internal(bool isVisibleAndFocus)
        {
            SetVisibleAndFocus_Internal(isVisibleAndFocus, isVisibleAndFocus, false);
        }

        protected void SetVisibleAndFocus_Internal(bool isVisibleAndFocus, bool delayed)
        {
            SetVisibleAndFocus_Internal(isVisibleAndFocus, isVisibleAndFocus, delayed);
        }

        protected void SetVisibleAndFocus_Internal(bool isVisible, bool isFocus, bool delayed)
        {
            SetVisible(isVisible);
            StopCoroutine("SetFocusRoutine");
            if (enabled && gameObject.activeInHierarchy && _isMobileInputCreated && delayed)
            {
                StartCoroutine("SetFocusRoutine", isFocus);
            }
            else
            {
                SetFocus(isFocus);
            }
        }

        private IEnumerator SetFocusRoutine(bool isVisible)
        {
            /*var waitCounter = (isVisible ? 1 : _defaultDelayCount);
            for (int i = 0; i < (isVisible ? 1 : _defaultDelayCount); i++)
            {
                yield return null;
            }*/

            var delay = isVisible ? 0.03333f : GetDefaultKeyboardStateChangedDelay();
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            yield return null;

            SetFocus(isVisible);
        }

        public void SetVisible(bool isVisible)
        {
            SetVisible(isVisible, !isVisible);
        }

        public void SetVisible(bool isVisible, bool delayed)
        {
            if (!_isMobileInputCreated)
            {
                _isVisibleOnCreate = isVisible;
                return;
            }
            StopCoroutine("EmitVisibleMsgRoutine");
            if (this.Visible != isVisible)
            {
                this.Visible = isVisible;
                //if (isVisible)
                //    _lastRect = Rect.zero;
            }
            //Try Delay betweem EmitVisible to prevent recrete keyboard while selecting another input field
            if (enabled && gameObject.activeInHierarchy && delayed)
            {
                StartCoroutine("EmitVisibleMsgRoutine", isVisible);
            }
            else
            {
                JsonObject data = new JsonObject();
                data["msg"] = SET_VISIBLE;
                data["is_visible"] = isVisible;
                this.Execute(data);
                CheckUnityFieldsVisibility();
            }
        }

        protected virtual IEnumerator EmitVisibleMsgRoutine(bool isVisible)
        {
            /*var delay = isVisible ? 0 : _defaultDelayCount;
            for (int i = 0; i < delay; i++)
            {
                yield return null;
            }*/

            var delay = isVisible ? 0 : GetDefaultKeyboardStateChangedDelay();
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            yield return null;

            JsonObject data = new JsonObject();
            data["msg"] = SET_VISIBLE;
            data["is_visible"] = isVisible;
            this.Execute(data);
            CheckUnityFieldsVisibility();
        }

        protected float GetDefaultKeyboardStateChangedDelay()
        {
            var defaultDelay = 0.1f;
            if (Environment.ProcessorCount <= 4)
                defaultDelay = 0.2f;
            return defaultDelay;
        }

        protected virtual void CheckUnityFieldsVisibility()
        {
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
            var isVisible = this.Visible && _isMobileInputCreated;
            if (_inputObject != null && _inputObject.placeholder != null)
                _inputObject.placeholder.gameObject.SetActive(!isVisible);
            SetUnityTextEnabled(!isVisible);

            if(_inputObject != null)
            {
                var isFocused = _inputObject.isFocused;
                _inputObject.enabled = !isVisible;
                if(isFocused && isVisible)
                    _inputObject.ActivateInputWithoutNotify();
            }
#endif
        }

        protected virtual void SetUnityTextEnabled(bool enabled)
        {
            //Add a canvas group in unitytext component to prevent disable it (this will make UI Layout be updated preventing bugs with Layouts)
            if (_inputObjectText != null)
            {
                var group = _inputObjectText.GetComponent<CanvasGroup>();
                if (group == null)
                    group = _inputObjectText.gameObject.AddComponent<CanvasGroup>();
                group.alpha = enabled ? 1 : 0;
            }
        }

        #endregion

        #region Android Fix Functions

#if UNITY_ANDROID && !UNITY_EDITOR

        /// <summary>
        /// Send android button state
        /// </summary>
        /// <param name="key">Code</param>
        private void ForceSendKeydownAndroid (string key) 
        {
            if (_isMobileInputCreated)
            {
                JsonObject data = new JsonObject ();
                data["msg"] = ANDROID_KEY_DOWN;
                data["key"] = key;
                this.Execute (data);
            }
        }

        /// <summary>
        /// Keyboard handler
        /// </summary>
        private void UpdateForceKeyeventForAndroid () {
            if (UnityEngine.Input.anyKeyDown) {
                if (UnityEngine.Input.GetKeyDown (KeyCode.Backspace)) {
                    this.ForceSendKeydownAndroid ("backspace");
                } else {
                    foreach (char c in UnityEngine.Input.inputString) {
                        if (c == '\n') {
                            this.ForceSendKeydownAndroid ("enter");
                        } else {
                            this.ForceSendKeydownAndroid (Input.inputString);
                        }
                    }
                }
            }
        }
#endif

        #endregion

        #region TMPPro Conversors

        static TextAnchor GetGraphicTextAnchor(Graphic textGraphic)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                    return TMPTextAlignToTextAnchor(((TMPro.TMP_Text)textGraphic).alignment);
                else if (textGraphic is Text)
                    return ((Text)textGraphic).alignment;
            }

            return TextAnchor.MiddleCenter;
        }

        static TextAnchor TMPTextAlignToTextAnchor(TMPro.TextAlignmentOptions align)
        {
            if (align == TMPro.TextAlignmentOptions.Bottom)
                return TextAnchor.LowerCenter;
            else if (align == TMPro.TextAlignmentOptions.BottomLeft)
                return TextAnchor.LowerLeft;
            else if (align == TMPro.TextAlignmentOptions.BottomRight)
                return TextAnchor.LowerRight;
            if (align == TMPro.TextAlignmentOptions.Top)
                return TextAnchor.UpperCenter;
            else if (align == TMPro.TextAlignmentOptions.TopLeft)
                return TextAnchor.UpperLeft;
            else if (align == TMPro.TextAlignmentOptions.TopRight)
                return TextAnchor.UpperRight;
            else if (align == TMPro.TextAlignmentOptions.Left)
                return TextAnchor.MiddleLeft;
            else if (align == TMPro.TextAlignmentOptions.Right)
                return TextAnchor.MiddleRight;

            return TextAnchor.MiddleCenter;
        }

        #endregion
    }
}