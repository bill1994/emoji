//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Kyub.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    public abstract class BaseDialogList : MaterialDialogCompat
    {
        #region Private Variables

        [SerializeField]
        protected DialogTitleSection m_TitleSection = null;
        [SerializeField]
        protected DialogButtonSection m_ButtonSection = null;
        [Space]
        [SerializeField]
        protected ScrollDataView m_ScrollDataView = null;
        [SerializeField]
        protected DialogClickableOption m_OptionTemplate = null;

        protected OptionData[] _Options;

        protected Action _onDismissiveButtonClicked = null;

        #endregion

        #region Properties

        public DialogTitleSection titleSection
        {
            get { return m_TitleSection; }
            set { m_TitleSection = value; }
        }
        public DialogButtonSection buttonSection
        {
            get { return m_ButtonSection; }
            set { m_ButtonSection = value; }
        }

        public ScrollDataView scrollDataView
        {
            get { return m_ScrollDataView; }
            set
            {
                if (m_ScrollDataView == null)
                    return;
                UnregisterEvents();
                m_ScrollDataView = value;
                if (enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
            }
        }

        public DialogClickableOption optionTemplate
        {
            get { return m_OptionTemplate; }
            set { m_OptionTemplate = value; }
        }

        public virtual OptionData[] options
        {
            get
            {
                if (_Options == null)
                    _Options = new OptionData[0];
                return _Options;
            }
            protected set
            {
                if (_Options == value)
                    return;
                _Options = value;

                if (m_ScrollDataView != null)
                {
                    m_ScrollDataView.DefaultTemplate = m_OptionTemplate != null ? m_OptionTemplate.gameObject : m_ScrollDataView.DefaultTemplate;
                    m_ScrollDataView.Setup(options);
                }
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterEvents();
            OverscrollConfig overscrollConfig = GetComponentInChildren<OverscrollConfig>();

            if (overscrollConfig != null)
            {
                overscrollConfig.Setup();
            }
        }

        protected override void OnDisable()
        {
            UnregisterEvents();
            base.OnDisable();
        }

        #endregion

        #region Helper Functions

        protected virtual void BaseInitialize(OptionData[] options, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            ClearList();

            _Options = options;


            if (m_ScrollDataView != null)
            {
                m_ScrollDataView.DefaultTemplate = m_OptionTemplate != null ? m_OptionTemplate.gameObject : m_ScrollDataView.DefaultTemplate;
                m_ScrollDataView.OnReloadElement.AddListener(HandleOnReloadElement);
                m_ScrollDataView.Setup(options);
            }

            if (m_TitleSection != null)
                m_TitleSection.SetTitle(titleText, icon);


            _onDismissiveButtonClicked = onDismissiveButtonClicked;
            
            if (m_ButtonSection != null)
            {
                m_ButtonSection.SetButtons(AffirmativeButtonClicked, affirmativeButtonText, DismissiveButtonClicked, dismissiveButtonText);
                m_ButtonSection.SetupButtonLayout(rectTransform);
            }
        }

        public virtual void ClearList()
        {
            options = null;
        }

        protected override void ValidateKeyTriggers(MaterialFocusGroup p_materialKeyFocus)
        {
            if (p_materialKeyFocus != null)
            {
                var v_affirmativeTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_affirmativeTrigger.Name = "Return KeyDown";
                v_affirmativeTrigger.Key = KeyCode.Return;
                v_affirmativeTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_affirmativeTrigger.OnCallTrigger, AffirmativeButtonClicked);

                var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_cancelTrigger.Name = "Escape KeyDown";
                v_cancelTrigger.Key = KeyCode.Escape;
                v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, DismissiveButtonClicked);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_affirmativeTrigger, v_cancelTrigger };
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (m_ScrollDataView != null)
                m_ScrollDataView.OnReloadElement.AddListener(HandleOnReloadElement);
        }

        protected virtual void UnregisterEvents()
        {
            if (m_ScrollDataView != null)
                m_ScrollDataView.OnReloadElement.RemoveListener(HandleOnReloadElement);
        }

        public abstract void AffirmativeButtonClicked();

        public virtual void DismissiveButtonClicked()
        {
            if (_onDismissiveButtonClicked != null)
                _onDismissiveButtonClicked.InvokeIfNotNull();
            Hide();
        }

        public abstract bool IsDataIndexSelected(int dataIndex);

        #endregion

        #region Receivers

        protected virtual void HandleOnReloadElement(ScrollDataView.ReloadEventArgs args)
        {
            var clickableOption = args.LayoutElement != null? args.LayoutElement.GetComponent<DialogClickableOption>() : null;
            if (clickableOption != null)
            {
                clickableOption.onItemClicked.RemoveListener(HandleOnItemClicked);
                clickableOption.onItemClicked.AddListener(HandleOnItemClicked);
            }
        }

        protected abstract void HandleOnItemClicked(int dataIndex);

        #endregion

        #region Activity Functions
        public override void OnActivityEndShow()
        {
            var canvasGroup = this.GetAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            base.OnActivityEndShow();
        }

        public override void OnActivityBeginHide()
        {
            var canvasGroup = this.GetAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            base.OnActivityBeginHide();
        }

        #endregion
    }
}