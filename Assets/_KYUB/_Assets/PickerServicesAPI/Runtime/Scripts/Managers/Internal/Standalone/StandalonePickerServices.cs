using UnityEngine;
using System.Collections;
using SFB;
using System.Linq;
using System.Collections.Generic;
using System.Text;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Kyub.PickerServices
{
    public class StandalonePickerServices : Singleton<StandalonePickerServices>
    {
        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            FixInstanceName();
        }

        protected virtual void Start()
        {
            FixInstanceName();
        }

        protected virtual void OnEnable()
        {
            FixInstanceName();
        }

        #endregion

        #region Extern Functions

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
#endif

        #endregion

        #region Static Functions

        public static void Init()
        {
            FixInstanceName();
        }

        public static void SerializeDataToAlbum(Texture2D texture, string fileName)
        {
            FixInstanceName();
            var extension = "." + CrossPickerServices.EncodeOption.ToString().ToLower();
            if (!string.IsNullOrEmpty(fileName) && !fileName.EndsWith(extension))
                fileName += extension;

            byte[] data = texture != null ? (CrossPickerServices.EncodeOption == CrossPickerServices.TextureEncoderEnum.JPG ? texture.EncodeToJPG() : texture.EncodeToPNG()) : null;

            var temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(data, System.IO.Path.GetFileName(fileName));
            if (!string.IsNullOrEmpty(temporarySavePath))
                CrossPickerServices.CallImageSavedSucessEvent(temporarySavePath);
            else
                CrossPickerServices.CallImageSavedFailedEvent();
        }

        public static void DeserializeAlbumImage()
        {
            FixInstanceName();

#if UNITY_WEBGL && !UNITY_EDITOR
            if(Instance != null)
                UploadFile(Instance.name, "NativeImagePickedEnd", ".png, .jpg, .jpeg", false);
#else
            var extensions = new[] {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
                new ExtensionFilter("All Files", "*" ),
            };

            StandaloneFileBrowser.OpenFilePanelAsync("Open Image", "", extensions, false, (result) =>
            {
                var path = result != null && result.Length > 0 ? result.First() : "";
                if (Instance != null)
                    Instance.NativeImagePickedEnd(path);
            });
#endif
        }

        public static void DeserializeCameraImage(bool saveToGallery)
        {
            FixInstanceName();
            CrossPickerServices.CallPickerFinishEvent(null);
            if (saveToGallery)
                CrossPickerServices.CallImageSavedFailedEvent();

            Debug.Log("DeserializeCameraImage is Invalid on Standalone");
        }

        public static void OpenFileBrowser(IList<string> allowedFileExtensions, bool multiselect)
        {
            FixInstanceName();

#if UNITY_WEBGL && !UNITY_EDITOR
            if(Instance != null)
            {
                var singleStringExtension = ConvertToSingleStringExtension(allowedFileExtensions);
                UploadFile(Instance.name, "NativeFilesPickedEndSingleString", singleStringExtension, allowMultiples);
            }
#else
            var nativeExtensions = ConvertToValidExtension(allowedFileExtensions);
            var extensions = nativeExtensions.Length > 0 ? new[]
            {
                new ExtensionFilter("Files", nativeExtensions ),
                new ExtensionFilter("All Files", "*" ),
            } : new[] 
            {
                new ExtensionFilter("All Files", "*" )
            };

            StandaloneFileBrowser.OpenFilePanelAsync("Select Files", "", extensions, multiselect, (result) =>
            {
                var path = result;
                if (Instance != null)
                    Instance.NativeFilesPickedEnd(path);
            });
#endif
        }

        public static void OpenImageBrowser(bool multiselect)
        {
            OpenFileBrowser(new string[] { "png", "jpg", "jpeg" }, multiselect);
        }

        #endregion

        #region Native Callbacks

        private static string[] ConvertToValidExtension(IList<string> fileExtensions)
        {
            //First we must convert file extension to mimetype like (pdf to application/pdf)
            HashSet<string> nativeFileExtensions = new HashSet<string>();
            if (fileExtensions != null && fileExtensions.Count > 0)
            {
                foreach (var file in fileExtensions)
                {
                    string extension = file != null && System.IO.Path.HasExtension(file) ? System.IO.Path.GetExtension(file) : file;
                    if (!string.IsNullOrEmpty(extension))
                    {
                        nativeFileExtensions.Add(extension);
                    }
                }
            }

            return nativeFileExtensions.ToArray();
        }

        private static string ConvertToSingleStringExtension(IList<string> fileExtensions)
        {
            //First we must convert file extension to mimetype like (pdf to application/pdf)
            StringBuilder builder = new StringBuilder();
            if (fileExtensions != null && fileExtensions.Count > 0)
            {
                foreach (var file in fileExtensions)
                {
                    string extension = file != null && System.IO.Path.HasExtension(file) ? System.IO.Path.GetExtension(file) : file;
                    if (!string.IsNullOrEmpty(extension))
                    {
                        if (builder.Length > 0)
                            builder.Append(" ");
                        builder.Append("." + extension);
                    }
                }
            }

            return builder.ToString();
        }

        protected virtual void NativeFilesPickedEndSingleString(string joinPaths)
        {
            var paths = string.IsNullOrEmpty(joinPaths)? null : 
                joinPaths.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            NativeFilesPickedEnd(paths);
        }

        protected virtual void NativeFilesPickedEnd(string[] paths)
        {
            CrossPickerServices.CallFilesPickerFinishEvent(paths);
        }

        protected virtual void NativeImagePickedEnd(string path)
        {
            var texture = !string.IsNullOrEmpty(path) ? NativeCamera.LoadImageAtPath(path, -1, false) : null;
            CrossPickerServices.CallPickerFinishEvent(path, texture);
        }

        #endregion

        #region Internal Static Helper Functions

        protected static void FixInstanceName()
        {
            if (GetInstance(false) != null)
                Instance.name = "StandalonePickerServices";
        }

        #endregion
    }
}
