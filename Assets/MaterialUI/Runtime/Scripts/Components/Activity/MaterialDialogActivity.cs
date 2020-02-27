using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Linq;

namespace MaterialUI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MaterialDialogActivity : MaterialActivity
    {
        #region Private Variables

        [SerializeField]
        MaterialFrame m_Frame = null;
        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_AutoCreateBackground")]
        bool m_HasBackground = true;
        [SerializeField]
        MaterialActivityBackground m_Background = null;
        [SerializeField]
        bool m_IsModal;

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

        public bool isModal
        {
            get { return m_IsModal; }
            set { m_IsModal = value; }
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

        #endregion

        #region Public Functions

        //Replace internal frame
        public MaterialFrame SetFrame(MaterialFrame frame, bool inflate)
        {
            if (frame == null)
                return null;

            var anchoredPosition = frame.transform is RectTransform? (frame.transform as RectTransform).anchoredPosition3D : frame.transform.localPosition;
            var localScale = frame.transform.localScale;
            var localRotation = frame.transform.localRotation;

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

            if (inflate)
                Inflate(frame.transform as RectTransform, false);
            else
            {
                if (frame.transform is RectTransform)
                    (frame.transform as RectTransform).anchoredPosition3D = anchoredPosition;
                else
                    frame.transform.localPosition = anchoredPosition;
                frame.transform.localScale = localScale;
                frame.transform.localRotation = localRotation;
            }

            m_Frame.transform.SetAsLastSibling();

            return frame;
        }

        //Set in pre-created frame a content as a child
        public RectTransform SetFrameContent(RectTransform frameContent)
        {
            if (frameContent == null)
                return null;

            if (m_Frame == null)
            {
                var frame = frameContent.GetComponent<MaterialFrame>();
                if (frame == null)
                {
                    Debug.LogWarning("DialogActivity requires a valid MaterialFrame");
                    return null;
                }
                //Instantiate as a Frame
                else
                {
                    frame = SetFrame(frame, false);
                    return frame != null ? frame.transform as RectTransform : null;
                }
            }

            //Is a prefab
            if (!frameContent.root.gameObject.scene.IsValid())
                frameContent = GameObject.Instantiate(frameContent);

            var content = m_Frame.transform;
            frameContent.SetParent(content);

            Inflate(frameContent as RectTransform, false);

            return frameContent;
        }

        public void ShowModal()
        {
            m_IsModal = true;
            Show();
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
            SetCanvasActive(true);
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
            if (!m_IsModal)
            {
                Hide();
            }
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
                m_Frame.OnActivityBeginShow();

            System.Action showAction = () =>
            {
                callback.InvokeIfNotNull();
                if (m_Frame != null)
                    m_Frame.OnActivityEndShow();
            };

            if (m_Frame)
                m_Frame.gameObject.SetActive(true);

            var tweener = frame != null ? frame.GetComponent<AbstractTweenBehaviour>() : null;
            if (tweener != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.transform as RectTransform);
                tweener.Tween("show", (tag) => { showAction.InvokeIfNotNull(); });
            }
            else
                showAction.InvokeIfNotNull();

        }

        void HideFrame(System.Action callback)
        {
            if (m_Frame != null)
                m_Frame.OnActivityBeginHide();

            System.Action hideAction = () =>
            {
                callback.InvokeIfNotNull();
                if (m_Frame != null)
                    m_Frame.OnActivityEndHide();
            };

            var tweener = frame != null ? frame.GetComponent<AbstractTweenBehaviour>() : null;
            if (tweener != null)
            {
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
                    bgImage.color = new Color(0, 0, 0, 0.5f);
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