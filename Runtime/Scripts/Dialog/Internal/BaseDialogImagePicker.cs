using UnityEngine;
using System.Collections;
using Kyub.UI;
using UnityEngine.SceneManagement;
using Kyub.Async;
using UnityEngine.Events;
using UnityEngine.UI;
using MaterialUI;

namespace Kyub.PickerServices
{
    public abstract class BaseDialogImagePicker : MaterialDialogCompat
    {
        [System.Serializable]
        public class ExternImgUnityEvent : UnityEvent<ExternImgFile> { }

        #region Private Variables

        [SerializeField]
        ExternalResourcesReceiver m_ExternalResources = null;
        [SerializeField]
        MaterialButton m_CancelButton = null;

        #endregion

        #region Callbacks

        public ExternImgUnityEvent OnPickerSucess = new ExternImgUnityEvent();
        public UnityEvent OnPickerFailed = new UnityEvent();

        #endregion

        #region Receivers

        protected virtual void HandleOnPickerFinish(ExternImgFile result)
        {
            CrossPickerServices.OnPickerFinish -= HandleOnPickerFinish;
            if (m_ExternalResources != null)
            {
                m_ExternalResources.Key = result != null ? result.Url : "";
                m_ExternalResources.TryApply();
            }
            Hide_Internal(result, false, true);
        }

        #endregion

        #region Public Functions

        protected System.Action _onPickerFailedCallback = null;
        protected System.Action<ExternImgFile> _onPickerSucessCallback = null;

        protected virtual void Initialize(System.Action<ExternImgFile> onPickerSucess, System.Action onPickerFailed)
        {
            _onPickerFailedCallback = onPickerFailed;
            _onPickerSucessCallback = onPickerSucess;
        }

        #endregion

        #region Helper Functions

        public override void OnActivityEndShow()
        {
            RegisterEvents();
            if(m_ExternalResources != null)
                m_ExternalResources.Key = "";
            base.OnActivityEndShow();
        }

        public override void OnActivityEndHide()
        {
            base.OnActivityEndHide();
            if (IsEventRegistered(CrossPickerServices.OnPickerFinish, HandleOnPickerFinish))
                Hide_Internal(null, true, false);
        }

        protected void Hide_Internal(ExternImgFile result, bool forceCallEventIfFailed, bool callHide)
        {
            if(callHide)
                Hide();

            UnregisterEvents();
            if (result != null && !string.IsNullOrEmpty(result.Url))
            {
                if (_onPickerSucessCallback != null)
                    _onPickerSucessCallback(result);
                _onPickerSucessCallback = null;
                if (OnPickerSucess != null)
                    OnPickerSucess.Invoke(result);
            }
            else
            {
                if (forceCallEventIfFailed)
                    CrossPickerServices.CallPickerFinishEvent(result);
                if (_onPickerFailedCallback != null)
                    _onPickerFailedCallback();
                _onPickerFailedCallback = null;
                OnPickerFailed.Invoke();
            }
            base.Hide();
        }

        public bool IsEventRegistered<ActionType>(ActionType eventHandler, ActionType eventToCheck) where ActionType : System.Delegate
        {
            if (eventHandler != null)
            {
                var registedEvents = eventHandler.GetInvocationList();
                foreach (System.Delegate existingHandler in registedEvents)
                {
                    if (existingHandler == eventToCheck)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            CrossPickerServices.OnPickerFinish += HandleOnPickerFinish;
            if (m_CancelButton != null && m_CancelButton.onClick != null)
                m_CancelButton.onClick.AddListener(Hide);
        }

        protected virtual void UnregisterEvents()
        {
            CrossPickerServices.OnPickerFinish -= HandleOnPickerFinish;
            if (m_CancelButton != null && m_CancelButton.onClick != null)
                m_CancelButton.onClick.RemoveListener(Hide);
        }

        #endregion

    }
}
