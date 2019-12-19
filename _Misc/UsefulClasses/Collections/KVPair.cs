using UnityEngine;
using System.Collections;
using Kyub.Extensions;

namespace Kyub.Collections
{
    public interface IKVPair
    {
        object Key
        {
            get;
            set;
        }

        object Value
        {
            get;
            set;
        }
    }

    [System.Serializable]
    public class KVPair<TKey, TValue> : FoldOutStruct, IKVPair
    {
        #region Private Variables

        [SerializeField, System.Xml.Serialization.XmlAttributeAttribute("Key")]
        TKey m_key;
        [SerializeField, System.Xml.Serialization.XmlAttributeAttribute("Value")]
        TValue m_value;

        #endregion

        #region Public Properties

        object IKVPair.Key
        {
            get
            {
                return m_key;
            }
            set
            {
                try
                {
                    m_key = (TKey)value;
                }
                catch { }
            }
        }

        object IKVPair.Value
        {
            get
            {
                return m_value;
            }
            set
            {
                try
                {
                    m_value = (TValue)value;
                }
                catch { }
            }
        }

        //Key is the Comparer
        public TKey Key
        {
            get
            {
                return m_key;
            }
            set
            {
                m_key = value;
            }
        }

        //Value is the Object
        public TValue Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value;
            }
        }

        #endregion

        #region Constructors

        public KVPair()
        {
        }

        public KVPair(TKey p_key, TValue p_value)
        {
            m_key = p_key;
            m_value = p_value;
        }

        #endregion

        #region Helper Methods

        public override bool Equals(object p_value)
        {
            if (p_value == this)
                return true;
            if (!EqualityExtension.IsNull(p_value))
            {
                try
                {
                    KVPair<TKey, TValue> v_castedObject = p_value as KVPair<TKey, TValue>;
                    if (v_castedObject != null)
                    {
                        if (((EqualityExtension.IsNull(v_castedObject.Key) && EqualityExtension.IsNull(this.Key)) || v_castedObject.Key.Equals(this.Key)) &&
                           ((EqualityExtension.IsNull(v_castedObject.Value) && EqualityExtension.IsNull(this.Value)) || v_castedObject.Value.Equals(this.Value)))
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
