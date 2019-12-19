using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace Kyub.UI
{

    public enum TimerStyle { OneTimeOnly, PingPong }

    //SafeRestartIfRunning call Finish before StartTime Again
    public enum RestartEnum { DontRestartIfRunning, OnlyForceFinishIfRunning, RestartIfRunning, SafeRestartIfRunning }
    public enum WhenStopDestroyEnum { DontDestroy, DestroyScript, DestroyGameObject }

    [System.Serializable]
    public class TimeTween : MonoBehaviour
    {
        #region Events

        public event System.Action<CycleEventArgs> OnFirstStart; // On Enabled
        public event System.Action<CycleEventArgs> OnStop; // Only when true finished (After Disable)
        public event System.Action<CycleEventArgs> OnCycleFinishedExecution;
        public event System.Action<CycleEventArgs> OnCycleStartedExecution;

        #endregion

        #region Private Variables

        [SerializeField]
        float m_pingDelayTime = 0;
        [SerializeField]
        float m_pongDelayTime = 0;
        [SerializeField]
        float m_initialDelay = 0;
        [SerializeField]
        float m_currentDelay = 0;
        [SerializeField]
        float m_maxTime = 0.6f;
        [SerializeField]
        float m_currentTime = 0f;
        [SerializeField]
        private AnimationCurve m_animationCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 1f), new Keyframe(1f, 1f, 1f, 0f));
        [SerializeField]
        TimerStyle m_timerStyle = TimerStyle.OneTimeOnly;
        [SerializeField]
        RestartEnum m_restartOption = RestartEnum.RestartIfRunning;
        [SerializeField]
        WhenStopDestroyEnum m_destroyOptionWhenStop = WhenStopDestroyEnum.DontDestroy;
        [SerializeField]
        bool m_ignoreTimeScale = true;
        [SerializeField]
        bool m_loop = false;
        [SerializeField]
        bool m_isPing = true;
        [SerializeField]
        bool m_forceFinishOnDisable = false;
        [SerializeField]
        Transform m_target = null;

        #endregion

        #region Callbacks

        public UnityEvent OnUpdateCallback = new UnityEvent();
        public UnityEvent OnCycleStartedCallback = new UnityEvent();
        public UnityEvent OnCycleFinishedCallback = new UnityEvent();
        public UnityEvent OnStartCallback = new UnityEvent();
        public UnityEvent OnStopCallback = new UnityEvent();

        #endregion

        #region Public Properties

        public float PingDelayTime { get { return m_pingDelayTime; } set { m_pingDelayTime = value; } }
        public float PongDelayTime { get { return m_pongDelayTime; } set { m_pongDelayTime = value; } }
        public float InitialDelay { get { return m_initialDelay; } set { m_initialDelay = value; CurrentDelay = value; } }
        public float CurrentDelay { get { return m_currentDelay; } set { m_currentDelay = value; } }
        public float MaxTime { get { return m_maxTime; } set { m_maxTime = value; } }
        public float CurrentTime { get { return m_currentTime; } set { m_currentTime = value; } }
        public bool IgnoreTimeScale { get { return m_ignoreTimeScale; } set { m_ignoreTimeScale = value; } }
        public bool Loop { get { return m_loop; } set { m_loop = value; } }
        public TimerStyle TimerStyle { get { return m_timerStyle; } set { m_timerStyle = value; } }
        public AnimationCurve AnimationCurve { get { return m_animationCurve; } set { m_animationCurve = value; } }
        public bool IsPing { get { return m_isPing; } set { m_isPing = value; } }
        public bool ForceFinishOnDisable { get { return m_forceFinishOnDisable; } set { m_forceFinishOnDisable = value; } }
        public RestartEnum RestartOption { get { return m_restartOption; } set { m_restartOption = value; } }
        public WhenStopDestroyEnum DestroyOptionWhenStop { get { return m_destroyOptionWhenStop; } set { m_destroyOptionWhenStop = value; } }
        public Transform Target
        {
            get
            {
                if (m_target == null)
                    return transform;
                return m_target;
            }
            set { m_target = value; }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnDisable()
        {
            if (CurrentTime > 0 && ForceFinishOnDisable)
            {
                var v_oldEnabled = enabled;
                ForceFinish(true);
                if(v_oldEnabled)
                    enabled = true;
            }
            CurrentTime = 0;
            CurrentDelay = 0;
        }

        protected virtual void OnEnable()
        {
            if (CurrentDelay != InitialDelay)
                CurrentDelay = InitialDelay;
            if (CurrentTime <= 0)
            {
                StartTimer(IsPing, false);
                CallFirstStartEvents(IsPing);
            }
        }

        protected virtual void Update()
        {
            if (CurrentDelay > 0)
            {
                CurrentDelay = Mathf.Max(0, (CurrentDelay - GetDeltaTime()));
                if (CurrentDelay <= 0)
                {
                    if (enabled)
                    {
                        if (m_isPing)
                            StartPing();
                        else
                            StartPong();
                    }
                }
            }
            else if (enabled)
            {
                if (m_currentTime > 0)
                {
                    m_currentTime = Mathf.Max(0, (m_currentTime - GetDeltaTime()));
                    if (m_isPing)
                    {
                        OnPingUpdate();
                        CallUpdateEvents();
                        if (m_currentTime <= 0)
                            FinishPing();
                    }
                    //Pong
                    else
                    {
                        OnPongUpdate();
                        CallUpdateEvents();
                        if (m_currentTime <= 0)
                            FinishPong();
                    }
                }
                else
                {
                    if (m_isPing)
                        FinishPing();
                    else
                        FinishPong();
                }
            }
        }

        #endregion

        #region Public Functions

        public bool Finished()
        {
            return !enabled || (enabled && m_currentTime <= 0) ? true : false;
        }

        public float GetTimeScale()
        {
            return GetTimeScale(true);
        }

        public float GetTimeScale(bool p_useCurve)
        {
            float v_delta = MaxTime > 0 ? CurrentTime / MaxTime : 0;
            if (p_useCurve)
            {
                if (IsPing)
                    return AnimationCurve != null ? AnimationCurve.Evaluate(1 - v_delta) : 1 - v_delta;
                else
                    return AnimationCurve != null ? AnimationCurve.Evaluate(v_delta) : v_delta;
            }
            else
            {
                if (IsPing)
                    return 1 - v_delta;
                else
                    return v_delta;
            }

        }

        public void StartTimer(bool p_isPing, bool p_forceEnable, RestartEnum p_restartOption, bool p_forceRefreshInitialDelay = false)
        {
            RestartOption = p_restartOption;
            if (CurrentTime > 0 && enabled)
            {
                if (RestartOption == RestartEnum.DontRestartIfRunning)
                {
                    return;
                }
                else if (RestartOption == RestartEnum.SafeRestartIfRunning)
                    ForceFinish();
                else if (RestartOption == RestartEnum.OnlyForceFinishIfRunning)
                {
                    ForceFinish();
                    return;
                }
            }
            m_currentTime = m_maxTime;
            m_isPing = p_isPing;
            CurrentDelay = 0;
            if (p_forceEnable)
            {
                if (!enabled)
                {
                    CurrentDelay = InitialDelay;
                    enabled = true;
                }
            }
            if (p_forceRefreshInitialDelay)
                CurrentDelay = InitialDelay;
            if (IsPing)
                CurrentDelay += PingDelayTime;
            else
                CurrentDelay += PongDelayTime;
            if (enabled && CurrentDelay <= 0)
            {
                if (m_isPing)
                    StartPing();
                else
                    StartPong();
            }
        }

        public void StartTimer(bool p_forceEnable = true, bool p_forceRefreshInitialDelay = false)
        {
            StartTimer(IsPing, p_forceEnable, RestartOption, p_forceRefreshInitialDelay);
        }

        public void ForceFinish(bool p_isPing, bool p_forceCallFinalAction)
        {
            IsPing = p_isPing;
            ForceFinish(p_forceCallFinalAction);
        }

        public void ForceFinish(bool p_forceCallFinalAction = false)
        {
            var v_oldFinish = m_loop;
            m_loop = false;
            if (!Finished() || p_forceCallFinalAction)
            {
                bool v_needStart = (CurrentDelay > 0 || !enabled) || !Application.isPlaying;
                enabled = false;
                m_currentTime = 0;
                CurrentDelay = 0;

                if (m_timerStyle == TimerStyle.PingPong)
                    FinishPong(v_needStart);
                else if (IsPing)
                    FinishPing(v_needStart);
                else
                    FinishPong(v_needStart);
            }
            else
            {
                enabled = false;
                m_currentTime = 0;
                CurrentDelay = 0;
            }
            m_loop = v_oldFinish;
        }

        #endregion

        #region Private Functions

        protected float GetDeltaTime()
        {
            if (IgnoreTimeScale)
                return Time.unscaledDeltaTime;
            else
                return Time.deltaTime;
        }

        private void StartPing()
        {
            CallStartCycleEvents(true);
            OnPingStart();
        }

        private void StartPong()
        {
            CallStartCycleEvents(false);
            OnPongStart();
        }

        private void FinishPing(bool p_needStart = false)
        {
            if (p_needStart)
                StartPing();
            CurrentDelay = 0;
            m_currentTime = 0;
            OnPingUpdate();
            CallUpdateEvents();
            OnBeforePingFinish();
            IsPing = TimerStyle == TimerStyle.PingPong ? false : true;
            OnPingFinish();
            CallFinishEvents(true);
            if (m_timerStyle == TimerStyle.PingPong || Loop)
                StartTimer(IsPing);
            else if (!Loop)
            {
                if (m_currentTime <= 0)
                {
                    Stop(true);
                }
            }
        }

        private void FinishPong(bool p_needStart = false)
        {
            if (p_needStart)
                StartPong();
            CurrentDelay = 0;
            m_currentTime = 0;
            OnPongUpdate();
            CallUpdateEvents();
            OnBeforePongFinish();
            IsPing = TimerStyle == TimerStyle.PingPong ? true : false;
            OnPongFinish();
            CallFinishEvents(false);
            if (Loop)
                StartTimer(IsPing);
            else
            {
                if (m_currentTime <= 0)
                {
                    Stop(false);
                }
            }
        }

        private void Stop(bool p_isPing)
        {
            enabled = false;
            CallStopEvents(p_isPing);
            try
            {
                if (DestroyOptionWhenStop == WhenStopDestroyEnum.DestroyScript)
                    Kyub.DestroyUtils.Destroy(this);
                if (DestroyOptionWhenStop == WhenStopDestroyEnum.DestroyGameObject)
                {
                    gameObject.SetActive(false);
                    Kyub.DestroyUtils.Destroy(this.gameObject);
                }
            }
            catch { }
        }

        private void CallUpdateEvents()
        {
            if (OnUpdateCallback != null && GetDeltaTime() != 0)
            {
                OnUpdateCallback.Invoke();
            }
        }

        private void CallFirstStartEvents(bool p_isPing)
        {
            if (OnFirstStart != null)
                OnFirstStart(new CycleEventArgs(TimerStyle, p_isPing, Loop, true));
            if (OnStartCallback != null)
                OnStartCallback.Invoke();
        }

        private void CallStopEvents(bool p_isPing)
        {
            if (OnStop != null)
                OnStop(new CycleEventArgs(TimerStyle, p_isPing, Loop, true));
            if (OnStopCallback != null)
                OnStopCallback.Invoke();
        }

        private void CallFinishEvents(bool p_isPing)
        {
            if (OnCycleFinishedExecution != null)
                OnCycleFinishedExecution(new CycleEventArgs(TimerStyle, p_isPing, Loop, Finished()));
            if (OnCycleFinishedCallback != null)
                OnCycleFinishedCallback.Invoke();
        }

        private void CallStartCycleEvents(bool p_isPing)
        {
            if (OnCycleStartedExecution != null)
                OnCycleStartedExecution(new CycleEventArgs(TimerStyle, p_isPing, Loop, false));
            if (OnCycleStartedCallback != null)
                OnCycleStartedCallback.Invoke();
        }

        #endregion

        #region Virtual Functions

        protected virtual void OnPingStart()
        {

        }

        protected virtual void OnPongStart()
        {

        }

        protected virtual void OnPingUpdate()
        {

        }

        protected virtual void OnPongUpdate()
        {

        }

        protected virtual void OnBeforePingFinish()
        {

        }

        protected virtual void OnBeforePongFinish()
        {

        }

        protected virtual void OnPingFinish()
        {

        }

        protected virtual void OnPongFinish()
        {

        }

        #endregion
    }

    public class CycleEventArgs
    {
        #region Private Variables

        TimerStyle m_timerStyle = TimerStyle.OneTimeOnly;
        bool m_isPing = true;
        bool m_isLoop = true;
        bool m_finished = true;

        #endregion

        #region Public Properties

        public TimerStyle TimerStyle { get { return m_timerStyle; } set { m_timerStyle = value; } }
        public bool IsPing { get { return m_isPing; } set { m_isPing = value; } }
        public bool IsLoop { get { return m_isLoop; } set { m_isLoop = value; } }
        public bool Finished { get { return m_finished; } set { m_finished = value; } }

        #endregion

        #region Constructor

        public CycleEventArgs(TimerStyle p_timerStyle, bool p_isPing, bool p_isLoop, bool p_finished)
        {
            m_timerStyle = p_timerStyle;
            m_isPing = p_isPing;
            m_isLoop = p_isLoop;
            m_finished = p_finished;
        }

        #endregion
    }
}
