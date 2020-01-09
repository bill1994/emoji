// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// If you wish to modify this template do so and then regenerate the unity
// events with the command line as shown below from within the directory
// that the template lives in.
//
// perl ../../Tools/Build/GenerateUnityEvents.pl 5 UnityEvent.template .

using System;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEngine.Events
{
    public delegate void UnityActionEx();

    [Serializable]
    public class UnityEventEx : UnityEventBaseEx
    {
        public UnityEventEx() { }

        public void AddListener(UnityActionEx call)
        {
            AddCall(GetDelegate(call));
        }

        public void RemoveListener(UnityActionEx call)
        {
            RemoveListener(call.Target, call.Method);
        }

        protected override MethodInfo FindMethod_Impl(string name, Type targetObjType)
        {
            return GetValidMethodInfo(targetObjType, name, new Type[] { });
        }

        internal override BaseInvokableCall GetDelegate(object target, MethodInfo theFunction)
        {
            return new InvokableCall(target, theFunction);
        }

        private static BaseInvokableCall GetDelegate(UnityActionEx action)
        {
            return new InvokableCall(action);
        }

        private object[] m_InvokeArray = null;
        public void Invoke()
        {
            List<BaseInvokableCall> calls = PrepareInvoke();
            for (var i = 0; i < calls.Count; i++)
            {
                var curCall = calls[i] as InvokableCall;
                if (curCall != null)
                    curCall.Invoke();
                else
                {
                    var staticCurCall = calls[i] as InvokableCall;
                    if (staticCurCall != null)
                        staticCurCall.Invoke();
                    else
                    {
                        var cachedCurCall = calls[i];
                        if (m_InvokeArray == null)
                            m_InvokeArray = new object[0];

                        cachedCurCall.Invoke(m_InvokeArray);
                    }
                }
            }
        }


        internal void AddPersistentListener(UnityActionEx call)
        {
            AddPersistentListener(call, UnityEventCallState.RuntimeOnly);
        }

        internal void AddPersistentListener(UnityActionEx call, UnityEventCallState callState)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterPersistentListener(count, call);
            SetPersistentListenerState(count, callState);
        }

        internal void RegisterPersistentListener(int index, UnityActionEx call)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            RegisterPersistentListener(index, call.Target as UnityEngine.Object, call.Method.DeclaringType, call.Method);
        }

    }
}