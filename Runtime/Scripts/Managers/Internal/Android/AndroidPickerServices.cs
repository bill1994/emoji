using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Async;
using System.Linq;

namespace Kyub.PickerServices
{
    public class AndroidPickerServices : Singleton<AndroidPickerServices>
    {
        #region Constructors

        static AndroidPickerServices()
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
            NativeGallery.MediaSaveCallback mediaSaveDelegate = (success, path) =>
            {
                if (success)
                {
                    //var temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(data, System.IO.Path.GetFileName(fileName));
                    Instance.NativeImageSaveSuccess(path);
                }
                else
                    Instance.NativeImageSaveFailed();
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
                    Instance.NativeCameraPickedEnd(path, saveToGallery);
                };
            NativeCamera.TakePicture(cameraCallback, CrossPickerServices.MaxImageLoadSize);
        }

        public static void OpenFileBrowser(IList<string> allowedFileExtensions, bool multiselect)
        {
            if (NativeFilePicker.IsFilePickerBusy())
            {
                if (Instance != null)
                    Instance.NativeFilesPickedEnd(null);
            }

            var permission = NativeFilePicker.CheckPermission(true);
            if (permission == NativeFilePicker.Permission.Denied)
                NativeFilePicker.OpenSettings();
            else if (permission == NativeFilePicker.Permission.ShouldAsk)
                NativeFilePicker.RequestPermission(true);

            var nativeFileExtensions = ConvertToMimeType(allowedFileExtensions);
            if (multiselect && NativeFilePicker.CanPickMultipleFiles())
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
                        Instance.NativeFilesPickedEnd(string.IsNullOrEmpty(file) ? null : new string[] { file });
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
            CrossPickerServices.CallPickerFinishEvent(path, texture);
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
            HashSet<string> nativeFileExtensions = new HashSet<string>();
            if (fileExtensions != null && fileExtensions.Count > 0)
            {
                foreach (var file in fileExtensions)
                {
                    string extension = file;
                    if (extension == "*" || extension == ".*" || extension == "*.*")
                        extension = "*/*";
                    else
                        extension = file != null && System.IO.Path.HasExtension(file) ? System.IO.Path.GetExtension(file) : file;

                    if (!string.IsNullOrEmpty(extension))
                    {
                        string nativeExtension = extension.Contains("/")? extension : NativeFilePicker.ConvertExtensionToFileType(extension);
                        if (!string.IsNullOrEmpty(nativeExtension))
                        {
                            nativeFileExtensions.Add(nativeExtension);
                        }
                    }
                }
            }
            else
                nativeFileExtensions.Add("*/*");

            return nativeFileExtensions.ToArray();
        }

        protected static void FixInstanceName()
        {
            //Force create instance
            if(Instance != null)
                Instance.name = "AndroidPickerServices";
        }

        #endregion
    }
}
