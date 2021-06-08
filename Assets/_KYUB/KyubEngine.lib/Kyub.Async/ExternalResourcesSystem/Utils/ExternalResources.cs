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

        public delegate ExternImgFile CustomImgUrlDownloader(string url, System.Action<ExternImgFile> callback);
        public delegate ExternAudioFile CustomAudioUrlDownloader(string url, System.Action<ExternAudioFile> callback);

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
                                texture = CreateTextureClone(originalTexture);
                                if(texture != null)
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

        static void HandleOnLevelLoaded(Scene scene, LoadSceneMode mode)
        {
            UnloadUnusedAssetsImmediate();
        }

        #endregion

        #region Public Register Functions

        public static void RegisterReceiver(ExternalResourcesReceiver receiver)
        {
            UnregisterReceiver(receiver);
            if (receiver != null)
                ReferenceCounter.AddChecking(receiver.Key, receiver);
        }

        public static void UnregisterReceiver(ExternalResourcesReceiver receiver)
        {
            if (receiver != null)
                ReferenceCounter.RemoveChecking(receiver);
        }

        public static void RegisterCustomImgDownloaderPattern(string pattern, CustomImgUrlDownloader delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(pattern))
                CustomImgDownloaderPatterns.AddReplacing(pattern, delegateToCallWithThisPattern);
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void RegisterCustomAudioDownloaderPattern(string pattern, CustomAudioUrlDownloader delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(pattern))
                CustomAudioDownloaderPatterns.AddReplacing(pattern, delegateToCallWithThisPattern);
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void RegisterCustomImgDownloaderPattern(int index, string pattern, CustomImgUrlDownloader delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                CustomImgDownloaderPatterns.RemoveByKey(pattern);
                var pair = new Kyub.Collections.KVPair<string, CustomImgUrlDownloader>(pattern, delegateToCallWithThisPattern);
                if (index < 0 || index >= CustomImgDownloaderPatterns.Count)
                    CustomImgDownloaderPatterns.Add(pair);
                else
                    CustomImgDownloaderPatterns.Insert(index, pair);
            }
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void RegisterCustomAudioDownloaderPattern(int index, string pattern, CustomAudioUrlDownloader delegateToCallWithThisPattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                CustomAudioDownloaderPatterns.RemoveByKey(pattern);
                var pair = new Kyub.Collections.KVPair<string, CustomAudioUrlDownloader>(pattern, delegateToCallWithThisPattern);
                if (index < 0 || index >= CustomAudioDownloaderPatterns.Count)
                    CustomAudioDownloaderPatterns.Add(pair);
                else
                    CustomAudioDownloaderPatterns.Insert(index, pair);
            }
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void UnregisterCustomImgDownloaderPattern(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
                CustomImgDownloaderPatterns.RemoveByKey(pattern);
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        public static void UnregisterCustomAudioDownloaderPattern(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
                CustomAudioDownloaderPatterns.RemoveByKey(pattern);
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
        }

        #endregion

        #region Public Audio Functions

        public static ExternAudioFile AddAudioIntoCache(ExternAudioFile audio)
        {
            AudioClip audioClip = audio != null ? audio.Clip : null;
            return AddAudioIntoCache(audio != null ? audio.Url : "", audioClip);
        }

        public static ExternAudioFile AddAudioIntoCache(string key, AudioClip audio)
        {
            //Try avoid add same sprite twice
            bool keyIsNull = string.IsNullOrEmpty(key);
            if (!keyIsNull &&
                (string.IsNullOrEmpty(TryFindKeyWithAudio(audio, false)))
               )
            {
                return AddClipIntoCache_Internal(key, audio);
            }
            else
            {
                if (keyIsNull)
                    Debug.LogWarning("Trying to add an empty key into ExternalResources cache");
                else
                    Debug.LogWarning("Trying to add a duplicated Audio into ExternalResources cache");
            }
            return null;
        }
        
        public static ExternAudioFile ReloadAudioAsync(string key, System.Action<ExternAudioFile> callback = null)
        {
            return ReloadAudioFromWebInternal(key, callback);
        }

        public static ExternAudioFile ReloadClipAsync(string key, System.Action<AudioClip> callback = null)
        {
            return ReloadAudioFromWebInternal(key, callback);
        }

        public static ExternAudioFile LoadAudioAsync(string key, System.Action<ExternAudioFile> callback = null)
        {
            return GetAudioInternal(key, callback);
        }

        public static ExternAudioFile LoadClipAsync(string key, System.Action<AudioClip> callback = null)
        {
            return GetAudioInternal(key, callback);
        }

        public static AudioClip LoadClipFromCache(string key)
        {
            return GetCachedClip(key);
        }

        #endregion

        #region Public Image Functions

        public static ExternImgFile AddImageIntoCache(ExternImgFile image, bool acceptMultipleKeysWithSameTexture = true)
        {
            Sprite sprite = image != null ? image.Sprite : null;
            return AddSpriteIntoCache(image != null ? image.Url : "", sprite, acceptMultipleKeysWithSameTexture);
        }

        public static ExternImgFile AddSpriteIntoCache(string key, Sprite sprite, bool acceptMultipleKeysWithSameTexture = true)
        {
            //Try avoid add same sprite twice
            bool keyIsNull = string.IsNullOrEmpty(key);
            if (!keyIsNull && 
                (acceptMultipleKeysWithSameTexture || string.IsNullOrEmpty(TryFindKeyWithSprite(sprite, false))) 
               )
            {
                return AddSpriteIntoCache_Internal(key, sprite);
            }
            else
            {
                if (keyIsNull)
                    Debug.LogWarning("Trying to add an empty key into ExternalResources cache");
                else
                    Debug.LogWarning("Trying to add a duplicated Sprite into ExternalResources cache");
            }
            return null;
        }

        public static ExternImgFile AddTextureIntoCache(string key, Texture2D texture, bool acceptMultipleKeysWithSameTexture = true)
        {
            //Try avoid add same sprite twice
            bool keyIsNull = string.IsNullOrEmpty(key);
            if (!keyIsNull && 
                (acceptMultipleKeysWithSameTexture || string.IsNullOrEmpty(TryFindKeyWithTexture(texture, false))) 
               )
            {
                Sprite sprite = texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f)) : null;
                return AddSpriteIntoCache_Internal(key, sprite);
            }
            else
            {
                if (keyIsNull)
                    Debug.LogWarning("Trying to add an empty key into ExternalResources cache");
                else
                    Debug.LogWarning("Trying to add a duplicated Texture2D into ExternalResources cache");
            }
            return null;
        }

        public static ExternImgFile AddTextureDataIntoCache(string key, byte[] textureData, bool acceptMultipleKeysWithSameTexture = true)
        {
            if (!string.IsNullOrEmpty(key))
            {
                Texture2D texture = new Texture2D(4, 4);
                if (!texture.LoadImage(textureData))
                {
                    if (texture != null)
                        Object.DestroyImmediate(texture);
                    texture = null;
                }
                return AddTextureIntoCache(key, texture, acceptMultipleKeysWithSameTexture);
            }
            return null;
        }

        public static ExternImgFile ReloadImageAsync(string key, System.Action<ExternImgFile> callback = null)
        {
            return ReloadImageFromWebInternal(key, callback);
        }

        public static ExternImgFile ReloadSpriteAsync(string key, System.Action<Sprite> callback = null)
        {
            return ReloadImageFromWebInternal(key, callback);
        }

        public static ExternImgFile ReloadTextureAsync(string key, System.Action<Texture2D> callback = null)
        {
            return ReloadImageFromWebInternal(key, callback);
        }

        public static ExternImgFile LoadImageAsync(string key, System.Action<ExternImgFile> callback = null)
        {
            return GetImageInternal(key, callback);
        }

        public static ExternImgFile LoadSpriteAsync(string key, System.Action<Sprite> callback = null)
        {
            return GetImageInternal(key, callback);
        }

        public static ExternImgFile LoadTextureAsync(string key, System.Action<Texture2D> callback = null)
        {
            return GetImageInternal(key, callback);
        }

        public static Sprite LoadSpriteFromCache(string key)
        {
            return GetCachedSprite(key);
        }

        public static Texture2D LoadTextureFromCache(string key)
        {
            return GetCachedTexture(key);
        }

        #endregion

        #region Public Asset Functions

        public static void UnloadAsset(string key, bool immediate = false)
        {
            UnloadAssetInternal(key, immediate);
        }

        public static void UnloadAssets(IEnumerable<string> keys, bool immediate = false)
        {
            if (keys == null)
                keys = new List<string>();
            HashSet<string> keysToUnload = new HashSet<string>();
            HashSet<Object> elementsToUnload = new HashSet<Object>();

            foreach (var key in keys)
            {
                if (!keysToUnload.Contains(key) && s_assetDictionary.ContainsKey(key))
                {
                    keysToUnload.Add(key);
                    var asset = AssetDictionary[key];
                    if (asset == null)
                        continue;

                    if (!elementsToUnload.Contains(asset))
                        elementsToUnload.Add(asset);

                    //Fill sprites that we must try destroy
                    var sprite = asset as Sprite;
                    if (sprite != null)
                    {
                        var texture = sprite.texture;
                        if (texture != null && !elementsToUnload.Contains(texture))
                            elementsToUnload.Add(texture);
                    }

                }
            }

            //We must skip destroy step to destroy everything in same loop (optimize find)
            foreach (string key in keysToUnload)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    UnloadAssetInternal(key, false, UnloadMode.SkipDestroyStep);
                }
            }

            //Reset to current dictionary keys
            var currentDictKeys = new List<string>(AssetDictionary.Keys);
            //Remove elements that keep reference in dictionary of images (two keys with same texture or sprite)
            foreach (var key in currentDictKeys)
            {
                var asset = AssetDictionary[key];
                if (asset == null)
                    continue;

                elementsToUnload.Remove(asset);

                var sprite = asset as Sprite;
                if (sprite != null)
                {
                    var texture = sprite.texture;
                    if (texture != null)
                        elementsToUnload.Remove(texture);
                }
            }
            //Destroy Sprite and Texture
            List<Object> elementsToDestroy = new List<Object>(elementsToUnload);
            elementsToUnload.Clear();
            for (int i = 0; i < elementsToDestroy.Count; i++)
            {
                if (elementsToDestroy[i] != null)
                {
                    if (immediate)
                        Object.DestroyImmediate(elementsToDestroy[i]);
                    else
                        DestroyUtils.DestroyImmediate(elementsToDestroy[i]);
                }
            }
            elementsToDestroy.Clear();
            RemoveKeysWithNullValuesInDictionary(AssetDictionary);
        }

        static bool _isUloadingAssets = false;
        public static void UnloadUnusedAssets()
        {
            if (!_isUloadingAssets)
            {
                _isUloadingAssets = true;
                ApplicationContext.RunOnMainThread(UnloadUnusedAssetsImmediate, 0.1f);
            }
        }

        public static bool IsUselessResources(string key)
        {
            ReferenceCounter.RemovePairsWithNullValues();
            return !ReferenceCounter.ContainsKey(key);
        }

        #endregion

        #region Internal Resources Functions

        private static void UnloadAssetInternal(string url, bool immediate = false, UnloadMode mode = UnloadMode.DestroyIfNeeded)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (AssetDictionary.ContainsKey(url) && AssetDictionary[url] is AudioClip)
                    UnloadAudioInternal(url, false, UnloadMode.SkipDestroyStep);
                else
                    UnloadImageInternal(url, false, UnloadMode.SkipDestroyStep);
            }
        }

        private static void UnloadUnusedAssetsImmediate()
        {
            HashSet<string> keysToUnload = new HashSet<string>();
            List<string> keys = new List<string>(AssetDictionary.Keys);
            foreach (var key in keys)
            {
                if (IsUselessResources(key) && !keysToUnload.Contains(key))
                    keysToUnload.Add(key);
            }
            UnloadAssets(keysToUnload, false);
            _isUloadingAssets = false;
        }

        private static void RemoveKeysWithNullValuesInDictionary<T>(Dictionary<string, T> dict) where T : UnityEngine.Object
        {
            List<string> keys = new List<string>(dict.Keys);
            foreach (var key in keys)
            {
                if (dict[key] == null)
                    dict.Remove(key);
            }
        }

        #endregion

        #region Audio Cacher Internal

        private static ExternAudioFile AddClipIntoCache_Internal(string key, AudioClip audioClip)
        {
            //Try avoid add same sprite twice
            Object asset = null;
            AudioClip loadedAudioClip = null;
            AssetDictionary.TryGetValue(key, out asset);
            loadedAudioClip = asset as AudioClip;

            if (loadedAudioClip != audioClip)
            {
                UnloadAudioInternal(key, false, UnloadMode.SkipDestroyStep);
                //Try destroy previous sprite and textures
                if (loadedAudioClip != null && loadedAudioClip != audioClip && string.IsNullOrEmpty(TryFindKeyWithAudio(loadedAudioClip)))
                    DestroyUtils.DestroyImmediate(loadedAudioClip);
            }
            AssetDictionary[key] = audioClip;

            ExternAudioFile externAudio = new ExternAudioFile();
            externAudio.Url = key;
            externAudio.Clip = audioClip;
            externAudio.Status = AsyncStatusEnum.Done;
            HandleOnAudioReceived(externAudio);
            return externAudio;
        }

        /// <summary>
        /// Try get Cached imaged in Dictionary or in PlayerPrefs. If not loaded, the function will try load from web and cache in Dictionary. 
        /// This functions will try return image loaded in dictionary immediately
        /// </summary>
        private static AudioClip GetCachedClip(string url)
        {
            Object asset = null;
            if (!AssetDictionary.ContainsKey(url))
                GetClip(url, null);
            AssetDictionary.TryGetValue(url, out asset);
            AudioClip audioClip = asset as AudioClip;

            return audioClip;
        }

        private static ExternAudioFile GetClip(string url, System.Action<AudioClip> callback)
        {
            return GetAudioInternal(url, callback);
        }

        /// <summary>
        /// Gets the image from Dictionary (If loaded), or from PlayerPrefs (If saved in prefs), or try load from Web (If not Loaded)
        /// This functions will return the Sprite Loaded in Function Callback (not immediately)
        /// </summary>
        private static ExternAudioFile GetAudioInternal(string url, System.Delegate callback)
        {
            if (string.IsNullOrEmpty(url))
            {
                ExternAudioFile audio = CreateExternAudioFile(url);
                TryScheduleAction(url, callback, true);
                TryCallAudioAction(audio, true);
                return audio;
            }
            else
            {
                TryScheduleAction(url, callback);
                if (!TryGetAudioFromDictionary(url))
                {
                    return ReloadAudioFromWebInternal(url, callback);
                }
            }
            return CreateExternAudioFile(url);
        }

        private static ExternAudioFile CreateExternAudioFile(string url)
        {
            if (url == null)
                url = "";
            ExternAudioFile audio = new ExternAudioFile();
            audio.Url = url;
            Object asset = null;
            AssetDictionary.TryGetValue(url, out asset);
            AudioClip audioClip = asset as AudioClip;

            audio.Clip = audioClip;
            audio.Status = AsyncStatusEnum.Done;
            if (audio.Clip != null)
            {
                audio.Error = null;
            }
            else
            {
                audio.Error = "Audio can't be loaded!";
            }
            return audio;
        }

        private static ExternAudioFile ReloadAudioFromWebInternal(string url, System.Delegate callback)
        {
            if (string.IsNullOrEmpty(url))
            {
                ExternAudioFile audio = CreateExternAudioFile(url);
                TryScheduleAction(url, callback, true);
                TryCallAudioAction(audio, true);
                return audio;
            }
            else
            {
                TryScheduleAction(url, callback);
                var action = new System.Action<ExternAudioFile>(HandleOnAudioReceived);
                var customDownloader = GetCustomAudioDownloader(url);
                if (customDownloader != null)
                    return customDownloader(url, action);
                else
                    return AudioSerializer.DeserializeFromWeb(url, action);
            }
        }

        private static void UnloadAudioInternal(string url, bool immediate = false, UnloadMode mode = UnloadMode.DestroyIfNeeded)
        {
            if (url == null)
                url = "";
            if (AssetDictionary.ContainsKey(url))
            {
                Object asset = null;
                AssetDictionary.TryGetValue(url, out asset);
                AudioClip audioClip = asset as AudioClip;

                AssetDictionary.Remove(url);
                if (mode != UnloadMode.SkipDestroyStep)
                {
                    try
                    {
                        var canDestroySprite = mode == UnloadMode.ForceDestroy || string.IsNullOrEmpty(TryFindKeyWithAudio(audioClip, false));
                        if (audioClip != null && canDestroySprite)
                        {
                            if (immediate)
                                Object.DestroyImmediate(audioClip);
                            else
                                DestroyUtils.DestroyImmediate(audioClip);
                        }
                    }
                    catch { }
                }

            }
            AudioDownloader downloader = AudioDownloader.GetDownloader(url);
            if (downloader != null)
                RequestStackManager.StopAllRequestsFromSender(downloader);
        }

        private static bool TryGetAudioFromDictionary(string url)
        {
            if (url == null)
                url = "";
            RemoveKeysWithNullValuesInDictionary(AssetDictionary);
            if (AssetDictionary.ContainsKey(url))
            {
                Object asset = null;
                AssetDictionary.TryGetValue(url, out asset);
                AudioClip audioClip = asset as AudioClip;
                ExternAudioFile callback = new ExternAudioFile();
                callback.Clip = audioClip;
                callback.Url = url;
                TryCallAudioAction(callback, false);
                return true;
            }
            return false;
        }

        private static void HandleOnAudioReceived(ExternAudioFile audio)
        {
            if (audio != null && audio.Clip != null && !string.IsNullOrEmpty(audio.Url) && audio.Error == null)
            {
                Object asset = null;
                AssetDictionary.TryGetValue(audio.Url, out asset);
                AudioClip audioClip = asset as AudioClip;
                //Destroy Previous Audio before replace
                if (audioClip != null)
                {
                    if (audioClip != null && audioClip != audio.Clip && TryFindKeyWithAudio(audioClip).Length <= 1)
                        DestroyUtils.DestroyImmediate(audioClip);
                }
                //Save in Dictionary
                AssetDictionary[audio.Url] = audio.Clip;
            }
            else if (audio != null && audio.Error != null) //Try Load From PlayerPrefs
            {
                if (AssetDictionary.ContainsKey(audio.Url))
                {
                    Object asset = null;
                    AssetDictionary.TryGetValue(audio.Url, out asset);
                    AudioClip audioClip = asset as AudioClip;

                    audio.Clip = audioClip;
                }
            }
            if (OnAudioLoaded != null)
                OnAudioLoaded(audio);
            TryCallAudioAction(audio, false);
        }

        #endregion

        #region Image Cacher Internal

        private static ExternImgFile AddSpriteIntoCache_Internal(string key, Sprite sprite)
        {
            //Try avoid add same sprite twice
            Object asset = null;
            Sprite loadedSprite = null;
            AssetDictionary.TryGetValue(key, out asset);
            loadedSprite = asset as Sprite;

            if (loadedSprite != sprite)
            {
                UnloadImageInternal(key, false, UnloadMode.SkipDestroyStep);
                //Try destroy previous sprite and textures
                var texture = loadedSprite != null ? loadedSprite.texture : null;
                if (loadedSprite != null && loadedSprite != sprite && string.IsNullOrEmpty(TryFindKeyWithSprite(loadedSprite)))
                    DestroyUtils.DestroyImmediate(loadedSprite);
                if (texture != null && (sprite == null || texture != sprite.texture) && string.IsNullOrEmpty(TryFindKeyWithTexture(texture)))
                    DestroyUtils.DestroyImmediate(texture);
            }
            AssetDictionary[key] = sprite;

            ExternImgFile externImage = new ExternImgFile();
            externImage.Url = key;
            externImage.Sprite = sprite;
            externImage.Status = AsyncStatusEnum.Done;
            HandleOnImageReceived(externImage);
            return externImage;
        }

        /// <summary>
        /// Try get Cached imaged in Dictionary or in PlayerPrefs. If not loaded, the function will try load from web and cache in Dictionary. 
        /// This functions will try return image loaded in dictionary immediately
        /// </summary>
        private static Sprite GetCachedSprite(string url)
        {
            Object asset = null;
            if (!AssetDictionary.ContainsKey(url))
                GetSprite(url, null);
            AssetDictionary.TryGetValue(url, out asset);
            Sprite sprite = asset as Sprite;

            return sprite;
        }

        private static Texture2D GetCachedTexture(string url)
        {
            Sprite sprite = GetCachedSprite(url);
            if (sprite != null)
                return sprite.texture;
            return null;
        }

        private static ExternImgFile GetSprite(string url, System.Action<Sprite> callback)
        {
            return GetImageInternal(url, callback);
        }

        private static ExternImgFile GetTexture(string url, System.Action<Texture2D> callback)
        {
            return GetImageInternal(url, callback);
        }

        /// <summary>
        /// Gets the image from Dictionary (If loaded), or from PlayerPrefs (If saved in prefs), or try load from Web (If not Loaded)
        /// This functions will return the Sprite Loaded in Function Callback (not immediately)
        /// </summary>
        private static ExternImgFile GetImageInternal(string url, System.Delegate callback)
        {
            if (string.IsNullOrEmpty(url))
            {
                ExternImgFile image = CreateExternImgFile(url);
                TryScheduleAction(url, callback, true);
                TryCallImageAction(image, true);
                return image;
            }
            else
            {
                TryScheduleAction(url, callback);
                if (!TryGetImageFromDictionary(url))
                {
                    return ReloadImageFromWebInternal(url, callback);
                }
            }
            return CreateExternImgFile(url);
        }

        private static ExternImgFile CreateExternImgFile(string url)
        {
            if (url == null)
                url = "";
            ExternImgFile image = new ExternImgFile();
            image.Url = url;
            Object asset = null;
            AssetDictionary.TryGetValue(url, out asset);
            Sprite sprite = asset as Sprite;

            image.Sprite = sprite;
            image.Status = AsyncStatusEnum.Done;
            if (image.Sprite != null)
            {
                image.Texture = image.Sprite.texture;
                image.Error = null;
            }
            else
            {
                image.Error = "Image can't be loaded!";
            }
            return image;
        }

        private static ExternImgFile ReloadImageFromWebInternal(string url, System.Delegate callback)
        {
            if (string.IsNullOrEmpty(url))
            {
                ExternImgFile img = CreateExternImgFile(url);
                TryScheduleAction(url, callback, true);
                TryCallImageAction(img, true);
                return img;
            }
            else
            {
                TryScheduleAction(url, callback);
                var action = new System.Action<ExternImgFile>(HandleOnImageReceived);
                var customDownloader = GetCustomImgDownloader(url);
                if(customDownloader != null)
                    return customDownloader(url, action);
                else
                    return TextureSerializer.DeserializeFromWeb(url, action);
            }
        }

        enum UnloadMode { DestroyIfNeeded, SkipDestroyStep, ForceDestroy }
        private static void UnloadImageInternal(string url, bool immediate = false, UnloadMode mode = UnloadMode.DestroyIfNeeded)
        {
            if (url == null)
                url = "";
            if (AssetDictionary.ContainsKey(url))
            {
                Object asset = null;
                AssetDictionary.TryGetValue(url, out asset);
                Sprite sprite = asset as Sprite;
                Texture2D texture = null;

                if (sprite != null)
                    texture = sprite.texture;

                AssetDictionary.Remove(url);
                if (mode != UnloadMode.SkipDestroyStep)
                {
                    try
                    {
                        //If found a second key in resources cache, we cant destroy this sprite because it is used in other cached url
                        var canDestroyTexture = mode == UnloadMode.ForceDestroy || string.IsNullOrEmpty(TryFindKeyWithTexture(texture, false));
                        if (texture != null && canDestroyTexture)
                        {
                            if (immediate)
                                Object.DestroyImmediate(texture);
                            else
                                DestroyUtils.DestroyImmediate(texture);
                        }
                        var canDestroySprite = mode == UnloadMode.ForceDestroy || string.IsNullOrEmpty(TryFindKeyWithSprite(sprite, false));
                        if (sprite != null && canDestroySprite)
                        {
                            if (immediate)
                                Object.DestroyImmediate(sprite);
                            else
                                DestroyUtils.DestroyImmediate(sprite);
                        }
                    }
                    catch { }
                }

            }
            TextureDownloader downloader = TextureDownloader.GetDownloader(url);
            if (downloader != null)
                RequestStackManager.StopAllRequestsFromSender(downloader);
        }

        private static bool TryGetImageFromDictionary(string url)
        {
            if (url == null)
                url = "";
            RemoveKeysWithNullValuesInDictionary(AssetDictionary);
            if (AssetDictionary.ContainsKey(url))
            {
                Object asset = null;
                AssetDictionary.TryGetValue(url, out asset);
                Sprite sprite = asset as Sprite;
                Texture2D texture = sprite != null ? sprite.texture : null;
                ExternImgFile callback = new ExternImgFile();
                callback.Sprite = sprite;
                callback.Texture = texture;
                callback.Url = url;
                TryCallImageAction(callback, false);
                return true;
            }
            return false;
        }

        private static void HandleOnImageReceived(ExternImgFile image)
        {
            if (image != null && image.Sprite != null && !string.IsNullOrEmpty(image.Url) && image.Error == null)
            {
                Object asset = null;
                AssetDictionary.TryGetValue(image.Url, out asset);
                Sprite sprite = asset as Sprite;
                //Destroy Previous Sprite before replace
                if (sprite != null)
                {
                    var texture = sprite != null ? sprite.texture : null;
                    if (sprite != null && sprite != image.Sprite && TryFindAllKeysWithSprite(sprite).Length <= 1)
                        DestroyUtils.DestroyImmediate(sprite);
                    if (texture != null && texture != image.Sprite.texture && TryFindAllKeysWithTexture(texture).Length <= 1)
                        DestroyUtils.DestroyImmediate(texture);
                }
                //Save in Dictionary
                AssetDictionary[image.Url] = image.Sprite;
            }
            else if (image != null && image.Error != null) //Try Load From PlayerPrefs
            {
                if (AssetDictionary.ContainsKey(image.Url))
                {
                    Object asset = null;
                    AssetDictionary.TryGetValue(image.Url, out asset);
                    Sprite sprite = asset as Sprite;

                    image.Sprite = sprite;
                    image.Texture = image.Sprite != null ? image.Sprite.texture : null;
                }
            }
            if (OnImageLoaded != null)
                OnImageLoaded(image);
            TryCallImageAction(image, false);
        }

        #endregion

        #region Public Helper Functions

        public static Texture2D CreateTextureClone(Texture2D source)
        {
            Texture2D readableText = null;
            
            if (source != null)
            {
                //Try efficient method
                if (!Application.isEditor && SystemInfo.copyTextureSupport != UnityEngine.Rendering.CopyTextureSupport.None)
                {
                    readableText = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1);
                    Graphics.CopyTexture(source, readableText);
                }
                //Clone original texture using default RawTextureData (only in Read/Write Texture)
                else if (!Application.isEditor && source.isReadable)
                {
                    readableText = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1);
                    var bytes = source.GetRawTextureData();
                    readableText.LoadRawTextureData(bytes);
                }
                //Fallback to Graphics.Blit Method
                else
                {
                    RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Default);

                    Graphics.Blit(source, renderTex);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = renderTex;

                    readableText = new Texture2D(source.width, source.height);
                    readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                    readableText.Apply();
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(renderTex);
                }
            }
            return readableText;
        }

        public static bool IsLoaded(string key)
        {
            return !string.IsNullOrEmpty(key) && AssetDictionary.ContainsKey(key);
        }

        public static bool IsDownloading(string key)
        {
            return !string.IsNullOrEmpty(key) && PendentActions.ContainsKey(key);
        }

        public static bool IsDownloadingAnyResource()
        {
            return PendentActions.Count > 0;
        }

        /// <summary>
        /// Try find a key with respective audio (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string TryFindKeyWithAudio(AudioClip audioClip, bool acceptNull = false)
        {
            if (audioClip != null || acceptNull)
            {
                foreach (var pair in s_assetDictionary)
                {
                    if (pair.Value == audioClip)
                        return pair.Key;
                }
            }
            return null;

        }

        /// <summary>
        /// Try find a key with respective sprite (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string TryFindKeyWithSprite(Sprite sprite, bool acceptNull = false)
        {
            if (sprite != null || acceptNull)
            {
                foreach (var pair in s_assetDictionary)
                {
                    if (pair.Value == sprite)
                        return pair.Key;
                }
            }
            return null;

        }

        /// <summary>
        /// Try find a key with respective texture (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string TryFindKeyWithTexture(Texture2D texture, bool acceptNull = false)
        {
            if (texture != null || acceptNull)
            {
                foreach (var pair in s_assetDictionary)
                {
                    var sprite = pair.Value as Sprite;
                    if (sprite != null && sprite.texture == texture)
                        return pair.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Try find a key with respective texture (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string[] TryFindAllKeysWithTexture(Texture2D texture, bool acceptNull = false)
        {
            List<string> keys = new List<string>();
            if (texture != null || acceptNull)
            {
                foreach (var pair in s_assetDictionary)
                {
                    var sprite = pair.Value as Sprite;
                    if (sprite != null && sprite.texture == texture)
                        keys.Add(pair.Key);
                }
            }
            return keys.ToArray();
        }

        /// <summary>
        /// Try find a key with respective texture (can search for null keys if second parameter is true
        /// </summary>
        /// <returns></returns>
        public static string[] TryFindAllKeysWithSprite(Sprite sprite, bool acceptNull = false)
        {
            List<string> keys = new List<string>();
            if (sprite != null || acceptNull)
            {
                foreach (var pair in s_assetDictionary)
                {
                    if (pair.Value == sprite)
                        keys.Add(pair.Key);
                }
            }
            return keys.ToArray();
        }

        #endregion

        #region Internal Helper Functions

        static CustomAudioUrlDownloader GetCustomAudioDownloader(string url)
        {
            CustomAudioUrlDownloader downloader = null;
            CustomAudioDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
            foreach (var pair in CustomAudioDownloaderPatterns)
            {
                var pattern = pair.Key;
                if (Regex.IsMatch(url, pattern))
                {
                    downloader = pair.Value;
                    break;
                }
            }
            return downloader;
        }

        static CustomImgUrlDownloader GetCustomImgDownloader(string url)
        {
            CustomImgUrlDownloader downloader = null;
            CustomImgDownloaderPatterns.RemovePairsWithNullValuesOrKeys();
            foreach (var pair in CustomImgDownloaderPatterns)
            {
                var pattern = pair.Key;
                if (Regex.IsMatch(url, pattern))
                {
                    downloader = pair.Value;
                    break;
                }
            }
            return downloader;
        }

        private static void TryScheduleAction(string url, System.Delegate callback, bool acceptEmptyUrls = false)
        {
            if (url == null)
                url = "";
            if (callback != null && url != null && (acceptEmptyUrls || !string.IsNullOrEmpty(url)))
            {
                bool canSchedule = true;
                foreach (var pair in PendentActions)
                {
                    if (string.Equals(pair.Key, url) && callback == pair.Value)
                        canSchedule = false;
                }
                if (canSchedule)
                    PendentActions.Add(url, callback);
            }

        }

        private static void TryCallAudioAction(ExternAudioFile param, bool acceptEmptyUrls = false)
        {
            if (param != null)
            {
                TryCallActionInternal(param.Url, param.Clip, false, typeof(AudioClip));
                TryCallActionInternal(param.Url, param, false, typeof(ExternAudioFile));
            }
        }

        private static void TryCallImageAction(ExternImgFile param, bool acceptEmptyUrls = false)
        {
            if (param != null)
            {
                TryCallActionInternal(param.Url, param.Sprite, false, typeof(Sprite));
                TryCallActionInternal(param.Url, param.Texture, false, typeof(Texture2D));
                TryCallActionInternal(param.Url, param, false, typeof(ExternImgFile));
            }
        }

        private static void TryCallActionInternal(string url, object param, bool acceptEmptyUrls = false, System.Type delegateParameterFilter = null)
        {
            if (url == null)
                url = "";
            if (url != null && (acceptEmptyUrls || !string.IsNullOrEmpty(url)))
            {
                ArrayDict<string, System.Delegate> updatedPendentActions = new ArrayDict<string, System.Delegate>();
                foreach (var pair in PendentActions)
                {
                    if (pair != null && pair.Key != null && (acceptEmptyUrls || !string.IsNullOrEmpty(pair.Key)) && pair.Value != null)
                    {
                        if (string.Equals(pair.Key, url))
                        {
                            FunctionAndParams func = new FunctionAndParams();
                            func.DelegatePointer = pair.Value;
                            func.Params.Add(param);
                            var funcParams = func.GetFunctionParameterTypes();
                            System.Type paramType = funcParams.Length == 1 ? funcParams[0] : null;
                            bool hasCorrectFilterType = delegateParameterFilter == null || func.DelegatePointer == null|| Kyub.Extensions.TypeExtensions.IsSameOrSubClassOrImplementInterface(paramType, delegateParameterFilter);
                            if (!hasCorrectFilterType/*|| !func.CallFunction()*/)
                            {
                                updatedPendentActions.Add(pair); //Wrong Parameters, We must Call it with diff Params
                            }
                        }
                        else
                        {
                            updatedPendentActions.Add(pair);
                        }
                    }
                }
                PendentActions = updatedPendentActions;
            }
        }

        #endregion
    }
}
