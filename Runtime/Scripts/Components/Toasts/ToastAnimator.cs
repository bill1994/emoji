//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Toast Animator", 100)]
    public class ToastAnimator : MonoBehaviour
    {
        public enum OffsetMode { Default, Normalized }

        #region Private Variables

        [SerializeField]
        protected Graphic m_Text = null;
        [SerializeField]
        protected RectTransform m_RectTransform = null;
        [SerializeField]
        protected Image m_PanelImage = null;
        [SerializeField]
        protected CanvasGroup m_CanvasGroup = null;
        [Space]
        [SerializeField]
        RectTransform m_TargetTransform = null;
        [Space]
        [SerializeField]
        protected bool m_canDestroyToast = true;
        [SerializeField]
        protected float m_TimeToWait = 0f;
        [Space]
        [SerializeField]
        public OffsetMode m_OffsetMode = OffsetMode.Normalized;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_OnPosNormalizedPos")]
        protected float m_InOffset = 1 / 8f;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_OffNormalizedPos")]
        protected float m_OutOffset = 1 / 10f;

        protected int m_State;
        protected bool m_AnimDown;
        protected Vector3 m_InPos;
        protected Vector3 m_OutPos;
        protected Vector3 m_TempVec2;
        protected float m_AnimSpeed = 0.5f;
        protected float m_AnimStartTime;
        protected float m_AnimDeltaTime;
        protected float m_OutAlpha = 0f;
        protected float m_CurrentPosition;
        float m_CurrentWaitTime = -1;

        protected bool m_MoveFab;
        //protected MaterialMovableFab m_MaterialMovableFab;
        protected RectTransform m_FabRectTransform;
        protected float m_FabStartPos;

        protected Toast _toast = null;
        protected System.Func<Toast, ToastAnimator, bool> _onToastCompleteCallback = null;

        #endregion

        #region Public Properties

        public float timeToWait
        {
            get
            {
                return m_TimeToWait;
            }
            set
            {
                if (m_TimeToWait == value)
                    return;
                m_TimeToWait = value;
            }
        }

        public string text
        {
            get
            {
                return m_Text != null ? m_Text.GetGraphicText() : "";
            }
            set
            {
                if (m_Text != null)
                    m_Text.SetGraphicText(value);
            }
        }

        public Color panelColor
        {
            get
            {
                return m_PanelImage != null ? m_PanelImage.color : Color.white;
            }
            set
            {
                if (m_PanelImage != null)
                    m_PanelImage.color = value;
            }
        }

        public Color textColor
        {
            get
            {
                return m_Text != null ? m_Text.color : Color.black;
            }
            set
            {
                if (m_Text != null)
                    m_Text.color = value;
            }
        }

        public float fontSize
        {
            get
            {
                return m_Text != null ? m_Text.GetGraphicFontSize() : 12;
            }
            set
            {
                if(m_Text != null)
                    m_Text.SetGraphicFontSize(value);
            }
        }

        public float canvasAlpha
        {
            get
            {
                return m_CanvasGroup != null ? m_CanvasGroup.alpha : 1;
            }
            set
            {
                if (m_CanvasGroup != null)
                    m_CanvasGroup.alpha = value;
            }
        }

        public bool CanDestroyToast
        {
            get
            {
                return m_canDestroyToast;
            }
            set
            {
                if (m_canDestroyToast == value)
                    return;
                m_canDestroyToast = value;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            Init(m_TargetTransform);

            transform.localScale = new Vector3(1, 1, 1);

            m_CanvasGroup.alpha = 0;
            m_CurrentWaitTime = -1;
            m_AnimStartTime = -1;
            m_CurrentPosition = m_RectTransform.position.y;
            m_State = 2;
        }

        protected virtual void Update()
        {
            if (m_State > 0)
            {
                if (m_AnimStartTime < 0)
                    m_AnimStartTime = Time.realtimeSinceStartup;

                m_AnimDeltaTime = Time.realtimeSinceStartup - m_AnimStartTime;

                var v_onWorldPos = m_TargetTransform != null? m_TargetTransform.TransformPoint((Vector3)m_InPos) : m_InPos;
                var v_offWorldPos = m_TargetTransform != null? m_TargetTransform.TransformPoint((Vector3)m_OutPos) : m_OutPos;

                Kyub.Performance.SustainedPerformanceManager.Refresh(this);
                if (m_CurrentWaitTime >= 0)
                    return;
                if (m_State == 1)
                {
                    
                    if (m_AnimDeltaTime < m_AnimSpeed)
                    {
                        m_TempVec2 = m_RectTransform.position;
                        m_TempVec2.y = Tween.CubeOut(m_CurrentPosition, v_onWorldPos.y, m_AnimDeltaTime, m_AnimSpeed);
                        m_RectTransform.position = m_TempVec2;
                        SetLocalPositionZ(m_RectTransform, 0);
                        m_CanvasGroup.alpha = Tween.CubeInOut(m_CanvasGroup.alpha, 1f, m_AnimDeltaTime, m_AnimSpeed);
                        if (m_MoveFab)
                        {
                            m_FabRectTransform.position = new Vector3(m_FabRectTransform.position.x, m_FabStartPos + (m_RectTransform.position.y - v_offWorldPos.y), m_FabRectTransform.position.z);
                            SetLocalPositionZ(m_FabRectTransform, 0);
                        }
                    }
                    else
                    {
                        m_RectTransform.position = v_onWorldPos;
                        SetLocalPositionZ(m_RectTransform, 0);
                        if (m_MoveFab)
                        {
                            m_FabRectTransform.position = new Vector3(m_FabRectTransform.position.x, m_FabStartPos + (m_RectTransform.position.y - v_offWorldPos.y), m_FabRectTransform.position.z);
                            SetLocalPositionZ(m_FabRectTransform, 0);
                        }
                        StartCoroutine(WaitTime());
                        m_State = 3;
                    }
                }
                else if (m_State == 2)
                {
                    if (m_AnimDeltaTime < m_AnimSpeed)
                    {
                        m_TempVec2 = m_RectTransform.position;
                        m_TempVec2.y = Tween.CubeInOut(m_CurrentPosition, v_offWorldPos.y, m_AnimDeltaTime, m_AnimSpeed);
                        m_RectTransform.position = m_TempVec2;
                        SetLocalPositionZ(m_RectTransform, 0);
                        m_CanvasGroup.alpha = Tween.CubeIn(m_CanvasGroup.alpha, m_OutAlpha, m_AnimDeltaTime, m_AnimSpeed);
                        if (m_MoveFab)
                        {
                            m_FabRectTransform.position = new Vector3(m_FabRectTransform.position.x, m_FabStartPos + (m_RectTransform.position.y - v_offWorldPos.y), m_FabRectTransform.position.z);
                            SetLocalPositionZ(m_FabRectTransform, 0);
                        }
                    }
                    else
                    {
                        if (m_MoveFab)
                        {
                            m_FabRectTransform.position = new Vector3(m_FabRectTransform.position.x, m_FabStartPos, m_FabRectTransform.position.z);
                            SetLocalPositionZ(m_FabRectTransform, 0);
                        }
                        m_State = 0;
                        OnAnimDone();
                    }
                }
            }
        }

        #endregion

        #region Public Functions

        public void Init(RectTransform targetTransform = null)
        {
            m_TargetTransform = targetTransform;

            Rect canvasRect = targetTransform != null ? targetTransform.rect : Screen.safeArea;

            m_InPos = new Vector2(canvasRect.center.x, canvasRect.yMin + (m_OffsetMode == OffsetMode.Normalized? canvasRect.height * m_InOffset : m_InOffset));
            m_OutPos = new Vector2(canvasRect.center.x, canvasRect.yMin + (m_OffsetMode == OffsetMode.Normalized ? canvasRect.height * m_OutOffset : m_OutOffset));

            /*if (targetTransform != null)
            {
                m_InPos = targetTransform.TransformPoint((Vector3)m_InPos);
                m_OutPos = targetTransform.TransformPoint((Vector3)m_OutPos);
            }*/
            m_RectTransform.position = targetTransform != null? targetTransform.TransformPoint((Vector3)m_OutPos) : m_OutPos;
            SetLocalPositionZ(m_RectTransform, 0);

            if(!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public void Show(Toast toast, Canvas canvasHierarchy, System.Func<Toast, ToastAnimator, bool> onToastComplete = null)
        {
            Transform targetTransform = null;
            if (canvasHierarchy != null)
            {
                CanvasSafeArea safeArea = canvasHierarchy.GetComponent<CanvasSafeArea>();
                targetTransform = safeArea != null && safeArea.Content != null ? safeArea.Content : canvasHierarchy.transform;
            }

            Show(toast, targetTransform as RectTransform, onToastComplete);
        }

        public virtual void Show(Toast toast, RectTransform targetTransform = null, System.Func<Toast, ToastAnimator, bool> onToastComplete = null)
        {
            _toast = toast;
            _onToastCompleteCallback = onToastComplete;

            m_CurrentWaitTime = -1;
            m_TimeToWait = toast.duration;
            m_Text.SetGraphicText(toast.content);
            if(toast.panelColor != null)
                m_PanelImage.color = toast.panelColor.Value;
            if(toast.textColor != null)
                m_Text.color = toast.textColor.Value;
            if(toast.fontSize != null)
                m_Text.SetGraphicFontSize(toast.fontSize.Value);

            Init(targetTransform);
            m_CanvasGroup.alpha = 0;

            m_AnimStartTime = -1;
            m_CurrentPosition = m_RectTransform.position.y;
            m_State = 1;
        }

        public void Renew(float p_timeToWait = -1)
        {
            if (p_timeToWait >= 0)
                m_TimeToWait = p_timeToWait;
            if (m_CurrentWaitTime >= 0 && m_State > 0)
                m_CurrentWaitTime = 0;
            else
            {
                if (m_State != 1) //Is state is not "show"
                    Show(new Toast(this.text, m_TimeToWait, this.panelColor, this.textColor, (int)this.fontSize), m_TargetTransform);
            }
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void SetLocalPositionZ(RectTransform p_transform, float p_zValue)
        {
            if (p_transform != null)
                p_transform.localPosition = new Vector3(p_transform.localPosition.x, p_transform.localPosition.y, p_zValue);
        }

        protected virtual IEnumerator WaitTime()
        {
            m_AnimDown = true;
            m_CurrentWaitTime = 0;
            while (m_CurrentWaitTime < m_TimeToWait)
            {
                m_CurrentWaitTime += Time.deltaTime;
                yield return null;
            }
            m_CurrentWaitTime = -1;
            if (m_AnimDown)
            {
                m_AnimStartTime = -1;
                m_CurrentPosition = m_RectTransform.position.y;
                m_State = 2;
            }
        }

        #endregion

        #region Receivers

        protected virtual void OnAnimDone()
        {
            m_CanvasGroup.alpha = 0.0f;

            var finalize = _onToastCompleteCallback != null? _onToastCompleteCallback.Invoke(_toast, this) : true;
            if (finalize && this.gameObject != null && m_State != 1)
            {
                if (CanDestroyToast)
                    Destroy(gameObject);
                else if (gameObject.activeSelf)
                    gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
