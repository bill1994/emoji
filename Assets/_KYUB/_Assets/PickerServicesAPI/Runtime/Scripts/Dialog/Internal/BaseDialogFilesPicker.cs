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
    public abstract class BaseDialogFilesPicker : MaterialDialogCompat
    {
        [System.Serializable]
        public class StrArrayUnityEvent : UnityEvent<string[]> { }

        #region Private Variables

        [SerializeField]
        MaterialButton m_CancelButton = null;

        #endregion

        #region Callbacks

        public StrArrayUnityEvent OnPickerSucess = new StrArrayUnityEvent();
        public UnityEvent OnPickerFailed = new UnityEvent();

        #endregion

        #region Receivers

        protected virtual void HandleOnFilesPickerFinish(string[] result)
        {
            Hide_Internal(result, true);
        }

        #endregion

        #region Public Functions

        protected System.Action _onPickerFailedCallback = null;
        protected System.Action<string[]> _onPickerSucessCallback = null;

        protected virtual void InitializeInternal(System.Action<string[]> onPickerSucess, System.Action onPickerFailed)
        {
            _onPickerFailedCallback = onPickerFailed;
            _onPickerSucessCallback = onPickerSucess;
        }

        #endregion

        #region Helper Functions

        public override void OnActivityEndShow()
        {
            RegisterEvents();
            base.OnActivityEndShow();
        }

        public override void OnActivityBeginHide()
        {
            base.OnActivityBeginHide();
            Hide_Internal(null, false);
        }

        protected void Hide_Internal(string[] result, bool callHide)
        {
            if (this == null)
                return;

            if (callHide)
            {
                var onDismiss = _onPickerFailedCallback;
                _onPickerFailedCallback = null;

                Hide();

                _onPickerFailedCallback = onDismiss;
            }

            UnregisterEvents();
            if (result != null && result.Length > 0)
            {
                if (_onPickerSucessCallback != null)
                    _onPickerSucessCallback(result);

                if (OnPickerSucess != null)
                    OnPickerSucess.Invoke(result);
            }
            else
            {
                if (_onPickerFailedCallback != null)
                    _onPickerFailedCallback();

                if(OnPickerFailed != null)
                    OnPickerFailed.Invoke();
            }
            base.Hide();
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (m_CancelButton != null && m_CancelButton.onClick != null)
                m_CancelButton.onClick.AddListener(Hide);
        }

        protected virtual void UnregisterEvents()
        {
            if (m_CancelButton != null && m_CancelButton.onClick != null)
                m_CancelButton.onClick.RemoveListener(Hide);
        }

        #endregion

        #region Static Events

        public static bool IsEventRegistered<ActionType>(ActionType eventHandler, ActionType eventToCheck) where ActionType : System.Delegate
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

        #endregion

    }
}
