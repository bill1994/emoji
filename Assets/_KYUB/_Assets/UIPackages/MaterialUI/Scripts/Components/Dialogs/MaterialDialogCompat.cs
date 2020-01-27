//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MaterialUI
{
    public abstract class MaterialDialogCompat : MaterialDialogFrame
    {
        #region Private Variables

        [SerializeField]
        protected bool m_UseFocusGroup = true;

        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_AutoCreateBackground")]
        protected bool m_HasBackground = true;
        [SerializeField]
        MaterialActivityBackground m_Background = null;
        [SerializeField]
        protected Color m_BackgroundColor = new Color(0, 0, 0, 0.5f);

        [Space]
        [SerializeField]
        protected bool m_DestroyOnHide = true;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_changeSibling")]
        protected bool m_ChangeSibling = true;
        [Space]
        [SerializeField]
        bool m_IsModal = false;
        [SerializeField, Tooltip("New dialog system inherit from MaterialFrame.\n" +
            "In old system the Dialog can show by it self, but in new system we must attach it to a MaterialActivity before show.\n" +
            "This option auto-attach the dialog to a MaterialDialogActivity")]
        bool m_LegacyActivityCompatibility = true;

        protected RectTransform m_RectTransform;

        #endregion

        #region Public Properties

        public bool legacyActivityCompatibility
        {
            get { return m_LegacyActivityCompatibility; }
            set { m_LegacyActivityCompatibility = value; }
        }

        public bool useFocusGroup
        {
            get { return m_UseFocusGroup; }
            set
            {
                if (m_UseFocusGroup == value)
                    return;
                m_UseFocusGroup = value;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (this != null)
                {
                    if (m_RectTransform == null)
                    {
                        m_RectTransform = transform as RectTransform;
                    }
                }

                return m_RectTransform;
            }
        }

		public bool isModal
		{
			get { return m_IsModal; }
			set
            {
                if (m_IsModal == value)
                    return;
                m_IsModal = value;
                ApplyPropertiesToActivity();
            }
        }

        public MaterialActivityBackground background
        {
            get { return m_Background; }
            set
            {
                if (m_Background == value)
                    return;
                m_Background = value;
                ApplyPropertiesToActivity(true);
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
                ApplyPropertiesToActivity();
            }
        }

        public Color backgroundColor
        {
            get { return m_BackgroundColor; }
            set
            {
                if (m_BackgroundColor == value)
                    return;
                m_BackgroundColor = value;
                ApplyPropertiesToActivity();
            }
        }

		public bool destroyOnHide
		{
			get { return m_DestroyOnHide; }
			set
            {
                if (m_DestroyOnHide == value)
                    return;
                m_DestroyOnHide = value;
                ApplyPropertiesToActivity();
            }
		}

        public bool changeSibling
        {
            get { return m_ChangeSibling; }
            set
            {
                if (m_ChangeSibling == value)
                    return;
                m_ChangeSibling = value;
                ApplyPropertiesToActivity();
            }
        }

        #endregion

        #region Helper Functions

        protected virtual void ValidateKeyTriggers(MaterialFocusGroup p_materialKeyFocus)
        {
            if (p_materialKeyFocus != null)
            {
                var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_cancelTrigger.Name = "Escape KeyDown";
                v_cancelTrigger.Key = KeyCode.Escape;
                v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, Hide);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_cancelTrigger };
            }
        }

        #endregion

        #region Public Functions

        public override void Hide()
        {
            if (activity == null && m_LegacyActivityCompatibility)
            {
                //Create Activity in Main Canvas
                if (this.transform.parent == null)
                    activity = DialogManager.CreateActivity(this);
                //Create Activity inside this parent
                else
                    activity = DialogManager.CreateActivity(this, this.transform.parent);
            }
            base.Hide();
        }

        public override void Show()
        {
            if (activity == null && m_LegacyActivityCompatibility)
            {
                //Create Activity in Main Canvas
                if(this.transform.parent == null)
                    activity = DialogManager.CreateActivity(this);
                //Create Activity inside this parent
                else
                    activity = DialogManager.CreateActivity(this, this.transform.parent);
            }
            base.Show();
        }

        #endregion

        #region Helper Functions

        protected override void OnAttachedActivityChanged(MaterialActivity activity)
        {
            ApplyPropertiesToActivity(true);
        }

        protected internal override void OnActivityBeginShow()
        {
            if (activity != null)
                ValidateKeyTriggers(activity.focusGroup);

            var materialAtivity = activity as MaterialDialogActivity;

            ApplyPropertiesToActivity();
            base.OnActivityBeginShow();
        }

        protected virtual void ApplyPropertiesToActivity(bool fullRecalc = false)
        {
            var materialAtivity = activity as MaterialDialogActivity;

            if (materialAtivity != null)
            {
                if (fullRecalc)
                {
                    if (GetComponent<AbstractTweenBehaviour>() == null)
                    {
                        var animator = this.GetAddComponent<EasyFrameAnimator>();
                        animator.slideIn = true;
                        animator.slideInDirection = ScreenView.SlideDirection.Down;
                        animator.slideOut = true;
                        animator.slideOutDirection = ScreenView.SlideDirection.Down;
                    }
                }
                materialAtivity.destroyOnHide = m_DestroyOnHide;
                materialAtivity.isModal = m_IsModal;
                materialAtivity.hasBackground = m_HasBackground;
                materialAtivity.changeSibling = m_ChangeSibling;
                if(fullRecalc)
                    materialAtivity.background = m_Background;

                if (materialAtivity.background != null)
                    materialAtivity.background.backgroundColor = m_BackgroundColor;
            }
        }

        #endregion
    }
}