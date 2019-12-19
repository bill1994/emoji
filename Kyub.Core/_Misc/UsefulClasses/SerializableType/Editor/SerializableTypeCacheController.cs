#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kyub.Collections;
using Kyub.Extensions;
using Kyub;

namespace KyubEditor
{
    public class SerializableTypeCacheController
    {
        static List<SerializableTypeCache> s_cachedFilters = new List<SerializableTypeCache>();

        public static SerializableTypeCache GetCache(SerializableType p_currentType, System.Type p_filterType, bool p_acceptGeneric, bool p_acceptAbstract, bool p_acceptNull)
        {
            SerializableTypeCache v_selectedCache = null;
            foreach (SerializableTypeCache v_cache in s_cachedFilters)
            {
                if (v_cache.AcceptAbstract == p_acceptAbstract &&
                    v_cache.AcceptGeneric == p_acceptGeneric &&
                    v_cache.AcceptNull == p_acceptNull &&
                    v_cache.FilterType == p_filterType &&
                    ((v_cache.CurrentType == p_currentType) ||
                    (p_currentType != null && v_cache.CurrentType != null &&
                    p_currentType.CastedType == v_cache.CurrentType.CastedType &&
                    p_currentType.StringType == v_cache.CurrentType.StringType)))

                {
                    v_selectedCache = v_cache;
                    break;
                }
            }
            if (v_selectedCache == null)
            {
                v_selectedCache = new SerializableTypeCache(p_currentType, p_filterType, p_acceptGeneric, p_acceptAbstract, p_acceptNull);
                s_cachedFilters.Add(v_selectedCache);
            }
            return v_selectedCache;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnUnityReloadScripts()
        {
            s_cachedFilters.Clear();

        }
    }

    public class SerializableTypeCache
    {
        #region Private Variables

        protected SerializableType m_currentType = null;
        protected System.Type m_filterType = null;
        protected Assembly m_currentAssembly = null;
        protected Assembly[] m_possibleAssemblies = null;
        protected System.Type[] m_possibleTypesInCurrentAssembly = null;
        protected System.Type[] m_possibleTypesInAllAssemblies = null;
        protected string[] m_possibleAssembliesString = null;
        protected string[] m_possibleTypesInCurrentAssemblyString = null;
        protected string[] m_possibleTypesInAllAssembliesString = null;

        protected bool m_acceptGeneric = true;
        protected bool m_acceptAbstract = true;
        protected bool m_acceptNull = true;
        protected int m_selectedAssemblyIndex = -1;
        protected int m_selectedTypeIndexInAllAssemblies = -1;
        protected int m_selectedTypeIndexInCurrentAssembly = -1;

        #endregion

        #region Public Properties

        public SerializableType CurrentType
        {
            get
            {
                return m_currentType;
            }
        }

        public System.Type FilterType
        {
            get
            {
                return m_filterType;
            }
        }

        public Assembly CurrentAssembly
        {
            get
            {
                return m_currentAssembly;
            }
        }

        public Assembly[] PossibleAssemblies
        {
            get
            {
                return m_possibleAssemblies;
            }
        }

        public System.Type[] PossibleTypesInCurrentAssembly
        {
            get
            {
                return m_possibleTypesInCurrentAssembly;
            }
        }

        public System.Type[] PossibleTypesInAllAssemblies
        {
            get
            {
                return m_possibleTypesInAllAssemblies;
            }
        }

        public string[] PossibleAssembliesString
        {
            get
            {
                return m_possibleAssembliesString;
            }
        }

        public string[] PossibleTypesInCurrentAssemblyString
        {
            get
            {
                return m_possibleTypesInCurrentAssemblyString;
            }
        }

        public string[] PossibleTypesInAllAssembliesString
        {
            get
            {
                return m_possibleTypesInAllAssembliesString;
            }
        }

        public bool AcceptGeneric
        {
            get
            {
                return m_acceptGeneric;
            }
        }

        public bool AcceptAbstract
        {
            get
            {
                return m_acceptAbstract;
            }
        }

        public bool AcceptNull
        {
            get
            {
                return m_acceptNull;
            }
        }

        public int SelectedAssemblyIndex
        {
            get
            {
                return m_selectedAssemblyIndex;
            }
        }

        public int SelectedTypeIndexInAllAssemblies
        {
            get
            {
                return m_selectedTypeIndexInAllAssemblies;
            }
        }

        public int SelectedTypeIndexInCurrentAssembly
        {
            get
            {
                return m_selectedTypeIndexInCurrentAssembly;
            }
        }

        #endregion

        public SerializableTypeCache(SerializableType p_currentType, System.Type p_filterType, bool p_acceptGeneric, bool p_acceptAbstract, bool p_acceptNull)
        {
            m_currentType = p_currentType;
            m_filterType = p_filterType;
            m_acceptGeneric = p_acceptGeneric;
            m_acceptAbstract = p_acceptAbstract;
            m_acceptNull = p_acceptNull;
            BuildCache();
        }

        protected virtual void BuildCache()
        {
            System.Type v_safeFilterType = m_filterType == null ? (m_acceptAbstract ? typeof(object) : null) : m_filterType;
            string v_safeTypeNameInAssembly = GetSafeTypedNameInAssembly(v_safeFilterType);
            ArrayDict<Assembly, System.Type[]> v_assemblyTypesDict = AssemblyUtils.GetAssemblyTypesDict(false);
            List<Assembly> v_assemblies = new List<Assembly>();
            m_currentAssembly = m_currentType == null || m_currentType.CastedType == null ? null : m_currentType.CastedType.Assembly;

            //Filter Assemblies
            foreach (KVPair<Assembly, System.Type[]> v_pair in v_assemblyTypesDict)
            {
                if (v_pair != null && v_pair.Key != null)
                {
                    if (AssemblyContainsFilteredType(v_pair.Key, m_filterType, m_acceptGeneric, m_acceptAbstract))
                        v_assemblies.Add(v_pair.Key);
                    try
                    {
                        if (m_currentAssembly == null && m_currentType != null && ((SerializableType)m_currentType).StringType.Contains(v_pair.Key.FullName))
                            m_currentAssembly = v_pair.Key;
                    }
                    catch { }
                }
            }
            if (m_acceptNull)
                v_assemblies.Insert(0, null);

            if (m_currentAssembly == null && !m_acceptNull)
                m_currentAssembly = v_safeFilterType.Assembly;

            //Draw Popup Select Assembly
            m_possibleAssemblies = v_assemblies.ToArray();
            m_possibleAssembliesString = v_assemblies.GetStringList().ToArray();
            m_selectedAssemblyIndex = v_assemblies.Contains(m_currentAssembly) ? v_assemblies.IndexOf(m_currentAssembly) : -1;

            //Pick All Types if dont use Assembly Filter
            List<System.Type> v_assemblyTypes = new List<System.Type>();

            foreach (var v_assembly in v_assemblies)
            {
                try
                {
                    var v_types = v_assemblyTypesDict.GetValueWithKey(v_assembly);
                    if (v_types != null)
                    {
                        foreach (System.Type v_type in v_types)
                        {
                            if (v_type != null &&
                               (!v_type.IsGenericTypeDefinition || m_acceptGeneric) &&
                               (!v_type.IsAbstract || m_acceptAbstract) &&
                               (m_filterType == null ||
                                TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, m_filterType) ||
                                v_type.FullName.Contains(v_safeTypeNameInAssembly)))
                            {
                                v_assemblyTypes.Add(v_type);
                            }
                        }
                    }
                }
                catch { }
            }

            if (m_acceptNull)
                v_assemblyTypes.Insert(0, null);
            m_possibleTypesInAllAssemblies = v_assemblyTypes.ToArray();
            m_possibleTypesInAllAssembliesString = v_assemblyTypes.GetStringList().ToArray();
            if (m_currentType == null && !m_acceptNull)
                m_selectedTypeIndexInAllAssemblies = v_assemblyTypes.Contains(v_safeFilterType) ? v_assemblyTypes.IndexOf(v_safeFilterType) : -1;
            else
                m_selectedTypeIndexInAllAssemblies = v_assemblyTypes.Contains(m_currentType) ? v_assemblyTypes.IndexOf(m_currentType) : -1;


            //Filter Types in Assembly Current Assembly
            v_assemblyTypes.Clear();
            if (m_currentAssembly != null)
            {
                try
                {
                    var v_types = v_assemblyTypesDict.GetValueWithKey(m_currentAssembly);
                    if (v_types != null)
                    {
                        foreach (System.Type v_type in v_types)
                        {
                            if (v_type != null &&
                               (!v_type.IsGenericTypeDefinition || m_acceptGeneric) &&
                               (!v_type.IsAbstract || m_acceptAbstract) &&
                               (m_filterType == null ||
                                TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, m_filterType) ||
                                v_type.FullName.Contains(v_safeTypeNameInAssembly)))
                            {
                                v_assemblyTypes.Add(v_type);
                            }
                        }
                    }
                }
                catch { }
            }
            if (m_acceptNull)
                v_assemblyTypes.Insert(0, null);
            m_possibleTypesInCurrentAssembly = v_assemblyTypes.ToArray();
            m_possibleTypesInCurrentAssemblyString = v_assemblyTypes.GetStringList().ToArray();
            if (m_currentType == null && !m_acceptNull)
                m_selectedTypeIndexInCurrentAssembly = v_assemblyTypes.Contains(v_safeFilterType) ? v_assemblyTypes.IndexOf(v_safeFilterType) : -1;
            else
                m_selectedTypeIndexInCurrentAssembly = v_assemblyTypes.Contains(m_currentType) ? v_assemblyTypes.IndexOf(m_currentType) : -1;
        }

        private bool AssemblyContainsFilteredType(Assembly p_assembly, SerializableType p_filterType, bool p_acceptGenericDefinition, bool p_acceptAbstractDefinition)
        {
            if (p_filterType == null || p_filterType.CastedType == null)
                return true;
            if (p_assembly != null)
            {
                string v_safeTypeNameInAssembly = GetSafeTypedNameInAssembly(p_filterType);
                foreach (System.Type v_type in p_assembly.GetTypes())
                {
                    if (v_type != null &&
                        p_filterType != null &&
                        (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, p_filterType.CastedType) ||
                        v_type.FullName.Contains(v_safeTypeNameInAssembly)) &&
                        (p_acceptGenericDefinition || !v_type.IsGenericTypeDefinition) &&
                        (p_acceptAbstractDefinition || !v_type.IsAbstract))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //Used to find correct type in assembly when filter is generic
        public static string GetSafeTypedNameInAssembly(SerializableType p_type)
        {
            if (p_type != null)
            {
                string[] v_splittedValues = p_type.StringType.Split('`');
                string v_typeString = v_splittedValues.Length > 0 ? v_splittedValues[0] : (p_type.CastedType != null ? p_type.CastedType.FullName : "");
                string v_genericArgString = v_splittedValues.Length > 1 && v_splittedValues[1].Length > 1 ? v_splittedValues[1][0] + "" : "";
                if (!string.IsNullOrEmpty(v_genericArgString))
                    v_typeString += "`" + v_genericArgString;
                return v_typeString;
            }
            return "";
        }
    }
}
#endif