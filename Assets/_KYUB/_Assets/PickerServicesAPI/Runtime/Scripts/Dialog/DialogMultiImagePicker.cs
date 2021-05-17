using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using Kyub.Async;
using MaterialUI;

namespace Kyub.PickerServices
{
    public class DialogMultiImagePicker : BaseDialogMultiImagePicker
    {
        #region Private Variables

        [SerializeField]
        MaterialButton m_GalleryButton = null;
        [SerializeField]
        MaterialButton m_CameraButton = null;
        [SerializeField]
        PickerSelectorForm.CameraModeEnum m_CameraMode = PickerSelectorForm.CameraModeEnum.ControlledByManager;
        [SerializeField]
        bool m_MultiSelect = true;

        #endregion

        #region Public Properties

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

        public void Initialize(PickerSelectorForm.CameraModeEnum cameraMode, bool multiSelect, System.Action<string[]> onPickerSucess = null, System.Action onPickerFailed = null)
        {
            base.Initialize(onPickerSucess, onPickerFailed);
            m_CameraMode = this.cameraMode;
            m_MultiSelect = multiSelect;
        }

        protected void CallPickerCamera()
        {
            RegisterEvents();

            //Save Camera Image into TempFolder
            System.Action<ExternImgFile> cameraCallback = (externImg) => 
            {
                var tempUrl = externImg.Texture != null? CrossPickerServices.SaveTextureToTemporaryPath(externImg.Texture) : null;
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

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
#if UNITY_WEBGL && !UNITY_EDITOR
            if (m_GalleryButton != null && m_GalleryButton.onPress != null)
                m_GalleryButton.onPress.AddListener(CallPickerGallery);
            if (m_CameraButton != null && m_CameraButton.onPress != null)
                m_CameraButton.onPress.AddListener(CallPickerCamera);
#else
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
            if (m_GalleryButton != null && m_GalleryButton.onPress != null)
                m_GalleryButton.onPress.RemoveListener(CallPickerGallery);
            if (m_CameraButton != null && m_CameraButton.onPress != null)
                m_CameraButton.onPress.RemoveListener(CallPickerCamera);
#else
            if (m_GalleryButton != null && m_GalleryButton.onClick != null)
                m_GalleryButton.onClick.RemoveListener(CallPickerGallery);
            if (m_CameraButton != null && m_CameraButton.onClick != null)
                m_CameraButton.onClick.RemoveListener(CallPickerCamera);
#endif
        }

        #endregion
    }
}
