using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MaterialUI
{
    [DisallowMultipleComponent]
    public class MaterialDialogFrame : MaterialFrame
    {
        #region Private Variables

        MaterialActivity _activity = null;

        #endregion

        #region Public Properties

        public virtual MaterialActivity activity
        {
            get
            {
                if (_activity == null)
                {
                    _activity = this != null ? GetComponent<MaterialActivity>() : null;
                    if(_activity != null)
                        OnAttachedActivityChanged(_activity);
                }
                return _activity;
            }
            protected internal set
            {
                if (_activity == value)
                    return;
                _activity = value;
                OnAttachedActivityChanged(_activity);
            }
        }

        #endregion

        #region Callbacks

        [UnityEngine.Serialization.FormerlySerializedAs("OnShowAnimationOver")]
        public UnityEvent onShow = new UnityEvent();
        [UnityEngine.Serialization.FormerlySerializedAs("OnHideAnimationOver")]
        public UnityEvent onHide = new UnityEvent();

        #endregion

        #region Public Functions

        public virtual void ShowModal()
        {
            MaterialDialogActivity materialActivity = activity as MaterialDialogActivity;
            if (materialActivity != null)
                materialActivity.isModal = true;
            Show();
        }

        public virtual void Show()
        {
            if (activity != null)
                activity.Show();
            else
                Debug.LogWarning("[MaterialDialogFrame] Can only show if is part of a 'MaterialActivity'");
        }

        public virtual void Hide()
        {
            if (activity != null)
                activity.Hide();
            else
                Debug.LogWarning("[MaterialDialogFrame] Can only hide if is part of a 'MaterialActivity'");
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void OnAttachedActivityChanged(MaterialActivity activity)
        {
        }

        #endregion
    }
}