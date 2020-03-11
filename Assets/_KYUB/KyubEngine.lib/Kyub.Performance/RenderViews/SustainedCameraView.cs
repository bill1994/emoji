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
                int v_currentCullingMask = 0;

                //Find Current CullingMask
                var v_camera = Camera;
                if (v_camera == null || !m_useRenderBuffer)
                    v_currentCullingMask = base.CullingMask;
                else
                    v_currentCullingMask = v_camera.cullingMask;

                if (_lastCullingMask != v_currentCullingMask)
                {
                    _lastCullingMask = v_currentCullingMask;
                    MarkDynamicElementDirty();
                }
                return v_currentCullingMask;
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

        protected override void SetViewActive(bool p_active)
        {
            if (m_useRenderBuffer)
                TryInitRenderBuffer();

            var v_camera = Camera;
            if (v_camera != null)
            {
                var cameraActive = p_active; //(p_active && (!m_useRenderBuffer || SustainedPerformanceManager.IsWaitingRenderBuffer));
                if (v_camera.enabled != cameraActive)
                {
                    //Special RenderBuffer Mode (we can't disable camera in this cycle when SustainedPerformanceManager.UseRenderBufferInHighPerformance == false)
                    //var v_canChangeCameraState = !m_useRenderBuffer || p_active || !SustainedPerformanceManager.IsWaitingRenderBuffer;
                    //if(v_canChangeCameraState)
                    v_camera.enabled = cameraActive;
                    if (cameraActive)
                        SetDirty();
                }
            }
            
            _isViewActive = p_active;
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
            var v_bufferCamera = Camera;
            if (v_bufferCamera != null && v_bufferCamera.targetTexture != null)
            {
                v_bufferCamera.targetTexture = null;
            }
        }

        protected virtual void TryInitRenderBuffer()
        {
            if (Application.isPlaying)
            {
                if (m_useRenderBuffer)
                {
                    var v_bufferCamera = Camera;
                    if (v_bufferCamera != null)
                    {
                        var v_renderTexture = SustainedPerformanceManager.GetRenderBuffer(m_renderBufferIndex);
                        if (v_bufferCamera.targetTexture != v_renderTexture)
                            v_bufferCamera.targetTexture = v_renderTexture;
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

        /*protected virtual void HandleOnAfterWaitingToPrepareRenderBuffer(int p_invalidCullingMask)
        {
            SetViewActive(SustainedPerformanceManager.IsCameraViewInvalid(this, p_invalidCullingMask));
        }*/

        protected virtual void HandleOnAfterDrawBuffer(Dictionary<int, RenderTexture> p_renderBuffersDict)
        {
            if (m_useRenderBuffer && !SustainedPerformanceManager.IsCameraViewInvalid(this))
            {
                var v_camera = Camera;
                if (v_camera != null)
                    v_camera.enabled = false;
            }
        }

        protected override void HandleOnSetLowPerformance()
        {
            SetViewActive(SustainedPerformanceManager.RequiresConstantBufferRepaint && SustainedPerformanceManager.IsCameraViewInvalid(this));
        }

        protected override void HandleOnSetHighPerformance(bool p_invalidateBuffer)
        {
            var v_isViewActive = (p_invalidateBuffer || SustainedPerformanceManager.RequiresConstantBufferRepaint) && SustainedPerformanceManager.IsCameraViewInvalid(this);
            if (!v_isViewActive)
                v_isViewActive = !m_useRenderBuffer;

            SetViewActive(v_isViewActive);
        }

        #endregion

        #region Static Helper Functions

        public static IList<SustainedCameraView> FindAllActiveCameraViewsWithRenderBufferState(bool? p_useRenderBuffer, bool? viewIsActive = null)
        {
            List<SustainedCameraView> v_activeCameraViews = new List<SustainedCameraView>();
            foreach (var v_view in s_sceneRenderViews)
            {
                var v_sustainedCameraView = v_view as SustainedCameraView;
                if (v_sustainedCameraView != null &&
                    v_sustainedCameraView.enabled && v_sustainedCameraView.gameObject.activeInHierarchy &&
                    (viewIsActive == null || viewIsActive == v_sustainedCameraView.IsViewActive()) &&
                    (p_useRenderBuffer == null || v_sustainedCameraView.m_useRenderBuffer == p_useRenderBuffer) && 
                    v_sustainedCameraView.Camera != null)
                    v_activeCameraViews.Add(v_sustainedCameraView);
            }

            //Sort cameras by depth
            if (v_activeCameraViews.Count > 1)
                v_activeCameraViews.Sort((a, b) => a.Camera.depth.CompareTo(b.Camera.depth));

            return v_activeCameraViews;
        }

        #endregion
    }
}