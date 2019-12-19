using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace MaterialUI
{
    public class CanvasSafeArea : UnityEngine.EventSystems.UIBehaviour
    {
        #region Private Variables

        [SerializeField] RectTransform m_Content = null;
        [SerializeField] RectTransform m_UnsafeContent = null;
        [Space]
        [SerializeField] bool m_ConformX = true;  // Conform to screen safe area on X-axis (default true, disable to ignore)
        [SerializeField] bool m_ConformY = true;  // Conform to screen safe area on Y-axis (default true, disable to ignore)
        [Space]
        [SerializeField] bool m_forceClip = true;
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

            if(Content != null)
                ResetRectTransform(m_Content, false);
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
                Refresh();
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying && this.isActiveAndEnabled)
                Refresh();
        }

        #endregion

        #region Helper Functions

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
                ApplySafeArea(safeArea);
                Theme.ApplyStatusBarTheme();
            }
        }

        protected virtual Rect GetSafeArea()
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
                else if (IsStatusBarActive() && Screen.safeArea == nsa)
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
            //Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}", name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
        }

        protected virtual void ResetRectTransform(RectTransform rect, bool applyAnchorMinMax = true)
        {
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localEulerAngles = Vector3.zero;
                if (applyAnchorMinMax)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                }
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
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

        #region Interop Static Services

#if UNITY_IPHONE
        [DllImport("__Internal")]
        static extern bool _IsIOSStatusBarActive();
        [DllImport("__Internal")]
        static extern float _GetStatusBarHeight();
#endif

        public static bool IsStatusBarActive()
        {
#if UNITY_IPHONE && !UNITY_EDITOR
		    return _IsIOSStatusBarActive();
#else
            return false;
#endif
        }

        public static float GetStatusBarHeight()
        {
#if UNITY_IPHONE && !UNITY_EDITOR
		    return _GetStatusBarHeight();
#else
            return 0;
#endif
        }

        #endregion
    }
}
