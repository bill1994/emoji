using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public abstract class MaterialActivity : UIBehaviour
    {
        #region Private Variables

        [SerializeField]
        protected bool m_UseFocusGroup = true;

        [Space]
        [SerializeField]
        protected bool m_CreateWithCanvas = true;

        [Space]
        [SerializeField]
        protected bool m_DestroyOnHide = true;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_changeSibling")]
        protected bool m_ChangeSibling = true;

        protected CanvasGroup _CanvasGroup;

        #endregion

        #region Callbacks

        [UnityEngine.Serialization.FormerlySerializedAs("m_OnScreenEndTransitionIn")]
        public UnityEvent OnShowAnimationOver = new UnityEvent();
        [UnityEngine.Serialization.FormerlySerializedAs("m_OnScreenEndTransitionOut")]
        public UnityEvent OnHideAnimationOver = new UnityEvent();

        #endregion

        #region Public Properties

        public virtual bool createWithCanvas
        {
            get { return m_CreateWithCanvas; }
            set { m_CreateWithCanvas = value; }
        }

        public virtual bool useFocusGroup
        {
            get { return m_UseFocusGroup; }
            set { m_UseFocusGroup = value; }
        }

        public virtual MaterialFocusGroup focusGroup
        {
            get { return GetComponent<MaterialFocusGroup>(); }
        }

        public virtual RectTransform rectTransform
        {
            get
            {
                return transform as RectTransform;
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (_CanvasGroup == null)
                {
                    _CanvasGroup = gameObject.GetAddComponent<CanvasGroup>();
                    _CanvasGroup.blocksRaycasts = true;
                    _CanvasGroup.interactable = true;
                    //m_CanvasGroup.ignoreParentGroups = true;
                }
                return _CanvasGroup;
            }
        }

        public virtual bool destroyOnHide
        {
            get { return m_DestroyOnHide; }
            set { m_DestroyOnHide = value; }
        }

        public virtual bool changeSibling
        {
            get { return m_ChangeSibling; }
            set { m_ChangeSibling = value; }
        }

        #endregion

        #region Public Functions

        public virtual void Build(Canvas parentCanvas = null)
        {
            /*Transform parentTransform = null;
            //Find RootCanvas
            if (parentCanvas == null || parentCanvas.transform.root == this.transform)
            {
                parentCanvas = FindObjectOfType<Canvas>();
                if (parentCanvas != null && parentCanvas.transform.root == this.transform)
                {
                    parentCanvas = null;
                    var allCanvas = FindObjectsOfType<Canvas>();
                    foreach (var canvasComponent in allCanvas)
                    {
                        if (canvasComponent != null && canvasComponent.transform.root != this.transform)
                        {
                            parentCanvas = canvasComponent;
                            break;
                        }
                    }
                }
            }

            //Set Transform
            if (parentCanvas != null)
            {
                parentCanvas = parentCanvas.transform.GetRootCanvas();
                CanvasSafeArea safeArea = parentCanvas.GetComponent<CanvasSafeArea>();
                parentTransform = safeArea != null && safeArea.Content != null ? safeArea.Content : parentCanvas.transform;
            }
            
            Build(parentTransform);*/

            //Set Transform
            Transform parentTransform = null;
            if (parentCanvas != null)
            {
                CanvasSafeArea safeArea = parentCanvas.GetComponent<CanvasSafeArea>();
                parentTransform = safeArea != null && safeArea.Content != null ? safeArea.Content : parentCanvas.transform;
            }

            Build(parentTransform);
        }

        public virtual void Build(Transform parent)
        {
            transform.SetParent(null);
            InitializeFocusGroup();

            SetCanvasActive(false);

            transform.SetParent(parent, false);
            Inflate(this.rectTransform, true);

            TryCreateCanvas(false);
            SetCanvasActive(false);
        }

        public abstract void Show();

        public abstract void Hide();

        #endregion

        #region Helper Functions

        protected virtual void SetCanvasActive(bool isActive)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = isActive;
            var graphicRaycaster = GetComponent<GraphicRaycaster>();
            if (graphicRaycaster != null)
                graphicRaycaster.enabled = isActive;
        }

        protected virtual bool TryCreateCanvas(bool force)
        {
            if (m_CreateWithCanvas || force)
            {
                var canvas = GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = gameObject.AddComponent<Canvas>();
                    var graphicRaycaster = GetComponent<GraphicRaycaster>();
                    if (graphicRaycaster == null)
                        graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
                    return true;
                }
            }
            return false;
        }

        protected virtual void InitializeFocusGroup()
        {
            var materialKeyFocus = GetComponent<MaterialFocusGroup>();
            if (m_UseFocusGroup && materialKeyFocus == null)
            {
                materialKeyFocus = gameObject.AddComponent<MaterialFocusGroup>();

                ValidateKeyTriggers(materialKeyFocus);
            }

            if (materialKeyFocus != null)
                materialKeyFocus.enabled = m_UseFocusGroup;
        }

        protected virtual void ValidateKeyTriggers(MaterialFocusGroup materialKeyFocus)
        {
            if (materialKeyFocus != null)
            {
                var cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                cancelTrigger.Name = "Escape KeyDown";
                cancelTrigger.Key = KeyCode.Escape;
                cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                AddEventListener(cancelTrigger.OnCallTrigger, Hide);

                materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { cancelTrigger };
            }
        }

        #endregion

        #region Static Functions

        public static void Inflate(RectTransform target, bool forceIgnoreLayout)
        {
            if (target == null || target.parent == null)
                return;

            target.localScale = Vector3.one;
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = Vector2.zero;
            target.anchoredPosition = Vector2.zero;
            target.localRotation = Quaternion.identity;

            var layoutElement = target.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = target.gameObject.AddComponent<LayoutElement>();

            if (layoutElement != null)
            {
                if (forceIgnoreLayout)
                    layoutElement.ignoreLayout = true;
                layoutElement.flexibleWidth = 1;
                layoutElement.flexibleHeight = 1;
            }
        }

        public static void AddEventListener(UnityEvent eventAction, UnityAction action)
        {
            if (eventAction != null)
            {
#if UNITY_EDITOR
                UnityEditor.Events.UnityEventTools.AddPersistentListener(eventAction, action);
#else
                eventAction.AddListener(action);
#endif
            }
        }

        public static void AddEventListener<T>(UnityEvent<T> eventAction, UnityAction<T> action)
        {
            if (eventAction != null)
            {
#if UNITY_EDITOR
                UnityEditor.Events.UnityEventTools.AddPersistentListener<T>(eventAction, action);
#else
                eventAction.AddListener(action);
#endif
            }
        }

        public static void RemoveEventListener(UnityEvent eventAction, UnityAction action)
        {
            if (eventAction != null)
            {
#if UNITY_EDITOR
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(eventAction, action);
#else
                eventAction.RemoveListener(action);
#endif
        }
    }

        public static void RemoveEventListener<T>(UnityEvent<T> eventAction, UnityAction<T> action)
        {
            if (eventAction != null)
            {
#if UNITY_EDITOR
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(eventAction, action);
#else
                eventAction.RemoveListener(action);
#endif
        }
    }

        #endregion
    }
}
