using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [System.Obsolete("Use EasyFrameAnimator instead")]
    public class DialogAnimatorSlide : DialogAnimator
    {
        public enum SlideDirection
        {
            Bottom,
            Left,
            Top,
            Right
        }

        [SerializeField]
        private SlideDirection m_SlideInDirection = SlideDirection.Bottom;
        public SlideDirection slideInDirection
        {
            get { return m_SlideInDirection; }
            set { m_SlideInDirection = value; }
        }

        [SerializeField]
        private SlideDirection m_SlideOutDirection = SlideDirection.Bottom;
        public SlideDirection slideOutDirection
        {
            get { return m_SlideOutDirection; }
            set { m_SlideOutDirection = value; }
        }

        [SerializeField]
        private Tween.TweenType m_SlideInTweenType = Tween.TweenType.EaseOutQuint;
        public Tween.TweenType slideInTweenType
        {
            get { return m_SlideInTweenType; }
            set
            {
                if (value == Tween.TweenType.Custom)
                {
                    Debug.LogWarning("Cannot set tween type to 'Custom'");
                    return;
                }
                m_SlideInTweenType = value;
            }
        }

        [SerializeField]
        private Tween.TweenType m_SlideOutTweenType = Tween.TweenType.EaseInCubed;
        public Tween.TweenType slideOutTweenType
        {
            get { return m_SlideOutTweenType; }
            set
            {
                if (value == Tween.TweenType.Custom)
                {
                    Debug.LogWarning("Cannot set tween type to 'Custom'");
                    return;
                }
                m_SlideOutTweenType = value;
            }
        }

        public override void AnimateShow(Action callback)
        {
            base.AnimateShow(callback);

            if (m_Dialog != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Dialog.rectTransform);

                m_Dialog.rectTransform.anchoredPosition = GetSlidePosition(m_SlideInDirection);

                m_Dialog.rectTransform.localScale = Vector3.one;

                TweenManager.TweenVector2(v2 =>
                {
                    if (this != null && m_Dialog != null)
                        m_Dialog.rectTransform.anchoredPosition = v2;
                },
                m_Dialog.rectTransform.anchoredPosition,
                Vector2.zero,
                m_AnimationDuration,
                0,
                callback,
                false,
                m_SlideInTweenType);
            }
        }

        public override void AnimateHide(Action callback)
        {
            base.AnimateHide(callback);

            if (m_Dialog != null)
            {
                TweenManager.TweenVector2(
                    v2 =>
                    {
                        if (this != null && m_Dialog != null)
                            m_Dialog.rectTransform.anchoredPosition = v2;
                    },
                m_Dialog.rectTransform.anchoredPosition,
                GetSlidePosition(m_SlideOutDirection),
                m_AnimationDuration,
                0,
                callback,
                false,
                m_SlideOutTweenType);
            }
        }

        private Vector2 GetSlidePosition(SlideDirection direction)
        {
            if (m_Dialog != null)
            {
                float canvasSize = (direction == SlideDirection.Left || direction == SlideDirection.Right) ? m_Dialog.rectTransform.GetRootCanvas().pixelRect.width : m_Dialog.rectTransform.GetRootCanvas().pixelRect.height;
                float dialogSize = (direction == SlideDirection.Left || direction == SlideDirection.Right) ? LayoutUtility.GetPreferredWidth(m_Dialog.rectTransform) : LayoutUtility.GetPreferredHeight(m_Dialog.rectTransform);

                dialogSize *= 1.1f;

                switch (direction)
                {
                    case SlideDirection.Bottom:
                        return new Vector2(0f, -(canvasSize + dialogSize));
                    case SlideDirection.Left:
                        return new Vector2(-(canvasSize + dialogSize), 0f);
                    case SlideDirection.Right:
                        return new Vector2(canvasSize + dialogSize, 0f);
                    case SlideDirection.Top:
                        return new Vector2(0f, canvasSize + dialogSize);
                }
            }

            return Vector2.zero;
        }

        #region Static Setup Functions

        public static DialogAnimatorSlide AddOrSetupDialogAnimatorSlide(MaterialDialogCompat materialDialog, float animationDuration = 0.5f)
        {
            var animator = GetOrAddTypedDialogAnimator<DialogAnimatorSlide>(materialDialog);
            if (animator != null)
            {
                animator.m_AnimationDuration = animationDuration;
            }

            return animator;
        }

        public static DialogAnimatorSlide AddOrSetupDialogAnimatorSlide(MaterialDialogCompat materialDialog, float animationDuration, SlideDirection slideInDirection, SlideDirection slideOutDirection)
        {
            var animator = GetOrAddTypedDialogAnimator<DialogAnimatorSlide>(materialDialog);
            if (animator != null)
            {
                animator.m_AnimationDuration = animationDuration;
                animator.m_SlideInDirection = slideInDirection;
                animator.m_SlideOutDirection = slideOutDirection;
            }

            return animator;
        }

        public static DialogAnimatorSlide AddOrSetupDialogAnimatorSlide(MaterialDialogCompat materialDialog, float animationDuration, SlideDirection slideInDirection, SlideDirection slideOutDirection, Tween.TweenType slideInTweenType, Tween.TweenType slideOutTweenType)
        {
            var animator = GetOrAddTypedDialogAnimator<DialogAnimatorSlide>(materialDialog);
            if (animator != null)
            {
                animator.m_AnimationDuration = animationDuration;
                animator.m_SlideInDirection = slideInDirection;
                animator.m_SlideOutDirection = slideOutDirection;

                if (slideInTweenType == Tween.TweenType.Custom || slideOutTweenType == Tween.TweenType.Custom)
                {
                    Debug.LogWarning("Cannot set tween type to 'Custom'");
                }
                else
                {
                    animator.m_SlideInTweenType = slideInTweenType;
                    animator.m_SlideOutTweenType = slideOutTweenType;
                }
            }

            return animator;
        }

        #endregion
    }
}