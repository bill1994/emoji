using Kyub.Collections;
using Kyub.Extensions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kyub.Async
{
    public static class ExternalResources
    {
        #region Helper Classes/Delegates

        public delegate ExternImgFile CustomImgUrlDownloader(string p_url, System.Action<ExternImgFile> p_callback);
        public delegate ExternAudioFile CustomAudioUrlDownloader(string p_url, System.Action<ExternAudioFile> p_callback);

        #endregion

        #region Events

        public static event System.Action<ExternImgFile> OnImageLoaded;
        public static event System.Action<ExternAudioFile> OnAudioLoaded;

        #endregion

        #region Properties

        public const string REGEX_FILE_PATH = @"(file:\/\/\/).+";
        public const string REGEX_TEMP_PATH_ASSETS_PATH = @"(TemporaryCache:\/\/).+";
        public const string REGEX_PERSISTENT_ASSETS_PATH = @"(PersistentData:\/\/).+";
        public const string REGEX_STREAMING_ASSETS_PATH = @"(StreamingAssets:\/\/).+";
        public const string REGEX_RESOURCES_PATH = @"(Resources:\/\/).+";
        
        static ArrayDict<string, CustomImgUrlDownloader> s_customImgDownloaderPatterns = new ArrayDict<string, CustomImgUrlDownloader>()
        {
            Buffer = new List<KVPair<string, CustomImgUrlDownloader>>()
            {
                //Urls Validator
                new KVPair<string, CustomImgUrlDownloader>(StringExtensions.REGEX_STRING_URL1, TextureSerializer.DeserializeFromWeb),
                new KVPair<string, CustomImgUrlDownloader>(StringExtensions.REGEX_STRING_URL2, TextureSerializer.DeserializeFromWeb),
                new KVPair<string, CustomImgUrlDownloader>(REGEX_FILE_PATH, TextureSerializer.DeserializeFromWeb),
                new KVPair<string, CustomImgUrlDownloader>(REGEX_TEMP_PATH_ASSETS_PATH, (path, callback) =>
                {
                    var temporaryCachePath = path.Replace("TemporaryCache://", Application.temporaryCachePath + "/");
                    System.Action<ExternImgFile> internalCallback = (externImgParam) =>
                    {
                        if(externImgParam != null)
                            externImgParam.Url = path;
                        if(callback != null)
                            callback(externImgParam);
                    };
                    var externalImg = TextureSerializer.DeserializeFromWeb(temporaryCachePath, callback);
                    if(externalImg != null)
                        externalImg.Url = path;
                    return externalImg;
                }),
                new KVPair<string, CustomImgUrlDownloader>(REGEX_PERSISTENT_ASSETS_PATH, (path, callback) => 
                {
                    var persistentDataPath = path.Replace("PersistentData://", Application.persistentDataPath + "/");
                    System.Action<ExternImgFile> internalCallback = (externImgParam) =>
                    {
                        if(externImgParam != null)
                            externImgParam.Url = path;
                        if(callback != null)
                            callback(externImgParam);
                    };
                    var externalImg = TextureSerializer.DeserializeFromWeb(persistentDataPath, callback);
                    if(externalImg != null)
                        externalImg.Url = path;
                    return externalImg;
                }),
                new KVPair<string, CustomImgUrlDownloader>(REGEX_STREAMING_ASSETS_PATH, (path, callback) =>
                {
                    var streamingAssetsPath = path.Replace("StreamingAssets://", Application.streamingAssetsPath + "/");
                    System.Action<ExternImgFile> internalCallback = (externImgParam) =>
                    {
                        if(externImgParam != null)
                            externImgParam.Url = path;
                        if(callback != null)
                            callback(externImgParam);
                    };
                    var externalImg = TextureSerializer.DeserializeFromWeb(streamingAssetsPath, callback);
                    if(externalImg != null)
                        externalImg.Url = path;
                    return externalImg;
                }),
                new KVPair<string, CustomImgUrlDownloader>(REGEX_RESOURCES_PATH, 
                    (path, callback) =>
                    {
                        var resourcesPath = path.Replace("Resources://", string.Empty);
                        ExternImgFile externalImg = new ExternImgFile();
                        externalImg.Url = path;
                        externalImg.Status = AsyncStatusEnum.Processing;

                        //Request as Texture
                        var textureLoadOperation  = Resources.LoadAsync<Texture2D>(resourcesPath);
                        textureLoadOperation.completed += (op2) =>
                        {
                            Sprite sprite = null;
                            var texture = textureLoadOperation.asset as Texture2D;
                            if(texture != null)
                            {
                                var originalTexture = texture;
                                texture = new Texture2D(originalTexture.width, originalTexture.height);
                                texture.LoadRawTextureData(originalTexture.GetRawTextureData());
                                sprite = Sprite.Create(texture, new Rect(0,0,texture.width, texture.height), new Vector2(0.5f, 0.5f));

                                //Now we must unload original Asset (DestroyImmediate cant handle resources asset)
                                Resources.UnloadAsset(originalTexture);
                            }

                            externalImg.Texture = texture;
                            externalImg.Sprite = sprite;
                            externalImg.Url = path;
                            externalImg.Status = AsyncStatusEnum.Done;
                            externalImg.Error = texture == null? "Invalid resources file path." : null;

                            if(callback != null)
                                callback.Invoke(externalImg);
                        };

                        return externalImg;
                    })
            }
        };

        static ArrayDict<string, CustomAudioUrlDownloader> s_customAudioDownloaderPatterns = new ArrayDict<string, CustomAudioUrlDownloader>()
        {
            Buffer = new List<KVPair<string, CustomAudioUrlDownloader>>()
            {
                //Urls Validator
                new KVPair<string, CustomAudioUrlDownloader>(StringExtensions.REGEX_STRING_URL1, AudioSerializer.DeserializeFromWeb),
                new KVPair<string, CustomAudioUrlDownloader>(StringExtensions.REGEX_STRING_URL2, AudioSerializer.DeserializeFromWeb),
                new KVPair<string, CustomAudioUrlDownloader>(REGEX_FILE_PATH, AudioSerializer.DeserializeFromWeb),
            }
        };

        public static ArrayDict<string, CustomImgUrlDownloader> CustomImgDownloaderPatterns
        {
            get
            {
                if (s_customImgDownloaderPatterns == null)
                    s_customImgDownloaderPatterns = new ArrayDict<string, CustomImgUrlDownloader>();
                return s_customImgDownloaderPatterns;
            }
        }

        public static ArrayDict<string, CustomAudioUrlDownloader> CustomAudioDownloaderPatterns
        {
            get
            {
                if (s_customAudioDownloaderPatterns == null)
                    s_customAudioDownloaderPatterns = new ArrayDict<string, CustomAudioUrlDownloader>();
                return s_customAudioDownloaderPatterns;
            }
        }

        static Dictionary<string, Object> s_assetDictionary = new Dictionary<string, Object>();

        public static Dictionary<string, Object> AssetDictionary
        {
            get
            {
                if (s_assetDictionary == null)
                    s_assetDictionary = new Dictionary<string, Object>();
                return s_assetDictionary;
            }
        }

        static ArrayDict<string, System.Delegate> _pendentActions = new ArrayDict<string, System.Delegate>();

        static ArrayDict<string, System.Delegate> PendentActions
        {
            get
            {
                if (_pendentActions == null)
                    _pendentActions = new ArrayDict<string, System.Delegate>();
                return _pendentActions;
            }
            set
            {
                if (_pendentActions == value)
                    return;
                _pendentActions = value;
            }
        }

        static ArrayDict<string, ExternalResourcesReceiver> _referenceCounter = new ArrayDict<string, ExternalResourcesReceiver>();

        public static ArrayDict<string, ExternalResourcesReceiver> ReferenceCounter
        {
            get
            {
                if (_referenceCounter == null)
                    _referenceCounter = new ArrayDict<string, ExternalResourcesReceiver>();
                return _referenceCounter;
            }
            set
            {
                if (_referenceCounter == value)
                    return;
                _referenceCounter = value;
            }
        }

        #endregion

        #region Constructor

        static ExternalResources()
        {
            SceneManager.sceneLoaded += HandleOnLevelLoaded;
        }

        #endregion

        #region Level Receivers

        static void HandleOnLevelLoaded(Scene p_scene, LoadSceneMode p_mode)
        {
            UnloadUnusedAssetsImmediate();
        }

        #endregion

        #region Public Register Functions

        public static void RegisterReceiver(ExternalResourcesReceiver p_receiver)
        {
            UnregisterReceiver(p_receiver);
            if (p_receiver != null)
                ReferenceCounter.AddChecking(p_receiver.Key, p_receiver);
        }

        public static void UnregisterReceiver(ExternalResourcesReceiver p_receiver)
        {
            if (p_receiver != null)
                ReferenceCounter.RemoveChecking(p_receiver);
        }

        public static void RegisterCustomImgDownloaderPattern(string p_pattern, CustomImgUrlDownloader p_delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(p_pattern))
                CustomImgDownloaderPatterns.AddReplacing(p_pattern, p_delegateToCallWithThisPattern);
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void RegisterCustomAudioDownloaderPattern(string p_pattern, CustomAudioUrlDownloader p_delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(p_pattern))
                CustomAudioDownloaderPatterns.AddReplacing(p_pattern, p_delegateToCallWithThisPattern);
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void RegisterCustomImgDownloaderPattern(int p_index, string p_pattern, CustomImgUrlDownloader p_delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(p_pattern))
            {
                CustomImgDownloaderPatterns.RemoveByKey(p_pattern);
                var v_pair = new Kyub.Collections.KVPair<string, CustomImgUrlDownloader>(p_pattern, p_delegateToCallWithThisPattern);
                if (p_index < 0 || p_index >= CustomImgDownloaderPatterns.Count)
                    CustomImgDownloaderPatterns.Add(v_pair);
                else
                    CustomImgDownloaderPatterns.Insert(p_index, v_pair);
            }
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void RegisterCustomAudioDownloaderPattern(int p_index, string p_pattern, CustomAudioUrlDownloader p_delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(p_pattern))
            {
                CustomAudioDownloaderPatterns.RemoveByKey(p_pattern);
                var v_pair = new Kyub.Collections.KVPair<string, CustomAudioUrlDownloader>(p_pattern, p_delegateToCallWithThisPattern);
                if (p_index < 0 || p_index >= CustomAudioDownloaderPatterns.Count)
                    CustomAudioDownloaderPatterns.Add(v_pair);
                else
                    CustomAudioDownloaderPatterns.Insert(p_index, v_pair);
            }
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void UnregisterCustomImgDownloaderPattern(string p_pattern)
        {
            if (!string.IsNullOrEmpty(p_pattern))
                CustomImgDownloaderPatterns.RemoveByKey(p_pattern);
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void UnregisterCustomAudioDownloaderPattern(string p_pattern)
        {
            if (!string.IsNullOrEmpty(p_pattern))
                CustomAudioDownloaderPatterns.RemoveByKey(p_pattern);
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        #endregion

        #region Public Audio Functions

        public static ExternAudioFile AddAudioIntoCache(ExternAudioFile p_audio)
        {
            AudioClip v_audioClip = p_audio != null ? p_audio.Clip : null;
            return AddAudioIntoCache(p_audio != null ? p_audio.Url : "", v_audioClip);
        }

        public static ExternAudioFile AddAudioIntoCache(string p_key, AudioClip p_audio)
        {
            //Try avoid add same sprite twice
            bool v_keyIsNull = string.IsNullOrEmpty(p_key);
            if (!v_keyIsNull &&
                (string.IsNullOrEmpty(TryFindKeyWithAudio(p_audio, false)))
               )
            {
                return AddClipIntoCache_Internal(p_key, p_audio);
            }
            else
            {
                if (v_keyIsNull)
                    Debug.LogWarning("Trying to add an empty key into ExternalResources cache");
                else
                    Debug.LogWarning("Trying to add a duplicated Audio into ExternalResources cache");
            }
            return null;
        }
        
        public static ExternAudioFile ReloadAudioAsync(string p_key, System.Action<ExternAudioFile> p_callback = null)
        {
            return ReloadAudioFromWebInternal(p_key, p_callback);
        }

        public static ExternAudioFile ReloadClipAsync(string p_key, System.Action<AudioClip> p_callback = null)
        {
            return ReloadAudioFromWebInternal(p_key, p_callback);
        }

        public static ExternAudioFile LoadAudioAsync(string p_key, System.Action<ExternAudioFile> p_callback = null)
        {
            return GetAudioInternal(p_key, p_callback);
        }

        public static ExternAudioFile LoadClipAsync(string p_key, System.Action<AudioClip> p_callback = null)
        {
            return GetAudioInternal(p_key, p_callback);
        }

        public static AudioClip LoadClipFromCache(string p_key)
        {
            return GetCachedClip(p_key);
        }

        #endregion

        #region Public Image Functions

        public static ExternImgFile AddImageIntoCache(ExternImgFile p_image, bool p_acceptMultipleKeysWithSameTexture = true)
        {
            Sprite v_sprite = p_image != null ? p_image.Sprite : null;
            return AddSpriteIntoCache(p_image != null ? p_image.Url : "", v_sprite, p_acceptMultipleKeysWithSameTexture);
        }

        public static ExternImgFile AddSpriteIntoCache(string p_key, Sprite p_sprite, bool p_acceptMultipleKeysWithSameTexture = true)
        {
            //Try avoid add same sprite twice
            bool v_keyIsNull = string.IsNullOrEmpty(p_key);
            if (!v_keyIsNull && 
                (p_acceptMultipleKeysWithSameTexture || string.IsNullOrEmpty(TryFindKeyWithSprite(p_sprite, false))) 
               )
            {
                return AddSpriteIntoCache_Internal(p_key, p_sprite);
            }
            else
            {
                if (v_keyIsNull)
                    Debug.LogWarning("Trying to add an empty key into ExternalResources cache");
                else
                    Debug.LogWarning("Trying to add a duplicated Sprite into ExternalResources cache");
            }
            return null;
        }

        public static ExternImgFile AddTextureIntoCache(string p_key, Texture2D p_texture, bool p_acceptMultipleKeysWithSameTexture = true)
        {
            //Try avoid add same sprite twice
            bool v_keyIsNull = string.IsNullOrEmpty(p_key);
            if (!v_keyIsNull && 
                (p_acceptMultipleKeysWithSameTexture || string.IsNullOrEmpty(TryFindKeyWithTexture(p_texture, false))) 
               )
            {
                Sprite v_sprite = p_texture != null ? Sprite.Create(p_texture, new Rect(0, 0, p_texture.width, p_texture.height), new Vector2(0.5f, 0.5f)) : null;
                return AddSpriteIntoCache_Internal(p_key, v_sprite);
            }
            else
            {
                if (v_keyIsNull)
                    Debug.LogWarning("Trying to add an empty key into ExternalResources cache");
                else
                    Debug.LogWarning("Trying to add a duplicated Texture2D into ExternalResources cache");
            }
            return null;
        }

        public static ExternImgFile AddTextureDataIntoCache(string p_key, byte[] p_textureData, bool p_acceptMultipleKeysWithSameTexture = true)
        {
            if (!string.IsNullOrEmpty(p_key))
            {
                Texture2D v_texture = new Texture2D(4, 4);
                if (!v_texture.LoadImage(p_textureData))
                {
                    if (v_texture != null)
                        Object.DestroyImmediate(v_texture);
                    v_texture = null;
                }
                return AddTextureIntoCache(p_key, v_texture, p_acceptMultipleKeysWithSameTexture);
            }
            return null;
        }

        public static ExternImgFile ReloadImageAsync(string p_key, System.Action<ExternImgFile> p_callback = null)
        {
            return ReloadImageFromWebInternal(p_key, p_callback);
        }

        public static ExternImgFile ReloadSpriteAsync(string p_key, System.Action<Sprite> p_callback = null)
        {
            return ReloadImageFromWebInternal(p_key, p_callback);
        }

        public static ExternImgFile ReloadTextureAsync(string p_key, System.Action<Texture2D> p_callback = null)
        {
            return ReloadImageFromWebInternal(p_key, p_callback);
        }

        public static ExternImgFile LoadImageAsync(string p_key, System.Action<ExternImgFile> p_callback = null)
        {
            return GetImageInternal(p_key, p_callback);
        }

        public static ExternImgFile LoadSpriteAsync(string p_key, System.Action<Sprite> p_callback = null)
        {
            return GetImageInternal(p_key, p_callback);
        }

        public static ExternImgFile LoadTextureAsync(string p_key, System.Action<Texture2D> p_callback = null)
        {
            return GetImageInternal(p_key, p_callback);
        }

        public static Sprite LoadSpriteFromCache(string p_key)
        {
            return GetCachedSprite(p_key);
        }

        public static Texture2D LoadTextureFromCache(string p_key)
        {
            return GetCachedTexture(p_key);
        }

        #endregion

        #region Public Asset Functions

        public static void UnloadAsset(string p_key, bool p_immediate = false)
        {
            UnloadAssetInternal(p_key, p_immediate);
        }

        public static void UnloadAssets(IEnumerable<string> p_keys, bool p_immediate = false)
        {
            if (p_keys == null)
                p_keys = new List<string>();
            HashSet<string> v_keysToUnload = new HashSet<string>();
            HashSet<Object> v_elementsToUnload = new HashSet<Object>();

            foreach (var v_key in p_keys)
            {
                if (!v_keysToUnload.Contains(v_key) && s_assetDictionary.ContainsKey(v_key))
                {
                    v_keysToUnload.Add(v_key);
                    var v_asset = AssetDictionary[v_key];
                    if (v_asset == null)
                        continue;

                    if (!v_elementsToUnload.Contains(v_asset))
                        v_elementsToUnload.Add(v_asset);

                    //Fill sprites that we must try destroy
                    var v_sprite = v_asset as Sprite;
                    if (v_sprite != null)
                    {
                        var v_texture = v_sprite.texture;
                        if (v_texture != null && !v_elementsToUnload.Contains(v_texture))
                            v_elementsToUnload.Add(v_texture);
                    }

                }
            }

            //We must skip destroy step to destroy everything in same loop (optimize find)
            foreach (string v_key in v_keysToUnload)
            {
                if (!string.IsNullOrEmpty(v_key))
                {
                    UnloadAssetInternal(v_key, false, UnloadMode.SkipDestroyStep);
                }
            }

            //Reset to current dictionary keys
            var v_currentDictKeys = new List<string>(AssetDictionary.Keys);
            //Remove elements that keep reference in dictionary of images (two keys with same texture or sprite)
            foreach (var v_key in v_currentDictKeys)
            {
                var v_asset = AssetDictionary[v_key];
                if (v_asset == null)
                    continue;

                v_elementsToUnload.Remove(v_asset);

                var v_sprite = v_asset as Sprite;
                if (v_sprite != null)
                {
                    var v_texture = v_sprite.texture;
                    if (v_texture != null)
                        v_elementsToUnload.Remove(v_texture);
                }
            }
            //Destroy Sprite and Texture
            List<Object> v_elementsToDestroy = new List<Object>(v_elementsToUnload);
            v_elementsToUnload.Clear();
            for (int i = 0; i < v_elementsToDestroy.Count; i++)
            {
                if (v_elementsToDestroy[i] != null)
                {
                    if (p_immediate)
                        Object.DestroyImmediate(v_elementsToDestroy[i]);
                    else
                        DestroyUtils.DestroyImmediate(v_elementsToDestroy[i]);
                }
            }
            v_elementsToDestroy.Clear();
            RemoveKeysWithNullValuesInDictionary(AssetDictionary);
        }

        static bool _isUloadingAssets = false;
        public static void UnloadUnusedAssets()
        {
            if (!_isUloadingAssets)
            {
                _isUloadingAssets = true;
                DelayedFunctionUtils.CallFunction(new System.Action(UnloadUnusedAssetsImmediate), 0.1f);
            }
        }

        public static bool IsUselessResources(string p_key)
        {
            ReferenceCounter.RemovePairsWithNullValues();
            return !ReferenceCounter.ContainsKey(p_key);
        }

        #endregion

        #region Internal Resources Functions

        private static void UnloadAssetInternal(string p_url, bool p_immediate = false, UnloadMode p_mode = UnloadMode.DestroyIfNeeded)
        {
            if (!string.IsNullOrEmpty(p_url))
            {
                if (AssetDictionary.ContainsKey(p_url) && AssetDictionary[p_url] is AudioClip)
                    UnloadAudioInternal(p_url, false, UnloadMode.SkipDestroyStep);
                else
                    UnloadImageInternal(p_url, false, UnloadMode.SkipDestroyStep);
            }
        }

        private static void UnloadUnusedAssetsImmediate()
        {
            HashSet<string> v_keysToUnload = new HashSet<string>();
            List<string> v_keys = new List<string>(AssetDictionary.Keys);
            foreach (var v_key in v_keys)
            {
                if (IsUselessResources(v_key) && !v_keysToUnload.Contains(v_key))
                    v_keysToUnload.Add(v_key);
            }
            UnloadAssets(v_keysToUnload, false);
            _isUloadingAssets = false;
        }

        private static void RemoveKeysWithNullValuesInDictionary<T>(Dictionary<string, T> p_dict) where T : UnityEngine.Object
        {
            List<string> v_keys = new List<string>(p_dict.Keys);
            foreach (var v_key in v_keys)
            {
                if (p_dict[v_key] == null)
                    p_dict.Remove(v_key);
            }
        }

        #endregion

        #region Audio Cacher Internal

        private static ExternAudioFile AddClipIntoCache_Internal(string p_key, AudioClip p_audioClip)
        {
            //Try avoid add same sprite twice
            Object v_asset = null;
            AudioClip v_loadedAudioClip = null;
            AssetDictionary.TryGetValue(p_key, out v_asset);
            v_loadedAudioClip = v_asset as AudioClip;

            if (v_loadedAudioClip != p_audioClip)
            {
                UnloadAudioInternal(p_key, false, UnloadMode.SkipDestroyStep);
                //Try destroy previous sprite and textures
                if (v_loadedAudioClip != null && v_loadedAudioClip != p_audioClip && string.IsNullOrEmpty(TryFindKeyWithAudio(v_loadedAudioClip)))
                    DestroyUtils.DestroyImmediate(v_loadedAudioClip);
            }
            AssetDictionary[p_key] = p_audioClip;

            ExternAudioFile v_externAudio = new ExternAudioFile();
            v_externAudio.Url = p_key;
            v_externAudio.Clip = p_audioClip;
            v_externAudio.Status = AsyncStatusEnum.Done;
            HandleOnAudioReceived(v_externAudio);
            return v_externAudio;
        }

        /// <summary>
        /// Try get Cached imaged in Dictionary or in PlayerPrefs. If not loaded, the function will try load from web and cache in Dictionary. 
        /// This functions will try return image loaded in dictionary immediately
        /// </summary>
        private static AudioClip GetCachedClip(string p_url)
        {
            Object v_asset = null;
            if (!AssetDictionary.ContainsKey(p_url))
                GetClip(p_url, null);
            AssetDictionary.TryGetValue(p_url, out v_asset);
            AudioClip v_audioClip = v_asset as AudioClip;

            return v_audioClip;
        }

        private static ExternAudioFile GetClip(string p_url, System.Action<AudioClip> p_callback)
        {
            return GetAudioInternal(p_url, p_callback);
        }

        /// <summary>
        /// Gets the image from Dictionary (If loaded), or from PlayerPrefs (If saved in prefs), or try load from Web (If not Loaded)
        /// This functions will return the Sprite Loaded in Function Callback (not immediately)
        /// </summary>
        private static ExternAudioFile GetAudioInternal(string p_url, System.Delegate p_callback)
        {
            if (string.IsNullOrEmpty(p_url))
            {
                ExternAudioFile v_audio = CreateExternAudioFile(p_url);
                TryScheduleAction(p_url, p_callback, true);
                TryCallAudioAction(v_audio, true);
                return v_audio;
            }
            else
            {
                TryScheduleAction(p_url, p_callback);
                if (!TryGetAudioFromDictionary(p_url))
                {
                    return ReloadAudioFromWebInternal(p_url, p_callback);
                }
            }
            return CreateExternAudioFile(p_url);
        }

        private static ExternAudioFile CreateExternAudioFile(string p_url)
        {
            if (p_url == null)
                p_url = "";
            ExternAudioFile v_audio = new ExternAudioFile();
            v_audio.Url = p_url;
            Object v_asset = null;
            AssetDictionary.TryGetValue(p_url, out v_asset);
            AudioClip v_audioClip = v_asset as AudioClip;

            v_audio.Clip = v_audioClip;
            v_audio.Status = AsyncStatusEnum.Done;
            if (v_audio.Clip != null)
            {
                v_audio.Error = null;
            }
            else
            {
                v_audio.Error = "Audio can't be loaded!";
            }
            return v_audio;
        }

        private static ExternAudioFile ReloadAudioFromWebInternal(string p_url, System.Delegate p_callback)
        {
            if (string.IsNullOrEmpty(p_url))
            {
                ExternAudioFile v_callback = CreateExternAudioFile(p_url);
                TryScheduleAction(p_url, p_callback, true);
                TryCallAudioAction(v_callback, true);
                return v_callback;
            }
            else
            {
                TryScheduleAction(p_url, p_callback);
                var v_delegate = new System.Action<ExternAudioFile>(HandleOnAudioReceived);
                var v_customDownloader = GetCustomAudioDownloader(p_url);
                if (v_customDownloader != null)
                    return v_customDownloader(p_url, v_delegate);
                else
                    return AudioSerializer.DeserializeFromWeb(p_url, v_delegate);
            }
        }

        private static void UnloadAudioInternal(string p_url, bool p_immediate = false, UnloadMode p_mode = UnloadMode.DestroyIfNeeded)
        {
            if (p_url == null)
                p_url = "";
            if (AssetDictionary.ContainsKey(p_url))
            {
                Object v_asset = null;
                AssetDictionary.TryGetValue(p_url, out v_asset);
                AudioClip v_audioClip = v_asset as AudioClip;

                AssetDictionary.Remove(p_url);
                if (p_mode != UnloadMode.SkipDestroyStep)
                {
                    try
                    {
                        var v_canDestroySprite = p_mode == UnloadMode.ForceDestroy || string.IsNullOrEmpty(TryFindKeyWithAudio(v_audioClip, false));
                        if (v_audioClip != null && v_canDestroySprite)
                        {
                            if (p_immediate)
                                Object.DestroyImmediate(v_audioClip);
                            else
                                DestroyUtils.DestroyImmediate(v_audioClip);
                        }
                    }
                    catch { }
                }

            }
            AudioDownloader v_downloader = AudioDownloader.GetDownloader(p_url);
            if (v_downloader != null)
                RequestStackManager.StopAllRequestsFromSender(v_downloader);
        }

        private static bool TryGetAudioFromDictionary(string p_url)
        {
            if (p_url == null)
                p_url = "";
            RemoveKeysWithNullValuesInDictionary(AssetDictionary);
            if (AssetDictionary.ContainsKey(p_url))
            {
                Object v_asset = null;
                AssetDictionary.TryGetValue(p_url, out v_asset);
                AudioClip v_audioClip = v_asset as AudioClip;
                ExternAudioFile v_callback = new ExternAudioFile();
                v_callback.Clip = v_audioClip;
                v_callback.Url = p_url;
                TryCallAudioAction(v_callback, false);
                return true;
            }
            return false;
        }

        private static void HandleOnAudioReceived(ExternAudioFile p_audio)
        {
            if (p_audio != null && p_audio.Clip != null && !string.IsNullOrEmpty(p_audio.Url) && p_audio.Error == null)
            {
                Object v_asset = null;
                AssetDictionary.TryGetValue(p_audio.Url, out v_asset);
                AudioClip v_audioClip = v_asset as AudioClip;
                //Destroy Previous Audio before replace
                if (v_audioClip != null)
                {
                    if (v_audioClip != null && v_audioClip != p_audio.Clip && TryFindKeyWithAudio(v_audioClip).Length <= 1)
                        DestroyUtils.DestroyImmediate(v_audioClip);
                }
                //Save in Dictionary
                AssetDictionary[p_audio.Url] = p_audio.Clip;
            }
            else if (p_audio != null && p_audio.Error != null) //Try Load From PlayerPrefs
            {
                if (AssetDictionary.ContainsKey(p_audio.Url))
                {
                    Object v_asset = null;
                    AssetDictionary.TryGetValue(p_audio.Url, out v_asset);
                    AudioClip v_audioClip = v_asset as AudioClip;

                    p_audio.Clip = v_audioClip;
                }
            }
            if (OnAudioLoaded != null)
                OnAudioLoaded(p_audio);
            TryCallAudioAction(p_audio, false);
        }

        #endregion

        #region Image Cacher Internal

        private static ExternImgFile AddSpriteIntoCache_Internal(string p_key, Sprite p_sprite)
        {
            //Try avoid add same sprite twice
            Object v_asset = null;
            Sprite v_loadedSprite = null;
            AssetDictionary.TryGetValue(p_key, out v_asset);
            v_loadedSprite = v_asset as Sprite;

            if (v_loadedSprite != p_sprite)
            {
                UnloadImageInternal(p_key, false, UnloadMode.SkipDestroyStep);
                //Try destroy previous sprite and textures
                var v_texture = v_loadedSprite != null ? v_loadedSprite.texture : null;
                if (v_loadedSprite != null && v_loadedSprite != p_sprite && string.IsNullOrEmpty(TryFindKeyWithSprite(v_loadedSprite)))
                    DestroyUtils.DestroyImmediate(v_loadedSprite);
                if (v_texture != null && (p_sprite == null || v_texture != p_sprite.texture) && string.IsNullOrEmpty(TryFindKeyWithTexture(v_texture)))
                    DestroyUtils.DestroyImmediate(v_texture);
            }
            AssetDictionary[p_key] = p_sprite;

            ExternImgFile v_externImage = new ExternImgFile();
            v_externImage.Url = p_key;
            v_externImage.Sprite = p_sprite;
            v_externImage.Status = AsyncStatusEnum.Done;
            HandleOnImageReceived(v_externImage);
            return v_externImage;
        }

        /// <summary>
        /// Try get Cached imaged in Dictionary or in PlayerPrefs. If not loaded, the function will try load from web and cache in Dictionary. 
        /// This functions will try return image loaded in dictionary immediately
        /// </summary>
        private static Sprite GetCachedSprite(string p_url)
        {
            Object v_asset = null;
            if (!AssetDictionary.ContainsKey(p_url))
                GetSprite(p_url, null);
            AssetDictionary.TryGetValue(p_url, out v_asset);
            Sprite v_sprite = v_asset as Sprite;

            return v_sprite;
        }

        private static Texture2D GetCachedTexture(string p_url)
        {
            Sprite v_sprite = GetCachedSprite(p_url);
            if (v_sprite != null)
                return v_sprite.texture;
            return null;
        }

        private static ExternImgFile GetSprite(string p_url, System.Action<Sprite> p_callback)
        {
            return GetImageInternal(p_url, p_callback);
        }

        private static ExternImgFile GetTexture(string p_url, System.Action<Texture2D> p_callback)
        {
            return GetImageInternal(p_url, p_callback);
        }

        /// <summary>
        /// Gets the image from Dictionary (If loaded), or from PlayerPrefs (If saved in prefs), or try load from Web (If not Loaded)
        /// This functions will return the Sprite Loaded in Function Callback (not immediately)
        /// </summary>
        private static ExternImgFile GetImageInternal(string p_url, System.Delegate p_callback)
        {
            if (string.IsNullOrEmpty(p_url))
            {
                ExternImgFile v_image = CreateExternImgFile(p_url);
                TryScheduleAction(p_url, p_callback, true);
                TryCallImageAction(v_image, true);
                return v_image;
            }
            else
            {
                TryScheduleAction(p_url, p_callback);
                if (!TryGetImageFromDictionary(p_url))
                {
                    return ReloadImageFromWebInternal(p_url, p_callback);
                }
            }
            return CreateExternImgFile(p_url);
        }

        private static ExternImgFile CreateExternImgFile(string p_url)
        {
            if (p_url == null)
                p_url = "";
            ExternImgFile v_image = new ExternImgFile();
            v_image.Url = p_url;
            Object v_asset = null;
            AssetDictionary.TryGetValue(p_url, out v_asset);
            Sprite v_sprite = v_asset as Sprite;

            v_image.Sprite = v_sprite;
            v_image.Status = AsyncStatusEnum.Done;
            if (v_image.Sprite != null)
            {
                v_image.Texture = v_image.Sprite.texture;
                v_image.Error = null;
            }
            else
            {
                v_image.Error = "Image can't be loaded!";
            }
            return v_image;
        }

        private static ExternImgFile ReloadImageFromWebInternal(string p_url, System.Delegate p_callback)
        {
            if (string.IsNullOrEmpty(p_url))
            {
                ExternImgFile v_callback = CreateExternImgFile(p_url);
                TryScheduleAction(p_url, p_callback, true);
                TryCallImageAction(v_callback, true);
                return v_callback;
            }
            else
            {
                TryScheduleAction(p_url, p_callback);
                var v_delegate = new System.Action<ExternImgFile>(HandleOnImageReceived);
                var v_customDownloader = GetCustomImgDownloader(p_url);
                if(v_customDownloader != null)
                    return v_customDownloader(p_url, v_delegate);
                else
                    return TextureSerializer.DeserializeFromWeb(p_url, v_delegate);
            }
        }

        enum UnloadMode { DestroyIfNeeded, SkipDestroyStep, ForceDestroy }
        private static void UnloadImageInternal(string p_url, bool p_immediate = false, UnloadMode p_mode = UnloadMode.DestroyIfNeeded)
        {
            if (p_url == null)
                p_url = "";
            if (AssetDictionary.ContainsKey(p_url))
            {
                Object v_asset = null;
                AssetDictionary.TryGetValue(p_url, out v_asset);
                Sprite v_sprite = v_asset as Sprite;
                Texture2D v_texture = null;

                if (v_sprite != null)
                    v_texture = v_sprite.texture;

                AssetDictionary.Remove(p_url);
                if (p_mode != UnloadMode.SkipDestroyStep)
                {
                    try
                    {
                        //If found a second key in resources cache, we cant destroy this sprite because it is used in other cached url
                        var v_canDestroyTexture = p_mode == UnloadMode.ForceDestroy || string.IsNullOrEmpty(TryFindKeyWithTexture(v_texture, false));
                        if (v_texture != null && v_canDestroyTexture)
                        {
                            if (p_immediate)
                                Object.DestroyImmediate(v_texture);
                            else
                                DestroyUtils.DestroyImmediate(v_texture);
                        }
                        var v_canDestroySprite = p_mode == UnloadMode.ForceDestroy || string.IsNullOrEmpty(TryFindKeyWithSprite(v_sprite, false));
                        if (v_sprite != null && v_canDestroySprite)
                        {
                            if (p_immediate)
                                Object.DestroyImmediate(v_sprite);
                            else
                                DestroyUtils.DestroyImmediate(v_sprite);
                        }
                    }
                    catch { }
                }

            }
            TextureDownloader v_downloader = TextureDownloader.GetDownloader(p_url);
            if (v_downloader != null)
                RequestStackManager.StopAllRequestsFromSender(v_downloader);
        }

        private static bool TryGetImageFromDictionary(string p_url)
        {
            if (p_url == null)
                p_url = "";
            RemoveKeysWithNullValuesInDictionary(AssetDictionary);
            if (AssetDictionary.ContainsKey(p_url))
            {
                Object v_asset = null;
                AssetDictionary.TryGetValue(p_url, out v_asset);
                Sprite v_sprite = v_asset as Sprite;
                Texture2D v_texture = v_sprite != null ? v_sprite.texture : null;
                ExternImgFile v_callback = new ExternImgFile();
                v_callback.Sprite = v_sprite;
                v_callback.Texture = v_texture;
                v_callback.Url = p_url;
                TryCallImageAction(v_callback, false);
                return true;
            }
            return false;
        }

        private static void HandleOnImageReceived(ExternImgFile p_image)
        {
            if (p_image != null && p_image.Sprite != null && !string.IsNullOrEmpty(p_image.Url) && p_image.Error == null)
            {
                Object v_asset = null;
                AssetDictionary.TryGetValue(p_image.Url, out v_asset);
                Sprite v_sprite = v_asset as Sprite;
                //Destroy Previous Sprite before replace
                if (v_sprite != null)
                {
                    var v_texture = v_sprite != null ? v_sprite.texture : null;
                    if (v_sprite != null && v_sprite != p_image.Sprite && TryFindAllKeysWithSprite(v_sprite).Length <= 1)
                        DestroyUtils.DestroyImmediate(v_sprite);
                    if (v_texture != null && v_texture != p_image.Sprite.texture && TryFindAllKeysWithTexture(v_texture).Length <= 1)
                        DestroyUtils.DestroyImmediate(v_texture);
                }
                //Save in Dictionary
                AssetDictionary[p_image.Url] = p_image.Sprite;
            }
            else if (p_image != null && p_image.Error != null) //Try Load From PlayerPrefs
            {
                if (AssetDictionary.ContainsKey(p_image.Url))
                {
                    Object v_asset = null;
                    AssetDictionary.TryGetValue(p_image.Url, out v_asset);
                    Sprite v_sprite = v_asset as Sprite;

                    p_image.Sprite = v_sprite;
                    p_image.Texture = p_image.Sprite != null ? p_image.Sprite.texture : null;
                }
            }
            if (OnImageLoaded != null)
                OnImageLoaded(p_image);
            TryCallImageAction(p_image, false);
        }

        #endregion

        #region Public Helper Functions

        public static bool IsLoaded(string p_key)
        {
            return !string.IsNullOrEmpty(p_key) && AssetDictionary.ContainsKey(p_key);
        }

        public static bool IsDownloading(string p_key)
        {
            return !string.IsNullOrEmpty(p_key) && PendentActions.ContainsKey(p_key);
        }

        public static bool IsDownloadingAnyResource()
        {
            return PendentActions.Count > 0;
        }

        /// <summary>
        /// Try find a key with respective audio (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string TryFindKeyWithAudio(AudioClip p_audioClip, bool p_acceptNull = false)
        {
            if (p_audioClip != null || p_acceptNull)
            {
                foreach (var v_pair in s_assetDictionary)
                {
                    if (v_pair.Value == p_audioClip)
                        return v_pair.Key;
                }
            }
            return null;

        }

        /// <summary>
        /// Try find a key with respective sprite (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string TryFindKeyWithSprite(Sprite p_sprite, bool p_acceptNull = false)
        {
            if (p_sprite != null || p_acceptNull)
            {
                foreach (var v_pair in s_assetDictionary)
                {
                    if (v_pair.Value == p_sprite)
                        return v_pair.Key;
                }
            }
            return null;

        }

        /// <summary>
        /// Try find a key with respective texture (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string TryFindKeyWithTexture(Texture2D p_texture, bool p_acceptNull = false)
        {
            if (p_texture != null || p_acceptNull)
            {
                foreach (var v_pair in s_assetDictionary)
                {
                    var v_sprite = v_pair.Value as Sprite;
                    if (v_sprite != null && v_sprite.texture == p_texture)
                        return v_pair.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Try find a key with respective texture (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string[] TryFindAllKeysWithTexture(Texture2D p_texture, bool p_acceptNull = false)
        {
            List<string> v_keys = new List<string>();
            if (p_texture != null || p_acceptNull)
            {
                foreach (var v_pair in s_assetDictionary)
                {
                    var v_sprite = v_pair.Value as Sprite;
                    if (v_sprite != null && v_sprite.texture == p_texture)
                        v_keys.Add(v_pair.Key);
                }
            }
            return v_keys.ToArray();
        }

        /// <summary>
        /// Try find a key with respective texture (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string[] TryFindAllKeysWithSprite(Sprite p_sprite, bool p_acceptNull = false)
        {
            List<string> v_keys = new List<string>();
            if (p_sprite != null || p_acceptNull)
            {
                foreach (var v_pair in s_assetDictionary)
                {
                    if (v_pair.Value == p_sprite)
                        v_keys.Add(v_pair.Key);
                }
            }
            return v_keys.ToArray();
        }

        #endregion

        #region Internal Helper Functions

        static CustomAudioUrlDownloader GetCustomAudioDownloader(string p_url)
        {
            CustomAudioUrlDownloader v_downloader = null;
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
            foreach (var v_pair in CustomAudioDownloaderPatterns)
            {
                var v_pattern = v_pair.Key;
                if (Regex.IsMatch(p_url, v_pattern))
                {
                    v_downloader = v_pair.Value;
                    break;
                }
            }
            return v_downloader;
        }

        static CustomImgUrlDownloader GetCustomImgDownloader(string p_url)
        {
            CustomImgUrlDownloader v_downloader = null;
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
            foreach (var v_pair in CustomImgDownloaderPatterns)
            {
                var v_pattern = v_pair.Key;
                if (Regex.IsMatch(p_url, v_pattern))
                {
                    v_downloader = v_pair.Value;
                    break;
                }
            }
            return v_downloader;
        }

        private static void TryScheduleAction(string p_url, System.Delegate p_callback, bool p_acceptEmptyUrls = false)
        {
            if (p_url == null)
                p_url = "";
            if (p_callback != null && p_url != null && (p_acceptEmptyUrls || !string.IsNullOrEmpty(p_url)))
            {
                bool v_canSchedule = true;
                foreach (var p_pair in PendentActions)
                {
                    if (string.Equals(p_pair.Key, p_url) && p_callback == p_pair.Value)
                        v_canSchedule = false;
                }
                if (v_canSchedule)
                    PendentActions.Add(p_url, p_callback);
            }

        }

        private static void TryCallAudioAction(ExternAudioFile p_param, bool p_acceptEmptyUrls = false)
        {
            if (p_param != null)
            {
                TryCallActionInternal(p_param.Url, p_param.Clip, false, typeof(AudioClip));
                TryCallActionInternal(p_param.Url, p_param, false, typeof(ExternAudioFile));
            }
        }

        private static void TryCallImageAction(ExternImgFile p_param, bool p_acceptEmptyUrls = false)
        {
            if (p_param != null)
            {
                TryCallActionInternal(p_param.Url, p_param.Sprite, false, typeof(Sprite));
                TryCallActionInternal(p_param.Url, p_param.Texture, false, typeof(Texture2D));
                TryCallActionInternal(p_param.Url, p_param, false, typeof(ExternImgFile));
            }
        }

        private static void TryCallActionInternal(string p_url, object p_param, bool p_acceptEmptyUrls = false, System.Type p_delegateParameterFilter = null)
        {
            if (p_url == null)
                p_url = "";
            if (p_url != null && (p_acceptEmptyUrls || !string.IsNullOrEmpty(p_url)))
            {
                ArrayDict<string, System.Delegate> v_updatedPendentActions = new ArrayDict<string, System.Delegate>();
                foreach (var p_pair in PendentActions)
                {
                    if (p_pair != null && p_pair.Key != null && (p_acceptEmptyUrls || !string.IsNullOrEmpty(p_pair.Key)) && p_pair.Value != null)
                    {
                        if (string.Equals(p_pair.Key, p_url))
                        {
                            FunctionAndParams v_func = new FunctionAndParams();
                            v_func.DelegatePointer = p_pair.Value;
                            v_func.Params.Add(p_param);
                            var v_params = v_func.GetFunctionParameterTypes();
                            System.Type v_paramType = v_params.Length == 1 ? v_params[0] : null;
                            bool v_hasCorrectFilterType = p_delegateParameterFilter == null || v_func.DelegatePointer == null|| Kyub.Extensions.TypeExtensions.IsSameOrSubClassOrImplementInterface(v_paramType, p_delegateParameterFilter);
                            if (!v_hasCorrectFilterType/*|| !v_func.CallFunction()*/)
                            {
                                v_updatedPendentActions.Add(p_pair); //Wrong Parameters, We must Call it with diff Params
                            }
                        }
                        else
                        {
                            v_updatedPendentActions.Add(p_pair);
                        }
                    }
                }
                PendentActions = v_updatedPendentActions;
            }
        }

        #endregion
    }
}
