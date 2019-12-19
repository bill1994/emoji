
using System.Collections;
using UnityEngine;

namespace MaterialUI
{
    [System.Serializable]
    public class PrefabAddress : GenericAssetAddress<GameObject>
    {
        public static explicit operator PrefabAddress(string s)
        {
            return new PrefabAddress() { AssetPath = s };
        }
    }

    [System.Serializable]
    public class ComponentAddress : GenericAssetAddress<Component>
    {
        public static explicit operator ComponentAddress(string s)
        {
            return new ComponentAddress() { AssetPath = s };
        }
    }

    [System.Serializable]
    public class ScriptableObjectAddress : GenericAssetAddress<ScriptableObject>
    {
        public static explicit operator ScriptableObjectAddress(string s)
        {
            return new ScriptableObjectAddress() { AssetPath = s };
        }
    }

    [System.Serializable]
    public class AssetAddress : GenericAssetAddress<Object>
    {
        public static explicit operator AssetAddress(string s)
        {
            return new AssetAddress() { AssetPath = s };
        }
    }

    [System.Serializable]
    public abstract class GenericAssetAddress<T> : IAssetAddress, ISerializationCallbackReceiver where T : Object
    {
        #region Private Variables
        [SerializeField]
        protected string m_name;
        [SerializeField]
        protected internal T m_asset;
        [SerializeField]
        protected internal string m_assetPath;
        [SerializeField, Tooltip("Cache the loaded prefab")]
        protected internal bool m_keepLoaded = false;

        [System.NonSerialized]
        T _loadedAsset = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get
            {
                ValidateName();
                return m_name;
            }
            set
            {
                if (m_name == value)
                    return;
                m_name = value;
                ValidateName();
            }
        }

        public T Asset
        {
            get
            {
                return m_asset;
            }

            set
            {
                if (m_asset == value)
                    return;
                m_asset = value;
                m_assetPath = "";
                ValidateName();
            }
        }

        public string AssetPath
        {
            get
            {
                return m_assetPath;
            }

            set
            {
                if (m_assetPath == value)
                    return;
                m_assetPath = value;
                m_asset = null;
                ValidateName();
            }
        }

        public bool KeepLoaded
        {
            get
            {
                return m_keepLoaded;
            }

            set
            {
                if (m_keepLoaded == value)
                    return;
                m_keepLoaded = value;

                if (!m_keepLoaded)
                    ClearCache();
            }
        }

        #endregion

        #region Runtime Functions

        public bool IsResources()
        {
            return m_asset == null && !string.IsNullOrEmpty(m_assetPath);
        }

        public bool IsEmpty()
        {
            return m_asset == null && string.IsNullOrEmpty(m_assetPath);
        }

        public T LoadAsset()
        {
            var v_directAsset = m_asset != null ? m_asset : _loadedAsset;
            if (v_directAsset == null && !string.IsNullOrEmpty(m_assetPath))
            {
                v_directAsset = Resources.Load<T>(m_assetPath);
            }

            _loadedAsset = m_keepLoaded ? v_directAsset : null;

            return v_directAsset;
        }

        public AssetAdressAsyncOperation LoadAssetAsync(System.Action<Object> callback = null)
        {
            System.Action<Object> v_internalCallback = (asset) =>
            {
                _loadedAsset = m_keepLoaded ? asset as T : null;
                if (callback != null)
                    callback(asset);
            };

            AssetAdressAsyncOperation v_operation = null;
            var v_directAsset = m_asset != null ? m_asset : _loadedAsset;
            if (v_directAsset == null && !string.IsNullOrEmpty(m_assetPath))
                v_operation = new AssetAdressAsyncOperation(Resources.LoadAsync<T>(m_assetPath));
            else
                v_operation = new AssetAdressAsyncOperation(v_directAsset);

            v_operation.InitRequest(v_internalCallback);

            return v_operation;
        }

        public AssetAdressAsyncOperation LoadAssetAsync(System.Action<T> callback)
        {
            System.Action<Object> castedCallback = callback != null ?
                (result) =>
                {
                    if (callback != null)
                        callback(result as T);
                }
            : (System.Action<Object>)null;
            return LoadAssetAsync(castedCallback);
        }

        public virtual void Validate()
        {
            ValidateName();
#if UNITY_EDITOR
            if (_updateCallback != null)
                UnityEditor.EditorApplication.update -= _updateCallback;

            if (m_asset != null)
                EditorValidateAssetPath();
            else if (!string.IsNullOrEmpty(m_assetPath))
                m_asset = Resources.Load<T>(m_assetPath);
#endif
        }

        public virtual bool ClearCache()
        {
            var result = _loadedAsset != null;
            _loadedAsset = null;

            return result;
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void ValidateName()
        {
            if (string.IsNullOrEmpty(m_name))
            {
                if (m_asset != null)
                    m_name = m_asset.name;
                else if (!string.IsNullOrEmpty(m_assetPath))
                    m_name = System.IO.Path.GetFileNameWithoutExtension(m_assetPath);

            }
        }

        #endregion

        #region Operators

        public static implicit operator string(GenericAssetAddress<T> a)
        {
            return a != null ? a.AssetPath : "";
        }

        #endregion

        #region Editor Functions

#if UNITY_EDITOR

        UnityEditor.EditorApplication.CallbackFunction _updateCallback = null;
        protected internal virtual void EditorLoadAssetFromPathAsync()
        {
            if (_updateCallback != null)
                UnityEditor.EditorApplication.update -= _updateCallback;

            _updateCallback = null;
            _updateCallback = () =>
            {
                UnityEditor.EditorApplication.update -= _updateCallback;
                if (!string.IsNullOrEmpty(m_assetPath))
                    m_asset = Resources.Load<T>(m_assetPath);
                EditorValidateAssetPath();
            };
            UnityEditor.EditorApplication.update += _updateCallback;
        }

        protected internal virtual void EditorValidateAssetPathAsync()
        {
            if (_updateCallback != null)
                UnityEditor.EditorApplication.update -= _updateCallback;

            _updateCallback = null;
            _updateCallback = () =>
            {
                UnityEditor.EditorApplication.update -= _updateCallback;
                EditorValidateAssetPath();
            };
            UnityEditor.EditorApplication.update += _updateCallback;
        }

        const string RESOURCES_PATH = "Resources/";
        const string EDITOR_PATH = "Editor/";
        protected internal virtual bool EditorValidateAssetPath()
        {
            if (m_asset != null || !string.IsNullOrEmpty(m_assetPath))
            {
                //Check if Path is valid
                if (Asset == null)
                    m_assetPath = "";

                if (m_asset != null)
                {
                    var v_assetPath = UnityEditor.AssetDatabase.GetAssetPath(m_asset);
                    //Try find Resources Path (if resources is not inside Editor Path)
                    if (v_assetPath.Contains(RESOURCES_PATH) && !v_assetPath.Contains(EDITOR_PATH))
                    {
                        var v_paths = v_assetPath.Split(new string[] { RESOURCES_PATH }, System.StringSplitOptions.None);
                        if (v_paths.Length > 1)
                        {
                            m_assetPath = v_paths[1] != null ? v_paths[1].Replace(System.IO.Path.GetExtension(v_paths[1]), "") : "";
                            ValidateName();
                            return true;
                        }
                    }
                }
            }

            ValidateName();
            return false;
        }
#endif

        #endregion

        #region IAssetAddress

        UnityEngine.Object IAssetAddress.Asset { get { return Asset; } set { Asset = value as T; } }

        UnityEngine.Object IAssetAddress.LoadAsset()
        {
            return LoadAsset();
        }

        #endregion

        #region Serialization Callback

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if !UNITY_EDITOR
            if (!string.IsNullOrEmpty(m_assetPath))
                m_asset = null;
            else if(m_asset != null)
                m_assetPath = "";
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (m_asset != null)
                EditorValidateAssetPathAsync();
            else if (!string.IsNullOrEmpty(m_assetPath))
                EditorLoadAssetFromPathAsync();
#endif
        }

        #endregion
    }

    public class AssetAdressAsyncOperation : YieldInstruction
    {
        #region Private Variables

        protected internal Object _directReference = null;
        protected internal ResourceRequest _internalRequest = null;

        #endregion

        #region Public Properties

        public Object asset
        {
            get
            {
                if (_internalRequest != null)
                    return _internalRequest.asset;
                return _directReference;
            }
        }

        public bool isDone
        {
            get
            {
                if (_internalRequest != null)
                    return _internalRequest.isDone;
                return true;
            }
        }

        public float progress
        {
            get
            {
                if (_internalRequest != null)
                    return _internalRequest.progress;
                return 1;
            }
        }

        #endregion

        #region Events

        public event System.Action<Object> onComplete;

        #endregion

        #region Constructors

        public AssetAdressAsyncOperation(ResourceRequest operation)
        {
            _internalRequest = operation;
            _directReference = null;
        }

        public AssetAdressAsyncOperation(Object asset)
        {
            _internalRequest = null;
            _directReference = asset;
        }

        #endregion

        #region Public Functions

        public void InitRequest(System.Action<Object> callback)
        {
            if (_internalRequest != null)
            {
                if (!_internalRequest.isDone)
                {
                    System.Action<AsyncOperation> resultCallback = null;
                    resultCallback = (result) =>
                    {
                        _internalRequest.completed -= resultCallback;
                        var assetLoaded = _internalRequest != null ? _internalRequest.asset : null;
                        if (onComplete != null)
                            onComplete(assetLoaded);

                        if (callback != null)
                            callback.Invoke(assetLoaded);
                    };
                    _internalRequest.completed += resultCallback;
                }
                else
                {
                    var assetLoaded = _internalRequest != null ? _internalRequest.asset : null;
                    if (onComplete != null)
                        onComplete(assetLoaded);

                    if (callback != null)
                        callback.Invoke(assetLoaded);
                }
            }
            else
            {
                if (onComplete != null)
                    onComplete(_directReference);

                if (callback != null)
                    callback.Invoke(_directReference);
            }
        }

        #endregion
    }

    public interface IAssetAddress
    {
        #region Public Properties

        UnityEngine.Object Asset
        {
            get;
            set;
        }

        string AssetPath
        {
            get;
            set;
        }

        #endregion

        #region Runtime Load Functions

        Object LoadAsset();

        AssetAdressAsyncOperation LoadAssetAsync(System.Action<Object> callback = null);

        #endregion
    }
}
