#if UNITY_WEBGL && !UNITY_EDITOR
#define DISABLE_MULTI_THREAD
#endif

using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kyub
{
    /// <summary>
    /// Context to Execute Code Delayed or in MainThread
    /// </summary>
    public class ApplicationContext
    {
        #region Events

        public static event Action OnUpdate;
        public static event Action OnLateUpdate;
        public static event Action<bool> OnApplicationPause;
        public static event Action<bool> OnApplicationFocus;

        #endregion

        #region Unity Initialization

        static InternalContextBehaviour _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new GameObject("RuntimeContext").AddComponent<InternalContextBehaviour>();
                _instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
                GameObject.DontDestroyOnLoad(_instance.gameObject);
            }
        }

        #endregion

        #region Public Functions

        public static void CancelAll()
        {
            lock (InternalContextBehaviour._cancelationBackLog)
            {
                InternalContextBehaviour._cancelationBackLog.Clear();
                InternalContextBehaviour._cancelAll = true;
                InternalContextBehaviour._queued = true;
            }
        }

        public static void Cancel(int guid)
        {
            lock (InternalContextBehaviour._cancelationBackLog)
            {
                InternalContextBehaviour._cancelationBackLog.Add(guid);
                InternalContextBehaviour._queued = true;
            }
        }

        public static void Cancel(Action action)
        {
            if (action != null)
                Cancel(action.GetHashCode());
        }

        public static void RunAsync(Action action)
        {
#if DISABLE_MULTI_THREAD
            RunOnMainThread(action);
#else
            if (action != null)
                ThreadPool.QueueUserWorkItem(o => action());
#endif
        }

        public static void RunAsync(int guid, Action action, float delay)
        {
            if (action != null)
                RunOnMainThread(guid, () => { RunAsync(action); }, delay);
        }

        public static void RunAsync(int guid, Action action)
        {
            RunAsync(guid, action, 0);
        }

        public static void RunAsync(Action action, float delay)
        {
            if (action != null)
            {
                var hashcode = action.GetHashCode();
                RunAsync(hashcode, action, delay);
            }
        }

        public static void RunOnMainThread(Action action)
        {
            RunOnMainThread(action, 0);
        }

        public static void RunOnMainThread(Action action, float delay)
        {
            if (action != null)
            {
                var hashcode = action.GetHashCode();
                RunOnMainThread(hashcode, action, delay);
            }
        }

        public static void RunOnMainThread(int guid, Action action)
        {
            RunOnMainThread(guid, action, 0);
        }

        public static void RunOnMainThread(int guid, Action action, float delay)
        {
            if (action != null)
                RunOnMainThreadInternal(new MainThreadActionInfo(action, delay) { Guid = guid });
        }

        #endregion

        #region Internal Helper Functions

        static void RunOnMainThreadInternal(MainThreadActionInfo actionInfo)
        {
            if (actionInfo != null && actionInfo.Action != null)
            {
                lock (InternalContextBehaviour._backlog)
                {
                    actionInfo.ScheduledWhenPlaying = InternalContextBehaviour._isPlaying;
                    InternalContextBehaviour._backlog.Add(actionInfo);
                    InternalContextBehaviour._queued = true;
                }
            }
        }

        #endregion

        #region Helper Classes

        class InternalContextBehaviour : MonoBehaviour
        {
            #region Editor Functions
#if UNITY_EDITOR
            [InitializeOnLoadMethod]
            private static void EditorInitialize()
            {
                _isPlaying = Application.isPlaying;
                EditorApplication.update -= EditorUpdate;
                EditorApplication.update += EditorUpdate;

                EditorApplication.playModeStateChanged -= EditorStateChanged;
                EditorApplication.playModeStateChanged += EditorStateChanged;
            }

            private static void EditorStateChanged(PlayModeStateChange state)
            {
                _isPlaying = state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode;
            }

            private static void EditorUpdate()
            {
                if (!_isPlaying)
                    CallUpdate();
            }
#endif
            #endregion

            #region Private Variables

            internal static bool _isPlaying = true;
            internal static volatile bool _queued = false;
            internal static List<MainThreadActionInfo> _backlog = new List<MainThreadActionInfo>(8);
            internal static List<MainThreadActionInfo> _actions = new List<MainThreadActionInfo>(8);

            internal static volatile bool _cancelAll = false;
            internal static HashSet<int> _cancelationBackLog = new HashSet<int>();
            internal static HashSet<int> _cancelActions = new HashSet<int>();

            #endregion

            #region Unity Functions

            private void Awake()
            {
                _isPlaying = true;
            }

            private void Update()
            {
                _isPlaying = true;
                CallUpdate();
            }

            private void LateUpdate()
            {
                CallLateUpdate();
            }

            private void OnApplicationPause(bool isPause)
            {
                CallApplicationPause(isPause);
            }

            private void OnApplicationFocus(bool isFocus)
            {
                CallApplicationFocus(isFocus);
            }

            #endregion

            #region Static Functions

            private static void CallUpdate()
            {
                if (ApplicationContext.OnUpdate != null)
                    ApplicationContext.OnUpdate.Invoke();

                if (_queued)
                {
                    lock (_backlog)
                    {
                        var tmp = _actions;
                        _actions = _backlog;
                        _backlog = tmp;
                    }

                    lock (_cancelationBackLog)
                    {
                        var tmp = _cancelActions;
                        _cancelActions = _cancelationBackLog;
                        _cancelationBackLog = tmp;

                        if (_cancelAll)
                        {
                            _cancelAll = false;
                            _actions.Clear();
                            _cancelActions.Clear();
                        }
                    }

                    float deltaTime = Time.unscaledDeltaTime;
                    for (int i = 0; i < _actions.Count; i++)
                    {
                        try
                        {
                            var actionInfo = _actions[i];
                            if (actionInfo == null ||
                                (actionInfo.Guid != null && _cancelActions.Contains(actionInfo.Guid.Value)) ||
                                actionInfo.TryExecute(deltaTime, _isPlaying))
                            {
                                _actions.RemoveAt(i);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("An exception occurred while dispatching an action on the main thread: " + e);
                        }
                    }

                    if (_cancelActions.Count > 0)
                        _cancelActions.Clear();

                    // Schedule to nex thread
                    if (_actions.Count > 0)
                    {
                        lock (_backlog)
                        {
                            _backlog.AddRange(_actions);
                            _queued = true;
                            _actions.Clear();
                        }
                    }
                }
            }

            private static void CallLateUpdate()
            {
                if (ApplicationContext.OnLateUpdate != null)
                    ApplicationContext.OnLateUpdate.Invoke();
            }

            private static void CallApplicationPause(bool isPause)
            {
                if (ApplicationContext.OnApplicationPause != null)
                    ApplicationContext.OnApplicationPause.Invoke(isPause);
            }

            private static void CallApplicationFocus(bool isFocus)
            {
                if (ApplicationContext.OnApplicationFocus != null)
                    ApplicationContext.OnApplicationFocus.Invoke(isFocus);
            }

            #endregion
        }

        class MainThreadActionInfo
        {
            #region Private Variables

            int? m_guid = 0;
            float m_remainingTime = 0;
            Action m_action = null;
            bool m_scheduledWhenPlaying = false;

            object _objectLock = new object();

            #endregion

            #region Public Properties

            public int? Guid
            {
                get
                {
                    return m_guid;
                }
                set
                {
                    if (m_guid == value)
                        return;
                    m_guid = value;
                }
            }

            public float RemainingTime
            {
                get
                {
                    return m_remainingTime;
                }
                set
                {
                    if (m_remainingTime == value)
                        return;
                    m_remainingTime = value;
                }
            }

            public Action Action
            {
                get
                {
                    return m_action;
                }
                set
                {
                    if (m_action == value)
                        return;
                    m_action = value;
                }
            }

            public bool ScheduledWhenPlaying
            {
                get
                {
                    return m_scheduledWhenPlaying;
                }
                set
                {
                    if (m_scheduledWhenPlaying == value)
                        return;
                    m_scheduledWhenPlaying = value;
                }
            }

            #endregion

            #region Contructors

            public MainThreadActionInfo() { }

            public MainThreadActionInfo(Action action) : this(action, 0)
            {
            }

            public MainThreadActionInfo(Action action, float remainingTime)
            {
                m_action = action;
                m_remainingTime = remainingTime;
            }

            #endregion

            #region Public Methods

            public bool TryExecute(float deltaTime, bool isPlaying)
            {
                lock (_objectLock)
                {
#if UNITY_EDITOR
                    //Cancel Action
                    if (m_scheduledWhenPlaying != isPlaying)
                        m_remainingTime = 1;
#endif
                    if (m_remainingTime <= 0)
                    {
                        if (m_remainingTime == 0 && m_action != null)
                            m_action.Invoke();
                    }
                    else
                    {
                        m_remainingTime = Mathf.Max(0, m_remainingTime - deltaTime);
                        return false;
                    }
                    return true;
                }
            }

            public bool TryCancel(int guid)
            {
                if (m_guid != null && m_guid.Value == guid)
                {
                    m_remainingTime = -1;
                    return true;
                }
                return false;
            }

            public void Cancel()
            {
                m_remainingTime = -1;
            }

            #endregion
        }

        #endregion
    }
}
