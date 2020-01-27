using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using Kyub.Async;
using MaterialUI;

namespace Kyub.PickerServices
{
    public class DialogImagePicker : BaseDialogImagePicker
    {
        #region Private Variables

        [SerializeField]
        MaterialButton m_GalleryButton = null;
        [SerializeField]
        MaterialButton m_CameraButton = null;
        [SerializeField]
        PickerSelectorForm.CameraModeEnum m_CameraMode = PickerSelectorForm.CameraModeEnum.ControlledByManager;

        #endregion

        #region Public Properties

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

        public void Initialize(PickerSelectorForm.CameraModeEnum cameraMode, System.Action<ExternImgFile> onPickerSucess = null, System.Action onPickerFailed = null)
        {
            base.Initialize(onPickerSucess, onPickerFailed);
            m_CameraMode = this.cameraMode;
        }

        protected void CallPickerCamera()
        {
            RegisterEvents();
            if (m_CameraMode == PickerSelectorForm.CameraModeEnum.ControlledByManager)
                CrossPickerServices.DeserializeCameraImage();
            else
                CrossPickerServices.DeserializeCameraImage(m_CameraMode == PickerSelectorForm.CameraModeEnum.SaveToGallery);
        }

        protected void CallPickerGallery()
        {
            RegisterEvents();
            CrossPickerServices.DeserializeAlbumImage();
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
