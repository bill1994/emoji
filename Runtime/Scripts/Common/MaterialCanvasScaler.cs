using Kyub.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialCanvasScaler : CanvasScaler
    {
        public enum ReferenceOrientations { None = 0, Portrait = 1, Landscape = 2 }

        [System.Serializable]
        public class CanvasAreaChangedEvent : UnityEvent<bool, bool> { }

        #region Private Variables

#if UNITY_EDITOR

        /// <summary>
        /// Should a fake DPI be used in the editor?
        /// <para></para>
        /// This helps when designing the UI.
        /// </summary>
        [SerializeField]
        protected bool m_EditorForceDPI = true;

        /// <summary>
        /// What DPI should be forced in the editor, if enabled?
        /// </summary>
        [SerializeField]
        protected float m_EditorForceDPIValue = 160f;

#endif

        [Tooltip("Use unity physical size calculation")]
        [SerializeField] protected bool m_UseLegacyPhysicalSizeCalc = false;

        [Tooltip("Activate safe area scaling")]
        [SerializeField] protected bool m_SupportSafeArea = true;

        [Tooltip("This property will be used to know if we must keep pixel size.\nWhen the device orientation was not inside this reference property, we will calculate scaling based in a default orientation (Keeping elements in screen with same pixel size)")]
        [SerializeField] protected ReferenceOrientations m_DefaultScalingOrientation = ReferenceOrientations.None;

        [System.NonSerialized]
        private Canvas _Canvas;
        [System.NonSerialized]
        private bool _PrevIsWideScreen = false;
        [System.NonSerialized]
        protected float _PrevScaleFactor = 1;
        [System.NonSerialized]
        protected float _PrevReferencePixelsPerUnit = 100;

        #endregion

        #region Callbacks

        public CanvasAreaChangedEvent onCanvasAreaChanged = new CanvasAreaChangedEvent();

        #endregion

        #region Properties

        public ReferenceOrientations defaultScalingOrientation
        {
            get
            {
                return m_DefaultScalingOrientation;
            }
            set
            {
                if (m_DefaultScalingOrientation == value)
                    return;
                m_DefaultScalingOrientation = value;
            }
        }

        public Vector2 safeScreenSize
        {
            get
            {
                var safeAreaComponent = GetComponent<CanvasSafeArea>();
                Vector2 screenSize = safeAreaComponent != null && m_SupportSafeArea ? safeAreaComponent.GetConformSafeArea().size : new Vector2(Screen.width, Screen.height);

                return screenSize;
            }
        }

        public bool isWideScreen
        {
            get
            {
                var screenSize = safeScreenSize;
                var isWideScreen = screenSize.x > screenSize.y;

                return isWideScreen;
            }
        }

        public Canvas canvas
        {
            get
            {
                if (_Canvas == null)
                    _Canvas = GetComponent<Canvas>();
                return _Canvas;
            }
        }

        public bool useLegacyPhysicalSize
        {
            get
            {
                return m_UseLegacyPhysicalSizeCalc;
            }
            set
            {
                if (m_UseLegacyPhysicalSizeCalc == value)
                    return;
                m_UseLegacyPhysicalSizeCalc = value;
            }
        }

        public bool supportSafeArea
        {
            get
            {
                return m_SupportSafeArea;
            }
            set
            {
                if (m_SupportSafeArea == value)
                    return;
                m_SupportSafeArea = value;
            }
        }

        public float screenSizeDigonal
        {
            get
            {
                return Mathf.Sqrt(Mathf.Pow(screenWidth, 2f) + Mathf.Pow(screenHeight, 2f)) / dpi;
            }
        }

        public int screenWidth
        {
            get
            {
                return canvas != null ? Display.displays[canvas.targetDisplay].renderingWidth : Screen.width;
            }
        }

        public int screenHeight
        {
            get
            {
                return canvas != null ? Display.displays[canvas.targetDisplay].renderingHeight : Screen.height;
            }
        }

        public float dpi
        {
            get
            {
                float currentDpi = EventSystemCompat.GetScreenDPI();
                //#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                //                currentDpi = Mathf.Round(1.445f * float.Parse(Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("Microsoft").OpenSubKey("Windows NT").OpenSubKey("CurrentVersion").OpenSubKey("FontDPI").GetValue("LogPixels").ToString()));
                //#else
                //                currentDpi = Screen.dpi;
                //#endif

#if UNITY_EDITOR
                if (m_EditorForceDPI && m_EditorForceDPIValue > m_DefaultSpriteDPI / 50f)
                {
                    currentDpi = m_EditorForceDPIValue;
                }
#endif

                if (currentDpi == 0f)
                {
                    currentDpi = m_FallbackScreenDPI;
                }

                return currentDpi;
            }
        }

        #endregion

        #region Constructors

        protected MaterialCanvasScaler()
        {
            m_DefaultSpriteDPI = 160;
            m_FallbackScreenDPI = 160;
        }

        #endregion

        #region Unity Functions

        protected override void OnDisable()
        {
            SetScaleFactor(1);
            SetReferencePixelsPerUnit(100);
        }

        #endregion

        #region Overriden Functions

        protected override void HandleWorldCanvas()
        {
            SetScaleFactor(m_DynamicPixelsPerUnit);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        protected override void HandleConstantPixelSize()
        {
            SetScaleFactor(m_ScaleFactor);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        protected virtual void HandleLegacyConstantPhysicalSize()
        {
            float currentDpi = EventSystemCompat.GetScreenDPI();
            float dpi = (currentDpi == 0 ? m_FallbackScreenDPI : currentDpi);
            float targetDPI = 1;
            switch (m_PhysicalUnit)
            {
                case Unit.Centimeters: targetDPI = 2.54f; break;
                case Unit.Millimeters: targetDPI = 25.4f; break;
                case Unit.Inches: targetDPI = 1; break;
                case Unit.Points: targetDPI = 72; break;
                case Unit.Picas: targetDPI = 6; break;
            }

            SetScaleFactor(dpi / targetDPI);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit * targetDPI / m_DefaultSpriteDPI);
        }

        protected override void HandleConstantPhysicalSize()
        {
            if (m_UseLegacyPhysicalSizeCalc)
                HandleLegacyConstantPhysicalSize();
            else
            {
                float scale;
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    scale = m_DynamicPixelsPerUnit;
                }
                else
                {
                    scale = (dpi / m_DefaultSpriteDPI) * m_ScaleFactor;
                }

                SetScaleFactor(scale);
                SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
            }
        }

        protected override void HandleScaleWithScreenSize()
        {
            Vector2 screenSize = safeScreenSize;
            // The log base doesn't have any influence on the results whatsoever, as long as the same base is used everywhere.
            float kLogBase = 2;

            // Multiple display support only when not the main display. For display 0 the reported
            // resolution is always the desktops resolution since its part of the display API,
            // so we use the standard none multiple display method. (case 741751)
            int displayIndex = canvas.targetDisplay;
            if (displayIndex > 0 && displayIndex < Display.displays.Length)
            {
                Display disp = Display.displays[displayIndex];
                screenSize = new Vector2(disp.renderingWidth, disp.renderingHeight);
            }

            if (m_DefaultScalingOrientation != ReferenceOrientations.None)
            {
                bool currentIsWideScreen = Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight;

                //Current orientation was not predicted in ReferenceOrientation, so we must change this value to reflect
                if((currentIsWideScreen && m_DefaultScalingOrientation != ReferenceOrientations.Landscape) ||
                   (!currentIsWideScreen && m_DefaultScalingOrientation != ReferenceOrientations.Portrait))
                {
                    screenSize = new Vector2(screenSize.y, screenSize.x);
                }
            }

            float scaleFactor = 0;
            switch (m_ScreenMatchMode)
            {
                case ScreenMatchMode.MatchWidthOrHeight:
                    {
                        // We take the log of the relative width and height before taking the average.
                        // Then we transform it back in the original space.
                        // the reason to transform in and out of logarithmic space is to have better behavior.
                        // If one axis has twice resolution and the other has half, it should even out if widthOrHeight value is at 0.5.
                        // In normal space the average would be (0.5 + 2) / 2 = 1.25
                        // In logarithmic space the average is (-1 + 1) / 2 = 0
                        float logWidth = Mathf.Log(screenSize.x / m_ReferenceResolution.x, kLogBase);
                        float logHeight = Mathf.Log(screenSize.y / m_ReferenceResolution.y, kLogBase);
                        float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, m_MatchWidthOrHeight);
                        scaleFactor = Mathf.Pow(kLogBase, logWeightedAverage);
                        break;
                    }
                case ScreenMatchMode.Expand:
                    {
                        scaleFactor = Mathf.Min(screenSize.x / m_ReferenceResolution.x, screenSize.y / m_ReferenceResolution.y);
                        break;
                    }
                case ScreenMatchMode.Shrink:
                    {
                        scaleFactor = Mathf.Max(screenSize.x / m_ReferenceResolution.x, screenSize.y / m_ReferenceResolution.y);
                        break;
                    }
            }

            SetScaleFactor(scaleFactor * m_ScaleFactor);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        #endregion

        #region Internal Apply Functions

        protected new void SetScaleFactor(float scaleFactor)
        {
            var isWideScreen = this.isWideScreen;

            bool scaleChanged = scaleFactor != _PrevScaleFactor;
            bool orientationChanged = isWideScreen != _PrevIsWideScreen;

            if (scaleChanged)
            {
                if (canvas != null)
                    canvas.scaleFactor = scaleFactor;
                _PrevScaleFactor = scaleFactor;
            }
            if (orientationChanged)
            {
                _PrevIsWideScreen = isWideScreen;
            }
            if ((scaleChanged || orientationChanged) && onCanvasAreaChanged != null)
                onCanvasAreaChanged.Invoke(scaleChanged, orientationChanged);
        }

        protected new void SetReferencePixelsPerUnit(float referencePixelsPerUnit)
        {
            if (referencePixelsPerUnit == _PrevReferencePixelsPerUnit)
                return;

            if (canvas != null)
                canvas.referencePixelsPerUnit = referencePixelsPerUnit;
            _PrevReferencePixelsPerUnit = referencePixelsPerUnit;
        }

        #endregion
    }
}
