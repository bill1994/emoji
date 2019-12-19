//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [System.Obsolete("Use EasyFrameAnimator instead")]
    public abstract class DialogAnimator : MonoBehaviour
    {
        protected MaterialDialogCompat m_Dialog;
        public MaterialDialogCompat dialog
        {
            get
            {
                if (m_Dialog == null)
                    m_Dialog = GetComponent<MaterialDialogCompat>();
                return m_Dialog;
            }
            set { m_Dialog = value; }
        }

        [SerializeField]
        protected float m_AnimationDuration = 0.5f;
        public float animationDuration
        {
            get { return m_AnimationDuration; }
            set { m_AnimationDuration = value; }
        }

        private DialogBackground m_Background;
        public DialogBackground background
        {
            get
            {
                if (m_Dialog != null && m_Dialog.hasBackground && m_Background == null)
                {
                    m_Background = PrefabManager.InstantiateGameObject("Dialogs/Dialog Background", m_Dialog.rectTransform.parent).GetComponent<DialogBackground>();
                    m_Background.SetSiblingIndex(m_Dialog.rectTransform.GetSiblingIndex());
                }

                return m_Background;
            }
            set
            {
                if (m_Background == value)
                    return;
                m_Background = value;
            }
        }

        public DialogBackground GetBackgroundNonAlloc()
        {
            return m_Background;
        }

        public virtual void AnimateShow(Action callback)
        {
            if (m_Dialog != null && m_Dialog.hasBackground)
                background.AnimateShowBackground(callback, animationDuration);
        }

        public virtual void AnimateHide(Action callback)
        {
            if (m_Background != null)
                m_Background.AnimateHideBackground(callback, animationDuration);
        }

        #region Static Setup Functions

        protected static TDialogAnimator GetOrAddTypedDialogAnimator<TDialogAnimator>(MaterialDialogCompat materialDialog) where TDialogAnimator : DialogAnimator
        {
            if (materialDialog != null)
            {
                if (!Application.isPlaying)
                {
                    return materialDialog.GetComponent<TDialogAnimator>();
                }
                else
                {
                    TDialogAnimator dialogAnimator = null;
                    var currentAnimator = materialDialog.GetComponent<DialogAnimator>();
                    if (currentAnimator != null && !(currentAnimator is TDialogAnimator))
                    {
                        UnityEngine.Object.Destroy(currentAnimator);
                        currentAnimator = null;
                    }

                    dialogAnimator = currentAnimator as TDialogAnimator;
                    if (dialogAnimator == null)
                        dialogAnimator = materialDialog.gameObject.AddComponent<TDialogAnimator>();

                    return dialogAnimator;
                }
            }
            return null;
        }

        #endregion
    }
}