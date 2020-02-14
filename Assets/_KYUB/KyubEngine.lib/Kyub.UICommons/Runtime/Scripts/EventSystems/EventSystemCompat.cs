using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace Kyub.EventSystems
{
    public class EventSystemCompat : EventSystem
    {
        #region Private Variables

        [SerializeField] private int m_ReferenceDpi = 100;
        [SerializeField] private int m_BaseDragThreshold = 10;

        float _previousDPI = -1;

        #endregion

        #region Public Properties

        public int referenceDPI
        {
            get
            {
                return m_ReferenceDpi;
            }
            set
            {
                if (m_ReferenceDpi == value)
                    return;
                m_ReferenceDpi = value;
                TryUpdatePixelDrag(true);
            }
        }

        public int baseDragThreshold
        {
            get
            {
                return m_BaseDragThreshold;
            }
            set
            {
                if (m_BaseDragThreshold == value)
                    return;
                m_BaseDragThreshold = value;
                TryUpdatePixelDrag(true);
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            TryUpdatePixelDrag();
        }


        protected override void Update()
        {
            base.Update();
            TryUpdatePixelDrag();
        }

        #endregion

        #region Helper Functions

        public void TryUpdatePixelDrag(bool force = false)
        {
            if (_previousDPI != Screen.dpi || force)
            {
                UpdatePixelDrag_Internal(Screen.dpi);
            }
        }

        protected virtual void UpdatePixelDrag_Internal(float screenDpi)
        {
            _previousDPI = screenDpi;
            base.pixelDragThreshold = Mathf.RoundToInt(screenDpi / m_ReferenceDpi * m_BaseDragThreshold);
        }

        #endregion
    }
}