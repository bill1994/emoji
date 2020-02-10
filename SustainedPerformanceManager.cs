using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub;
using System;
using UnityEngine.SceneManagement;

namespace Kyub.Performance
{
    //public enum RenderBufferModeEnum { Immediate, Delayed }

    public class SustainedPerformanceManager : Kyub.Singleton<SustainedPerformanceManager>
    {
        #region Static Events

        public static event Action OnBeforeSetPerformance; //Before Process Performances (Useful to know if LowPerformanceView will be activated)
        public static event Action<bool> OnSetHighPerformance;
        public static event Action OnSetLowPerformance;
        //public static event Action<int> OnAfterWaitingToPrepareRenderBuffer; //Only in Delayed RenderBuffer Mode
        public static event Action OnAfterSetPerformance; //After Process Performances
        public static event Action<Dictionary<int, RenderTexture>> OnAfterDrawBuffer;

        #endregion

        #region Public Static Properties

        static bool s_isEndOfFrame = false;
        public static bool IsEndOfFrame
        {
            get
            {
                return s_isEndOfFrame;
            }
        }

        public new static SustainedPerformanceManager Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = GetInstanceFastSearch();
                return s_instance;
            }
        }

        protected static Dictionary<int, RenderTexture> s_renderBufferDict = new Dictionary<int, RenderTexture>();
        public static RenderTexture GetRenderBuffer(int p_bufferIndex)
        {
            //if (IsWaitingRenderBuffer || (!UseImmediateRenderBufferMode && RequiresConstantRepaint))
            //    return null;

            RenderTexture v_renderBuffer = null;
            if (s_renderBufferDict.ContainsKey(p_bufferIndex))
            {
                v_renderBuffer = s_renderBufferDict[p_bufferIndex];
                if ((s_useRenderBuffer && v_renderBuffer == null) || (!s_useRenderBuffer && v_renderBuffer != null))
                {
                    CheckBufferTexture(ref v_renderBuffer);

                    if (v_renderBuffer != null)
                        s_renderBufferDict[p_bufferIndex] = v_renderBuffer;
                    else
                        s_renderBufferDict.Remove(p_bufferIndex);
                }
            }

            return v_renderBuffer;
        }

        public static bool UseSimulatedFrameBuffer
        {
            get
            {
#if UNITY_EDITOR && UNITY_IOS
                return true;
#else
                if (s_instance == null)
                    s_instance = GetInstanceFastSearch();

                //Metal/Mobile require simulate frameBuffer
                return (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal || Application.isMobilePlatform) ||
                    (s_instance != null && s_instance.m_forceSimulateFrameBuffer);
#endif
            }
        }

        protected static bool s_useRenderBuffer = false;
        public static bool UseRenderBuffer
        {
            get
            {
                return s_useRenderBuffer;
            }
        }

        /*public static bool UseImmediateRenderBufferMode
        {
            get
            {
                var v_instance = GetInstanceFastSearch();
                if (v_instance != null)
                    return v_instance.m_renderBufferMode == RenderBufferModeEnum.Immediate;

                return true;
            }
        }

        static bool s_isWaitingRenderBuffer = false;
        public static bool IsWaitingRenderBuffer
        {
            get
            {
                return s_isWaitingRenderBuffer;
            }
        }*/

        public static bool UseSafeRefreshMode
        {
            get
            {
                var v_instance = GetInstanceFastSearch();
                if (v_instance != null)
                    return v_instance.m_safeRefreshMode;

                return true;
            }
        }

        protected static bool s_requireConstantRepaint = false;
        public static bool RequiresConstantRepaint
        {
            get
            {
                var v_instance = GetInstanceFastSearch();
                if (v_instance != null)
                    return s_requireConstantRepaint || v_instance.m_forceAlwaysInvalidate;

                return s_requireConstantRepaint;
            }
        }

        protected static bool s_requireConstantBufferRepaint = false;
        public static bool RequiresConstantBufferRepaint
        {
            get
            {
                var v_instance = GetInstanceFastSearch();
                if (v_instance != null)
                    return s_requireConstantBufferRepaint || v_instance.m_forceAlwaysInvalidate;

                return s_requireConstantBufferRepaint;
            }
        }

        protected static int s_minimumSupportedFps = 0;
        public static int MinimumSupportedFps
        {
            get
            {
                var v_instance = GetInstanceFastSearch();
                if (v_instance != null)
                    return Mathf.Clamp(s_minimumSupportedFps, (int)v_instance.m_performanceFpsRange.x, (int)v_instance.m_performanceFpsRange.y);

                return s_minimumSupportedFps;
            }
        }

        #endregion

        #region Private Variables

        [SerializeField, Tooltip("This property will try to add a SustainedRenderView on each Camera/Canvas in Scene")]
        bool m_autoConfigureRenderViews = true;
        [SerializeField, Tooltip("This property try simulate FrameBuffer (keep last frame between frames). \nSome platforms dicard frameBuffer every cycle so we can activate this property to simulate frameBuffer state")]
        bool m_forceSimulateFrameBuffer = false;
        [Space]
        //[Tooltip("* RenderBufferMode.Delayed will wait one cycle to draw to a RenderTexture after invalidation (it optimizes the app if invalidate will be called every cycle)\n" +
        //         "* RenderBufferModeEnum.Immediate will draw at same cycle of invalidation to a RenderTexture")]
        //[SerializeField]
        //RenderBufferModeEnum m_renderBufferMode = RenderBufferModeEnum.Immediate;
        [SerializeField, Tooltip("This property will force invalidate even if not emitted by this canvas hierarchy (Prevent Alpha-Overlap bug)")]
        bool m_safeRefreshMode = true;
        [Space]
        [SerializeField, Tooltip("Useful for debug purpose (will force invalidate every frame)")]
        bool m_forceAlwaysInvalidate = false;
        [Space]
        [SerializeField, Tooltip("Enable this to activate DynamicFps Controller")]
        bool m_canControlFps = true;
        [SerializeField, MinMaxSlider(5, 150)]
        Vector2 m_performanceFpsRange = new Vector2(25, 60);
        [Space]
        [SerializeField, Range(0.5f, 5.0f)]
        float m_autoDisableHighPerformanceTime = 1.0f;

        int _defaultInvalidCullingMask = 0;
        bool _performanceIsDirty = false;
        bool _bufferIsDirty = false;
        bool _isHighPerformance = true;

        protected internal static LowPerformanceView s_lowPerformanceView = null;
        protected static bool s_canGenerateLowPerformanceView = false;
        protected static Vector2Int s_lastScreenSize = Vector2Int.zero;

        #endregion

        #region Properties

        public Vector2 PerformanceFpsRange
        {
            get
            {
                return m_performanceFpsRange;
            }

            set
            {
                if (m_performanceFpsRange == value)
                    return;
                m_performanceFpsRange = value;

                if (enabled && gameObject.activeInHierarchy)
                    Refresh();
            }
        }

        public bool CanControlFps
        {
            get
            {
                return m_canControlFps;
            }

            set
            {
                if (m_canControlFps == value)
                    return;
                m_canControlFps = value;

                if (enabled && gameObject.activeInHierarchy)
                    Refresh();
            }
        }

        public bool AutoConfigureRenderViews
        {
            get
            {
                return m_autoConfigureRenderViews;
            }

            set
            {
                if (m_autoConfigureRenderViews == value)
                    return;
                m_autoConfigureRenderViews = value;

                if (enabled && gameObject.activeInHierarchy && value)
                    TryAutoCreateRenderViews();
            }
        }

        public bool ForceAlwaysInvalidate
        {
            get
            {
                return m_forceAlwaysInvalidate;
            }

            set
            {
                if (m_forceAlwaysInvalidate == value)
                    return;
                m_forceAlwaysInvalidate = value;
                Invalidate();
            }
        }

        public bool SafeRefreshMode
        {
            get
            {
                return m_safeRefreshMode;
            }

            set
            {
                if (m_safeRefreshMode == value)
                    return;
                m_safeRefreshMode = value;
                Invalidate();
            }
        }

        public bool ForceSimulateFrameBuffer
        {
            get
            {
                return m_forceSimulateFrameBuffer;
            }

            set
            {
                if (m_forceSimulateFrameBuffer == value)
                    return;
                m_forceSimulateFrameBuffer = value;
                CheckBufferTextures();
                Invalidate();
            }
        }

        /*public RenderBufferModeEnum RenderBufferMode
        {
            get
            {
                return m_renderBufferMode;
            }

            set
            {
                if (m_renderBufferMode == value)
                    return;
                m_renderBufferMode = value;
                CheckBufferTextures();
                Invalidate();
            }
        }*/

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            if (s_instance == this)
            {
                s_isEndOfFrame = false;
                s_canGenerateLowPerformanceView = true;
                MarkDynamicElementsDirty();
                TryAutoCreateRenderViews();

                RegisterEvents();

                CheckBufferTextures();
                Invalidate();
            }
        }

        protected virtual void OnDisable()
        {
            UnregisterEvents();
            if (s_instance == this)
            {
                s_isEndOfFrame = false;
                s_canGenerateLowPerformanceView = false;
                s_useRenderBuffer = false;
                _bufferIsDirty = true;
                CancelSetLowPerformance();
                SetHighPerformanceImmediate();
                ReleaseRenderBuffers();
                ConfigureLowPerformanceView();
            }
        }

        protected override void OnSceneWasLoaded(Scene p_scene, LoadSceneMode p_mode)
        {
            base.OnSceneWasLoaded(p_scene, p_mode);
            MarkDynamicElementsDirty();
            TryAutoCreateRenderViews();

            Invalidate();
            //_bufferIsDirty = true;
            //s_invalidCullingMask = ~0;
            //SetHighPerformanceDelayed();
        }

        protected virtual void Update()
        {
            if (s_instance == this)
            {
                s_isEndOfFrame = false;
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    //Force refresh when SceneView was selected
                    if (Camera.current != null && Camera.current.name == "SceneCamera" && !Camera.current.scene.IsValid())
                        _performanceIsDirty = true;
                }
#endif

                TryCheckDynamicElements();
                //We must check if screen size changed
                //if (!_bufferIsDirty)
                //{
                //    _bufferIsDirty = CheckBufferTextures();
                //}
                TryApplyPerformanceUpdate();
            }
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            Invalidate();
        }

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            Invalidate();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            m_performanceFpsRange = new Vector2(Mathf.RoundToInt(m_performanceFpsRange.x), Mathf.RoundToInt(m_performanceFpsRange.y));
            MarkToSetHighPerformance(true);
        }
#endif

        #endregion

        #region Internal Helper Functions

        protected internal virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (CanvasRebuildRegistry.instance != null)
                CanvasRebuildRegistry.OnWillPerformRebuild += HandleOnWillPerformRebuild;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneWasLoaded;
        }

        protected internal virtual void UnregisterEvents()
        {
            if (CanvasRebuildRegistry.instance != null)
                CanvasRebuildRegistry.OnWillPerformRebuild -= HandleOnWillPerformRebuild;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneWasLoaded;
        }

        #endregion

        #region Calvas Rebuild Receivers

        private void HandleOnWillPerformRebuild()
        {
            if (_ignoreNextLayoutRebuild)
            {
                ClearPendentInvalidTransforms(false);
                _ignoreNextLayoutRebuild = false;
            }
            else
                Refresh(/*UseSafeRefreshMode? null : CanvasRebuildRegistry.instance.GraphicRebuildQueue*/);
        }

        #endregion

        #region Performance Important Functions

        protected virtual bool OnPerformanceUpdate()
        {
            return SetHighPerformanceDelayed();
        }

        #endregion

        #region Performance Checker Functions

        protected virtual void TryApplyPerformanceUpdate()
        {
            //invalidate based in last screen size (we must redraw screen when size changed)
            if (Screen.width != s_lastScreenSize.x || Screen.height != s_lastScreenSize.y)
            {
                s_lastScreenSize = new Vector2Int(Screen.width, Screen.height);
                s_invalidCullingMask = ~0;
                _bufferIsDirty = true;
            }
            if (_performanceIsDirty || _bufferIsDirty)
            {
                _performanceIsDirty = false;
                if (OnPerformanceUpdate())
                    _bufferIsDirty = false;
            }
        }

        Coroutine _routineSetLowPerformance = null;
        protected virtual void CancelSetLowPerformance()
        {
            _lowPerformanceWaitTime = 0;
            if (_routineSetLowPerformance != null)
            {
                StopCoroutine(_routineSetLowPerformance);
                _routineSetLowPerformance = null;
            }
        }

        protected virtual void SetLowPerformanceDelayed(float p_waitTime = 0)
        {
            CancelSetHighPerformance();
            if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
            {
                if (_lowPerformanceWaitTime <= 0)
                {
                    CancelSetLowPerformance();
                    _routineSetLowPerformance = StartCoroutine(SetLowPerformanceInEndOfFrameRoutine(p_waitTime));
                }
                else
                    _lowPerformanceWaitTime = p_waitTime;
            }
        }

        float _lowPerformanceWaitTime = 0;
        bool _ignoreNextLayoutRebuild = false;
        protected virtual IEnumerator SetLowPerformanceInEndOfFrameRoutine(float p_waitTime)
        {
            _lowPerformanceWaitTime = p_waitTime;
            while (_lowPerformanceWaitTime > 0)
            {
                _lowPerformanceWaitTime -= Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForEndOfFrame(); //Wait until finish draw this cycle
            s_isEndOfFrame = true;

            _isHighPerformance = false;

            CallOnBeforeSetPerformance();

            if (m_canControlFps)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = (int)Mathf.Max(5, MinimumSupportedFps, m_performanceFpsRange.x);
            }
            else
                Application.targetFrameRate = -1;

            if (OnSetLowPerformance != null)
                OnSetLowPerformance();

            ClearPendentInvalidTransforms();

            _ignoreNextLayoutRebuild = true;

            CallOnAfterSetPerformance();

            _routineSetLowPerformance = null;
        }

        protected virtual void CancelSetHighPerformance()
        {
            _highPerformanceAutoDisable = false;
            _highPerformanceWaitTime = 0;
            if (_routineSetHighPerformance != null)
            {
                StopCoroutine(_routineSetHighPerformance);
                _routineSetHighPerformance = null;
            }
        }

        protected virtual void SetHighPerformanceImmediate(bool p_autoDisable = true)
        {
            CancelSetLowPerformance();
            CancelSetHighPerformance();
            var bufferIsDirty = _bufferIsDirty;
            var routine = SetHighPerformanceInEndOfFrameRoutine(bufferIsDirty, p_autoDisable, 0, true);
            while (routine.MoveNext()) { };
        }

        bool _highPerformanceAutoDisable = false;
        float _highPerformanceWaitTime = 0;
        Coroutine _routineSetHighPerformance = null;
        protected virtual bool SetHighPerformanceDelayed(bool p_autoDisable = true, float p_waitTime = 0)
        {
            CancelSetLowPerformance();
            if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
            {
                if (_routineSetHighPerformance == null || _bufferIsDirty)
                {
                    CancelSetHighPerformance();
                    var bufferIsDirty = _bufferIsDirty;
                    _routineSetHighPerformance = StartCoroutine(SetHighPerformanceInEndOfFrameRoutine(bufferIsDirty, p_autoDisable, p_waitTime));
                }
                else
                {
                    _highPerformanceWaitTime = p_waitTime;
                    _highPerformanceAutoDisable = p_autoDisable || _highPerformanceAutoDisable;
                }
                return true;
            }
            else
            {
                CancelSetHighPerformance();
                _performanceIsDirty = true;
            }
            return false;
        }

        protected virtual IEnumerator SetHighPerformanceInEndOfFrameRoutine(bool bufferIsDirty, bool p_autoDisable = true, float p_waitTime = 0, bool skipEndOfFrame = false)
        {
            var invalidCullingMask = s_invalidCullingMask;
            bufferIsDirty = _bufferIsDirty || bufferIsDirty;

            _highPerformanceAutoDisable = p_autoDisable;
            _highPerformanceWaitTime = p_waitTime;
            while (_highPerformanceWaitTime > 0)
            {
                _highPerformanceWaitTime -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!skipEndOfFrame)
            {
                yield return new WaitForEndOfFrame(); //Wait until finish draw this cycle
                s_isEndOfFrame = true;
            }

            _isHighPerformance = true;

            CallOnBeforeSetPerformance();

            if (m_canControlFps)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = (int)Mathf.Max(5, MinimumSupportedFps, m_performanceFpsRange.y);
            }
            else
                Application.targetFrameRate = -1;

            IEnumerator onAfterDrawBufferEnumerator = null;
            bufferIsDirty = _bufferIsDirty || bufferIsDirty;
            if (bufferIsDirty)
            {
                if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
                {
                    invalidCullingMask |= s_invalidCullingMask;
                    //s_isWaitingRenderBuffer = s_useRenderBuffer && m_renderBufferMode == RenderBufferModeEnum.Delayed && !s_requireConstantRepaint && !m_forceAlwaysInvalidate;
                    onAfterDrawBufferEnumerator = OnAfterDrawBufferRoutine(invalidCullingMask);
                }
            }

            if (OnSetHighPerformance != null)
                OnSetHighPerformance(bufferIsDirty);

            ClearPendentInvalidTransforms();

            _bufferIsDirty = false;
            _ignoreNextLayoutRebuild = true;

            CallOnAfterSetPerformance();

            _routineSetHighPerformance = null;

            //Draw Buffer
            if (onAfterDrawBufferEnumerator != null)
                yield return onAfterDrawBufferEnumerator;

            if (_highPerformanceAutoDisable)
                SetLowPerformanceDelayed(m_autoDisableHighPerformanceTime);
            else
                CancelSetLowPerformance();
        }

        protected virtual void ClearPendentInvalidTransforms(bool clearInvalidCullingMask = true)
        {
            if (UseSafeRefreshMode)
                s_invalidComplexShape = null;
            else
            {
                if (s_invalidComplexShape != null)
                    s_invalidComplexShape.Shapes.Clear();
                else
                    s_invalidComplexShape = new ComplexShape();
            }

            s_pendentInvalidRectTransforms.Clear();
            if (clearInvalidCullingMask)
                s_invalidCullingMask = _defaultInvalidCullingMask;
        }

        protected virtual void CallOnBeforeSetPerformance()
        {
            if (OnBeforeSetPerformance != null)
                OnBeforeSetPerformance.Invoke();
        }


        protected virtual void CallOnAfterSetPerformance()
        {
            if (OnAfterSetPerformance != null)
                OnAfterSetPerformance.Invoke();

            UpdateLowPerformanceViewActiveStatus();
        }

        protected IEnumerator OnAfterDrawBufferRoutine(int p_invalidCullingMask)
        {
            p_invalidCullingMask |= s_invalidCullingMask;
            //Wait one cycle to draw to a RenderTexture
            /*if (s_isWaitingRenderBuffer)
            {
                var bufferCameraViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);

                //Reset camera RenderTextures
                foreach (var v_cameraView in bufferCameraViews)
                {
                    if (v_cameraView != null && v_cameraView.Camera != null)
                        v_cameraView.Camera.targetTexture = null;
                }

                //Wait two cycles before DrawBuffer
                _ignoreNextLayoutRebuild = true;
                yield return null;
                s_isWaitingRenderBuffer = false;

                yield return new WaitForEndOfFrame();

                //Call finish event
                if (OnAfterWaitingToPrepareRenderBuffer != null)
                    OnAfterWaitingToPrepareRenderBuffer(p_invalidCullingMask);

                bufferCameraViews = PrepareCameraViewsToDrawInBuffer(p_invalidCullingMask);
                DrawCameraViewsWithRenderBufferState(bufferCameraViews, true);
                s_isEndOfFrame = true;

                _ignoreNextLayoutRebuild = true;
                //Finish Processing Buffer
            }
            else
            {*/
            CheckBufferTextures();
            var bufferCameraViews = PrepareCameraViewsToDrawInBuffer(p_invalidCullingMask);
            if (s_isEndOfFrame)
                DrawCameraViewsWithRenderBufferState(bufferCameraViews, true);
            else
                yield return new WaitForEndOfFrame();
            s_isEndOfFrame = true;
            //}
            //s_isWaitingRenderBuffer = false;

            if (OnAfterDrawBuffer != null)
                OnAfterDrawBuffer(new Dictionary<int, RenderTexture>(s_renderBufferDict));

            UpdateLowPerformanceViewActiveStatus();
        }

        protected IList<SustainedCameraView> PrepareCameraViewsToDrawInBuffer(int p_invalidCullingMask)
        {
            //prepare all cameras draw into RenderBuffer changing the render texture target based in index
            var v_cameraViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);

            var v_invalidCameraViews = new List<SustainedCameraView>();
            foreach (var v_cameraView in v_cameraViews)
            {
                if (v_cameraView != null && IsCameraViewInvalid(v_cameraView, p_invalidCullingMask))
                {
                    var v_renderBufferIndex = v_cameraView.RenderBufferIndex;
                    RenderTexture v_renderTexture = null;
                    s_renderBufferDict.TryGetValue(v_renderBufferIndex, out v_renderTexture);
                    v_cameraView.Camera.targetTexture = v_renderTexture;
                    v_invalidCameraViews.Add(v_cameraView);
                }
            }
            return v_invalidCameraViews;
        }

        protected void DrawCameraViewsWithRenderBufferState(IList<SustainedCameraView> cameraViews, bool isBuffer)
        {
            //prepare all cameras draw into RenderBuffer changing the render texture target based in index
            if (cameraViews == null)
                cameraViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(isBuffer);

            foreach (var cameraView in cameraViews)
            {
                if (cameraView != null)
                {
                    if (cameraView.IsDirty())
                        cameraView.Camera.Render();
                }
            }
        }

        protected void UpdateLowPerformanceViewActiveStatus()
        {
            if (s_canGenerateLowPerformanceView && Application.isPlaying)
            {
                var v_activateLowPerformance = !HasAnyCameraRenderingToScreen();

                if (s_lowPerformanceView == null && v_activateLowPerformance)
                    ConfigureLowPerformanceView();

                if (s_lowPerformanceView != null)
                    s_lowPerformanceView.SetViewActive(v_activateLowPerformance);
            }
            else if (s_lowPerformanceView != null)
                ConfigureLowPerformanceView();
        }

        protected bool HasAnyCameraRenderingToScreen()
        {
            var v_allCameras = Camera.allCameras;
            return HasAnyCameraRenderingToScreen(ref v_allCameras);
        }

        protected bool HasAnyCameraRenderingToScreen(ref Camera[] p_allCameras)
        {
            if (p_allCameras == null)
                p_allCameras = Camera.allCameras;
            foreach (var v_camera in p_allCameras)
            {
                if (v_camera != null && (s_lowPerformanceView == null || v_camera != s_lowPerformanceView.Camera) && v_camera.enabled && v_camera.targetTexture == null)
                    return true;
            }
            return false;
        }

        #endregion

        #region Public Static Functions

        static int s_invalidCullingMask = 0;
        public static void Invalidate(Transform p_transform, bool p_includeChildrens)
        {
            var v_cullingMasks = 0;
            if (p_transform != null && s_invalidCullingMask != ~0)
            {
                List<Transform> v_transforms = new List<Transform>();
                if (p_includeChildrens)
                    v_transforms.AddRange(p_transform.GetComponentsInChildren<Transform>());
                else
                    v_transforms.Add(p_transform);

                //Calculate tha InvalidLayer
                for (int i = 0; i < v_transforms.Count; i++)
                {
                    v_cullingMasks |= 1 << v_transforms[i].gameObject.layer;
                }
            }
            else
                v_cullingMasks = ~0;
            Invalidate(v_cullingMasks);
        }

        public static void Invalidate(ISustainedElement p_sustainedElement)
        {
            LayerMask v_cullingMask = ~0;
            if (p_sustainedElement != null && !p_sustainedElement.IsDestroyed())
                v_cullingMask = p_sustainedElement.CullingMask;

            Invalidate(v_cullingMask);
        }

        public static void Invalidate(LayerMask p_cullingMask)
        {
            if (s_invalidCullingMask == ~0 || p_cullingMask == ~0)
                s_invalidCullingMask = ~0;
            else
                s_invalidCullingMask |= p_cullingMask;
            MarkToSetHighPerformance(true);
        }

        public static void Invalidate()
        {
            Invalidate(~0);
        }

        public static void Refresh<T>(IList<T> p_senders)
        {
            var v_invalidateAll = p_senders == null || p_senders.Count == 0 || UseSafeRefreshMode;
            if (!v_invalidateAll)
            {
                for (int i = 0; i < p_senders.Count; i++)
                {
                    var v_senderTransform = (p_senders[i] as Component) != null ? (p_senders[i] as Component).transform as RectTransform : null;
                    if (v_senderTransform == null)
                    {
                        v_invalidateAll = true;
                        break;
                    }
                    else
                    {
                        s_pendentInvalidRectTransforms.Add(v_senderTransform);
                    }
                }
            }

            if (v_invalidateAll)
            {
                s_pendentInvalidRectTransforms.Clear();
                s_invalidComplexShape = null;
            }

            MarkToSetHighPerformance(false);
        }

        static HashSet<RectTransform> s_pendentInvalidRectTransforms = new HashSet<RectTransform>();
        public static void Refresh(Component p_sender = null)
        {
            RectTransform v_senderTransform = !UseSafeRefreshMode && p_sender != null ? p_sender.transform as RectTransform : null;
            //We set ComplexShape to null to force all CanvasViews to refresh
            if (v_senderTransform == null)
                s_invalidComplexShape = null;
            else if (s_invalidComplexShape != null && !s_pendentInvalidRectTransforms.Contains(v_senderTransform))
            {
                s_pendentInvalidRectTransforms.Add(v_senderTransform);
            }

            MarkToSetHighPerformance(false);
        }

        public static void Refresh(Rect p_invalidationScreenRect)
        {
            if (!UseSafeRefreshMode)
            {
                if (s_invalidComplexShape != null)
                    s_invalidComplexShape.AddShape(p_invalidationScreenRect);
            }
            else
                s_invalidComplexShape = null;

            MarkToSetHighPerformance(false);
        }

        public static void MarkToSetLowPerformance()
        {
            var v_instance = GetInstanceFastSearch();
            if (v_instance != null && v_instance._isHighPerformance)
                v_instance.SetLowPerformanceDelayed(0.1f);
        }

        public static bool IsHighPerformanceActive()
        {
            var v_instance = GetInstanceFastSearch();
            if (v_instance != null)
                return v_instance._isHighPerformance;

            return true;
        }

        public static bool IsActive()
        {
            var v_instance = GetInstanceFastSearch();
            return v_instance != null && v_instance.enabled && v_instance.gameObject.activeInHierarchy;
        }

        #endregion

        #region Other Internal Static Functions

        protected static void MarkToSetHighPerformance(bool p_invalidateBuffer)
        {
            var v_instance = GetInstanceFastSearch();
            if (v_instance != null)
            {
                var v_bufferIsDirty = v_instance._bufferIsDirty || p_invalidateBuffer || RequiresConstantBufferRepaint;

                //We must FORCE call SetHighPerformance Event (Buffer changed state)
                if (v_bufferIsDirty != v_instance._bufferIsDirty)
                {
                    v_instance._bufferIsDirty = v_bufferIsDirty;
                    v_instance.CancelSetLowPerformance();
                    v_instance._isHighPerformance = false;
                }
                //Invalidate all Cavas
                if (v_bufferIsDirty)
                    s_invalidComplexShape = null;

                if (!v_instance._isHighPerformance || !v_instance.m_safeRefreshMode)
                    v_instance._performanceIsDirty = true;
                else
                    v_instance.SetLowPerformanceDelayed(v_instance.m_autoDisableHighPerformanceTime);
            }
        }

        protected internal static bool IsCameraViewInvalid(SustainedCameraView p_cameraView)
        {
            return IsCameraViewInvalid(p_cameraView, s_invalidCullingMask);
        }

        protected internal static bool IsCameraViewInvalid(SustainedCameraView p_cameraView, int p_cullingMask)
        {
            return p_cameraView != null && (!p_cameraView.UseRenderBuffer || (int)(p_cameraView.CullingMask & p_cullingMask) != 0);
        }

        protected internal static bool IsCanvasViewInvalid(SustainedCanvasView p_canvasView)
        {
            if (s_invalidComplexShape == null || UseSafeRefreshMode || !p_canvasView.IsScreenCanvasMember())
                return true;

            if (p_canvasView != null)
            {
                var v_canvasViewScreenRect = ScreenRectUtils.GetScreenRect(p_canvasView.transform as RectTransform);
                if (v_canvasViewScreenRect.width > 0 && v_canvasViewScreenRect.height > 0)
                    UpdateInvalidShape();

                //If Intersection exists or complexShape is null
                var v_result = !s_invalidComplexShape.Intersection(v_canvasViewScreenRect).IsEmpty();
                return v_result;
            }
            return false;
        }

        static ComplexShape s_invalidComplexShape = new ComplexShape();
        protected static ComplexShape UpdateInvalidShape()
        {
            if (s_invalidComplexShape != null)
            {
                foreach (var v_invalidRectTransform in s_pendentInvalidRectTransforms)
                {
                    s_invalidComplexShape.AddShape(ScreenRectUtils.GetScreenRect(v_invalidRectTransform), false);
                }
                s_invalidComplexShape.Optimize();
            }
            //Always need to clear invalid transforms (this field is always temporary)
            s_pendentInvalidRectTransforms.Clear();

            return s_invalidComplexShape;
        }

        #endregion

        #region RenderView Important Functions

        protected virtual void ConfigureLowPerformanceView()
        {
            if (s_canGenerateLowPerformanceView && Application.isPlaying)
            {
                if (s_lowPerformanceView == null)
                {
                    var _lowPerformanceGameObject = new GameObject("LowPerformanceView");
                    _lowPerformanceGameObject.gameObject.SetActive(false);
                    //Simulate KeepFrameBuffer (useful in Metal) using LowPerformanceCameraView
                    s_lowPerformanceView = _lowPerformanceGameObject.AddComponent<LowPerformanceView>();
                }
            }
            else if (s_lowPerformanceView != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(s_lowPerformanceView.gameObject);
                else
                    GameObject.DestroyImmediate(s_lowPerformanceView.gameObject);
            }
        }

        protected virtual void TryAutoCreateRenderViews()
        {
            if (Application.isPlaying && m_autoConfigureRenderViews)
            {
                var v_allCameras = Resources.FindObjectsOfTypeAll<Camera>();
                foreach (var v_camera in v_allCameras)
                {
                    if (v_camera != null && (s_lowPerformanceView == null || v_camera != s_lowPerformanceView.Camera) && v_camera.gameObject.scene.IsValid())
                    {
                        if (v_camera.GetComponent<SustainedRenderView>() == null) //Yeah, we must check for base class
                            v_camera.gameObject.AddComponent<SustainedCameraView>();
                    }
                }

                var v_allCanvas = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (var v_canvas in v_allCanvas)
                {
                    if (v_canvas != null &&
                        (v_canvas.transform.parent == null || v_canvas.transform.parent.GetComponentInParent<Canvas>() == null)
                        && v_canvas.gameObject.scene.IsValid())

                    //if (v_canvas != null && v_canvas.gameObject.scene.IsValid())
                    {
                        if (v_canvas.GetComponent<SustainedRenderView>() == null) //Yeah, we must check for base class
                            v_canvas.gameObject.AddComponent<SustainedCanvasView>();
                    }
                }
                Invalidate();
            }
        }

        protected static bool CheckBufferTextures()
        {
            var allCameras = Camera.allCameras;
            var v_sucess = false;
            //Fill all Used indexes of render buffer in scene
            HashSet<int> v_validBufferIndexes = new HashSet<int>();
            var v_sceneViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);
            foreach (var v_renderViews in v_sceneViews)
            {
                var v_cameraView = v_renderViews as SustainedCameraView;
                if (v_cameraView != null)
                {
                    var v_validIndex = v_cameraView.RenderBufferIndex;
                    v_validBufferIndexes.Add(v_validIndex);
                    //Create Buffer Index Entry
                    if (!s_renderBufferDict.ContainsKey(v_validIndex) && Application.isPlaying)
                        s_renderBufferDict[v_validIndex] = null;
                }
            }

            //Check each of this index to try update renderbuffer textures
            var v_bufferIndexes = new HashSet<int>(s_renderBufferDict.Keys);
            foreach (var v_bufferIndex in v_bufferIndexes)
            {
                //Remove Unused Render Buffers
                var v_renderBuffer = s_renderBufferDict[v_bufferIndex];
                if (!v_validBufferIndexes.Contains(v_bufferIndex))
                {
                    s_renderBufferDict.Remove(v_bufferIndex);
                    ReleaseRenderBuffer(ref v_renderBuffer, allCameras);
                }
                //Update Render Buffer Texture
                else
                {
                    v_sucess = CheckBufferTexture(ref v_renderBuffer, allCameras) || v_sucess;
                    s_renderBufferDict[v_bufferIndex] = v_renderBuffer;
                }
            }

            return v_sucess;
        }

        protected static bool ReleaseRenderBuffers()
        {
            var allCameras = Camera.allCameras;
            var v_sucess = false;
            var v_bufferIndexes = new HashSet<int>(s_renderBufferDict.Keys);
            foreach (var v_bufferIndex in v_bufferIndexes)
            {
                var v_renderBuffer = s_renderBufferDict[v_bufferIndex];
                v_sucess = ReleaseRenderBuffer(ref v_renderBuffer, allCameras) || v_sucess;
            }
            s_renderBufferDict.Clear();

            return v_sucess;
        }

        protected static bool CheckBufferTexture(ref RenderTexture p_renderBuffer, Camera[] p_cameras = null)
        {
            return CheckBufferTexture(ref p_renderBuffer, s_useRenderBuffer, p_cameras);
        }

        protected static bool CheckBufferTexture(ref RenderTexture p_renderBuffer, bool p_isActive, Camera[] p_cameras = null)
        {
            if (Application.isPlaying)
            {
                if (p_isActive)
                {
                    if (p_renderBuffer == null || !p_renderBuffer.IsCreated() ||
                        p_renderBuffer.width != Screen.width || p_renderBuffer.height != Screen.height)
                    {
                        ReleaseRenderBuffer(ref p_renderBuffer, p_cameras);

                        //s_renderBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                        p_renderBuffer = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                        p_renderBuffer.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
                        p_renderBuffer.name = "RenderBuffer (" + p_renderBuffer.GetInstanceID() + ")";
                        p_renderBuffer.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                        p_renderBuffer.Create();
                        s_instance._bufferIsDirty = true;

                        return true;
                    }
                }
                else
                    ReleaseRenderBuffer(ref p_renderBuffer, p_cameras);
            }
            return false;
        }

        protected static bool ReleaseRenderBuffer(ref RenderTexture p_renderBuffer, Camera[] p_cameras = null)
        {
            if (p_renderBuffer != null)
            {
                if (p_renderBuffer == RenderTexture.active)
                    RenderTexture.active = null;
                if (p_cameras == null)
                    p_cameras = Camera.allCameras;//Resources.FindObjectsOfTypeAll<Camera>();
                foreach (var v_camera in p_cameras)
                {
                    if (v_camera != null && v_camera.scene.IsValid() && v_camera.activeTexture == p_renderBuffer || v_camera.targetTexture == p_renderBuffer)
                    {
                        v_camera.targetTexture = null;
                    }
                }

                //RenderTexture.ReleaseTemporary(s_renderBuffer);
                if (p_renderBuffer.IsCreated())
                    p_renderBuffer.Release();
                RenderTexture.Destroy(p_renderBuffer);
                p_renderBuffer = null;

                return true;
            }
            return false;
        }

        #endregion

        #region SustainedElement Utils

        protected virtual void TryCheckDynamicElements()
        {
            if (s_dynamicElementsDirty)
            {
                s_dynamicElementsDirty = false;
                CheckDynamicElements();
            }
        }

        protected virtual void CheckDynamicElements()
        {
            var v_oldDefaultCullingMask = _defaultInvalidCullingMask;
            var v_oldMinimumSupportedFps = s_minimumSupportedFps;
            var v_oldRequireConstantRepaint = s_requireConstantRepaint;
            var v_oldRequireConstantBufferRepaint = s_requireConstantBufferRepaint;
            var v_oldUseRenderBuffer = s_useRenderBuffer;

            //In this mode we want to invalidate all layers
            if (m_forceAlwaysInvalidate)
                _defaultInvalidCullingMask = ~0;
            else
                _defaultInvalidCullingMask = 0;

            s_requireConstantRepaint = m_forceAlwaysInvalidate;
            s_requireConstantBufferRepaint = m_forceAlwaysInvalidate;
            s_minimumSupportedFps = 0;
            s_useRenderBuffer = false;
            foreach (var v_element in s_elements)
            {
                if (!v_element.IsDestroyed())
                {
                    var v_elementBufferConstantRepaint = (!v_element.IsScreenCanvasMember() && v_element.RequiresConstantRepaint);
                    s_minimumSupportedFps = Mathf.Max(s_minimumSupportedFps, v_element.MinimumSupportedFps);
                    s_requireConstantRepaint = s_requireConstantRepaint || v_element.RequiresConstantRepaint;
                    s_requireConstantBufferRepaint = s_requireConstantBufferRepaint || v_elementBufferConstantRepaint;
                    s_useRenderBuffer = s_useRenderBuffer || v_element.UseRenderBuffer;

                    //We want to add contant repaint element culling masks in defaultInvalidCullingMask
                    if (v_elementBufferConstantRepaint)
                        _defaultInvalidCullingMask |= v_element.CullingMask;
                }
            }
            s_minimumSupportedFps = Mathf.Clamp(s_minimumSupportedFps, (int)m_performanceFpsRange.x, (int)m_performanceFpsRange.y);

            if (v_oldRequireConstantRepaint != s_requireConstantRepaint ||
                v_oldRequireConstantBufferRepaint != s_requireConstantBufferRepaint ||
                v_oldMinimumSupportedFps != s_minimumSupportedFps ||
                v_oldUseRenderBuffer != s_useRenderBuffer ||
                v_oldDefaultCullingMask != _defaultInvalidCullingMask)
            {
                if (s_useRenderBuffer)
                    Invalidate(_defaultInvalidCullingMask);
                else
                    Refresh();
            }
        }

        static HashSet<ISustainedElement> s_elements = new HashSet<ISustainedElement>();
        public static void RegisterDynamicElement(ISustainedElement p_element)
        {
            if (!s_elements.Contains(p_element))
            {
                s_elements.Add(p_element);
                MarkDynamicElementsDirty();
            }
        }

        public static void UnregisterDynamicElement(ISustainedElement p_element)
        {
            if (s_elements.Contains(p_element))
            {
                s_elements.Remove(p_element);
                MarkDynamicElementsDirty();
            }
        }

        static bool s_dynamicElementsDirty = true;
        protected internal static void MarkDynamicElementsDirty()
        {
            s_dynamicElementsDirty = true;
        }

        #endregion
    }

    #region Helper Interfaces

    public interface ISustainedElement
    {
        bool UseRenderBuffer { get; }
        bool RequiresConstantRepaint { get; }
        int MinimumSupportedFps { get; }
        int CullingMask { get; }

        bool IsScreenCanvasMember();
        bool IsDestroyed();
    }

    #endregion
}