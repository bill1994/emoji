#if UNITY_2019_3_OR_NEWER
#define SUPPORT_ONDEMAND_RENDERING
#endif

#if SUPPORT_ONDEMAND_RENDERING
using UnityEngine.Rendering;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub;
using System;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace Kyub.Performance
{
    public class SustainedPerformanceManager : Kyub.Singleton<SustainedPerformanceManager>
    {
        public enum RenderTechniqueEnum { PreserveFrameBuffer = 1, ForceSimulateFrameBuffer = 2}
        
        [System.Flags]
        public enum FpsTechniqueEnum { TargetFps = 1, OnDemandRendering = 2 }
        #region Static Events

        public static event Action OnBeforeSetPerformance; //Before Process Performances (Useful to know if LowPerformanceView will be activated)
        public static event Action<bool> OnSetHighPerformance;
        public static event Action OnSetLowPerformance;
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

        //Used to scale down render buffer
        static Dictionary<int, int> s_maxSizePerBufferIndex = new Dictionary<int, int>();
        public static int GetRenderBufferMaxSize(int bufferIndex)
        {
            if (s_maxSizePerBufferIndex.ContainsKey(bufferIndex))
                return s_maxSizePerBufferIndex[bufferIndex];

            var instance = GetInstanceFastSearch();
            return instance != null ? instance.m_maxRenderBufferSize : -1;
        }

        public static int GetDepthBufferSize()
        {
            var instance = GetInstanceFastSearch();
            return instance != null && instance.UseHighPrecDepthBuffer ? 32 : 24;
        }

        protected static Dictionary<int, RenderTexture> s_renderBufferDict = new Dictionary<int, RenderTexture>();
        public static RenderTexture GetRenderBuffer(int bufferIndex)
        {
            //if (IsWaitingRenderBuffer || (!UseImmediateRenderBufferMode && RequiresConstantRepaint))
            //    return null;

            RenderTexture renderBuffer = null;
            if (s_renderBufferDict.ContainsKey(bufferIndex))
            {
                renderBuffer = s_renderBufferDict[bufferIndex];
                if ((s_useRenderBuffer && renderBuffer == null) || (!s_useRenderBuffer && renderBuffer != null))
                {
                    CheckBufferTexture(ref renderBuffer, GetRenderBufferMaxSize(bufferIndex), GetDepthBufferSize());

                    if (renderBuffer != null)
                        s_renderBufferDict[bufferIndex] = renderBuffer;
                    else
                        s_renderBufferDict.Remove(bufferIndex);
                }
            }

            return renderBuffer;
        }

        public static bool UseOnDemandRendering
        {
            get
            {
                if (s_instance == null)
                    s_instance = GetInstanceFastSearch();

                return s_instance != null? s_instance.CanControlRenderingFps : false;
            }
        }

        public static bool UseSimulatedFrameBuffer
        {
            get
            {
                if (s_instance == null)
                    s_instance = GetInstanceFastSearch();


#if UNITY_EDITOR && UNITY_IOS
                return true;
#else
                var renderTechnique = s_instance != null ? s_instance.SustainedRenderTechnique : RenderTechniqueEnum.PreserveFrameBuffer;
                var forceSimulateFrameBuffer = renderTechnique.HasFlag(RenderTechniqueEnum.ForceSimulateFrameBuffer);
                //Metal/Mobile require simulate frameBuffer in Legacy Mode
                return forceSimulateFrameBuffer ||
                    SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal ||
                    Application.isMobilePlatform ||
                    Application.platform == RuntimePlatform.WebGLPlayer;
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

        public static bool UseSafeRefreshMode
        {
            get
            {
                var instance = GetInstanceFastSearch();
                if (instance != null)
                    return instance.m_safeRefreshMode;

                return true;
            }
        }

        protected static bool s_requireConstantRepaint = false;
        public static bool RequiresConstantRepaint
        {
            get
            {
                var instance = GetInstanceFastSearch();
                if (instance != null)
                    return s_requireConstantRepaint || instance.m_forceAlwaysInvalidate;

                return s_requireConstantRepaint;
            }
        }

        protected static bool s_requireConstantBufferRepaint = false;
        public static bool RequiresConstantBufferRepaint
        {
            get
            {
                var instance = GetInstanceFastSearch();
                if (instance != null)
                    return s_requireConstantBufferRepaint || instance.m_forceAlwaysInvalidate;

                return s_requireConstantBufferRepaint;
            }
        }

        protected static int s_minimumSupportedFps = 5;
        public static int MinimumSupportedFps
        {
            get
            {
                var instance = GetInstanceFastSearch();
                if (instance != null)
                    return Mathf.Clamp(s_minimumSupportedFps, (int)instance.m_targetFpsRange.x, (int)instance.m_targetFpsRange.y);

                return s_minimumSupportedFps;
            }
        }

        #endregion

        #region Private Variables

        [SerializeField, Tooltip("This property will try to add a SustainedRenderView on each Camera/Canvas in Scene")]
        protected bool m_autoConfigureRenderViews = true;
        [SerializeField, Tooltip("Change the Adaptative Render Mode Technique.\n* PreserveFrameBuffer: Draw with ClearFlag None (only works in stadalone and will fallback to 'ForceSimulateFrameBuffer' in Mobile).\n* ForceSimulateFrameBuffer: Take Screenshot of the Screen to simulate ClearFlag None in Non-Supported Platforms.")]
        protected RenderTechniqueEnum m_sustainedRenderTechnique = RenderTechniqueEnum.ForceSimulateFrameBuffer;
        [Space]
        [SerializeField, Tooltip("This property will force invalidate even if not emitted by this canvas hierarchy (Prevent Alpha-Overlap bug)")]
        protected bool m_safeRefreshMode = true;
        [Space]
        [SerializeField, Tooltip("Useful for debug purpose (will force invalidate every frame)")]
        protected bool m_forceAlwaysInvalidate = false;
        [Space]
        [SerializeField, IntPopup(-1, "ScreenSize", 16, 32, 64, 128, 256, 512, 768, 1024, 1280, 1536, 1792, 2048, 2560, 3072, 4096)]
        protected int m_maxRenderBufferSize = -1;
        [SerializeField, Tooltip("Enable 32bits Depth Buffer")]
        protected bool m_useHighPrecDepthBuffer = false;

        [Space]
        [SerializeField, Tooltip("DynamicFps Controller Options.\n* TargetFps: reduce fps from all threads.\n* OnDemandRendering: reduce FPS from render thread")]
        protected FpsTechniqueEnum m_fpsTechnique = (FpsTechniqueEnum)(-1);
        [Space]
        [SerializeField, MinMaxSlider(5, 150)]
        protected Vector2 m_targetFpsRange = new Vector2(30, 60);
        [Space]
        [SerializeField, Range(5, 150), Tooltip("When OnDemandRendering is active, this value will be used when low performance is active. Set 1 to draw all frame.")]
        protected int m_renderingTargetFps = 30;
        [Space]
        [SerializeField, Range(0.5f, 5.0f)]
        protected float m_autoDisableHighPerformanceTime = 1.0f;

        protected int _defaultInvalidCullingMask = 0;
        protected bool _performanceIsDirty = false;
        protected bool _bufferIsDirty = false;
        protected bool _isHighPerformance = true;

        protected internal static LowPerformanceView s_lowPerformanceView = null;
        protected static bool s_canGenerateLowPerformanceView = false;
        protected static Vector2Int s_lastScreenSize = Vector2Int.zero;

        #endregion

        #region Properties

        public bool UseHighPrecDepthBuffer
        {   
            get
            {
                return m_useHighPrecDepthBuffer;
            }
            set
            {
                if (m_useHighPrecDepthBuffer == value)
                    return;
                m_useHighPrecDepthBuffer = value;

                ReleaseRenderBuffers();
                CheckBufferTextures();
                Invalidate();
            }
        }

        public RenderTechniqueEnum SustainedRenderTechnique
        {
            get
            {
#if !SUPPORT_ONDEMAND_RENDERING
                if (m_sustainedRenderTechnique.HasFlag(RenderTechniqueEnum.OnDemandRendering))
                {
                    //Clear OnDemandRendering Flag
                    var renderTechnique = m_sustainedRenderTechnique & ~RenderTechniqueEnum.OnDemandRendering;
                    if (Application.isPlaying)
                        m_sustainedRenderTechnique = renderTechnique;
                    else
                        return renderTechnique;
                }
#endif
                return m_sustainedRenderTechnique;
            }
            set
            {
                if (m_sustainedRenderTechnique == value)
                    return;
                m_sustainedRenderTechnique = value;

                CheckBufferTextures();
                Invalidate();
            }
        }

        public FpsTechniqueEnum FpsTechnique
        {
            get
            {
                var isPlaying = Application.isPlaying;
                var fpsTechnique = m_fpsTechnique;
#if !SUPPORT_ONDEMAND_RENDERING
                if (m_fpsTechnique.HasFlag(FpsTechniqueEnum.OnDemandRendering))
                {
                    //Clear OnDemandRendering Flag
                    fpsTechnique = m_fpsTechnique & ~FpsTechniqueEnum.OnDemandRendering;
                    if (isPlaying)
                    {
                        m_fpsTechnique = fpsTechnique;
                    }
                }
#endif
                if (Application.platform == RuntimePlatform.WebGLPlayer && m_fpsTechnique.HasFlag(FpsTechniqueEnum.TargetFps))
                {
                    //Clear Fps Flag
                    fpsTechnique = m_fpsTechnique & ~FpsTechniqueEnum.TargetFps;
                    if (isPlaying)
                    {
                        m_fpsTechnique = fpsTechnique;
                    }
                }

                return isPlaying? m_fpsTechnique : fpsTechnique;
            }
            set
            {
                if (m_fpsTechnique == value)
                    return;
                m_fpsTechnique = value;

                if (enabled && gameObject.activeInHierarchy)
                    Refresh();
            }
        }

        public int MaxTextureSize
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
                Invalidate();
            }
        }

        public Vector2 TargetFpsRange
        {
            get
            {
                return m_targetFpsRange;
            }

            set
            {
                if (m_targetFpsRange == value)
                    return;
                m_targetFpsRange = value;

                if (enabled && gameObject.activeInHierarchy)
                    Refresh();
            }
        }

        public int RenderingTargetFps
        {
            get
            {
                return m_renderingTargetFps;
            }

            set
            {
                if (m_renderingTargetFps == value)
                    return;
                m_renderingTargetFps = value;

                if (enabled && gameObject.activeInHierarchy)
                    Refresh();
            }
        }

        public bool CanControlRenderingFps
        {
            get
            {
                return FpsTechnique.HasFlag(FpsTechniqueEnum.OnDemandRendering);
            }

            set
            {
                var result = m_fpsTechnique;
                if (value)
                {
                    result |= FpsTechniqueEnum.OnDemandRendering;
                }
                else
                {
                    result &= ~FpsTechniqueEnum.OnDemandRendering;
                }

                if (m_fpsTechnique != result)
                {
                    m_fpsTechnique = result;
                    if (enabled && gameObject.activeInHierarchy)
                        Refresh();
                }
            }
        }

        public bool CanControlFps
        {
            get
            {
                return FpsTechnique.HasFlag(FpsTechniqueEnum.TargetFps);
            }

            set
            {
                var result = m_fpsTechnique;
                if (value)
                {
                    result |= FpsTechniqueEnum.TargetFps;
                }
                else
                {
                    result &= ~FpsTechniqueEnum.TargetFps;
                }

                if (m_fpsTechnique != result)
                {
                    m_fpsTechnique = result;
                    if (enabled && gameObject.activeInHierarchy)
                        Refresh();
                }
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
                var renderTechnique = SustainedRenderTechnique;
                return renderTechnique.HasFlag(RenderTechniqueEnum.ForceSimulateFrameBuffer);
            }
        }

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

        protected override void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
        {
            base.OnSceneWasLoaded(scene, mode);
            MarkDynamicElementsDirty();
            TryAutoCreateRenderViews();

            Invalidate();
        }

        protected virtual void Update()
        {
            if (s_instance == this)
            {
                s_isEndOfFrame = false;
                /*#if UNITY_EDITOR
                                if (!_isHighPerformance )
                                {
                                    var editorForceInvalidate = false;
                                    var cameraCurrent = Camera.current;
                                    if (cameraCurrent != null && cameraCurrent.name == "SceneCamera" && !cameraCurrent.scene.IsValid())
                                        editorForceInvalidate = true;
                                    if (editorForceInvalidate)
                                    {
                                        _performanceIsDirty = true;
                                    }
                                }
                #endif*/
                TryCheckDynamicElements();
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
            m_targetFpsRange = new Vector2(Mathf.RoundToInt(m_targetFpsRange.x), Mathf.RoundToInt(m_targetFpsRange.y));
            RecalculateMaxTextureSize();
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
            if (_routineSetHighPerformance == null && (_performanceIsDirty || _bufferIsDirty))
            {
                OnPerformanceUpdate();
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

        protected virtual void SetLowPerformanceDelayed(float waitTime = 0)
        {
            CancelSetHighPerformance();
            if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
            {
                if (_lowPerformanceWaitTime <= 0)
                {
                    CancelSetLowPerformance();
                    _routineSetLowPerformance = StartCoroutine(SetLowPerformanceInEndOfFrameRoutine(waitTime));
                }
                else
                    _lowPerformanceWaitTime = waitTime;
            }
        }

        float _lowPerformanceWaitTime = 0;
        bool _ignoreNextLayoutRebuild = false;
        protected virtual IEnumerator SetLowPerformanceInEndOfFrameRoutine(float waitTime)
        {
            _lowPerformanceWaitTime = waitTime;
            while (_lowPerformanceWaitTime > 0)
            {
                _lowPerformanceWaitTime -= Time.unscaledDeltaTime;
                yield return null;
            }

#if SUPPORT_ONDEMAND_RENDERING
            //We must revert low performance this cicle because this can never finish if low performance is below than 1 fps
            OnDemandRendering.renderFrameInterval = 1;
#endif

            yield return new WaitForEndOfFrame(); //Wait until finish draw this cycle
            s_isEndOfFrame = true;

            _isHighPerformance = false;

            CallOnBeforeSetPerformance();

            var minFramerate = s_minimumSupportedFps;
            if (CanControlFps)
            {
                minFramerate = (int)Mathf.Max(5, MinimumSupportedFps, m_targetFpsRange.x);
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = minFramerate;
            }
            else
                Application.targetFrameRate = -1;

            if (OnSetLowPerformance != null)
                OnSetLowPerformance();

            ClearPendentInvalidTransforms();

            _ignoreNextLayoutRebuild = true;

            CallOnAfterSetPerformance();

#if SUPPORT_ONDEMAND_RENDERING
            var useOnDemandRendering = CanControlRenderingFps && !RequiresConstantRepaint && !RequiresConstantBufferRepaint;
            if (useOnDemandRendering)
            {
                //Calculate render frame interval
                var maxRenderFps = OnDemandRendering.effectiveRenderFrameRate * OnDemandRendering.renderFrameInterval;
                int targetRenderFrameInterval = RenderingTargetFps < maxRenderFps && RenderingTargetFps > 0?
                    (maxRenderFps) / RenderingTargetFps : 1;

                targetRenderFrameInterval = Mathf.Max(1, targetRenderFrameInterval);
                OnDemandRendering.renderFrameInterval = targetRenderFrameInterval;
            }
#endif

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

        protected virtual void SetHighPerformanceImmediate(bool autoDisable = true)
        {
            CancelSetLowPerformance();
            CancelSetHighPerformance();
            var bufferIsDirty = _bufferIsDirty;
            var routine = SetHighPerformanceInEndOfFrameRoutine(bufferIsDirty, autoDisable, 0, true);
            while (routine.MoveNext()) { };
        }

        bool _highPerformanceAutoDisable = false;
        float _highPerformanceWaitTime = 0;
        Coroutine _routineSetHighPerformance = null;
        protected virtual bool SetHighPerformanceDelayed(bool autoDisable = true, float waitTime = 0)
        {
            CancelSetLowPerformance();
            if (Application.isPlaying && enabled && gameObject.activeInHierarchy && _routineSetHighPerformance == null)
            {
                //if (_routineSetHighPerformance == null || _bufferIsDirty)
                //{
                CancelSetHighPerformance();
                var bufferIsDirty = _bufferIsDirty;
                _routineSetHighPerformance = StartCoroutine(SetHighPerformanceInEndOfFrameRoutine(bufferIsDirty, autoDisable, waitTime));
                //}
                //else
                //{
                //    _highPerformanceWaitTime = waitTime;
                //    _highPerformanceAutoDisable = autoDisable || _highPerformanceAutoDisable;
                //}
                return true;
            }
            else
            {
                CancelSetHighPerformance();
                _performanceIsDirty = true;
            }
            return false;
        }

        protected virtual IEnumerator SetHighPerformanceInEndOfFrameRoutine(bool bufferIsDirty, bool autoDisable = true, float waitTime = 0, bool skipEndOfFrame = false)
        {
#if SUPPORT_ONDEMAND_RENDERING
            OnDemandRendering.renderFrameInterval = 1;
#endif

            var invalidCullingMask = s_invalidCullingMask;
            bufferIsDirty = _bufferIsDirty || bufferIsDirty;

            _highPerformanceAutoDisable = autoDisable;
            _highPerformanceWaitTime = waitTime;
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

            _performanceIsDirty = false;
            _isHighPerformance = true;

            CallOnBeforeSetPerformance();

            if (CanControlFps)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = (int)Mathf.Max(5, MinimumSupportedFps, m_targetFpsRange.y);
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

            _performanceIsDirty = false;
            //if (!IsEndOfFrame)
            //    _ignoreNextLayoutRebuild = true;

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

        protected IEnumerator OnAfterDrawBufferRoutine(int invalidCullingMask)
        {
            invalidCullingMask |= s_invalidCullingMask;

            CheckBufferTextures();
            var bufferCameraViews = PrepareCameraViewsToDrawInBuffer(invalidCullingMask);
            if (s_isEndOfFrame)
                DrawCameraViewsWithRenderBufferState(bufferCameraViews, true);
            else
                yield return new WaitForEndOfFrame();
            s_isEndOfFrame = true;

            _bufferIsDirty = false;

            if (OnAfterDrawBuffer != null)
                OnAfterDrawBuffer(new Dictionary<int, RenderTexture>(s_renderBufferDict));

            UpdateLowPerformanceViewActiveStatus();
        }

        protected IList<SustainedCameraView> PrepareCameraViewsToDrawInBuffer(int invalidCullingMask)
        {
            //prepare all cameras draw into RenderBuffer changing the render texture target based in index
            var cameraViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);

            var invalidCameraViews = new List<SustainedCameraView>();
            foreach (var cameraView in cameraViews)
            {
                if (cameraView != null && IsCameraViewInvalid(cameraView, invalidCullingMask))
                {
                    var renderBufferIndex = cameraView.RenderBufferIndex;
                    RenderTexture renderTexture = null;
                    s_renderBufferDict.TryGetValue(renderBufferIndex, out renderTexture);
                    cameraView.Camera.targetTexture = renderTexture;
                    invalidCameraViews.Add(cameraView);
                }
            }
            return invalidCameraViews;
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
                var activateLowPerformance = !HasAnyCameraRenderingToScreen();

                if (s_lowPerformanceView == null && activateLowPerformance)
                    ConfigureLowPerformanceView();

                if (s_lowPerformanceView != null)
                    s_lowPerformanceView.SetViewActive(activateLowPerformance);
            }
            else if (s_lowPerformanceView != null)
                ConfigureLowPerformanceView();
        }

        protected bool HasAnyCameraRenderingToScreen()
        {
            var allCameras = Camera.allCameras;
            return HasAnyCameraRenderingToScreen(ref allCameras);
        }

        protected bool HasAnyCameraRenderingToScreen(ref Camera[] allCameras)
        {
            if (allCameras == null)
                allCameras = Camera.allCameras;
            foreach (var camera in allCameras)
            {
                if (camera != null && (s_lowPerformanceView == null || camera != s_lowPerformanceView.Camera) && camera.enabled && camera.targetTexture == null)
                    return true;
            }
            return false;
        }

        #endregion

        #region Public Static Functions

        static int s_invalidCullingMask = 0;
        public static void Invalidate(Transform transform, bool includeChildrens)
        {
            var cullingMasks = 0;
            if (transform != null && s_invalidCullingMask != ~0)
            {
                List<Transform> transforms = new List<Transform>();
                if (includeChildrens)
                    transforms.AddRange(transform.GetComponentsInChildren<Transform>());
                else
                    transforms.Add(transform);

                //Calculate tha InvalidLayer
                for (int i = 0; i < transforms.Count; i++)
                {
                    cullingMasks |= 1 << transforms[i].gameObject.layer;
                }
            }
            else
                cullingMasks = ~0;
            Invalidate(cullingMasks);
        }

        public static void Invalidate(ISustainedElement sustainedElement)
        {
            LayerMask cullingMask = ~0;
            if (sustainedElement != null && !sustainedElement.IsDestroyed())
                cullingMask = sustainedElement.CullingMask;

            Invalidate(cullingMask);
        }

        public static void Invalidate(LayerMask cullingMask)
        {
            if (s_invalidCullingMask == ~0 || cullingMask == ~0)
                s_invalidCullingMask = ~0;
            else
                s_invalidCullingMask |= cullingMask;
            MarkToSetHighPerformance(true);
        }

        public static void Invalidate()
        {
            Invalidate(~0);
        }

        public static void Refresh<T>(IList<T> senders)
        {
            var invalidateAll = senders == null || senders.Count == 0 || UseSafeRefreshMode;
            if (!invalidateAll)
            {
                for (int i = 0; i < senders.Count; i++)
                {
                    var senderTransform = (senders[i] as Component) != null ? (senders[i] as Component).transform as RectTransform : null;
                    if (senderTransform == null)
                    {
                        invalidateAll = true;
                        break;
                    }
                    else
                    {
                        s_pendentInvalidRectTransforms.Add(senderTransform);
                    }
                }
            }

            if (invalidateAll)
            {
                s_pendentInvalidRectTransforms.Clear();
                s_invalidComplexShape = null;
            }

            MarkToSetHighPerformance(false);
        }

        static HashSet<RectTransform> s_pendentInvalidRectTransforms = new HashSet<RectTransform>();
        public static void Refresh(Component sender = null)
        {
            RectTransform senderTransform = !UseSafeRefreshMode && sender != null ? sender.transform as RectTransform : null;
            //We set ComplexShape to null to force all CanvasViews to refresh
            if (senderTransform == null)
                s_invalidComplexShape = null;
            else if (s_invalidComplexShape != null && !s_pendentInvalidRectTransforms.Contains(senderTransform))
            {
                s_pendentInvalidRectTransforms.Add(senderTransform);
            }

            MarkToSetHighPerformance(false);
        }

        public static void Refresh(Rect invalidationScreenRect)
        {
            if (!UseSafeRefreshMode)
            {
                if (s_invalidComplexShape != null)
                    s_invalidComplexShape.AddShape(invalidationScreenRect);
            }
            else
                s_invalidComplexShape = null;

            MarkToSetHighPerformance(false);
        }

        public static void MarkToSetLowPerformance()
        {
            var instance = GetInstanceFastSearch();
            if (instance != null && instance._isHighPerformance)
                instance.SetLowPerformanceDelayed(0.1f);
        }

        public static bool IsHighPerformanceActive()
        {
            var instance = GetInstanceFastSearch();
            if (instance != null)
                return instance._isHighPerformance;

            return true;
        }

        public static bool IsActive()
        {
            var instance = GetInstanceFastSearch();
            return instance != null && instance.enabled && instance.gameObject.activeInHierarchy;
        }

        #endregion

        #region Other Internal Static Functions

        protected static void MarkToSetHighPerformance(bool invalidateBuffer)
        {
            var instance = GetInstanceFastSearch();
            if (instance != null)
            {
                var bufferIsDirty = instance._bufferIsDirty || invalidateBuffer || RequiresConstantBufferRepaint;

                //We must FORCE call SetHighPerformance Event (Buffer changed state)
                if (bufferIsDirty != instance._bufferIsDirty)
                {
                    instance._bufferIsDirty = bufferIsDirty;
                    instance.CancelSetLowPerformance();
                    instance._isHighPerformance = false;
                }
                //Invalidate all Cavas
                if (bufferIsDirty)
                    s_invalidComplexShape = null;

                if (!instance._isHighPerformance || !instance.m_safeRefreshMode)
                    instance._performanceIsDirty = true;
                else if (instance._isHighPerformance && instance._routineSetHighPerformance == null)
                {
                    var autoDisableTime = Mathf.Max(0.1f, instance.m_autoDisableHighPerformanceTime);
                    if (instance._lowPerformanceWaitTime <= 0 || instance._routineSetLowPerformance == null)
                        instance.SetLowPerformanceDelayed(autoDisableTime);
                    else
                        instance._lowPerformanceWaitTime = autoDisableTime;
                }
            }
        }

        protected internal static bool IsCameraViewInvalid(SustainedCameraView cameraView)
        {
            return IsCameraViewInvalid(cameraView, s_invalidCullingMask);
        }

        protected internal static bool IsCameraViewInvalid(SustainedCameraView cameraView, int cullingMask)
        {
            return cameraView != null && (!cameraView.UseRenderBuffer || (int)(cameraView.CullingMask & cullingMask) != 0);
        }

        protected internal static bool IsCanvasViewInvalid(SustainedCanvasView canvasView)
        {
            if (s_invalidComplexShape == null || UseSafeRefreshMode || !canvasView.IsScreenCanvasMember())
                return true;

            if (canvasView != null)
            {
                var canvasViewScreenRect = ScreenRectUtils.GetScreenRect(canvasView.transform as RectTransform);
                if (canvasViewScreenRect.width > 0 && canvasViewScreenRect.height > 0)
                    UpdateInvalidShape();

                //If Intersection exists or complexShape is null
                var result = !s_invalidComplexShape.Intersection(canvasViewScreenRect).IsEmpty();
                return result;
            }
            return false;
        }

        static ComplexShape s_invalidComplexShape = new ComplexShape();
        protected static ComplexShape UpdateInvalidShape()
        {
            if (s_invalidComplexShape != null)
            {
                foreach (var invalidRectTransform in s_pendentInvalidRectTransforms)
                {
                    s_invalidComplexShape.AddShape(ScreenRectUtils.GetScreenRect(invalidRectTransform), false);
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
                var allCameras = Resources.FindObjectsOfTypeAll<Camera>();
                foreach (var camera in allCameras)
                {
                    if (camera != null && (s_lowPerformanceView == null || camera != s_lowPerformanceView.Camera) && camera.gameObject.scene.IsValid())
                    {
                        if (camera.GetComponent<SustainedRenderView>() == null) //Yeah, we must check for base class
                            camera.gameObject.AddComponent<SustainedCameraView>();
                    }
                }

                var allCanvas = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (var canvas in allCanvas)
                {
                    if (canvas != null &&
                        (canvas.transform.parent == null || canvas.transform.parent.GetComponentInParent<Canvas>() == null)
                        && canvas.gameObject.scene.IsValid())

                    //if (canvas != null && canvas.gameObject.scene.IsValid())
                    {
                        if (canvas.GetComponent<SustainedRenderView>() == null) //Yeah, we must check for base class
                            canvas.gameObject.AddComponent<SustainedCanvasView>();
                    }
                }
                Invalidate();
            }
        }

        protected static bool CheckBufferTextures()
        {
            var sucess = false;
            //Fill all Used indexes of render buffer in scene
            HashSet<int> validBufferIndexes = new HashSet<int>();
            var sceneViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);
            foreach (var renderViews in sceneViews)
            {
                var cameraView = renderViews as SustainedCameraView;
                if (cameraView != null)
                {
                    var validIndex = cameraView.RenderBufferIndex;
                    validBufferIndexes.Add(validIndex);
                    //Create Buffer Index Entry
                    if (!s_renderBufferDict.ContainsKey(validIndex) && Application.isPlaying)
                        s_renderBufferDict[validIndex] = null;
                }
            }

            //Check each of this index to try update renderbuffer textures
            var bufferIndexes = new HashSet<int>(s_renderBufferDict.Keys);
            foreach (var bufferIndex in bufferIndexes)
            {
                //Remove Unused Render Buffers
                var renderBuffer = s_renderBufferDict[bufferIndex];
                if (!validBufferIndexes.Contains(bufferIndex))
                {
                    s_renderBufferDict.Remove(bufferIndex);
                    ReleaseRenderBuffer(ref renderBuffer, sceneViews);
                }
                //Update Render Buffer Texture
                else
                {
                    sucess = CheckBufferTexture(ref renderBuffer, GetRenderBufferMaxSize(bufferIndex), GetDepthBufferSize(), sceneViews) || sucess;
                    s_renderBufferDict[bufferIndex] = renderBuffer;
                }
            }

            return sucess;
        }

        protected static bool ReleaseRenderBuffers()
        {
            var cameraViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);
            var sucess = false;
            var bufferIndexes = new HashSet<int>(s_renderBufferDict.Keys);
            foreach (var bufferIndex in bufferIndexes)
            {
                var renderBuffer = s_renderBufferDict[bufferIndex];
                sucess = ReleaseRenderBuffer(ref renderBuffer, cameraViews) || sucess;
            }
            s_renderBufferDict.Clear();

            return sucess;
        }

        protected static bool CheckBufferTexture(ref RenderTexture renderBuffer, int maxSize, int depthBufferSize, ICollection<SustainedCameraView> cameraViews = null)
        {
            return CheckBufferTexture(ref renderBuffer, s_useRenderBuffer, maxSize, depthBufferSize, cameraViews);
        }

        protected static bool CheckBufferTexture(ref RenderTexture renderBuffer, bool isActive, int maxSize, int depthBufferSize, ICollection<SustainedCameraView> cameraViews = null)
        {
            if (Application.isPlaying)
            {
                if (isActive)
                {
                    var screenSize = GetScreenSizeClamped(maxSize);
                    if (renderBuffer == null || !renderBuffer.IsCreated() ||
                        renderBuffer.width != screenSize.x || renderBuffer.height != screenSize.y)
                    {
                        ReleaseRenderBuffer(ref renderBuffer, cameraViews);

                        renderBuffer = new RenderTexture(screenSize.x, screenSize.y, depthBufferSize, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                        renderBuffer.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
                        renderBuffer.name = "RenderBuffer (" + renderBuffer.GetInstanceID() + ")";
                        renderBuffer.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                        renderBuffer.Create();
                        s_instance._bufferIsDirty = true;

                        return true;
                    }
                }
                else
                    ReleaseRenderBuffer(ref renderBuffer, cameraViews);
            }
            return false;
        }

        protected static bool ReleaseRenderBuffer(ref RenderTexture renderBuffer, ICollection<SustainedCameraView> cameraViews = null)
        {
            if (renderBuffer != null)
            {
                if (renderBuffer == RenderTexture.active)
                    RenderTexture.active = null;
                if (cameraViews == null)
                    cameraViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);
                foreach (var camera in cameraViews)
                {
                    if (camera != null && camera.Camera != null && camera.gameObject.scene.IsValid() && camera.Camera.activeTexture == renderBuffer || camera.Camera.targetTexture == renderBuffer)
                    {
                        camera.Camera.targetTexture = null;
                    }
                }

                //RenderTexture.ReleaseTemporary(s_renderBuffer);
                if (renderBuffer.IsCreated())
                    renderBuffer.Release();
                RenderTexture.Destroy(renderBuffer);
                renderBuffer = null;

                return true;
            }
            return false;
        }

        protected static Vector2Int GetScreenSizeClamped(int maxSize)
        {
            var defaultScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (maxSize > 0)
            {
                maxSize = Mathf.Max(16, maxSize);
                if (maxSize < defaultScreenSize.x || maxSize < defaultScreenSize.y)
                {
                    float defaultMaxSize = Mathf.Max(defaultScreenSize.x, defaultScreenSize.y);
                    var multiplier = maxSize / defaultMaxSize;

                    defaultScreenSize = new Vector2Int((int)(Screen.width * multiplier), (int)(Screen.height * multiplier)); ;
                }
            }

            return defaultScreenSize;
        }

        protected static bool RecalculateMaxTextureSize(ICollection<SustainedCameraView> cameraViews = null)
        {
            var sucess = false;
            var newMaxSizePerBufferIndex = new Dictionary<int, int>();
            if (cameraViews == null)
                cameraViews = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(true);

            var instance = GetInstanceFastSearch();
            var instanceMaxRenderTextureSize = instance != null ? instance.m_maxRenderBufferSize : -1;
            foreach (var cameraView in cameraViews)
            {
                if (cameraView != null && cameraView.Camera != null && cameraView.UseRenderBuffer)
                {
                    //Get cached value
                    var cachedMaxSizePerBufferIndex = -1;
                    if (newMaxSizePerBufferIndex.ContainsKey(cameraView.RenderBufferIndex))
                        cachedMaxSizePerBufferIndex = newMaxSizePerBufferIndex[cameraView.RenderBufferIndex] <= 0 ? -1 : newMaxSizePerBufferIndex[cameraView.RenderBufferIndex];

                    //Clamp based in Global TextureSize
                    if (instanceMaxRenderTextureSize > 0)
                    {
                        if (cachedMaxSizePerBufferIndex <= 0)
                            cachedMaxSizePerBufferIndex = instanceMaxRenderTextureSize;
                        else
                            cachedMaxSizePerBufferIndex = Mathf.Min(cachedMaxSizePerBufferIndex, instanceMaxRenderTextureSize);
                    }

                    //Clamp based in MaxRenderBufferSize
                    if (cameraView.MaxRenderBufferSize > 0)
                    {
                        if (cachedMaxSizePerBufferIndex <= 0)
                            cachedMaxSizePerBufferIndex = cameraView.MaxRenderBufferSize;
                        else
                            cachedMaxSizePerBufferIndex = Mathf.Min(cachedMaxSizePerBufferIndex, cameraView.MaxRenderBufferSize);
                    }

                    if (cachedMaxSizePerBufferIndex > 0)
                        cachedMaxSizePerBufferIndex = Mathf.Max(16, cachedMaxSizePerBufferIndex);
                    newMaxSizePerBufferIndex[cameraView.RenderBufferIndex] = cachedMaxSizePerBufferIndex;
                }
            }

            if (newMaxSizePerBufferIndex.Count != s_maxSizePerBufferIndex.Count)
                sucess = true;
            else
            {
                foreach (var pair in newMaxSizePerBufferIndex)
                {
                    var index = pair.Key;
                    var maxSize = pair.Value;
                    if (!s_maxSizePerBufferIndex.ContainsKey(index) || s_maxSizePerBufferIndex[index] != maxSize)
                    {
                        sucess = true;
                        break;
                    }
                }
            }
            if (sucess)
                s_maxSizePerBufferIndex = newMaxSizePerBufferIndex;

            return sucess;
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
            var oldDefaultCullingMask = _defaultInvalidCullingMask;
            var oldMinimumSupportedFps = s_minimumSupportedFps;
            var oldRequireConstantRepaint = s_requireConstantRepaint;
            var oldRequireConstantBufferRepaint = s_requireConstantBufferRepaint;
            var oldUseRenderBuffer = s_useRenderBuffer;

            //In this mode we want to invalidate all layers
            if (m_forceAlwaysInvalidate)
                _defaultInvalidCullingMask = ~0;
            else
                _defaultInvalidCullingMask = 0;

            s_requireConstantRepaint = m_forceAlwaysInvalidate;
            s_requireConstantBufferRepaint = m_forceAlwaysInvalidate;
            s_minimumSupportedFps = 5;
            s_useRenderBuffer = false;
            foreach (var element in s_elements)
            {
                if (!element.IsDestroyed())
                {
                    var elementBufferConstantRepaint = (!element.IsScreenCanvasMember() && element.RequiresConstantRepaint);
                    s_minimumSupportedFps = Mathf.Max(s_minimumSupportedFps, element.MinimumSupportedFps);
                    s_requireConstantRepaint = s_requireConstantRepaint || element.RequiresConstantRepaint;
                    s_requireConstantBufferRepaint = s_requireConstantBufferRepaint || elementBufferConstantRepaint;
                    s_useRenderBuffer = s_useRenderBuffer || element.UseRenderBuffer;

                    //We want to add contant repaint element culling masks in defaultInvalidCullingMask
                    if (elementBufferConstantRepaint)
                        _defaultInvalidCullingMask |= element.CullingMask;
                }
            }
            s_minimumSupportedFps = Mathf.Clamp(s_minimumSupportedFps, (int)m_targetFpsRange.x, (int)m_targetFpsRange.y);

            var textureSizeChanged = RecalculateMaxTextureSize();
            if (oldRequireConstantRepaint != s_requireConstantRepaint ||
                oldRequireConstantBufferRepaint != s_requireConstantBufferRepaint ||
                oldMinimumSupportedFps != s_minimumSupportedFps ||
                oldUseRenderBuffer != s_useRenderBuffer ||
                oldDefaultCullingMask != _defaultInvalidCullingMask)
            {
                if (s_useRenderBuffer)
                    Invalidate(textureSizeChanged ? ~0 : _defaultInvalidCullingMask);
                else
                    Refresh();
            }
        }

        static HashSet<ISustainedElement> s_elements = new HashSet<ISustainedElement>();
        public static void RegisterDynamicElement(ISustainedElement element)
        {
            if (!s_elements.Contains(element))
            {
                s_elements.Add(element);
                MarkDynamicElementsDirty();
            }
        }

        public static void UnregisterDynamicElement(ISustainedElement element)
        {
            if (s_elements.Contains(element))
            {
                s_elements.Remove(element);
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