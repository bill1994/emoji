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

        protected internal static void CallImageSavedSucessEvent(string p_path)
        {
            if (OnImageSavedSucess != null)
                OnImageSavedSucess(p_path);
        }

        protected internal static void CallPickerFinishEvent(ExternImgFile p_file)
        {
            if (p_file != null)
                ExternalResources.AddImageIntoCache(p_file);

            if (OnPickerFinish != null)
                OnPickerFinish(p_file);
        }

        protected internal static void CallPickerFinishEvent(string p_key, Sprite p_spriteReturned)
        {
            ExternImgFile v_callback = null;
            if (p_spriteReturned != null && p_spriteReturned.texture != null)
            {
                v_callback = new ExternImgFile();
                v_callback.Error = "";
                v_callback.Status = Kyub.Async.AsyncStatusEnum.Done;
                v_callback.Texture = p_spriteReturned.texture;
                v_callback.Sprite = p_spriteReturned;
                v_callback.Url = v_callback.Url != null ? p_key : "";
            }
            CallPickerFinishEvent(v_callback);
        }

        protected internal static void CallPickerFinishEvent(string p_key, Texture2D p_textureReturned)
        {
            //if (p_textureReturned != null && MaxImageLoadSize > 0)
            //    Kyub.ImagePicker.Extensions.CrossPickerTexture2DExtensions.ClampSize(p_textureReturned, 4, MaxImageLoadSize);
            Sprite v_sprite = p_textureReturned != null ? Sprite.Create(p_textureReturned, new Rect(0, 0, p_textureReturned.width, p_textureReturned.height), new Vector2(0.5f, 0.5f)) : null;
            CallPickerFinishEvent(p_key, v_sprite);
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

        public static void SerializeDataToAlbum(Texture2D p_image)
        {
            if (p_image != null)
            {
                SerializeDataToAlbum(p_image, p_image.name + "." + EncodeOption.ToString().ToLower());
            }
        }

        public static void SerializeDataToAlbum(Texture2D p_image, string p_name)
        {
            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidPickerServices.SerializeDataToAlbum(p_image, p_name);
#elif UNITY_IOS && !UNITY_EDITOR
			IOSPickerServices.SerializeDataToAlbum(p_image, p_name);
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.SerializeDataToAlbum(p_image, p_name);
#else
            CallImageSavedFailedEvent();
            //WebPickerServices.SerializeDataToAlbum(p_image, p_name);
#endif
        }

        /*public static void DeserializeWebImage()
        {
            SetWebInitParemeters();
            WebPickerServices.DeserializeAlbumImage();
        }*/

        public static void DeserializeAlbumImage()
        {
            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
		    AndroidPickerServices.DeserializeAlbumImage();
#elif UNITY_IOS && !UNITY_EDITOR
		    IOSPickerServices.DeserializeAlbumImage();
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.DeserializeAlbumImage();
#else
            CallPickerFinishEvent(null);
            //WebPickerServices.DeserializeAlbumImage();
#endif
        }

        public static void DeserializeCameraImage()
        {
            DeserializeCameraImage(SaveCameraImageToGallery);
        }

        public static void DeserializeCameraImage(bool p_saveToGallery)
        {
            SetInitParemeters();
#if UNITY_ANDROID && !UNITY_EDITOR
		    AndroidPickerServices.DeserializeCameraImage(p_saveToGallery);
#elif UNITY_IOS && !UNITY_EDITOR
		    IOSPickerServices.DeserializeCameraImage(p_saveToGallery);
#elif UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            StandalonePickerServices.DeserializeCameraImage(p_saveToGallery);
#else
            CallPickerFinishEvent(null);
            //WebPickerServices.DeserializeCameraImage(p_saveToGallery);
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

        public static string SaveTextureToPersistentPath(Texture2D p_texture, string p_fileName = "")
        {
            return SaveTextureToPath_Internal(p_texture, Application.isEditor ? Application.temporaryCachePath : Application.persistentDataPath, p_fileName);
        }

        public static string SaveTextureToPersistentPath(byte[] p_bytes, string p_fileName)
        {
            return SaveTextureToPath_Internal(p_bytes, Application.isEditor? Application.temporaryCachePath : Application.persistentDataPath, p_fileName);
        }

        public static string SaveTextureToTemporaryPath(Texture2D p_texture, string p_fileName = "")
        {
            return SaveTextureToPath_Internal(p_texture, Application.temporaryCachePath, p_fileName);
        }

        public static string SaveTextureToTemporaryPath(byte[] p_bytes, string p_fileName)
        {
            return SaveTextureToPath_Internal(p_bytes, Application.temporaryCachePath, p_fileName);
        }

        protected static string GetUniqueImgFileName(CrossPickerServices.TextureEncoderEnum p_encodeOption)
        {
            return GetUniqueImgFileName("", p_encodeOption);
        }

        protected static string GetUniqueImgFileName(string p_pattern, CrossPickerServices.TextureEncoderEnum p_encodeOption)
        {
            if (string.IsNullOrEmpty(p_pattern))
                p_pattern = "TempImg";
            return p_pattern + System.DateTime.UtcNow.ToString("ddMMyyyy").Replace(" ", "") + "_" + UnityEngine.Random.Range(0, 99999) + "." + p_encodeOption.ToString().ToLower();
        }

        protected static string SaveTextureToPath_Internal(Texture2D p_texture, string p_folderPath, string p_fileName = "")
        {
            if (p_texture != null)
            {
                if (string.IsNullOrEmpty(p_fileName))
                    p_fileName = GetUniqueImgFileName(CrossPickerServices.EncodeOption);
                byte[] v_bytes = null;
                if (p_fileName.Contains(".png"))
                    v_bytes = p_texture.EncodeToPNG();
                else if (p_fileName.Contains(".jpg"))
                    v_bytes = p_texture.EncodeToJPG();
                else if (CrossPickerServices.EncodeOption == CrossPickerServices.TextureEncoderEnum.JPG)
                {
                    v_bytes = p_texture.EncodeToJPG();
                    p_fileName += ".jpg";
                }
                else
                {
                    v_bytes = p_texture.EncodeToPNG();
                    p_fileName += ".png";
                }
                return SaveTextureToPath_Internal(v_bytes, p_folderPath, p_fileName);
            }
            return "";
        }

        protected static string SaveTextureToPath_Internal(byte[] p_bytes, string p_folderPath, string p_fileName)
        {
            if (p_bytes != null)
            {
                if (string.IsNullOrEmpty(p_fileName))
                    p_fileName = GetUniqueImgFileName(CrossPickerServices.EncodeOption);
                var v_savePath = GetSavePath_Internal(p_folderPath, p_fileName, true);
                System.IO.File.WriteAllBytes(v_savePath, p_bytes);
                return v_savePath;
            }
            return "";
        }

        /// <summary>
        /// Combine PersistentSavePath with FormattedFileName (FileName + Extension)
        /// </summary>
        public static string GetPersistentSavePath(string p_formattedFileName)
        {
            return GetSavePath_Internal(Application.isEditor ? Application.temporaryCachePath : Application.persistentDataPath, p_formattedFileName, false);
        }

        /// <summary>
        /// Combine TemporarySavePath with FormattedFileName (FileName + Extension)
        /// </summary>
        public static string GetTemporarySavePath(string p_formattedFileName)
        {
            return GetSavePath_Internal(Application.temporaryCachePath, p_formattedFileName, false);
        }

        protected static string GetSavePath_Internal(string p_folderPath, string p_formattedFileName, bool p_canCreateDirectory)
        {
            var v_extension = "." + CrossPickerServices.EncodeOption.ToString().ToLower();
            if (!string.IsNullOrEmpty(p_formattedFileName) && !p_formattedFileName.EndsWith(v_extension))
                p_formattedFileName += v_extension;
            if (!string.IsNullOrEmpty(p_formattedFileName))
            {
                string v_fileName = System.IO.Path.GetFileName(p_formattedFileName);
                string v_mainFolderPath = p_folderPath != null? p_folderPath.Replace("\\", "/") : "";
                string v_subPath = p_formattedFileName.Replace("\\", "/").Replace(v_fileName, "").Replace(v_mainFolderPath, "");
                //Prevent bug when combining a path with only "/" that will return "/" fullpath
                while (v_subPath.StartsWith("/"))
                {
                    v_subPath = v_subPath.Length > 1? v_subPath.Substring(1, v_subPath.Length - 1) : "";
                }
                string v_fullFolder = System.IO.Path.Combine(v_mainFolderPath, v_subPath).Replace("\\", "/");
                //We have a subfolder
                if (p_canCreateDirectory && !string.Equals(v_fullFolder, v_mainFolderPath) && !System.IO.Directory.Exists(v_fullFolder))
                    System.IO.Directory.CreateDirectory(v_fullFolder);
                var v_savePath = System.IO.Path.Combine(v_fullFolder, v_fileName).Replace("\\", "/");
                return v_savePath;
            }
            return "";
        }


        #endregion

        #region Helper Static Functions

        public static string EncodeTo64(Texture2D p_texture)
        {
            return EncodeTo64(p_texture, System.Text.Encoding.UTF8);
        }

        public static string EncodeTo64(Texture2D p_texture, System.Text.Encoding p_encoding)
        {
            return EncodeTo64(p_texture, p_texture != null && p_texture.format == TextureFormat.RGB24, p_encoding);
        }

        public static string EncodeTo64(Texture2D p_texture, bool isJpg)
        {
            return EncodeTo64(p_texture, isJpg, System.Text.Encoding.UTF8);
        }

        public static string EncodeTo64(Texture2D p_texture, bool isJpg, System.Text.Encoding p_encoding)
        {
            return EncodeTo64(p_texture != null? (isJpg? p_texture.EncodeToJPG() : p_texture.EncodeToPNG()) : null, p_encoding);
        }

        public static string EncodeTo64(string p_toEncode)
        {
            return EncodeTo64(p_toEncode, System.Text.Encoding.UTF8);
        }

        public static string EncodeTo64(string p_toEncode, System.Text.Encoding p_encoding)
        {
            p_toEncode = p_toEncode == null ? "" : p_toEncode;
            string v_returnValue = p_toEncode;
            try
            {
                byte[] v_toEncodeAsBytes = p_encoding.GetBytes(p_toEncode);
                if (p_encoding != null)
                    v_returnValue = System.Convert.ToBase64String(v_toEncodeAsBytes);
            }
            catch { }
            return v_returnValue;
        }

        public static string EncodeTo64(byte[] p_data, System.Text.Encoding p_encoding)
        {
            p_data = p_data == null ? new byte[0] : p_data;
            string v_returnValue = "";
            try
            {
                if (p_encoding != null)
                    v_returnValue = System.Convert.ToBase64String(p_data);
            }
            catch { }
            return v_returnValue;
        }

        public static string DecodeFrom64(string p_encodedData)
        {
            return DecodeFrom64(p_encodedData, System.Text.Encoding.UTF8);
        }

        public static string DecodeFrom64(string p_encodedData, System.Text.Encoding p_encoding)
        {
            p_encodedData = p_encodedData == null ? "" : p_encodedData;
            string v_returnValue = p_encodedData;
            try
            {
                byte[] v_encodedDataAsBytes = System.Convert.FromBase64String(p_encodedData);
                if (p_encoding != null)
                    v_returnValue = p_encoding.GetString(v_encodedDataAsBytes);
            }
            catch { }
            return v_returnValue;
        }

        public static string DecodeFrom64(byte[] p_data, System.Text.Encoding p_encoding)
        {
            p_data = p_data == null ? new byte[0] : p_data;
            string v_returnValue = "";
            try
            {
                if (p_encoding != null)
                    v_returnValue = p_encoding.GetString(p_data);
            }
            catch { }
            return v_returnValue;
        }

        public static byte[] GetBytesFromString(string p_string, System.Text.Encoding p_encoding)
        {
            p_string = p_string == null ? "" : p_string;
            byte[] v_returnValue = new byte[0];
            try
            {
                byte[] v_bytes = p_encoding.GetBytes(p_string);
                if (v_bytes != null)
                    v_returnValue = v_bytes;
            }
            catch { }
            return v_returnValue;
        }

        public static byte[] GetBytesFromString(string p_string)
        {
            return !string.IsNullOrEmpty(p_string)? System.Convert.FromBase64String(p_string) : new byte[0];
        }

        public static string GetStringFromBytes(byte[] p_data)
        {
            p_data = p_data == null ? new byte[0] : p_data;
            string v_returnValue = "";
            try
            {
                string v_string = System.Convert.ToBase64String(p_data);
                if (v_string != null)
                    v_returnValue = v_string;
            }
            catch { }
            return v_returnValue;
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
        private ExternImgFile GetImageFromPath(string p_key, System.Action<ExternImgFile> p_callback)
        {
            ExternImgFile v_wwwImage = CreateImageStruct(p_key);
            if(p_callback != null)
                p_callback(v_wwwImage);
            return v_wwwImage;
        }

        protected ExternImgFile CreateImageStruct(string p_key)
        {
            ExternImgFile v_wwwImage = null;
            _cachedImages.TryGetValue(p_key, out v_wwwImage);
            if (v_wwwImage == null)
                v_wwwImage = new ExternImgFile();
            v_wwwImage.Error = null;
            v_wwwImage.Url = p_key;
            v_wwwImage.Texture = !string.IsNullOrEmpty(p_key) && CrossFileProvider.FileExists(p_key)? NativeGallery.LoadImageAtPath(p_key, MaxImageLoadSize, false, false) : null;
            if (v_wwwImage.Texture != null)
                v_wwwImage.Sprite = Sprite.Create(v_wwwImage.Texture, new Rect(0, 0, v_wwwImage.Texture.width, v_wwwImage.Texture.height), new Vector2(0.5f, 0.5f));
            if (v_wwwImage.Texture == null)
                v_wwwImage.Error = "Unable to get image from path";
            v_wwwImage.Status = AsyncStatusEnum.Done;
            _cachedImages[p_key] = v_wwwImage;
            return v_wwwImage;
        }

        #endregion
    }
}