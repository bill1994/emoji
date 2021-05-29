using System.Linq;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.Events;
using Kyub;
using MaterialUI.Internal;

namespace MaterialUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [ExecuteInEditMode]
    //[RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("MaterialUI/Material Input Field", 100)]
    public class MaterialInputField : SelectableStyleElement<MaterialInputField.InputFieldStyleProperty>, ILayoutGroup, ILayoutElement, IDeselectHandler, ISerializationCallbackReceiver
    {
        [System.Serializable]
        public class DialogPromptAddress : ComponentPrefabAddress<DialogPrompt>
        {
            public static explicit operator DialogPromptAddress(string s)
            {
                return new DialogPromptAddress() { AssetPath = s };
            }
        }

        public enum BackgroundLayoutMode
        {
            TextOnly,
            TextAndUpperContent,
            TextAndLowerContent,
            All,
            Manual
        }

        public enum InputPromptDisplayMode
        {
            None,
            WhenCanShowMobileInput,
            WhenSupportTouchScreen,
            Always
        }

        public enum LineLayoutMode
        {
            Default,
            IgnoreContent,
            IgnoreContentAndPadding
        }

        public enum ClearButtonDisplayModeEnum
        {
            Manual,
            Auto
        }

        public enum ColorSelectionState
        {
            EnabledSelected,
            EnabledDeselected,
            DisabledSelected,
            DisabledDeselected
        }

        #region Private Varibles

        //This is used to override default "Unity Input Prompt" to a custom dialog
        [SerializeField]
        protected InputPromptDisplayMode m_InputPromptDisplayOption = (InputPromptDisplayMode)0;
        [SerializeField]
        protected DialogPromptAddress m_CustomPromptDialogAddress = null;
        [SerializeField]
        protected bool m_OpenPromptOnSelect = true;

        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_BackgroundSizeMode")]
        protected BackgroundLayoutMode m_BackgroundLayoutMode = BackgroundLayoutMode.TextOnly;
        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_LineSizeMode")]
        protected LineLayoutMode m_LineLayoutMode = LineLayoutMode.Default;
        //[SerializeField]
        //protected string m_HintText = null;
        [SerializeField, SerializeStyleProperty]
        protected bool m_FloatingHint = true;
        [SerializeField]
        protected bool m_HasValidation = true;
        [SerializeField]
        protected bool m_ValidateOnStart = false;
        [SerializeField]
        protected bool m_HasCharacterCounter = true;
        [SerializeField]
        protected bool m_MatchInputFieldCharacterLimit = true;
        [SerializeField]
        protected int m_CharacterLimit = 0;
        [SerializeField, SerializeStyleProperty]
        protected int m_FloatingHintFontSize = 12;
        [SerializeField, SerializeStyleProperty]
        protected bool m_FitWidthToContent = false;
        [SerializeField, SerializeStyleProperty]
        protected bool m_FitHeightToContent = true;
        [SerializeField, SerializeStyleProperty]
        protected Vector2 m_LeftContentOffset = Vector2.zero;
        [SerializeField, SerializeStyleProperty]
        protected Vector2 m_RightContentOffset = Vector2.zero;
        [SerializeField, SerializeStyleProperty]
        protected bool m_ManualPreferredWidth = false;
        [SerializeField, SerializeStyleProperty]
        protected bool m_ManualPreferredHeight = false;
        [SerializeField, SerializeStyleProperty]
        protected Vector2 m_ManualSize = Vector2.zero;
        [SerializeField]
        protected GameObject m_TextValidator = null;
        [SerializeField]
        protected RectTransform m_RectTransform = null;
        [SerializeField]
        protected RectTransform m_InputTextTransform = null;
        [SerializeField]
        protected RectTransform m_HintTextTransform = null;
        [SerializeField]
        protected RectTransform m_CounterTextTransform = null;
        [SerializeField]
        protected RectTransform m_ValidationTextTransform = null;
        [SerializeField]
        protected RectTransform m_LineTransform = null;
        [SerializeField]
        protected RectTransform m_ActiveLineTransform = null;
        [SerializeField]
        protected RectTransform m_LeftContentTransform = null;
        [SerializeField]
        protected RectTransform m_RightContentTransform = null;
        [SerializeField]
        protected RectTransform m_TopContentTransform = null;
        [SerializeField]
        protected RectTransform m_BottomContentTransform = null;
        [SerializeField]
        protected RectTransform m_ClearButton = null;

        [SerializeField, SerializeStyleProperty]
        protected Graphic m_InputText = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_HintTextObject = null;
        [SerializeField]
        protected Graphic m_CounterText = null;
        [SerializeField]
        protected Graphic m_ValidationText = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_BackgroundGraphic = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_OutlineGraphic = null;

        [SerializeField, SerializeStyleProperty]
        protected Color m_LeftContentActiveColor = MaterialColor.iconDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_LeftContentInactiveColor = MaterialColor.disabledDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_RightContentActiveColor = MaterialColor.iconDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_RightContentInactiveColor = MaterialColor.disabledDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_HintTextActiveColor = MaterialColor.textHintDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_HintTextInactiveColor = MaterialColor.disabledDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_LineActiveColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected Color m_LineInactiveColor = MaterialColor.disabledDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_BackgroundActiveColor = MaterialColor.iconDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_BackgroundInactiveColor = MaterialColor.disabledDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_OutlineActiveColor = MaterialColor.iconDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_OutlineInactiveColor = MaterialColor.disabledDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_ValidationActiveColor = MaterialColor.red500;
        [SerializeField, SerializeStyleProperty]
        protected Color m_ValidationInactiveColor = MaterialColor.disabledDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_CounterActiveColor = MaterialColor.textSecondaryDark;
        [SerializeField, SerializeStyleProperty]
        protected Color m_CounterInactiveColor = MaterialColor.disabledDark;

        [SerializeField]
        protected Graphic m_LeftContentGraphic = null;
        [SerializeField]
        protected Graphic m_RightContentGraphic = null;
        [SerializeField]
        protected float m_HintTextFloatingValue = 0;
        [SerializeField]
        protected bool m_Interactable = true;
        //[SerializeField]
        //protected bool m_LastCounterState = false;
        [SerializeField, SerializeStyleProperty]
        protected float m_AnimationDuration = 0.25f;
        [SerializeField, SerializeStyleProperty]
        protected RectOffset m_Padding = new RectOffset();
        [SerializeField, SerializeStyleProperty]
        protected ClearButtonDisplayModeEnum m_ClearButtonDisplayMode = ClearButtonDisplayModeEnum.Manual;

        [System.NonSerialized]
        protected Selectable _InputField = null;
        [System.NonSerialized]
        protected InputPromptDisplayer _PromptDisplayer = null;
        [System.NonSerialized]
        protected CanvasGroup _ActiveLineCanvasGroup = null;
        [System.NonSerialized]
        protected CanvasGroup _HintTextCanvasGroup = null;
        [System.NonSerialized]
        protected CanvasGroup _ValidationCanvasGroup = null;

        protected RectTransform _CaretTransform;
        protected CanvasGroup _CanvasGroup;
        //protected static Sprite _LineDisabledSprite;
        protected ITextValidator _CustomTextValidator;

        protected bool _AnimateHintText;
        protected bool _HasBeenSelected;

        protected int _BackgroundTweener;
        protected int _OutlineTweener;
        protected int _LeftContentTweener;
        protected int _RightContentTweener;
        protected int _HintTextTweener;
        protected int _ValidationColorTweener;
        protected int _CounterTweener;


        protected Vector2 _LastSize;
        protected bool _LastFocussedState;
        protected ColorSelectionState _CurrentSelectionState;
        protected ColorSelectionState _LastSelectionState;

        protected float _TopSectionHeight;
        protected float _BottomSectionHeight;
        protected float _LeftSectionWidth;
        protected float _RightSectionWidth;

        protected float _BottomContentHeight = 0;
        protected float _TopContentHeight = 0;

        protected int _ActiveLinePosTweener;
        protected int _ActiveLineSizeTweener;
        protected int _ActiveLineAlphaTweener;
        protected int _HintTextFloatingValueTweener;
        protected int _ValidationTweener;

        protected Vector2 _LastRectPosition;
        protected Vector2 _LastRectSize;
        protected Vector2 _LayoutSize;

        protected bool? _CachedSupportPromptStatus = null;

        protected bool _CachedIsTextValid = true;

        //#if UNITY_EDITOR
        //        private string m_LastHintText;
        //#endif
        #endregion

        #region InputField Callbacks

        public UnityEvent onActivate = new UnityEvent();
        public UnityEvent onDeactivate = new UnityEvent();

        //Events when inputfield is null and text is not null
        public InputField.OnChangeEvent onValueChanged = new InputField.OnChangeEvent();
        public InputField.OnChangeEvent onEndEdit = new InputField.OnChangeEvent();
        public UnityEvent onReturnPressed = new UnityEvent();

        /*public UnityEvent<string> onValueChanged
        {
            get
            {
                var unityInputField = m_InputField as InputField;
                var tmpInputField = m_InputField as TMP_InputField;
                var promptInputField = m_InputField as InputPromptField;

                var eventObj = unityInputField != null ? unityInputField.onValueChanged as UnityEvent<string> : (tmpInputField != null ? tmpInputField.onValueChanged as UnityEvent<string> : (promptInputField != null ? promptInputField.onValueChanged as UnityEvent<string> : null));
                if (eventObj == null)
                {
                    if (_onValueChanged == null)
                        _onValueChanged = new InputField.OnChangeEvent();
                    eventObj = _onValueChanged;
                }

                return eventObj;
            }
        }

        public UnityEvent<string> onEndEdit
        {
            get
            {
                var unityInputField = m_InputField as InputField;
                var tmpInputField = m_InputField as TMP_InputField;
                var promptInputField = m_InputField as InputPromptField;

                var eventObj = unityInputField != null ? unityInputField.onEndEdit as UnityEvent<string> : (tmpInputField != null ? tmpInputField.onEndEdit as UnityEvent<string> : (promptInputField != null ? promptInputField.onEndEdit as UnityEvent<string> : null));
                if (eventObj == null)
                {
                    if(_onEndEdit == null)
                        _onEndEdit = new InputField.OnChangeEvent();
                    eventObj = _onEndEdit;
                }

                return eventObj;
            }
        }*/

        #endregion

        #region Properties

        protected bool cachedIsTextValid
        {
            get
            {
                return _CachedIsTextValid;
            }
            set
            {
                if (_CachedIsTextValid == value)
                    return;
                _CachedIsTextValid = value;
                RefreshVisualStyles();
            }
        }

        public bool openPromptOnSelect
        {
            get
            {
                return m_OpenPromptOnSelect;
            }
            set
            {
                if (m_OpenPromptOnSelect == value)
                    return;
                m_OpenPromptOnSelect = value;
            }
        }

        public DialogPromptAddress customPromptDialogAddress
        {
            get
            {
                return m_CustomPromptDialogAddress;
            }
            set
            {
                if (m_CustomPromptDialogAddress == value)
                    return;
                m_CustomPromptDialogAddress = value;
            }
        }

        public InputPromptDisplayMode inputPromptDisplayOption
        {
            get
            {
                return m_InputPromptDisplayOption;
            }
            set
            {
                if (m_InputPromptDisplayOption == value)
                    return;
                m_InputPromptDisplayOption = value;
                if (enabled && gameObject.activeInHierarchy)
                    CheckInputPromptDisplayModeVisibility();
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

        public BackgroundLayoutMode backgroundLayoutMode
        {
            get
            {
                return m_BackgroundLayoutMode;
            }
            set
            {
                if (m_BackgroundLayoutMode == value)
                    return;
                m_BackgroundLayoutMode = value;
                SetLayoutDirty();
            }
        }

        public LineLayoutMode lineLayoutMode
        {
            get
            {
                return m_LineLayoutMode;
            }
            set
            {
                if (m_LineLayoutMode == value)
                    return;
                m_LineLayoutMode = value;
                SetLayoutDirty();
            }
        }

        public bool isPassword
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.inputType == InputField.InputType.Password : (tmpInputField != null ? tmpInputField.inputType == TMP_InputField.InputType.Password : false);
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                {
                    unityInputField.inputType = value ? InputField.InputType.Password : (unityInputField.inputType != InputField.InputType.Password ? unityInputField.inputType : InputField.InputType.Standard);
                    ForceUpdateAll();
                }
                else if (tmpInputField != null)
                {
                    tmpInputField.inputType = value ? TMP_InputField.InputType.Password : (tmpInputField.inputType != TMP_InputField.InputType.Password ? tmpInputField.inputType : TMP_InputField.InputType.Standard);
                    ForceUpdateAll();
                }
            }
        }

        public UnityEngine.UI.InputField.ContentType contentType
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.contentType : (tmpInputField != null ? (InputField.ContentType)((int)tmpInputField.contentType) : InputField.ContentType.Standard);
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                {
                    unityInputField.contentType = value;
                    ForceUpdateAll();
                }
                else if (tmpInputField != null)
                {
                    tmpInputField.contentType = (TMP_InputField.ContentType)((int)value);
                    ForceUpdateAll();
                }
            }
        }

        public InputField.CharacterValidation characterValidation
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                InputField.CharacterValidation enumValue;
                if (tmpInputField == null || !Enum.TryParse(tmpInputField.characterValidation.ToString(), true, out enumValue))
                {
                    enumValue = unityInputField != null ? unityInputField.characterValidation :
                        (tmpInputField != null ? (InputField.CharacterValidation)((int)tmpInputField.characterValidation) :
                        InputField.CharacterValidation.None);
                }
                return enumValue;
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                {
                    unityInputField.characterValidation = value;
                    ForceUpdateAll();
                }
                else if (tmpInputField != null)
                {
                    TMP_InputField.CharacterValidation enumValue;
                    if (!Enum.TryParse(tmpInputField.characterValidation.ToString(), true, out enumValue))
                    {
                        enumValue = (TMP_InputField.CharacterValidation)((int)value);
                    }
                    tmpInputField.characterValidation = enumValue;
                    ForceUpdateAll();
                }
            }
        }

        public UnityEngine.UI.InputField.InputType inputType
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.inputType : (tmpInputField != null ? (InputField.InputType)((int)tmpInputField.inputType) : InputField.InputType.Standard);
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                {
                    unityInputField.inputType = value;
                    ForceUpdateAll();
                }
                else if (tmpInputField != null)
                {
                    tmpInputField.inputType = (TMP_InputField.InputType)((int)value);
                    ForceUpdateAll();
                }
            }
        }

        public UnityEngine.UI.InputField.LineType lineType
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.lineType : (tmpInputField != null ? (InputField.LineType)((int)tmpInputField.lineType) : InputField.LineType.SingleLine);
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                {
                    unityInputField.lineType = value;
                    ForceUpdateAll();
                }
                else if (tmpInputField != null)
                {
                    tmpInputField.lineType = (TMP_InputField.LineType)((int)value);
                    ForceUpdateAll();
                }
            }
        }

        public TouchScreenKeyboardType keyboardType
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.keyboardType : (tmpInputField != null ? tmpInputField.keyboardType : TouchScreenKeyboardType.Default);
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                {
                    unityInputField.keyboardType = value;
                    ForceUpdateAll();
                }
                else if (tmpInputField != null)
                {
                    tmpInputField.keyboardType = value;
                    ForceUpdateAll();
                }
            }
        }

        [SerializeStyleProperty]
        public Color selectionColor
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.selectionColor : (tmpInputField != null ? tmpInputField.selectionColor : Color.clear);
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                    unityInputField.selectionColor = value;
                else if (tmpInputField != null)
                    tmpInputField.selectionColor = value;

#if UNITY_EDITOR
                if (_InputField != null)
                    UnityEditor.EditorUtility.SetDirty(_InputField);
#endif
            }
        }

        [SerializeStyleProperty]
        public int fontSize
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null && unityInputField.textComponent != null ? unityInputField.textComponent.fontSize : (tmpInputField != null ? (int)tmpInputField.pointSize : (m_InputText != null ? (int)m_InputText.GetGraphicFontSize() : 0));
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null && unityInputField.textComponent != null)
                    unityInputField.textComponent.fontSize = value;
                else if (tmpInputField != null)
                    tmpInputField.pointSize = value;
                else if (m_InputText != null)
                    m_InputText.SetGraphicFontSize(value);
            }
        }

        [SerializeStyleProperty]
        public UnityEngine.Object fontAsset
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null && unityInputField.textComponent != null ? (UnityEngine.Object)unityInputField.textComponent.font : (tmpInputField != null ? tmpInputField.fontAsset :
                    (m_InputText is TMP_Text ? (m_InputText as TMP_Text).font : (m_InputText is Text ? (UnityEngine.Object)(m_InputText as Text).font : null)));
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null && unityInputField.textComponent != null && (value is Font || value == (UnityEngine.Object)null))
                    unityInputField.textComponent.font = value as Font;
                else if (tmpInputField != null && (value is TMP_FontAsset || value == (UnityEngine.Object)null))
                    tmpInputField.fontAsset = value as TMP_FontAsset;
                else if (m_InputText is TMP_Text)
                    (m_InputText as TMP_Text).font = value as TMP_FontAsset;
                else if (m_InputText is Text)
                    (m_InputText as Text).font = value as Font;
            }
        }

        [SerializeStyleProperty]
        public char asteriskChar
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.asteriskChar : (tmpInputField != null ? tmpInputField.asteriskChar : '•');
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                    unityInputField.asteriskChar = value;
                else if (tmpInputField != null)
                    tmpInputField.asteriskChar = value;
            }
        }

        public bool shouldHideMobileInput
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.shouldHideMobileInput : (tmpInputField != null ? tmpInputField.shouldHideMobileInput : true);
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                    unityInputField.shouldHideMobileInput = value;
                else if (tmpInputField != null)
                    tmpInputField.shouldHideMobileInput = value;
            }
        }

        public bool multiLine
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.multiLine : (tmpInputField != null ? tmpInputField.multiLine : false);
            }
        }

        public string text
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                System.Func<string> getGraphicText = () =>
                {
                    var graphicText = m_InputText != null ? m_InputText.GetGraphicText() : "";
                    if (!string.IsNullOrEmpty(graphicText))
                        graphicText = graphicText.EndsWith("\u200B") ? graphicText.Substring(0, graphicText.Length - 1) : graphicText;
                    else
                        graphicText = "";

                    return graphicText;
                };
                return unityInputField != null ? unityInputField.text : (tmpInputField != null ? tmpInputField.text : getGraphicText());
            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                var changed = false;
                if (unityInputField != null)
                {
                    changed = unityInputField.text != value;
                    unityInputField.text = value;
                }
                else if (tmpInputField != null)
                {
                    changed = tmpInputField.text != value;
                    tmpInputField.text = value;
                }
                else if (m_InputText != null)
                {
                    changed = m_InputText.GetGraphicText() != value;
                    m_InputText.SetGraphicText(value);
                }

                if (_PromptDisplayer != null)
                {
                    if (_PromptDisplayer.enabled && _PromptDisplayer.gameObject.activeInHierarchy)
                        _PromptDisplayer.SetTextToPromptDialog(value);

                    if (changed)
                        _PromptDisplayer.SendOnValueChangedAndUpdateLabel();
                }
            }
        }

        public bool isFocused
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.isFocused : (tmpInputField != null ? tmpInputField.isFocused : (promptDisplayer != null ? promptDisplayer.isFocused : false));
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

        public string hintText
        {
            get { return hintTextObject != null ? hintTextObject.GetGraphicText() : ""; }
            set
            {
                if (hintTextObject != null && !string.Equals(hintTextObject.GetGraphicText(), value))
                {
                    hintTextObject.SetGraphicText(value);

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                }
            }
        }

        public bool floatingHint
        {
            get { return m_FloatingHint; }
            set
            {
                m_FloatingHint = value;
                SetLayoutDirty();
            }
        }

        public bool hasValidation
        {
            get { return m_HasValidation; }
            set
            {
                m_HasValidation = value;
                SetLayoutDirty();
                ValidateText();
            }
        }

        public bool validateOnStart
        {
            get { return m_ValidateOnStart; }
            set
            {
                m_ValidateOnStart = value;
                if (value)
                {
                    ValidateText();
                }
            }
        }

        public bool hasCharacterCounter
        {
            get { return m_HasCharacterCounter; }
            set
            {
                m_HasCharacterCounter = value;
                m_CounterText.gameObject.SetActive(m_HasCharacterCounter);
                SetLayoutDirty();
                UpdateCounter();
            }
        }

        public bool matchInputFieldCharacterLimit
        {
            get { return m_MatchInputFieldCharacterLimit; }
            set
            {
                m_MatchInputFieldCharacterLimit = value;
                SetLayoutDirty();
                UpdateCounter();
            }
        }

        public int characterLimit
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                var limit = m_MatchInputFieldCharacterLimit ?
                    (unityInputField != null ? unityInputField.characterLimit :
                    (tmpInputField != null ? tmpInputField.characterLimit : m_CharacterLimit)) :
                    m_CharacterLimit;

                if (limit != m_CharacterLimit)
                {
                    m_CharacterLimit = limit;
                    SetLayoutDirty();
                    UpdateCounter();
                }

                return m_CharacterLimit;

            }
            set
            {
                if (m_CharacterLimit == value)
                    return;

                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                m_CharacterLimit = value;
                if (m_MatchInputFieldCharacterLimit)
                {
                    if (unityInputField != null)
                        unityInputField.characterLimit = m_CharacterLimit;
                    else if (tmpInputField != null)
                        tmpInputField.characterLimit = m_CharacterLimit;
                }

                SetLayoutDirty();
                UpdateCounter();
            }
        }

        public int floatingHintFontSize
        {
            get { return m_FloatingHintFontSize; }
            set
            {
                m_FloatingHintFontSize = value;
                SetLayoutDirty();
            }
        }

        public bool fitWidthToContent
        {
            get { return m_FitWidthToContent; }
            set
            {
                m_FitWidthToContent = value;
                SetLayoutDirty();
            }
        }

        public bool fitHeightToContent
        {
            get { return m_FitHeightToContent; }
            set
            {
                m_FitHeightToContent = value;
                SetLayoutDirty();
            }
        }

        public bool manualPreferredWidth
        {
            get { return m_ManualPreferredWidth; }
            set
            {
                m_ManualPreferredWidth = value;
                SetLayoutDirty();
            }
        }

        public bool manualPreferredHeight
        {
            get { return m_ManualPreferredHeight; }
            set
            {
                m_ManualPreferredHeight = value;
                SetLayoutDirty();
            }
        }

        public Vector2 manualSize
        {
            get { return m_ManualSize; }
            set
            {
                m_ManualSize = value;
                SetLayoutDirty();
            }
        }

        public GameObject textValidator
        {
            get { return m_TextValidator; }
            set
            {
                m_TextValidator = value;
                ValidateText();
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    m_RectTransform = transform as RectTransform;
                    if (m_RectTransform == null)
                        m_RectTransform = gameObject.AddComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }

        public int caretPosition
        {
            get
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                return unityInputField != null ? unityInputField.caretPosition : (tmpInputField != null ? tmpInputField.caretPosition : 0);

            }
            set
            {
                var unityInputField = inputField as InputField;
                var tmpInputField = _InputField as TMP_InputField;

                if (unityInputField != null)
                    unityInputField.caretPosition = value;
                else if (tmpInputField != null)
                {
                    tmpInputField.caretPosition = value;
                    tmpInputField.stringPosition = value;
                }
            }
        }

        public Selectable inputField
        {
            get
            {
                if (_InputField == null)
                {
                    _InputField = GetComponent<Selectable>();
                    if (_InputField != null)
                        RegisterEvents();
                }
                return _InputField;
            }
        }

        public InputPromptDisplayer promptDisplayer
        {
            get
            {
                if (_PromptDisplayer == null)
                {
                    _PromptDisplayer = GetComponent<InputPromptDisplayer>();
                    if (_PromptDisplayer != null)
                        RegisterEvents();
                }
                return _PromptDisplayer;
            }
        }

        public RectTransform inputTextTransform
        {
            get
            {
                if (m_InputTextTransform == null)
                {
                    if (inputField != null)
                    {
                        var unityInputField = _InputField as InputField;
                        var tmpInputField = _InputField as TMP_InputField;

                        if (unityInputField != null && unityInputField.textComponent != null)
                        {
                            m_InputTextTransform = unityInputField.textComponent.GetComponent<RectTransform>();
                        }
                        else if (tmpInputField != null && tmpInputField.textComponent != null)
                        {
                            m_InputTextTransform = tmpInputField.textComponent.GetComponent<RectTransform>();
                        }
                    }
                    else if (m_InputText != null)
                        m_InputTextTransform = m_InputText.GetComponent<RectTransform>();

                }

                return m_InputTextTransform;
            }
        }

        public RectTransform hintTextTransform
        {
            get { return m_HintTextTransform; }
            set
            {
                m_HintTextTransform = value;
                SetLayoutDirty();
                UpdateCounter();
                ValidateText();
                RefreshVisualStyles();
            }
        }

        public RectTransform counterTextTransform
        {
            get { return m_CounterTextTransform; }
            set
            {
                m_CounterTextTransform = value;
                SetLayoutDirty();
                UpdateCounter();
                ValidateText();
                RefreshVisualStyles();
            }
        }

        public RectTransform validationTextTransform
        {
            get { return m_ValidationTextTransform; }
            set
            {
                m_ValidationTextTransform = value;
                SetLayoutDirty();
                UpdateCounter();
                ValidateText();
                RefreshVisualStyles();
            }
        }

        public RectTransform lineTransform
        {
            get { return m_LineTransform; }
            set
            {
                m_LineTransform = value;
                SetLayoutDirty();
                RefreshVisualStyles();
            }
        }

        public RectTransform activeLineTransform
        {
            get { return m_ActiveLineTransform; }
            set
            {
                m_ActiveLineTransform = value;
                SetLayoutDirty();
                RefreshVisualStyles();
            }
        }

        public RectTransform upperContentTransform
        {
            get { return m_TopContentTransform; }
            set
            {
                m_TopContentTransform = value;
                SetLayoutDirty();
                RefreshVisualStyles();
            }
        }

        public RectTransform bottomContentTransform
        {
            get { return m_BottomContentTransform; }
            set
            {
                m_BottomContentTransform = value;
                SetLayoutDirty();
                RefreshVisualStyles();
            }
        }


        public RectTransform leftContentTransform
        {
            get { return m_LeftContentTransform; }
            set
            {
                m_LeftContentTransform = value;
                SetLayoutDirty();
                RefreshVisualStyles();
            }
        }

        public RectTransform rightContentTransform
        {
            get { return m_RightContentTransform; }
            set
            {
                m_RightContentTransform = value;
                SetLayoutDirty();
                RefreshVisualStyles();
            }
        }

        public RectTransform clearButton
        {
            get { return m_ClearButton; }
            set
            {
                m_ClearButton = value;
                SetLayoutDirty();
                RefreshVisualStyles();

                if (m_ClearButton != null && m_ClearButtonDisplayMode == ClearButtonDisplayModeEnum.Auto)
                    SetClearButtonActive(!string.IsNullOrEmpty(text));
            }
        }

        public Graphic inputText
        {
            get
            {
                if (m_InputText == null)
                {
                    if (inputTextTransform != null)
                    {
                        m_InputText = inputTextTransform.GetComponentInChildren<Graphic>();
                        if (inputField != null)
                        {
                            var tmpInputField = _InputField as TMP_InputField;
                            if (tmpInputField != null && tmpInputField.textComponent != m_InputText)
                                tmpInputField.textComponent = m_InputText as TMP_Text;

                            var unityInputField = _InputField as InputField;
                            if (unityInputField != null && unityInputField.textComponent != m_InputText)
                                unityInputField.textComponent = m_InputText as Text;
                        }
                    }
                }
                return m_InputText;
            }
        }

        [SerializeStyleProperty]
        public Color inputTextColor
        {
            get
            {
                return inputText != null ? inputText.color : Color.black;
            }
            set
            {
                if (inputText != null)
                    inputText.color = value;
            }
        }

        public Graphic hintTextObject
        {
            get
            {
                if (m_HintTextObject == null)
                {
                    if (m_HintTextTransform != null)
                    {
                        m_HintTextObject = m_HintTextTransform.GetComponent<Graphic>();
                    }
                }
                return m_HintTextObject;
            }
        }

        public Graphic counterText
        {
            get
            {
                if (m_CounterText == null)
                {
                    if (m_CounterTextTransform != null)
                    {
                        m_CounterText = m_CounterTextTransform.GetComponent<Graphic>();
                    }
                }
                return m_CounterText;
            }
        }

        public Graphic validationText
        {
            get
            {
                if (m_ValidationText == null)
                {
                    if (m_ValidationTextTransform != null)
                    {
                        m_ValidationText = m_ValidationTextTransform.GetComponent<Graphic>();
                    }
                }
                return m_ValidationText;
            }
        }

        public Graphic backgroundGraphic
        {
            get
            {
                return m_BackgroundGraphic;
            }
        }

        public Graphic outlineGraphic
        {
            get
            {
                return m_OutlineGraphic;
            }
        }

        /*public Image lineImage
        {
            get
            {
                if (m_LineImage == null)
                {
                    if (m_LineTransform != null)
                    {
                        m_LineImage = m_LineTransform.GetComponent<Image>();
                    }
                }
                return m_LineImage;
            }
        }*/

        public CanvasGroup activeLineCanvasGroup
        {
            get
            {
                if (_ActiveLineCanvasGroup == null)
                {
                    if (m_ActiveLineTransform != null)
                    {
                        _ActiveLineCanvasGroup = m_ActiveLineTransform.GetComponent<CanvasGroup>();
                    }
                }
                return _ActiveLineCanvasGroup;
            }
        }

        public CanvasGroup hintTextCanvasGroup
        {
            get
            {
                if (_HintTextCanvasGroup == null)
                {
                    if (m_HintTextTransform != null)
                    {
                        _HintTextCanvasGroup = m_HintTextTransform.GetComponent<CanvasGroup>();
                    }
                }
                return _HintTextCanvasGroup;
            }
        }

        public CanvasGroup validationCanvasGroup
        {
            get
            {
                if (_ValidationCanvasGroup == null)
                {
                    if (m_ValidationTextTransform != null)
                    {
                        _ValidationCanvasGroup = m_ValidationTextTransform.GetComponent<CanvasGroup>();
                    }
                }
                return _ValidationCanvasGroup;
            }
        }

        public RectTransform caretTransform
        {
            get
            {
                if (_CaretTransform == null)
                {
                    LayoutElement[] elements = GetComponentsInChildren<LayoutElement>();

                    for (int i = 0; i < elements.Length; i++)
                    {
                        if (elements[i].name == name + " Input Caret")
                        {
                            _CaretTransform = (RectTransform)elements[i].transform;
                        }
                    }
                }
                return _CaretTransform;
            }
        }

        public Color leftContentActiveColor
        {
            get { return m_LeftContentActiveColor; }
            set { m_LeftContentActiveColor = value; }
        }

        public Color leftContentInactiveColor
        {
            get { return m_LeftContentInactiveColor; }
            set { m_LeftContentInactiveColor = value; }
        }

        public Color rightContentActiveColor
        {
            get { return m_RightContentActiveColor; }
            set { m_RightContentActiveColor = value; }
        }

        public Color rightContentInactiveColor
        {
            get { return m_RightContentInactiveColor; }
            set { m_RightContentInactiveColor = value; }
        }

        public Color hintTextActiveColor
        {
            get { return m_HintTextActiveColor; }
            set { m_HintTextActiveColor = value; }
        }

        public Color hintTextInactiveColor
        {
            get { return m_HintTextInactiveColor; }
            set { m_HintTextInactiveColor = value; }
        }

        public Color lineActiveColor
        {
            get { return m_LineActiveColor; }
            set { m_LineActiveColor = value; }
        }

        public Color lineInactiveColor
        {
            get { return m_LineInactiveColor; }
            set { m_LineInactiveColor = value; }
        }

        public Color backgroundActiveColor
        {
            get { return m_BackgroundActiveColor; }
            set { m_BackgroundActiveColor = value; }
        }

        public Color backgroundInactiveColor
        {
            get { return m_BackgroundInactiveColor; }
            set { m_BackgroundInactiveColor = value; }
        }

        public Color outlineActiveColor
        {
            get { return m_OutlineActiveColor; }
            set { m_OutlineActiveColor = value; }
        }

        public Color outlineInactiveColor
        {
            get { return m_OutlineInactiveColor; }
            set { m_OutlineInactiveColor = value; }
        }

        public Color validationActiveColor
        {
            get { return m_ValidationActiveColor; }
            set { m_ValidationActiveColor = value; }
        }

        public Color validationInactiveColor
        {
            get { return m_ValidationInactiveColor; }
            set { m_ValidationInactiveColor = value; }
        }

        public Color counterActiveColor
        {
            get { return m_CounterActiveColor; }
            set { m_CounterActiveColor = value; }
        }

        public Color counterInactiveColor
        {
            get { return m_CounterInactiveColor; }
            set { m_CounterInactiveColor = value; }
        }

        public Graphic leftContentGraphic
        {
            get { return m_LeftContentGraphic; }
            set { m_LeftContentGraphic = value; }
        }

        public Graphic rightContentGraphic
        {
            get { return m_RightContentGraphic; }
            set { m_RightContentGraphic = value; }
        }

        public float hintTextFloatingValue
        {
            get { return m_HintTextFloatingValue; }
            set { m_HintTextFloatingValue = value; }
        }

        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                ApplyCanvasGroupChanged();
                RefreshVisualStyles();
                if (inputField != null)
                    _InputField.interactable = value;
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (!_CanvasGroup)
                {
                    _CanvasGroup = gameObject.GetComponent<CanvasGroup>();
                }
                return _CanvasGroup;
            }
        }

        /*private static Sprite lineDisabledSprite
        {
            get
            {
                if (m_LineDisabledSprite == null)
                {
                    Color[] colors =
                    {
                        Color.white,
                        Color.white,
                        Color.clear,
                        Color.clear,
                        Color.white,
                        Color.white,
                        Color.clear,
                        Color.clear
                    };

                    Texture2D texture = new Texture2D(4, 2, TextureFormat.ARGB32, false);
                    texture.filterMode = FilterMode.Point;
                    texture.SetPixels(colors);
                    texture.hideFlags = HideFlags.HideAndDontSave;

                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 4, 2), new Vector2(0.5f, 0.5f));
                    sprite.hideFlags = HideFlags.HideAndDontSave;

                    m_LineDisabledSprite = sprite;
                }

                return m_LineDisabledSprite;
            }
        }*/

        public ITextValidator customTextValidator
        {
            get { return _CustomTextValidator; }
            set
            {
                if (_CustomTextValidator != null)
                    _CustomTextValidator.Dispose();

                _CustomTextValidator = value;

                if (_CustomTextValidator != null)
                {
                    _CustomTextValidator.Init(this);
                }
            }
        }

        public RectOffset padding
        {
            get
            {
                return m_Padding;
            }
            set
            {
                if (m_Padding == value)
                    return;
                m_Padding = value;
                SetLayoutDirty();
            }
        }

        public ClearButtonDisplayModeEnum clearButtonDisplayMode
        {
            get
            {
                return m_ClearButtonDisplayMode;
            }
            set
            {
                if (m_ClearButtonDisplayMode == value)
                    return;
                m_ClearButtonDisplayMode = value;

                if (m_ClearButtonDisplayMode == ClearButtonDisplayModeEnum.Auto)
                    SetClearButtonActive(!string.IsNullOrEmpty(text));
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            if (inputField != null || inputText != null)
                RegisterEvents();
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (started)
            {
                OnTextChangedInternal(text);
                //CheckHintText();
                SetLayoutDirty();
            }
        }

        protected bool started = false;
        protected override void Start()
        {
            started = true;
            base.Start();
            RefreshVisualStyles();
            OnTextChangedInternal(text);
            SetLayoutDirty();
            CheckInputPromptDisplayModeVisibility();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetLayoutDirty();
            ForceDisableDisplayer(false);
        }

        protected override void OnDestroy()
        {
            UnregisterEvents();
            base.OnDestroy();
            ForceDisableDisplayer(true);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetLayoutDirty();
            if (_HasBeenSelected && isFocused)
                AnimateActiveLineSelect(true);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            RefreshVisualStyles();
            OnTextChangedInternal(text);
            SetLayoutDirty();
        }

        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();
            SetLayoutDirty();
            ApplyCanvasGroupChanged();
        }

#if UNITY_EDITOR
        protected override void OnValidateDelayed()
        {
            base.OnValidateDelayed();
            CheckInputPromptDisplayModeVisibility();
            if (!Application.isPlaying)
            {
                OnTextChangedInternal(text);
                SetLayoutDirty();
            }

            if (inputField != null && canvasGroup != null)
            {
                //canvasGroup.alpha = inputField == null || inputField.interactable ? 1 : 0.5f;
                canvasGroup.interactable = inputField.interactable;
                canvasGroup.blocksRaycasts = inputField.interactable;
            }
        }
#endif

        protected virtual void Update()
        {
            if (Application.isPlaying)
            {
                CheckInputPromptDisplayModeVisibility();
                var changed = _LastFocussedState != isFocused;
                if (isFocused)
                {
                    if (changed)
                        OnSelect(new PointerEventData(EventSystem.current));
                }
                else
                {
                    if (changed)
                        OnDeselect(new PointerEventData(EventSystem.current));
                }

                //if (changed)
                //    RefreshVisualStyles();
                //CheckHintText();

                if (_AnimateHintText)
                {
                    SetHintLayoutToFloatingValue();
                }
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            var callEvent = !_HasBeenSelected;

            _HasBeenSelected = true;
            var canSelect = (_InputField == null || isFocused);

            //We can only activate inputfield if not raised by pointerdown event as we want to only activate inputfield in OnPointerClick
            if (canSelect)
            {
                _LastFocussedState = true;
                AnimateActiveLineSelect();
                AnimateHintTextSelect();
                RefreshVisualStyles();

                ValidateText();

                SnapTo();

                if (onActivate != null)
                    onActivate.Invoke();
            }
            else if (callEvent)
            {
                if (onActivate != null)
                    onActivate.Invoke();
            }
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            _HasBeenSelected = false;
            _LastFocussedState = false;
            AnimateActiveLineDeselect();
            AnimateHintTextDeselect();
            ValidateText();
            RefreshVisualStyles();

            if (onDeactivate != null)
                onDeactivate.Invoke();
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnTextChanged(string value)
        {
            OnTextChangedInternal(value, false);
        }

        protected virtual void HandleOnGraphicTextChanged()
        {
            //Prevent Execute Immediate. If Executed immediate the bug will make prefab mode never save.
            ApplicationContext.RunOnMainThread(() =>
            {
                if (this == null)
                    return;
                OnTextChangedInternal(text, true);
            });
        }

        protected virtual void HandleOnGraphicEndEdit()
        {
            //Prevent Execute Immediate. If Executed immediate the bug will make prefab mode never save.
            ApplicationContext.RunOnMainThread(() =>
            {
                if (this == null)
                    return;
                HandleOnEndEdit(text);
            });
        }

        protected virtual void HandleOnEndEdit(string value)
        {
            ValidateText();
            if (onEndEdit != null)
                onEndEdit.Invoke(value);
        }

        protected virtual void HandleOnReturnPressed()
        {
            if (onReturnPressed != null)
                onReturnPressed.Invoke();
        }

        protected virtual void OnTextChangedInternal(string value)
        {
            var unityInputField = _InputField as InputField;
            var tmpInputField = _InputField as TMP_InputField;

            OnTextChangedInternal(value, unityInputField == null && tmpInputField == null && m_InputText != null);
        }

        protected virtual void OnTextChangedInternal(string value, bool canFixText)
        {
            if (onValueChanged != null)
                onValueChanged.Invoke(value);

            //Prevent Empty Text (fix Layout Bug)
            if (canFixText && m_InputText != null && string.IsNullOrEmpty(value))
            {
                UnregisterEvents();
                m_InputText.SetGraphicText("\u200B");
                RegisterEvents();
            }

            SetLayoutDirty();
            UpdateCounter();
            ValidateText();
            if (!m_FloatingHint)
                SetHintLayoutToFloatingValue();

            if (m_ClearButtonDisplayMode == ClearButtonDisplayModeEnum.Auto)
                SetClearButtonActive(!string.IsNullOrEmpty(value));
        }

        #endregion

        #region Other Functions

        public virtual bool SupportCustomPrompt()
        {
            var supportPrompt = false;
            if (inputField != null)
            {
                if (m_InputPromptDisplayOption == InputPromptDisplayMode.Always)
                    supportPrompt = true;
                else if (m_InputPromptDisplayOption == InputPromptDisplayMode.WhenCanShowMobileInput)
                    supportPrompt = !shouldHideMobileInput;
                else if (m_InputPromptDisplayOption == InputPromptDisplayMode.WhenSupportTouchScreen)
                    supportPrompt = TouchScreenKeyboard.isSupported;
            }
            return supportPrompt;
        }

        protected virtual void CheckInputPromptDisplayModeVisibility(bool force = false)
        {
            if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
            {
                var supportPrompt = SupportCustomPrompt();
                if (force || _CachedSupportPromptStatus == null || _CachedSupportPromptStatus.Value != supportPrompt)
                {
                    var editNativeInputEnable = false;
                    if (supportPrompt && promptDisplayer == null)
                    {
                        editNativeInputEnable = true;
                        _PromptDisplayer = gameObject.AddComponent<InputPromptDisplayer>();
                        _PromptDisplayer.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInInspector;
                    }
                    else if (promptDisplayer != null)
                    {
                        if (!supportPrompt)
                        {
                            editNativeInputEnable = true;
                            ForceDisableDisplayer(true);
                        }
                        else if (_PromptDisplayer.enabled != supportPrompt)
                        {
                            editNativeInputEnable = true;
                            _PromptDisplayer.enabled = supportPrompt;
                        }
                    }

                    //Apply last support prompt result to prevent enter in this checking again
                    _CachedSupportPromptStatus = supportPrompt;

                    //Special case when we must change inputField Visibility
                    if (inputField != null && editNativeInputEnable)
                    {
                        editNativeInputEnable = false;
                        if (!isFocused)
                        {
                            inputField.enabled = !supportPrompt;
                        }
                        // If focused we must wait to leave focus to change status, to do it we must clear SupportPromptStatus
                        else
                        {
                            _CachedSupportPromptStatus = null;
                        }
                    }
                }
            }
        }

        protected virtual void ForceDisableDisplayer(bool canDestroy)
        {
            if (promptDisplayer != null)
            {
                _CachedSupportPromptStatus = null;
                _PromptDisplayer.enabled = false;
                if (canDestroy && _PromptDisplayer.hideFlags != HideFlags.None)
                    _PromptDisplayer.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInInspector;
            }
        }

        public void SetTextWithoutNotify(string text)
        {
            text = string.IsNullOrEmpty(text) ? string.Empty : text;
            var unityInputField = _InputField as InputField;
            var tmpInputField = _InputField as TMP_InputField;

            if (unityInputField != null)
                unityInputField.SetTextWithoutNotify(text);
            else if (tmpInputField != null)
                tmpInputField.SetTextWithoutNotify(text);
        }

        public virtual void ForceUpdateAll()
        {
            SetLayoutDirty();
            ForceLabelUpdate();
            if (inputField != null)
            {
                var method = inputField.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).FirstOrDefault(m =>
                            m.Name == "RecreateKeyboard" &&
                            m.GetParameters().Length == 0);

                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters == null || parameters.Length == 0)
                        method.Invoke(inputField, null);
                }
            }
        }

        public virtual void ForceLabelUpdate()
        {
            var unityInputField = inputField as InputField;
            var tmpInputField = _InputField as TMP_InputField;

            if (unityInputField != null)
                unityInputField.ForceLabelUpdate();
            else if (tmpInputField != null)
                tmpInputField.ForceLabelUpdate();
        }

        public override void SnapTo()
        {
#if UI_COMMONS_DEFINED
            if (enabled && gameObject.activeInHierarchy)
            {
                var nestedRect = GetComponentInParent<Kyub.UI.NestedScrollRect>();
                if (nestedRect != null)
                {
                    var keyboardSupported = TouchScreenKeyboard.isSupported;
                    if (keyboardSupported)
                        nestedRect.SnapToImmediate(this.transform as RectTransform);
                    else
                        nestedRect.SnapTo(this.transform as RectTransform);
                }
            }
#endif
        }

        public virtual void ActivateInputField()
        {
            Select();

            if (promptDisplayer != null && promptDisplayer.enabled && !m_OpenPromptOnSelect)
            {
                promptDisplayer.ActivateInputField();
            }
        }

        public virtual void DeactivateInputField()
        {
            if (inputField != null)
            {
                var method = inputField.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).FirstOrDefault(m =>
                            m.Name == "DeactivateInputField" &&
                            m.GetParameters().Length == 0);
                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters == null || parameters.Length == 0)
                        method.Invoke(inputField, null);
                    else if (inputField is TMP_InputField)
                        (inputField as TMP_InputField).DeactivateInputField();
                }
            }

            if (promptDisplayer != null && promptDisplayer.enabled)
            {
                promptDisplayer.DeactivateInputField();
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();

            var unityInputField = _InputField as InputField;
            var tmpInputField = _InputField as TMP_InputField;

            if (unityInputField != null)
                unityInputField.onValueChanged.AddListener(HandleOnTextChanged);
            else if (tmpInputField != null)
                tmpInputField.onValueChanged.AddListener(HandleOnTextChanged);
            else if (m_InputText != null)
                m_InputText.RegisterDirtyVerticesCallback(HandleOnGraphicTextChanged);

            if (unityInputField != null)
                unityInputField.onEndEdit.AddListener(HandleOnEndEdit);
            else if (tmpInputField != null)
                tmpInputField.onEndEdit.AddListener(HandleOnEndEdit);
            else if (m_InputText != null)
                m_InputText.RegisterDirtyVerticesCallback(HandleOnGraphicEndEdit);

            if (_InputField != null)
            {
                var onReturnPressedField = _InputField.GetType().GetField("OnReturnPressed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (onReturnPressedField != null)
                {
                    var onReturnPressedEvent = onReturnPressedField.GetValue(_InputField) as UnityEvent;
                    if (onReturnPressedEvent != null)
                        onReturnPressedEvent.AddListener(HandleOnReturnPressed);
                }
            }
        }

        protected virtual void UnregisterEvents()
        {
            var unityInputField = _InputField as InputField;
            var tmpInputField = _InputField as TMP_InputField;

            if (unityInputField != null)
                unityInputField.onValueChanged.RemoveListener(HandleOnTextChanged);
            else if (tmpInputField != null)
                tmpInputField.onValueChanged.RemoveListener(HandleOnTextChanged);
            else if (m_InputText != null)
                m_InputText.UnregisterDirtyVerticesCallback(HandleOnGraphicTextChanged);

            if (unityInputField != null)
                unityInputField.onEndEdit.RemoveListener(HandleOnEndEdit);
            else if (tmpInputField != null)
                tmpInputField.onEndEdit.RemoveListener(HandleOnEndEdit);
            else if (m_InputText != null)
                m_InputText.UnregisterDirtyVerticesCallback(HandleOnGraphicEndEdit);

            if (_InputField != null)
            {
                var onReturnPressedField = _InputField.GetType().GetField("OnReturnPressed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (onReturnPressedField != null)
                {
                    var onReturnPressedEvent = onReturnPressedField.GetValue(_InputField) as UnityEvent;
                    if (onReturnPressedEvent != null)
                        onReturnPressedEvent.RemoveListener(HandleOnReturnPressed);
                }
            }
        }

        public virtual void ClearText()
        {
            var unityInputField = _InputField as InputField;
            var tmpInputField = _InputField as TMP_InputField;

            bool requireCallback = true;
            if (unityInputField != null)
                unityInputField.text = "";
            else if (tmpInputField != null)
                tmpInputField.text = "";
            else if (m_InputText != null)
            {
                requireCallback = false;
                m_InputText.SetGraphicText("");
            }

            if (requireCallback)
                OnTextChangedInternal("", false);
            SetLayoutDirty();
        }

        public virtual void ValidateText()
        {
            ValidateText(false);
        }

        public virtual void ValidateText(bool force)
        {
            ITextValidator validator = m_TextValidator != null ? m_TextValidator.GetComponent<ITextValidator>() : null;

            if (_CustomTextValidator != null)
            {
                if (validator != null && validator != customTextValidator)
                    validator.Dispose();
                validator = customTextValidator;
            }

            if (validator != null && !m_HasValidation)
                validator.Dispose();

            //if (validationText == null) return;

            if (!force && !m_ValidateOnStart && !_HasBeenSelected)
                return;

            if (validationText != null)
                m_ValidationText.color = IsSelected() ? m_ValidationActiveColor : m_ValidationInactiveColor;

            if (m_TextValidator != null && validator != null)
            {
                validator.Init(this);
            }

            cachedIsTextValid = validator != null && m_HasValidation && validator.IsTextValid();
            if (validationCanvasGroup != null)
            {
                if (m_HasValidation != validationCanvasGroup.gameObject.activeSelf)
                {
                    validationCanvasGroup.alpha = 0;
                    validationCanvasGroup.interactable = false;
                    validationCanvasGroup.blocksRaycasts = false;
                    validationCanvasGroup.gameObject.SetActive(m_HasValidation);
                }

                if (validator != null && m_HasValidation)
                {
                    TweenManager.EndTween(_ValidationTweener);

                    if (!_CachedIsTextValid)
                    {
                        if (Application.isPlaying)
                        {
                            validationCanvasGroup.interactable = true;
                            validationCanvasGroup.blocksRaycasts = true;

                            _ValidationTweener = TweenManager.TweenFloat(
                                f =>
                                {
                                    if (this != null && validationCanvasGroup != null)
                                        validationCanvasGroup.alpha = f;
                                },
                                validationCanvasGroup.alpha, 1f,
                                m_AnimationDuration / 2,
                                tweenType: Tween.TweenType.Linear);
                        }
                    }
                    else
                    {
                        if (Application.isPlaying)
                        {
                            _ValidationTweener = TweenManager.TweenFloat(
                                f =>
                                {
                                    if (this != null && validationCanvasGroup != null)
                                        validationCanvasGroup.alpha = f;
                                },
                                validationCanvasGroup.alpha, 0f, m_AnimationDuration / 2,
                                0,
                                () =>
                                {
                                    if (this != null && validationCanvasGroup != null)
                                    {
                                        validationCanvasGroup.interactable = false;
                                        validationCanvasGroup.blocksRaycasts = false;
                                    }
                                },
                                false,
                                Tween.TweenType.Linear);
                        }
                    }
                }
                else
                {
                    validationCanvasGroup.alpha = 0;
                    validationCanvasGroup.interactable = false;
                    validationCanvasGroup.blocksRaycasts = false;
                }
            }
            if (m_HasValidation && m_ValidationText != null && string.IsNullOrEmpty(m_ValidationText.GetGraphicText()))
                m_ValidationText.SetGraphicText("\u200B");

            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
        }

        private void UpdateCounter()
        {
            if (counterText == null)
            {
                return;
            }

            int limit = characterLimit;

            string outOf = limit > 0 ? " / " + limit : "";

            counterText.SetGraphicText(text.Length + outOf);
        }

        private void UpdateSelectionState()
        {
            if (m_CounterText != null)
                m_CounterText.gameObject.SetActive(m_HasCharacterCounter);
            if (m_ValidationText != null)
                m_ValidationText.gameObject.SetActive(m_HasValidation);

            if (IsInteractable())
            {
                _CurrentSelectionState = isFocused ? ColorSelectionState.EnabledSelected : ColorSelectionState.EnabledDeselected;

                /*if (lineImage != null)
                {
                    lineImage.sprite = null;
                }*/
            }
            else
            {
                _CurrentSelectionState = isFocused ? ColorSelectionState.DisabledSelected : ColorSelectionState.DisabledDeselected;

                /*if (lineImage != null)
                {
                    lineImage.sprite = lineDisabledSprite;
                    lineImage.type = Image.Type.Tiled;
                }*/
            }

            if (_CurrentSelectionState != _LastSelectionState)
            {
                _LastSelectionState = _CurrentSelectionState;

                TweenManager.EndTween(_BackgroundTweener);

                if (Application.isPlaying)
                {
                    if (m_BackgroundGraphic)
                    {
                        _BackgroundTweener = TweenManager.TweenColor(color => m_BackgroundGraphic.color = color,
                            m_BackgroundGraphic.color,
                            IsSelected() ? m_BackgroundActiveColor : m_BackgroundInactiveColor, m_AnimationDuration);
                    }
                }
                else
                {
                    if (m_BackgroundGraphic)
                    {
                        m_BackgroundGraphic.color = IsSelected()
                            ? m_BackgroundActiveColor
                            : m_BackgroundInactiveColor;
                    }
                }

                TweenManager.EndTween(_OutlineTweener);

                if (Application.isPlaying)
                {
                    if (m_OutlineGraphic)
                    {
                        _OutlineTweener = TweenManager.TweenColor(color => m_OutlineGraphic.color = color,
                            m_OutlineGraphic.color,
                            IsSelected() ? m_OutlineActiveColor : m_OutlineInactiveColor, m_AnimationDuration);
                    }
                }
                else
                {
                    if (m_OutlineGraphic)
                    {
                        m_OutlineGraphic.color = IsSelected()
                            ? m_OutlineActiveColor
                            : m_OutlineInactiveColor;
                    }
                }

                TweenManager.EndTween(_LeftContentTweener);

                if (Application.isPlaying)
                {
                    if (m_LeftContentGraphic)
                    {
                        _LeftContentTweener = TweenManager.TweenColor(color => m_LeftContentGraphic.color = color,
                            m_LeftContentGraphic.color,
                            IsSelected() ? m_LeftContentActiveColor : m_LeftContentInactiveColor, m_AnimationDuration);
                    }
                }
                else
                {
                    if (m_LeftContentGraphic)
                    {
                        m_LeftContentGraphic.color = IsSelected()
                            ? m_LeftContentActiveColor
                            : m_LeftContentInactiveColor;
                    }
                }

                TweenManager.EndTween(_RightContentTweener);

                if (Application.isPlaying)
                {
                    if (m_RightContentGraphic)
                    {
                        _RightContentTweener = TweenManager.TweenColor(color => m_RightContentGraphic.color = color,
                            m_RightContentGraphic.color,
                            IsSelected() ? m_RightContentActiveColor : m_RightContentInactiveColor, m_AnimationDuration);
                    }
                }
                else
                {
                    if (m_RightContentGraphic)
                    {
                        m_RightContentGraphic.color = IsSelected()
                            ? m_RightContentActiveColor
                            : m_RightContentInactiveColor;
                    }
                }

                TweenManager.EndTween(_HintTextTweener);

                if (m_HintTextObject != null)
                {
                    if (Application.isPlaying)
                    {
                        _HintTextTweener = TweenManager.TweenColor(color => m_HintTextObject.color = color,
                            m_HintTextObject.color, IsSelected() && m_FloatingHint ? m_HintTextActiveColor : m_HintTextInactiveColor,
                            m_AnimationDuration);
                    }
                    else
                    {
                        m_HintTextObject.color = IsSelected() && m_FloatingHint ? m_HintTextActiveColor : m_HintTextInactiveColor;
                    }
                }

                TweenManager.EndTween(_CounterTweener);

                if (m_CounterText != null)
                {
                    if (Application.isPlaying)
                    {
                        _CounterTweener = TweenManager.TweenColor(color => m_CounterText.color = color,
                            m_CounterText.color, IsSelected() ? m_CounterActiveColor : m_CounterInactiveColor,
                            m_AnimationDuration);
                    }
                    else
                    {
                        m_CounterText.color = IsSelected() ? m_CounterActiveColor : m_CounterInactiveColor;
                    }
                }
                TweenManager.EndTween(_ValidationColorTweener);

                if (m_ValidationText != null)
                {
                    if (Application.isPlaying)
                    {

                        _ValidationColorTweener = TweenManager.TweenColor(color => m_ValidationText.color = color,
                            m_ValidationText.color, IsSelected() ? m_ValidationActiveColor : m_ValidationInactiveColor,
                            m_AnimationDuration);
                    }
                    else
                    {
                        m_ValidationText.color = IsSelected() ? m_ValidationActiveColor : m_ValidationInactiveColor;
                    }
                }

                if (m_LineTransform != null)
                    m_LineTransform.GetComponent<Graphic>().color = m_LineInactiveColor;
                if (m_ActiveLineTransform)
                    m_ActiveLineTransform.GetComponent<Graphic>().color = m_LineActiveColor;

                //canvasGroup.alpha = m_Interactable ? 1 : 0.5f;
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = m_Interactable;
                    canvasGroup.blocksRaycasts = m_Interactable;
                }
            }
        }

        public bool IsTextValid()
        {
            return !m_HasValidation || _CachedIsTextValid;
        }

        public bool IsSelected()
        {
            return _CurrentSelectionState == ColorSelectionState.DisabledSelected ||
                   _CurrentSelectionState == ColorSelectionState.EnabledSelected;
        }

        private float GetTextPreferredHeight()
        {
            return m_InputText != null && !HasIgnoreLayout(m_InputText) ? LayoutUtility.GetPreferredHeight(m_InputText.rectTransform) : 0; // textGenerator.GetPreferredHeight(layoutText, textGenerationSettings) * fontScale;
        }

        private float GetTextPreferredWidth()
        {
            var hintWidth = m_HintTextObject != null && !HasIgnoreLayout(m_HintTextObject) ? LayoutUtility.GetPreferredWidth(m_HintTextObject.rectTransform) : 0;
            var textWidth = m_InputText != null && !HasIgnoreLayout(m_InputText) ? LayoutUtility.GetPreferredWidth(m_InputText.rectTransform) : 0;
            return Mathf.Max(textWidth, hintWidth);
        }

        private float GetSmallHintTextHeight()
        {
            if (hintTextObject == null || HasIgnoreLayout(hintTextObject))
            {
                return 0;
            }

            /*TextGenerator textGenerator = hintTextObject.GetGraphicTextGeneratorForLayout();
            TextGenerationSettings textGenerationSettings = inputText.GetGraphicGenerationSettings(new Vector2(float.MaxValue, float.MaxValue));

            float fontAssetSize = hintTextObject.GetGraphicFontAssetFontSize();
            float fontScale = fontAssetSize == 0 ? ((float)hintTextObject.GetGraphicFontSize() / fontAssetSize) : 1.0f; //hintTextObject.font.fontSize > 0 && !hintTextObject.font.dynamic? ((float)hintTextObject.fontSize / m_FloatingHintFontSize) : 1.0f;

            textGenerationSettings.scaleFactor = 1f;
            if(hintTextObject.font.dynamic)
                textGenerationSettings.fontSize = m_FloatingHintFontSize;*/

            var fontSize = hintTextObject.GetGraphicFontSize();
            ILayoutElement hintLayoutElement = hintTextObject as ILayoutElement;

            var isHintLayoutDirty = false;
            if (floatingHint && hintLayoutElement != null)
            {
                //Calculate preferredHeight of FloatingHintFontSize and revert to old fontsize value
                if (fontSize != floatingHintFontSize)
                {
                    isHintLayoutDirty = true;
                    hintTextObject.SetGraphicFontSize(floatingHintFontSize);
                    hintLayoutElement.CalculateLayoutInputHorizontal();
                    hintLayoutElement.CalculateLayoutInputVertical();

                }
            }

            var preferredHeight = hintTextObject != null ? LayoutUtility.GetPreferredHeight(hintTextObject.rectTransform) : 0; //textGenerator.GetPreferredHeight(hintTextObject.GetGraphicText(), textGenerationSettings) * fontScale;

            //Revert to original font size and preferredHeight
            if (isHintLayoutDirty)
            {
                hintTextObject.SetGraphicFontSize(fontSize);
                hintLayoutElement.CalculateLayoutInputHorizontal();
                hintLayoutElement.CalculateLayoutInputVertical();
            }

            return preferredHeight;
        }

        private void AnimateHintTextSelect()
        {
            CancelHintTextTweeners();

            var isInsideOutline = IsHintInsideOutline();
            _HintTextFloatingValueTweener = TweenManager.TweenFloat(f => hintTextFloatingValue = f, hintTextFloatingValue, 1f, m_AnimationDuration, 0, () =>
            {
                _AnimateHintText = false;
                SetHintLayoutToFloatingValue(isInsideOutline);
            });
            _AnimateHintText = true;
        }

        private void AnimateHintTextDeselect()
        {
            CancelHintTextTweeners();

            if (!isFocused && text.Length == 0)
            {
                _HintTextFloatingValueTweener = TweenManager.TweenFloat(f => hintTextFloatingValue = f,
                    () => hintTextFloatingValue, 0f, m_AnimationDuration, 0f, () =>
                    {
                        _AnimateHintText = false;
                        SetHintLayoutToFloatingValue();
                    });
                _AnimateHintText = true;
            }
        }

        private void AnimateActiveLineSelect(bool instant = false)
        {
            CancelActivelineTweeners();

            if (m_LineTransform == null || m_ActiveLineTransform == null) return;

            if (instant)
            {
                m_ActiveLineTransform.anchoredPosition = Vector2.zero;
                m_ActiveLineTransform.sizeDelta = new Vector2(m_LineTransform.GetProperSize().x, m_ActiveLineTransform.sizeDelta.y);
                activeLineCanvasGroup.alpha = 1;
            }
            else
            {
                float lineLength = m_LineTransform.GetProperSize().x;

                m_ActiveLineTransform.sizeDelta = new Vector2(0, m_ActiveLineTransform.sizeDelta.y);
                m_ActiveLineTransform.position = input.mousePosition;
                m_ActiveLineTransform.anchoredPosition = new Vector2(Mathf.Clamp(m_ActiveLineTransform.anchoredPosition.x, -lineLength / 2, lineLength / 2), 0);
                activeLineCanvasGroup.alpha = 1;

                _ActiveLinePosTweener = TweenManager.TweenFloat(f =>
                {
                    if (m_ActiveLineTransform != null)
                        m_ActiveLineTransform.anchoredPosition = new Vector2(f, m_ActiveLineTransform.anchoredPosition.y);
                }, m_ActiveLineTransform.anchoredPosition.x, 0f, m_AnimationDuration);

                _ActiveLineSizeTweener = TweenManager.TweenFloat(f =>
                {
                    if (m_ActiveLineTransform != null)
                        m_ActiveLineTransform.sizeDelta = new Vector2(f, m_ActiveLineTransform.sizeDelta.y);
                }, m_ActiveLineTransform.sizeDelta.x, m_LineTransform.GetProperSize().x, m_AnimationDuration);
            }
        }

        private void AnimateActiveLineDeselect(bool instant = false)
        {
            CancelActivelineTweeners();

            if (activeLineTransform == null) return;

            if (instant)
            {
                activeLineCanvasGroup.alpha = 0;
            }
            else
            {
                activeLineCanvasGroup.alpha = 1;

                _ActiveLineAlphaTweener = TweenManager.TweenFloat(f => activeLineCanvasGroup.alpha = f, activeLineCanvasGroup.alpha, 0f, m_AnimationDuration);
            }
        }

        private void CancelHintTextTweeners()
        {
            TweenManager.EndTween(_HintTextFloatingValueTweener);
            _AnimateHintText = false;
        }

        private void CancelActivelineTweeners()
        {
            TweenManager.EndTween(_ActiveLineSizeTweener);
            TweenManager.EndTween(_ActiveLinePosTweener);
            TweenManager.EndTween(_ActiveLineAlphaTweener);
        }

        protected virtual bool IsBottomContentInsideOutline()
        {
            var isInsideOutline = m_BackgroundLayoutMode != BackgroundLayoutMode.TextOnly && (m_BackgroundLayoutMode == BackgroundLayoutMode.TextAndLowerContent || m_BackgroundLayoutMode == BackgroundLayoutMode.All);
            if (isInsideOutline)
            {
                //check if background or outline is valid
                isInsideOutline = (m_OutlineGraphic != null && !HasIgnoreLayout(m_OutlineGraphic) && m_BackgroundGraphic.transform != this.transform && m_OutlineGraphic.gameObject.activeInHierarchy && m_OutlineGraphic.enabled) ||
                    (m_BackgroundGraphic != null && !HasIgnoreLayout(m_BackgroundGraphic) && m_BackgroundGraphic.transform != this && m_BackgroundGraphic.gameObject.activeInHierarchy && m_BackgroundGraphic.enabled);
            }
            return isInsideOutline;
        }

        protected virtual bool IsTopContentInsideOutline()
        {
            var isInsideOutline = m_BackgroundLayoutMode != BackgroundLayoutMode.TextOnly && (m_BackgroundLayoutMode == BackgroundLayoutMode.TextAndUpperContent || m_BackgroundLayoutMode == BackgroundLayoutMode.All);
            if (isInsideOutline)
            {
                //check if background or outline is valid
                isInsideOutline = (m_OutlineGraphic != null && !HasIgnoreLayout(m_OutlineGraphic) && m_BackgroundGraphic.transform != this.transform && m_OutlineGraphic.gameObject.activeInHierarchy && m_OutlineGraphic.enabled) ||
                    (m_BackgroundGraphic != null && !HasIgnoreLayout(m_BackgroundGraphic) && m_BackgroundGraphic.transform != this && m_BackgroundGraphic.gameObject.activeInHierarchy && m_BackgroundGraphic.enabled);
            }
            return isInsideOutline;
        }

        protected virtual bool IsHintInsideOutline()
        {
            var isInsideOutline = m_FloatingHint && IsTopContentInsideOutline();
            return isInsideOutline;
        }

        protected void SetHintLayoutToFloatingValue()
        {
            SetHintLayoutToFloatingValue(IsHintInsideOutline());
        }

        protected void SetClearButtonActive(bool isActive)
        {
            if (this == null || !Application.isPlaying || !this.gameObject.scene.IsValid())
                return;

            if (m_ClearButton != null && m_ClearButton.gameObject.activeSelf != isActive)
                m_ClearButton.gameObject.SetActive(isActive);
        }

        protected void SetHintLayoutToFloatingValue(bool isInsideOutline)
        {
            if (m_HintTextTransform == null || HasIgnoreLayout(m_HintTextTransform)) return;

            if (m_FloatingHint)
            {
                var hintTopPosition = (isInsideOutline ? m_Padding.top : 4) + _TopContentHeight;
                m_HintTextTransform.offsetMin = new Vector2(_LeftSectionWidth, _BottomSectionHeight);
                m_HintTextTransform.offsetMax = new Vector2(-_RightSectionWidth, -Tween.Linear(_TopSectionHeight, hintTopPosition, hintTextFloatingValue, 1));
                if (hintTextObject != null)
                    hintTextObject.SetGraphicFontSize(Mathf.RoundToInt(Tween.Linear(fontSize, m_FloatingHintFontSize, hintTextFloatingValue, 1)));

                float realFontSize = Tween.Linear(fontSize, m_FloatingHintFontSize, hintTextFloatingValue, 1);

                float hintFontSize = hintTextObject != null ? hintTextObject.GetGraphicFontSize() : 0;
                float scaleFactor = hintFontSize == 0 ? 0 : realFontSize / hintFontSize;

                m_HintTextTransform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                if (hintTextCanvasGroup != null)
                    hintTextCanvasGroup.alpha = 1;
            }
            else
            {
                //m_HintTextTransform.offsetMin = new Vector2(m_LeftSectionWidth, m_BottomSectionHeight);
                //m_HintTextTransform.offsetMax = new Vector2(-m_RightSectionWidth, -m_TopSectionHeight);
                if (hintTextObject != null)
                    hintTextObject.SetGraphicFontSize(fontSize);
                m_HintTextTransform.localScale = Vector3.one;

                if (inputTextTransform != null)
                {
                    m_HintTextTransform.pivot = inputTextTransform.pivot;
                    m_HintTextTransform.anchorMin = inputTextTransform.anchorMin;
                    m_HintTextTransform.anchorMax = inputTextTransform.anchorMax;
                    m_HintTextTransform.anchoredPosition = inputTextTransform.anchoredPosition;
                    m_HintTextTransform.sizeDelta = inputTextTransform.sizeDelta;
                }
                if (hintTextCanvasGroup != null)
                    hintTextCanvasGroup.alpha = (inputField != null || inputText != null) && text.Length > 0 ? 0 : 1;
            }
        }

        public void SetBackgroundAndOutlineLayout(bool isVertical)
        {
            if (m_BackgroundGraphic != null || m_OutlineGraphic != null)
            {
                Vector2 offsetMin = Vector2.zero;
                Vector2 offsetMax = Vector2.zero;
                if (m_BackgroundLayoutMode == BackgroundLayoutMode.TextOnly)
                {
                    offsetMin = new Vector2(0, (_BottomSectionHeight - m_Padding.bottom));
                    offsetMax = new Vector2(0, -(_TopSectionHeight - m_Padding.top));
                }
                else if (m_BackgroundLayoutMode == BackgroundLayoutMode.TextAndLowerContent)
                {
                    offsetMin = new Vector2(0, 0);
                    offsetMax = new Vector2(0, -(_TopSectionHeight - m_Padding.top));
                }
                else if (m_BackgroundLayoutMode == BackgroundLayoutMode.TextAndUpperContent)
                {
                    offsetMin = new Vector2(0, (_BottomSectionHeight - m_Padding.bottom));
                    offsetMax = new Vector2(0, 0);
                }
                else if (m_BackgroundLayoutMode == BackgroundLayoutMode.All)
                {
                    offsetMin = Vector2.zero;
                    offsetMax = Vector2.zero;
                }
                else
                {
                    return;
                }

                if (m_BackgroundGraphic != null && m_BackgroundGraphic.transform != this.transform && !HasIgnoreLayout(m_BackgroundGraphic))
                {
                    m_BackgroundGraphic.rectTransform.offsetMin = isVertical ? offsetMin : new Vector2(0, m_BackgroundGraphic.rectTransform.offsetMin.y);
                    m_BackgroundGraphic.rectTransform.offsetMax = isVertical ? offsetMax : new Vector2(0, m_BackgroundGraphic.rectTransform.offsetMax.y);
                }

                if (m_OutlineGraphic != null && m_OutlineGraphic.transform != this.transform && !HasIgnoreLayout(m_OutlineGraphic))
                {
                    m_OutlineGraphic.rectTransform.offsetMin = isVertical ? offsetMin : new Vector2(0, m_OutlineGraphic.rectTransform.offsetMin.y);
                    m_OutlineGraphic.rectTransform.offsetMax = isVertical ? offsetMax : new Vector2(0, m_OutlineGraphic.rectTransform.offsetMax.y);
                }
            }
        }

        public void SetCounterAndValidationLayout(bool isVertical)
        {
            if (isVertical)
            {
                if (m_ValidationTextTransform != null || m_CounterTextTransform != null)
                {
                    var counterIsInsideBG = IsBottomContentInsideOutline();
                    var defaultOffset = 0;

                    var positionY = (counterIsInsideBG ? m_Padding.bottom : defaultOffset) + _BottomContentHeight;
                    if (_BottomContentHeight > 0)
                        positionY += 4;

                    if (m_ValidationTextTransform != null && validationText != null && !HasIgnoreLayout(m_ValidationTextTransform))
                    {

                        m_ValidationTextTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, positionY, m_ValidationText != null ? LayoutUtility.GetPreferredHeight(m_ValidationText.rectTransform) : 0);
                    }

                    if (m_CounterTextTransform != null && counterText != null && !HasIgnoreLayout(m_CounterTextTransform))
                    {
                        m_CounterTextTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, positionY, m_CounterText != null ? LayoutUtility.GetPreferredHeight(m_CounterText.rectTransform) : 0);
                    }
                }
            }
            else
            {
                if (m_ValidationTextTransform != null && !HasIgnoreLayout(m_ValidationTextTransform))
                {
                    m_ValidationTextTransform.offsetMin = new Vector2(m_LineLayoutMode == LineLayoutMode.Default ? _LeftSectionWidth : m_Padding.left, m_ValidationTextTransform.offsetMin.y);
                    m_ValidationTextTransform.offsetMax = new Vector2(m_LineLayoutMode == LineLayoutMode.Default ? -_RightSectionWidth : -m_Padding.right, m_ValidationTextTransform.offsetMax.y);
                }

                if (m_CounterTextTransform != null && !HasIgnoreLayout(m_CounterTextTransform))
                {
                    m_CounterTextTransform.offsetMin = new Vector2(m_LineLayoutMode == LineLayoutMode.Default ? _LeftSectionWidth : m_Padding.left, m_CounterTextTransform.offsetMin.y);
                    m_CounterTextTransform.offsetMax = new Vector2(m_LineLayoutMode == LineLayoutMode.Default ? -_RightSectionWidth : -m_Padding.right, m_CounterTextTransform.offsetMax.y);
                }
            }
        }

        protected bool HasIgnoreLayout(Component component)
        {
            if (component != null)
            {
                var layout = component.GetComponent<LayoutElement>();
                return layout != null && layout.ignoreLayout;
            }
            return false;
        }

        #endregion

        #region Layout Functions

        public void SetLayoutDirty()
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public void SetLayoutHorizontal()
        {
            if (!TweenManager.TweenIsActive(_HintTextFloatingValueTweener))
            {
                hintTextFloatingValue = (isFocused || text.Length > 0) ? 1 : 0;
                SetHintLayoutToFloatingValue();
            }

            if (inputTextTransform != null && !HasIgnoreLayout(inputTextTransform))
            {
                inputTextTransform.offsetMin = new Vector2(_LeftSectionWidth, inputTextTransform.offsetMin.y);
                inputTextTransform.offsetMax = new Vector2(-_RightSectionWidth, inputTextTransform.offsetMax.y);
            }

            if (m_LineTransform != null && !HasIgnoreLayout(m_LineTransform))
            {
                m_LineTransform.offsetMin = new Vector2(m_LineLayoutMode == LineLayoutMode.Default ? _LeftSectionWidth : (m_LineLayoutMode == LineLayoutMode.IgnoreContent ? m_Padding.left : 0), m_LineTransform.offsetMin.y);
                m_LineTransform.offsetMax = new Vector2(m_LineLayoutMode == LineLayoutMode.Default ? -_RightSectionWidth : (m_LineLayoutMode == LineLayoutMode.IgnoreContent ? -m_Padding.right : 0), m_LineTransform.offsetMax.y);
            }

            /*if (caretTransform != null)
            {
                caretTransform.offsetMin = new Vector2(inputTextTransform.offsetMin.x, caretTransform.offsetMin.y);
                caretTransform.offsetMax = new Vector2(inputTextTransform.offsetMax.x, caretTransform.offsetMax.y);
            }*/

            SetBackgroundAndOutlineLayout(false);
            SetCounterAndValidationLayout(false);
        }

        public void SetLayoutVertical()
        {
            if (inputTextTransform != null && !HasIgnoreLayout(inputTextTransform))
            {
                inputTextTransform.offsetMin = new Vector2(inputTextTransform.offsetMin.x, _BottomSectionHeight);
                inputTextTransform.offsetMax = new Vector2(inputTextTransform.offsetMax.x, -_TopSectionHeight);
            }

            if (!TweenManager.TweenIsActive(_HintTextFloatingValueTweener))
            {
                hintTextFloatingValue = (isFocused || text.Length > 0) ? 1 : 0;
                SetHintLayoutToFloatingValue();
            }

            if (m_LeftContentTransform != null && !HasIgnoreLayout(m_LeftContentTransform))
            {
                ILayoutController[] controllers = m_LeftContentTransform.GetComponentsInChildren<ILayoutController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    controllers[i].SetLayoutVertical();
                }

                m_LeftContentTransform.anchoredPosition = new Vector2(m_LeftContentOffset.x + m_Padding.left, (inputTextTransform.offsetMax.y - (m_InputText.GetGraphicFontSize() / 2) - 2) + m_LeftContentOffset.y /*+ (m_Padding.bottom)*/);
            }

            if (m_RightContentTransform != null && !HasIgnoreLayout(m_RightContentTransform))
            {
                ILayoutController[] controllers = m_RightContentTransform.GetComponentsInChildren<ILayoutController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    controllers[i].SetLayoutVertical();
                }

                m_RightContentTransform.anchoredPosition = new Vector2(m_RightContentOffset.x - m_Padding.right, (inputTextTransform.offsetMax.y - (m_InputText.GetGraphicFontSize() / 2) - 2) + m_RightContentOffset.y /*+ (m_Padding.bottom)*/);
            }

            if (m_TopContentTransform != null && !HasIgnoreLayout(m_TopContentTransform))
            {
                ILayoutController[] controllers = m_TopContentTransform.GetComponentsInChildren<ILayoutController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    controllers[i].SetLayoutVertical();
                }

                var counterIsInsideBG = IsTopContentInsideOutline();
                m_TopContentTransform.offsetMin = new Vector2(m_Padding.left, -(_TopContentHeight + (counterIsInsideBG ? m_Padding.bottom : 0)));
                m_TopContentTransform.offsetMax = new Vector2(-m_Padding.right, -(counterIsInsideBG ? m_Padding.bottom : 0));
            }

            if (m_BottomContentTransform != null && !HasIgnoreLayout(m_BottomContentTransform))
            {
                ILayoutController[] controllers = m_BottomContentTransform.GetComponentsInChildren<ILayoutController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    controllers[i].SetLayoutVertical();
                }

                var counterIsInsideBG = IsBottomContentInsideOutline();
                m_BottomContentTransform.offsetMin = new Vector2(m_Padding.left, (counterIsInsideBG ? m_Padding.bottom : 0));
                m_BottomContentTransform.offsetMax = new Vector2(-m_Padding.right, (counterIsInsideBG ? m_Padding.bottom : 0) + _BottomContentHeight);
            }


            if (m_LineTransform != null && !HasIgnoreLayout(m_LineTransform))
            {
                m_LineTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, _BottomSectionHeight - m_Padding.bottom, 1);
            }

            if (inputField is InputField)
            {
                if (caretTransform != null)
                {
                    caretTransform.localPosition = inputText.rectTransform.localPosition;
                    caretTransform.localRotation = inputText.rectTransform.localRotation;
                    caretTransform.localScale = inputText.rectTransform.localScale;
                    caretTransform.anchorMin = inputText.rectTransform.anchorMin;
                    caretTransform.anchorMax = inputText.rectTransform.anchorMax;
                    caretTransform.anchoredPosition = inputText.rectTransform.anchoredPosition;
                    caretTransform.sizeDelta = inputText.rectTransform.sizeDelta;
                    caretTransform.pivot = inputText.rectTransform.pivot;
                    caretTransform.offsetMin = inputText.rectTransform.offsetMin;
                    caretTransform.offsetMax = inputText.rectTransform.offsetMax;
                }
            }

            SetBackgroundAndOutlineLayout(true);
            SetCounterAndValidationLayout(true);
        }

        public void CalculateLayoutInputHorizontal()
        {
            _LeftSectionWidth = 0;
            _RightSectionWidth = 0;

            if (m_LeftContentTransform != null && !HasIgnoreLayout(m_LeftContentTransform))
            {
                if (m_LeftContentTransform.gameObject.activeSelf)
                {
                    _LeftSectionWidth = m_LeftContentTransform.GetProperSize().x;
                    _LeftSectionWidth += 8;
                }
            }

            if (m_RightContentTransform != null && !HasIgnoreLayout(m_RightContentTransform))
            {
                if (m_RightContentTransform.gameObject.activeSelf)
                {
                    _RightSectionWidth = m_RightContentTransform.GetProperSize().x;
                    _RightSectionWidth += 8;
                }
            }

            SumPaddingHorizontal();

            if (m_FitWidthToContent)
            {
                _LayoutSize.x = GetTextPreferredWidth();
                _LayoutSize.x += _LeftSectionWidth;
                _LayoutSize.x += _RightSectionWidth;
            }
            else
            {
                _LayoutSize.x = m_ManualPreferredWidth ? m_ManualSize.x : -1;
            }
        }

        public void CalculateLayoutInputVertical()
        {
            _TopSectionHeight = _TopContentHeight = 0;
            if (m_FloatingHint || m_TopContentTransform != null)
            {
                _TopSectionHeight = _TopContentHeight = m_TopContentTransform != null && !HasIgnoreLayout(m_TopContentTransform) ? Mathf.Max(0, LayoutUtility.GetPreferredHeight(m_TopContentTransform)) : 0;

                if (_TopSectionHeight > 0)
                    _TopSectionHeight += 4;

                if (m_FloatingHint)
                {
                    _TopSectionHeight += Mathf.Max(0, GetSmallHintTextHeight());
                    if (_TopSectionHeight > 0)
                        _TopSectionHeight += 4;
                }
            }

            _BottomSectionHeight = _BottomContentHeight = 0;
            if (m_HasCharacterCounter || m_HasValidation || m_BottomContentTransform != null)
            {
                _BottomSectionHeight = _BottomContentHeight = m_BottomContentTransform != null && !HasIgnoreLayout(m_BottomContentTransform) ? Mathf.Max(0, LayoutUtility.GetPreferredHeight(m_BottomContentTransform)) : 0;

                if (m_HasCharacterCounter && counterText != null && !HasIgnoreLayout(counterText))
                {
                    //Spacing between UpperContent
                    if (_BottomSectionHeight > 0)
                        _BottomSectionHeight += 4;
                    _BottomSectionHeight += counterText != null ? Mathf.Max(0, LayoutUtility.GetPreferredHeight(counterText.rectTransform)) : 0;
                }
                else if (m_HasValidation && validationText != null && !HasIgnoreLayout(validationText))
                {
                    //Spacing between UpperContent
                    if (_BottomSectionHeight > 0)
                        _BottomSectionHeight += 4;
                    _BottomSectionHeight += validationText != null ? Mathf.Max(0, LayoutUtility.GetPreferredHeight(validationText.rectTransform)) : 0;
                }
            }
            SumPaddingVertical();

            if (m_FitHeightToContent)
            {
                _LayoutSize.y = GetTextPreferredHeight() + 4;
                _LayoutSize.y += _TopSectionHeight;
                _LayoutSize.y += _BottomSectionHeight;
            }
            else
            {
                _LayoutSize.y = m_ManualPreferredHeight ? m_ManualSize.y : -1;
            }

            if (m_LeftContentTransform != null && !HasIgnoreLayout(m_LeftContentTransform))
            {
                ILayoutElement[] elements = m_LeftContentTransform.GetComponentsInChildren<ILayoutElement>();
                elements = elements.Reverse().ToArray();
                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i].CalculateLayoutInputVertical();
                }
            }

            if (m_RightContentTransform != null && !HasIgnoreLayout(m_RightContentTransform))
            {
                ILayoutElement[] elements = m_RightContentTransform.GetComponentsInChildren<ILayoutElement>();
                elements = elements.Reverse().ToArray();
                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i].CalculateLayoutInputVertical();
                }
            }
        }

        protected void SumPaddingHorizontal()
        {
            _LeftSectionWidth += m_Padding.left;
            _RightSectionWidth += m_Padding.right;
        }

        protected void SumPaddingVertical()
        {
            _TopSectionHeight += m_Padding.top;
            _BottomSectionHeight += m_Padding.bottom;
        }

        public float minWidth { get { return -1; } }
        public float preferredWidth { get { return _LayoutSize.x; } }
        public float flexibleWidth { get { return -1; } }
        public float minHeight { get { return -1; } }
        public float preferredHeight { get { return _LayoutSize.y; } }
        public float flexibleHeight { get { return -1; } }
        public int layoutPriority { get { return 1; } }

        #endregion

        #region BaseStyleElement Overrides

        protected virtual void ApplyCanvasGroupChanged()
        {
            var isInteractable = IsInteractable();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = m_Interactable;
                canvasGroup.blocksRaycasts = m_Interactable;
                canvasGroup.alpha = isInteractable ? 1f : 0.5f;
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

        public override void RefreshVisualStyles(bool canAnimate = true)
        {
            UpdateSelectionState();
            SetStylePropertyColorsActive_Internal(canAnimate, m_AnimationDuration);
        }

        #endregion

        #region BaseStyleElement Helper Classes



        [System.Serializable]
        public class InputFieldStyleProperty : StyleProperty
        {
            public enum ColorModeEnum { Default, WithValidation, None }

            #region Private Variables

            [SerializeField, SerializeStyleProperty]
            protected ColorModeEnum m_colorMode = ColorModeEnum.Default;
            [SerializeField, SerializeStyleProperty]
            protected Color m_colorActive = Color.white;
            [SerializeField, SerializeStyleProperty]
            protected Color m_colorInactive = Color.gray;
            [SerializeField, SerializeStyleProperty]
            protected Color m_validationActive = Color.white;
            [SerializeField, SerializeStyleProperty]
            protected Color m_validationInactive = Color.gray;

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

            public Color ColorActive
            {
                get
                {
                    return m_colorActive;
                }

                set
                {
                    m_colorActive = value;
                }
            }

            public Color ColorInactive
            {
                get
                {
                    return m_colorInactive;
                }

                set
                {
                    m_colorInactive = value;
                }
            }

            public Color ValidationActive
            {
                get
                {
                    return m_validationActive;
                }

                set
                {
                    m_validationActive = value;
                }
            }

            public Color ValidationInactive
            {
                get
                {
                    return m_validationInactive;
                }

                set
                {
                    m_validationInactive = value;
                }
            }

            #endregion

            #region Constructor

            public InputFieldStyleProperty()
            {
            }

            public InputFieldStyleProperty(string name, Component target, Color colorActive, Color colorInactive, bool useStyleGraphic)
            {
                m_target = target != null ? target.transform : null;
                m_name = name;
                m_colorActive = colorActive;
                m_colorInactive = colorInactive;
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
                    var supportValidationColor = m_colorMode == ColorModeEnum.WithValidation;
                    var inputField = sender as MaterialInputField;
                    var isActive = inputField != null ? inputField.IsSelected() : true;
                    var isTextValid = supportValidationColor && inputField != null ? inputField.IsTextValid() : true;

                    var endColor = isActive ?
                        (isTextValid ? m_colorActive : m_validationActive) :
                        (isTextValid ? m_colorInactive : m_validationInactive);
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

        #region Editor Conversors

#if UNITY_EDITOR
        [ContextMenu("Convert To TextView Model", true)]
        bool ConvertToViewportModelValidate()
        {
            return m_InputText != null && (m_InputText.rectTransform == m_InputTextTransform || m_InputTextTransform == null);
        }

        [ContextMenu("Convert To TextView Model", false)]
        void ConvertToViewportModel()
        {
            if (m_InputText != null && (m_InputText.rectTransform == m_InputTextTransform || m_InputTextTransform == null))
            {
                RectMask2D textView = new GameObject("TextView").AddComponent<RectMask2D>();
                textView.transform.SetParent(m_InputText.transform.parent, false);

                //Set Property of TextViewport
                if (inputField != null)
                {
                    var textViewportMetaProperty = inputField.GetType().GetProperty("textViewport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (textViewportMetaProperty != null)
                        textViewportMetaProperty.SetValue(inputField, textView.rectTransform, null);
                }

                //Create ViewPort
                textView.rectTransform.SetSiblingIndex(inputText.rectTransform.GetSiblingIndex());
                textView.rectTransform.localPosition = inputText.rectTransform.localPosition;
                textView.rectTransform.localRotation = inputText.rectTransform.localRotation;
                textView.rectTransform.localScale = inputText.rectTransform.localScale;
                textView.rectTransform.anchorMin = inputText.rectTransform.anchorMin;
                textView.rectTransform.anchorMax = inputText.rectTransform.anchorMax;
                textView.rectTransform.anchoredPosition = inputText.rectTransform.anchoredPosition;
                textView.rectTransform.sizeDelta = inputText.rectTransform.sizeDelta;
                textView.rectTransform.pivot = new Vector2(0.5f, 0.5f); //inputText.rectTransform.pivot;
                textView.rectTransform.offsetMin = inputText.rectTransform.offsetMin;
                textView.rectTransform.offsetMax = inputText.rectTransform.offsetMax;

                //Change InputText to ViewPort Parent
                inputText.transform.SetParent(textView.rectTransform, true);
                inputText.rectTransform.localPosition = Vector2.zero;
                inputText.rectTransform.localRotation = Quaternion.identity;
                inputText.rectTransform.localScale = Vector3.one;
                inputText.rectTransform.anchorMin = Vector2.zero;
                inputText.rectTransform.anchorMax = Vector2.one;
                inputText.rectTransform.anchoredPosition = inputText.rectTransform.anchoredPosition;
                inputText.rectTransform.sizeDelta = Vector2.zero;
                inputText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                inputText.rectTransform.offsetMin = Vector2.zero;
                inputText.rectTransform.offsetMax = Vector2.zero;

                m_InputTextTransform = textView.rectTransform;
            }
        }
#endif

        #endregion

        #region ISerializationCallbacks Implementations

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                RegisterEvents();
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        #endregion
    }
}