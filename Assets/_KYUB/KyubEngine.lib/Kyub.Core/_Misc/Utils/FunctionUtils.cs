using UnityEngine;
using System.Collections;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub
{
    public static class FunctionUtils
    {
        #region Function Caller

        public static bool StaticFunctionExists(string p_typeName, string p_functionName)
        {
            try
            {
                return StaticFunctionExists(System.Type.GetType(p_typeName), p_functionName);
            }
            catch { }
            return false;
        }

        public static bool StaticFunctionExists(System.Type p_type, string p_functionName)
        {
            if (p_type != null)
            {
                try
                {
                    System.Reflection.MethodInfo v_info = p_type.GetMethod(p_functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (v_info != null)
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static bool FunctionExists(string p_typeName, string p_functionName)
        {
            try
            {
                return FunctionExists(System.Type.GetType(p_typeName), p_functionName);
            }
            catch { }
            return false;
        }

        public static bool FunctionExists(System.Type p_type, string p_functionName)
        {
            if (p_type != null)
            {
                try
                {
                    System.Reflection.MethodInfo v_info = p_type.GetMethod(p_functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (v_info != null)
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static bool CallStaticFunction(string p_typeName, string p_functionName, params object[] p_param)
        {
            try
            {
                return CallStaticFunction(System.Type.GetType(p_typeName), p_functionName, p_param);
            }
            catch { }
            return false;
        }

        public static bool CallStaticFunction(System.Type p_type, string p_functionName, params object[] p_param)
        {
            if (p_type != null)
            {
                try
                {
                    System.Reflection.MethodInfo v_info = p_type.GetMethod(p_functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (v_info != null)
                    {
                        v_info.Invoke(null, p_param);
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static T CallStaticFunctionWithReturn<T>(string p_typeName, string p_functionName, params object[] p_param)
        {
            return CallStaticFunctionWithReturn<T>(System.Type.GetType(p_typeName), p_functionName, p_param);
        }

        public static T CallStaticFunctionWithReturn<T>(System.Type p_type, string p_functionName, params object[] p_param)
        {
            bool p_sucess = false;
            T v_return = TryCallStaticFunctionWithReturn<T>(p_type, p_functionName, out p_sucess, p_param);
            return v_return;
        }

        public static T TryCallStaticFunctionWithReturn<T>(System.Type p_type, string p_functionName, out bool p_sucess, params object[] p_param)
        {
            T v_return = default(T);
            p_sucess = false;
            System.Type v_type = p_type;
            if (v_type != null)
            {
                try
                {
                    System.Reflection.MethodInfo v_info = v_type.GetMethod(p_functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (v_info != null)
                    {
                        v_return = (T)v_info.Invoke(null, p_param);
                        p_sucess = true;
                    }
                }
                catch { }
            }
            return v_return;
        }

        public static bool CallFunction(object p_instance, string p_functionName, params object[] p_param)
        {
            if (p_instance != null)
            {
                System.Type v_type = p_instance.GetType();
                return CallFunction(p_instance, v_type, p_functionName, p_param);
            }
            return false;
        }

        public static bool CallFunction(object p_instance, string p_typeName, string p_functionName, params object[] p_param)
        {
            if (p_instance != null)
            {
                System.Type v_type = System.Type.GetType(p_typeName);
                return CallFunction(p_instance, v_type, p_functionName, p_param);
            }
            return false;
        }

        public static bool CallFunction(object p_instance, System.Type p_type, string p_functionName, params object[] p_param)
        {
            if (p_instance != null)
            {
                System.Type v_type = p_type;
                if (v_type != null)
                {
                    try
                    {
                        System.Reflection.MethodInfo v_info = v_type.GetMethod(p_functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (v_info != null)
                        {
                            v_info.Invoke(p_instance, p_param);
                            return true;
                        }
                    }
                    catch { }
                }
            }
            return false;
        }

        public static bool CallEditorStaticFunction(string p_typeName, string p_functionName, params object[] p_param)
        {
#if UNITY_EDITOR
            System.Reflection.Assembly v_editorAssembly = AssemblyUtils.GetEditorAssembly();
            if (v_editorAssembly != null)
            {
                p_typeName = string.IsNullOrEmpty(p_typeName) ? "" : p_typeName;
                string v_fullTypeNameWithAssembly = p_typeName.Contains(v_editorAssembly.FullName) ? p_typeName : p_typeName + ", " + v_editorAssembly.FullName;
                System.Type v_type = System.Type.GetType(v_fullTypeNameWithAssembly);
                return CallStaticFunction(v_type, p_functionName, p_param);
            }
#endif
            return false;
        }

        public static T CallEditorStaticFunctionWithReturn<T>(string p_typeName, string p_functionName, params object[] p_param)
        {
            bool p_sucess = false;
            T v_return = TryCallEditorStaticFunctionWithReturn<T>(p_typeName, p_functionName, out p_sucess, p_param);
            return v_return;
        }

        public static T TryCallEditorStaticFunctionWithReturn<T>(string p_typeName, string p_functionName, out bool p_sucess, params object[] p_param)
        {
            p_sucess = false;
            T v_return = default(T);
#if UNITY_EDITOR
            System.Reflection.Assembly v_editorAssembly = AssemblyUtils.GetEditorAssembly();
            if (v_editorAssembly != null)
            {
                System.Type v_type = System.Type.GetType(p_typeName + ", " + v_editorAssembly.FullName);
                v_return = TryCallStaticFunctionWithReturn<T>(v_type, p_functionName, out p_sucess, p_param);
            }
#endif
            return v_return;
        }

        #endregion
    }
}
