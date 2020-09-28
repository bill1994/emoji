using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace Kyub.UI
{

    public class ScrollDataViewElement : MonoBehaviour, IDelayedReloadableDataViewElement
    {
        #region Private Variables

        //[SerializeField, Tooltip("Apply reload in next cycle (can cause layout inconsistencies)")]
        //private bool m_delayedReload = false;

        protected ScrollDataView.ReloadEventArgs _cachedReloadEventArgs = new ScrollDataView.ReloadEventArgs() { LayoutElementIndex = -1, DataIndex = -1 };

        #endregion

        #region Callbacks

        public UnityEvent OnReload = new UnityEvent();

        #endregion

        #region Public Properties

        public int LayoutElementIndex
        {
            get
            {
                return _cachedReloadEventArgs.LayoutElementIndex;
            }
        }

        public ScrollDataView DataView
        {
            get
            {
                if (_cachedReloadEventArgs.Sender == null)
                    _cachedReloadEventArgs.Sender = GetComponentInParent<ScrollDataView>();
                return _cachedReloadEventArgs.Sender;
            }
        }

        public object Data
        {
            get
            {
                return _cachedReloadEventArgs.Data;
            }
        }

        public int DataIndex
        {
            get
            {
                return _cachedReloadEventArgs.DataIndex;
            }
        }

        public ScrollDataView Sender
        {
            get
            {
                return _cachedReloadEventArgs.Sender;
            }
        }

        /*public bool DelayedReload
        {
            get
            {
                return m_delayedReload;
            }

            set
            {
                if (m_delayedReload == value)
                    return;
                m_delayedReload = value;
            }
        }*/

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            //_reloadRoutine = null;
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Force Reload based in last cached event received
        /// </summary>
        public void Reload()
        {
            Reload(_cachedReloadEventArgs);
        }

        #endregion

        #region Internal Helper Functions

        //Coroutine _reloadRoutine = null;
        protected void Reload(ScrollDataView.ReloadEventArgs args)
        {
            var oldArgs = _cachedReloadEventArgs;
            _cachedReloadEventArgs = args;

            ApplyReload(oldArgs, args);
            if (OnReload != null)
                OnReload.Invoke();

            //Cancel previous reloads
            /*if (_reloadRoutine != null)
            {
                StopCoroutine(_reloadRoutine);
                _reloadRoutine = null;
            }

            var routine = ReloadRoutine(oldArgs, args);
            //Execure Delayed
            if (m_delayedReload && enabled && gameObject.activeInHierarchy)
            {
                _reloadRoutine = StartCoroutine(routine);
            }
            else
            {
                //Execute Synchronously
                while (routine.MoveNext()) { }
            }*/
        }

        /*protected virtual IEnumerator ReloadRoutine(ScrollDataView.ReloadEventArgs oldArgs, ScrollDataView.ReloadEventArgs newArgs)
        {
            yield return null;
            ApplyReload(oldArgs, newArgs);
            if (OnReload != null)
                OnReload.Invoke();

            _reloadRoutine = null;
        }*/

        /// <summary>
        /// Override this function to implement your own custom logic to reload
        /// </summary>
        protected virtual void ApplyReload(ScrollDataView.ReloadEventArgs oldArgs, ScrollDataView.ReloadEventArgs newArgs)
        {
        }

        protected virtual bool IsReloading()
        {
            return false; //_reloadRoutine != null;
        }

        #endregion

        #region Interface Implementations

        bool IDelayedReloadableDataViewElement.IsReloading()
        {
            return IsReloading();
        }

        void IReloadableDataViewElement.Reload(ScrollDataView.ReloadEventArgs args)
        {
            Reload(args);
        }

        bool IReloadableDataViewElement.IsDestroyed()
        {
            return this == null;
        }

        #endregion
    }
}
