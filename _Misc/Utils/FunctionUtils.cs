using UnityEngine;
using System.Collections;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub
{
    public static class FunctionUtils
    {
        #region Function Caller

        public static bool StaticFunctionExists(string typeName, string functionName)
        {
            try
            {
                return StaticFunctionExists(System.Type.GetType(typeName), functionName);
            }
            catch { }
            return false;
        }

        public static bool StaticFunctionExists(System.Type type, string functionName)
        {
            if (type != null)
            {
                try
                {
                    System.Reflection.MethodInfo info = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (info != null)
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static bool FunctionExists(string typeName, string functionName)
        {
            try
            {
                return FunctionExists(System.Type.GetType(typeName), functionName);
            }
            catch { }
            return false;
        }

        public static bool FunctionExists(System.Type type, string functionName)
        {
            if (type != null)
            {
                try
                {
                    System.Reflection.MethodInfo info = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (info != null)
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static bool CallStaticFunction(string typeName, string functionName, params object[] param)
        {
            try
            {
                return CallStaticFunction(System.Type.GetType(typeName), functionName, param);
            }
            catch { }
            return false;
        }

        public static bool CallStaticFunction(System.Type type, string functionName, params object[] param)
        {
            if (type != null)
            {
                try
                {
                    System.Reflection.MethodInfo info = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (info != null)
                    {
                        info.Invoke(null, param);
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static T CallStaticFunctionWithReturn<T>(string typeName, string functionName, params object[] param)
        {
            return CallStaticFunctionWithReturn<T>(System.Type.GetType(typeName), functionName, param);
        }

        public static T CallStaticFunctionWithReturn<T>(System.Type type, string functionName, params object[] param)
        {
            bool sucess = false;
            T result = TryCallStaticFunctionWithReturn<T>(type, functionName, out sucess, param);
            return result;
        }

        public static T TryCallStaticFunctionWithReturn<T>(System.Type type, string functionName, out bool sucess, params object[] param)
        {
            T result = default(T);
            sucess = false;
            if (type != null)
            {
                try
                {
                    System.Reflection.MethodInfo info = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (info != null)
                    {
                        result = (T)info.Invoke(null, param);
                        sucess = true;
                    }
                }
                catch { }
            }
            return result;
        }

        public static bool CallFunction(object instance, string functionName, params object[] param)
        {
            if (instance != null)
            {
                System.Type type = instance.GetType();
                return CallFunction(instance, type, functionName, param);
            }
            return false;
        }

        public static bool CallFunction(object instance, string typeName, string functionName, params object[] param)
        {
            if (instance != null)
            {
                System.Type type = System.Type.GetType(typeName);
                return CallFunction(instance, type, functionName, param);
            }
            return false;
        }

        public static bool CallFunction(object instance, System.Type type, string functionName, params object[] param)
        {
            if (instance != null)
            {
                if (type != null)
                {
                    try
                    {
                        System.Reflection.MethodInfo info = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (info != null)
                        {
                            info.Invoke(instance, param);
                            return true;
                        }
                    }
                    catch { }
                }
            }
            return false;
        }

        public static bool CallEditorStaticFunction(string typeName, string functionName, params object[] param)
        {
#if UNITY_EDITOR
            System.Reflection.Assembly editorAssembly = AssemblyUtils.GetEditorAssembly();
            if (editorAssembly != null)
            {
                typeName = string.IsNullOrEmpty(typeName) ? "" : typeName;
                string fullTypeNameWithAssembly = typeName.Contains(editorAssembly.FullName) ? typeName : typeName + ", " + editorAssembly.FullName;
                System.Type type = System.Type.GetType(fullTypeNameWithAssembly);
                return CallStaticFunction(type, functionName, param);
            }
#endif
            return false;
        }

        public static T CallEditorStaticFunctionWithReturn<T>(string typeName, string functionName, params object[] param)
        {
            bool sucess = false;
            T result = TryCallEditorStaticFunctionWithReturn<T>(typeName, functionName, out sucess, param);
            return result;
        }

        public static T TryCallEditorStaticFunctionWithReturn<T>(string typeName, string functionName, out bool sucess, params object[] param)
        {
            sucess = false;
            T result = default(T);
#if UNITY_EDITOR
            System.Reflection.Assembly editorAssembly = AssemblyUtils.GetEditorAssembly();
            if (editorAssembly != null)
            {
                System.Type type = System.Type.GetType(typeName + ", " + editorAssembly.FullName);
                result = TryCallStaticFunctionWithReturn<T>(type, functionName, out sucess, param);
            }
#endif
            return result;
        }

        #endregion
    }
}
