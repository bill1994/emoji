using Kyub;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MaterialUI
{
    [DisallowMultipleComponent]
    public class MaterialDialogFrame : MaterialFrame, ISerializationCallbackReceiver
    {
        #region Private Variables

        [SerializeField]
        MaterialActivity m_activity = null;

        #endregion

        #region Public Properties

        public virtual MaterialActivity activity
        {
            get
            {
                if (m_activity == null)
                {
                    m_activity = this != null ? GetComponent<MaterialActivity>() : null;
                    if (m_activity != null)
                        OnAttachedActivityChanged(m_activity);
                }

                return m_activity;
            }
            protected internal set
            {
                if (m_activity == value || (value != null && !transform.IsChildOf(value.transform)))
                    return;
                m_activity = value;
                OnAttachedActivityChanged(m_activity);
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

        #region ISerialization Callbacks

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            //Validate activity After deserialize
            ApplicationContext.RunOnMainThread((GetHashCode() + "OnAfterDeserialize").GetHashCode(), () =>
            {
                if (this != null && m_activity != null && !transform.IsChildOf(m_activity.transform))
                    m_activity = null;
            });
        }

        #endregion
    }
}