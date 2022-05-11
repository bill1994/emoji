using UnityEngine;
using System.Collections;

namespace Kyub.Async
{
    public class ExternalResourcesReceiver : DirtyBehaviour
    {
        #region Private Variables

        [SerializeField]
        protected string m_key = "";

        #endregion

        #region Public Properties

        public virtual string Key
        {
            get
            {
                if (m_key == null)
                    m_key = "";
                return m_key;
            }
            set
            {
                value = value == null ? "" : value;
                if (m_key == value)
                    return;
                if (Application.isPlaying)
                    UnregisterReceiver();
                m_key = value;
                if (Application.isPlaying)
                    RegisterReceiver();
                SetDirty();
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            RegisterReceiver();
        }

        protected virtual void OnDestroy()
        {
            UnregisterReceiver();
        }

        #endregion

        #region Helper Functions

        protected virtual void RegisterReceiver()
        {
            ExternalResources.RegisterReceiver(this);
        }

        protected virtual void UnregisterReceiver()
        {
            ExternalResources.UnregisterReceiver(this);
        }

        #endregion
    }
}
