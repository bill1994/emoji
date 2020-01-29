using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [ExecuteInEditMode]
    public class SplittedProgressIndicator : ProgressIndicator
    {
        #region Private Variables

        [Space]
        [SerializeField]
        float m_animationDuration = 0.65f;
        [SerializeField]
        List<Image> m_stages = new List<Image>();

        int _animBar = 0; // 0 ==  NoAnim, 1 == Hide, 2 == Show

        float _lastFillAmount = 0;
        float _animBarStartTime = 0;

        #endregion

        #region Public Properties

        public List<Image> Stages
        {
            get
            {
                return m_stages;
            }

            set
            {
                m_stages = value;
            }
        }

        public float AnimationDuration
        {
            get
            {
                return m_animationDuration;
            }

            set
            {
                m_animationDuration = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;

            if (m_StartsIndeterminate)
            {
                StartIndeterminate();
            }
        }


        protected virtual void Update()
        {
            UpdateAnimBar();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SetProgress(m_CurrentProgress);
            }
#endif
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetProgress(m_CurrentProgress);
        }
#endif

        #endregion

        #region Animation Functions

        private void UpdateAnimBar()
        {
            if (rectTransform == null) return;
            if (_animBar == 0) return;

            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            if (_animBarStartTime < 0)
                _animBarStartTime = Time.realtimeSinceStartup;
            float deltaTime = Time.realtimeSinceStartup - _animBarStartTime;
            //Hide
            if (_animBar == 1)
            {
                if (deltaTime < AnimationDuration)
                {
                    _lastFillAmount = Tween.CubeOut(_lastFillAmount, 0, deltaTime, AnimationDuration);
                }
                else
                {
                    _lastFillAmount = 0;
                    _animBar = 0;
                }
            }
            //Show
            else if (_animBar == 2)
            {
                if (deltaTime < AnimationDuration)
                {
                    _lastFillAmount = Tween.CubeIn(_lastFillAmount, m_CurrentProgress, deltaTime, AnimationDuration);
                }
                else
                {
                    _lastFillAmount = m_CurrentProgress;
                    _animBar = 0;
                }
            }
            UpdateStages(_lastFillAmount);
        }

        #endregion

        protected virtual void UpdateStages(float progress)
        {
            progress = Mathf.Clamp(progress, 0f, 1f);
            var deltaState = m_stages.Count == 0? 0 : 1.0f / m_stages.Count;

            for (int i = 0; i < m_stages.Count; i++)
            {
                if (m_stages[i] != null)
                {
                    var initialState = deltaState * i;
                    var normalized = Mathf.Clamp((progress - initialState) * m_stages.Count, 0, 1);
                    m_stages[i].fillAmount = normalized;
                }
            }
        }

        public override void SetProgress(float progress, bool animated)
        {
            if (rectTransform == null) return;

            progress = Mathf.Clamp(progress, 0f, 1f);
            currentProgress = progress;
            m_IsAnimatingIndeterminate = false;

            if (!animated || !Application.isPlaying)
            {
                _animBar = 0;
                _lastFillAmount = m_CurrentProgress;
                UpdateStages(m_CurrentProgress);
            }
            //Show
            else
            {
                Show();
            }
        }

        public override void StartIndeterminate()
        {
            Show(true);
        }

        public override void Show(bool startIndeterminate)
        {
            if (startIndeterminate)
            {
                _lastFillAmount = Random.Range(0, m_CurrentProgress);
            }
            _animBarStartTime = -1;
            _animBar = 2;
        }

        public override void Hide()
        {
            if (scaledRectTransform == null) return;

            _animBarStartTime = -1;
            _animBar = 1;
        }
    }
}
