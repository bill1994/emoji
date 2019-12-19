using System.Collections;
using System.Collections.Generic;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Extensions
{
    public static class ListExtensions
    {
        public static System.Type GetElementType(this IList p_list)
        {
            System.Type v_returnedType = null;
            System.Type v_arrayType = p_list != null ? p_list.GetType() : null;
            if (v_arrayType != null)
            {
                if (v_arrayType.IsArray)
                {
                    v_returnedType = v_arrayType.GetElementType();
                }
                else
                {

                    System.Type[] v_interfaceTypes = v_arrayType.GetInterfaces();
                    foreach (System.Type v_interfaceType in v_interfaceTypes)
                    {
                        string v_interfaceSafeName = v_interfaceType.FullName;
                        if (v_interfaceSafeName.Contains("IList`1") ||
                            v_interfaceSafeName.Contains("ICollection`1") ||
                            v_interfaceSafeName.Contains("IEnumerable`1"))

                        {
                            try
                            {
                                v_returnedType = v_interfaceType.GetGenericArguments()[0];
                                break;
                            }
                            catch { }
                        }
                    }
                }
            }
            return v_returnedType;
        }

        public static T GetFirst<T>(this IList<T> p_list)
        {
            T v_first = default(T);
            if (p_list != null && p_list.Count > 0)
                v_first = p_list[0];
            return v_first;
        }

        public static T GetLast<T>(this IList<T> p_list)
        {
            T v_first = default(T);
            if (p_list != null && p_list.Count > 0)
                v_first = p_list[p_list.Count - 1];
            return v_first;
        }

        public static List<string> GetStringList<T>(this IList<T> p_list)
        {
            List<string> v_stringList = new List<string>();
            if (p_list != null)
            {
                for (int i = 0; i < p_list.Count; i++)
                {
                    object v_object = p_list[i];
                    string v_toString = "NULL";
                    try
                    {
                        v_toString = v_object.ToString();
                    }
                    catch
                    {
                        v_toString = "NULL";
                    }
                    v_stringList.Add(v_toString);
                }
            }
            return v_stringList;
        }

        public static List<T> CloneList<T>(this IList<T> p_list)
        {
            List<T> v_clonedList = new List<T>();
            if (p_list != null)
            {
                foreach (T v_object in p_list)
                {
                    v_clonedList.Add(v_object);
                }
            }
            return v_clonedList;
        }

        public static bool ContainsNull<T>(this IList<T> p_list) where T : class
        {
            if (p_list != null)
            {
                for (int i = 0; i < p_list.Count; i++)
                {
                    object v_object = p_list[i];
                    if (v_object == null)
                        return true;
                }
            }
            return false;
        }

        public static void RemoveNulls<T>(this IList<T> p_list) where T : class
        {
            if (p_list != null)
            {
                List<T> v_newList = new List<T>();

                for (int i = 0; i < p_list.Count; i++)
                {
                    T v_object = p_list[i];
                    if (!EqualityExtension.IsNull(v_object))
                        v_newList.Add(v_object);
                }
                p_list.Clear();
                foreach (T v_object in v_newList)
                    p_list.Add(v_object);
            }
        }

        public static bool RemoveChecking<T>(this IList<T> p_list, T p_object, bool p_removeNulls = true)
        {
            bool v_sucess = false;
            if (p_list != null && !EqualityExtension.IsNull(p_object))
            {
                List<T> v_newList = new List<T>();
                for (int i = 0; i < p_list.Count; i++)
                {
                    try
                    {
                        T v_object = p_list[i];
                        if (!p_removeNulls || !EqualityExtension.IsNull(v_object))
                        {
                            if (!EqualityExtension.Equals(p_object, v_object))
                                v_newList.Add(v_object);
                            else
                                v_sucess = true;
                        }
                    }
                    catch
                    {
                        UnityEngine.Debug.Log("An error occurred when trying to RemoveChecking");
                        v_sucess = true;
                    }
                }
                p_list.Clear();
                foreach (T v_object in v_newList)
                    p_list.Add(v_object);
            }
            return v_sucess;
        }

        public static bool AddChecking<T>(this IList<T> p_list, T p_object)
        {
            bool v_sucess = false;
            try
            {
                if (p_list != null && !EqualityExtension.IsNull(p_object)
                   && !p_list.Contains(p_object))
                {
                    p_list.Add(p_object);
                    v_sucess = true;
                }
            }
            catch
            {
                UnityEngine.Debug.Log("An error occurred when trying to AddChecking");
            }
            return v_sucess;
        }

        public static void MergeList<T>(this IList<T> p_list, IList<T> p_otherList)
        {
            if (p_otherList != null)
            {
                foreach (T v_object in p_otherList)
                {
                    p_list.AddChecking(v_object);
                }
            }
        }

        public static void UnmergeList<T>(this IList<T> p_list, IList<T> p_otherList)
        {
            if (p_otherList != null)
            {
                List<T> v_dummyList = new List<T>();
                for (int i = 0; i < p_list.Count; i++)
                //foreach(T v_object in p_otherList)
                {
                    T v_object = p_list[i];
                    if (!p_otherList.Contains(v_object))
                        v_dummyList.Add(v_object);
                }
                p_list.Clear();
                for (int i = 0; i < v_dummyList.Count; i++)
                {
                    try
                    {
                        T v_object = v_dummyList[i];
                        if (!EqualityExtension.IsNull(v_object))
                        {
                            p_list.Add(v_object);
                        }
                    }
                    catch
                    {
                        UnityEngine.Debug.Log("An error occurred when trying to UnmergeList");
                    }
                }
            }
        }

        public static void Shuffle<T>(this IList<T> p_list)
        {
            int n = p_list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = p_list[k];
                p_list[k] = p_list[n];
                p_list[n] = value;
            }
        }

        public static T RandomElement<T>(this IList<T> p_list)
        {
            if (p_list != null)
            {
                int v_index = UnityEngine.Random.Range(0, p_list.Count);
                if (p_list.Count > v_index)
                    return p_list[v_index];
            }
            return default(T);
        }

        public static int IndexOf<T>(this System.Array p_array, T p_object)
        {
            return p_array != null? System.Array.IndexOf(p_array, p_object) : -1;
        }

        public static void ClampToCount<T>(this IList<T> p_list, int p_maxAmountOfElements)
        {
            int v_maxAmount = UnityEngine.Mathf.Max(0, p_maxAmountOfElements);
            while (p_list != null && p_list.Count > v_maxAmount && p_list.Count > 0)
            {
                p_list.RemoveAt(p_list.Count - 1);
            }
        }

        public static void StableSort<T>(this T[] p_array, System.Comparison<T> p_comparison)
        {
            if (p_array != null)
            {
                var v_keys = new KeyValuePair<int, T>[p_array.Length];
                for (var i = 0; i < p_array.Length; i++)
                    v_keys[i] = new KeyValuePair<int, T>(i, p_array[i]);
                System.Array.Sort(v_keys, p_array, new StabilizingComparer<T>(p_comparison));
            }
        }

        public static void StableSort<T>(this List<T> p_list, System.Comparison<T> p_comparison)
        {
            if (p_list != null)
            {
                var v_array = p_list.ToArray();
                v_array.StableSort<T>(p_comparison);
                p_list.Clear();
                p_list.AddRange(v_array);
            }
        }

        private sealed class StabilizingComparer<T> : IComparer<KeyValuePair<int, T>>
        {
            private readonly System.Comparison<T> _comparison;

            public StabilizingComparer(System.Comparison<T> comparison)
            {
                _comparison = comparison;
            }

            public int Compare(KeyValuePair<int, T> x,
                               KeyValuePair<int, T> y)
            {
                var result = _comparison(x.Value, y.Value);
                return result != 0 ? result : x.Key.CompareTo(y.Key);
            }
        }
    }
}
