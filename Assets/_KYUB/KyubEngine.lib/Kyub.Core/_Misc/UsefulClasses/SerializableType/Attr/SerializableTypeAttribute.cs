using UnityEngine;
using System.Collections;

namespace Kyub
{
    public class SerializableTypeAttribute : DependentFieldAttribute
    {
        #region Properties

        [SerializeField]
        System.Type m_filterType = null;
        public System.Type FilterType
        {
            get
            {
                return m_filterType;
            }
            set
            {
                if (m_filterType == value)
                    return;
                m_filterType = value;
            }
        }

        [SerializeField]
        bool m_acceptGenericDefinitions = false;
        public bool AcceptGenericDefinitions
        {
            get
            {
                return m_acceptGenericDefinitions;
            }
            set
            {
                if (m_acceptGenericDefinitions == value)
                    return;
                m_acceptGenericDefinitions = value;
            }
        }

        [SerializeField]
        bool m_acceptAbstractDefinitions = false;
        public bool AcceptAbstractDefinitions
        {
            get
            {
                return m_acceptAbstractDefinitions;
            }
            set
            {
                if (m_acceptAbstractDefinitions == value)
                    return;
                m_acceptAbstractDefinitions = value;
            }
        }

        [SerializeField]
        bool m_acceptNulls = false;
        public bool AcceptNulls
        {
            get
            {
                return m_acceptNulls;
            }
            set
            {
                if (m_acceptNulls == value)
                    return;
                m_acceptNulls = value;
            }
        }

        [SerializeField]
        bool m_filterAssemblies = false;
        public bool FilterAssemblies
        {
            get
            {
                return m_filterAssemblies;
            }
            set
            {
                if (m_filterAssemblies == value)
                    return;
                m_filterAssemblies = value;
            }
        }

        #endregion

        #region Constructor

        public SerializableTypeAttribute() : base(false, typeof(SerializableType), "", null)
        {
        }

        public SerializableTypeAttribute(bool p_readOnly) : base(p_readOnly, typeof(SerializableType), "", null)
        {
        }

        public SerializableTypeAttribute(System.Type p_filterType, bool p_acceptGenericDefinition = false, bool p_acceptAbstractDefinitions = false, bool p_acceptNulls = false, bool p_filterAssemblies = false, bool p_readOnly = false, string p_dependentFieldName = "", object p_valueToCompare = null, DependentDrawOptionEnum p_drawOption = DependentDrawOptionEnum.ReadOnlyFieldWhenNotExpectedValue) : base(p_readOnly, typeof(SerializableType), p_dependentFieldName, p_valueToCompare, p_drawOption)
        {
            m_filterType = p_filterType;
            m_acceptGenericDefinitions = p_acceptGenericDefinition;
            m_acceptAbstractDefinitions = p_acceptAbstractDefinitions;
            m_acceptNulls = p_acceptNulls;
            m_filterAssemblies = p_filterAssemblies;

        }

        #endregion
    }
}
