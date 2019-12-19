using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;

namespace Kyub
{
    public static class AssemblyUtils
    {
        static ArrayDict<System.Reflection.Assembly, System.Type[]> _assemblyTypesDict = null;
        public static ArrayDict<System.Reflection.Assembly, System.Type[]> GetAssemblyTypesDict(bool p_forceRefresh = false)
        {
            if (_assemblyTypesDict == null || p_forceRefresh)
            {
                _assemblyTypesDict = new ArrayDict<System.Reflection.Assembly, System.Type[]>();
                System.Reflection.Assembly[] v_assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (System.Reflection.Assembly v_assembly in v_assemblies)
                {
                    if (v_assembly != null)
                    {
#if UNITY_WINRT && !UNITY_EDITOR && !UNITY_WP8
					List<System.Type> v_typesList = new List<System.Type>();
					foreach(var v_typeInfo in v_assembly.DefinedTypes)
					{
						v_typesList.Add(v_typeInfo.GetType());
					}
					System.Type[] v_types = v_typesList.ToArray();
#else
                        System.Type[] v_types = v_assembly.GetTypes();
#endif
                        _assemblyTypesDict.Add(v_assembly, v_types);
                    }
                }
            }
            return _assemblyTypesDict;
        }

        static System.Reflection.Assembly _editorAssembly = null;
        public static System.Reflection.Assembly GetEditorAssembly()
        {
#if UNITY_EDITOR
            if (_editorAssembly == null)
            {
                System.Reflection.Assembly[] v_assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (System.Reflection.Assembly v_assembly in v_assemblies)
                {
                    if (v_assembly.FullName.Contains("Assembly-CSharp-Editor,"))
                    {
                        _editorAssembly = v_assembly;
                        break;
                    }
                }
            }
#endif
            return _editorAssembly;
        }
    }
}
