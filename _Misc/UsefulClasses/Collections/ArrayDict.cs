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
        IKVPair GetPairWithKey(object key);
        IKVPair GetPairWithValue(object value);
        object GetValueWithKey(object key);
        object GetKeyWithValue(object value);
        bool HasDuplicatedKey(object key);
        bool ContainsKey(object key);
        bool ContainsValue(object value);
        void Add(object key, object value);
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
                List<TKey> keys = new List<TKey>();
                foreach (var pair in this)
                {
                    if (pair != null)
                    {
                        keys.Add(pair.Key);
                    }
                }
                return keys.ToArray();
            }
        }

        public TValue[] Values
        {
            get
            {
                List<TValue> keys = new List<TValue>();
                foreach (var pair in this)
                {
                    if (pair != null)
                    {
                        keys.Add(pair.Value);
                    }
                }
                return keys.ToArray();
            }

        }

        #endregion

        #region Dictionary Functions

        public bool HasDuplicatedKey(TKey key)
        {
            return GetAllValuesWithKey(key).Count > 1;
        }

        public TPair GetPairWithValue(TValue value)
        {
            foreach (TPair pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValues.Value, value))
                        return pairValues;
                }
                catch { }
            }
            return default(TPair);
        }

        public TPair GetPairWithKey(TKey key)
        {
            foreach (TPair pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValues.Key, key))
                        //if(key.Equals(pairValues.Comparer))
                        return pairValues;
                }
                catch { }
            }
            return default(TPair);
        }

        public TKey GetKeyWithValue(TValue value)
        {
            foreach (TPair pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValues.Value, value))
                        //if(value.Equals(pairValues.Object))
                        return pairValues.Key;
                }
                catch { }
            }
            return default(TKey);
        }

        public TValue GetValueWithKey(TKey key)
        {
            foreach (TPair pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValues.Key, key))
                        //if(key.Equals(pairValues.Comparer))
                        return pairValues.Value;
                }
                catch { }
            }
            return default(TValue);
        }

        public ArrayList<TValue> GetAllValuesWithKey(TKey key)
        {
            ArrayList<TValue> values = new ArrayList<TValue>();
            foreach (TPair pairValues in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValues.Key, key))
                        //if(key.Equals(pairValues.Comparer))
                        values.Add(pairValues.Value);
                }
                catch { }
            }
            return values;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            bool sucess = ContainsKey(key);
            value = sucess? GetValueWithKey(key) : default(TValue);
            return sucess;
        }

        public void RemovePairsWithNullValuesOrKeys()
        {
            RemoveNulls();
            RemovePairsWithNullValues();
            RemovePairsWithNullKeys();
        }

        public void RemovePairsWithNullValues()
        {
            ArrayList<TPair> newList = new ArrayList<TPair>();
            foreach (TPair pairValue in this)
            {
                try
                {
                    if (!EqualityExtension.IsNull(pairValue.Value))
                        newList.Add(pairValue);
                }
                catch { }
            }
            this.Clear();
            foreach (TPair pairValue in newList)
                this.AddWithoutCallEvents(pairValue);
        }

        public void RemovePairsWithNullKeys()
        {
            ArrayList<TPair> newList = new ArrayList<TPair>();
            foreach (TPair pairValue in this)
            {
                try
                {
                    if (!EqualityExtension.IsNull(pairValue.Key))
                        newList.Add(pairValue);
                }
                catch { }
            }
            this.Clear();
            foreach (TPair pairValue in newList)
                this.AddWithoutCallEvents(pairValue);
        }

        public bool ContainsKey(TKey key)
        {
            foreach (TPair pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValue.Key, key))
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public bool ContainsValue(TValue value)
        {
            foreach (TPair pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValue.Value, value))
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            try
            {
                TPair pair = new TPair();
                pair.Value = value;
                pair.Key = key;
                base.Add(pair);
            }
            catch { }
        }

        public void AddChecking(TKey key, TValue value)
        {
            bool found = false;
            foreach (TPair pair in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pair.Value, value) &&
                       EqualityExtension.Equals(pair.Key, key))
                    {
                        found = true;
                        break;
                    }
                }
                catch { }
            }
            if (!found)
                Add(key, value);
        }

        public void AddReplacing(TKey key, TValue value)
        {
            bool found = false;
            for (int i = 0; i < this.Count; i++)
            {
                try
                {
                    if (EqualityExtension.Equals(this[i].Key, key))
                    {
                        found = true;
                        TPair pair = new TPair();
                        pair.Value = value;
                        pair.Key = key;
                        this[i] = pair;
                        break;
                    }
                }
                catch { }
            }
            if (!found)
                Add(key, value);
        }

        public void AddRange(IDictionary<TKey, TValue> dictToMerge)
        {
            if (dictToMerge != null)
            {
                foreach (TKey key in dictToMerge.Keys)
                {
                    try
                    {
                        AddReplacing(key, dictToMerge[key]);
                    }
                    catch { }
                }
            }
        }

        public void AddRange<TParamPair>(GenericArrayDict<TParamPair, TKey, TValue> dictToMerge) where TParamPair : KVPair<TKey, TValue>, new()
        {
            if (dictToMerge != null)
            {
                foreach (TParamPair pair in dictToMerge)
                {
                    try
                    {
                        TValue value = pair.Value;
                        TKey key = pair.Key;
                        AddReplacing(key, value);
                    }
                    catch { }
                }
            }
        }

        public bool Remove(TValue value)
        {
            TPair removePair = default(TPair);
            foreach (TPair pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValue.Value, value))
                    {
                        removePair = pairValue;
                        break;
                    }
                }
                catch { }
            }
            return base.Remove(removePair);
        }

        public bool RemoveChecking(TValue value)
        {
            TPair removePair = default(TPair);
            foreach (TPair pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValue.Value, value))
                    {
                        removePair = pairValue;
                        break;
                    }
                }
                catch { }
            }
            return this.RemoveChecking(removePair);
        }

        public bool RemoveByKey(TKey key)
        {
            TPair removePair = default(TPair);
            foreach (TPair pairValue in this)
            {
                try
                {
                    if (EqualityExtension.Equals(pairValue.Key, key))
                    {
                        removePair = pairValue;
                        break;
                    }
                }
                catch { }
            }
            return this.RemoveChecking(removePair);
        }

        public virtual GenericArrayDict<TPair, TKey, TValue> CloneDict()
        {
            try
            {
                IArrayDict clonedDict = System.Activator.CreateInstance(GetType()) as GenericArrayDict<TPair, TKey, TValue>;
                foreach (IKVPair pair in this)
                {
                    try
                    {
                        clonedDict.Add(pair.Key, pair.Value);
                    }
                    catch { }
                }
                return clonedDict as GenericArrayDict<TPair, TKey, TValue>;
            }
            catch { }
            return null;
        }

        public virtual Dictionary<TKey, TValue> ToDict()
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            foreach (var pair in this)
            {
                if (pair != null && !dict.ContainsKey(pair.Key))
                {
                    dict.Add(pair.Key, pair.Value);
                }
            }
            return dict;
        }

        #endregion

        #region IArrayDict Implementations

        IKVPair IArrayDict.GetPairWithKey(object key)
        {
            return GetPairWithKey((TKey)key);
        }

        IKVPair IArrayDict.GetPairWithValue(object value)
        {
            return GetPairWithValue((TValue)value);
        }

        object IArrayDict.GetValueWithKey(object key)
        {
            return GetValueWithKey((TKey)key);
        }

        object IArrayDict.GetKeyWithValue(object value)
        {
            return GetKeyWithValue((TValue)value);
        }

        bool IArrayDict.ContainsKey(object key)
        {
            return ContainsKey((TKey)key);
        }

        bool IArrayDict.ContainsValue(object value)
        {
            return ContainsValue((TValue)value);
        }

        void IArrayDict.Add(object key, object value)
        {
            Add((TKey)key, (TValue)value);
        }

        bool IArrayDict.HasDuplicatedKey(object key)
        {
            return HasDuplicatedKey((TKey)key);
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
                    KVPair<TKey, TValue> pair = new KVPair<TKey, TValue>();
                    pair.Value = _values[i];
                    pair.Key = _keys[i];
                    this.AddWithoutCallEvents(pair);
                }
            }
            else
            {
                GetAllKeyValuesFromPairs();
            }
        }

        protected override void OnAdd(KVPair<TKey, TValue> pair)
        {
            if (EnableUnityInspectorSupport)
            {
                if (pair != null)
                    _values.Add(pair.Value);
                if (pair != null)
                    _keys.Add(pair.Key);
            }
            else
                GetAllKeyValuesFromPairs();
        }

        protected override void OnRemove(KVPair<TKey, TValue> pair)
        {
            if (EnableUnityInspectorSupport)
            {
                if (pair != null)
                    _values.RemoveChecking(pair.Value, false);
                if (pair != null)
                    _keys.RemoveChecking(pair.Key, false);
            }
            else
                GetAllKeyValuesFromPairs();
        }

        protected virtual void GetAllKeyValuesFromPairs()
        {
            _values.Clear();
            _keys.Clear();
            int counter = 0;
            foreach (KVPair<TKey, TValue> pair in this)
            {
                if (this.Count > counter)
                {
                    _values.Add(pair.Value);
                    _keys.Add(pair.Key);
                    counter++;
                    break;
                }
            }
        }
        #endregion
    }
}