using MaterialUI.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialSpinnerActivity : MaterialActivity
    {
        #region Private Variables

        [SerializeField]
        MaterialFrame m_Frame = null;
        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_AutoCreateBackground")]
        bool m_HasBackground = true;
        [SerializeField]
        MaterialActivityBackground m_Background = null;

        #endregion

        #region Public Properties

        public MaterialFrame frame
        {
            get
            {
                if (m_Frame == null)
                    GetComponent<MaterialFrame>();
                return m_Frame;
            }
        }

        public MaterialActivityBackground background
        {
            get { return m_Background; }
            set
            {
                if (m_Background == value)
                    return;
                UnregisterBackgroundEvents();
                m_Background = value;
                RegisterBackgroundEvents();
                ApplyBackgroundVisibility();
            }
        }

        public bool hasBackground
        {
            get { return m_HasBackground; }
            set
            {
                if (m_HasBackground == value)
                    return;
                m_HasBackground = value;
                ApplyBackgroundVisibility();
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterBackgroundEvents();
        }


        protected virtual void Update()
        {
            if (m_Frame != null && m_Frame.gameObject.activeSelf)
            {
                m_Frame.transform.localPosition = GetValidLocalPositionInsideActivity(m_Frame);
            }
        }


        #endregion

        #region Public Functions

        public MaterialFrame SetFrame(MaterialFrame frame, Vector3 frameOpenWorldPosition, Vector2 framePivot, Vector2 minSize, Vector2 preferredSize)
        {
            frame = SetFrame(frame);
            if (frame != null)
                RecalculatePosition(frameOpenWorldPosition, framePivot, minSize, preferredSize);

            return frame;
        }

        public MaterialFrame SetFrame(MaterialFrame frame, IBaseSpinner spinner)
        {
            frame = SetFrame(frame);
            if (frame != null)
                RecalculatePosition(spinner);

            return frame;
        }

        public MaterialFrame SetFrame(MaterialFrame frame)
        {
            if (frame == null)
                return null;

            //Is a prefab
            if (!frame.transform.root.gameObject.scene.IsValid())
                frame = GameObject.Instantiate(frame);

            //Delete Default Frame
            if (m_Frame != null && m_Frame != frame)
                GameObject.Destroy(m_Frame.gameObject);

            //MaterialDialogActivity does not support activities in same object as the frame added
            var frameActivity = frame.GetComponent<MaterialActivity>();
            if (frameActivity != null)
                Component.DestroyImmediate(frameActivity);

            frame.transform.SetParent(this.transform, false);
            m_Frame = frame;
            if (frame is MaterialDialogFrame)
                (frame as MaterialDialogFrame).activity = this;

            m_Frame.transform.SetAsLastSibling();

            return frame;
        }

        public virtual void RecalculatePosition(IBaseSpinner spinner)
        {
            var frameAnchoredPosition = spinner != null && !spinner.IsDestroyed() && spinner.rectTransform != null ? 
                spinner.rectTransform.TransformPoint(Rect.NormalizedToPoint(spinner.rectTransform.rect, spinner.dropdownExpandPivot) + spinner.dropdownOffset) : 
                Vector3.zero;

            var spinnerMinSize = spinner.rectTransform != null ? spinner.rectTransform.GetProperSize() : Vector2.zero;
            var spinnerPreferredSize = spinner.rectTransform != null ? spinner.dropdownFramePreferredSize : Vector2.zero;
            RecalculatePosition(frameAnchoredPosition, spinner.dropdownFramePivot, spinnerMinSize, spinnerPreferredSize);
        }

        public virtual void RecalculatePosition(Vector3 frameOpenWorldPosition, Vector2 framePivot, Vector2 minSize, Vector2 preferredSize)
        {
            var frameAnchoredPosition = this.transform.InverseTransformPoint(frameOpenWorldPosition);

            var localScale = frame.transform.localScale;
            var localRotation = frame.transform.localRotation;

            var frameRectTransform = frame.transform as RectTransform;
            if (frameRectTransform != null)
            {
                frameRectTransform.pivot = framePivot;
                frameRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                frameRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }

            frame.transform.localPosition = frameAnchoredPosition;
            frame.transform.localScale = localScale;
            frame.transform.localRotation = localRotation;

            var frameLayoutElement = frame.GetAddComponent<LayoutElement>();
            if (frameLayoutElement != null)
            {
                frameLayoutElement.minWidth = Mathf.Max(frameLayoutElement.minWidth, minSize.x, preferredSize.x);
                frameLayoutElement.minHeight = Mathf.Max(frameLayoutElement.minHeight, minSize.y, preferredSize.y);
                frameLayoutElement.preferredWidth = preferredSize.x < 0 ? frameLayoutElement.preferredWidth : preferredSize.x;
                frameLayoutElement.preferredHeight = preferredSize.y < 0 ? frameLayoutElement.preferredHeight : preferredSize.y;
            }
            frameRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredSize.x < 0 ? minSize.x : preferredSize.x);
            frameRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredSize.y < 0 ? minSize.y : preferredSize.y);

            LayoutRebuilder.ForceRebuildLayoutImmediate(frameRectTransform);

            m_Frame.transform.localPosition = GetValidLocalPositionInsideActivity(m_Frame);
        }

        protected virtual Vector2 GetValidLocalPositionInsideActivity(MaterialFrame frame)
        {
            if (frame == null)
                return Vector2.zero;

            var frameRectTranform = (frame.transform as RectTransform);
            if (frameRectTranform == null)
                return frame.transform.localPosition;

            var frameAnchoredPosition = frameRectTranform.localPosition;

            //Convert FrameRect to Activity Space
            var frameRect = frameRectTranform.rect;
            var frameMin = this.rectTransform.InverseTransformPoint(frameRectTranform.TransformPoint(frameRect.min));
            var frameMax = this.rectTransform.InverseTransformPoint(frameRectTranform.TransformPoint(frameRect.max));
            frameRect = Rect.MinMaxRect(frameMin.x, frameMin.y, frameMax.x, frameMax.y);

            var acvitityRect = this.rectTransform.rect;

            //  Left edge
            float activityEdge = acvitityRect.xMin;
            float frameEdge = frameRect.xMin;
            if (frameEdge < activityEdge)
            {
                frameAnchoredPosition.x += activityEdge - frameEdge;
            }

            //  Right edge
            activityEdge = acvitityRect.xMax;
            frameEdge = frameRect.xMax;
            if (frameEdge > activityEdge)
            {
                frameAnchoredPosition.x += activityEdge - frameEdge;
            }

            //  Top edge
            activityEdge = acvitityRect.yMax;
            frameEdge = frameRect.yMax;
            if (frameEdge > activityEdge)
            {
                frameAnchoredPosition.y += activityEdge - frameEdge;
            }

            //  Bottom edge
            activityEdge = acvitityRect.yMin;
            frameEdge = frameRect.yMin;
            if (frameEdge < activityEdge)
            {
                frameAnchoredPosition.y += activityEdge - frameEdge;
            }

            return frameAnchoredPosition;
        }

        public override void Show()
        {
            CreateBackground();
            if (changeSibling)
                this.transform.SetAsLastSibling();
            gameObject.SetActive(true);
            SetCanvasActive(true);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            System.Action onShow = () =>
            {
                if (this == null)
                    return;

                canvasGroup.blocksRaycasts = true;
                HandleOnShowAnimationOver();
            };

            if (m_Background != null)
                m_Background.AnimateShowBackground(null);
            if (m_Frame != null)
                ShowFrame(onShow);
            else
                onShow();
        }

        public override void Hide()
        {
            System.Action onHide = () =>
            {
                if (this == null)
                    return;

                canvasGroup.blocksRaycasts = false;
                HandleOnHideAnimationOver();
            };

            if (m_Background != null)
                m_Background.AnimateHideBackground(null);
            if (m_Frame != null)
                HideFrame(onHide);
            else
                onHide();
        }

        #endregion

        #region Receivers

        void HandleOnBackgroundClick()
        {
            Hide();
        }

        void HandleOnShowAnimationOver()
        {
            OnShowAnimationOver.InvokeIfNotNull();
        }

        void HandleOnHideAnimationOver()
        {
            OnHideAnimationOver.InvokeIfNotNull();

            if (m_DestroyOnHide)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Internal Frame Functions

        void ShowFrame(System.Action callback)
        {
            if (m_Frame != null)
                m_Frame.SendMessageToAll("OnActivityBeginShow");

            System.Action showAction = () =>
            {
                if (m_Frame != null)
                    m_Frame.transform.localPosition = GetValidLocalPositionInsideActivity(m_Frame);
                callback.InvokeIfNotNull();
                if (m_Frame != null)
                    m_Frame.SendMessageToAll("OnActivityEndShow");
            };

            if (m_Frame)
                m_Frame.gameObject.SetActive(true);

            var tweener = frame != null ? frame.GetAddComponent<EasyFrameAnimator>() : null;
            if (tweener != null)
            {
                tweener.Clear();
                tweener.transitionDuration = 0.2f;
                tweener.scaleIn = true;
                tweener.scaleInScale = 0;
                tweener.fadeIn = true;
                tweener.fadeInAlpha = 0;

                LayoutRebuilder.ForceRebuildLayoutImmediate(this.transform as RectTransform);
                tweener.Tween("show", (tag) => { showAction.InvokeIfNotNull(); });
                if (m_Frame != null)
                    m_Frame.transform.localPosition = GetValidLocalPositionInsideActivity(m_Frame);
            }
            else
                showAction.InvokeIfNotNull();
        }

        void HideFrame(System.Action callback)
        {
            if (m_Frame != null)
                m_Frame.SendMessageToAll("OnActivityBeginHide");

            System.Action hideAction = () =>
            {
                callback.InvokeIfNotNull();
                if (m_Frame != null)
                    m_Frame.SendMessageToAll("OnActivityEndHide");
            };

            var tweener = frame != null ? frame.GetAddComponent<EasyFrameAnimator>() : null;
            if (tweener != null)
            {
                tweener.Clear();
                tweener.transitionDuration = 0.2f;
                tweener.fadeOut = true;
                tweener.fadeOutAlpha = 0;
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.transform as RectTransform);
                tweener.Tween("hide", (tag) => { hideAction.InvokeIfNotNull(); });
            }
            else
                hideAction.InvokeIfNotNull();
        }

        #endregion

        #region Internal Background Functions

        void CreateBackground()
        {
            if (this == null)
                return;

            if (m_HasBackground)
            {
                if (m_Background == null)
                {
                    m_Background = new GameObject("Background").AddComponent<MaterialActivityBackground>();
                    var bgImage = m_Background.gameObject.AddComponent<Image>();
                    bgImage.color = new Color(0, 0, 0, 0);
                    m_Background.targetGraphic = bgImage;

                    m_Background.transform.SetParent(this.rectTransform);
                    Inflate(m_Background.transform as RectTransform, true);
                    m_Background.transform.SetAsFirstSibling();
                    m_Background.transition = Selectable.Transition.None;

                    var frameAnimator = m_Background.gameObject.AddComponent<EasyFrameAnimator>();
                    frameAnimator.fadeIn = true;
                    frameAnimator.fadeOut = true;
                }
                //Object from another scene or this is not hierarchy child (template object?)
                else if (m_Background.gameObject.scene != this.gameObject.scene ||
                  (m_Background.transform.parent != this.transform && m_Background.transform != this.transform))
                {
                    var oldBackground = m_Background;
                    UnregisterBackgroundEvents();
                    m_Background = GameObject.Instantiate(m_Background, this.rectTransform);
                    Inflate(m_Background.transform as RectTransform, true);
                    m_Background.transform.SetAsFirstSibling();
                    m_Background.transition = Selectable.Transition.None;
                    oldBackground.gameObject.SetActive(false);

                    if (m_Background.tweener == null)
                    {
                        var frameAnimator = m_Background.gameObject.GetAddComponent<EasyFrameAnimator>();
                        frameAnimator.fadeIn = true;
                        frameAnimator.fadeOut = true;
                    }
                }
            }

            ApplyBackgroundVisibility();
        }

        void ApplyBackgroundVisibility()
        {
            if (m_Background != null && m_Background.gameObject.scene == this.gameObject.scene &&
              (m_Background.transform.parent == this.transform || m_Background.transform == this.transform))
            {
                //Same as Activity
                if (m_Background.transform == this.transform)
                {
                    if (m_HasBackground)
                        RegisterBackgroundEvents();
                    else
                        UnregisterBackgroundEvents();
                }
                else
                {
                    RegisterBackgroundEvents();
                    m_Background.gameObject.SetActive(m_HasBackground);
                }
            }
            else if (m_Background != null)
            {
                UnregisterBackgroundEvents();
                m_Background.gameObject.SetActive(false);
            }
        }

        void RegisterBackgroundEvents()
        {
            UnregisterBackgroundEvents();

            if (m_Background != null && m_Background.onBackgroundClick != null)
                m_Background.onBackgroundClick.AddListener(HandleOnBackgroundClick);
        }

        void UnregisterBackgroundEvents()
        {
            if (m_Background != null && m_Background.onBackgroundClick != null)
                m_Background.onBackgroundClick.RemoveListener(HandleOnBackgroundClick);
        }

        #endregion
    }
}
