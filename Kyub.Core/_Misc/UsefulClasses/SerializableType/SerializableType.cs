using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub
{
    [System.Serializable]
    public class SerializableType : Kyub.FoldOutStruct
    {
        #region Private Variables

        [SerializeField]
        string m_stringType = "";
        System.Type m_castedType = null;

        #endregion

        #region Public Properties

        public string StringType
        {
            get
            {
                if (m_stringType == null)
                    m_stringType = "";
                return m_stringType;
            }
            set
            {
                if (m_stringType == value)
                    return;
                m_stringType = value;
                //CheckIfNeedReapplyType(true);
            }
        }

        public System.Type CastedType
        {
            get
            {
                CheckIfNeedReapplyType(false);
                return m_castedType;
            }
            set
            {
                if (m_castedType == value)
                    return;
                m_castedType = value;
                m_stringType = GetStringTypeFromType(value);
            }
        }

        #endregion

        #region Construtor

        public SerializableType()
        {
        }

        public SerializableType(System.Type p_type)
        {
            m_castedType = p_type;
            m_stringType = GetStringTypeFromType(p_type);
        }

        #endregion

        #region Helper Functions

        protected string GetStringTypeFromType(System.Type p_type)
        {
		    return p_type != null? p_type.FullName + ", " + p_type.Assembly().FullName : "";
        }

        public void CheckIfNeedReapplyType(bool p_force = false)
        {
            if ((m_castedType == null && !string.IsNullOrEmpty(m_stringType)) || p_force)
            {
                if (!string.IsNullOrEmpty(m_stringType))
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    string v_assemblyName = "";
                    string v_fullNameType = "";
                    string[] v_splits = m_stringType.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (v_splits.Length > 4)
                        v_assemblyName = v_splits[v_splits.Length - 4]; // Type, AssemblyName, Version, Culture, PublicToken

                    for (int i = 0; i < v_splits.Length-4; i++)
                    {
                        if (!string.IsNullOrEmpty(v_fullNameType))
                            v_fullNameType += ",";
                        v_fullNameType += v_splits[i];
                    }
                    var v_assembly = !string.IsNullOrEmpty(v_assemblyName)? Assembly.Load(v_assemblyName) : null;
                    if(v_assembly != null && !string.IsNullOrEmpty(v_fullNameType))
                        m_castedType = v_assembly.GetType(v_fullNameType);

#else
                    m_castedType = System.Type.GetType(m_stringType);
#endif
                }
            }
        }

#endregion

#region Operator Overloads

        public static implicit operator System.Type(SerializableType p_type)
        {
            return p_type != null ? p_type.CastedType : null;
        }

        public static implicit operator SerializableType(System.Type p_type)
        {
            return new SerializableType(p_type);
        }

        // Override the Object.Equals(object o) method:
        public override bool Equals(object p_object)
        {
            try
            {
                if (p_object is System.Type)
                    return ((System.Type)this == (System.Type)p_object);
                else
                    return (this == (SerializableType)p_object);
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return CastedType != null ? CastedType.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return GetStringTypeFromType(CastedType);
        }

#endregion
    }
}