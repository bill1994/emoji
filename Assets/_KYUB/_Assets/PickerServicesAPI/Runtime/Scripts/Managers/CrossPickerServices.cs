using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Kyub.Async;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kyub.PickerServices
{
    public enum CaptureOptionEnum { Thumbnail = 0, FullScreen = 1 }

    [ExecuteInEditMode]
    public class CrossPickerServices : Singleton<CrossPickerServices>
    {
        public enum TextureEncoderEnum { JPG, PNG }

        #region Events

        public static System.Action<string[]> OnFilesPickerFinish;
        public static System.Action<ExternImgFile> OnPickerFinish;
        public static System.Action<string> OnImageSavedSucess;
        public static System.Action OnImageSavedFailed;

        #endregion

        #region Static Properties

        public static bool SaveCameraImageToGallery
        {
            get
            {
                if (Instance != null)
                    return Instance.m_saveCameraImageToGallery;
                return false;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_saveCameraImageToGallery = value;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(Instance);
#endif
                }
            }
        }

        public static int MaxImageLoadSize
        {
            get
            {
                if (Instance != null)
                    return Instance.m_maxImageLoadSize;
                return 2048;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_maxImageLoadSize = value;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(Instance);
#endif
                }
            }
        }

        public static CaptureOptionEnum CameraOption
        {
            get
            {
                if (Instance != null)
                    return Instance.m_cameraOption;
                return CaptureOptionEnum.Thumbnail;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_cameraOption = value;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(Instance);
#endif
                }
            }
        }

        public static TextureEncoderEnum EncodeOption
        {
            get
            {
                if (Instance != null)
                    return Instance.m_encodeOption;
                return TextureEncoderEnum.JPG;
            }
            set
            {
                if (Instance != null)
                {
                    Instance.m_encodeOption = value;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(Instance);
#endif
                }
            }
        }

        #endregion

        #region Event Caller

        protected internal static void CallImageSavedFailedEvent()
        {
            if (OnImageSavedFailed != null)
                OnImageSavedFailed();
        }

        protected internal static void CallImageSavedSucessEvent(string path)
        {
            if (OnImageSavedSucess != null)
                OnImageSavedSucess(path);
        }

        protected internal static void CallPickerFinishEvent(ExternImgFile file)
        {
            if (file != null)
                ExternalResources.AddImageIntoCache(file);

            if (OnPickerFinish != null)
                OnPickerFinish(file);
        }

        protected internal static void CallFilesPickerFinishEvent(string[] files)
        {
            if (OnFilesPickerFinish != null)
                OnFilesPickerFinish(files);
        }

        protected internal static void CallPickerFinishEvent(string key, Sprite spriteReturned)
        {
            ExternImgFile callback = null;
            if (spriteReturned != null && spriteReturned.texture != null)
            {
                callback = new ExternImgFile();
                callback.Error = "";
                callback.Status = Kyub.Async.AsyncStatusEnum.Done;
                callback.Texture = spriteReturned.texture;
                callback.Sprite = spriteReturned;
                callback.Url = callback.Url != null ? key : "";
            }
            CallPickerFinishEvent(callback);
        }

        protected internal static void CallPickerFinishEvent(string key, Texture2D textureReturned)
        {
            if (textureReturned != null && MaxImageLoadSize > 0)
                Kyub.PickerServices.Extensions.CrossPickerTexture2DExtensions.ClampSize(textureReturned, 4, MaxImageLoadSize);
            Sprite sprite = textureReturned != null ? Sprite.Create(textureReturned, new Rect(0, 0, textureReturned.width, textureReturned.height), new Vector2(0.5f, 0.5f)) : null;
            CallPickerFinishEvent(key, sprite);
        }

        #endregion

        #region Static Functions

        protected static void SetInitParemeters()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
		    AndroidPickerServices.Init();
#elif UNITY_IOS && !UNITY_EDITOR
		    IOSPickerServices.Init();
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.Init();
#else
            //SetWebInitParemeters();
#endif
        }

        public static void SerializeDataToAlbum(Texture2D image, System.Action<string> callback = null)
        {
            if (image != null)
            {
                SerializeDataToAlbum(image, image.name + "." + EncodeOption.ToString().ToLower(), callback);
            }
        }

        public static void SerializeDataToAlbum(Texture2D image, string name, System.Action<string> callback = null)
        {
            //AutoRegister/Unregister callback
            if (callback != null)
            {
                System.Action<string> internalSucessCallback = null;
                System.Action internalFailedCallback = null;

                internalSucessCallback = (result) =>
                {
                    if (callback != null)
                        callback.Invoke(result);
                    OnImageSavedFailed -= internalFailedCallback;
                    OnImageSavedSucess -= internalSucessCallback;
                };

                internalFailedCallback = () =>
                {
                    internalSucessCallback(null);
                };

                OnImageSavedSucess -= internalSucessCallback;
                OnImageSavedSucess += internalSucessCallback;

                OnImageSavedFailed -= internalFailedCallback;
                OnImageSavedFailed += internalFailedCallback;
            }

            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidPickerServices.SerializeDataToAlbum(image, name);
#elif UNITY_IOS && !UNITY_EDITOR
			IOSPickerServices.SerializeDataToAlbum(image, name);
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.SerializeDataToAlbum(image, name);
#else
            CallImageSavedFailedEvent();
#endif
        }

        public static void DeserializeAlbumImage(System.Action<ExternImgFile> callback = null)
        {
            //AutoRegister/Unregister callback
            if (callback != null)
            {
                System.Action<ExternImgFile> internalCallback = null;
                internalCallback = (result) =>
                {
                    if (callback != null)
                        callback.Invoke(result);
                    OnPickerFinish -= internalCallback;
                };
                OnPickerFinish -= internalCallback;
                OnPickerFinish += internalCallback;
            }

            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
		    AndroidPickerServices.DeserializeAlbumImage();
#elif UNITY_IOS && !UNITY_EDITOR
		    IOSPickerServices.DeserializeAlbumImage();
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.DeserializeAlbumImage();
#else
            CallPickerFinishEvent(null);
#endif
        }

        public static void DeserializeCameraImage(System.Action<ExternImgFile> callback = null)
        {
            DeserializeCameraImage(SaveCameraImageToGallery, callback);
        }

        public static void DeserializeCameraImage(bool saveToGallery, System.Action<ExternImgFile> callback = null)
        {
            //AutoRegister/Unregister callback
            if (callback != null)
            {
                System.Action<ExternImgFile> internalCallback = null;
                internalCallback = (result) =>
                {
                    if (callback != null)
                        callback.Invoke(result);
                    OnPickerFinish -= internalCallback;
                };
                OnPickerFinish -= internalCallback;
                OnPickerFinish += internalCallback;
            }

            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
		    AndroidPickerServices.DeserializeCameraImage(saveToGallery);
#elif UNITY_IOS && !UNITY_EDITOR
		    IOSPickerServices.DeserializeCameraImage(saveToGallery);
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.DeserializeCameraImage(saveToGallery);
#else
            CallPickerFinishEvent(null);
#endif
        }

        public static void OpenImageBrowser(bool multiselect, System.Action<string[]> callback = null)
        {
            //AutoRegister/Unregister callback
            if (callback != null)
            {
                System.Action<string[]> internalCallback = null;
                internalCallback = (result) =>
                {
                    if (callback != null)
                        callback.Invoke(result);
                    OnFilesPickerFinish -= internalCallback;
                };
                OnFilesPickerFinish -= internalCallback;
                OnFilesPickerFinish += internalCallback;
            }

            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
		    AndroidPickerServices.OpenImageBrowser(multiselect);
#elif UNITY_IOS && !UNITY_EDITOR
		    IOSPickerServices.OpenImageBrowser(multiselect);
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.OpenImageBrowser(multiselect);
#else
            CallFilesPickerFinishEvent(null);
#endif
        }

        public static void OpenFileBrowser(bool multiselect, System.Action<string[]> callback = null)
        {
            OpenFileBrowser(null, multiselect, callback);
        }

        public static void OpenFileBrowser(IList<string> allowedFileExtensions, bool multiselect, System.Action<string[]> callback = null)
        {
            //AutoRegister/Unregister callback
            if (callback != null)
            {
                System.Action<string[]> internalCallback = null;
                internalCallback = (result) =>
                {
                    if (callback != null)
                        callback.Invoke(result);
                    OnFilesPickerFinish -= internalCallback;
                };
                OnFilesPickerFinish -= internalCallback;
                OnFilesPickerFinish += internalCallback;
            }

            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
		    AndroidPickerServices.OpenFileBrowser(allowedFileExtensions, multiselect);
#elif UNITY_IOS && !UNITY_EDITOR
		    IOSPickerServices.OpenFileBrowser(allowedFileExtensions, multiselect);
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.OpenFileBrowser(allowedFileExtensions, multiselect);
#else
            CallFilesPickerFinishEvent(null);
#endif
        }

        #endregion

        #region Instance Functions/Properties

        #region Private Variables

        [Header("General Configurations")]
        [SerializeField]
        bool m_saveCameraImageToGallery = false;
        [SerializeField]
        int m_maxImageLoadSize = 2048;
        [SerializeField]
        CaptureOptionEnum m_cameraOption = CaptureOptionEnum.Thumbnail;
        [SerializeField]
        TextureEncoderEnum m_encodeOption = TextureEncoderEnum.JPG;

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            RegisterCustomGetFileFromPathPattern();
        }

        protected virtual void Start()
        {
            if (Application.isPlaying)
                CheckOtherServices();
            //This actions must be executed in editmode
            SetInitParemeters();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterCustomGetFileFromPathPattern();
        }

        #endregion

        #region Helper Functions

        public void CheckOtherServices()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
		    if(AndroidPickerServices.Instance != null) {}
#elif UNITY_IOS && !UNITY_EDITOR
		    if(IOSPickerServices.Instance != null) {}
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            if (StandalonePickerServices.Instance != null) { }
#else
            //if (WebPickerServices.Instance != null) { }
#endif
        }

        #endregion

        #endregion

        #region Helper Save Image Functions

        public static string SaveTextureToPersistentPath(Texture2D texture, string fileName = "")
        {
            return SaveTextureToPath_Internal(texture, Application.isEditor ? Application.temporaryCachePath : Application.persistentDataPath, fileName);
        }

        public static string SaveTextureToPersistentPath(byte[] bytes, string fileName = "")
        {
            return SaveTextureToPath_Internal(bytes, Application.isEditor ? Application.temporaryCachePath : Application.persistentDataPath, fileName);
        }

        public static string SaveTextureToTemporaryPath(Texture2D texture, string fileName = "")
        {
            return SaveTextureToPath_Internal(texture, Application.temporaryCachePath, fileName);
        }

        public static string SaveTextureToTemporaryPath(byte[] bytes, string fileName = "")
        {
            return SaveTextureToPath_Internal(bytes, Application.temporaryCachePath, fileName);
        }

        public static string GetUniqueImgFileName(CrossPickerServices.TextureEncoderEnum encodeOption)
        {
            return GetUniqueImgFileName(string.Empty, encodeOption);
        }

        public static string GetUniqueImgFileName(string extension)
        {
            return GetUniqueImgFileName(string.Empty, extension);
        }

        public static string GetUniqueImgFileName(string pattern, CrossPickerServices.TextureEncoderEnum encodeOption)
        {
            return GetUniqueImgFileName(pattern, "." + encodeOption.ToString().ToLower());
        }

        public static string GetUniqueImgFileName(string pattern, string extension)
        {
            if (string.IsNullOrEmpty(extension))
                extension = "." + CrossPickerServices.EncodeOption.ToString().ToLower();
            if (string.IsNullOrEmpty(pattern))
                pattern = "TempImg";
            return pattern + System.DateTime.UtcNow.ToString("ddMMyyyy").Replace(" ", string.Empty) + "_" + UnityEngine.Random.Range(0, 99999) + extension;
        }

        protected static string SaveTextureToPath_Internal(Texture2D texture, string folderPath, string fileName)
        {
            if (texture != null)
            {
                const string PNG_EXTENSION = ".png";
                const string JPG_EXTENSION = ".jpg";
                string extension;
                if (string.IsNullOrEmpty(fileName))
                    fileName = GetUniqueImgFileName(CrossPickerServices.EncodeOption);
                byte[] bytes = null;
                if (fileName.EndsWith(PNG_EXTENSION, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    extension = PNG_EXTENSION;
                    bytes = texture.EncodeToPNG();
                }
                else if (fileName.EndsWith(JPG_EXTENSION, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    extension = JPG_EXTENSION;
                    bytes = texture.EncodeToJPG();
                }
                else if (CrossPickerServices.EncodeOption == CrossPickerServices.TextureEncoderEnum.JPG)
                {
                    extension = JPG_EXTENSION;
                    bytes = texture.EncodeToJPG();
                    fileName += extension;

                }
                else
                {
                    extension = PNG_EXTENSION;
                    bytes = texture.EncodeToPNG();
                    fileName += extension;
                }
                return SaveTextureToPath_ExtensionProcessing_Internal(bytes, folderPath, fileName, extension);
            }
            return string.Empty;
        }

        protected static string SaveTextureToPath_Internal(byte[] bytes, string folderPath, string fileName)
        {
            if (bytes != null && bytes.Length > 0)
            {
                const string PNG_EXTENSION = ".png";
                const string JPG_EXTENSION = ".jpg";

                string extension;
                if (string.IsNullOrEmpty(fileName))
                    fileName = GetUniqueImgFileName(CrossPickerServices.EncodeOption);
                if (fileName.EndsWith(PNG_EXTENSION, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    extension = PNG_EXTENSION;
                }
                else if (fileName.EndsWith(JPG_EXTENSION, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    extension = JPG_EXTENSION;
                }
                else if (CrossPickerServices.EncodeOption == CrossPickerServices.TextureEncoderEnum.JPG)
                {
                    extension = JPG_EXTENSION;
                    fileName += extension;
                }
                else
                {
                    extension = PNG_EXTENSION;
                    fileName += extension;
                }
                return SaveTextureToPath_ExtensionProcessing_Internal(bytes, folderPath, fileName, extension);
            }
            return string.Empty;
        }

        protected static string SaveTextureToPath_ExtensionProcessing_Internal(byte[] bytes, string folderPath, string fileName, string extension)
        {
            if (bytes != null)
            {
                if (string.IsNullOrEmpty(fileName))
                    fileName = GetUniqueImgFileName(extension);
                var savePath = GetSavePath_Internal(folderPath, fileName, true, extension);
                System.IO.File.WriteAllBytes(savePath, bytes);
                return savePath;
            }
            return "";
        }

        public static string GetPersistentSavePath(string formattedFileName)
        {
            var extension = "." + CrossPickerServices.EncodeOption.ToString().ToLower();
            return GetPersistentSavePath(formattedFileName, extension);
        }

        /// <summary>
        /// Combine PersistentSavePath with FormattedFileName (FileName + Extension)
        /// </summary>
        public static string GetPersistentSavePath(string formattedFileName, string extension)
        {
            return GetSavePath_Internal(Application.isEditor ? Application.temporaryCachePath : Application.persistentDataPath, formattedFileName, false, extension);
        }

        /// <summary>
        /// Combine TemporarySavePath with FormattedFileName (FileName + Extension)
        /// </summary>
        public static string GetTemporarySavePath(string formattedFileName)
        {
            var extension = "." + CrossPickerServices.EncodeOption.ToString().ToLower();
            return GetPersistentSavePath(formattedFileName, extension);
        }

        public static string GetTemporarySavePath(string formattedFileName, string extension)
        {
            return GetSavePath_Internal(Application.temporaryCachePath, formattedFileName, false, extension);
        }

        protected static string GetSavePath_Internal(string folderPath, string formattedFileName, bool canCreateDirectory, string extension)
        {
            if (!string.IsNullOrEmpty(extension) && !string.IsNullOrEmpty(formattedFileName) && !formattedFileName.EndsWith(extension, System.StringComparison.InvariantCultureIgnoreCase))
                formattedFileName += extension;
            if (!string.IsNullOrEmpty(formattedFileName))
            {
                string fileName = System.IO.Path.GetFileName(formattedFileName);
                string mainFolderPath = folderPath != null ? folderPath.Replace("\\", "/") : "";
                string subPath = formattedFileName.Replace("\\", "/").Replace(fileName, "").Replace(mainFolderPath, "");
                //Prevent bug when combining a path with only "/" that will return "/" fullpath
                while (subPath.StartsWith("/"))
                {
                    subPath = subPath.Length > 1 ? subPath.Substring(1, subPath.Length - 1) : "";
                }
                string fullFolder = System.IO.Path.Combine(mainFolderPath, subPath).Replace("\\", "/");
                //We have a subfolder
                if (canCreateDirectory && !string.Equals(fullFolder, mainFolderPath) && !System.IO.Directory.Exists(fullFolder))
                    System.IO.Directory.CreateDirectory(fullFolder);
                var savePath = System.IO.Path.Combine(fullFolder, fileName).Replace("\\", "/");
                return savePath;
            }
            return "";
        }


        #endregion

        #region Helper Static Functions

        public static string EncodeTo64(Texture2D texture)
        {
            return EncodeTo64(texture, System.Text.Encoding.UTF8);
        }

        public static string EncodeTo64(Texture2D texture, System.Text.Encoding encoding)
        {
            return EncodeTo64(texture, texture != null && texture.format == TextureFormat.RGB24, encoding);
        }

        public static string EncodeTo64(Texture2D texture, bool isJpg)
        {
            return EncodeTo64(texture, isJpg, System.Text.Encoding.UTF8);
        }

        public static string EncodeTo64(Texture2D texture, bool isJpg, System.Text.Encoding encoding)
        {
            return EncodeTo64(texture != null ? (isJpg ? texture.EncodeToJPG() : texture.EncodeToPNG()) : null, encoding);
        }

        public static string EncodeTo64(string toEncode)
        {
            return EncodeTo64(toEncode, System.Text.Encoding.UTF8);
        }

        public static string EncodeTo64(string toEncode, System.Text.Encoding encoding)
        {
            toEncode = toEncode == null ? "" : toEncode;
            string returnValue = toEncode;
            try
            {
                byte[] toEncodeAsBytes = encoding.GetBytes(toEncode);
                if (encoding != null)
                    returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            }
            catch { }
            return returnValue;
        }

        public static string EncodeTo64(byte[] data, System.Text.Encoding encoding)
        {
            data = data == null ? new byte[0] : data;
            string returnValue = "";
            try
            {
                if (encoding != null)
                    returnValue = System.Convert.ToBase64String(data);
            }
            catch { }
            return returnValue;
        }

        public static string DecodeFrom64(string encodedData)
        {
            return DecodeFrom64(encodedData, System.Text.Encoding.UTF8);
        }

        public static string DecodeFrom64(string encodedData, System.Text.Encoding encoding)
        {
            encodedData = encodedData == null ? "" : encodedData;
            string returnValue = encodedData;
            try
            {
                byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
                if (encoding != null)
                    returnValue = encoding.GetString(encodedDataAsBytes);
            }
            catch { }
            return returnValue;
        }

        public static string DecodeFrom64(byte[] data, System.Text.Encoding encoding)
        {
            data = data == null ? new byte[0] : data;
            string returnValue = "";
            try
            {
                if (encoding != null)
                    returnValue = encoding.GetString(data);
            }
            catch { }
            return returnValue;
        }

        public static byte[] GetBytesFromString(string value, System.Text.Encoding encoding)
        {
            value = value == null ? "" : value;
            byte[] returnValue = new byte[0];
            try
            {
                byte[] bytes = encoding.GetBytes(value);
                if (bytes != null)
                    returnValue = bytes;
            }
            catch { }
            return returnValue;
        }

        public static byte[] GetBytesFromString(string value)
        {
            return !string.IsNullOrEmpty(value) ? System.Convert.FromBase64String(value) : new byte[0];
        }

        public static string GetStringFromBytes(byte[] data)
        {
            data = data == null ? new byte[0] : data;
            string returnValue = "";
            try
            {
                string value = System.Convert.ToBase64String(data);
                if (value != null)
                    returnValue = value;
            }
            catch { }
            return returnValue;
        }

        #endregion

        #region Custom ExternalImage Downloader

        static string s_customDownloderPattern = ".*";

        protected virtual void RegisterCustomGetFileFromPathPattern()
        {
            UnregisterCustomGetFileFromPathPattern();
            ExternalResources.RegisterCustomImgDownloaderPattern(s_customDownloderPattern, GetImageFromPath);
        }

        protected virtual void UnregisterCustomGetFileFromPathPattern()
        {
            ExternalResources.UnregisterCustomImgDownloaderPattern(s_customDownloderPattern);
        }

        Dictionary<string, ExternImgFile> _cachedImages = new Dictionary<string, ExternImgFile>();
        private ExternImgFile GetImageFromPath(string key, System.Action<ExternImgFile> callback)
        {
            ExternImgFile wwwImage = CreateImageStruct(key);
            if (callback != null)
                callback(wwwImage);
            return wwwImage;
        }

        protected ExternImgFile CreateImageStruct(string key)
        {
            ExternImgFile wwwImage = null;
            _cachedImages.TryGetValue(key, out wwwImage);
            if (wwwImage == null)
                wwwImage = new ExternImgFile();
            wwwImage.Error = null;
            wwwImage.Url = key;
            wwwImage.Texture = !string.IsNullOrEmpty(key) && CrossFileProvider.FileExists(key) ? NativeGallery.LoadImageAtPath(key, MaxImageLoadSize, false, false) : null;
            if (wwwImage.Texture != null)
                wwwImage.Sprite = Sprite.Create(wwwImage.Texture, new Rect(0, 0, wwwImage.Texture.width, wwwImage.Texture.height), new Vector2(0.5f, 0.5f));
            if (wwwImage.Texture == null)
                wwwImage.Error = "Unable to get image from path";
            wwwImage.Status = AsyncStatusEnum.Done;
            _cachedImages[key] = wwwImage;
            return wwwImage;
        }

        #endregion
    }
}