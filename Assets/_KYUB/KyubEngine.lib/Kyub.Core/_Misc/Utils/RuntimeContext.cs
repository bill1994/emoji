#if UNITY_WEBGL && !UNITY_EDITOR
#define DISABLE_MULTI_THREAD
#endif

using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;

namespace Kyub
{
    /// <summary>
    /// Context to Execute Code Delayed or in MainThread
    /// </summary>
    public class RuntimeContext
    {
        #region Events

        public static event Action OnUpdate;

        #endregion

        #region Unity Initialization

        static RuntimeContextBehaviour _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new GameObject("RuntimeContext").AddComponent<RuntimeContextBehaviour>();
                _instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
                GameObject.DontDestroyOnLoad(_instance.gameObject);
            }
        }

        #endregion

        #region Public Functions

        public static void CancelAll()
        {
            lock (RuntimeContextBehaviour._cancelationBackLog)
            {
                RuntimeContextBehaviour._cancelationBackLog.Clear();
                RuntimeContextBehaviour._cancelAll = true;
                RuntimeContextBehaviour._queued = true;
            }
        }

        public static void Cancel(int guid)
        {
            lock (RuntimeContextBehaviour._cancelationBackLog)
            {
                RuntimeContextBehaviour._cancelationBackLog.Add(guid);
                RuntimeContextBehaviour._queued = true;
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
            if(action != null)
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
            if(action != null)
                RunOnMainThreadInternal(new ActionInfo(action, delay) { Guid = guid });
        }

        #endregion

        #region Internal Helper Functions

        static void RunOnMainThreadInternal(ActionInfo actionInfo)
        {
            if (actionInfo != null && actionInfo.Action != null)
            {
                lock (RuntimeContextBehaviour._backlog)
                {
                    RuntimeContextBehaviour._backlog.Add(actionInfo);
                    RuntimeContextBehaviour._queued = true;
                }
            }
        }

        #endregion

        #region Helper Classes

        class RuntimeContextBehaviour : MonoBehaviour
        {
            #region Private Variables

            internal static volatile bool _queued = false;
            internal static List<ActionInfo> _backlog = new List<ActionInfo>(8);
            internal static List<ActionInfo> _actions = new List<ActionInfo>(8);

            internal static volatile bool _cancelAll = false;
            internal static HashSet<int> _cancelationBackLog = new HashSet<int>();
            internal static HashSet<int> _cancelActions = new HashSet<int>();

            #endregion

            #region Unity Functions

            private void Update()
            {
                if (RuntimeContext.OnUpdate != null)
                    RuntimeContext.OnUpdate.Invoke();

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
                                actionInfo.TryExecute(deltaTime))
                            {
                                _actions.RemoveAt(i);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("An exception occurred while dispatching an action on the main thread: " + e);
                        }
                    }

                    if(_cancelActions.Count > 0)
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

            #endregion
        }

        class ActionInfo
        {
            #region Private Variables

            int? m_guid = 0;
            volatile float m_remainingTime = 0;
            Action m_action = null;

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

            #endregion

            #region Contructors

            public ActionInfo() { }

            public ActionInfo(Action action) : this(action, 0)
            {
            }

            public ActionInfo(Action action, float remainingTime)
            {
                m_action = action;
                m_remainingTime = remainingTime;
            }

            #endregion

            #region Public Methods

            public bool TryExecute(float deltaTime)
            {
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
