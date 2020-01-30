//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace MaterialUI
{
    /// <summary>
    /// Static class to help with the instantiation of MaterialUI objects.
    /// </summary>
    public class PrefabManager : Kyub.Singleton<PrefabManager>
    {
        //[SerializeField]
        //private bool m_autoCacheLoadedPrefabs = true;
        [SerializeField]
        private ResourcePrefabsDatabase m_ResourcePrefabs = null;

        //private Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

        public static ResourcePrefabsDatabase ResourcePrefabs
        {
            get
            {
                if (Instance.m_ResourcePrefabs == null)
                {
                    Instance.m_ResourcePrefabs = ScriptableObject.CreateInstance<ResourcePrefabsDatabase>();
                    Instance.m_ResourcePrefabs.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                }

                return Instance.m_ResourcePrefabs;
            }
        }

        public static void ClearCache()
        {
            ResourcePrefabs.ClearCache();
        }

        /// <summary>
        /// Finds the GameObject with the matching name in the object pool, or if not pooled, from the path.
        /// </summary>
        /// <param name="nameWithPath">The name of the prefab, including the path.</param>
        /// <returns>The uninstantiated GameObject that matches the path, if found. If no GameObject is found, returns null.</returns>
        public static GameObject GetGameObject(string nameWithPath)
        {
            GameObject gameObject = null;
            if (!string.IsNullOrEmpty(nameWithPath))
            {
                //Instance._cache.TryGetValue(nameWithPath, out gameObject);

                if (gameObject == null)
                {
                    var addressAsset = ResourcePrefabs.GetPrefabAddressWithName(nameWithPath);
                    if (addressAsset != null && !addressAsset.IsEmpty())
                        gameObject = addressAsset.LoadAsset();
                    else
                        gameObject = Resources.Load<GameObject>(nameWithPath);

                    //if (gameObject != null && Instance.m_autoCacheLoadedPrefabs)
                    //    Instance._cache[nameWithPath] = gameObject;
                }
            }

            return gameObject;
        }

        /// <summary>
        /// Finds the GameObject with the matching name in the object pool, or if not pooled, from the path.
        /// </summary>
        /// <param name="nameWithPath">The name of the prefab, including the path.</param>
        /// <returns>The uninstantiated GameObject that matches the path, if found. If no GameObject is found, returns null.</returns>
        public static void GetGameObjectAsync(string nameWithPath, System.Action<string, GameObject> callback)
        {
            GameObject gameObject = null;
            if (!string.IsNullOrEmpty(nameWithPath))
            {
                //Instance._cache.TryGetValue(nameWithPath, out gameObject);

                if (gameObject == null)
                {
                    System.Action<string, GameObject> internalCallback = (path, asset) =>
                    {
                        //if (gameObject != null && Instance.m_autoCacheLoadedPrefabs)
                        //    Instance._cache[nameWithPath] = gameObject;

                        if (callback != null)
                            callback(path, asset);
                    };

                    var addressAsset = ResourcePrefabs.GetPrefabAddressWithName(nameWithPath);
                    if (addressAsset != null && !addressAsset.IsEmpty())
                        ResourcesLoader.LoadAsync(addressAsset, internalCallback);
                    else
                        ResourcesLoader.LoadAsync<GameObject>(nameWithPath, internalCallback);
                }
            }
            else
                callback(nameWithPath, null);
        }

        /// <summary>
        /// Finds the GameObject with the matching name in the object pool, or if not pooled, from the path, then instantiates it.
        /// </summary>
        /// <param name="nameWithPath">The name of the prefab, including the path.</param>
        /// <param name="parent">The transform to set the parent of the instantiated GameObject.</param>
        /// <returns>The instantiated GameObject that matches the path, if found. If no GameObject is found, returns null.</returns>
        public static GameObject InstantiateGameObject(string nameWithPath, Transform parent)
        {
            GameObject go = GetGameObject(nameWithPath);

            if (go == null)
            {
                return null;
            }

            go = GameObject.Instantiate(go);

            if (parent == null)
            {
                return go;
            }

            go.transform.SetParent(parent);
            go.transform.localScale = Vector3.one;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localPosition = Vector3.zero;

            return go;
        }

        public static GameObject InstantiateGameObject(PrefabAddress addressAsset, Transform parent)
        {
            return InstantiateGameObject(addressAsset != null ? addressAsset.Name : "", parent);
        }

        /// <summary>
        /// Finds the GameObject with the matching name in the object pool, or if not pooled, from the path, then instantiates it.
        /// </summary>
        /// <param name="nameWithPath">The name of the prefab, including the path.</param>
        /// <param name="parent">The transform to set the parent of the instantiated GameObject.</param>
        /// <returns>The instantiated GameObject that matches the path, if found. If no GameObject is found, returns null.</returns>
        public static void InstantiateGameObjectAsync(string nameWithPath, Transform parent, System.Action<string, GameObject> callback)
        {
            System.Action<string, GameObject> internalCallback = (path, asset) =>
            {
                var go = asset != null ? GameObject.Instantiate(asset, parent) : null;
                if (go != null && parent != null)
                {
                    go.transform.localScale = Vector3.one;
                    go.transform.localEulerAngles = Vector3.zero;
                    go.transform.localPosition = Vector3.zero;
                }
                if (callback != null)
                    callback(path, go);
            };
            GetGameObjectAsync(nameWithPath, internalCallback);
        }

        public static void InstantiateGameObjectAsync(PrefabAddress addressAsset, Transform parent, System.Action<string, GameObject> callback)
        {
            InstantiateGameObjectAsync(addressAsset != null ? addressAsset.Name : "", parent, callback);
        }

        class ResourcesLoader : MonoBehaviour
        {
            public void Awake()
            {
                DontDestroyOnLoad(this);
            }

            public static void LoadAsync<T>(string path, System.Action<string, T> callback) where T : UnityEngine.Object
            {
                var loader = new GameObject().AddComponent<ResourcesLoader>();
                loader.StartCoroutine(loader.LoadAsyncRoutine<T>(path, callback));
            }

            public static void LoadAsync<T>(GenericAssetAddress<T> assetAddress, System.Action<string, T> callback) where T : UnityEngine.Object
            {
                var loader = new GameObject().AddComponent<ResourcesLoader>();
                loader.StartCoroutine(loader.LoadAsyncRoutine<T>(assetAddress, callback));
            }

            IEnumerator LoadAsyncRoutine<T>(GenericAssetAddress<T> assetAddress, System.Action<string, T> callback) where T : UnityEngine.Object
            {
                var v_request = assetAddress.LoadAssetAsync();
                while (!v_request.isDone)
                {
                    yield return null;
                }
                if (callback != null)
                    callback(assetAddress.Name, v_request.asset as T);

                if (Application.isPlaying)
                    GameObject.Destroy(gameObject);
                else
                    GameObject.DestroyImmediate(gameObject);
            }

            IEnumerator LoadAsyncRoutine<T>(string path, System.Action<string, T> callback) where T : UnityEngine.Object
            {
                var v_request = Resources.LoadAsync<T>(path);
                while (!v_request.isDone)
                {
                    yield return null;
                }
                if (callback != null)
                    callback(path, v_request.asset as T);

                if (Application.isPlaying)
                    GameObject.Destroy(gameObject);
                else
                    GameObject.DestroyImmediate(gameObject);
            }
        }
    }
}