using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;

namespace Kyub.Extensions
{
    public static class ObjectExtensions
    {
        #region Transform

        /// <summary>
        /// Transforms rotation from local space to world space.
        /// </summary>
        /// <param name="p_localRotation"></param>
        /// <returns></returns>
        public static Quaternion TransformRotation(this Transform p_transform, Quaternion p_localRotation)
        {
            Quaternion v_worldRotation = p_transform.rotation * p_localRotation;
            return v_worldRotation;
        }

        /// <summary>
        /// Transforms rotation from world space to local space.
        /// </summary>
        /// <param name="p_worldRotation"></param>
        /// <returns></returns>
        public static Quaternion InverseTransformRotation(this Transform p_transform, Quaternion p_worldRotation)
        {
            Quaternion v_localRotation = Quaternion.Inverse(p_transform.rotation) * p_worldRotation;
            return v_localRotation;
        }

        #endregion

        #region Component Funcions

        public static T[] GetNonMarkedComponents<T>(this Component p_component, bool p_checkGameobject = false) where T : Component
        {
            return p_component.gameObject.GetNonMarkedComponents<T>(p_checkGameobject);
        }

        public static T[] GetNonMarkedComponentsInChildren<T>(this Component p_component, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            return p_component.gameObject.GetNonMarkedComponentsInChildren<T>(p_includeInactive, p_includeSelf, p_checkGameobject);
        }

        public static T[] GetNonMarkedComponentsInParent<T>(this Component p_component, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            return p_component.gameObject.GetNonMarkedComponentsInParent<T>(p_includeInactive, p_includeSelf, p_checkGameobject);
        }

        public static T GetNonMarkedComponent<T>(this Component p_component, bool p_checkGameobject = false) where T : Component
        {
            return p_component.gameObject.GetNonMarkedComponent<T>(p_checkGameobject);
        }

        public static T GetNonMarkedComponentInChildren<T>(this Component p_component, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            return p_component.gameObject.GetNonMarkedComponentInChildren<T>(p_includeInactive, p_includeSelf, p_checkGameobject);
        }

        public static T GetNonMarkedComponentInParent<T>(this Component p_component, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            return p_component.gameObject.GetNonMarkedComponentInParent<T>(p_includeInactive, p_includeSelf, p_checkGameobject);
        }

        public static bool IsMarkedToDestroy(this Component p_component, bool p_checkGameObjectToo = false)
        {
            bool v_sucess = MarkedToDestroy.IsMarked(p_component);
            if (!v_sucess && p_checkGameObjectToo)
            {
                try
                {
                    v_sucess = MarkedToDestroy.IsMarked(p_component.gameObject);
                }
                catch { }
            }
            return v_sucess;
        }

        public static T GetComponentInParentExcludingSelf<T>(this Component p_component, bool p_includeInactive = false) where T : Component
        {
            return p_component.gameObject.GetComponentInParentExcludingSelf<T>(p_includeInactive);
        }

        public static T GetComponentInChildrenExcludingSelf<T>(this Component p_component, bool p_includeInactive = false) where T : Component
        {
            return p_component.gameObject.GetComponentInChildrenExcludingSelf<T>(p_includeInactive);
        }

        public static T[] GetComponentsInParentExcludingSelf<T>(this Component p_component, bool p_includeInactive = false) where T : Component
        {
            return p_component.gameObject.GetComponentsInParentExcludingSelf<T>(p_includeInactive);
        }

        public static T[] GetComponentsInChildrenExcludingSelf<T>(this Component p_component, bool p_includeInactive = false) where T : Component
        {
            return p_component.gameObject.GetComponentsInChildrenExcludingSelf<T>(p_includeInactive);
        }

        #endregion

        #region Game Object Functions

        public static T[] GetNonMarkedComponents<T>(this GameObject p_object, bool p_checkGameobject = false) where T : Component
        {
            T[] v_components = p_object.GetComponents<T>();
            List<T> v_return = new List<T>();
            foreach (T v_component in v_components)
            {
                if (!IsMarkedToDestroy(v_component, p_checkGameobject))
                {
                    v_return.Add(v_component);
                }
            }
            return v_return.ToArray();
        }

        public static T[] GetNonMarkedComponentsInChildren<T>(this GameObject p_object, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            ArrayList<T> v_components = new ArrayList<T>(p_object.GetComponentsInChildren<T>(p_includeInactive));
            ArrayList<T> v_selfComponents = new ArrayList<T>(p_object.GetComponents<T>());
            if (p_includeSelf)
                v_components.MergeList(v_selfComponents);
            else
                v_components.UnmergeList(v_selfComponents);
            ArrayList<T> v_return = new ArrayList<T>();
            foreach (T v_component in v_components)
            {
                if (!IsMarkedToDestroy(v_component, p_checkGameobject))
                {
                    v_return.Add(v_component);
                }
            }
            return v_return.ToArray();
        }

        public static T[] GetNonMarkedComponentsInParent<T>(this GameObject p_object, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            ArrayList<T> v_components = new ArrayList<T>(p_object.GetComponentsInParent<T>(p_includeInactive));
            ArrayList<T> v_selfComponents = new ArrayList<T>(p_object.GetComponents<T>());
            if (p_includeSelf)
                v_components.MergeList(v_selfComponents);
            else
                v_components.UnmergeList(v_selfComponents);
            ArrayList<T> v_return = new ArrayList<T>();
            foreach (T v_component in v_components)
            {
                if (!IsMarkedToDestroy(v_component, p_checkGameobject))
                {
                    v_return.Add(v_component);
                }
            }
            return v_return.ToArray();
        }

        public static T GetNonMarkedComponent<T>(this GameObject p_object, bool p_checkGameobject = false) where T : Component
        {
            List<T> v_selfComponents = new List<T>(p_object.GetComponents<T>());
            T v_return = null;
            foreach (T v_component in v_selfComponents)
            {
                if (!IsMarkedToDestroy(v_component, p_checkGameobject))
                {
                    v_return = v_component;
                    break;
                }
            }
            return v_return;
        }

        public static T GetNonMarkedComponentInChildren<T>(this GameObject p_object, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            ArrayList<T> v_components = new ArrayList<T>(p_object.GetComponentsInChildren<T>(p_includeInactive));
            ArrayList<T> v_selfComponents = new ArrayList<T>(p_object.GetComponents<T>());
            if (p_includeSelf)
                v_components.MergeList(v_selfComponents);
            else
                v_components.UnmergeList(v_selfComponents);
            T v_return = null;
            foreach (T v_component in v_components)
            {
                if (!IsMarkedToDestroy(v_component, p_checkGameobject))
                {
                    v_return = v_component;
                    break;
                }
            }
            return v_return;
        }

        public static T GetNonMarkedComponentInParent<T>(this GameObject p_object, bool p_includeInactive = false, bool p_includeSelf = true, bool p_checkGameobject = false) where T : Component
        {
            ArrayList<T> v_components = new ArrayList<T>(p_object.GetComponentsInParent<T>(p_includeInactive));
            ArrayList<T> v_selfComponents = new ArrayList<T>(p_object.GetComponents<T>());
            if (p_includeSelf)
                v_components.MergeList(v_selfComponents);
            else
                v_components.UnmergeList(v_selfComponents);
            T v_return = null;
            foreach (T v_component in v_components)
            {
                if (!IsMarkedToDestroy(v_component, p_checkGameobject))
                {
                    v_return = v_component;
                    break;
                }
            }
            return v_return;
        }

        public static bool IsMarkedToDestroy(this GameObject p_object)
        {
            bool v_sucess = MarkedToDestroy.IsMarked(p_object);
            return v_sucess;
        }

        public static T GetComponentInParentExcludingSelf<T>(this GameObject p_object, bool p_includeInactive = false) where T : Component
        {
            List<T> v_components = new List<T>(p_object.GetComponentsInParent<T>(p_includeInactive));
            List<T> v_selfComponents = new List<T>(p_object.GetComponents<T>());
            v_components.UnmergeList(v_selfComponents);
            T v_return = null;
            foreach (T v_component in v_components)
            {
                v_return = v_component;
                break;
            }
            return v_return;
        }

        public static T GetComponentInChildrenExcludingSelf<T>(this GameObject p_object, bool p_includeInactive = false) where T : Component
        {
            List<T> v_components = new List<T>(p_object.GetComponentsInChildren<T>(p_includeInactive));
            List<T> v_selfComponents = new List<T>(p_object.GetComponents<T>());
            v_components.UnmergeList(v_selfComponents);
            T v_return = null;
            foreach (T v_component in v_components)
            {
                v_return = v_component;
                break;
            }
            return v_return;
        }

        public static T[] GetComponentsInParentExcludingSelf<T>(this GameObject p_object, bool p_includeInactive = false) where T : Component
        {
            List<T> v_components = new List<T>(p_object.GetComponentsInParent<T>(p_includeInactive));
            List<T> v_selfComponents = new List<T>(p_object.GetComponents<T>());
            v_components.UnmergeList(v_selfComponents);
            List<T> v_return = new List<T>();
            foreach (T v_component in v_components)
            {
                v_return.Add(v_component);
            }
            return v_return.ToArray();
        }

        public static T[] GetComponentsInChildrenExcludingSelf<T>(this GameObject p_object, bool p_includeInactive = false) where T : Component
        {
            List<T> v_components = new List<T>(p_object.GetComponentsInChildren<T>(p_includeInactive));
            List<T> v_selfComponents = new List<T>(p_object.GetComponents<T>());
            v_components.UnmergeList(v_selfComponents);
            List<T> v_return = new List<T>();
            foreach (T v_component in v_components)
            {
                v_return.Add(v_component);
            }
            return v_return.ToArray();
        }

        #endregion
    }
}
