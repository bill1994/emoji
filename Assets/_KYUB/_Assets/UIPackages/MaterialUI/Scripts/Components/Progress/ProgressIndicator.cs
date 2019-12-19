//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MaterialUI
{
    public class ProgressIndicator : MaterialStylePanel, ILayoutElement
    {
        [System.Serializable]
        public class FloatUnityEvent : UnityEvent<float> { }

        #region Private Variables

        [SerializeField]
        protected bool m_StartsIndeterminate = true;
        [SerializeField]
        protected bool m_StartsHidden = false;

        [SerializeField]
        [Range(0f, 1f)]
        protected float m_CurrentProgress = 0;
        [SerializeField]
        private RectTransform m_BaseObjectOverride = null;

        private RectTransform m_RectTransform;
        protected bool m_IsAnimatingIndeterminate;

        #endregion

        #region Callback

        public FloatUnityEvent onValueChanged = new FloatUnityEvent();

        #endregion

        #region Public Properties

        public float currentProgress
        {
            get { return m_CurrentProgress; }
            protected set
            {
                if (m_CurrentProgress == value)
                    return;
                m_CurrentProgress = value;

                if (onValueChanged != null)
                    onValueChanged.Invoke(m_CurrentProgress);
            }
        }

        public RectTransform baseObjectOverride
        {
            get { return m_BaseObjectOverride; }
            set { m_BaseObjectOverride = value; }
        }

        protected RectTransform scaledRectTransform
        {
            get { return m_BaseObjectOverride != null ? m_BaseObjectOverride : rectTransform; }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (this != null && m_RectTransform == null)
                {
                    m_RectTransform = (RectTransform)transform;
                }
                return m_RectTransform;
            }
        }

        #endregion

        #region Helper Functions

        public void Show() { Show(m_StartsIndeterminate);  }
        public virtual void Show(bool startIndeterminate) { }
        public virtual void Hide() { }
        public virtual void StartIndeterminate() { }
        public virtual void SetProgress(float progress, bool animated) { }
        public virtual void SetColor(Color color) { }

        public void SetProgressImmediate(float progress)
        {
            SetProgress(progress, false);
        }

        public void SetProgress(float progress)
        {
            SetProgress(progress, true);
        }

        #endregion

        #region Layout Functions

        public virtual float GetMinWidth() { return -1; }
        public virtual float GetMinHeight() { return -1; }

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float preferredWidth { get { return -1; } }
        public float minWidth { get { return GetMinWidth(); } }
        public float flexibleWidth { get { return -1; } }
        public float preferredHeight { get { return -1; } }
        public float minHeight { get { return GetMinHeight(); } }
        public float flexibleHeight { get { return -1; } }
        public int layoutPriority { get { return -1; } }

        #endregion
    }
}