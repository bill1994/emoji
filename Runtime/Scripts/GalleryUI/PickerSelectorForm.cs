using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Async;
using Kyub.UI;
using UnityEngine.UI;
using UnityEngine.Events;
using MaterialUI;

namespace Kyub.PickerServices
{
    public class PickerSelectorForm : BaseSpinner<EmptyStyleProperty>
    {
        #region Helper Functions

        [System.Serializable]
        public class StringUnityEvent : UnityEvent<string> { }

        public enum PickerModeEnum { Gallery, Camera, Both }
        public enum CameraModeEnum { ControlledByManager, SaveToGallery, DontSaveToGallery }

        #endregion

        #region Consts

        public const string IMAGE_PICKER_DIALOG = "CustomUI/DialogImagePicker";

        #endregion

        #region Private Variables

        [SerializeField]
        protected ExternalImage m_externalImage = null;
        //[SerializeField]
        //protected MaterialButton m_pickerSelectorButton = null;
        //[SerializeField]
        //protected CrossImagePickerSelectorDialog m_customPickerDialog = null;
        [SerializeField]
        protected PickerModeEnum m_pickerMode = PickerModeEnum.Gallery;
        [Space]
        [SerializeField]
        protected CameraModeEnum m_cameraMode = CameraModeEnum.ControlledByManager;

        DialogImagePicker _CachedDialog = null;

        #endregion

        #region Callbacks

        public StringUnityEvent OnPickerSucessCallback;

        #endregion

        #region Public Properties

        public ExternalImage ExternalImage
        {
            get
            {
                return m_externalImage;
            }
            set
            {
                if (m_externalImage == value)
                    return;
                m_externalImage = value;
            }
        }

        public PickerModeEnum PickerMode
        {
            get
            {
                return m_pickerMode;
            }
            set
            {
                if (m_pickerMode == value)
                    return;
                m_pickerMode = value;
            }
        }

        public CameraModeEnum CameraMode
        {
            get
            {
                return m_cameraMode;
            }
            set
            {
                if (m_cameraMode == value)
                    return;
                m_cameraMode = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (IsExpanded())
            {
                _CachedDialog.destroyOnHide = true;
                _CachedDialog.Hide();
            }
        }

        #endregion

        #region Helper Functions

        public override bool IsExpanded()
        {
            return (_CachedDialog != null && _CachedDialog.gameObject.activeSelf);
        }

        public override void Hide()
        {
            if (_CachedDialog != null && _CachedDialog.gameObject.activeSelf)
                _CachedDialog.Hide();

            HandleOnHide();
        }

        public override void Show()
        {
            CrossPickerServices.OnPickerFinish -= HandleOnPickerReturn;
            CrossPickerServices.OnPickerFinish += HandleOnPickerReturn;
            if (m_pickerMode == PickerModeEnum.Gallery)
                CrossPickerServices.DeserializeAlbumImage();
            else if (m_pickerMode == PickerModeEnum.Camera)
            {
                if (m_cameraMode == CameraModeEnum.ControlledByManager)
                    CrossPickerServices.DeserializeCameraImage();
                else
                    CrossPickerServices.DeserializeCameraImage(m_cameraMode == CameraModeEnum.SaveToGallery);
            }
            else
            {
                _CachedDialog = null;
                ShowFrameActivity(_CachedDialog, IMAGE_PICKER_DIALOG, (dialog, isDialog) =>
                {
                    dialog.destroyOnHide = true;
                    dialog.Initialize(m_cameraMode,
                        (file) => {
                            //Prevent call event two times
                            if (!BaseDialogImagePicker.IsEventRegistered(CrossPickerServices.OnPickerFinish, HandleOnPickerReturn))
                                return;
                            HandleOnPickerReturn(file);
                        }, HandleOnHide);
                });
            }
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnPickerReturn(ExternImgFile p_file)
        {
            CrossPickerServices.OnPickerFinish -= HandleOnPickerReturn;

            if (p_file == null || string.IsNullOrEmpty(p_file.Url))
                HandleOnHide();
            else
            {
                if (m_externalImage != null && p_file != null)
                    m_externalImage.Key = p_file.Url;

                if (OnPickerSucessCallback != null)
                    OnPickerSucessCallback.Invoke(p_file != null ? p_file.Url : "");
            }
        }



        #endregion
    }
}
