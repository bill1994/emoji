using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Performance
{
    public class SustainedCanvasView : SustainedRenderView, Kyub.Performance.ICanvasRebuildListener
    {
        #region Private Variables

        HashSet<ISustainedElement> _sustainedChildrenElements = new HashSet<ISustainedElement>();

        bool _childrenNeedsConstantRepaint = false;
        bool _isChildrensDirty = false;

        protected SustainedCanvasView _sustainedCanvasParent = null;
        protected Canvas _rootParent = null;

        #endregion

        #region Public Properties

        Canvas _cachedCanvas = null;
        public virtual Canvas Canvas
        {
            get
            {
                if (this != null && _cachedCanvas == null)
                    _cachedCanvas = GetComponentInParent<Canvas>();
                return _cachedCanvas;
            }
        }

        public override bool RequiresConstantRepaint
        {
            get
            {
                return _childrenNeedsConstantRepaint || base.RequiresConstantRepaint;
            }

            set
            {
                base.RequiresConstantRepaint = value;
            }
        }

        public override bool UseRenderBuffer  
        {
            get
            {
                if (_rootParent != null && _rootParent.worldCamera != null)
                {
                    var v_cameraView = _rootParent.worldCamera.GetComponent<SustainedCameraView>();
                    if (v_cameraView != null)
                        return v_cameraView.UseRenderBuffer;
                }
                return false;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            if (Canvas != null)
                ConfigureLowPerformanceCanvasGroup();
            base.Awake();
        }

        protected override void OnEnable()
        {
            FindRootCanvasParent();
            
            base.OnEnable();
            if (_started)
                TryCheckSustainedChildrens(true);
        }

        protected override void OnDisable()
        {
            SustainedCanvasView.UnregisterDynamicElement(this);
            base.OnDisable();
        }

        protected override void Start()
        {
            if (Application.isPlaying && Canvas == null)
            {
                Debug.LogWarning("[SustainedCanvasView] Must contain a canvas to work, removing component to prevent instabilities (sender: " + name + ")");
                Object.Destroy(this);
                return;
            }

            BroadcastMessage("OnCanvasHierarchyChanged", SendMessageOptions.DontRequireReceiver);
            TryCheckSustainedChildrens(true);
            base.Start();
        }

        protected virtual void Update()
        {
            TryCheckSustainedChildrens();
        }

        protected virtual void OnCanvasHierarchyChanged()
        {
            FindRootCanvasParent();
        }

        protected virtual void OnBeforeTransformParentChanged()
        {
            SustainedCanvasView.UnregisterDynamicElement(this);
            UnregisterEvents();
        }

        protected virtual void OnTransformParentChanged()
        {
            FindRootCanvasParent();
            RegisterEvents();
        }

        #endregion

        #region Rendering Helper Functions

        protected internal float _cachedAlphaValue = 1;
        internal CanvasGroup ConfigureLowPerformanceCanvasGroup()
        {
            var v_canvasGroup = GetComponent<CanvasGroup>();
            if (v_canvasGroup == null)
                v_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (_isViewActive)
                _cachedAlphaValue = v_canvasGroup.alpha;

            return v_canvasGroup;
        }

        protected override void SetViewActive(bool p_active)
        {
            var v_canvas = Canvas;
            if (v_canvas != null && _isViewActive != p_active)
            {
                var v_canvasGroup = GetComponent<CanvasGroup>();
                if (v_canvasGroup == null)
                    v_canvasGroup = ConfigureLowPerformanceCanvasGroup();

                if (!p_active)
                {
                    _cachedAlphaValue = v_canvasGroup.alpha;
                    v_canvasGroup.alpha = 0;
                }
                else
                    v_canvasGroup.alpha = _cachedAlphaValue;
            }

            _isViewActive = p_active;
        }

        #endregion

        #region Helper Functions

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            //Only if is a World Canvas
            if (!IsScreenCanvasMember())
                CanvasRebuildRegistry.RegisterRebuildListener(this);
            //We must stop drawing canvas for a frame (when switching between ScreenBuffer to RenderBuffer)
            //else if(SustainedPerformanceManager.IsWaitingRenderBuffer)
            //    RegisterBufferEvents();
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            UnregisterBufferEvents();
            CanvasRebuildRegistry.UnregisterRebuildListener(this);
        }

        protected virtual void RegisterBufferEvents()
        {
            UnregisterBufferEvents();
            SustainedPerformanceManager.OnAfterDrawBuffer += HandleOnAfterDrawBuffer;
            //SustainedPerformanceManager.OnAfterWaitingToPrepareRenderBuffer += HandleOnAfterWaitingToPrepareRenderBuffer;
        }

        protected virtual void UnregisterBufferEvents()
        {
            SustainedPerformanceManager.OnAfterDrawBuffer -= HandleOnAfterDrawBuffer;
            //SustainedPerformanceManager.OnAfterWaitingToPrepareRenderBuffer -= HandleOnAfterWaitingToPrepareRenderBuffer;
        }


        protected virtual Canvas FindRootCanvasParent()
        {
            _rootParent = Canvas != null? Canvas.rootCanvas : null;

            //Renew Sustained canvas parent based in Canvas
            var v_newSustainedParent = FindSustainedCanvasParent(this);

            //Unregister self from the old SustainedCanvas Parent
            if (_sustainedCanvasParent != null && v_newSustainedParent != _sustainedCanvasParent)
                SustainedCanvasView.UnregisterDynamicElement(this);

            //Register self to a new SustainedCanvas
            _sustainedCanvasParent = v_newSustainedParent;
            if (_sustainedCanvasParent != null && enabled && gameObject.activeInHierarchy)
                SustainedCanvasView.RegisterDynamicElement(this);

            return _rootParent;
        }

        public override bool IsScreenCanvasMember()
        {
            return _rootParent != null && _rootParent.renderMode != RenderMode.WorldSpace;
        }

        public override void MarkDynamicElementDirty()
        {
            _isChildrensDirty = true;
        }

        protected virtual void TryCheckSustainedChildrens(bool p_force = false)
        {
            if (_isChildrensDirty || p_force)
            {
                _isChildrensDirty = false;

                //Invalidate constant repaint based in childrens
                var v_requireConstantRepaint = false;
                foreach (var _sustainedElement in _sustainedChildrenElements)
                {
                    if (!_sustainedElement.IsDestroyed())
                    {
                        v_requireConstantRepaint = v_requireConstantRepaint || _sustainedElement.RequiresConstantRepaint;
                        if (v_requireConstantRepaint)
                            break;
                    }
                }

                _childrenNeedsConstantRepaint = v_requireConstantRepaint;

                //We must invalidate all SustainedCanvas Parents
                var v_sustainedParent = FindSustainedCanvasParent(this);
                if (v_sustainedParent != null)
                    v_sustainedParent.TryCheckSustainedChildrens(true);
                else
                    SustainedPerformanceManager.MarkDynamicElementsDirty();
            }
        }

        #endregion

        #region SustainedPerformance Receivers

        /*protected virtual void HandleOnAfterWaitingToPrepareRenderBuffer(int p_invalidCullingMask)
        {
            SetViewActive(false);
        }*/

        protected virtual void HandleOnAfterDrawBuffer(Dictionary<int, RenderTexture> p_renderBuffersDict)
        {
            UnregisterBufferEvents();
            SetViewActive(true);
        }

        protected override void HandleOnSetLowPerformance()
        {
            //We only want to invalidate this canvas if is SelfRequired Constant Repaint
            TryCheckSustainedChildrens();

            //var v_viewIsActive = SustainedPerformanceManager.UseSafeRefreshMode ? SustainedPerformanceManager.RequiresConstantRepaint : this.RequiresConstantRepaint;

            //In WorldSpace Canvas we must check for buffer repaint
            var v_viewIsActive = IsScreenCanvasMember()? 
                SustainedPerformanceManager.RequiresConstantRepaint : 
                SustainedPerformanceManager.RequiresConstantBufferRepaint; 
            
            SetViewActive(v_viewIsActive);
        }

        protected override void HandleOnSetHighPerformance(bool p_invalidateBuffer)
        {
            //We only want to invalidate this canvas if is SelfRequired Constant Repaint
            TryCheckSustainedChildrens();

            if (IsScreenCanvasMember())
            {
                /*if (SustainedPerformanceManager.IsWaitingRenderBuffer)
                {
                    //Register to invalidate After DrawBuffer
                    RegisterBufferEvents();
                }*/
                var v_viewIsActive = SustainedPerformanceManager.UseSafeRefreshMode;
                if (!v_viewIsActive)
                {
                    v_viewIsActive = p_invalidateBuffer || SustainedPerformanceManager.RequiresConstantRepaint || SustainedPerformanceManager.IsCanvasViewInvalid(this);
                }
                SetViewActive(v_viewIsActive);
            }
            //WorldSpace Canvas
            else
            {
                var v_isViewActive = p_invalidateBuffer || SustainedPerformanceManager.RequiresConstantBufferRepaint;
                if (!v_isViewActive)
                    v_isViewActive = !UseRenderBuffer;

                SetViewActive(v_isViewActive);
            }
        }

        //Canvas Rebuild Listener for WorldSpace Canvas
        public void OnCanvasRebuild()
        {
            //We must invalidate buffer when in WorldSpace
            if (!IsScreenCanvasMember())
                SustainedPerformanceManager.Invalidate(this);
        }

        #endregion

        #region Static Helper Functions

        public static void RegisterDynamicElement(ISustainedElement p_element)
        {
            SustainedCanvasView v_canvasView = FindSustainedCanvasParent(p_element);
            if (v_canvasView != null)
            {
                if (!v_canvasView._sustainedChildrenElements.Contains(p_element))
                {
                    v_canvasView._sustainedChildrenElements.Add(p_element);
                    v_canvasView.MarkDynamicElementDirty();
                }
            }
        }

        public static void UnregisterDynamicElement(ISustainedElement p_element)
        {
            SustainedCanvasView v_canvasView = FindSustainedCanvasParent(p_element);
            if (v_canvasView != null)
            {
                v_canvasView._sustainedChildrenElements.Remove(p_element);
                v_canvasView.MarkDynamicElementDirty();
            }
        }

        public static SustainedCanvasView FindSustainedCanvasParent(ISustainedElement p_element)
        {
            SustainedCanvasView v_canvasView = null;
            if (!p_element.IsDestroyed())
            {
                if (p_element is SustainedCanvasView)
                {
                    v_canvasView = ((SustainedCanvasView)p_element);
                    if (v_canvasView.transform.parent != null)
                        v_canvasView = v_canvasView.transform.parent.GetComponentInParent<SustainedCanvasView>();
                    else
                        v_canvasView = null;
                }
                else if (p_element is Component)
                {
                    v_canvasView = (p_element as Component).GetComponentInParent<SustainedCanvasView>();
                }
            }
            return v_canvasView;
        }

        public static IList<SustainedCanvasView> FindAllActiveCanvasView()
        {
            List<SustainedCanvasView> v_activeCanvasViews = new List<SustainedCanvasView>();
            foreach (var v_view in s_sceneRenderViews)
            {
                var v_sustainedCanvasView = v_view as SustainedCanvasView;
                if (v_sustainedCanvasView != null &&
                    v_sustainedCanvasView.enabled && v_sustainedCanvasView.gameObject.activeInHierarchy &&
                    v_sustainedCanvasView.Canvas != null)
                    v_activeCanvasViews.Add(v_sustainedCanvasView);
            }

            //Sort cameras by depth
            if (v_activeCanvasViews.Count > 1)
                v_activeCanvasViews.Sort((a, b) => a.Canvas.renderOrder.CompareTo(b.Canvas.renderOrder));

            return v_activeCanvasViews;
        }

        #endregion
    }
}