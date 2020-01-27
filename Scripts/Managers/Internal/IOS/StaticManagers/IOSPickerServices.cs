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

        public static void SerializeDataToAlbum(Texture2D p_texture, string p_fileName)
        {
            FixInstanceName();
            var v_extension = "." + CrossPickerServices.EncodeOption.ToString().ToLower();
            if (!string.IsNullOrEmpty(p_fileName) && !p_fileName.EndsWith(v_extension))
                p_fileName += v_extension;

            byte[] v_data = p_texture != null ? (CrossPickerServices.EncodeOption == CrossPickerServices.TextureEncoderEnum.JPG ? p_texture.EncodeToJPG() : p_texture.EncodeToPNG()) : null;
            NativeGallery.MediaSaveCallback v_mediaSaveDelegate = (error) =>
            {
                if (string.IsNullOrEmpty(error))
                {
                    var v_temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(v_data, System.IO.Path.GetFileName(p_fileName));
                    Instance.NativeImageSaveSuccess(v_temporarySavePath);
                }
                else
                    Instance.NativeImageSaveFailed();
            };
            NativeGallery.SaveImageToGallery(v_data, Application.productName, p_fileName, v_mediaSaveDelegate);
        }

        public static void DeserializeAlbumImage()
        {
            FixInstanceName();
            NativeGallery.GetImageFromGallery(Instance.NativeImagePickedEnd, "", "image/*");
        }

        public static void DeserializeCameraImage(bool p_saveToGallery)
        {
            FixInstanceName();
            NativeCamera.CameraCallback v_cameraCallback =
                (path) =>
                {
                    Instance.NativeCameraPickedEnd(path, p_saveToGallery);
                };
            NativeCamera.TakePicture(v_cameraCallback, CrossPickerServices.MaxImageLoadSize);
        }

        #endregion

        #region Native Callbacks

        protected virtual void NativeImageSaveFailed()
        {
            CrossPickerServices.CallImageSavedFailedEvent();
        }

        protected virtual void NativeImageSaveSuccess(string p_path)
        {
            CrossPickerServices.CallImageSavedSucessEvent(p_path);
        }

        protected virtual void NativeCameraPickedEnd(string p_path, bool p_saveToGallery)
        {
            var v_texture = !string.IsNullOrEmpty(p_path) ? NativeGallery.LoadImageAtPath(p_path, CrossPickerServices.MaxImageLoadSize, false, false) : null;
            if (p_saveToGallery && v_texture != null)
                CrossPickerServices.SerializeDataToAlbum(v_texture, System.IO.Path.GetFileNameWithoutExtension(p_path));
            var v_temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(v_texture);
            CrossPickerServices.CallPickerFinishEvent(v_temporarySavePath, v_texture);
        }

        protected virtual void NativeImagePickedEnd(string p_path)
        {
            var v_texture = !string.IsNullOrEmpty(p_path)? NativeCamera.LoadImageAtPath(p_path, -1, false) : null;
            var v_temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(v_texture);
            CrossPickerServices.CallPickerFinishEvent(v_temporarySavePath, v_texture);
        }

        #endregion

        #region Internal Static Helper Functions

        public static void FixInstanceName()
        {
            //Force create instance
            if (Instance != null)
                Instance.name = "IOSPickerServices";
        }

        #endregion
    }
}
