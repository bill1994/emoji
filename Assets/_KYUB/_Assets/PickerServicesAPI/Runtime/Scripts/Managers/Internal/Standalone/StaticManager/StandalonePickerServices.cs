using UnityEngine;
using System.Collections;
using SFB;
using System.Linq;
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

        public static void SerializeDataToAlbum(Texture2D p_texture, string p_fileName)
        {
            FixInstanceName();
            var v_extension = "." + CrossPickerServices.EncodeOption.ToString().ToLower();
            if (!string.IsNullOrEmpty(p_fileName) && !p_fileName.EndsWith(v_extension))
                p_fileName += v_extension;

            byte[] v_data = p_texture != null ? (CrossPickerServices.EncodeOption == CrossPickerServices.TextureEncoderEnum.JPG ? p_texture.EncodeToJPG() : p_texture.EncodeToPNG()) : null;

            var v_temporarySavePath = CrossPickerServices.SaveTextureToTemporaryPath(v_data, System.IO.Path.GetFileName(p_fileName));
            if (!string.IsNullOrEmpty(v_temporarySavePath))
                CrossPickerServices.CallImageSavedSucessEvent(v_temporarySavePath);
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

        public static void DeserializeCameraImage(bool p_saveToGallery)
        {
            FixInstanceName();
            CrossPickerServices.CallPickerFinishEvent(null);
            if (p_saveToGallery)
                CrossPickerServices.CallImageSavedFailedEvent();

            Debug.Log("DeserializeCameraImage is Invalid on Standalone");
        }

        #endregion

        #region Native Callbacks

        protected virtual void NativeImagePickedEnd(string p_path)
        {
            var v_texture = !string.IsNullOrEmpty(p_path) ? NativeCamera.LoadImageAtPath(p_path, -1, false) : null;
            CrossPickerServices.CallPickerFinishEvent(p_path, v_texture);
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
