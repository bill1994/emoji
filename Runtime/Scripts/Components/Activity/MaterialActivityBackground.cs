//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MaterialUI
{
    //[AddComponentMenu("MaterialUI/Dialogs/Background", 100)]
    //[RequireComponent(typeof(CanvasGroup))]
    public class MaterialActivityBackground : Selectable, IPointerClickHandler
    {
        #region Helper Functions

        [SerializeField]
        protected CanvasGroup m_CanvasGroup = null;
        [SerializeField]
        protected AbstractTweenBehaviour m_Tweener;

        #endregion

        #region Callbacks

        public UnityEvent onBackgroundClick = new UnityEvent();

        #endregion

        #region Public Properties

        public AbstractTweenBehaviour tweener
        {
            get
            {
                if (m_Tweener == null)
                    m_Tweener = GetComponent<AbstractTweenBehaviour>();
                return m_Tweener;
            }
            set
            {
                if (m_Tweener == value)
                    return;
                m_Tweener = value;
            }
        }

        public float backgroundAlpha
        {
            get { return canvasGroup != null? m_CanvasGroup.alpha : 1; }
            set
            {
                if(canvasGroup != null)
                    m_CanvasGroup.alpha = value;
            }
        }

        public Color backgroundColor
        {
            get { return targetGraphic != null? targetGraphic.color : Color.black; }
            set
            {
                if (targetGraphic == null)
                    targetGraphic = GetComponent<Graphic>();

                if (targetGraphic != null)
                {
                    targetGraphic.color = value;
                }
            }
        }

        public CanvasGroup canvasGroup
        {
            get
            {
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = this.GetAddComponent<CanvasGroup>();
                }

                return m_CanvasGroup;
            }
            set
            {
                if (m_CanvasGroup == value)
                    return;
                m_CanvasGroup = value;
            }
        }

        #endregion

        #region Unity Functions

        public void OnPointerClick(PointerEventData eventData)
        {
            if (onBackgroundClick != null)
            {
                onBackgroundClick.Invoke();
            }
        }

        #endregion

        #region Helper Functions

        public virtual void AnimateShowBackground(Action callback = null)
        {
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            if (tweener != null)
                tweener.Tween("show", callback);
            else
                callback.InvokeIfNotNull();
        }

        public virtual void AnimateHideBackground(Action callback = null, float delay = 0f)
        {
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            if (tweener != null)
                tweener.Tween("hide", callback);
            else
                callback.InvokeIfNotNull();
        }

        #endregion
    }
}