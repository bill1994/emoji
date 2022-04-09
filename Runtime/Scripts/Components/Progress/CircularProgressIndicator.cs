// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    //[ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Progress/Circular Progress Indicator")]
    public class CircularProgressIndicator : ProgressIndicator
    {
        #region Private Variables

        [SerializeField]
        private RectTransform m_CircleRectTransform = null;
        [SerializeField]
        float m_Size = 48f;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_labelText")]
        private Graphic m_LabelText = null;
        [SerializeField]
        bool m_CanAnimateSize = true;
		[SerializeField]
        private float m_AnimScaleDuration = 0.65f;
		
        private const float m_AnimDuration = 0.65f;
        private Image m_CircleImage;
        private Graphic m_CircleIcon;

        private int m_AnimCircle;
        private float m_AnimCircleStartTime;
        private float m_AnimCircleCurrentFillAmount;
        private float m_AnimCircleCurrentRotation;

        private bool m_AnimColor;
        private float m_AnimColorStartTime;
        private Color m_AnimColorCurrentColor;
        private Color m_AnimColorTargetColor;

        private bool m_AnimSize;
        private float m_AnimSizeStartTime;
        private float m_AnimSizeCurrentSize;
        private float m_AnimSizeTargetSize = 1;

        #endregion

        #region Callbacks

        [Header("Callbacks")]
        public UnityEvent OnShow;
        public UnityEvent OnHide;

        #endregion

        #region Public Properties

        public Graphic circleIcon
        {
            get
            {
                if (m_CircleIcon == null)
                {
                    if (circleImage != null)
                    {
                        m_CircleIcon = circleImage.GetComponentInChildren<IVectorImage>() as Graphic;
                    }
                }
                return m_CircleIcon;
            }
        }
        public Image circleImage
        {
            get
            {
                if (m_CircleImage == null)
                {
                    if (m_CircleRectTransform != null)
                    {
                        m_CircleImage = m_CircleRectTransform.GetComponent<Image>();
                    }
                }
                return m_CircleImage;
            }
        }
        public Graphic labelText
        {
            get
            {
                return m_LabelText;
            }
        }
        public float size
        {
            get { return m_Size; }
            set
            {
                m_Size = value;
            }
        }
        public RectTransform circleRectTransform
        {
            get { return m_CircleRectTransform; }
            set { m_CircleRectTransform = value; }
        }

        public bool CanAnimateSize
        {
            get
            {
                return m_CanAnimateSize;
            }

            set
            {
                m_CanAnimateSize = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            if (OnShow != null)
                OnShow.Invoke();
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;

            //If Target is 0, someone called hide before start
            if (m_AnimSizeTargetSize != 0)
            {
                if (m_StartsHidden)
                {
                    scaledRectTransform.localScale = new Vector3(0f, 0f, 1f);
                }
                else if (m_StartsIndeterminate && gameObject.activeSelf)
                {
                    StartIndeterminate();
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (OnHide != null)
                OnHide.Invoke();
        }

        protected virtual void Update()
        {
            UpdateAnimCircle();
            UpdateAnimColor();
            UpdateAnimSize();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SetProgress(m_CurrentProgress);
            }
#endif
        }

        #endregion

        #region Helper Functions

        private void UpdateAnimCircle()
        {
            if (circleImage == null) return;
            if (m_AnimCircle == 0) return;

            if (m_AnimCircleStartTime < 0)
                m_AnimCircleStartTime = Time.realtimeSinceStartup;

            if (m_AnimCircle == 1)
            {
                float animDeltaTime = Time.realtimeSinceStartup - m_AnimCircleStartTime;

                if (animDeltaTime < m_AnimDuration)
                {
                    circleImage.fillAmount = Tween.CubeInOut(m_AnimCircleCurrentFillAmount, 0.75f, animDeltaTime,
                                                                            m_AnimDuration);
                }
                else
                {
                    m_AnimCircleCurrentFillAmount = 0.75f;
                    circleImage.fillAmount = 0.75f;
                    FlipCircle(false);
                    m_AnimCircleStartTime = Time.realtimeSinceStartup;
                    m_AnimCircle = 2;
                }

                m_CircleRectTransform.localEulerAngles = new Vector3(m_CircleRectTransform.localEulerAngles.x,
                                                                               m_CircleRectTransform.localEulerAngles.y,
                                                                               m_CircleRectTransform.localEulerAngles.z - Time.unscaledDeltaTime * 200f);
                return;
            }

            if (m_AnimCircle == 2)
            {
                float animDeltaTime = Time.realtimeSinceStartup - m_AnimCircleStartTime;

                if (animDeltaTime < m_AnimDuration)
                {
                    circleImage.fillAmount = Tween.CubeInOut(m_AnimCircleCurrentFillAmount, 0.1f, animDeltaTime,
                                                                            m_AnimDuration);
                }
                else
                {
                    m_AnimCircleCurrentFillAmount = 0.1f;
                    circleImage.fillAmount = 0.1f;
                    FlipCircle(true);
                    m_AnimCircleStartTime = Time.realtimeSinceStartup;
                    m_AnimCircle = 1;
                }

                m_CircleRectTransform.localEulerAngles = new Vector3(m_CircleRectTransform.localEulerAngles.x,
                                                                               m_CircleRectTransform.localEulerAngles.y,
                                                                               m_CircleRectTransform.localEulerAngles.z - Time.unscaledDeltaTime * 200f);
                return;
            }

            if (m_AnimCircle == 3)
            {
                float animDeltaTime = Time.realtimeSinceStartup - m_AnimCircleStartTime;

                if (animDeltaTime < m_AnimDuration)
                {
                    circleImage.fillAmount = Tween.CubeInOut(m_AnimCircleCurrentFillAmount, m_CurrentProgress, animDeltaTime,
                                                                            m_AnimDuration);
                    Vector3 tempVector3 = m_CircleRectTransform.localEulerAngles;
                    tempVector3.z = Tween.CubeInOut(m_AnimCircleCurrentRotation, 0f, animDeltaTime, m_AnimDuration);
                    m_CircleRectTransform.localEulerAngles = tempVector3;
                }
                else
                {
                    m_AnimCircleCurrentFillAmount = circleImage.fillAmount = m_CurrentProgress;
                    Vector3 tempVector3 = m_CircleRectTransform.localEulerAngles;
                    tempVector3.z = 0f;
                    m_CircleRectTransform.localEulerAngles = tempVector3;
                    m_AnimCircleStartTime = Time.realtimeSinceStartup;
                    m_AnimCircle = 0;
                }
            }
        }

        private void UpdateAnimColor()
        {
            if (!m_AnimColor) return;

            float animDeltaTime = Time.realtimeSinceStartup - m_AnimColorStartTime;

            if (animDeltaTime < m_AnimDuration)
            {
                circleIcon.color = Tween.CubeInOut(m_AnimColorCurrentColor, m_AnimColorTargetColor, animDeltaTime,
                                                                m_AnimDuration);
            }
            else
            {
                circleIcon.color = m_AnimColorTargetColor;
                m_AnimColor = false;
            }
        }

        private void UpdateAnimSize()
        {
            if (!m_AnimSize) return;

            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            if(m_AnimSizeStartTime < 0)
                m_AnimSizeStartTime = Time.realtimeSinceStartup;

            float animDeltaTime = Time.realtimeSinceStartup - m_AnimSizeStartTime;

            if (animDeltaTime < m_AnimScaleDuration)
            {
                Vector3 tempVector3 = scaledRectTransform.localScale;
                tempVector3.x = Tween.CubeInOut(m_AnimSizeCurrentSize, m_AnimSizeTargetSize, animDeltaTime, m_AnimScaleDuration);
                tempVector3.y = tempVector3.x;
                tempVector3.z = tempVector3.x;
                scaledRectTransform.localScale = tempVector3;
            }
            else
            {
                Vector3 tempVector3 = scaledRectTransform.localScale;
                tempVector3.x = m_AnimSizeTargetSize;
                tempVector3.y = tempVector3.x;
                tempVector3.z = tempVector3.x;
                scaledRectTransform.localScale = tempVector3;
                m_AnimSize = false;
                if (m_AnimSizeTargetSize == 0f)
                {
                    gameObject.SetActive(false);
                    HandleOnHideAnimationFinished();
                }
                else
                    HandleOnShowAnimationFinished();
            }
        }

        protected virtual bool IsAnimatingSize(bool isShowing)
        {
            return m_AnimSize &&
                   enabled &&
                   gameObject.activeInHierarchy &&
                   (isShowing ? m_AnimSizeTargetSize > 0 : m_AnimSizeTargetSize == 0f);
        }

        public override void Show(bool startIndeterminate)
        {
            Show(startIndeterminate, null);
        }

        public virtual void Show(bool startIndeterminate, string labelText)
        {
            if (scaledRectTransform == null) return;

            if (m_LabelText != null)
            {
                if (!string.IsNullOrEmpty(labelText))
                {
                    m_LabelText.SetGraphicText(labelText);
                    m_LabelText.enabled = true;
                }
                else
                {
                    m_LabelText.enabled = false;
                }
            }

            gameObject.SetActive(true);
            m_AnimSizeCurrentSize = scaledRectTransform.localScale.x;
            m_AnimSizeTargetSize = 1f;
            m_AnimSizeStartTime = -1;
            m_AnimSize = true;

            if (!m_IsAnimatingIndeterminate && startIndeterminate)
            {
                StartIndeterminate(labelText);
            }
        }

        public override void Hide()
        {
            if (scaledRectTransform == null) return;

            m_AnimSizeCurrentSize = scaledRectTransform.localScale.x;
            m_AnimSizeTargetSize = 0f;
            m_AnimSizeStartTime = -1;
            m_AnimSize = m_CanAnimateSize;

            if (!m_AnimSize)
            {
                gameObject.SetActive(false);
                HandleOnHideAnimationFinished();
            }
        }

        public void StartIndeterminate(string labelText = null)
        {
            FlipCircle(true);
            SetAnimCurrents();
            m_IsAnimatingIndeterminate = true;
            m_AnimCircle = 1;

            Show(true, labelText);
        }

        public override void SetProgress(float progress, bool animated)
        {
            if (circleImage == null) return;
            if (circleRectTransform == null) return;

            progress = Mathf.Clamp(progress, 0f, 1f);

            if (!animated)
            {
                FlipCircle(true);
                currentProgress = progress;
                m_IsAnimatingIndeterminate = false;
                circleImage.fillAmount = m_CurrentProgress;
                Vector3 tempVector3 = m_CircleRectTransform.localEulerAngles;
                tempVector3.z = 0f;
                m_CircleRectTransform.localEulerAngles = tempVector3;
                m_AnimCircle = 0;
            }
            else
            {
                FlipCircle(true);
                SetAnimCurrents();
                currentProgress = progress;
                m_IsAnimatingIndeterminate = false;
                m_AnimCircle = 3;
            }
        }

        public override void SetColor(Color color)
        {
            m_AnimColorCurrentColor = circleIcon.color;
            m_AnimColorTargetColor = color;
            m_AnimColorStartTime = Time.realtimeSinceStartup;
            m_AnimColor = true;
        }

        private void SetAnimCurrents()
        {
            if (circleImage == null) return;

            m_AnimCircleCurrentRotation = m_CircleRectTransform.localEulerAngles.z;
            m_AnimCircleCurrentFillAmount = circleImage.fillAmount;
            m_AnimCircleStartTime = -1;
        }

        private void FlipCircle(bool clockwise)
        {
            if (circleImage == null) return;

            if (!circleImage.fillClockwise && clockwise)
            {
                m_CircleRectTransform.localEulerAngles = new Vector3(m_CircleRectTransform.localEulerAngles.x,
                                                                               m_CircleRectTransform.localEulerAngles.y,
                                                                               m_CircleRectTransform.localEulerAngles.z + (360f * circleImage.fillAmount));
                circleImage.fillClockwise = true;
            }
            else if (circleImage.fillClockwise && !clockwise)
            {
                m_CircleRectTransform.localEulerAngles = new Vector3(m_CircleRectTransform.localEulerAngles.x,
                                                                               m_CircleRectTransform.localEulerAngles.y,
                                                                               m_CircleRectTransform.localEulerAngles.z - (360f * circleImage.fillAmount));
                circleImage.fillClockwise = false;
            }
        }

        protected virtual void HandleOnShowAnimationFinished()
        {
        }

        protected virtual void HandleOnHideAnimationFinished()
        {
        }

        #endregion

        #region Layout Functions

        public override float GetMinWidth()
        {
            return m_Size;
        }

        public override float GetMinHeight()
        {
            return m_Size;
        }

        #endregion
    }
}