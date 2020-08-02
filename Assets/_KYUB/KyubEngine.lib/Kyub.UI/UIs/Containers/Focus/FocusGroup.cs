using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Kyub;
using Kyub.Extensions;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class FocusGroup : MonoBehaviour
    {
        #region Static Properties

        static List<FocusGroup> _focusOrder = new List<FocusGroup>();
        protected static List<FocusGroup> FocusOrder
        {
            get
            {
                if (_focusOrder == null)
                    _focusOrder = new List<FocusGroup>();
                return _focusOrder;
            }
            set
            {
                if (_focusOrder == value)
                    return;
                _focusOrder = value;
            }
        }

        #endregion

        #region Private Variables

        [SerializeField]
        bool m_fullScreenRendering = false;
        [SerializeField]
        bool m_useCanvasRaycasterOptimization = false;

        #endregion

        #region Callbacks

        public UnityEvent OnGainFocusCallback;
        public UnityEvent OnLoseFocusCallback;

        #endregion

        #region Public Properties

        public bool FullScreenRendering
        {
            get
            {
                return m_fullScreenRendering;
            }
            set
            {
                if (m_fullScreenRendering == value)
                    return;
                m_fullScreenRendering = value;
                CheckCanvasAndGraphicsRaycaster();
            }
        }

        public bool UseCanvasRaycasterOptimization
        {
            get
            {
                return m_useCanvasRaycasterOptimization;
            }
            set
            {
                if (m_useCanvasRaycasterOptimization == value)
                    return;
                m_useCanvasRaycasterOptimization = value;
                CheckCanvasAndGraphicsRaycaster();
            }
        }

        #endregion

        #region Unity Functions

        protected bool _started = false;
        protected virtual void Start()
        {
            _started = true;
            CheckCanvasAndGraphicsRaycaster();
            if (Application.isPlaying && enabled && gameObject.activeSelf && gameObject.activeInHierarchy)
            {
                CheckFocus(TweenContainerUtils.GetContainerVisibilityInHierarchy(this.gameObject), false);
            }
        }

        protected virtual void OnEnable()
        {
            if (Application.isPlaying && enabled && gameObject.activeSelf && gameObject.activeInHierarchy)
            {
                RegisterEvents();
                if (_started)
                    CheckFocus(TweenContainerUtils.GetContainerVisibilityInHierarchy(this.gameObject));
            }
            else if (!Application.isPlaying)
            {
                CheckCanvasAndGraphicsRaycaster();
            }
        }

        protected virtual void OnDisable()
        {
            if (Application.isPlaying)
            {
                UnregisterEvents();
                CheckFocus(PanelStateEnum.Closed);
            }
            else
                CheckCanvasAndGraphicsRaycaster();
        }

        protected virtual void Update()
        {
            if (Application.isPlaying)
            {
                if (_markToCheckCanvasAndGraphicsRaycaster)
                    CheckCanvasAndGraphicsRaycaster();
            }
        }

        #endregion

        #region Events Receivers

        protected virtual void OnPanelStateChanged(PanelStateEnum p_panelState)
        {
            if ((!enabled || !gameObject.activeSelf || !gameObject.activeInHierarchy) && (p_panelState == PanelStateEnum.Opening || p_panelState == PanelStateEnum.Opened))
            {
                p_panelState = PanelStateEnum.Closing;
            }
            if (p_panelState == PanelStateEnum.Opening || p_panelState == PanelStateEnum.Opened || p_panelState == PanelStateEnum.Closing)
            {
                CheckFocus(p_panelState);
            }
            else
            {
                ApplicationContext.RunOnMainThread(() => {
                    if (this != null)
                        CheckFocus(p_panelState); 
                }, 0.1f);
            }
        }

        /*protected virtual void HandleOnGlobalPress(bool p_pressed)
        {
            if (KyubUICamera.currentTouch != null && p_pressed)
            {
                FocusGroup v_directParentFocus = GetDirectFocusGroupComponent(KyubUICamera.currentTouch.pressed);
                if (v_directParentFocus == this && enabled && gameObject.activeSelf && gameObject.activeInHierarchy)
                    FocusGroup.SetFocus(this);
            }
        }*/

        #endregion

        #region Helper Functions

        protected bool _markToCheckCanvasAndGraphicsRaycaster = false;
        protected virtual void MarkToCheckCanvasAndGraphicsRaycaster()
        {
            _markToCheckCanvasAndGraphicsRaycaster = true;
        }

        protected virtual void CheckCanvasAndGraphicsRaycaster()
        {
            if (!Application.isPlaying)
                TryCreateCanvasAndGraphics();
            else
            {
                if (_started)
                {
                    _markToCheckCanvasAndGraphicsRaycaster = false;
                    TryCreateCanvasAndGraphics();
                    CanvasGroup v_canvas = GetComponent<CanvasGroup>();
                    if (FullScreenRendering)
                    {
                        if (v_canvas == null)
                            v_canvas = gameObject.AddComponent<CanvasGroup>();
                    }
                }
                else
                {
                    MarkToCheckCanvasAndGraphicsRaycaster();
                }
            }
        }

        protected virtual void TryCreateCanvasAndGraphics()
        {
            Canvas v_canvasOptimizer = GetComponent<Canvas>();
            UnityEngine.UI.GraphicRaycaster v_graphicsRaycasting = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (UseCanvasRaycasterOptimization && FullScreenRendering)
            {
                if (FullScreenRendering)
                {
                    if (v_canvasOptimizer == null)
                        v_canvasOptimizer = gameObject.AddComponent<Canvas>();
                    if (v_graphicsRaycasting == null)
                        v_graphicsRaycasting = gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }
            else
            {
                if (v_graphicsRaycasting != null)
                    DestroyUtils.Destroy(v_canvasOptimizer);
                if (v_canvasOptimizer != null)
                    DestroyUtils.DestroyImmediate(v_graphicsRaycasting);
            }
        }

        protected void CheckFocus(PanelStateEnum p_panelState)
        {
            CheckFocus(p_panelState, true);
        }

        protected virtual void CheckFocus(PanelStateEnum p_panelState, bool p_ignoreChildrenFocus)
        {
            if (p_panelState == PanelStateEnum.Opening || p_panelState == PanelStateEnum.Opened)
            {
                FocusGroup v_currentFocus = FocusGroup.GetFocus();
                bool v_canIgnoreCurrentFocus = p_ignoreChildrenFocus || v_currentFocus == null || !TweenContainerUtils.IsChildObject(this.gameObject, v_currentFocus.gameObject, false);
                if (v_canIgnoreCurrentFocus)
                {
                    FocusGroup.SetFocus(this);
                }
                else
                {
                    //Find index to add self to Focus (Index after your last children)
                    int v_indexToAddThis = 0;
                    FocusGroup.FocusOrder.RemoveChecking(this);
                    for (int i = 0; i < FocusGroup.FocusOrder.Count; i++)
                    {
                        FocusGroup v_container = FocusGroup.FocusOrder[i];
                        bool v_isChildrenContainer = v_container != null && TweenContainerUtils.IsChildObject(this.gameObject, v_container.gameObject, false);
                        if (v_isChildrenContainer)
                            v_indexToAddThis = i + 1;
                    }
                    FocusGroup.FocusOrder.Insert(v_indexToAddThis, this);
                }
                if (FullScreenRendering)
                    FocusGroup.CorrectFullScreenRendering(p_panelState == PanelStateEnum.Opening);
            }
            else if (p_panelState == PanelStateEnum.Closing || p_panelState == PanelStateEnum.Closed)
            {
                if (FullScreenRendering)
                    FocusGroup.CorrectFullScreenRendering(p_panelState == PanelStateEnum.Closing);
                FocusGroup.RemoveFocus(this);

                if (p_panelState == PanelStateEnum.Closed)
                {
                    //set Renderer To False After Remove
                    SetCanvasEnabled(false);
                }
            }
        }

        protected virtual void SetCanvasEnabled(bool p_enabled, bool p_immediate = true)
        {
            if (isActiveAndEnabled && FullScreenRendering)
            {
                if (p_immediate)
                {
                    CanvasGroup v_canvas = GetComponent<CanvasGroup>();
                    if (v_canvas != null)
                    {
                        bool v_isCanvasEnabled = v_canvas.alpha > 0;
                        if (v_isCanvasEnabled != p_enabled)
                        {
                            v_canvas.alpha = p_enabled ? 1 : 0;
                            v_canvas.blocksRaycasts = p_enabled;
                        }
                    }
                }
                else
                {
                    StopCoroutine("RequestSetCanvasEnabled");
                    StartCoroutine("RequestSetCanvasEnabled", p_enabled);
                }
            }
        }

        protected virtual IEnumerator RequestSetCanvasEnabled(bool p_enabled)
        {
            yield return null; //Wait One Cycle
            SetCanvasEnabled(p_enabled, true);
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            //GlobalPressController.OnGlobalPress += HandleOnGlobalPress;
        }

        protected virtual void UnregisterEvents()
        {
            ///GlobalPressController.OnGlobalPress -= HandleOnGlobalPress;
        }

        #endregion

        #region Static Functions

        //If Any Parent or Self contain Focus, Or Focus equal null and panel is Opened or GameObject is Active
        public static bool IsUnderFocus(GameObject p_object)
        {
            if (p_object != null)
            {
                FocusGroup v_focus = FocusGroup.GetFocus();
                PanelStateEnum v_panelState = v_focus == null ? PanelStateEnum.Opened : TweenContainerUtils.GetContainerVisibility(v_focus.gameObject);
                if (v_panelState == PanelStateEnum.Opened && (FocusGroup.GetDirectFocusGroupComponent(p_object) == v_focus))
                    return true;
            }
            return false;
        }

        public static FocusGroup GetDirectFocusGroupComponent(GameObject p_child)
        {
            if (p_child != null)
            {
                FocusGroup[] v_parentsFocus = p_child.GetComponentsInParent<FocusGroup>();
                FocusGroup v_directParentFocus = null;
                foreach (FocusGroup v_parentFocus in v_parentsFocus)
                {
                    if (v_parentFocus != null && v_parentFocus.enabled)
                    {
                        v_directParentFocus = v_parentFocus;
                        break;
                    }
                }
                return v_directParentFocus;
            }
            return null;
        }

        public static bool ParentContainFocus(GameObject p_child)
        {
            if (p_child != null)
            {
                FocusGroup v_directParentFocus = GetDirectFocusGroupComponent(p_child);
                return ContainFocus(v_directParentFocus);
            }
            return false;
        }

        public static bool ContainFocus(FocusGroup p_container)
        {
            if (p_container != null && p_container == GetFocus())
            {
                return true;
            }
            return false;
        }

        public static void RemoveFocus(FocusGroup p_container)
        {
            if (p_container != null)
            {
                FocusGroup v_oldFocus = GetFocus();
                FocusOrder.RemoveChecking(p_container);
                //Call Focus Events
                if (v_oldFocus == p_container)
                {
                    FocusGroup v_newFocus = GetFocus();
                    if (v_oldFocus.OnLoseFocusCallback != null)
                        v_oldFocus.OnLoseFocusCallback.Invoke();
                    if (v_newFocus != null && v_newFocus.OnGainFocusCallback != null)
                        v_newFocus.OnGainFocusCallback.Invoke();
                }
            }
        }

        public static void SetFocus(FocusGroup p_container)
        {
            FocusGroup v_oldFocus = GetFocus();
            if (p_container != null)
            {
                if (v_oldFocus != p_container)
                {
                    FocusOrder.RemoveChecking(p_container);
                    if (FocusOrder.Count > 0)
                        FocusOrder.Insert(0, p_container);
                    else
                        FocusOrder.Add(p_container);
                    //Call Focus Events
                    if (v_oldFocus != null && v_oldFocus.OnLoseFocusCallback != null)
                        v_oldFocus.OnLoseFocusCallback.Invoke();
                    if (p_container.OnGainFocusCallback != null)
                        p_container.OnGainFocusCallback.Invoke();
                }
            }
        }

        public static FocusGroup GetFocus()
        {
            FocusOrder.RemoveNulls();
            FocusGroup v_container = FocusOrder.GetFirst();
            return v_container;
        }

        protected static void CorrectFullScreenRendering(bool p_isTransition)
        {
            FocusGroup v_firstFullScreenRenderingContainer = null;
            FocusGroup v_secondFullScreenRenderingContainer = null;
            foreach (FocusGroup v_container in FocusOrder)
            {
                if (v_container != null && v_container.FullScreenRendering)
                {
                    v_container.CheckCanvasAndGraphicsRaycaster();
                    if (v_firstFullScreenRenderingContainer == null)
                        v_firstFullScreenRenderingContainer = v_container;
                    else if (p_isTransition && v_firstFullScreenRenderingContainer != null && v_secondFullScreenRenderingContainer == null)
                        v_secondFullScreenRenderingContainer = v_container;

                    bool v_canvasEnabled = v_firstFullScreenRenderingContainer == v_container || v_secondFullScreenRenderingContainer == v_container;
                    v_container.SetCanvasEnabled(v_canvasEnabled, v_canvasEnabled);
                }
            }
        }

        #endregion
    }
}
