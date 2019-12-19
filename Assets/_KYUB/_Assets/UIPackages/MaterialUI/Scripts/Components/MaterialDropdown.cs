//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{
    /// <summary>
    /// Component that handles a dropdown control.
    /// </summary>
    /// <seealso cref="UnityEngine.EventSystems.UIBehaviour" />
    /// <seealso cref="MaterialUI.IOptionDataListContainer" />
    [AddComponentMenu("MaterialUI/Dropdown", 100)]
    public class MaterialDropdown : UIBehaviour, IOptionDataListContainer
    {
        #region Helper Classes

        public enum VerticalPivotType
        {
            /// <summary>
            /// The dropdown's top will align with the bottom of the base rectTransform when Shown.
            /// </summary>
            BelowBase,
            /// <summary>
            /// The dropdown's top will align with the top of the base rectTransform when Shown.
            /// </summary>
            Top,
            /// <summary>
            /// The dropdown's first item will align with the center of the base rectTransform when Shown.
            /// </summary>
            FirstItem,
            /// <summary>
            /// The dropdown's center will align with the center of the base rectTransform when Shown.
            /// </summary>
            Center,
            /// <summary>
            /// The dropdown's last item will align with the center of the base rectTransform when Shown.
            /// </summary>
            LastItem,
            /// <summary>
            /// The dropdown's bottom will align with the bottom of the base rectTransform when Shown.
            /// </summary>
            Bottom,
            /// <summary>
            /// The dropdown's bottom will align with the top of the base rectTransform when Shown.
            /// </summary>
            AboveBase
        }

        public enum HorizontalPivotType
        {
            /// <summary>
            /// The dropdown's left edge will align with the left edge of the base rectTransform when shown.
            /// </summary>
            Left,
            /// <summary>
            /// The dropdown's center will align with the center of the base rectTransform when shown.
            /// </summary>
            Center,
            /// <summary>
            /// The dropdown's right edge will align with the right edge of the base rectTransform when shown.
            /// </summary>
            Right
        }

        public enum ExpandStartType
        {
            /// <summary>
            /// The dropdown's size will be (0,0) when it begins to expand.
            /// </summary>
            ExpandFromNothing,
            /// <summary>
            /// The dropdown's width will match the width of the base RectTransform when it begins to expand.
            /// </summary>
            ExpandFromBaseTransformWidth,
            /// <summary>
            /// The dropdown's height will match the height of the base RectTransform when it begins to expand.
            /// </summary>
            ExpandFromBaseTransformHeight,
            /// <summary>
            /// The dropdown's size will match the size of the base RectTransform when it begins to expand.
            /// </summary>
            ExpandFromBaseTransformSize
        }

        [Serializable]
        public class MaterialDropdownEvent : UnityEvent<int> { }

        #endregion

        #region Private Variables

        [SerializeField]
        private VerticalPivotType m_VerticalPivotType = VerticalPivotType.FirstItem;
        [SerializeField]
        private HorizontalPivotType m_HorizontalPivotType = HorizontalPivotType.Left;
        [SerializeField]
        private ExpandStartType m_ExpandStartType = ExpandStartType.ExpandFromBaseTransformSize;
        [SerializeField]
        private float m_IgnoreInputAfterShowTimer = 0;
        [SerializeField]
        private float m_MaxHeight = 200;
        [SerializeField]
        private bool m_CapitalizeButtonText = true;
        [SerializeField]
        private bool m_HighlightCurrentlySelected = true;
        [SerializeField]
        private bool m_UpdateHeader = true;
        [SerializeField]
        private float m_AnimationDuration = 0.3f;
        [SerializeField]
        private float m_MinDistanceFromEdge = 16f;
        [SerializeField]
        private Color m_PanelColor = Color.white;
        [SerializeField]
        private RippleData m_ItemRippleData = null;
        [SerializeField]
        private Color m_ItemTextColor = MaterialColor.textDark;
        [SerializeField]
        private Color m_ItemIconColor = MaterialColor.iconDark;
        [SerializeField]
        private RectTransform m_BaseTransform = null;
        [SerializeField]
        private Selectable m_BaseSelectable = null;
        [SerializeField]
        private Graphic m_ButtonTextContent = null;
        [SerializeField]
        private Graphic m_ButtonImageContent = null;
        [SerializeField]
        private int m_CurrentlySelected = 0;
        [SerializeField]
        private OptionDataList m_OptionDataList = null;
        [Space]
        [SerializeField]
        private RectTransform m_DropdownPanelTemplate = null;

        private List<DropdownListItem> m_ListItems = new List<DropdownListItem>();
        private MaterialUIScaler m_Scaler;
        private RectTransform m_DropdownPanel;
        private RectTransform m_PanelLayer;
        private CanvasGroup m_DropdownCanvasGroup;
        private Canvas m_DropdownCanvas;
        private CanvasGroup m_ShadowCanvasGroup;
        private DropdownListItem m_ListItemTemplate;
        private RectTransform m_CancelPanel;
        private Vector2 m_ExpandedSize;
        private Vector3 m_ExpandedPosition;
        private float m_FullHeight;
        private bool m_IsExpanded;
        private float m_TempMaxHeight;
        private float m_ScrollPosOffset;
        private float m_TimeShown;
        private GameObject m_DropdownCanvasGameObject;
        private List<int> m_AutoTweeners;
        private List<int> m_ListItemAutoTweeners = new List<int>();

        #endregion

        #region Callbacks

        [SerializeField]
        private MaterialDropdownEvent m_OnItemSelected = new MaterialDropdownEvent();

        #endregion

        #region Public Properties

        public VerticalPivotType verticalPivotType
        {
            get { return m_VerticalPivotType; }
            set { m_VerticalPivotType = value; }
        }

        public HorizontalPivotType horizontalPivotType
        {
            get { return m_HorizontalPivotType; }
            set { m_HorizontalPivotType = value; }
        }

        public ExpandStartType expandStartType
        {
            get { return m_ExpandStartType; }
            set { m_ExpandStartType = value; }
        }

        public float ignoreInputAfterShowTimer
        {
            get { return m_IgnoreInputAfterShowTimer; }
            set { m_IgnoreInputAfterShowTimer = value; }
        }

        public float maxHeight
        {
            get { return m_MaxHeight; }
            set { m_MaxHeight = value; }
        }

        public bool capitalizeButtonText
        {
            get { return m_CapitalizeButtonText; }
            set { m_CapitalizeButtonText = value; }
        }

        public bool highlightCurrentlySelected
        {
            get { return m_HighlightCurrentlySelected; }
            set { m_HighlightCurrentlySelected = value; }
        }

        public bool updateHeader
        {
            get { return m_UpdateHeader; }
            set { m_UpdateHeader = value; }
        }

        public float animationDuration
        {
            get { return m_AnimationDuration; }
            set { m_AnimationDuration = value; }
        }

        public float minDistanceFromEdge
        {
            get { return m_MinDistanceFromEdge; }
            set { m_MinDistanceFromEdge = value; }
        }

        public Color panelColor
        {
            get { return m_PanelColor; }
            set { m_PanelColor = value; }
        }

        public RippleData itemRippleData
        {
            get { return m_ItemRippleData; }
            set { m_ItemRippleData = value; }
        }

        public Color itemTextColor
        {
            get { return m_ItemTextColor; }
            set { m_ItemTextColor = value; }
        }

        public Color itemIconColor
        {
            get { return m_ItemIconColor; }
            set { m_ItemIconColor = value; }
        }

        public RectTransform baseTransform
        {
            get { return m_BaseTransform; }
            set { m_BaseTransform = value; }
        }

        public Selectable baseSelectable
        {
            get { return m_BaseSelectable; }
            set { m_BaseSelectable = value; }
        }

        public Graphic buttonTextContent
        {
            get { return m_ButtonTextContent; }
            set { m_ButtonTextContent = value; }
        }

        public Graphic buttonImageContent
        {
            get { return m_ButtonImageContent; }
            set { m_ButtonImageContent = value; }
        }

        public int currentlySelected
        {
            get { return m_CurrentlySelected; }
            set
            {
                m_CurrentlySelected = Mathf.Clamp(value, -1, m_OptionDataList.options.Count - 1);

                if (m_CurrentlySelected >= 0)
                {
                    if (m_ButtonImageContent != null)
                    {
                        m_ButtonImageContent.SetImage(m_OptionDataList.options[m_CurrentlySelected].imageData);
                    }

                    if (m_ButtonTextContent != null)
                    {
                        string itemText = m_OptionDataList.options[m_CurrentlySelected].text;

                        if (m_CapitalizeButtonText)
                        {
                            itemText = itemText.ToUpper();
                        }

                        m_ButtonTextContent.SetGraphicText(itemText);
                    }
                }
            }
        }

        public OptionDataList optionDataList
        {
            get { return m_OptionDataList; }
            set { m_OptionDataList = value; }
        }

        public MaterialDropdownEvent onItemSelected
        {
            get { return m_OnItemSelected; }
            set { m_OnItemSelected = value; }
        }

        public List<DropdownListItem> listItems
        {
            get { return m_ListItems; }
            set { m_ListItems = value; }
        }

        private MaterialUIScaler scaler
        {
            get
            {
                if (m_Scaler == null)
                {
                    m_Scaler = MaterialUIScaler.GetRootScaler(transform as RectTransform);
                }

                return m_Scaler;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if(m_DropdownPanelTemplate != null)
                m_DropdownPanelTemplate.gameObject.SetActive(false);
        }

        protected override void Start()
        {
            Canvas[] canvasses = FindObjectsOfType<Canvas>();

            for (int i = 0; i < canvasses.Length; i++)
            {
                if (canvasses[i].name == "Dropdown Canvas")
                {
                    m_DropdownCanvasGameObject = canvasses[i].gameObject;
                }
            }

            if (m_DropdownCanvasGameObject == null)
            {
                m_DropdownCanvasGameObject = new GameObject("Dropdown Canvas");
            }

            var rootCanvas = m_BaseTransform.GetRootCanvas();
            m_DropdownCanvas = rootCanvas.Copy(m_DropdownCanvasGameObject);
        }


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (m_OptionDataList == null)
                return;
            for (int i = 0; i < m_OptionDataList.options.Count; i++)
            {
                m_OptionDataList.options[i].imageData.imageDataType = m_OptionDataList.imageType;
            }

            m_CurrentlySelected = Mathf.Clamp(m_CurrentlySelected, -1, m_OptionDataList.options.Count - 1);

            if (m_CurrentlySelected >= 0)
            {
                if (m_ButtonImageContent != null && m_UpdateHeader)
                {
                    m_ButtonImageContent.SetImage(m_OptionDataList.options[m_CurrentlySelected].imageData);
                }

                if (m_ButtonTextContent != null && m_UpdateHeader)
                {
                    string itemText = m_OptionDataList.options[m_CurrentlySelected].text;

                    if (m_CapitalizeButtonText)
                    {
                        itemText = itemText.ToUpper();
                    }

                    m_ButtonTextContent.SetGraphicText(itemText);
                }
            }
        }
#endif

        #endregion

        #region Helper Functions

        public void AddData(OptionData data)
        {
            m_OptionDataList.options.Add(data);
        }

        public void AddData(OptionData[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                AddData(data[i]);
            }
        }

        public void RemoveData(OptionData data)
        {
            m_OptionDataList.options.Remove(data);

            m_CurrentlySelected = Mathf.Clamp(m_CurrentlySelected, 0, m_OptionDataList.options.Count - 1);
        }

        public void RemoveData(OptionData[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                RemoveData(data[i]);
            }
        }

        public void ClearData()
        {
            m_OptionDataList.options.Clear();

            m_CurrentlySelected = Mathf.Clamp(m_CurrentlySelected, 0, m_OptionDataList.options.Count - 1);
        }

        public void Show()
        {
            Canvas rootCanvas = m_BaseTransform.GetRootCanvas();
            rootCanvas.CopySettingsToOtherCanvas(m_DropdownCanvas);
            //m_DropdownCanvas.pixelPerfect = false;
            m_DropdownCanvas.sortingOrder = 30000;

            if (m_DropdownPanelTemplate == null)
                m_DropdownPanel = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dropdownPanel, m_DropdownCanvas.transform).GetComponent<RectTransform>();
            else
            {
                m_DropdownPanelTemplate.gameObject.SetActive(false);
                m_DropdownPanel = GameObject.Instantiate(m_DropdownPanelTemplate);
                m_DropdownPanel.transform.SetParent(m_DropdownCanvas.transform);
                m_DropdownPanel.transform.localScale = Vector3.one;
                m_DropdownPanel.transform.localEulerAngles = Vector3.zero;
                m_DropdownPanel.transform.localPosition = Vector3.zero;
                m_DropdownPanel.gameObject.SetActive(true);
            }
            m_PanelLayer = m_DropdownPanel.GetChildByName<RectTransform>("PanelLayer");
            m_DropdownCanvasGroup = m_DropdownPanel.GetComponent<CanvasGroup>();
            m_ShadowCanvasGroup = m_DropdownPanel.GetChildByName<CanvasGroup>("Shadow");

            m_DropdownPanel.GetRootCanvas().scaleFactor = rootCanvas.scaleFactor;

            m_CancelPanel = m_DropdownPanel.GetChildByName<RectTransform>("Cancel Panel");
            m_CancelPanel.sizeDelta = scaler.canvas.pixelRect.size * 2;
            DropdownTrigger trigger = m_DropdownPanel.gameObject.GetChildByName<DropdownTrigger>("Cancel Panel");
            trigger.index = -1;
            trigger.dropdown = this;

            m_DropdownPanel.gameObject.GetChildByName<Image>("ScrollRect").color = m_PanelColor;

            m_ListItemTemplate = m_DropdownPanel.GetChildByName<DropdownListItem>("Item");
            if (m_ListItemTemplate.text == null)
            {
                m_ListItemTemplate.text = m_ListItemTemplate.GetChildByName<Graphic>("Text");
            }

            if (m_OptionDataList.imageType == ImageDataType.Sprite)
            {
                if(m_ListItemTemplate.image == null)
                    m_ListItemTemplate.image = m_ListItemTemplate.GetChildByName<Image>("Icon");
                if(m_ListItemTemplate.image != null)
                    m_ListItemTemplate.image.gameObject.SetActive(true);
                if(m_ListItemTemplate.vectorImage != null)
                    m_ListItemTemplate.vectorImage.gameObject.SetActive(false);
            }
            else
            {
                if (m_ListItemTemplate.vectorImage == null)
                    m_ListItemTemplate.vectorImage = m_ListItemTemplate.GetChildByName<IVectorImage>("VectorIcon");
                if (m_ListItemTemplate.vectorImage != null)
                    m_ListItemTemplate.vectorImage.gameObject.SetActive(true);
                if (m_ListItemTemplate.image != null)
                    m_ListItemTemplate.image.gameObject.SetActive(false);
            }

            m_ListItems = new List<DropdownListItem>();

            for (int i = 0; i < m_OptionDataList.options.Count; i++)
            {
                m_ListItems.Add(CreateItem(m_OptionDataList.options[i], i));
            }

            for (int i = 0; i < m_ListItems.Count; i++)
            {
                Selectable selectable = m_ListItems[i].rectTransform.GetComponent<Selectable>();
                Navigation navigation = new Navigation();
                navigation.mode = Navigation.Mode.Explicit;

                if (i > 0)
                {
                    navigation.selectOnUp = m_ListItems[i - 1].rectTransform.GetComponent<Selectable>();
                }
                if (i < m_ListItems.Count - 1)
                {
                    navigation.selectOnDown = m_ListItems[i + 1].rectTransform.GetComponent<Selectable>();
                }

                selectable.navigation = navigation;
            }

            if (m_BaseSelectable != null)
            {
                if (m_ListItems.Count > 0)
                {
                    Navigation navigation = Navigation.defaultNavigation;
                    navigation.selectOnDown = m_ListItems[0].rectTransform.GetComponent<Selectable>();
                    m_BaseSelectable.navigation = navigation;
                }
            }

            float maxWidth = CalculateMaxItemWidth();
            float buttonWidth = m_BaseTransform.rect.width;

            m_FullHeight = m_OptionDataList.options.Count * LayoutUtility.GetPreferredHeight(m_ListItemTemplate.rectTransform) + 16;
            m_ListItemTemplate.gameObject.SetActive(false);

            //Check if DropdownPanel will Autocalculate Size
            var dropDownFitter = m_DropdownPanel.GetComponent<ContentSizeFitter>();
            if (dropDownFitter != null && dropDownFitter.enabled)
            {
                var recalculateHeight = dropDownFitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained;
                var recalculateWidth = dropDownFitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained;
                if (recalculateHeight || recalculateWidth)
                {
                    //We must recalculate two times to apply correct size fitter values
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_DropdownPanel);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_DropdownPanel);
                }

                if (recalculateHeight)
                    m_FullHeight = LayoutUtility.GetPreferredHeight(m_DropdownPanel);
                if (recalculateWidth)
                    maxWidth = LayoutUtility.GetPreferredWidth(m_DropdownPanel);
            }

            m_ExpandedSize = new Vector2(Mathf.Max(maxWidth, buttonWidth), m_FullHeight);

            m_TempMaxHeight = m_MaxHeight;

            if (m_TempMaxHeight == 0)
            {
                m_TempMaxHeight = MaterialUIScaler.GetRootScaler(m_DropdownPanel).canvas.GetComponent<RectTransform>().rect.height - 32;
            }

            if (m_ExpandedSize.y > m_TempMaxHeight)
            {
                m_ExpandedSize.y = m_TempMaxHeight;
            }
            else
            {
                m_DropdownPanel.GetChildByName<Image>("Handle").gameObject.SetActive(false);
            }

            

            m_DropdownPanel.position = m_BaseTransform.GetPositionRegardlessOfPivot();

            if (m_ExpandStartType == ExpandStartType.ExpandFromBaseTransformWidth)
            {
                if (m_VerticalPivotType == VerticalPivotType.BelowBase)
                {
                    m_DropdownPanel.position = new Vector3(m_DropdownPanel.position.x, m_DropdownPanel.position.y - m_BaseTransform.GetProperSize().y / 2, m_DropdownPanel.position.z);
                }
                else if (m_VerticalPivotType == VerticalPivotType.AboveBase)
                {
                    m_DropdownPanel.position = new Vector3(m_DropdownPanel.position.x, m_DropdownPanel.position.y + m_BaseTransform.GetProperSize().y / 2, m_DropdownPanel.position.z);
                }
            }


            m_ExpandedPosition = CalculatedPosition();
            m_ExpandedPosition.z = m_BaseTransform.position.z;

            m_DropdownCanvasGroup.alpha = 0f;
            m_ShadowCanvasGroup.alpha = 0f;

            if (m_ExpandStartType == ExpandStartType.ExpandFromBaseTransformWidth)
            {
                m_DropdownPanel.sizeDelta = new Vector2(m_BaseTransform.rect.size.x, 0f);
            }
            else if (m_ExpandStartType == ExpandStartType.ExpandFromBaseTransformHeight)
            {
                m_DropdownPanel.sizeDelta = new Vector2(0f, m_BaseTransform.rect.size.y);
            }
            else if (m_ExpandStartType == ExpandStartType.ExpandFromBaseTransformSize)
            {
                m_DropdownPanel.sizeDelta = m_BaseTransform.rect.size;
            }
            else
            {
                m_DropdownPanel.sizeDelta = Vector2.zero;
            }

            m_DropdownPanel.gameObject.SetActive(true);

            for (int i = 0; i < m_ListItemAutoTweeners.Count; i++)
            {
                TweenManager.EndTween(m_ListItemAutoTweeners[i]);
            }

            m_AutoTweeners = new List<int>();
            m_ListItemAutoTweeners = new List<int>();

            m_AutoTweeners.Add(TweenManager.TweenFloat(
                f =>
                {
                    if (m_DropdownCanvasGroup != null)
                        m_DropdownCanvasGroup.alpha = f;
                }, m_DropdownCanvasGroup.alpha, 1f, m_AnimationDuration * 0.66f, 0, null, false, Tween.TweenType.Linear));
            m_AutoTweeners.Add(TweenManager.TweenFloat(
                f => 
                {
                    if(m_ShadowCanvasGroup != null)
                        m_ShadowCanvasGroup.alpha = f;
                }, 
                m_ShadowCanvasGroup.alpha, 1f, m_AnimationDuration * 0.66f, 0, null, false, Tween.TweenType.Linear));

            /*m_AutoTweeners.Add(TweenManager.TweenVector2(
                vector2 =>
                {
                    if(m_DropdownPanel != null)
                        m_DropdownPanel.sizeDelta = vector2;
                },
                m_DropdownPanel.sizeDelta, m_ExpandedSize, m_AnimationDuration, m_AnimationDuration / 3, null, false, Tween.TweenType.EaseInOutQuint));*/

            m_DropdownPanel.localScale = Vector3.zero;
            m_AutoTweeners.Add(TweenManager.TweenVector3(
                vector3 =>
                {
                    if(m_DropdownPanel != null)
                        m_DropdownPanel.localScale = vector3;
                },
                Vector3.zero, Vector3.one, m_AnimationDuration, m_AnimationDuration / 3, null, false, Tween.TweenType.EaseInOutQuint));

            m_AutoTweeners.Add(TweenManager.TweenVector3(UpdateDropdownPos, m_DropdownPanel.position, m_ExpandedPosition, m_AnimationDuration, m_AnimationDuration / 3, () =>
            {
                if (m_BaseSelectable != null && m_IsExpanded)
                {
                    m_BaseSelectable.interactable = false;
                }
                if (m_PanelLayer != null)
                {
                    Vector2 tempVector2 = m_PanelLayer.anchoredPosition;
                    tempVector2.x = Mathf.RoundToInt(tempVector2.x);
                    tempVector2.y = Mathf.RoundToInt(tempVector2.y);
                    m_PanelLayer.anchoredPosition = tempVector2;
                }
            }, false, Tween.TweenType.EaseInOutQuint));

            for (int i = 0; i < m_ListItems.Count; i++)
            {
                int i1 = i;
                CanvasGroup canvasGroup = m_ListItems[i].canvasGroup;
                m_ListItemAutoTweeners.Add(TweenManager.TweenFloat(
                    f => 
                    {
                        if(canvasGroup != null)
                            canvasGroup.alpha = f;
                    }, 
                    canvasGroup.alpha, 1f,
                    m_AnimationDuration * 1.66f, (i1 * (m_AnimationDuration / 6) + m_AnimationDuration) - m_ScrollPosOffset / 800, null, false, Tween.TweenType.Linear));
            }

            if (m_FullHeight > m_TempMaxHeight)
            {
                m_DropdownPanel.GetChildByName<ScrollRect>("ScrollRect").gameObject.AddComponent<RectMask2D>();
            }

            m_IsExpanded = true;

            m_TimeShown = Time.unscaledTime;
        }

        private void UpdateDropdownPos(Vector3 position)
        {
            m_DropdownPanel.position = position;
            m_DropdownPanel.localPosition = new Vector3(m_DropdownPanel.localPosition.x, m_DropdownPanel.localPosition.y, 0f);
        }

        public void Hide()
        {
            for (int i = 0; i < m_ListItemAutoTweeners.Count; i++)
            {
                TweenManager.EndTween(m_ListItemAutoTweeners[i]);
            }

            m_IsExpanded = false;

            if (m_BaseSelectable != null)
            {
                m_BaseSelectable.interactable = true;
            }

            for (int i = 0; i < m_ListItems.Count; i++)
            {
                int i1 = i;
                CanvasGroup canvasGroup = m_ListItems[i].canvasGroup;
                TweenManager.TweenFloat(f => canvasGroup.alpha = f, canvasGroup.alpha, 0f, m_AnimationDuration * 0.66f, (m_ListItems.Count - i1) * (m_AnimationDuration / 6), null, false, Tween.TweenType.Linear);
            }

            m_AutoTweeners.Add(TweenManager.TweenFloat(f => m_DropdownCanvasGroup.alpha = f, m_DropdownCanvasGroup.alpha, 0f, m_AnimationDuration * 0.66f, m_AnimationDuration, null, false, Tween.TweenType.Linear));

            TweenManager.TweenFloat(f => m_ShadowCanvasGroup.alpha = f, m_ShadowCanvasGroup.alpha, 0f, m_AnimationDuration * 0.66f, m_AnimationDuration, () =>
            {
                for (int i = 0; i < m_AutoTweeners.Count; i++)
                {
                    TweenManager.EndTween(m_AutoTweeners[i]);
                }

                Destroy(m_DropdownPanel.gameObject);
            }, false, Tween.TweenType.Linear);
        }

        public void Select(int selectedItem, bool submitted = false)
        {
            if (Time.unscaledTime - m_TimeShown < m_IgnoreInputAfterShowTimer) return;

            if (selectedItem >= 0)
            {
                if (m_ButtonImageContent != null && m_UpdateHeader)
                {
                    m_ButtonImageContent.SetImage(m_OptionDataList.options[selectedItem].imageData);
                }

                if (m_ButtonTextContent != null && m_UpdateHeader)
                {
                    string itemText = m_OptionDataList.options[selectedItem].text;

                    if (m_CapitalizeButtonText)
                    {
                        itemText = itemText.ToUpper();
                    }

                    m_ButtonTextContent.SetGraphicText(itemText);
                }

                m_CurrentlySelected = selectedItem;
            }

            if (!m_IsExpanded) return;

            Hide();

            if (submitted && m_BaseSelectable != null)
            {
                EventSystem.current.SetSelectedGameObject(m_BaseSelectable.gameObject);
            }

            if (m_OnItemSelected != null)
            {
                m_OnItemSelected.Invoke(selectedItem);
            }

            if (selectedItem >= 0 && selectedItem < m_OptionDataList.options.Count)
            {
                m_OptionDataList.options[selectedItem].onOptionSelected.InvokeIfNotNull();
            }
        }

        private Vector3 CalculatedPosition()
        {
            Vector3 position = m_BaseTransform.GetPositionRegardlessOfPivot();
            float itemHeight = m_ListItemTemplate.rectTransform.GetProperSize().y;
            float minScrollPos = 0f;
            float maxScrollPos = Mathf.Clamp(m_FullHeight - m_TempMaxHeight, 0f, float.MaxValue);

            int flipper = (int)m_VerticalPivotType < 3 ? 1 : -1;

            if (m_VerticalPivotType == VerticalPivotType.BelowBase || m_VerticalPivotType == VerticalPivotType.AboveBase)
            {
                float baseHeight = m_BaseTransform.GetProperSize().y;

                position.y -= m_ExpandedSize.y * scaler.canvas.transform.localScale.x / 2 * flipper;
                position.y -= baseHeight * scaler.canvas.transform.localScale.x / 2 * flipper;
            }
            else if (m_VerticalPivotType == VerticalPivotType.Top || m_VerticalPivotType == VerticalPivotType.Bottom)
            {
                position.y -= m_ExpandedSize.y * scaler.canvas.transform.localScale.x / 2 * flipper;
                position.y += itemHeight * scaler.canvas.transform.localScale.x / 2 * flipper;
                //  I have absolutely no idea why 3 works better than 4 (according to my math, it should be 4). I've probably missed something, but it works :)
                position.y -= 3 * scaler.canvas.transform.localScale.x * flipper;
            }
            else if (m_VerticalPivotType == VerticalPivotType.FirstItem || m_VerticalPivotType == VerticalPivotType.LastItem)
            {
                position.y -= m_ExpandedSize.y * scaler.canvas.transform.localScale.x / 2f * flipper;
                position.y += itemHeight * scaler.canvas.transform.localScale.x / 2f * flipper;
                position.y += 8f * scaler.canvas.transform.localScale.x / 2f * flipper;
            }

            if (m_HighlightCurrentlySelected)
            {
                Vector2 tempVector2 = m_PanelLayer.anchoredPosition;
                tempVector2.y += itemHeight * Mathf.Clamp(m_CurrentlySelected, 0, int.MaxValue);
                if (m_VerticalPivotType == VerticalPivotType.Center)
                {
                    tempVector2.y -= m_ExpandedSize.y / 2;
                    tempVector2.y += itemHeight / 2;
                    tempVector2.y += 8;
                }
                else if (m_VerticalPivotType == VerticalPivotType.LastItem)
                {
                    tempVector2.y -= m_ExpandedSize.y;
                    tempVector2.y += itemHeight;
                    tempVector2.y += 16;
                }
                tempVector2.y = Mathf.Clamp(tempVector2.y, minScrollPos, maxScrollPos);
                m_PanelLayer.anchoredPosition = tempVector2;

                m_ScrollPosOffset = tempVector2.y;
            }
            else
            {
                m_ScrollPosOffset = 0;
            }

            flipper = m_HorizontalPivotType == HorizontalPivotType.Left ? 1 : -1;

            if (m_HorizontalPivotType != HorizontalPivotType.Center)
            {
                position.x -= m_BaseTransform.GetProperSize().x * scaler.canvas.transform.localScale.x / 2 * flipper;
                position.x += m_ExpandedSize.x * scaler.canvas.transform.localScale.x / 2 * flipper;
            }

            RectTransform rootCanvasRectTransform = MaterialUIScaler.GetRootScaler(m_DropdownPanel).GetComponent<RectTransform>();

            //  Left edge
            float canvasEdge = rootCanvasRectTransform.position.x / scaler.canvas.transform.localScale.x - rootCanvasRectTransform.rect.width / 2;
            float dropdownEdge = position.x / scaler.canvas.transform.localScale.x - m_ExpandedSize.x / 2;
            if (dropdownEdge < canvasEdge + m_MinDistanceFromEdge)
            {
                position.x = (canvasEdge + (m_MinDistanceFromEdge + m_ExpandedSize.x / 2)) * scaler.canvas.transform.localScale.x;
                //position.x += (canvasEdge + m_MinDistanceFromEdge - dropdownEdge) * scaler.canvas.transform.localScale.x;
            }

            //  Right edge
            canvasEdge = rootCanvasRectTransform.position.x / scaler.canvas.transform.localScale.x + rootCanvasRectTransform.rect.width / 2;
            dropdownEdge = position.x / scaler.canvas.transform.localScale.x + m_ExpandedSize.x / 2;
            if (dropdownEdge > canvasEdge - m_MinDistanceFromEdge)
            {
                position.x = (canvasEdge - (m_MinDistanceFromEdge + m_ExpandedSize.x / 2)) * scaler.canvas.transform.localScale.x;
                //position.x -= (dropdownEdge - (canvasEdge - m_MinDistanceFromEdge)) * scaler.canvas.transform.localScale.x;
            }

            //  Top edge
            canvasEdge = rootCanvasRectTransform.position.y / scaler.canvas.transform.localScale.x + rootCanvasRectTransform.rect.height / 2;
            dropdownEdge = position.y / scaler.canvas.transform.localScale.x + m_ExpandedSize.y / 2;
            if (dropdownEdge > canvasEdge - m_MinDistanceFromEdge)
            {
                position.y = (canvasEdge - (m_MinDistanceFromEdge + m_ExpandedSize.y / 2)) * scaler.canvas.transform.localScale.y;
                //position.y -= (dropdownEdge - (canvasEdge - m_MinDistanceFromEdge)) * scaler.canvas.transform.localScale.x;
            }

            //  Bottom edge
            canvasEdge = rootCanvasRectTransform.position.y / scaler.canvas.transform.localScale.x - rootCanvasRectTransform.rect.height / 2;
            dropdownEdge = position.y / scaler.canvas.transform.localScale.x - m_ExpandedSize.y / 2;
            if (dropdownEdge < canvasEdge + m_MinDistanceFromEdge)
            {
                position.y = (canvasEdge + (m_MinDistanceFromEdge + m_ExpandedSize.y / 2)) * scaler.canvas.transform.localScale.y;
                //position.y += ((canvasEdge + m_MinDistanceFromEdge) - dropdownEdge) * scaler.canvas.transform.localScale.x;
            }

            return position;
        }

        private DropdownListItem CreateItem(OptionData data, int index)
        {
            DropdownListItem item = Instantiate(m_ListItemTemplate);

            item.rectTransform.SetParent(m_ListItemTemplate.rectTransform.parent);
            item.rectTransform.localScale = Vector3.one;
            item.rectTransform.localEulerAngles = Vector3.zero;
            item.rectTransform.anchoredPosition3D = Vector3.zero;

            if (item.canvasGroup == null)
                item.canvasGroup = item.rectTransform.GetComponent<CanvasGroup>();
            if(item.text == null)
                item.text = item.rectTransform.GetChildByName<Text>("Text");

            if (m_OptionDataList.imageType == ImageDataType.Sprite)
            {
                if (item.image == null)
                    item.image = item.rectTransform.GetChildByName<Image>("Icon");

                if (item.image != null)
                    item.image.gameObject.SetActive(true);
                if(item.vectorImage as Graphic != null)
                    (item.vectorImage as Graphic).gameObject.SetActive(false);
            }
            else
            {
                if (item.vectorImage as Graphic == null)
                    item.vectorImage = item.rectTransform.GetChildByName<IVectorImage>("VectorIcon");

                if (item.vectorImage as Graphic != null)
                    item.vectorImage.gameObject.SetActive(true);
                if (item.image != null)
                    item.image.gameObject.SetActive(false);
            }

            DropdownTrigger trigger = item.GetComponent<DropdownTrigger>();
            trigger.index = index;
            trigger.dropdown = this;

            if (!string.IsNullOrEmpty(data.text))
            {
                item.text.SetGraphicText(data.text);
            }
            else
            {
                item.text.gameObject.SetActive(false);
            }

            if (data.imageData != null && data.imageData.ContainsData(true))
            {
                if (data.imageData.imageDataType == ImageDataType.Sprite)
                    item.image.SetImage(data.imageData);
                else
                    (item.vectorImage as Graphic).SetImage(data.imageData);
            }
            else
            {
                (item.vectorImage as Graphic).gameObject.SetActive(false);
                item.image.gameObject.SetActive(false);
            }

            item.GetComponent<MaterialRipple>().rippleData = m_ItemRippleData.Copy();

            if (m_HighlightCurrentlySelected && index == m_CurrentlySelected)
            {
                item.GetComponent<Image>().color = m_ItemRippleData.Color.WithAlpha(m_ItemRippleData.EndAlpha);
            }

            item.text.color = m_ItemTextColor;
            item.image.color = m_ItemIconColor;
            (item.vectorImage as Graphic).color = m_ItemIconColor;

            item.canvasGroup.alpha = 0f;

            item.gameObject.SetActive(true);

            return item;
        }

        private float CalculateMaxItemWidth()
        {
            float maxWidth = 0f;
            ILayoutElement layoutElement = m_ListItemTemplate.text as ILayoutElement;

            for (int i = 0; i < m_OptionDataList.options.Count; i++)
            {
                float currentWidth = 0f;
                if (!string.IsNullOrEmpty(m_OptionDataList.options[i].text))
                {
                    if (m_ListItemTemplate.text != null)
                    {
                        m_ListItemTemplate.text.SetGraphicText(m_OptionDataList.options[i].text);
                        if (layoutElement != null)
                        {
                            layoutElement.CalculateLayoutInputHorizontal();
                            layoutElement.CalculateLayoutInputVertical();
                        }

                        currentWidth = layoutElement != null && !m_ListItemTemplate.gameObject.activeSelf? layoutElement.preferredWidth : LayoutUtility.GetPreferredWidth(m_ListItemTemplate.text.rectTransform);// textGenerator.GetPreferredWidth(m_OptionDataList.options[i].text, textGenerationSettings);
                        currentWidth += 16;
                    }
                }

                if (m_OptionDataList.imageType == ImageDataType.Sprite)
                {
                    if (m_ListItemTemplate.image != null && m_OptionDataList.options[i].imageData.sprite != null)
                    {
                        if (currentWidth > 0)
                            currentWidth += 16;
                        currentWidth += m_ListItemTemplate.image.rectTransform.rect.width;
                    }
                }
                else
                {
                    if (m_ListItemTemplate.vectorImage != null && m_OptionDataList.options[i].imageData != null && m_OptionDataList.options[i].imageData.vectorImageData != null)
                    {
                        if (currentWidth > 0)
                            currentWidth += 16;
                        currentWidth += m_ListItemTemplate.vectorImage.rectTransform.rect.width;
                    }
                }

                currentWidth += 16;

                maxWidth = Mathf.Max(maxWidth, currentWidth);
            }

            return Mathf.Max(LayoutUtility.GetPreferredWidth(m_ListItemTemplate.rectTransform), maxWidth);
        }

        #endregion
    }
}