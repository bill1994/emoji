using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Kyub.EventSystems
{
    public class EventSystemCompat : EventSystem
    {
        #region Static Functions

#if UNITY_WEBGL && !UNITY_EDITOR

        [DllImport("__Internal")]
        private static extern float _JSGetScreenDPI();
#endif

        public static float GetScreenDPI(float defaultValue = 96)
        {
            float value = 0;
#if UNITY_WEBGL && !UNITY_EDITOR
            value = _JSGetScreenDPI();
#else
            value = Screen.dpi;
#endif
            if (value <= 0)
                value = defaultValue;

            return value;
        }

        #endregion

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
            var screenDpi = EventSystemCompat.GetScreenDPI();
            if (_previousDPI != screenDpi || force)
            {
                UpdatePixelDrag_Internal(screenDpi);
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