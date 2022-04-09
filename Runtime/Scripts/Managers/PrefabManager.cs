// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

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
                    //Try load default resource
                    Instance.m_ResourcePrefabs = Resources.Load<ResourcePrefabsDatabase>("Theme/ResourcePrefabsDatabase");

                    //Create a new one
                    if (Instance.m_ResourcePrefabs == null)
                    {
                        Instance.m_ResourcePrefabs = ScriptableObject.CreateInstance<ResourcePrefabsDatabase>();
                        Instance.m_ResourcePrefabs.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                    }
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
                        InternalResourcesLoader.LoadAsync(addressAsset, internalCallback);
                    else
                        InternalResourcesLoader.LoadAsync<GameObject>(nameWithPath, internalCallback);
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
        public static GameObject InstantiateGameObject(string nameWithPath, Transform parent, bool? active = null)
        {
            GameObject asset = GetGameObject(nameWithPath);

            var instance = InstantiateInternal(asset, parent, active);
            return instance;
        }

        public static GameObject InstantiateGameObject(PrefabAddress addressAsset, Transform parent, bool? active = null)
        {
            return InstantiateGameObject(addressAsset != null ? addressAsset.Name : "", parent, active);
        }

        /// <summary>
        /// Finds the GameObject with the matching name in the object pool, or if not pooled, from the path, then instantiates it.
        /// </summary>
        /// <param name="nameWithPath">The name of the prefab, including the path.</param>
        /// <param name="parent">The transform to set the parent of the instantiated GameObject.</param>
        /// <returns>The instantiated GameObject that matches the path, if found. If no GameObject is found, returns null.</returns>
        public static void InstantiateGameObjectAsync(string nameWithPath, Transform parent, System.Action<string, GameObject> callback, bool? active = null)
        {
            System.Action<string, GameObject> internalCallback = (path, asset) =>
            {
                var instance = InstantiateInternal(asset, parent, active);
                if (callback != null)
                    callback(path, instance);
            };
            GetGameObjectAsync(nameWithPath, internalCallback);
        }

        public static void InstantiateGameObjectAsync(PrefabAddress addressAsset, Transform parent, System.Action<string, GameObject> callback, bool? active = null)
        {
            InstantiateGameObjectAsync(addressAsset != null ? addressAsset.Name : "", parent, callback, active);
        }

        static GameObject InstantiateInternal(GameObject asset, Transform parent, bool? active = null)
        {
            var cachedActiveSelf = asset != null ? asset.activeSelf : true;
            var targetActiveSelf = active != null ? active.Value : cachedActiveSelf;

            //Change active state, if needed
            if (asset != null && cachedActiveSelf != targetActiveSelf)
                asset.SetActive(targetActiveSelf);

            var instance = asset != null ? GameObject.Instantiate(asset, parent) : null;

            //Revert to old active state (in prefab)
            if (asset != null && asset.activeSelf != cachedActiveSelf)
                asset.SetActive(cachedActiveSelf);

            if (instance != null && parent != null)
            {
                instance.transform.localScale = Vector3.one;
                instance.transform.localEulerAngles = Vector3.zero;
                instance.transform.localPosition = Vector3.zero;
            }

            return instance;
        }

        class InternalResourcesLoader
        {
            public static void LoadAsync<T>(GenericAssetAddress<T> assetAddress, System.Action<string, T> callback) where T : UnityEngine.Object
            {
                var request = assetAddress.LoadAssetAsync(
                    (result) =>
                        {
                            if (callback != null)
                                callback(assetAddress.Name, result as T);
                        });
            }

            public static void LoadAsync<T>(string path, System.Action<string, T> callback) where T : UnityEngine.Object
            {
                var request = Resources.LoadAsync<T>(path);
                request.completed += (operation) =>
                {
                    if (callback != null)
                        callback(path, request.asset as T);
                };
            }
        }
    }
}