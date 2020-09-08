using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kyub.Performance
{
    public class SustainedCameraView : SustainedRenderView
    {
        #region Private Variables

        [Header("CameraView Fields")]
        [SerializeField]
        bool m_useRenderBuffer = false;
        [SerializeField]
        int m_renderBufferIndex = 0;
        [SerializeField, IntPopup(-1, "ScreenSize", 16, 32, 64, 128, 256, 512, 768, 1024, 1280, 1536, 1792, 2048, 2560, 3072, 4096)]
        int m_maxRenderBufferSize = -1;

        #endregion

        #region Callbacks

        public UnityEvent OnInvalidate = new UnityEvent();

        #endregion

        #region Public Properties

        public int MaxRenderBufferSize
        {
            get
            {
                return m_maxRenderBufferSize;
            }
            set
            {
                if (m_maxRenderBufferSize == value)
                    return;

                m_maxRenderBufferSize = value;
                if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
                MarkDynamicElementDirty();
            }
        }

        Camera _cachedCamera = null;
        public virtual Camera Camera
        {
            get
            {
                if (this != null && _cachedCamera == null)
                    _cachedCamera = GetComponentInParent<Camera>();
                return _cachedCamera;
            }
        }

        public override bool UseRenderBuffer
        {
            get
            {
                return m_useRenderBuffer;
            }
            set
            {
                if (m_useRenderBuffer == value)
                    return;

                m_useRenderBuffer = value;
                if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
                MarkDynamicElementDirty();
            }
        }

        public virtual int RenderBufferIndex
        {
            get
            {
                return m_renderBufferIndex;
            }
            set
            {
                if (m_renderBufferIndex == value)
                    return;

                m_renderBufferIndex = value;
                if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
                MarkDynamicElementDirty();
            }
        }

        int _lastCullingMask = 0;
        public override int CullingMask
        {
            get
            {
                int currentCullingMask = 0;

                //Find Current CullingMask
                var camera = Camera;
                if (camera == null || !m_useRenderBuffer)
                    currentCullingMask = base.CullingMask;
                else
                    currentCullingMask = camera.cullingMask;

                if (_lastCullingMask != currentCullingMask)
                {
                    _lastCullingMask = currentCullingMask;
                    MarkDynamicElementDirty();
                }
                return currentCullingMask;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Start()
        {
            if (Application.isPlaying && Camera == null)
            {
                Debug.Log("[SustainedCameraView] Must contain a camera to work, removing component to prevent instabilities (sender: " + name + ")");
                Object.Destroy(this);
                return;
            }
            _lastCullingMask = CullingMask;
            //Camera.enabled = !m_useRenderBuffer;
            TryInitRenderBuffer();
            base.Start();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearRenderBuffer();
            SustainedPerformanceManager.Invalidate(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearRenderBuffer();
            SustainedPerformanceManager.Invalidate(this);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (Application.isPlaying && s_sceneRenderViews.Contains(this))
                MarkDynamicElementDirty();
            base.OnValidate();
        }
#endif

        protected virtual void OnPreRender()
        {
            _isDirty = false;
            if (OnInvalidate != null)
                OnInvalidate.Invoke();
        }

        #endregion

        #region Rendering Helper Functions

        protected override void SetViewActive(bool active)
        {
            if (m_useRenderBuffer)
                TryInitRenderBuffer();

            var camera = Camera;
            if (camera != null)
            {
                var cameraActive = active; //(active && (!m_useRenderBuffer || SustainedPerformanceManager.IsWaitingRenderBuffer));
                if (camera.enabled != cameraActive)
                {
                    //Special RenderBuffer Mode (we can't disable camera in this cycle when SustainedPerformanceManager.UseRenderBufferInHighPerformance == false)
                    //var canChangeCameraState = !m_useRenderBuffer || active || !SustainedPerformanceManager.IsWaitingRenderBuffer;
                    //if(canChangeCameraState)
                    camera.enabled = cameraActive;
                    if (cameraActive)
                        SetDirty();
                }
            }
            
            _isViewActive = active;
        }

        #endregion

        #region Buffer Helper Functions

        bool _isDirty = true;
        public bool IsDirty()
        {
            return _isDirty;
        }

        public void SetDirty()
        {
            _isDirty = true;
        }

        protected internal virtual void ClearRenderBuffer()
        {
            var bufferCamera = Camera;
            if (bufferCamera != null && bufferCamera.targetTexture != null)
            {
                bufferCamera.targetTexture = null;
            }
        }

        protected virtual void TryInitRenderBuffer()
        {
            if (Application.isPlaying)
            {
                if (m_useRenderBuffer)
                {
                    var bufferCamera = Camera;
                    if (bufferCamera != null)
                    {
                        var renderTexture = SustainedPerformanceManager.GetRenderBuffer(m_renderBufferIndex);
                        if (bufferCamera.targetTexture != renderTexture)
                            bufferCamera.targetTexture = renderTexture;
                    }
                }
            }
        }

        #endregion

        #region Other Helper Functions

        protected override void RegisterEvents()
        {
            UnregisterEvents();
            SustainedPerformanceManager.OnSetHighPerformance += HandleOnSetHighPerformance;
            SustainedPerformanceManager.OnSetLowPerformance += HandleOnSetLowPerformance;

            if (Camera != null && m_useRenderBuffer)
            {
                //SustainedPerformanceManager.OnAfterWaitingToPrepareRenderBuffer += HandleOnAfterWaitingToPrepareRenderBuffer;
                SustainedPerformanceManager.OnAfterDrawBuffer += HandleOnAfterDrawBuffer;
            }
        }

        protected override void UnregisterEvents()
        {
            SustainedPerformanceManager.OnSetHighPerformance -= HandleOnSetHighPerformance;
            SustainedPerformanceManager.OnSetLowPerformance -= HandleOnSetLowPerformance;

            //SustainedPerformanceManager.OnAfterWaitingToPrepareRenderBuffer -= HandleOnAfterWaitingToPrepareRenderBuffer;
            SustainedPerformanceManager.OnAfterDrawBuffer -= HandleOnAfterDrawBuffer;
        }

        #endregion

        #region SustainedPerformance Receivers

        /*protected virtual void HandleOnAfterWaitingToPrepareRenderBuffer(int invalidCullingMask)
        {
            SetViewActive(SustainedPerformanceManager.IsCameraViewInvalid(this, invalidCullingMask));
        }*/

        protected virtual void HandleOnAfterDrawBuffer(Dictionary<int, RenderTexture> renderBuffersDict)
        {
            if (m_useRenderBuffer && !SustainedPerformanceManager.IsCameraViewInvalid(this))
            {
                var camera = Camera;
                if (camera != null)
                    camera.enabled = false;
            }
        }

        protected override void HandleOnSetLowPerformance()
        {
            SetViewActive(SustainedPerformanceManager.RequiresConstantBufferRepaint && SustainedPerformanceManager.IsCameraViewInvalid(this));
        }

        protected override void HandleOnSetHighPerformance(bool invalidateBuffer)
        {
            var isViewActive = (invalidateBuffer || SustainedPerformanceManager.RequiresConstantBufferRepaint) && SustainedPerformanceManager.IsCameraViewInvalid(this);
            if (!isViewActive)
                isViewActive = !m_useRenderBuffer;

            SetViewActive(isViewActive);
        }

        #endregion

        #region Static Helper Functions

        public static IList<SustainedCameraView> FindAllActiveCameraViewsWithRenderBufferState(bool? useRenderBuffer, bool? viewIsActive = null)
        {
            List<SustainedCameraView> activeCameraViews = new List<SustainedCameraView>();
            foreach (var view in s_sceneRenderViews)
            {
                var sustainedCameraView = view as SustainedCameraView;
                if (sustainedCameraView != null &&
                    sustainedCameraView.enabled && sustainedCameraView.gameObject.activeInHierarchy &&
                    (viewIsActive == null || viewIsActive == sustainedCameraView.IsViewActive()) &&
                    (useRenderBuffer == null || sustainedCameraView.m_useRenderBuffer == useRenderBuffer) && 
                    sustainedCameraView.Camera != null)
                    activeCameraViews.Add(sustainedCameraView);
            }

            //Sort cameras by depth
            if (activeCameraViews.Count > 1)
                activeCameraViews.Sort((a, b) => a.Camera.depth.CompareTo(b.Camera.depth));

            return activeCameraViews;
        }

        #endregion
    }
}