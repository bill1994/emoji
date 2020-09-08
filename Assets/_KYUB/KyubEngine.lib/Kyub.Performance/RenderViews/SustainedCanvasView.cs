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
                    var cameraView = _rootParent.worldCamera.GetComponent<SustainedCameraView>();
                    if (cameraView != null)
                        return cameraView.UseRenderBuffer;
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
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (_isViewActive)
                _cachedAlphaValue = canvasGroup.alpha;

            return canvasGroup;
        }

        protected override void SetViewActive(bool active)
        {
            var canvas = Canvas;
            if (canvas != null && _isViewActive != active)
            {
                var canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = ConfigureLowPerformanceCanvasGroup();

                if (!active && !Application.isEditor)
                {
                    _cachedAlphaValue = canvasGroup.alpha;
                    canvasGroup.alpha = 0;
                }
                else
                    canvasGroup.alpha = _cachedAlphaValue;
            }

            _isViewActive = active;
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
            var newSustainedParent = FindSustainedCanvasParent(this);

            //Unregister self from the old SustainedCanvas Parent
            if (_sustainedCanvasParent != null && newSustainedParent != _sustainedCanvasParent)
                SustainedCanvasView.UnregisterDynamicElement(this);

            //Register self to a new SustainedCanvas
            _sustainedCanvasParent = newSustainedParent;
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

        protected virtual void TryCheckSustainedChildrens(bool force = false)
        {
            if (_isChildrensDirty || force)
            {
                _isChildrensDirty = false;

                //Invalidate constant repaint based in childrens
                var requireConstantRepaint = false;
                foreach (var _sustainedElement in _sustainedChildrenElements)
                {
                    if (!_sustainedElement.IsDestroyed())
                    {
                        requireConstantRepaint = requireConstantRepaint || _sustainedElement.RequiresConstantRepaint;
                        if (requireConstantRepaint)
                            break;
                    }
                }

                _childrenNeedsConstantRepaint = requireConstantRepaint;

                //We must invalidate all SustainedCanvas Parents
                var sustainedParent = FindSustainedCanvasParent(this);
                if (sustainedParent != null)
                    sustainedParent.TryCheckSustainedChildrens(true);
                else
                    SustainedPerformanceManager.MarkDynamicElementsDirty();
            }
        }

        #endregion

        #region SustainedPerformance Receivers

        /*protected virtual void HandleOnAfterWaitingToPrepareRenderBuffer(int invalidCullingMask)
        {
            SetViewActive(false);
        }*/

        protected virtual void HandleOnAfterDrawBuffer(Dictionary<int, RenderTexture> renderBuffersDict)
        {
            UnregisterBufferEvents();
            SetViewActive(true);
        }

        protected override void HandleOnSetLowPerformance()
        {
            //We only want to invalidate this canvas if is SelfRequired Constant Repaint
            TryCheckSustainedChildrens();

            //var viewIsActive = SustainedPerformanceManager.UseSafeRefreshMode ? SustainedPerformanceManager.RequiresConstantRepaint : this.RequiresConstantRepaint;

            //In WorldSpace Canvas we must check for buffer repaint
            var viewIsActive = IsScreenCanvasMember()? 
                SustainedPerformanceManager.RequiresConstantRepaint : 
                SustainedPerformanceManager.RequiresConstantBufferRepaint; 
            
            SetViewActive(viewIsActive);
        }

        protected override void HandleOnSetHighPerformance(bool invalidateBuffer)
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
                var viewIsActive = SustainedPerformanceManager.UseSafeRefreshMode;
                if (!viewIsActive)
                {
                    viewIsActive = invalidateBuffer || SustainedPerformanceManager.RequiresConstantRepaint || SustainedPerformanceManager.IsCanvasViewInvalid(this);
                }
                SetViewActive(viewIsActive);
            }
            //WorldSpace Canvas
            else
            {
                var isViewActive = invalidateBuffer || SustainedPerformanceManager.RequiresConstantBufferRepaint;
                if (!isViewActive)
                    isViewActive = !UseRenderBuffer;

                SetViewActive(isViewActive);
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

        public static void RegisterDynamicElement(ISustainedElement element)
        {
            SustainedCanvasView canvasView = FindSustainedCanvasParent(element);
            if (canvasView != null)
            {
                if (!canvasView._sustainedChildrenElements.Contains(element))
                {
                    canvasView._sustainedChildrenElements.Add(element);
                    canvasView.MarkDynamicElementDirty();
                }
            }
        }

        public static void UnregisterDynamicElement(ISustainedElement element)
        {
            SustainedCanvasView canvasView = FindSustainedCanvasParent(element);
            if (canvasView != null)
            {
                canvasView._sustainedChildrenElements.Remove(element);
                canvasView.MarkDynamicElementDirty();
            }
        }

        public static SustainedCanvasView FindSustainedCanvasParent(ISustainedElement element)
        {
            SustainedCanvasView canvasView = null;
            if (!element.IsDestroyed())
            {
                if (element is SustainedCanvasView)
                {
                    canvasView = ((SustainedCanvasView)element);
                    if (canvasView.transform.parent != null)
                        canvasView = canvasView.transform.parent.GetComponentInParent<SustainedCanvasView>();
                    else
                        canvasView = null;
                }
                else if (element is Component)
                {
                    canvasView = (element as Component).GetComponentInParent<SustainedCanvasView>();
                }
            }
            return canvasView;
        }

        public static IList<SustainedCanvasView> FindAllActiveCanvasView()
        {
            List<SustainedCanvasView> activeCanvasViews = new List<SustainedCanvasView>();
            foreach (var view in s_sceneRenderViews)
            {
                var sustainedCanvasView = view as SustainedCanvasView;
                if (sustainedCanvasView != null &&
                    sustainedCanvasView.enabled && sustainedCanvasView.gameObject.activeInHierarchy &&
                    sustainedCanvasView.Canvas != null)
                    activeCanvasViews.Add(sustainedCanvasView);
            }

            //Sort cameras by depth
            if (activeCanvasViews.Count > 1)
                activeCanvasViews.Sort((a, b) => a.Canvas.renderOrder.CompareTo(b.Canvas.renderOrder));

            return activeCanvasViews;
        }

        #endregion
    }
}