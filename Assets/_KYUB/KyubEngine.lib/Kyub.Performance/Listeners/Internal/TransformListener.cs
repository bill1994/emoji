using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Kyub.Performance
{
    [DisallowMultipleComponent]
    public abstract class TransformListener : MonoBehaviour
    {
        #region Private Variables

        [Header("Transform Listener Fields")]
        [SerializeField, Range(0, 60), Tooltip("Disable Listener if interval <= 0")]
        protected int m_intervalFramerate = 7;

        #endregion

        #region Callbacks

        [Space]
        public UnityEvent OnTransformHasChanged = new UnityEvent();

        #endregion

        #region Public Properties

        public int IntervalFramerate
        {
            get
            {
                return m_intervalFramerate;
            }

            set
            {
                if (m_intervalFramerate == value)
                    return;
                SetInterval_Internal(value);
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            if (m_intervalFramerate > 0)
                InitListener();
        }

        protected virtual void OnDisable()
        {
            CancelListener();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            m_intervalFramerate = Mathf.Max(0, m_intervalFramerate);
            if (m_intervalFramerate <= 0)
                CancelListener();
            else if (m_intervalFramerate > 0)
                InitListener();
        }
#endif

        #endregion

        #region Public Functions

        public virtual bool IsListeningTransform()
        {
            return m_intervalFramerate > 0;
        }

        #endregion

        #region Internal Helper Functions

        protected void SetInterval_Internal(int p_value)
        {
            if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
            {
                if (p_value <= 0 && m_intervalFramerate > 0)
                    CancelListener();
                else if (p_value > 0 && m_intervalFramerate <= 0)
                    InitListener();
            }
            m_intervalFramerate = Mathf.Max(0, p_value);
        }

        Coroutine _coroutine = null;
        protected void InitListener()
        {
            CancelListener();
            if(enabled && gameObject.activeInHierarchy)
                _coroutine = StartCoroutine(ListenerUpdateRoutine());
        }

        protected void CancelListener()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);
        }

        protected IEnumerator ListenerUpdateRoutine()
        {
            yield return null;
            while (true)
            {
                OnIntervalUpdate();
                //Cancel ListenerUpdate if interval <= zero
                if (m_intervalFramerate <= 0)
                    break;
                else
                {
                    var v_waitTime = 1 / m_intervalFramerate;
                    yield return new WaitForSecondsRealtime(v_waitTime);
                }
            }
        }

        protected virtual void OnIntervalUpdate()
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                OnBeforeTransformHasChanged();
                if (OnTransformHasChanged != null)
                    OnTransformHasChanged.Invoke();
            }
        }

        #endregion

        #region Receivers

        protected virtual void OnBeforeTransformHasChanged()
        {
        }

        #endregion
    }
}