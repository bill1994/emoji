using System;
using System.Reflection;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// Provides APIs for looking up types based on their name.
    /// </summary>
    internal static class TypeLookup {
        /// <summary>
        /// Attempts to lookup the given type. Returns null if the type lookup fails.
        /// </summary>
        public static Type GetType(string typeName)
        {
            Type type = null;

#if UNITY_EDITOR//UNITY_WEBGL && !UNITY_EDITOR
            string v_assemblyName = "Assembly-CSharp";
            string v_fullNameType = "";
            string[] v_splits = typeName.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (v_splits.Length >= 2)
                v_assemblyName = v_splits[1]; // Type, AssemblyName, Version, Culture, PublicToken

            for (int i = 0; i < Math.Min(1, v_splits.Length); i++)
            {
                if (!string.IsNullOrEmpty(v_fullNameType))
                    v_fullNameType += ",";
                v_fullNameType += v_splits[i];
            }
            var v_assembly = !string.IsNullOrEmpty(v_assemblyName)? Assembly.Load(v_assemblyName) : null;
            if (v_assembly != null && !string.IsNullOrEmpty(v_fullNameType))
                type = v_assembly.GetType(v_fullNameType);
#else

            // Try a direct type lookup
            type = Type.GetType(typeName);
#endif

            if (type != null)
                return type;

#if (!UNITY_EDITOR && UNITY_METRO) == false // no AppDomain on WinRT
            // If we still haven't found the proper type, we can enumerate all of the loaded
            // assemblies and see if any of them define the type
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // See if that assembly defines the named type
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
#endif
            return null;
        }
    }
}