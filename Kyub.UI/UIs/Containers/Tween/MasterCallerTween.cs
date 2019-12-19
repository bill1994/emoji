using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kyub.UI
{
    //TODO Turn this Control More Flexible
    // * Need Accept PingPong
    // * Need Accept Loop
    // * Need Check Other Master Tweens (Circular Reference)
    // * Need Correct PongTime To Call Objects In Correct Order
    // * Need To Accept Slave Tweens with PingPong without break logic
    // * Need an InspectorEditor
    public class MasterCallerTween : TimeTween
    {
        #region Private Variables

        List<TimeTween> m_schedulersList = new List<TimeTween>();

        #endregion

        #region Public Properties

        public List<TimeTween> TweensList
        {
            get
            {
                if (m_schedulersList == null)
                    m_schedulersList = new List<TimeTween>();
                return m_schedulersList;
            }
            protected set
            {
                m_schedulersList = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            TweensList = GetAllPossibleSlaveTweensInObject();
            UpdateMaxTime();
            base.OnEnable();
        }

        #endregion

        #region Helper Functions

        protected List<TimeTween> GetAllPossibleSlaveTweensInObject()
        {
            List<TimeTween> m_unfilteredList = new List<TimeTween>(GetComponents<TimeTween>());
            if (m_unfilteredList.Contains(this))
                m_unfilteredList.Remove(this);
            return m_unfilteredList;
        }

        public void UpdateMaxTime()
        {
            float v_finalDuration = 0;
            //Math of All Slave Tweens Duration
            foreach (TimeTween v_scheduler in TweensList)
            {
                float v_currentTweenDuration = 0;
                if (v_scheduler != null)
                {
                    //Dont Accept Loops in slave Tweens
                    if (v_scheduler.Loop)
                        v_scheduler.Loop = false;
                    float v_initialDelay = v_scheduler.InitialDelay;
                    float v_initialPingDelay = v_scheduler.IsPing || v_scheduler.TimerStyle == TimerStyle.PingPong ? v_scheduler.PingDelayTime : 0;
                    float v_initialPongDelay = !v_scheduler.IsPing || v_scheduler.TimerStyle == TimerStyle.PingPong ? v_scheduler.PongDelayTime : 0;
                    float v_duration = v_scheduler.TimerStyle == TimerStyle.PingPong ? v_scheduler.MaxTime * 2 : v_scheduler.MaxTime;
                    v_currentTweenDuration = v_initialDelay + v_initialPingDelay + v_initialPongDelay + v_duration;
                }
                v_finalDuration = Mathf.Max(v_finalDuration, v_currentTweenDuration);
            }
            MaxTime = v_finalDuration;
        }

        /*public void SetIsPingValueInSlaves(bool p_isPing)
        {
            foreach(TimeTween v_scheduler in TweensList)
                v_scheduler.IsPing = p_isPing;
        }*/

        public void CallAllSlaveTweens()
        {
            foreach (TimeTween v_scheduler in TweensList)
            {
                v_scheduler.IgnoreTimeScale = IgnoreTimeScale;
                v_scheduler.ForceFinishOnDisable = ForceFinishOnDisable;
                v_scheduler.StartTimer(IsPing, true, RestartOption);
            }
        }

        #endregion

        #region Inherited Functions

        protected override void OnPingStart()
        {
            //UpdateMaxTime();
            CallAllSlaveTweens();
        }

        protected override void OnPongStart()
        {
            //UpdateMaxTime();
            CallAllSlaveTweens();
        }

        #endregion
    }
}
