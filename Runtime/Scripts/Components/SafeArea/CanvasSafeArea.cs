using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine.Events;

namespace MaterialUI
{
    public class CanvasSafeArea : UnityEngine.EventSystems.UIBehaviour
    {
        [System.Serializable]
        public class RectUnityEvent : UnityEvent<Rect> { }

        #region Private Variables

        [SerializeField] RectTransform m_Content = null;
        [SerializeField] RectTransform m_UnsafeContent = null;
        [Space]
        [SerializeField] bool m_ConformX = true;  // Conform to screen safe area on X-axis (default true, disable to ignore)
        [SerializeField] bool m_ConformY = true;  // Conform to screen safe area on Y-axis (default true, disable to ignore)
        [Space]
        [SerializeField] bool m_forceClip = true;
        [Space]
        [SerializeField] bool m_AutoReparentDirectChildren = true;
        [Space]
        [SerializeField] bool m_AutoCreateUnsafeContent = false;
        //[SerializeField] Color m_AutoCreateColor = Color.white;
        [Space]
        [SerializeField] SafeAreaStyleAsset m_theme = null;


        Rect _LastSafeArea = new Rect(0, 0, 0, 0);

#if UNITY_EDITOR
        Image _simulatorSpriteContent = null;
#endif

        #endregion

        #region Callbacks

        public RectUnityEvent OnApplySafeArea = new RectUnityEvent();

        #endregion

        #region Public Properties

        public RectTransform Content
        {
            get
            {
                return m_Content;
            }
        }

        public RectTransform UnsafeContent
        {
            get
            {
                return m_UnsafeContent;
            }
        }

        public SafeAreaStyleAsset Theme
        {
            get
            {
                if (m_theme == null)
                {
                    m_theme = ScriptableObject.CreateInstance<SafeAreaStyleAsset>();
                    m_theme.Enabled = false;
                }
                return m_theme;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            TryInstantiateContent();
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying)
                return;

            if (m_Content == null)
            {
                Debug.LogError("Cannot apply safe area - no Content RectTransform found on " + name);
                Destroy(this);
            }
            else
            {
                if (m_forceClip)
                    Content.gameObject.GetAddComponent<RectMask2D>();
                var layoutElement = Content.gameObject.GetAddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                ResetRectTransform(m_Content, false);
                RefreshContentChildren();

                if (m_UnsafeContent == null && m_AutoCreateUnsafeContent)
                {
                    m_UnsafeContent = new GameObject("UnsafeAreaContent").AddComponent<RectTransform>();
                    m_UnsafeContent.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                    m_UnsafeContent.SetParent(Content.parent);
                    //m_UnsafeContent.gameObject.GetAddComponent<RectMask2D>();
                    m_UnsafeContent.transform.SetSiblingIndex(Content.transform.GetSiblingIndex());

                    var unsafeLayoutElement = m_UnsafeContent.gameObject.GetAddComponent<LayoutElement>();
                    unsafeLayoutElement.ignoreLayout = true;

                    if (Theme.Enabled)
                    {
                        var unsafeImage = m_UnsafeContent.gameObject.GetAddComponent<Image>();
                        unsafeImage.raycastTarget = false;
                        unsafeImage.color = Theme.UnsafeContentColor;
                    }
                }

#if UNITY_EDITOR
                EditorSafeAreaSimulator.RegisterSafeAreaComponent(this);
                SetupSimulatorMaskContent();
#endif

                ResetRectTransform(m_UnsafeContent, true);

                Refresh();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CancelInvoke();

#if UNITY_EDITOR
            EditorSafeAreaSimulator.UnregisterSafeAreaComponent(this);
#endif
        }

        protected override void OnCanvasHierarchyChanged()
        {
            if (Application.isPlaying && this.isActiveAndEnabled)
                Refresh();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (Application.isPlaying && this.isActiveAndEnabled)
            {
                Invoke("Refresh", 0);
            }
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying && this.isActiveAndEnabled)
                Refresh();
        }

        protected virtual void OnTransformChildrenChanged()
        {
            if(Application.isPlaying)
                RefreshContentChildrenDelayed();
        }

        #endregion

        #region Helper Functions

        public virtual void RefreshContentChildrenDelayed()
        {
            if (!_isExecutingRefreshContentChildren)
            {
                CancelInvoke("RefreshContentChildren");

                if(m_AutoReparentDirectChildren)
                    Invoke("RefreshContentChildren", 0);
            }
        }

        bool _isExecutingRefreshContentChildren = false;
        public virtual void RefreshContentChildren()
        {
            CancelInvoke("RefreshContentChildren");

            _isExecutingRefreshContentChildren = true;
            List<Transform> childrenToMove = null;
            if (m_Content != null && m_AutoReparentDirectChildren)
            {
                childrenToMove = new List<Transform>();
                //Find Non-Content Children
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child != null && child != m_Content && child != m_UnsafeContent)
                        childrenToMove.Add(child);
                }

                foreach (Transform child in childrenToMove)
                {
                    RectTransform rectChild = child as RectTransform;

                    //Save transform informations
                    var localRotation = child.localRotation;
                    var localScale = child.localScale;

                    var sizeDelta = rectChild != null? rectChild.sizeDelta : Vector2.zero;
                    var pivot = rectChild != null? rectChild.pivot : Vector2.zero;
                    var anchoredPosition3D = rectChild != null ? rectChild.anchoredPosition3D : child.localPosition;
                    var anchorMin = rectChild != null? rectChild.anchorMin : Vector2.zero;
                    var anchorMax = rectChild != null? rectChild.anchorMax : Vector2.zero;

                    child.SetParent(m_Content);

                    //Apply transform informations
                    child.localRotation = localRotation;
                    child.localScale = localScale;
                    if (rectChild != null)
                    {
                        rectChild.sizeDelta = sizeDelta;
                        rectChild.pivot = pivot;
                        rectChild.anchoredPosition3D = anchoredPosition3D;
                        rectChild.anchorMin = anchorMin;
                        rectChild.anchorMax = anchorMax;
                    }
                    else
                        rectChild.localPosition = anchoredPosition3D;
                }
            }
            _isExecutingRefreshContentChildren = false;
        }

        protected bool IsRootSafeArea()
        {
            if (transform.parent != null)
                return transform.parent.GetComponentInParent<CanvasSafeArea>() == null;
            return true;
        }

        protected internal virtual void Refresh()
        {
            Refresh(false);
        }

        protected virtual void Refresh(bool force)
        {
            Rect safeArea = GetSafeArea();

            if (safeArea != _LastSafeArea || force)
            {
                Theme.ApplyStatusBarTheme();
                safeArea = GetSafeArea(); //useful when Theme change safe area size (when statusbar is hidden, for example)
                ApplySafeArea(safeArea);
            }
        }

        public Rect GetConformSafeArea()
        {
            var safeArea = GetSafeArea();

            // Ignore x-axis?
            if (!m_ConformX)
            {
                safeArea.x = 0;
                safeArea.width = Screen.width;
            }

            // Ignore y-axis?
            if (!m_ConformY)
            {
                safeArea.y = 0;
                safeArea.height = Screen.height;
            }

            return safeArea;
        }

        public virtual Rect GetSafeArea()
        {
            Rect nsa = new Rect(0, 0, Screen.width, Screen.height);
            if (!IsRootSafeArea())
            {
                Debug.Log("IsRootSafeArea: " + name);
                return nsa;
            }
            else
            {
                Rect safeArea = Screen.safeArea;
                if (Application.isEditor)
                {
#if UNITY_EDITOR
                    nsa = EditorSafeAreaSimulator.GetNormalizedSafeArea();
                    safeArea = new Rect(Screen.width * nsa.x, Screen.height * nsa.y, Screen.width * nsa.width, Screen.height * nsa.height);
#endif
                }
                else if (IsStatusBarActiveWithLayoutStable() && Screen.safeArea == nsa)
                {
                    //This is the size of statusbar in IOS without notch
                    safeArea.height = Mathf.Max(0, safeArea.height - GetStatusBarHeight());
                }


                return safeArea;
            }
        }

        protected virtual void ApplySafeArea(Rect r)
        {
            _LastSafeArea = r;

            // Ignore x-axis?
            if (!m_ConformX)
            {
                r.x = 0;
                r.width = Screen.width;
            }

            // Ignore y-axis?
            if (!m_ConformY)
            {
                r.y = 0;
                r.height = Screen.height;
            }

            // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            m_Content.anchorMin = anchorMin;
            m_Content.anchorMax = anchorMax;

#if UNITY_EDITOR
            SetupSimulatorMaskContent();
#endif

            Kyub.Performance.SustainedPerformanceManager.Invalidate();

            if (OnApplySafeArea != null)
                OnApplySafeArea.Invoke(r);
            //Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}", name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
        }

        protected virtual void ResetRectTransform(RectTransform rect, bool inflate = true)
        {
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localEulerAngles = Vector3.zero;
                if (inflate)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                }
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        protected virtual RectTransform TryInstantiateContent()
        {
            if ((m_AutoReparentDirectChildren && m_Content == null) ||
                    !m_Content.gameObject.scene.IsValid() ||
                    !m_Content.IsChildOf(this.transform))
            {
                if (m_Content == null)
                {
                    m_Content = InstantiateContentFromRootModel();
                }
                else
                {
                    var clonedInstance = GameObject.Instantiate(m_Content);
                    clonedInstance.name = m_Content.name;
                    //Disable component if is scene member
                    if(m_Content.gameObject.scene.IsValid())
                        m_Content.gameObject.SetActive(false);
                    m_Content = clonedInstance;
                }
            }

            if (m_Content != null)
            {
                //Reparent
                if (!m_Content.IsChildOf(this.transform))
                    m_Content.SetParent(this.transform);
                ResetRectTransform(m_Content, false);
            }

            return m_Content;
        }

#if UNITY_EDITOR
        protected virtual void SetupSimulatorMaskContent()
        {
            var spriteToApply = EditorSafeAreaSimulator.GetOrLoadSimulatorSprite();
            if (Application.isPlaying && spriteToApply != null && m_UnsafeContent != null)
            {
                if (_simulatorSpriteContent == null)
                {
                    _simulatorSpriteContent = new GameObject("SimulatorMaskContent").AddComponent<Image>();
                    _simulatorSpriteContent.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                    _simulatorSpriteContent.transform.SetParent(Content.parent);
                    _simulatorSpriteContent.transform.SetSiblingIndex(Content.transform.GetSiblingIndex() + 1);
                    _simulatorSpriteContent.type = Image.Type.Sliced;
                    
                    var layoutElement = _simulatorSpriteContent.gameObject.GetAddComponent<LayoutElement>();
                    layoutElement.ignoreLayout = true;
                }
                if (_simulatorSpriteContent != null)
                {
                    _simulatorSpriteContent.raycastTarget = false;
                    _simulatorSpriteContent.sprite = spriteToApply;
                    _simulatorSpriteContent.gameObject.SetActive(spriteToApply != null);
                    if (_simulatorSpriteContent.gameObject.activeSelf)
                        ResetRectTransform(_simulatorSpriteContent.rectTransform);
                }
            }
            else if (_simulatorSpriteContent != null)
            {
                if(Application.isPlaying)
                    GameObject.Destroy(_simulatorSpriteContent.gameObject);
                else
                    GameObject.DestroyImmediate(_simulatorSpriteContent.gameObject);
            }
        }

#endif

        #endregion

        #region Instantiate Internal Functions

        /// <summary>
        /// Create content trying to copy layout and image properties from root
        /// </summary>
        /// <returns></returns>
        protected virtual RectTransform InstantiateContentFromRootModel()
        {
            var content = new GameObject("Content").GetAddComponent<RectTransform>();

            //Try Clone LayoutGroup
            AddBehaviourClone(GetComponent<LayoutGroup>(), content.gameObject, true);
            //Try Clone Background Graphic
            AddBehaviourClone(GetComponent<Graphic>(), content.gameObject, true);

            return content;
        }

        protected virtual Behaviour AddBehaviourClone(Behaviour template, GameObject target, bool disableTemplate)
        {
            if (template != null && target != null)
            {
                var cloneComponent = target.AddComponent(template.GetType()) as Behaviour;
                if (cloneComponent != null)
                {
                    cloneComponent.enabled = false;
                    var templateJson = JsonUtility.ToJson(template);
                    //Apply Json Properties
                    JsonUtility.FromJsonOverwrite(templateJson, cloneComponent);
                    cloneComponent.enabled = template.enabled;
                    if (disableTemplate)
                        template.enabled = false;

                    return cloneComponent;
                }

            }
            return null;
        }

        #endregion

        #region Interop Static Services

#if UNITY_IPHONE
        [DllImport("__Internal")]
        static extern bool _IsIOSStatusBarActive();
        [DllImport("__Internal")]
        static extern float _GetStatusBarHeight();
#endif

        public static bool IsStatusBarActiveWithLayoutStable()
        {
#if UNITY_IPHONE && !UNITY_EDITOR
		    return _IsIOSStatusBarActive();
#elif UNITY_ANDROID && !UNITY_EDITOR
            var activity = AndroidThemeNativeUtils.GetActivity();
            var window = AndroidThemeNativeUtils.GetWindow(activity);

            return !Screen.fullScreen && AndroidThemeNativeUtils.IsStatusBarActive(window) && AndroidThemeNativeUtils.IsViewBehindBars(window);
#else
            return false;
#endif
        }

        public static float GetStatusBarHeight()
        {
#if UNITY_IPHONE && !UNITY_EDITOR
		    return _GetStatusBarHeight();
#elif UNITY_ANDROID && UNITY_2019_1_OR_NEWER && !UNITY_EDITOR
            var activity = AndroidThemeNativeUtils.GetActivity();
            return AndroidThemeNativeUtils.GetStatusBarHeight(activity);
#else
            return 0;
#endif
        }

#endregion
    }
}
