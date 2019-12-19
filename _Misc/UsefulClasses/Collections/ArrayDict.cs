using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Kyub.Collections;
using Kyub.Extensions;

namespace Kyub.Collections
{
    public interface IArrayDict : IArrayList
    {
        IKVPair GetPairWithKey(object p_key);
        IKVPair GetPairWithValue(object p_value);
        object GetValueWithKey(object p_key);
        object GetKeyWithValue(object p_value);
        bool HasDuplicatedKey(object p_key);
        bool ContainsKey(object p_key);
        bool ContainsValue(object p_value);
        void Add(object p_key, object p_value);
    }

    public interface IUnityInspectorArrayDict : IArrayDict
    {
        bool EnableUnityInspectorSupport
        {
            get;
            set;
        }
    }

    [System.Serializable]
    //Generic Dictionary Exists if you want to create your own serializable dict, so you can pass your own KVPair to dict
    public abstract class GenericArrayDict<TPair, TKey, TValue> : ArrayList<TPair>, IArrayDict where TPair : KVPair<TKey, TValue>, new()
    {
        #region Properties

        public TKey[] Keys
        {
            get
            {
                List<TKey> v_keys = new List<TKey>();
                foreach (var v_pair in this)
                {
                    if (v_pair != null)
                    {
                        v_keys.Add(v_pair.Key);
                    }
                }
                return v_keys.ToArray();
            }
        }

        public TValue[] Values
        {
            get
            {
                List<TValue> v_keys = new List<TValue>();
                foreach (var v_pair in this)
                {
                    if (v_pair != null)
                    {
                        v_keys.Add(v_pair.Value);
                    }
                }
                return v_keys.ToArray();
            }

        }

        #endregion

        #region Dictionary Functions

        public bool HasDuplicatedKey(TKey p_key)
        {
            return GetAllValuesWithKey(p_key).Count > 1;
        }

        public TPair GetPairWithValue(TValue p_value)
        {
            foreach (TPair v_pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValues.Value, p_value))
                        return v_pairValues;
                }
                catch { }
            }
            return default(TPair);
        }

        public TPair GetPairWithKey(TKey p_key)
        {
            foreach (TPair v_pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValues.Key, p_key))
                        //if(p_key.Equals(v_pairValues.Comparer))
                        return v_pairValues;
                }
                catch { }
            }
            return default(TPair);
        }

        public TKey GetKeyWithValue(TValue p_value)
        {
            foreach (TPair v_pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValues.Value, p_value))
                        //if(p_value.Equals(v_pairValues.Object))
                        return v_pairValues.Key;
                }
                catch { }
            }
            return default(TKey);
        }

        public TValue GetValueWithKey(TKey p_key)
        {
            foreach (TPair v_pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValues.Key, p_key))
                        //if(p_key.Equals(v_pairValues.Comparer))
                        return v_pairValues.Value;
                }
                catch { }
            }
            return default(TValue);
        }

        public ArrayList<TValue> GetAllValuesWithKey(TKey p_key)
        {
            ArrayList<TValue> v_values = new ArrayList<TValue>();
            foreach (TPair v_pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValues.Key, p_key))
                        //if(p_key.Equals(v_pairValues.Comparer))
                        v_values.Add(v_pairValues.Value);
                }
                catch { }
            }
            return v_values;
        }

        public bool TryGetValue(TKey p_key, out TValue p_value)
        {
            bool v_sucess = ContainsKey(p_key);
            p_value = v_sucess? GetValueWithKey(p_key) : default(TValue);
            return v_sucess;
        }

        public void RemovePairsWithNullValuesOrKeys()
        {
            RemoveNulls();
            RemovePairsWithNullValues();
            RemovePairsWithNullKeys();
        }

        public void RemovePairsWithNullValues()
        {
            ArrayList<TPair> v_newList = new ArrayList<TPair>();
            foreach (TPair v_pairValue in this)
            {
                try
                {
                    if (!EqualityExtension.IsNull(v_pairValue.Value))
                        v_newList.Add(v_pairValue);
                }
                catch { }
            }
            this.Clear();
            foreach (TPair v_pairValue in v_newList)
                this.AddWithoutCallEvents(v_pairValue);
        }

        public void RemovePairsWithNullKeys()
        {
            ArrayList<TPair> v_newList = new ArrayList<TPair>();
            foreach (TPair v_pairValue in this)
            {
                try
                {
                    if (!EqualityExtension.IsNull(v_pairValue.Key))
                        v_newList.Add(v_pairValue);
                }
                catch { }
            }
            this.Clear();
            foreach (TPair v_pairValue in v_newList)
                this.AddWithoutCallEvents(v_pairValue);
        }

        public bool ContainsKey(TKey p_key)
        {
            foreach (TPair v_pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValue.Key, p_key))
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public bool ContainsValue(TValue p_value)
        {
            foreach (TPair v_pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValue.Value, p_value))
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public void Add(TKey p_key, TValue p_value)
        {
            try
            {
                TPair v_pair = new TPair();
                v_pair.Value = p_value;
                v_pair.Key = p_key;
                base.Add(v_pair);
            }
            catch { }
        }

        public void AddChecking(TKey p_key, TValue p_value)
        {
            bool v_found = false;
            foreach (TPair v_pair in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pair.Value, p_value) &&
                       EqualityExtension.Equals(v_pair.Key, p_key))
                    {
                        v_found = true;
                        break;
                    }
                }
                catch { }
            }
            if (!v_found)
                Add(p_key, p_value);
        }

        public void AddReplacing(TKey p_key, TValue p_value)
        {
            bool v_found = false;
            for (int i = 0; i < this.Count; i++)
            {
                try
                {
                    if (EqualityExtension.Equals(this[i].Key, p_key))
                    {
                        v_found = true;
                        TPair v_pair = new TPair();
                        v_pair.Value = p_value;
                        v_pair.Key = p_key;
                        this[i] = v_pair;
                        break;
                    }
                }
                catch { }
            }
            if (!v_found)
                Add(p_key, p_value);
        }

        public void AddRange(IDictionary<TKey, TValue> p_dictToMerge)
        {
            if (p_dictToMerge != null)
            {
                foreach (TKey v_key in p_dictToMerge.Keys)
                {
                    try
                    {
                        AddReplacing(v_key, p_dictToMerge[v_key]);
                    }
                    catch { }
                }
            }
        }

        public void AddRange<TParamPair>(GenericArrayDict<TParamPair, TKey, TValue> p_dictToMerge) where TParamPair : KVPair<TKey, TValue>, new()
        {
            if (p_dictToMerge != null)
            {
                foreach (TParamPair v_pair in p_dictToMerge)
                {
                    try
                    {
                        TValue v_value = v_pair.Value;
                        TKey v_key = v_pair.Key;
                        AddReplacing(v_key, v_value);
                    }
                    catch { }
                }
            }
        }

        public bool Remove(TValue p_value)
        {
            TPair v_removePair = default(TPair);
            foreach (TPair v_pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValue.Value, p_value))
                    {
                        v_removePair = v_pairValue;
                        break;
                    }
                }
                catch { }
            }
            return base.Remove(v_removePair);
        }

        public bool RemoveChecking(TValue p_value)
        {
            TPair v_removePair = default(TPair);
            foreach (TPair v_pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValue.Value, p_value))
                    {
                        v_removePair = v_pairValue;
                        break;
                    }
                }
                catch { }
            }
            return this.RemoveChecking(v_removePair);
        }

        public bool RemoveByKey(TKey p_key)
        {
            TPair v_removePair = default(TPair);
            foreach (TPair v_pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(v_pairValue.Key, p_key))
                    {
                        v_removePair = v_pairValue;
                        break;
                    }
                }
                catch { }
            }
            return this.RemoveChecking(v_removePair);
        }

        public virtual GenericArrayDict<TPair, TKey, TValue> CloneDict()
        {
            try
            {
                IArrayDict v_clonedDict = System.Activator.CreateInstance(GetType()) as GenericArrayDict<TPair, TKey, TValue>;
                foreach (IKVPair v_pair in this)
                {
                    try
                    {
                        v_clonedDict.Add(v_pair.Key, v_pair.Value);
                    }
                    catch { }
                }
                return v_clonedDict as GenericArrayDict<TPair, TKey, TValue>;
            }
            catch { }
            return null;
        }

        public virtual Dictionary<TKey, TValue> ToDict()
        {
            Dictionary<TKey, TValue> v_dict = new Dictionary<TKey, TValue>();
            foreach (var v_pair in this)
            {
                if (v_pair != null && !v_dict.ContainsKey(v_pair.Key))
                {
                    v_dict.Add(v_pair.Key, v_pair.Value);
                }
            }
            return v_dict;
        }

        #endregion

        #region IArrayDict Implementations

        IKVPair IArrayDict.GetPairWithKey(object p_key)
        {
            return GetPairWithKey((TKey)p_key);
        }

        IKVPair IArrayDict.GetPairWithValue(object p_value)
        {
            return GetPairWithValue((TValue)p_value);
        }

        object IArrayDict.GetValueWithKey(object p_key)
        {
            return GetValueWithKey((TKey)p_key);
        }

        object IArrayDict.GetKeyWithValue(object p_value)
        {
            return GetKeyWithValue((TValue)p_value);
        }

        bool IArrayDict.ContainsKey(object p_key)
        {
            return ContainsKey((TKey)p_key);
        }

        bool IArrayDict.ContainsValue(object p_value)
        {
            return ContainsValue((TValue)p_value);
        }

        void IArrayDict.Add(object p_key, object p_value)
        {
            Add((TKey)p_key, (TValue)p_value);
        }

        bool IArrayDict.HasDuplicatedKey(object p_key)
        {
            return HasDuplicatedKey((TKey)p_key);
        }

        #endregion
    }

    [System.Serializable]
    public class ArrayDict<TKey, TValue> : GenericArrayDict<KVPair<TKey, TValue>, TKey, TValue>
    {
    }

    //Implement this guy if you want to show _keys and _values in unity default inspector (Not a good idea)
    public class UnityInspectorArrayDict<TKey, TValue> : ArrayDict<TKey, TValue>, ISerializationCallbackReceiver, IUnityInspectorArrayDict
    {
        #region Serializable CallBack Variables

        //Only use this property when you are not using a custom drawers and want to edit dictionary in default Unity Inspector (Bad Idea).
        // This will force AotDictionary to sincronize all itens with _values and _keys so if you add and value to dict with it enabled the value will be erased.. 
        // so you can only add items inside dict using _keys and _values when _enableUnityInspectorSupport is enabled
        [SerializeField]
        bool _enableUnityInspectorSupport = false;
        [SerializeField, DependentField("_enableSerializationCallback", true)]
        List<TValue> _values = new List<TValue>();
        [SerializeField, DependentField("_enableSerializationCallback", true)]
        List<TKey> _keys = new List<TKey>();

        #endregion

        #region Serializable CallBack Properties

        public bool EnableUnityInspectorSupport
        {
            get
            {
                return _enableUnityInspectorSupport;
            }
            set
            {
                if (_enableUnityInspectorSupport == value)
                    return;
                _enableUnityInspectorSupport = value;
            }
        }

        #endregion

        #region Serializable CallBacks Functions

        // save the dictionary to lists
        public virtual void OnBeforeSerialize()
        {
            if (EnableUnityInspectorSupport)
            {
                if (Application.isPlaying)
                {
                    _values.Clear();
                    _keys.Clear();
                    foreach (var pair in this)
                    {
                        _values.Add(pair.Value);
                        _keys.Add(pair.Key);
                    }
                }
            }
            else
            {
                GetAllKeyValuesFromPairs();
            }
        }

        // load dictionary from lists
        public virtual void OnAfterDeserialize()
        {
            if (EnableUnityInspectorSupport)
            {
                this.Clear();
                for (int i = 0; i < Mathf.Min(_values.Count, _keys.Count); i++)
                {
                    KVPair<TKey, TValue> v_pair = new KVPair<TKey, TValue>();
                    v_pair.Value = _values[i];
                    v_pair.Key = _keys[i];
                    this.AddWithoutCallEvents(v_pair);
                }
            }
            else
            {
                GetAllKeyValuesFromPairs();
            }
        }

        protected override void OnAdd(KVPair<TKey, TValue> p_pair)
        {
            if (EnableUnityInspectorSupport)
            {
                if (p_pair != null)
                    _values.Add(p_pair.Value);
                if (p_pair != null)
                    _keys.Add(p_pair.Key);
            }
            else
                GetAllKeyValuesFromPairs();
        }

        protected override void OnRemove(KVPair<TKey, TValue> p_pair)
        {
            if (EnableUnityInspectorSupport)
            {
                if (p_pair != null)
                    _values.RemoveChecking(p_pair.Value, false);
                if (p_pair != null)
                    _keys.RemoveChecking(p_pair.Key, false);
            }
            else
                GetAllKeyValuesFromPairs();
        }

        protected virtual void GetAllKeyValuesFromPairs()
        {
            _values.Clear();
            _keys.Clear();
            int v_counter = 0;
            foreach (KVPair<TKey, TValue> pair in this)
            {
                if (this.Count > v_counter)
                {
                    _values.Add(pair.Value);
                    _keys.Add(pair.Key);
                    v_counter++;
                    break;
                }
            }
        }
        #endregion
    }
}