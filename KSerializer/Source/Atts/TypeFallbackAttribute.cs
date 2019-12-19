using UnityEngine;
using System.Collections;

namespace Kyub.Serialization
{
    /// <summary>
    /// Use this attribute if you want to Cast an System.Type to other name when Deserializing. 
    /// Useful when deserializing obsolete classes so it is possible to convert it to other class instead.
    /// If the Type and String is null or Empty the serializer will not write the type.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TypeFallbackAttribute : System.Attribute
    {
        string m_stringTypeFallback = "";
        System.Type m_typeFallBack = null;

        public string StringTypeFallback
        {
            get
            {
                if (string.IsNullOrEmpty(m_stringTypeFallback) && m_typeFallBack != null)
                    m_stringTypeFallback = m_typeFallBack.AssemblyQualifiedName;
                return m_stringTypeFallback;
            }
        }

        public System.Type TypeFallBack
        {
            get
            {
                try
                {
                    if (m_typeFallBack == null && !string.IsNullOrEmpty(m_stringTypeFallback))
                        m_typeFallBack = System.Type.GetType(m_stringTypeFallback);
                }
                catch { }
                return m_typeFallBack;
            }
        }

        public TypeFallbackAttribute()
        {
        }

        public TypeFallbackAttribute(System.Type p_typeToFallback)
        {
            m_typeFallBack = p_typeToFallback;
        }

        public TypeFallbackAttribute(string p_stringTypeFallback)
        {
            m_stringTypeFallback = p_stringTypeFallback;
        }
    }
}
