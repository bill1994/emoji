using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Extensions;
using Kyub.Reflection;
using UnityEngine.SceneManagement;

namespace Kyub
{
    public abstract class Singleton<T> : BaseSingleton where T : BaseSingleton
    {
        #region Static Functions

        protected static HashSet<System.Type> s_searchInstanceBlocked = new HashSet<System.Type>(); //Used to prevent multiple FindObjectOfType when using InstanceExists ans s_instance is null in scene
        protected static bool s_shuttingDown = false;
        protected static object s_lock = new object();
        protected static T s_instance;

        public static T Instance
        {
            get
            {
                if (s_shuttingDown)
                {
                    if (s_instance == null)
                        Debug.Log("Shutting Down");
                    //Debug.LogWarning("[Singleton] Instance '" + typeof(TweenManager) + "' already destroyed. Returning null.");
                    return null;
                }

                lock (s_lock)
                {
                    if (s_instance == null && EnableSingletonCreation)
                    {
                        System.Type v_type = typeof(T);
                        s_instance = GameObject.FindObjectOfType(v_type) as T;

                        try
                        {
                            if (s_instance == null && v_type != null && !typeof(T).IsAbstract())
                            {
                                string v_name = "(singleton) " + typeof(T).ToString();
                                T v_object = new GameObject(v_name).AddComponent<T>();
                                if (!Application.isPlaying)
                                    v_object.gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                                s_instance = v_object;
                                DestroyUtils.Destroy(v_object.gameObject, true);
                            }
                        }
                        catch { }
                    }

                    if (s_instance == null)
                        Debug.Log(string.Format("(singleton) Failed to create instance from type '{0}'", typeof(T)));

                    return s_instance;
                }
            }
            set
            {
                if (s_instance == value)
                    return;
                s_instance = value;
            }
        }

        public static bool InstanceExists()
        {
            return GetInstanceFastSearch() != null;
        }

        public static T GetInstanceFastSearch()
        {
            if (s_shuttingDown)
                return null;

            if (!s_searchInstanceBlocked.Contains(typeof(T)))
            {
                s_searchInstanceBlocked.Add(typeof(T));
                var v_instance = GetInstance(false);
                return v_instance;
            }
            else
                return s_instance;
        }

        public static T GetInstance(bool p_canCreateANewOne = false)
        {
            if (s_shuttingDown)
                return null;

            T v_instance = null;
            if (p_canCreateANewOne && IsMainThread())
                v_instance = Instance;
            else
            {
                if (s_instance == null)
                    s_instance = GameObject.FindObjectOfType(typeof(T)) as T;
                v_instance = s_instance;
            }
            return v_instance;
        }

        public static bool IsMainThread()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1 ||
                System.Threading.Thread.CurrentThread.IsBackground || System.Threading.Thread.CurrentThread.IsThreadPoolThread)
            {
                // not the main thread
                return false;
            }
            return true;
        }

        #endregion

        # region Internal Properties

        protected bool _keepNewInstanceIfDuplicated = false;
        protected virtual bool KeepNewInstanceIfDuplicated
        {
            get

            {
                return _keepNewInstanceIfDuplicated;
            }
            set

            {
                if (_keepNewInstanceIfDuplicated == value)
                    return;
                _keepNewInstanceIfDuplicated = value;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            CheckInstance();
            RegisterSceneEvents();
        }

        protected virtual void OnDestroy()
        {
            if (s_instance == null || s_instance == this)
                s_searchInstanceBlocked.Remove(typeof(T));

            UnregisterSceneEvents();
            //CheckShuttingDown();
        }

        protected virtual void OnApplicationQuit()
        {
            CheckShuttingDown();
        }

        protected virtual void OnSceneWasLoaded(Scene p_scene, LoadSceneMode p_mode)
        {
            if (s_instance == this)
            {
                //We can search instance again
                s_searchInstanceBlocked.Remove(typeof(T));
            }
        }

        #endregion

        #region Helper Functions

        protected void RegisterSceneEvents()
        {
            UnregisterSceneEvents();

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneWasLoaded;
        }

        protected void UnregisterSceneEvents()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneWasLoaded;
        }

        protected virtual void CheckShuttingDown()
        {
            if (s_instance == null || s_instance == this)
                s_shuttingDown = true;
        }

        protected virtual void CheckInstance()
        {
            if (KeepNewInstanceIfDuplicated)
            {
                if (s_instance != this && s_instance != null)
                    Kyub.DestroyUtils.Destroy(s_instance.gameObject);
                s_instance = this as T;
            }
            else
            {

                if (s_instance != this && s_instance != null && !s_instance.IsMarkedToDestroy(true))
                    DestroyUtils.Destroy(this.gameObject);
                else if (s_instance == null || (s_instance.IsMarkedToDestroy(true) && !this.IsMarkedToDestroy(true)))
                {
                    s_instance = this as T;
                }
            }
            if (s_instance == null || s_instance == this)
                s_shuttingDown = false;
        }

        #endregion
    }
}
