using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Events;

namespace Kyub.Async.Experimental
{
    [ExecuteInEditMode]
    [CreateAssetMenu(fileName ="Experimental_ExternalTexture2D")]
    public sealed class ExternalTexture2D : ScriptableObject, UnityEngine.ISerializationCallbackReceiver
    {
        [System.Serializable]
        public class Texture2DUnityEvent : UnityEvent<Texture2D> { }

        #region Fields

        [SerializeField]
        public string m_Url;
        [SerializeField, ReadOnly]
        public Texture2D m_Texture2D;
        public int m_MaxSize = 2048;
        [SerializeField, HideInInspector]
        bool m_IsPrefab = false;

        [System.NonSerialized]
        string _previousDownloadedUrl = string.Empty;
        [System.NonSerialized]
        bool _downloadIsDirty = false;

        #endregion

        #region Callbacks

        public Texture2DUnityEvent onDownload = new Texture2DUnityEvent();

        #endregion

        #region Properties

        public int maxSize
        {
            get
            {
                return m_MaxSize;
            }
            set
            {
                if (m_MaxSize == value)
                    return;
                m_MaxSize = value;
                if (hasTexture)
                {
                    var size = Mathf.Max(m_Texture2D.width, m_Texture2D.height);
                    if (size > m_MaxSize)
                    {
                        float mult = m_MaxSize / (float)size;
                        int width = Mathf.Clamp((int)Mathf.Ceil(m_Texture2D.width * mult), 1, m_MaxSize);
                        int height = Mathf.Clamp((int)Mathf.Ceil(m_Texture2D.height * mult), 1, m_MaxSize);
                        Kyub.Extensions.Texture2DExtensions.BilinearScale(m_Texture2D, width, height);
                    }
                }
            }
        }

        public bool hasTexture
        {
            get
            {
                return (texture != null && m_Texture2D.width > Texture2D.whiteTexture.width && m_Texture2D.height > Texture2D.whiteTexture.height) ;
            }
        }

        public Texture2D texture
        {
            get
            {
                if (!_downloadIsDirty)
                    OnEnable();
                return m_Texture2D;
            }
        }

        public string url
        {
            get
            {
                if (m_Url == null)
                    m_Url = string.Empty;
                return m_Url;
            }
            private set
            {
                if (m_Url == value)
                    return;
                m_Url = value;
                _previousDownloadedUrl = value;
                var newName = "Texture2D" + (!string.IsNullOrEmpty(url) ? "_" + url : String.Empty);
                if (newName != m_Texture2D.name)
                    m_Texture2D.name = newName;

                Download();
            }
        }

        public bool isDownloading
        {
            get
            {
                return RequestStackManager.IsRequesting(url);
            }
        }

        #endregion

        #region Texture Properties

        /// <summary>
        ///   <para>How many mipmap levels are in this texture (Read Only).</para>
        /// </summary>
        public int mipmapCount { get { return texture != null? m_Texture2D.mipmapCount : 0; } }

        //
        // Summary:
        //     Texture U coordinate wrapping mode.
        public TextureWrapMode wrapModeU
        {
            get { return texture != null ? m_Texture2D.wrapModeU : TextureWrapMode.Clamp; }
            set
            {
                if (m_Texture2D != null)
                    m_Texture2D.wrapModeU = value;
            }
        }
        //
        // Summary:
        //     Returns the GraphicsFormat format or color format of a texture object.
        public GraphicsFormat graphicsFormat { get { return texture != null ? m_Texture2D.graphicsFormat : GraphicsFormat.R8G8B8A8_SRGB; } }

        //
        // Summary:
        //     Height of the texture in pixels. (Read Only)
        public int height
        {
            get { return texture != null ? m_Texture2D.height : 0; }
        }
        //
        // Summary:
        //     Dimensionality (type) of the texture (Read Only).
        public TextureDimension dimension { get { return texture != null ? m_Texture2D.dimension : TextureDimension.Tex2D; } }

        // Summary:
        //     Width of the texture in pixels. (Read Only)
        public int width
        {
            get { return texture != null ? m_Texture2D.width : 0; }
        }
        //
        // Summary:
        //     This counter is incremented when the texture is updated.
        public uint updateCount
        {
            get { return texture != null ? m_Texture2D.updateCount : 0; }
        }
        public Vector2 texelSize
        {
            get { return texture != null ? m_Texture2D.texelSize : Vector2.zero; }
        }
        //
        // Summary:
        //     Mip map bias of the texture.
        public float mipMapBias
        {
            get { return texture != null ? m_Texture2D.mipMapBias : 0; }
            set
            {
                if (texture != null)
                    m_Texture2D.mipMapBias = value;
            }
        }
        //
        // Summary:
        //     Anisotropic filtering level of the texture.
        public int anisoLevel
        {
            get { return texture != null ? m_Texture2D.anisoLevel : 0; }
            set
            {
                if (texture != null)
                    m_Texture2D.anisoLevel = value;
            }
        }
        //
        // Summary:
        //     Returns true if the Read/Write Enabled checkbox was checked when the texture
        //     was imported; otherwise returns false. For a dynamic Texture created from script,
        //     always returns true. For additional information, see TextureImporter.isReadable.
        public bool isReadable
        {
            get { return texture != null ? m_Texture2D.isReadable : true; }
        }
        //
        // Summary:
        //     Texture W coordinate wrapping mode for Texture3D.
        public TextureWrapMode wrapModeW
        {
            get { return texture != null ? m_Texture2D.wrapModeW : TextureWrapMode.Clamp; }
            set
            {
                if (texture != null)
                    m_Texture2D.wrapModeW = value;
            }
        }
        //
        // Summary:
        //     Texture V coordinate wrapping mode.
        public TextureWrapMode wrapModeV
        {
            get { return m_Texture2D != null ? m_Texture2D.wrapModeV : TextureWrapMode.Clamp; }
            set
            {
                if (texture != null)
                    m_Texture2D.wrapModeV = value;
            }
        }
        //
        // Summary:
        //     The hash value of the Texture.
        public Hash128 imageContentsHash
        {
            get { return m_Texture2D != null ? m_Texture2D.imageContentsHash : new Hash128(); }
            set
            {
                if (texture != null)
                    m_Texture2D.imageContentsHash = value;
            }
        }
        //
        // Summary:
        //     Filtering mode of the texture.
        public FilterMode filterMode
        {
            get { return m_Texture2D != null ? m_Texture2D.filterMode : FilterMode.Point; }
            set
            {
                if (texture != null)
                    m_Texture2D.filterMode = value;
            }
        }

        /// <summary>
        ///   <para>The format of the pixel data in the texture (Read Only).</para>
        /// </summary>
        public TextureFormat format { get { return texture != null ? texture.format : TextureFormat.ARGB32; } }

        public bool alphaIsTransparency
        {
            get { return texture != null ? texture.alphaIsTransparency : true; }
            set
            {
                if (texture != null)
                    m_Texture2D.alphaIsTransparency = value;
            }
        }

        #endregion

        #region Constructors

        private ExternalTexture2D() : base()
        {
            m_Url = string.Empty;
        }

        public ExternalTexture2D(string url) : base()
        {
            m_Url = url;
        }

        public ExternalTexture2D(string url, int maxSize) : base()
        {
            m_Url = url;
            m_MaxSize = maxSize;
        }

        #endregion

        #region Unity Functions

        void OnEnable()
        {
#if UNITY_EDITOR
            m_IsPrefab = !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(this));
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            UnityEditor.EditorApplication.wantsToQuit -= OnWantsToQuit;
            UnityEditor.EditorApplication.wantsToQuit += OnWantsToQuit;
#endif
            if (m_Texture2D == null)
            {
                Clear();
                var newName = "Texture2D" + ( !string.IsNullOrEmpty(url) ? "_" + url : String.Empty);
                if (newName != m_Texture2D.name)
                    m_Texture2D.name = newName;
            }

            //Only begin when in playmode
            if (Application.isPlaying)
            {
                _downloadIsDirty = true;
                Download();
            }
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            OnValidate();
#endif
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.wantsToQuit -= OnWantsToQuit;
#endif
            if (!m_IsPrefab)
            {
                if (m_Texture2D != null)
                {
                    if (Application.isPlaying)
                        Texture2D.Destroy(m_Texture2D);
                    else
                        Texture2D.DestroyImmediate(m_Texture2D);
                }
            }
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                Clear(false);
            OnValidate();
#endif
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            OnValidate();
#endif
        }

        #endregion

        #region Editor Unity Functions

#if UNITY_EDITOR

        void OnValidateImmediate()
        {
            m_IsPrefab = !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(this));
            if (texture != null)
            {
                //Clear Pre-loaded data when not playing
                if (!Application.isPlaying && hasTexture)
                    Clear();

                var newName = "Texture2D" + (!string.IsNullOrEmpty(url) ? "_" + url : String.Empty);
                if (newName != m_Texture2D.name)
                {
                    m_Texture2D.name = newName;
                    if (m_IsPrefab && !Application.isPlaying)
                        UnityEditor.EditorUtility.SetDirty(this);
                }
                if (!Application.isPlaying && m_IsPrefab && string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(m_Texture2D)))
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(m_Texture2D, this);
                    UnityEditor.AssetDatabase.ImportAsset(UnityEditor.AssetDatabase.GetAssetPath(m_Texture2D));
                }
            }

            if (Application.isPlaying && _previousDownloadedUrl != url)
            {
                _previousDownloadedUrl = url;
                Download();
            }
        }

        void OnValidate()
        {
            UnityEditor.EditorApplication.CallbackFunction autoUnregister = null;
            autoUnregister = () =>
            {
                UnityEditor.EditorApplication.update -= autoUnregister;
                OnValidateImmediate();
            };
            UnityEditor.EditorApplication.update += autoUnregister;
        }

        void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange mode)
        {
            Clear(false);
            OnValidate();
        }

        bool OnWantsToQuit()
        {
            Clear(false);
            return true;
        }
#endif

        #endregion

        #region Download Functions

        public void Clear(bool canCreateTexture = true)
        {
            if (m_Texture2D == null)
            {
                m_Texture2D = new Texture2D(Texture2D.whiteTexture.width, Texture2D.whiteTexture.height, TextureFormat.ARGB32, false);
                m_Texture2D.hideFlags = HideFlags.NotEditable;
            }

            if (hasTexture)
            {
                m_Texture2D.LoadImage(Texture2D.whiteTexture.EncodeToJPG());
                m_Texture2D.hideFlags = HideFlags.NotEditable;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /*bool RecreateSprite()
        {
            if (m_Sprite == null || m_Texture2D == null || (m_Sprite.rect.width != m_Texture2D.width || m_Sprite.rect.height != m_Texture2D.height))
            {
                if (m_Texture2D != null)
                {
                    m_Sprite = Sprite.Create(m_Texture2D, new Rect(0, 0, m_Texture2D.width, m_Texture2D.height), new Vector2(0.5f, 0.5f));
                    return true;
                }
            }
            return false;
        }*/

        public void Download(System.Action<string, Texture2D> onFinish = null)
        {
            if (texture != null && !RequestStackManager.IsRequesting(url))
            {
                if (RequestStackManager.Instance != null)
                    RequestStackManager.RequestRoutine(null, RequestTextureRoutine(onFinish), url);
            }
        }

        IEnumerator RequestTextureRoutine(System.Action<string, Texture2D> onFinish)
        {
            if (this != null)
            {
                using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return www.SendWebRequest();

                    if (this != null)
                    {
                        if (www.isNetworkError || www.isHttpError)
                        {
                            Clear();
                            Debug.Log("[ExternalTexture2D] " + url + " Failed: " + www.error);
                        }
                        else
                        {
                            if (m_Texture2D == null)
                                Clear();

                            var bytes = www.downloadHandler.data;
                            m_Texture2D.LoadImage(bytes);
                            m_Texture2D.hideFlags = HideFlags.NotEditable;
                            if (hasTexture)
                            {
                                var size = Mathf.Max(m_Texture2D.width, m_Texture2D.height);
                                if (size > m_MaxSize)
                                {
                                    float mult = m_MaxSize / (float)size;
                                    int width = Mathf.Clamp((int)Mathf.Ceil(m_Texture2D.width * mult), 1, m_MaxSize);
                                    int height = Mathf.Clamp((int)Mathf.Ceil(m_Texture2D.height * mult), 1, m_MaxSize);
                                    Kyub.Extensions.Texture2DExtensions.BilinearScale(m_Texture2D, width, height);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (onFinish != null)
                            onFinish(url, null);
                        yield break;
                    }
                }

                m_Texture2D.IncrementUpdateCount();
                _previousDownloadedUrl = url;
                if (onDownload != null)
                    onDownload.Invoke(m_Texture2D);
            }

            if (onFinish != null)
                onFinish(url, m_Texture2D);
        }

        #endregion

        #region Texture2D Functions

        /// <summary>
        ///   <para>Sets pixel color at coordinates (x,y).</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixel(x, y, color);
        }

        /// <summary>
        ///   <para>Returns pixel color at coordinates (x, y).</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Color GetPixel(int x, int y)
        {
            Color color = Color.clear;
            if (m_Texture2D != null)
                color = m_Texture2D.GetPixel(x, y);

            return color;
        }

        /// <summary>
        ///   <para>Returns filtered pixel color at normalized coordinates (u, v).</para>
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        public Color GetPixelBilinear(float u, float v)
        {
            Color color = Color.clear;
            if (m_Texture2D != null)
                color = m_Texture2D.GetPixelBilinear(u, v);

            return color;
        }

        public void SetPixels(Color[] colors)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels(colors);
        }

        /// <summary>
        ///   <para>Set a block of pixel colors.</para>
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="miplevel"></param>
        public void SetPixels(Color[] colors, int miplevel)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels(colors, miplevel);
        }

        /// <summary>
        ///   <para>Set a block of pixel colors.</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        /// <param name="colors"></param>
        /// <param name="miplevel"></param>
        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors, int miplevel)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels(x, y, blockWidth, blockHeight, colors, miplevel);
        }

        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels(x, y, blockWidth, blockHeight, colors);
        }

        public void SetPixels32(Color32[] colors)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels32(colors);
        }

        /// <summary>
        ///   <para>Set a block of pixel colors.</para>
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="miplevel"></param>
        public void SetPixels32(Color32[] colors, int miplevel)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels32(colors, miplevel);
        }

        public void SetPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels32(x, y, blockWidth, blockHeight, colors);
        }

        /// <summary>
        ///   <para>Set a block of pixel colors.</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        /// <param name="colors"></param>
        /// <param name="miplevel"></param>
        public void SetPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors,  int miplevel)
        {
            if (m_Texture2D != null)
                m_Texture2D.SetPixels32(x, y, blockWidth, blockHeight, colors, miplevel);
        }

        /// <summary>
        ///   <para>Loads PNG/JPG image byte array into a texture.</para>
        /// </summary>
        /// <param name="data">The byte array containing the image data to load.</param>
        /// <param name="markNonReadable">Set to false by default, pass true to optionally mark the texture as non-readable.</param>
        /// <returns>
        ///   <para>Returns true if the data can be loaded, false otherwise.</para>
        /// </returns>
        public bool LoadImage(byte[] data, bool markNonReadable)
        {
            if(m_Texture2D != null)
                return m_Texture2D.LoadImage(data, markNonReadable);

            return false;
        }

        public bool LoadImage(byte[] data)
        {
            bool markNonReadable = false;
            return this.LoadImage(data, markNonReadable);
        }

        /// <summary>
        ///   <para>Fills texture pixels with raw preformatted data.</para>
        /// </summary>
        /// <param name="data">Byte array to initialize texture pixels with.</param>
        /// <param name="size">Size of data in bytes.</param>
        public void LoadRawTextureData(byte[] data)
        {
            if (m_Texture2D != null)
                m_Texture2D.LoadRawTextureData(data);
        }

        /// <summary>
        ///   <para>Fills texture pixels with raw preformatted data.</para>
        /// </summary>
        /// <param name="data">Byte array to initialize texture pixels with.</param>
        /// <param name="size">Size of data in bytes.</param>
        public void LoadRawTextureData(IntPtr data, int size)
        {
            if (m_Texture2D != null)
                m_Texture2D.LoadRawTextureData(data, size);
        }

        /// <summary>
        ///   <para>Get raw data from a texture.</para>
        /// </summary>
        /// <returns>
        ///   <para>Raw texture data as a byte array.</para>
        /// </returns>
        public byte[] GetRawTextureData()
        {
            if (m_Texture2D != null)
                return m_Texture2D.GetRawTextureData();

            return new byte[0];
        }

        public Color[] GetPixels()
        {
            return this.GetPixels(0);
        }

        /// <summary>
        ///   <para>Get a block of pixel colors.</para>
        /// </summary>
        /// <param name="miplevel"></param>
        public Color[] GetPixels(int miplevel)
        {
            if(m_Texture2D != null)
                return m_Texture2D.GetPixels(miplevel);

            return new Color[0];
        }

        /// <summary>
        ///   <para>Get a block of pixel colors.</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        /// <param name="miplevel"></param>
        public Color[] GetPixels(int x, int y, int blockWidth, int blockHeight, int miplevel)
        {
            if (m_Texture2D != null)
                return m_Texture2D.GetPixels(x, y, blockWidth, blockHeight, miplevel);

            return new Color[0];
        }

        public Color[] GetPixels(int x, int y, int blockWidth, int blockHeight)
        {
            int miplevel = 0;
            return this.GetPixels(x, y, blockWidth, blockHeight, miplevel);
        }

        public Color32[] GetPixels32(int miplevel)
        {
            if (m_Texture2D != null)
                return m_Texture2D.GetPixels32(miplevel);

            return new Color32[0];
        }

        public Color32[] GetPixels32()
        {
            return this.GetPixels32(0);
        }

        /// <summary>
        ///   <para>Actually apply all previous SetPixel and SetPixels changes.</para>
        /// </summary>
        /// <param name="updateMipmaps"></param>
        /// <param name="makeNoLongerReadable"></param>
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable)
        {
            if (m_Texture2D != null)
                m_Texture2D.Apply(updateMipmaps, makeNoLongerReadable);
        }

        public void Apply(bool updateMipmaps)
        {
            bool makeNoLongerReadable = false;
            this.Apply(updateMipmaps, makeNoLongerReadable);
        }

        public void Apply()
        {
            this.Apply(true, false);
        }

        public bool Resize(int width, int height, TextureFormat format, bool hasMipMap)
        {
            if (m_Texture2D != null)
                return m_Texture2D.Resize(width, height, format, hasMipMap);

            return false;
        }

        /// <summary>
        ///   <para>Resizes the texture.</para>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public bool Resize(int width, int height)
        {
            if (m_Texture2D != null)
                return m_Texture2D.Resize(width, height);

            return false;
        }

        /// <summary>
        ///   <para>Compress texture into DXT format.</para>
        /// </summary>
        /// <param name="highQuality"></param>
        public void Compress(bool highQuality)
        {
            if (m_Texture2D != null)
                m_Texture2D.Compress(highQuality);
        }
        
        /// <summary>
        ///   <para>Encodes this texture into PNG format.</para>
        /// </summary>
        public byte[] EncodeToPNG()
        {
            if (m_Texture2D != null)
                return m_Texture2D.EncodeToPNG();

            return new byte[0];
        }

        /// <summary>
        ///   <para>Encodes this texture into JPG format.</para>
        /// </summary>
        /// <param name="quality">JPG quality to encode with, 1..100 (default 75).</param>
        public byte[] EncodeToJPG(int quality)
        {
            if (m_Texture2D != null)
                return m_Texture2D.EncodeToJPG(quality);

            return new byte[0];
        }

        /// <summary>
        ///   <para>Encodes this texture into JPG format.</para>
        /// </summary>
        /// <param name="quality">JPG quality to encode with, 1..100 (default 75).</param>
        public byte[] EncodeToJPG()
        {
            return this.EncodeToJPG(75);
        }

        #endregion

        #region Implicit Conversors

        public static implicit operator Texture2D(ExternalTexture2D externalTex)
        {
            return externalTex != null? externalTex.m_Texture2D : null;
        }

        #endregion
    }
}
