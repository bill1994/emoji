using UnityEngine;
using System.Collections;

namespace Kyub
{
    public enum DependentDrawOptionEnum { AlwaysDrawField, ReadOnlyFieldWhenNotExpectedValue, DontDrawFieldWhenNotExpectedValue }

    public class DependentFieldAttribute : SpecificFieldAttribute
    {
        #region Properties

        [SerializeField]
        string m_dependentFieldName = "";
        public string DependentFieldName
        {
            get
            {
                return m_dependentFieldName;
            }
            set
            {
                if (m_dependentFieldName == value)
                    return;
                m_dependentFieldName = value;
            }
        }

        [SerializeField]
        DependentDrawOptionEnum m_drawOption = DependentDrawOptionEnum.ReadOnlyFieldWhenNotExpectedValue;
        public DependentDrawOptionEnum DrawOption
        {
            get
            {
                return m_drawOption;
            }
            set
            {
                if (m_drawOption == value)
                    return;
                m_drawOption = value;
            }
        }

        [SerializeField]
        object m_valueToTrigger = null;
        public object ValueToTrigger
        {
            get
            {
                return m_valueToTrigger;
            }
            set
            {
                if (m_valueToTrigger == value)
                    return;
                m_valueToTrigger = value;
            }
        }

        [SerializeField]
        bool m_useNotEqualComparer = false;
        public bool UseNotEqualComparer
        {
            get
            {
                return m_useNotEqualComparer;
            }
            set
            {
                if (m_useNotEqualComparer == value)
                    return;
                m_useNotEqualComparer = value;
            }
        }


        #endregion

        #region Constructor

        public DependentFieldAttribute(string p_dependentFieldName, object p_valueToCompare) : base(typeof(object))
        {
            m_dependentFieldName = p_dependentFieldName;
            m_valueToTrigger = p_valueToCompare;
        }

        public DependentFieldAttribute(string p_dependentFieldName, object p_valueToCompare, DependentDrawOptionEnum p_drawOption) : base(typeof(object))
        {
            m_dependentFieldName = p_dependentFieldName;
            m_drawOption = p_drawOption;
            m_valueToTrigger = p_valueToCompare;
        }

        public DependentFieldAttribute(string p_dependentFieldName, object p_valueToCompare, bool p_readOnly) : base(p_readOnly, typeof(object))
        {
            m_dependentFieldName = p_dependentFieldName;
            m_valueToTrigger = p_valueToCompare;
        }

        public DependentFieldAttribute(string p_dependentFieldName, object p_valueToCompare, DependentDrawOptionEnum p_drawOption, bool p_readOnly) : base(p_readOnly, typeof(object))
        {
            m_dependentFieldName = p_dependentFieldName;
            m_drawOption = p_drawOption;
            m_valueToTrigger = p_valueToCompare;
        }

        public DependentFieldAttribute(string p_dependentFieldName, object p_valueToCompare, DependentDrawOptionEnum p_drawOption, bool p_readOnly, bool p_useNotEqualComparer) : base(p_readOnly, typeof(object))
        {
            m_dependentFieldName = p_dependentFieldName;
            m_drawOption = p_drawOption;
            m_valueToTrigger = p_valueToCompare;
            m_useNotEqualComparer = p_useNotEqualComparer;
        }

        public DependentFieldAttribute(bool p_readOnly, System.Type p_acceptedType, string p_dependentFieldName, object p_valueToCompare, DependentDrawOptionEnum p_drawOption = DependentDrawOptionEnum.ReadOnlyFieldWhenNotExpectedValue, bool p_useNotEqualComparer = false) : base(p_readOnly, p_acceptedType)
        {
            m_dependentFieldName = p_dependentFieldName;
            m_drawOption = p_drawOption;
            m_valueToTrigger = p_valueToCompare;
            m_useNotEqualComparer = p_useNotEqualComparer;
        }

        #endregion
    }
}
