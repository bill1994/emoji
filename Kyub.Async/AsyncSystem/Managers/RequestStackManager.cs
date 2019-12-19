using Kyub.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Async
{
    public class RequestStackManager : Singleton<RequestStackManager>
    {
        #region Static Callbacks

        public static event System.Action<MonoBehaviour, IEnumerator, string> OnCancelRequest;
        public static event System.Action<MonoBehaviour, IEnumerator, string> OnRequestExecutionFinish;

        #endregion

        #region Private Variables

        [Header("General Configurations")]
        [SerializeField] private int m_requestTimeLimit = 120;
        [SerializeField] private int m_maxAllowedRequests = 10;

        #endregion

        #region Unity Functions

        protected virtual void Update()
        {
            if (this == s_instance)
                UpdateRequestRoutines();
        }

        #endregion

        #region Fields

        static Dictionary<IEnumerator, CoroutineController> s_executingRequests = new Dictionary<IEnumerator, CoroutineController>();
        static List<IEnumerator> s_pendingRequests = new List<IEnumerator>();
        static HashSet<IEnumerator> s_pendingRequestsHash = new HashSet<IEnumerator>();

        static Dictionary<string, IEnumerator> s_hashToEnumeratorMap = new Dictionary<string, IEnumerator>();
        static Dictionary<IEnumerator, string> s_enumeratorToHashMap = new Dictionary<IEnumerator, string>();
        static Dictionary<IEnumerator, MonoBehaviour> s_enumeratorToSenders = new Dictionary<IEnumerator, MonoBehaviour>();

        #endregion

        #region Properties

        public static int ExecutingRequestsCount
        {
            get
            {
                return s_executingRequests.Count;
            }
        }

        public static int PendingRequestsCount
        {
            get
            {
                return s_pendingRequests.Count;
            }
        }

        public static int MaxAllowedRequestsCount
        {
            get
            {
                return s_instance != null? s_instance.m_maxAllowedRequests : 0;
            }
        }

        public static int RequestTimeLimit
        {
            get
            {
                return s_instance != null ? s_instance.m_requestTimeLimit : -1;
            }
        }


        #endregion

        #region Request Stack Methods

        public static bool HasRequestSlotsAvailable()
        {
            var v_maxRequests = s_instance != null ? s_instance.m_maxAllowedRequests : 0;
            return s_executingRequests.Count + s_pendingRequests.Count < v_maxRequests;
        }

        public static bool IsRequestingAny()
        {
            return s_executingRequests.Count > 0 || s_pendingRequestsHash.Count > 0;
        }

        public static bool IsRequesting(IEnumerator routine)
        {
            return s_executingRequests.ContainsKey(routine) || s_pendingRequestsHash.Contains(routine);
        }

        public static bool IsRequesting(string hash)
        {
            return s_hashToEnumeratorMap.ContainsKey(hash);
        }

        public static IEnumerator GetRoutineWithHash(string hash)
        {
            IEnumerator v_enumerator = null;
            s_hashToEnumeratorMap.TryGetValue(hash, out v_enumerator);
            return v_enumerator;
        }

        public static bool StopRequest(IEnumerator routine)
        {
            var sucess = false;
            CoroutineController controller = null;
            if (s_executingRequests.TryGetValue(routine, out controller))
            {
                if (controller != null)
                    s_executingRequests.Remove(routine);

                if (controller.state != CoroutineState.Finished)
                {
                    sucess = true;
                    //Stop Running Routines
                    if(controller.state == CoroutineState.Running || controller.state == CoroutineState.Paused)
                        controller.Stop();
                }
            }

            if (s_pendingRequestsHash.Contains(routine))
            {
                int v_index = s_pendingRequests.IndexOf(routine);
                if (v_index >= 0)
                    s_pendingRequests.RemoveAt(v_index);
                sucess = s_pendingRequestsHash.Remove(routine) || sucess || v_index >= 0;
            }

            //Remove Sender from this routine
            MonoBehaviour sender = null;
            if (s_enumeratorToSenders.TryGetValue(routine, out sender))
            {
                s_enumeratorToSenders.Remove(routine);
            }

            //Remove Hash
            var hash = "";
            if (s_enumeratorToHashMap.TryGetValue(routine, out hash))
            {
                s_enumeratorToHashMap.Remove(routine);
                s_hashToEnumeratorMap.Remove(hash);
            }

            if (sucess)
            {
                if(OnCancelRequest != null)
                    OnCancelRequest(sender, routine, hash);
            }
            //The routine was finished but will only be unscheduled in next frame, so we can report the "OnRequestExecutionFinish" now
            else if(!sucess && controller != null)
            {
                if(OnRequestExecutionFinish != null)
                    OnRequestExecutionFinish(sender, routine, hash);
            }

            return sucess;
        }

        public static bool StopRequest(string hash)
        {
            IEnumerator routine = null;
            if (s_hashToEnumeratorMap.TryGetValue(hash, out routine))
            {
                if(routine != null)
                    return StopRequest(routine);
            }
            return false;
        }

        public static bool StopAllRequestsFromSender(MonoBehaviour behaviour)
        {
            var sucess = false;

            //Find running routines
            List<IEnumerator> routinesToStop = new List<IEnumerator>();
            foreach (var pair in s_enumeratorToSenders)
            {
                if (pair.Value == behaviour)
                {
                    routinesToStop.Add(pair.Key);
                }
            }

            //Stop all requests of the scheduled behaviour
            for (int i = 0; i < routinesToStop.Count; i++)
            {
                sucess = StopRequest(routinesToStop[i]) || sucess;
            }
            return sucess;
        }

        public static bool RequestRoutine(MonoBehaviour sender, IEnumerator routine, string customHash = null)
        {
            if (Instance != null && routine != null && !s_executingRequests.ContainsKey(routine) && !s_pendingRequestsHash.Contains(routine))
            {
                if (!string.IsNullOrEmpty(customHash))
                {
                    if (IsRequesting(customHash))
                    {
                        Debug.LogWarning("[RequestStackManager] Detected routine with same hash but diferent IEnumerator. Aborting duplicated request...");
                        return false;
                    }
                    s_hashToEnumeratorMap[customHash] = routine;
                    s_enumeratorToHashMap[routine] = customHash;
                }

                //Schedule to custom sender if exists (or leave it unscheduled so the coroutine will be executed in RequestStackManager Object)
                if (sender != null)
                    s_enumeratorToSenders[routine] = sender;

                s_pendingRequestsHash.Add(routine);
                s_pendingRequests.Add(routine);
                return true;
            }
            return false;
        }

        public static bool RequestPriorityRoutine(MonoBehaviour sender, IEnumerator routine, string customHash = null)
        {
            if (Instance != null && routine != null && !s_executingRequests.ContainsKey(routine))
            {
                if (!string.IsNullOrEmpty(customHash))
                {
                    if (IsRequesting(customHash))
                    {
                        Debug.LogWarning("[RequestStackManager] Detected routine with same hash but diferent IEnumerator. Aborting duplicated request...");
                        return false;
                    }
                    s_hashToEnumeratorMap[customHash] = routine;
                    s_enumeratorToHashMap[routine] = customHash;
                }

                //Schedule to custom sender if exists (or leave it unscheduled so the coroutine will be executed in RequestStackManager Object)
                if (sender != null)
                    s_enumeratorToSenders[routine] = sender;

                int index = s_pendingRequests.IndexOf(routine);
                if (index > 0)
                    s_pendingRequests.RemoveAt(index);
                //We dont need to add if this is the first member of the list
                if (index != 0)
                {
                    s_pendingRequests.Insert(0, routine);
                    if (index < 0)
                        s_pendingRequestsHash.Add(routine);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Instance Request Methods

        protected virtual void UpdateRequestRoutines()
        {
            //Remove finished routines
            var keys = new List<IEnumerator>(s_executingRequests.Keys);
            foreach (var key in keys)
            {
                MonoBehaviour sender = null;
                if (!s_enumeratorToSenders.TryGetValue(key, out sender))
                    sender = this;

                //Check if need to abort request
                var cancelled = false;
                if (m_requestTimeLimit > 0 && s_executingRequests[key].state != CoroutineState.Finished  &&
                    s_executingRequests[key].accumulatedTime > m_requestTimeLimit)
                {
                    Debug.LogWarning(string.Format("[RequestStackManager] Aborting timeout process (sender: {0})", sender.name));
                    if(s_executingRequests[key].state == CoroutineState.Running || s_executingRequests[key].state == CoroutineState.Paused)
                        s_executingRequests[key].Stop();
                    cancelled = true;
                }

                //Check if state aborted or finished
                if (cancelled || sender == null || s_executingRequests[key].state == CoroutineState.Finished)
                {
                    s_executingRequests.Remove(key);

                    //Remove Hash
                    var hash = "";
                    if (s_enumeratorToHashMap.TryGetValue(key, out hash))
                    {
                        s_enumeratorToHashMap.Remove(key);
                        s_hashToEnumeratorMap.Remove(hash);
                    }

                    //Remove Sender
                    s_enumeratorToSenders.Remove(key);

                    //Call correct event
                    if (cancelled || sender == null)
                    {
                        if (OnCancelRequest != null)
                            OnCancelRequest(sender, key, hash);
                    }
                    else
                    {
                        if (OnRequestExecutionFinish != null)
                            OnRequestExecutionFinish(sender, key, hash);
                    }
                }
            }

            //Schedule Pendent routines in execution Routines free slots
            List<IEnumerator> enumeratorsToExecute = new List<IEnumerator>();
            List<IEnumerator> enumeratorsToRetry = new List<IEnumerator>();
            while (s_executingRequests.Count < m_maxAllowedRequests && s_pendingRequests.Count > 0)
            {
                var enumerator = s_pendingRequests[0];

                s_pendingRequests.RemoveAt(0);
                s_pendingRequestsHash.Remove(enumerator);

                if (enumerator != null)
                {
                    if (s_executingRequests.ContainsKey(enumerator))
                    {
                        Debug.LogWarning("[RequestStackManager] Same coroutine already detected executing " + enumerator.GetHashCode());
                        //Add in the end of Request List (we can try again later)
                        enumeratorsToRetry.Add(enumerator);
                    }
                    else
                    {
                        //Prevent "Infinity Loops" when coroutine starts immediate and check for RequestRoutine too
                        s_executingRequests[enumerator] = null;
                        enumeratorsToExecute.Add(enumerator);
                    }
                }
            }

            //Add again enumerators with error (we will try again in next cycle)
            foreach (var enumeratorToRetry in enumeratorsToRetry)
            {
                s_pendingRequests.Add(enumeratorToRetry);
                s_pendingRequestsHash.Add(enumeratorToRetry);
            }

            //Execute Coroutines (Prevent infinity loop when routine execute immediate and call RequestWithPriority)
            CoroutineController coroutineController;
            foreach (var enumeratorToExecute in enumeratorsToExecute)
            {
                MonoBehaviour sender = null;
                if (!s_enumeratorToSenders.TryGetValue(enumeratorToExecute, out sender))
                    sender = this;

                var senderIsValid = sender != null && sender.enabled && sender.gameObject.activeInHierarchy;
                if (sender != null && sender.enabled && sender.gameObject.activeInHierarchy)
                {
                    sender.StartCoroutineEx(enumeratorToExecute, out coroutineController);
                    s_executingRequests[enumeratorToExecute] = coroutineController;
                }
                else
                {
                    //Pick Request Hash
                    var hash = "";
                    s_enumeratorToHashMap.TryGetValue(enumeratorToExecute, out hash);

                    var senderName = sender != null ? sender.name : "-";
                    Debug.Log(string.Format("[RequestStackManager] Removing invalid Routines (sender: {0}, active: {1}, CustomHash: {2})",
                        senderName, senderIsValid, hash));

                    //Cancel Request
                    if (OnCancelRequest != null)
                        OnCancelRequest(sender, enumeratorToExecute, hash);
                }
            }
        }

        #endregion
    }
}
