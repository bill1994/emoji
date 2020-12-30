using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Async;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Kyub.PickerServices
{
	public class IOSPickerServices : Singleton<IOSPickerServices> {

        #region Constructors

        static IOSPickerServices()
        {
        }

        #endregion

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
            NativeGallery.MediaSaveCallback mediaSaveDelegate = (error) =>
            {
                if (Instance != null)
                {
                    if (string.IsNullOrEmpty(error))
                    {
                        var temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(data, System.IO.Path.GetFileName(fileName));
                        Instance.NativeImageSaveSuccess(temporarySavePath);
                    }
                    else
                        Instance.NativeImageSaveFailed();
                }
            };
            NativeGallery.SaveImageToGallery(data, Application.productName, fileName, mediaSaveDelegate);
        }

        public static void DeserializeAlbumImage()
        {
            FixInstanceName();
            NativeGallery.GetImageFromGallery(Instance.NativeImagePickedEnd, "", "image/*");
        }

        public static void DeserializeCameraImage(bool saveToGallery)
        {
            FixInstanceName();
            NativeCamera.CameraCallback cameraCallback =
                (path) =>
                {
                    if(Instance != null)
                        Instance.NativeCameraPickedEnd(path, saveToGallery);
                };
            NativeCamera.TakePicture(cameraCallback, CrossPickerServices.MaxImageLoadSize);
        }

        public static void OpenFileBrowser(IList<string> allowedFileExtensions, bool multiselect)
        {
            var nativeFileExtensions = ConvertToMimeType(allowedFileExtensions);

            if (multiselect)
            {
                NativeFilePicker.PickMultipleFiles((files) =>
                {
                    if (Instance != null)
                        Instance.NativeFilesPickedEnd(files);
                }, nativeFileExtensions);
            }
            else
            {
                NativeFilePicker.PickFile((file) =>
                {
                    if (Instance != null)
                        Instance.NativeFilesPickedEnd(string.IsNullOrEmpty(file)? null : new string[] { file });
                }, nativeFileExtensions);
            }
        }

        #endregion

        #region Native Callbacks

        protected virtual void NativeImageSaveFailed()
        {
            CrossPickerServices.CallImageSavedFailedEvent();
        }

        protected virtual void NativeImageSaveSuccess(string path)
        {
            CrossPickerServices.CallImageSavedSucessEvent(path);
        }

        protected virtual void NativeCameraPickedEnd(string path, bool saveToGallery)
        {
            var texture = !string.IsNullOrEmpty(path) ? NativeGallery.LoadImageAtPath(path, CrossPickerServices.MaxImageLoadSize, false, false) : null;

            if (saveToGallery && texture != null)
                CrossPickerServices.SerializeDataToAlbum(texture, System.IO.Path.GetFileNameWithoutExtension(path));
            var temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(texture); //CrossPickerServices.GetTemporarySavePath(CrossPickerServices.GetUniqueImgFileName(CrossPickerServices.EncodeOption));
            CrossPickerServices.CallPickerFinishEvent(temporarySavePath, texture);
        }

        protected virtual void NativeImagePickedEnd(string path)
        {
            var texture = !string.IsNullOrEmpty(path)? NativeCamera.LoadImageAtPath(path, -1, false) : null;
            var temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(texture);
            CrossPickerServices.CallPickerFinishEvent(temporarySavePath, texture);
        }

        protected virtual void NativeFilesPickedEnd(string[] paths)
        {
            CrossPickerServices.CallFilesPickerFinishEvent(paths);
        }

        #endregion

        #region Internal Static Helper Functions

        private static string[] ConvertToMimeType(IList<string> fileExtensions)
        {
            //First we must convert file extension to mimetype like (pdf to application/pdf)
            List<string> nativeFileExtensions = new List<string>();
            if (fileExtensions != null && fileExtensions.Count > 0)
            {
                foreach (var file in fileExtensions)
                {
                    string extension = file != null && System.IO.Path.HasExtension(file) ? System.IO.Path.GetExtension(file) : file;
                    if (!string.IsNullOrEmpty(extension))
                    {
                        string nativeExtension = NativeFilePicker.ConvertExtensionToFileType(extension);
                        if (!string.IsNullOrEmpty(nativeExtension))
                        {
                            nativeFileExtensions.Add(nativeExtension);
                        }
                    }
                }
            }

            return nativeFileExtensions.ToArray();
        }

        public static void FixInstanceName()
        {
            //Force create instance
            if (Instance != null)
                Instance.name = "IOSPickerServices";
        }

        #endregion
    }
}
