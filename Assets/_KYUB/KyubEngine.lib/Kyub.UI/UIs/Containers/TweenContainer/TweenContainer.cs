using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Kyub.Collections;
using Kyub;

namespace Kyub.UI
{
    public enum PanelStateEnum { Opening, Closing, Opened, Closed }
    public enum CloseSpecialActionEnum { Nothing, Deactivate }

    public class TweenContainer : MonoBehaviour
    {
        #region Events

        public event System.Action OnOpening;
        public event System.Action OnOpened;
        public event System.Action OnClosing;
        public event System.Action OnClosed;

        #endregion

        #region Private Variables

        [SerializeField]
        PanelStateEnum _panelState = PanelStateEnum.Closed;
        PanelStateEnum m_panelState = PanelStateEnum.Closed;
        [SerializeField]
        CloseSpecialActionEnum m_closeSpecialAction = CloseSpecialActionEnum.Deactivate;
        [SerializeField]
        RestartEnum m_restartOption = RestartEnum.DontRestartIfRunning;
        [SerializeField]
        bool m_ignoreTimeScale = true;
        [SerializeField]
        bool m_enableTween = true;

        TimeTween _scheduler = null;

        #endregion

        #region UnityEvents Callback

        public UnityEvent OnOpeningCallBack;
        public UnityEvent OnOpenedCallBack;
        public UnityEvent OnClosingCallBack;
        public UnityEvent OnClosedCallBack;

        #endregion

        #region Public Properties

        public PanelStateEnum PanelState
        {
            get
            {
                if (!_awaked)
                    m_panelState = _panelState;
                return m_panelState;
            }
            protected set
            {
                if (_panelState == value && m_panelState == value)
                    return;
                _panelState = value;
                m_panelState = _panelState;
                if (Application.isPlaying)
                    ForceSetPanelStateValue(_panelState, false);
            }
        }

        public RestartEnum RestartOption { get { return m_restartOption; } set { m_restartOption = value; } }

        public bool IgnoreTimeScale { get { return m_ignoreTimeScale; } set { m_ignoreTimeScale = value; } }

        public bool EnableTween { get { return m_enableTween; } set { m_enableTween = value; } }

        public CloseSpecialActionEnum CloseSpecialAction
        {
            get { return m_closeSpecialAction; }
            set
            {
                if (m_closeSpecialAction == value)
                    return;
                m_closeSpecialAction = value;
            }
        }

        public TimeTween Tween
        {
            get
            {
                if (_scheduler == null)
                    _scheduler = this.GetComponent<TimeTween>();
                return _scheduler;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            if (!_awaked)
            {
                CorrectPanelState(false);
                ForceSetPanelStateValue(PanelState, true, true);
            }
            _awaked = false;
        }

        protected virtual void OnDisable()
        {
            if (PanelState != PanelStateEnum.Closed && CloseSpecialAction == CloseSpecialActionEnum.Deactivate && (!gameObject.activeSelf || !enabled))
                ForceSetPanelStateValue(PanelStateEnum.Closed, true, true);
        }

        bool _awaked = false;
        protected virtual void Awake()
        {
            if (Tween != null)
            {
                Tween.OnCycleFinishedExecution -= OnCycleFinishedExecution;
                Tween.OnCycleFinishedExecution += OnCycleFinishedExecution;
                Tween.OnCycleStartedExecution -= OnCycleStartedExecution;
                Tween.OnCycleStartedExecution += OnCycleStartedExecution;
            }

            if (!_awaked)
            {
                _awaked = true;
                CorrectPanelState(false);
                ForceSetPanelStateValue(PanelState, true);
            }
        }

        protected virtual void LateUpdate()
        {
            CorrectPanelState(true);
            ControlSpecialAction();
            CorrectTweenEnableOnOpeningOrClosing();
            CheckNeedActivate();
        }

        #endregion

        #region Events Receiver

        protected virtual void OnCycleStartedExecution(CycleEventArgs p_args)
        {
            if (EnableTween)
            {
                if (p_args.IsPing)
                {
                    ForceSetPanelStateValue(PanelStateEnum.Opening, false);
                }
                else
                {
                    ForceSetPanelStateValue(PanelStateEnum.Closing, false);
                }
            }
        }

        protected virtual void OnCycleFinishedExecution(CycleEventArgs p_args)
        {
            if (EnableTween)
            {
                if (p_args.IsPing)
                {
                    ForceSetPanelStateValue(PanelStateEnum.Opened, false);
                }
                else
                {
                    ForceSetPanelStateValue(PanelStateEnum.Closed, false);
                }
            }
        }

        #endregion

        #region Helper Functions

        private void CorrectTweenEnableOnOpeningOrClosing()
        {
            if (EnableTween && Tween != null && !Tween.enabled && (PanelState == PanelStateEnum.Opening || PanelState == PanelStateEnum.Closing))
            {
                Tween.enabled = true;
            }
        }

        private void CorrectPanelState(bool p_callLogic)
        {
            if (_panelState != m_panelState)
            {
                if (p_callLogic)
                    ForceSetPanelStateValue(_panelState, true);
                else
                    m_panelState = _panelState;
            }
        }

        #region PanelState Functions

        public void ForceSetPanelStateValue(PanelStateEnum p_value)
        {
            ForceSetPanelStateValue(p_value, true);
        }

        public void ForceSetPanelStateValue(PanelStateEnum p_value, bool p_updateEffect, bool p_underDisableOrEnable = false)
        {
            bool p_changed = CheckIfStateWillChange(p_value);
            m_panelState = p_value;
            _panelState = m_panelState;

            if (p_changed)
            {
                if (CloseSpecialAction != CloseSpecialActionEnum.Deactivate || m_panelState != PanelStateEnum.Closed)
                {
                    if (!p_underDisableOrEnable)
                        SetActiveAndEnable(true, true);
                    if (Application.isPlaying)
                        gameObject.SendMessage("OnPanelStateChanged", m_panelState, SendMessageOptions.DontRequireReceiver);
                }
            }
            if (Application.isPlaying && (CloseSpecialAction != CloseSpecialActionEnum.Deactivate || m_panelState != PanelStateEnum.Closed))
                gameObject.SendMessage("CallEventsInternal", m_panelState, SendMessageOptions.DontRequireReceiver);
            else
                CallEventsInternal(m_panelState);

            if (p_updateEffect)
                UpdateEffect();

            //Used to Prevent Bugs while activating
            if (p_underDisableOrEnable)
            {
                if (Application.isPlaying && (CloseSpecialAction != CloseSpecialActionEnum.Deactivate || m_panelState != PanelStateEnum.Closed))
                {
                    gameObject.SendMessage("CallControlSpecialAction", SendMessageOptions.DontRequireReceiver);
                    gameObject.SendMessage("UpdateObjectsToShowOrHideByState", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    CallControlSpecialAction();
                }
            }
            else
            {
                CallControlSpecialAction();
                if (!gameObject.activeSelf)
                {
                    if (Application.isPlaying)
                    {
                        RuntimeContext.RunOnMainThread(() =>
                        {
                            if (this != null)
                                CheckNeedActivate();
                        }, 0.01f);
                    }
                }
            }
            if (!Application.isPlaying)
            {
                ControlSpecialAction();
                CheckNeedActivate();
            }

            if ((Tween == null || !EnableTween) && 
                (p_value == PanelStateEnum.Opening || p_value == PanelStateEnum.Closing))
            {
                if (p_value == PanelStateEnum.Opening)
                    p_value = PanelStateEnum.Opened;
                if (p_value == PanelStateEnum.Closing)
                    p_value = PanelStateEnum.Closed;
                ForceSetPanelStateValue(p_value, p_updateEffect, p_underDisableOrEnable);
            }
        }

        protected virtual void CallEventsInternal(PanelStateEnum p_panelState)
        {
            if (p_panelState == PanelStateEnum.Opened)
            {
                if (OnOpened != null)
                    OnOpened();
                if (OnOpenedCallBack != null)
                    OnOpenedCallBack.Invoke();
            }
            else if (p_panelState == PanelStateEnum.Closed)
            {
                if (OnClosed != null)
                    OnClosed();
                if (OnClosedCallBack != null)
                    OnClosedCallBack.Invoke();
            }
            else if (p_panelState == PanelStateEnum.Opening)
            {
                if (OnOpening != null)
                    OnOpening();
                if (OnOpeningCallBack != null)
                    OnOpeningCallBack.Invoke();
            }
            else if (p_panelState == PanelStateEnum.Closing)
            {
                if (OnClosing != null)
                    OnClosing();
                if (OnClosingCallBack != null)
                    OnClosingCallBack.Invoke();
            }
        }

        #region Internal PanelState Functions

        protected void SetActiveAndEnable(bool p_active = true, bool p_enabled = true)
        {
            if (gameObject.activeSelf != p_active)
                gameObject.SetActive(p_active);
            if (enabled != p_enabled)
                enabled = p_enabled;
        }

        protected bool CheckIfStateWillChange(PanelStateEnum p_value)
        {

            bool v_return = m_panelState != p_value ? true : false;
            /*if(RestartOption == RestartEnum.DontRestartIfRunning)
            {
                if(m_panelState == PanelStateEnum.Closing || m_panelState == PanelStateEnum.Opening)
                    v_return = false;
            }*/
            if ((m_panelState == PanelStateEnum.Closed && p_value == PanelStateEnum.Closing)
               || (m_panelState == PanelStateEnum.Opened && p_value == PanelStateEnum.Opening))
            {
                v_return = false;
            }

            return v_return;
        }

        protected void CheckNeedActivate()
        {
            if (_needActivate != null && _needActivate.Key)
            {
                _needActivate.Key = false;
                if (gameObject.activeSelf != _needActivate.Value)
                    gameObject.SetActive(_needActivate.Value);
            }
        }

        bool _canCallControlSpecialAction = false;
        protected void CallControlSpecialAction()
        {
            CallControlSpecialAction(true);
        }

        protected void CallControlSpecialAction(bool p_forceEnable)
        {
            _canCallControlSpecialAction = true;
            if ((!gameObject.activeSelf || !enabled) && p_forceEnable)
            {
                ControlSpecialAction();
            }
        }

        protected void ControlSpecialAction()
        {
            ControlSpecialAction(false);
        }

        KVPair<bool, bool> _needActivate = new KVPair<bool, bool>();
        protected void ControlSpecialAction(bool p_forceExecution)
        {
            if (_canCallControlSpecialAction || p_forceExecution)
            {
                try
                {
                    _canCallControlSpecialAction = false;
                    if (!enabled)
                        enabled = true;
                    if (m_panelState == PanelStateEnum.Closed)
                    {
                        if (CloseSpecialAction == CloseSpecialActionEnum.Deactivate)
                        {
                            if (gameObject.activeSelf)
                            {
                                _needActivate.Key = true;
                                _needActivate.Value = false;
                                //gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            if (!gameObject.activeSelf)
                            {
                                _needActivate.Key = true;
                                _needActivate.Value = true;
                                //gameObject.SetActive(true);
                            }
                        }
                    }
                    else
                    {
                        if (!gameObject.activeSelf)
                        {
                            _needActivate.Key = true;
                            _needActivate.Value = true;
                            //gameObject.SetActive(true);
                        }
                    }
                }
                catch
                {
                    CallControlSpecialAction(false);
                }
            }
        }

        protected void UpdateEffect()
        {
            //Active Tweens
            if (Tween != null && EnableTween)
            {
                Tween.TimerStyle = TimerStyle.OneTimeOnly;
                Tween.Loop = false;

                bool v_visibility = true;
                bool v_forceFinish = true;
                if (PanelState == PanelStateEnum.Opened)
                {
                    v_visibility = true;
                    v_forceFinish = true;
                }
                else if (PanelState == PanelStateEnum.Opening)
                {
                    v_visibility = true;
                    v_forceFinish = false;
                }
                else if (PanelState == PanelStateEnum.Closed)
                {
                    v_visibility = false;
                    v_forceFinish = true;
                }
                else if (PanelState == PanelStateEnum.Closing)
                {
                    v_visibility = false;
                    v_forceFinish = false;
                }

                Tween.IgnoreTimeScale = IgnoreTimeScale;
                if (v_forceFinish)
                {
                    Tween.ForceFinish(v_visibility, true);
                }
                else
                {
                    Tween.StartTimer(v_visibility, true, RestartOption);
                }
            }
        }

        #endregion

        #endregion

        #region Visibility Functions

        /// <summary>
        /// This function will can Show() if "true" or Hide if "false"
        /// </summary>
        /// <param name="p_active"></param>
        public void SetContainerActive(bool p_active)
        {
            if (p_active)
                Show();
            else
                Hide();
        }

        public void ShowIfClosedOrClosing()
        {
            ShowIfClosedOrClosing(false);
        }

        //Dont restart if state is equal Opening/Opened
        public void ShowIfClosedOrClosing(bool p_forceFinish)
        {
            if (!Application.isPlaying || (PanelState != PanelStateEnum.Opening && PanelState != PanelStateEnum.Opened))
                Show(p_forceFinish);
        }

        public void Show()
        {
            Show(false);
        }

        public void Show(bool p_forceFinish)
        {
            _awaked = true;
            if (!Application.isPlaying)
                p_forceFinish = true;
            if (PanelState != PanelStateEnum.Opened)
            {
                if (p_forceFinish)
                    ForceSetPanelStateValue(PanelStateEnum.Opened, true);
                else if (RestartOption != RestartEnum.DontRestartIfRunning || !Application.isPlaying ||
                        (PanelState != PanelStateEnum.Opening && PanelState != PanelStateEnum.Closing))
                    ForceSetPanelStateValue(PanelStateEnum.Opening, true);
            }
        }

        public void HideIfOpenedOrOpening()
        {
            HideIfOpenedOrOpening(false);
        }

        //Dont restart if state is equal Closing/Closed
        public void HideIfOpenedOrOpening(bool p_forceFinish)
        {
            if (!Application.isPlaying || (PanelState != PanelStateEnum.Closing && PanelState != PanelStateEnum.Closed))
                Hide(p_forceFinish);
        }

        public void Hide()
        {
            Hide(false);
        }

        public void Hide(bool p_forceFinish)
        {
            _awaked = false;
            if (!Application.isPlaying)
                p_forceFinish = true;
            if (PanelState != PanelStateEnum.Closed)
            {
                if (p_forceFinish)
                    ForceSetPanelStateValue(PanelStateEnum.Closed, true);
                else if (RestartOption != RestartEnum.DontRestartIfRunning || !Application.isPlaying
                        || (PanelState != PanelStateEnum.Opening && PanelState != PanelStateEnum.Closing))
                    ForceSetPanelStateValue(PanelStateEnum.Closing, true);
            }
        }

        #endregion

        #endregion
    }
}

