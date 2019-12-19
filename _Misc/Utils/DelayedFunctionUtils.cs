using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;
using System.Reflection;
using Kyub.Reflection;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kyub
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class DelayedFunctionUtils : Singleton<DelayedFunctionUtils>
    {
        #region Non-Instance 

        #region Static Variables/Properties

        static ArrayDict<float, FunctionAndParams> m_functionsToCallOverTime = new ArrayDict<float, FunctionAndParams>();

        public static ArrayDict<float, FunctionAndParams> FunctionsToCallOverTime
        {
            get
            {
                if (m_functionsToCallOverTime == null)
                    m_functionsToCallOverTime = new ArrayDict<float, FunctionAndParams>();
                return m_functionsToCallOverTime;
            }
        }

        #endregion

        #region Static Constructor

        static DelayedFunctionUtils()
        {
            Init();
        }

        static System.DateTime _lastCheckedTime = System.DateTime.UtcNow;
        static void Init()
        {
            _lastCheckedTime = System.DateTime.UtcNow;
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
            EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
#endif
        }

#if UNITY_EDITOR

        protected static bool EditorIsPlaying { get; set; }

        private static void HandleOnPlayModeChanged(PlayModeStateChange p_playState)
        {
            if (!EditorIsPlaying)
                EditorIsPlaying = (p_playState & PlayModeStateChange.EnteredPlayMode) == PlayModeStateChange.EnteredPlayMode;
            else
                EditorIsPlaying = !((p_playState & PlayModeStateChange.ExitingPlayMode) == PlayModeStateChange.ExitingPlayMode);
        }

#endif

        #endregion

        #region Static Callbacks

        static void EditorUpdate()
        {
            ProcessStaticTime();
        }

        #endregion

        #region Helper Non-Intance Functions

        static void RegisterInCallerOverTime(FunctionAndParams p_struct, float p_time)
        {
            if (p_struct != null)
            {
                if (p_time <= 0)
                    p_struct.CallFunction();
                else
                    FunctionsToCallOverTime.AddChecking(p_time, p_struct);
            }

        }

        static void ProcessStaticTime()
        {
            bool v_needClean = false;
            System.DateTime v_utfNow = System.DateTime.UtcNow;
            double v_seconds = (v_utfNow - _lastCheckedTime).TotalSeconds;
            _lastCheckedTime = v_utfNow;
            foreach (KVPair<float, FunctionAndParams> v_pair in FunctionsToCallOverTime)
            {
                if (v_pair != null && v_pair.Value != null)
                {
                    v_pair.Key -= (float)v_seconds;
                    if (v_pair.Key <= 0)
                    {
                        v_pair.Value.CallFunction();
                        v_pair.Value = null;
                        v_needClean = true;
                    }
                }
                else
                    v_needClean = true;
            }
            if (v_needClean)
            {
                FunctionsToCallOverTime.RemoveNulls();
                FunctionsToCallOverTime.RemovePairsWithNullValues();
            }
#if UNITY_EDITOR
            if (FunctionsToCallOverTime == null || FunctionsToCallOverTime.Count == 0)
            {
                EditorApplication.update -= EditorUpdate;
            }
#endif
        }

        #endregion

        #endregion

        #region Unity Instance Functions

        protected virtual void Update()
        {
            if (!Application.isEditor)
                ProcessStaticTime();
        }

        #endregion

        #region Public Functions Caller

        public static void CallFunction(System.Delegate p_functionPointer, float p_time, bool p_forceCreateInstance = true)
        {
            CallFunction(p_functionPointer, null, p_time, p_forceCreateInstance);
        }

        public static void CallFunction(System.Delegate p_functionPointer, object[] p_params, float p_time, bool p_forceCreateInstance = true)
        {
#if UNITY_EDITOR
            Init();
#else
            if(p_forceCreateInstance)
                TryCreateInstance();
#endif
            FunctionAndParams v_newStruct = new FunctionAndParams();
            v_newStruct.DelegatePointer = p_functionPointer;
            v_newStruct.Params = p_params != null ? new List<object>(p_params) : new List<object>();
            v_newStruct.Target = null;
            if (p_time <= 0)
                p_time = 0.001f;
            RegisterInCallerOverTime(v_newStruct, p_time);
        }

        static void TryCreateInstance()
        {
#if UNITY_EDITOR
            if (!EditorIsPlaying)
                return;
#endif
            if (DelayedFunctionUtils.GetInstance(true))
                return;
        }

        #endregion
    }

    #region Helper Classes

    [System.Serializable]
    public class FunctionAndParams
    {
        #region Private Variables

        [SerializeField]
        object m_target = null;
        [SerializeField]
        System.Type m_functionType = null;
        [SerializeField]
        string m_stringFunctionName = "";
        [SerializeField]
        System.Delegate m_delegatePointer = null;
        [SerializeField]
        List<object> m_params = new List<object>();

        #endregion

        #region Public Properties

        public object Target
        {
            get
            {
                return m_target;
            }
            set
            {
                if (m_target == value)
                    return;
                m_target = value;
            }
        }

        public System.Type FunctionType
        {
            get
            {
                return m_functionType;
            }
            set
            {
                if (m_functionType == value)
                    return;
                m_functionType = value;
            }
        }

        public string StringFunctionName
        {
            get
            {
                return m_stringFunctionName;
            }
            set
            {
                if (m_stringFunctionName == value)
                    return;
                m_stringFunctionName = value;
            }
        }

        public System.Delegate DelegatePointer
        {
            get
            {
                return m_delegatePointer;
            }
            set
            {
                if (m_delegatePointer == value)
                    return;
                m_delegatePointer = value;
            }
        }

        public List<object> Params
        {
            get
            {
                if (m_params == null)
                    m_params = new List<object>();
                return m_params;
            }
            set
            {
                if (m_params == value)
                    return;
                m_params = value;
            }
        }

        #endregion

        #region Helper Methods

        public bool CallFunction()
        {
            if (m_delegatePointer != null)
            {
                return CallDelegateFunction();
            }
            if (string.IsNullOrEmpty(m_stringFunctionName))
            {
                if (m_target != null)
                    return FunctionUtils.CallFunction(m_target, FunctionType, m_stringFunctionName, Params);
                else
                    return FunctionUtils.CallStaticFunction(FunctionType, m_stringFunctionName, Params);
            }
            return false;
        }

        protected bool CallDelegateFunction()
        {
            try
            {
                System.Delegate v_tempFunctionPointer = DelegatePointer;
                object[] v_params = Params.ToArray();
                if (v_tempFunctionPointer != null)
                {
                    if (Params.Count == 0)
                        v_tempFunctionPointer.DynamicInvoke(null);
                    else
                        v_tempFunctionPointer.DynamicInvoke(v_params);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public System.Type[] GetFunctionParameterTypes()
        {
            List<System.Type> v_parameters = new List<System.Type>();
            if (DelegatePointer != null)
            {
                MethodInfo v_invoke = DelegatePointer.GetType().GetMethod("Invoke");
                if (v_invoke != null)
                {
                    ParameterInfo[] v_params = v_invoke.GetParameters();
                    foreach (ParameterInfo v_param in v_params)
                    {
                        if (v_params != null)
                            v_parameters.Add(v_param.ParameterType);
                    }
                }
            }
            return v_parameters.ToArray();
        }

        #endregion
    }

    #endregion
}
