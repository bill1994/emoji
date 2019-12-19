using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Kyub.Extensions;

namespace Kyub.Collections
{
    public interface IArrayList
    {
        IList Buffer
        {
            get;
        }

        int Count
        {
            get;
        }

        bool FoldOut
        {
            get;
            set;
        }

        object this[int i]
        {
            get;
            set;
        }

        bool Add(object p_object);
        bool Remove(object p_item);
        void RemoveAt(int p_index);
        void Clear();
        bool Contains(object p_value);
    }

    [System.Serializable]
    public class ArrayList<T> : FoldOutStruct, IArrayList
    {
        #region Internal Variables

        [SerializeField]
        private List<T> buffer = null;
        private System.Type type = typeof(T);

        #endregion

        #region  Public Properties

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Type ContainerType
        {
            get
            {
                return type;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int Count
        {
            get
            {
                return Buffer != null? Buffer.Count : 0;
            }
        }

        public List<T> Buffer
        {
            get
            {
                if (buffer == null)
                {
                    buffer = new List<T>();
                }
                return buffer;
            }
            set { buffer = value; }
        }

        /// <summary>
        /// Convenience function. I recommend using .buffer instead.
        /// </summary>

        public virtual T this[int i]
        {
            get { return buffer[i]; }
            set { buffer[i] = value; }
        }

        #endregion

        #region Constructors

        public ArrayList()
        {
            buffer = new List<T>();
        }

        public ArrayList(IList<T> p_list)
        {
            buffer = new List<T>(CloneArray(p_list != null ? p_list : null));
        }

        public ArrayList(ArrayList<T> p_aotList)
        {
            buffer = new List<T>(CloneArray(p_aotList != null ? p_aotList.ToArray() : null));
        }

        #endregion

        #region List Functions

        /// <summary>
        /// For 'foreach' functionality.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (buffer != null)
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return buffer[i];
                }
            }
        }

        public void Clear() { buffer = null;}

        /// <summary>
        /// Add the specified item to the end of the list.
        /// </summary>
        public void Add(T item)
        {
            AddWithoutCallEvents(item);
            OnAdd(item);
        }

        protected void AddWithoutCallEvents(T item)
        {
            Buffer.Add(item);
        }

        /// <summary>
        /// Insert an item at the specified index, pushing the entries back.
        /// </summary>

        public void Insert(int index, T item)
        {
            
            index = Mathf.Max(0, index);
            if (index < Count)
            {
                Buffer.Insert(index, item);
                OnAdd(item);
            }
            else Add(item);
        }

        /// <summary>
        /// Returns 'true' if the specified item is within the list.
        /// </summary>

        public bool Contains(T item)
        {
            if (buffer == null) return false;
            for (int i = 0; i < Count; ++i)
            {
                if (
                    (EqualityExtension.IsNull(buffer[i]) && EqualityExtension.IsNull(item)) ||
                    (!EqualityExtension.IsNull(buffer[i]) && buffer[i].Equals(item))
                  )
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
        /// </summary>

        public bool Remove(T item)
        {
            bool v_sucess = RemoveWithoutCallEvents(item);
            if (v_sucess)
                OnRemove(item);
            return v_sucess;
        }

        protected bool RemoveWithoutCallEvents(T item)
        {
            bool v_sucess = false;
            if (Buffer != null && Buffer.Contains(item))
            {
                return Buffer.Remove(item);
            }
            return v_sucess;
        }

        /// <summary>
        /// Remove an item at the specified index.
        /// </summary>

        public void RemoveAt(int index)
        {
            if (Buffer != null && index < Count)
            {
                Buffer.RemoveAt(index);
            }
        }

        public T[] ToArray()
        {
            return Buffer.ToArray();
        }

        /// <summary>
        /// List.Sort equivalent.
        /// </summary>

        public void Sort(System.Comparison<T> comparer)
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = 1; i < Count; ++i)
                {
                    if (comparer.Invoke(Buffer[i - 1], Buffer[i]) > 0)
                    {
                        T temp = Buffer[i];
                        Buffer[i] = Buffer[i - 1];
                        Buffer[i - 1] = temp;
                        changed = true;
                    }
                }
            }
        }

        #endregion

        #region New List Functions

        public T GetFirst()
        {
            T v_first = default(T);
            if (Count > 0)
                v_first = this[0];
            return v_first;
        }

        public T GetLast()
        {
            T v_first = default(T);
            if (Count > 0)
                v_first = this[Count - 1];
            return v_first;
        }

        public ArrayList<string> GetStringList()
        {
            ArrayList<string> v_stringList = new ArrayList<string>();
            for (int i = 0; i < Count; i++)
            {
                object v_object = this[i];
                string v_toString = "null";
                try
                {
                    v_toString = v_object.ToString();
                }
                catch
                {
                    v_toString = "null";
                }
                v_stringList.Add(v_toString);
            }
            return v_stringList;
        }

        public ArrayList<T> CloneList()
        {
            ArrayList<T> v_clonedList = new ArrayList<T>();
            foreach (T v_object in this)
            {
                v_clonedList.Add(v_object);
            }
            return v_clonedList;
        }

        public bool ContainsNull()
        {
            for (int i = 0; i < this.Count; i++)
            {
                object v_object = this[i];
                if (EqualityExtension.IsNull(v_object))
                    return true;
            }
            return false;
        }

        public void RemoveNulls()
        {
            ArrayList<T> v_newList = new ArrayList<T>();

            for (int i = 0; i < this.Count; i++)
            {
                T v_object = this[i];
                if (!EqualityExtension.IsNull(v_object))
                    v_newList.Add(v_object);
            }
            this.Clear();
            foreach (T v_object in v_newList)
                this.AddWithoutCallEvents(v_object);
        }

        public bool RemoveChecking(T p_object, bool p_removeNulls = true)
        {
            bool v_sucess = false;
            if (!EqualityExtension.IsNull(p_object))
            {
                if (p_removeNulls)
                    RemoveNulls();
                ArrayList<T> v_newList = new ArrayList<T>();
                for (int i = 0; i < this.Count; i++)
                {
                    try
                    {
                        T v_object = this[i];
                        if (!EqualityExtension.Equals(p_object, v_object))
                            v_newList.Add(v_object);
                        else
                            v_sucess = true;
                    }
                    catch
                    {
                        UnityEngine.Debug.Log("An error occurred when trying to RemoveChecking");
                        v_sucess = true;
                    }
                }
                this.Clear();
                foreach (T v_object in v_newList)
                    this.AddWithoutCallEvents(v_object);
            }
            if (v_sucess)
                OnRemove(p_object);
            return v_sucess;
        }

        public bool AddChecking(T p_object)
        {
            bool v_sucess = false;
            try
            {
                if (!EqualityExtension.IsNull(p_object)
                   && !this.Contains(p_object))
                {
                    this.Add(p_object);
                    v_sucess = true;
                }
            }
            catch
            {
                UnityEngine.Debug.Log("An error occurred when trying to AddChecking");
            }
            return v_sucess;
        }

        public void MergeList(ArrayList<T> p_otherList)
        {
            if (p_otherList != null)
            {
                foreach (T v_object in p_otherList)
                {
                    this.AddChecking(v_object);
                }
            }
        }

        public void MergeList(T[] p_array)
        {
            MergeList(new ArrayList<T>(p_array));
        }

        public void MergeList(List<T> p_otherList)
        {
            MergeList(new ArrayList<T>(p_otherList));
        }

        public void UnmergeList(ArrayList<T> p_otherList)
        {
            if (p_otherList != null)
            {
                ArrayList<T> v_dummyList = new ArrayList<T>();
                this.RemoveNulls();
                for (int i = 0; i < this.Count; i++)
                {
                    T v_object = this[i];
                    if (!p_otherList.Contains(v_object))
                        v_dummyList.Add(v_object);
                    else
                        OnRemove(v_object);
                }
                this.Clear();
                for (int i = 0; i < v_dummyList.Count; i++)
                {
                    try
                    {
                        T v_object = v_dummyList[i];
                        this.AddWithoutCallEvents(v_object);
                    }
                    catch
                    {
                        UnityEngine.Debug.Log("An error occurred when trying to UnmergeList");
                    }
                }
            }
        }

        public void UnmergeList(List<T> p_otherList)
        {
            UnmergeList(new ArrayList<T>(p_otherList));
        }

        public void UnmergeList(T[] p_array)
        {
            UnmergeList(new ArrayList<T>(p_array));
        }

        public void Shuffle()
        {
            System.Random rng = new System.Random();
            int n = this.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = this[k];
                this[k] = this[n];
                this[n] = value;
            }
        }

        public int ObjectIndex(T p_object)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (EqualityExtension.Equals(this[i], p_object))
                    return i;
            }
            return -1;
        }

        public static System.Type GetContainerFilterType()
        {
            return typeof(T);
        }

        #endregion

        #region IArrayList Implementations

        IList IArrayList.Buffer
        {
            get
            {
                return Buffer;
            }
        }

        object IArrayList.this[int i]
        {
            get { return buffer[i]; }
            set
            {
                try
                {
                    if (value is T)
                        buffer[i] = (T)value;
                }
                catch { }
            }
        }

        bool IArrayList.Remove(object p_object)
        {
            try
            {
                if (p_object is T)
                    return Remove((T)p_object);
            }
            catch { }
            return false;
        }

        bool IArrayList.Add(object p_object)
        {
            try
            {
                if (p_object is T)
                {
                    Add((T)p_object);
                    return true;
                }
            }
            catch { }
            return false;
        }

        bool IArrayList.Contains(object item)
        {
            return Contains((T)item);
        }

        #endregion

        #region Internal Events

        protected virtual void OnAdd(T p_item)
        {
        }

        protected virtual void OnRemove(T p_item)
        {
        }

        #endregion

        #region Static Functions

        private static T[] CloneArray(IList<T> p_buffer)
        {
            T[] v_newBuffer = (p_buffer != null) ? new T[p_buffer.Count] : new T[0];
            if (p_buffer != null && p_buffer.Count > 0) p_buffer.CopyTo(v_newBuffer, 0);
            return v_newBuffer;
        }

        #endregion
    }
}