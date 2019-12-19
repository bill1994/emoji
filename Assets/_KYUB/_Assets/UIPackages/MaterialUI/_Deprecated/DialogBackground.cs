//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

namespace MaterialUI
{
    //[AddComponentMenu("MaterialUI/Dialogs/Background", 100)]
    [RequireComponent(typeof(CanvasGroup))]
    [System.Obsolete]
    public class DialogBackground : MonoBehaviour, IPointerClickHandler
    {
        #region Helper Functions

        [SerializeField]
        bool m_keepDefaultTransformPresets = false;
        [SerializeField]
        float m_BackgroundAlpha = 0.5f;
        [SerializeField]
        Graphic m_BackgroundGraphic = null;

        private int m_siblingIndex = 0;

        private Color m_BackgroundColor = Color.black;
        private CanvasGroup m_canvasGroup;

        #endregion

        #region Callbacks

        public Action OnBackgroundClick;

        #endregion

        #region Public Properties

        public float backgroundAlpha
        {
            get { return m_BackgroundAlpha; }
            set { m_BackgroundAlpha = value; }
        }

        public Color backgroundColor
        {
            get { return m_BackgroundColor; }
            set
            {
                m_BackgroundColor = value;

                if(m_BackgroundGraphic == null)
                    m_BackgroundGraphic = GetComponent<Graphic>();

                if (m_BackgroundGraphic != null)
                {
                    m_BackgroundGraphic.color = backgroundColor;
                }
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (m_canvasGroup == null)
                {
                    m_canvasGroup = GetComponent<CanvasGroup>();
                }

                return m_canvasGroup;
            }
        }

        public Graphic backgroundGraphic
        {
            get
            {
                return m_BackgroundGraphic;
            }

            set
            {
                if (m_BackgroundGraphic == value)
                    return;

                m_BackgroundGraphic = value;
                if (m_BackgroundGraphic != null)
                    m_BackgroundGraphic.color = m_BackgroundColor;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Start()
        {
            Initialize();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (OnBackgroundClick != null)
            {
                OnBackgroundClick();
            }
        }

        #endregion

        #region Helper Functions

        public void Initialize()
        {
            if (!m_keepDefaultTransformPresets)
            {
                RectTransform backgroundTransform = transform as RectTransform;
                backgroundTransform.SetSiblingIndex(m_siblingIndex);
                backgroundTransform.anchoredPosition = Vector2.zero;
                backgroundTransform.sizeDelta = Vector2.zero;
            }
        }

        public void SetSiblingIndex(int index)
        {
            if (!m_keepDefaultTransformPresets)
            {
                RectTransform backgroundTransform = transform as RectTransform;
                backgroundTransform.SetSiblingIndex(index);
                m_siblingIndex = index;
            }
        }

        int _tweenId = -1;
        bool _isHiding = true;
        public virtual void AnimateShowBackground(Action callback = null, float animationDuration = 0.5f, float delay = 0f)
        {
            if (_isHiding || _tweenId < 0)
            {
                _isHiding = false;
                canvasGroup.blocksRaycasts = true;
                TweenManager.EndTween(_tweenId);
                _tweenId = TweenManager.TweenFloat(
                    f => 
                    {
                        if (this != null && canvasGroup != null)
                            canvasGroup.alpha = f;
                    }, 
                    canvasGroup.alpha, 
                    m_BackgroundAlpha, 
                    animationDuration, 
                    delay, 
                    callback);
            }
        }

        public virtual void AnimateHideBackground(Action callback = null, float animationDuration = 0.5f, float delay = 0f)
        {
            if(!_isHiding || _tweenId < 0)
            {
                _isHiding = true;
                canvasGroup.blocksRaycasts = false;
                
                TweenManager.EndTween(_tweenId);
                _tweenId = TweenManager.TweenFloat(
                    f =>
                    {
                        if (this != null && canvasGroup != null)
                            canvasGroup.alpha = f;
                    },
                    canvasGroup.alpha,
                    0f,
                    animationDuration,
                    delay,
                    callback += () =>
                    {
                        if (this != null && gameObject != null)
                        {
                            Destroy(gameObject, 0.01f);
                        }
                    },
                    false,
                    Tween.TweenType.Linear);
            }
        }

        #endregion
    }
}