//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace MaterialUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [ExecuteInEditMode]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("MaterialUI/Material Button", 100)]
    public class MaterialButton : StyleElement<MaterialButton.ButtonStyleProperty>, ILayoutGroup, ILayoutElement, ILayoutSelfController
    {
        #region Consts

        private const string pathToCirclePrefab = "Assets/MaterialUI/Prefabs/Components/Buttons/Floating Action Button.prefab";
        private const string pathToRectPrefab = "Assets/MaterialUI/Prefabs/Components/Buttons/Button.prefab";

        #endregion

        #region Private Variables

        [SerializeField]
        private RectTransform m_RectTransform = null;
        [SerializeField]
        private RectTransform m_ContentRectTransform = null;
        [SerializeField]
        private Button m_ButtonObject = null;
        [SerializeField]
        private MaterialRipple m_MaterialRipple = null;
        [SerializeField]
        private MaterialShadow m_MaterialShadow = null;
        [SerializeField]
        private CanvasGroup m_CanvasGroup = null;

        [SerializeField]
        private Graphic m_Icon = null;

        [SerializeField, SerializeStyleProperty]
        private CanvasGroup m_ShadowsCanvasGroup;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_BackgroundImage;
        [SerializeField, SerializeStyleProperty]
        private Graphic m_Text;

        [SerializeField]
        private bool m_Interactable = true;
        [SerializeField]
        private Vector2 m_ContentPadding = new Vector2(30f, 18f);
        [SerializeField]
        private Vector2 m_ContentSize = Vector2.zero;
        [SerializeField]
        private Vector2 m_Size = Vector2.zero;
        [SerializeField]
        private bool m_FitWidthToContent = false;
        [SerializeField]
        private bool m_FitHeightToContent = false;
        /*[SerializeField]
        private bool m_IsCircularButton = false;
        [SerializeField]
        private bool m_IsRaisedButton = false;*/
        [SerializeField]
        private bool m_ResetRippleOnDisable = true;

#if UNITY_EDITOR
        private Vector2 m_LastPosition;
#endif

        private DrivenRectTransformTracker m_Tracker = new DrivenRectTransformTracker();

        #endregion

        #region Properties

        public Button.ButtonClickedEvent OnClick
        {
            get
            {
                return buttonObject != null ? buttonObject.onClick : null;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    m_RectTransform = (RectTransform)transform;
                }
                return m_RectTransform;
            }
        }

        public RectTransform contentRectTransform
        {
            get { return m_ContentRectTransform; }
            set
            {
                m_ContentRectTransform = value;
                SetLayoutDirty();
            }
        }

        public Button buttonObject
        {
            get
            {
                if (m_ButtonObject == null)
                {
                    m_ButtonObject = gameObject.GetAddComponent<Button>();
                }
                return m_ButtonObject;
            }
        }

        public Graphic backgroundImage
        {
            get { return m_BackgroundImage; }
            set { m_BackgroundImage = value; }
        }

        public Graphic text
        {
            get { return m_Text; }
            set
            {
                m_Text = value;
                SetLayoutDirty();
            }
        }

        public Graphic icon
        {
            get { return m_Icon; }
            set
            {
                m_Icon = value;
                SetLayoutDirty();
            }
        }

        public MaterialRipple materialRipple
        {
            get
            {
                if (m_MaterialRipple == null)
                {
                    m_MaterialRipple = GetComponent<MaterialRipple>();
                }
                return m_MaterialRipple;
            }
        }

        public MaterialShadow materialShadow
        {
            get
            {
                if (m_MaterialShadow == null)
                {
                    m_MaterialShadow = GetComponent<MaterialShadow>();
                }
                return m_MaterialShadow;
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = gameObject.GetAddComponent<CanvasGroup>();
                }
                return m_CanvasGroup;
            }
        }

        public CanvasGroup shadowsCanvasGroup
        {
            get { return m_ShadowsCanvasGroup; }
            set { m_ShadowsCanvasGroup = value; }
        }

        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                m_ButtonObject.interactable = m_Interactable;
                canvasGroup.alpha = m_Interactable ? 1f : 0.5f;
                canvasGroup.blocksRaycasts = m_Interactable;
                if (shadowsCanvasGroup)
                {
                    shadowsCanvasGroup.alpha = m_Interactable ? 1f : 0f;
                }
                ApplyCanvasGroupChanged();
            }
        }

        public Vector2 contentPadding
        {
            get { return m_ContentPadding; }
            set
            {
                m_ContentPadding = value;
                SetLayoutDirty();
            }
        }

        public Vector2 contentSize
        {
            get { return m_ContentSize; }
        }

        public Vector2 size
        {
            get { return m_Size; }
        }

        public bool fitWidthToContent
        {
            get { return m_FitWidthToContent; }
            set
            {
                m_FitWidthToContent = value;
                m_Tracker.Clear();
                SetLayoutDirty();
            }
        }

        public bool fitHeightToContent
        {
            get { return m_FitHeightToContent; }
            set
            {
                m_FitHeightToContent = value;
                m_Tracker.Clear();
                SetLayoutDirty();
            }
        }

        /*public bool isCircularButton
        {
            get { return m_IsCircularButton; }
            set { m_IsCircularButton = value; }
        }

        public bool isRaisedButton
        {
            get { return m_IsRaisedButton; }
            set { m_IsRaisedButton = value; }
        }*/

        public bool resetRippleOnDisable
        {
            get { return m_ResetRippleOnDisable; }
            set { m_ResetRippleOnDisable = value; }
        }

        #endregion

        #region ExternalProperties

        [SerializeStyleProperty]
        public Color textColor
        {
            get { return m_Text != null? m_Text.color : Color.clear; }
            set
            {
                if(m_Text != null)
                    m_Text.color = value;
            }
        }

        [SerializeStyleProperty]
        public Color iconColor
        {
            get { return m_Icon != null? m_Icon.color : Color.clear; }
            set
            {
                if(m_Icon != null)
                    m_Icon.color = value;
            }
        }

        [SerializeStyleProperty]
        public Color backgroundColor
        {
            get { return m_BackgroundImage != null? m_BackgroundImage.color : Color.clear; }
            set
            {
                if(m_BackgroundImage != null)
                    m_BackgroundImage.color = value;
            }
        }

        public string textText
        {
            get { return m_Text != null? m_Text.GetGraphicText() : ""; }
            set
            {
                if(m_Text != null)
                    m_Text.SetGraphicText(value);
            }
        }

        public VectorImageData iconVectorImageData
        {
            get { return m_Icon != null ? m_Icon.GetVectorImage() : null; }
            set
            {
                if(m_Icon != null)
                    m_Icon.SetImage(value);
            }
        }

        public Sprite iconSprite
        {
            get { return m_Icon != null ? m_Icon.GetSpriteImage() : null; }
            set
            {
                if(m_Icon != null)
                    m_Icon.SetImage(value);
            }
        }

        public VectorImageData backgroundVectorImageData
        {
            get { return m_BackgroundImage != null ? m_BackgroundImage.GetVectorImage() : null; }
            set
            {
                if(m_BackgroundImage != null)
                    m_BackgroundImage.SetImage(value);
            }
        }

        public Sprite backgroundSprite
        {
            get { return m_BackgroundImage != null ? m_BackgroundImage.GetSpriteImage() : null; }
            set
            {
                if(m_BackgroundImage != null)
                    m_BackgroundImage.SetImage(value);
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            SetLayoutDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_Tracker.Clear();

            if (m_ResetRippleOnDisable)
            {
                EventSystem system = EventSystem.current;

                if (system != null)
                {
                    if (system.currentSelectedGameObject == gameObject)
                    {
                        system.SetSelectedGameObject(null);
                        if(materialRipple != null)
                            materialRipple.InstantDestroyAllRipples();
                    }
                }
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetLayoutDirty();
        }

        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();
            SetLayoutDirty();
            ApplyCanvasGroupChanged();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            SetLayoutDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidateDelayed()
        {
            base.OnValidateDelayed();
            if (m_RectTransform == null)
            {
                m_RectTransform = GetComponent<RectTransform>();
            }
            if (m_ButtonObject == null)
            {
                m_ButtonObject = gameObject.GetAddComponent<Button>();
            }
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = gameObject.GetAddComponent<CanvasGroup>();
            }

            SetLayoutDirty();
        }
#endif

        #endregion

        #region Other Functions

        public void SetButtonBackgroundColor(Color color, bool animate = true)
        {
            if (m_MaterialRipple != null)
            {
                m_MaterialRipple.SetGraphicColor(color, animate);
            }
            else
            {
                if (m_BackgroundImage == null) return;
                if (animate && Application.isPlaying)
                {
                    TweenManager.TweenColor(color1 => m_BackgroundImage.color = color1, m_BackgroundImage.color, color, 0.5f);
                }
                else
                {
                    m_BackgroundImage.color = color;
                }
            }
        }

        public void RefreshRippleMatchColor()
        {
            if (m_MaterialRipple != null)
            {
                m_MaterialRipple.RefreshGraphicMatchColor();
            }
        }

        /*public void Convert(bool noExitGUI = false)
        {
#if UNITY_EDITOR
            string flatRoundedSquare = "Assets/MaterialUI/Images/RoundedSquare/roundedsquare_";
            string raisedRoundedSquare = "Assets/MaterialUI/Images/RoundedSquare_Stroke/roundedsquare_stroke_";

            string imagePath = "";

            if (!isCircularButton)
            {
                imagePath = isRaisedButton ? flatRoundedSquare : raisedRoundedSquare;
            }

            if (isRaisedButton)
            {
                DestroyImmediate(m_ShadowsCanvasGroup.gameObject);
                m_ShadowsCanvasGroup = null;

                if (materialShadow)
                {
                    DestroyImmediate(materialShadow);
                }

                if (materialRipple != null)
                {
                    materialRipple.highlightWhen = MaterialRipple.HighlightActive.Hovered;
                }
            }
            else
            {
                string path = isCircularButton ? pathToCirclePrefab : pathToRectPrefab;

                GameObject tempButton = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));

#if UNITY_5_6_OR_NEWER
                GameObject newShadow = tempButton.transform.Find("Shadows").gameObject;
#else
                GameObject newShadow = tempButton.transform.Find("Shadows").gameObject;
#endif

                m_ShadowsCanvasGroup = newShadow.GetComponent<CanvasGroup>();

                RectTransform newShadowRectTransform = (RectTransform)newShadow.transform;

                newShadowRectTransform.SetParent(rectTransform);
                newShadowRectTransform.SetAsFirstSibling();
                newShadowRectTransform.localScale = Vector3.one;
                newShadowRectTransform.localEulerAngles = Vector3.zero;

                RectTransform tempRectTransform = m_BackgroundImage != null
                    ? (RectTransform)m_BackgroundImage.transform
                    : rectTransform;

                if (isCircularButton)
                {
                    newShadowRectTransform.anchoredPosition = Vector2.zero;
                    RectTransformSnap newSnapper = newShadow.GetAddComponent<RectTransformSnap>();
                    newSnapper.sourceRectTransform = tempRectTransform;
                    newSnapper.valuesArePercentage = true;
                    newSnapper.snapWidth = true;
                    newSnapper.snapHeight = true;
                    newSnapper.snapEveryFrame = true;
                    newSnapper.paddingPercent = new Vector2(225, 225);
                    Vector3 tempVector3 = rectTransform.GetPositionRegardlessOfPivot();
                    tempVector3.y -= 1f;
                    newShadowRectTransform.position = tempVector3;
                }
                else
                {
                    newShadowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tempRectTransform.GetProperSize().x + 54);
                    newShadowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tempRectTransform.GetProperSize().y + 54);
                    Vector3 tempVector3 = rectTransform.GetPositionRegardlessOfPivot();
                    newShadowRectTransform.position = tempVector3;
                }

                DestroyImmediate(tempButton);

                gameObject.AddComponent<MaterialShadow>();

                materialShadow.shadowsActiveWhen = MaterialShadow.ShadowsActive.Hovered;

                materialShadow.animatedShadows = newShadow.GetComponentsInChildren<AnimatedShadow>();

                materialShadow.isEnabled = true;

                if (materialRipple != null)
                {
                    materialRipple.highlightWhen = MaterialRipple.HighlightActive.Clicked;
                }
            }

            if (!isCircularButton)
            {
                SpriteSwapper spriteSwapper = GetComponent<SpriteSwapper>();

                if (spriteSwapper != null)
                {
                    spriteSwapper.sprite1X = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath + "100%.png");
                    spriteSwapper.sprite2X = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath + "200%.png");
                    spriteSwapper.sprite4X = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath + "400%.png");
                }
                else
                {
                    if (m_BackgroundImage != null)
                    {
                        ((Image)m_BackgroundImage).sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath + "100%.png");
                    }
                }
            }
            else
            {
                if (!isRaisedButton)
                {

                    RectTransform tempRectTransform = (RectTransform)new GameObject("Stroke", typeof(VectorImageTMPro)).transform;

                    tempRectTransform.SetParent(m_BackgroundImage.rectTransform);
                    tempRectTransform.localScale = Vector3.one;
                    tempRectTransform.localEulerAngles = Vector3.zero;
                    tempRectTransform.anchorMin = Vector2.zero;
                    tempRectTransform.anchorMax = Vector2.one;
                    tempRectTransform.anchoredPosition = Vector2.zero;
                    tempRectTransform.sizeDelta = Vector2.zero;

                    VectorImageTMPro vectorImage = tempRectTransform.GetComponent<VectorImageTMPro>();
                    vectorImage.vectorImageData = MaterialUIIconHelper.GetIcon("circle_stroke_thin").vectorImageData;
                    vectorImage.sizeMode = VectorImageSizeMode.MatchMin;
                    vectorImage.color = new Color(0f, 0f, 0f, 0.125f);

                    tempRectTransform.name = "Stroke";
                }
                else
                {
                    IVectorImage[] images = backgroundImage.GetComponentsInChildren<IVectorImage>();

                    for (int i = 0; i < images.Length; i++)
                    {
                        if (images[i].name == "Stroke")
                        {
                            DestroyImmediate(images[i].gameObject);
                        }
                    }
                }
            }

            name = isRaisedButton ? name.Replace("Raised", "Flat") : name.Replace("Flat", "Raised");

            if (m_BackgroundImage != null)
            {
                if (!isRaisedButton)
                {
                    if (m_BackgroundImage.color == Color.clear)
                    {
                        m_BackgroundImage.color = Color.white;
                    }
                }
                else
                {

                    if (m_BackgroundImage.color == Color.white)
                    {
                        m_BackgroundImage.color = Color.clear;
                    }
                }
            }

            m_IsRaisedButton = !m_IsRaisedButton;

            if (!noExitGUI)
            {
                GUIUtility.ExitGUI();
            }
#endif
        }*/

        public void ClearTracker()
        {
            m_Tracker.Clear();
        }

        protected virtual void ApplyCanvasGroupChanged()
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
            canvasGroup.alpha = interactable ? 1f : 0.5f;
            if (shadowsCanvasGroup)
                shadowsCanvasGroup.alpha = interactable ? 1f : 0f;
        }

        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0);
        }

        #endregion

        #region Layout

        public void SetLayoutDirty()
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public void SetLayoutHorizontal()
        {
            if (m_FitWidthToContent)
            {
                if (m_ContentRectTransform == null) return;
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_Size.x);
                m_Tracker.Add(this, m_ContentRectTransform, DrivenTransformProperties.SizeDeltaX);
                m_ContentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_ContentSize.x);
            }
        }

        public void SetLayoutVertical()
        {
            if (m_FitHeightToContent)
            {
                if (m_ContentRectTransform == null) return;
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_Size.y);
                m_Tracker.Add(this, m_ContentRectTransform, DrivenTransformProperties.SizeDeltaY);
                m_ContentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_ContentSize.y);
            }
        }

        public void CalculateLayoutInputHorizontal()
        {
            if (m_FitWidthToContent)
            {
                if (m_ContentRectTransform == null) return;
                m_ContentSize.x = LayoutUtility.GetPreferredWidth(m_ContentRectTransform);
                m_Size.x = m_ContentSize.x + m_ContentPadding.x;
            }
            else
            {
                m_Size.x = -1;
            }
        }

        public void CalculateLayoutInputVertical()
        {
            if (m_FitHeightToContent)
            {
                if (m_ContentRectTransform == null) return;
                m_ContentSize.y = LayoutUtility.GetPreferredHeight(m_ContentRectTransform);
                m_Size.y = m_ContentSize.y + m_ContentPadding.y;
            }
            else
            {
                m_Size.y = -1;
            }
        }

        public float minWidth { get { return enabled ? m_Size.x : 0; } }
        public float preferredWidth { get { return minWidth; } }
        public float flexibleWidth { get { return -1; } }
        public float minHeight { get { return enabled ? m_Size.y : 0; } }
        public float preferredHeight { get { return minHeight; } }
        public float flexibleHeight { get { return -1; } }
        public int layoutPriority { get { return 1; } }

        #endregion

        #region BaseStyleElement Helper Classes

        [System.Serializable]
        public class ButtonStyleProperty : StyleProperty
        {
            #region Private Variables

            [SerializeField, SerializeStyleProperty]
            protected Color m_color = Color.white;

            #endregion

            #region Public Properties

            public Color Color
            {
                get
                {
                    return m_color;
                }

                set
                {
                    m_color = value;
                }
            }

            #endregion

            #region Constructor

            public ButtonStyleProperty()
            {
            }

            public ButtonStyleProperty(string p_name, Component p_target, Color p_color, bool p_useStyleGraphic)
            {
                m_target = p_target != null ? p_target.transform : null;
                m_name = p_name;
                m_color = p_color;
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
                    var v_endColor = m_color;
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