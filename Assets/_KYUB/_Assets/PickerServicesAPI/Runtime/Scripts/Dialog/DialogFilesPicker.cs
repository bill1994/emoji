using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using Kyub.Async;
using MaterialUI;

namespace Kyub.PickerServices
{
    public class DialogFilesPicker : BaseDialogFilesPicker
    {
        #region Private Variables

        [SerializeField]
        MaterialButton m_FileButton = null;
        [SerializeField]
        MaterialButton m_GalleryButton = null;
        [SerializeField]
        MaterialButton m_CameraButton = null;
        [SerializeField]
        PickerSelectorForm.CameraModeEnum m_CameraMode = PickerSelectorForm.CameraModeEnum.ControlledByManager;
        [SerializeField]
        bool m_MultiSelect = true;
        [SerializeField]
        List<string> m_AllowerFileExtensions = new List<string>();


        #endregion

        #region Public Properties

        public List<string> allowerFileExtensions
        {
            get
            {
                return m_AllowerFileExtensions;
            }
            set
            {
                if (m_AllowerFileExtensions == value)
                    return;
                m_AllowerFileExtensions = value;
            }
        }

        public bool multiSelect
        {
            get
            {
                return m_MultiSelect;
            }
            set
            {
                if (m_MultiSelect == value)
                    return;
                m_MultiSelect = value;
            }
        }

        public PickerSelectorForm.CameraModeEnum cameraMode
        {
            get
            {
                return m_CameraMode;
            }
            set
            {
                if (m_CameraMode == value)
                    return;
                m_CameraMode = value;
            }
        }

        #endregion

        #region Helper Functions

        public void Initialize(System.Action<string[]> onPickerSucess = null, System.Action onPickerFailed = null)
        {
            Initialize(this.cameraMode, this.multiSelect, this.allowerFileExtensions, onPickerSucess, onPickerFailed);
        }

        public void Initialize(PickerSelectorForm.CameraModeEnum cameraMode, bool multiSelect, IList<string> allowedFileExtensions, System.Action<string[]> onPickerSucess = null, System.Action onPickerFailed = null)
        {
            base.InitializeInternal(onPickerSucess, onPickerFailed);
            m_CameraMode = cameraMode;
            m_MultiSelect = multiSelect;
            m_AllowerFileExtensions = allowedFileExtensions != null ? new List<string>(allowedFileExtensions) : new List<string>();
        }

        protected void CallPickerCamera()
        {
            RegisterEvents();

            //Save Camera Image into TempFolder
            System.Action<ExternImgFile> cameraCallback = (externImg) => 
            {
                var tempUrl = externImg != null && externImg.Texture != null? CrossPickerServices.SaveTextureToTemporaryPath(externImg.Texture) : null;
                HandleOnFilesPickerFinish(!string.IsNullOrEmpty(tempUrl) ? new string[] { tempUrl } : null);
            };
            if (m_CameraMode == PickerSelectorForm.CameraModeEnum.ControlledByManager)
                CrossPickerServices.DeserializeCameraImage(cameraCallback);
            else
                CrossPickerServices.DeserializeCameraImage(m_CameraMode == PickerSelectorForm.CameraModeEnum.SaveToGallery, cameraCallback);
        }

        protected void CallPickerGallery()
        {
            RegisterEvents();
            CrossPickerServices.OpenImageBrowser(m_MultiSelect, HandleOnFilesPickerFinish);
        }

        protected void CallPickerFiles()
        {
            RegisterEvents();
            CrossPickerServices.OpenFileBrowser(m_AllowerFileExtensions, m_MultiSelect, HandleOnFilesPickerFinish);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
#if UNITY_WEBGL && !UNITY_EDITOR
i           if (m_FileButton != null && m_FileButton.onPress != null)
                m_FileButton.onPress.AddListener(CallPickerFiles);
            if (m_GalleryButton != null && m_GalleryButton.onPress != null)
                m_GalleryButton.onPress.AddListener(CallPickerGallery);
            if (m_CameraButton != null && m_CameraButton.onPress != null)
                m_CameraButton.onPress.AddListener(CallPickerCamera);
#else
            if(m_FileButton != null && m_FileButton.onClick != null)
                m_FileButton.onClick.AddListener(CallPickerFiles);
            if (m_GalleryButton != null && m_GalleryButton.onClick != null)
                m_GalleryButton.onClick.AddListener(CallPickerGallery);
            if (m_CameraButton != null && m_CameraButton.onClick != null)
                m_CameraButton.onClick.AddListener(CallPickerCamera);
#endif
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();
#if UNITY_WEBGL && !UNITY_EDITOR
            if (m_FileButton != null && m_FileButton.onPress != null)
                m_FileButton.onPress.RemoveListener(CallPickerFiles);
            if (m_GalleryButton != null && m_GalleryButton.onPress != null)
                m_GalleryButton.onPress.RemoveListener(CallPickerGallery);
            if (m_CameraButton != null && m_CameraButton.onPress != null)
                m_CameraButton.onPress.RemoveListener(CallPickerCamera);
#else
            if (m_FileButton != null && m_FileButton.onClick != null)
                m_FileButton.onClick.RemoveListener(CallPickerFiles);
            if (m_GalleryButton != null && m_GalleryButton.onClick != null)
                m_GalleryButton.onClick.RemoveListener(CallPickerGallery);
            if (m_CameraButton != null && m_CameraButton.onClick != null)
                m_CameraButton.onClick.RemoveListener(CallPickerCamera);
#endif
        }

        #endregion
    }
}
