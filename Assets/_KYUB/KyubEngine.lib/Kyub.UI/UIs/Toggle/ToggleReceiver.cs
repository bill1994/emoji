using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class ToggleReceiver : MonoBehaviour
    {
        #region Helper Functions

        public enum EventOnEnableModeEnum { OnEnable, OnStart, All }

        #endregion

        #region Private Variables

        [SerializeField]
        bool m_callEventsOnEnable = false;
        [SerializeField]
        float m_eventOnEnableDelay = 0;
        [SerializeField, Tooltip("Define if will call toggle event OnStart, OnEnable or in both cases when 'CallEventsOnEnable' is on")]
        EventOnEnableModeEnum m_eventOnEnableMode = EventOnEnableModeEnum.All;
        [SerializeField]
        bool m_ignoreTimeScale = false;

        #endregion

        #region Callbacks

        public UnityEvent OnToggleOn;
        public UnityEvent OnToggleOff;

        #endregion

        #region Public Properties

        public bool CallEventsOnEnable
        {
            get
            {
                return m_callEventsOnEnable;
            }
            set
            {
                if (m_callEventsOnEnable == value)
                    return;
                m_callEventsOnEnable = value;
            }
        }

        public EventOnEnableModeEnum EventOnEnableMode
        {
            get
            {
                return m_eventOnEnableMode;
            }
            set
            {
                if (m_eventOnEnableMode == value)
                    return;
                m_eventOnEnableMode = value;
            }
        }

        #endregion

        #region Constructor

        /*public ToggleReceiver()
        {
            GlobalTween.CallFunctionWhenOutOfMainThread(new System.Action(Awake), 0f); //Force Call this Event when gameobject is disabled when unity start scene!
        }*/

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            _oldToggle = GetToggleValue();
            if (enabled && Application.isPlaying)
                RegisterEvents();
        }

        protected virtual void OnEnable()
        {
            if (enabled && Application.isPlaying)
                RegisterEvents();
            if (m_callEventsOnEnable && (m_eventOnEnableMode == EventOnEnableModeEnum.OnEnable || (_started && m_eventOnEnableMode == EventOnEnableModeEnum.All)))
                Init();
        }

        protected bool _started = true;
        protected virtual void Start()
        {
            _started = true;
            if(m_callEventsOnEnable && (m_eventOnEnableMode == EventOnEnableModeEnum.All || m_eventOnEnableMode == EventOnEnableModeEnum.OnStart))
                Init();
        }

        protected virtual void OnDisable()
        {
            StopCoroutine("TryCallEventOnEnableDisableRoutine");
            if(m_callEventsOnEnable)
                TryCallEventOnEnableDisable();
            if (!enabled && Application.isPlaying)
                UnregisterEvents();
        }

        protected virtual void OnDestroy()
        {
            if (Application.isPlaying)
                UnregisterEvents();
        }

        protected virtual void Update()
        {
            if (!Application.isPlaying && Application.isEditor)
                CheckToggle();
        }

        #endregion

        #region Events Receiver

        public void OnToggleChanged(bool p_toggleActive)
        {
            if (_oldToggle != p_toggleActive)
            {
                _oldToggle = p_toggleActive;
                if (p_toggleActive)
                {
                    if (OnToggleOn != null)
                        OnToggleOn.Invoke();
                }
                else
                {
                    if (OnToggleOff != null)
                        OnToggleOff.Invoke();
                }
            }
        }

        public void OnSliderChanged(float p_slider)
        {
            OnToggleChanged(p_slider != 0);
        }

        #endregion

        #region Helper Functions

        protected virtual void Init()
        {
            if (Application.isPlaying && gameObject.activeInHierarchy && enabled && m_eventOnEnableDelay > 0)
            {
                StopCoroutine("TryCallEventOnEnableDisableRoutine");
                StartCoroutine("TryCallEventOnEnableDisableRoutine");
            }
            else
                TryCallEventOnEnableDisable();
        }

        protected IEnumerator TryCallEventOnEnableDisableRoutine()
        {
            if (Application.isPlaying)
            {
                if (m_eventOnEnableDelay > 0)
                {
                    if (m_ignoreTimeScale)
                        yield return new WaitForSecondsRealtime(m_eventOnEnableDelay);
                    else
                        yield return new WaitForSeconds(m_eventOnEnableDelay);
                }
                TryCallEventOnEnableDisable();
            }
        }

        protected virtual void TryCallEventOnEnableDisable()
        {
            if (Application.isPlaying)
            {
                UnregisterEvents();
                _oldToggle = !GetToggleValue();
                OnToggleChanged(!_oldToggle); //We Must Force Call Event
                if(enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
            }
        }

        public void RegisterEvents()
        {
            UnregisterEvents();
            Toggle v_toggle = GetComponent<Toggle>();
            Slider v_slider = GetComponent<Slider>();
            if (v_toggle == null && v_slider == null)
            {
                v_toggle = GetComponentInParent<Toggle>();
                v_slider = GetComponentInParent<Slider>();
            }
            if (v_toggle != null)
            {
                v_toggle.onValueChanged.AddListener(OnToggleChanged);
            }
            else if (v_slider != null)
            {
                v_slider.onValueChanged.AddListener(OnSliderChanged);
            }
        }

        public void UnregisterEvents()
        {
            Toggle v_toggle = GetComponent<Toggle>();
            Slider v_slider = GetComponent<Slider>();
            if (v_toggle != null)
            {
                v_toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
            if (v_slider != null)
            {
                v_slider.onValueChanged.RemoveListener(OnSliderChanged);
            }
        }

        bool _oldToggle = false;
        protected virtual void CheckToggle()
        {
            bool v_getToggle = GetToggleValue();
            if (_oldToggle != v_getToggle)
            {
                OnToggleChanged(v_getToggle);
                _oldToggle = v_getToggle;
            }
        }

        public virtual bool GetToggleValue()
        {
            Toggle v_toggle = GetComponent<Toggle>();
            Slider v_slider = GetComponent<Slider>();
            if (v_toggle == null && v_slider == null)
            {
                v_toggle = GetComponentInParent<Toggle>();
                v_slider = GetComponentInParent<Slider>();
            }
            if (v_toggle != null)
                return v_toggle.isOn;
            if (v_slider != null)
                return (v_slider.value != 0);
            return false;
        }

        public virtual void SwapToggleValue()
        {
            SetToggleValue(!GetToggleValue());
        }

        public virtual void SetToggleValue(bool p_value)
        {
            SetToggleValue(p_value, true);
        }

        public virtual void SetToggleValue(bool p_value, bool p_callEvents)
        {
            if (!p_callEvents)
                UnregisterEvents();
            else
                RegisterEvents();

            Toggle v_toggle = GetComponent<Toggle>();
            Slider v_slider = GetComponent<Slider>();
            if (v_toggle == null && v_slider == null)
            {
                v_toggle = GetComponentInParent<Toggle>();
                v_slider = GetComponentInParent<Slider>();
            }
            if (v_toggle != null)
            {
                v_toggle.isOn = p_value;
                if (Application.isPlaying && p_callEvents)
                    CheckToggle();
            }
            if (v_slider != null)
            {
                v_slider.value = p_value ? 1 : 0;
                if (Application.isPlaying && p_callEvents)
                    CheckToggle();
            }
            if (!p_callEvents)
                RegisterEvents();
        }

        #endregion

    }
}
