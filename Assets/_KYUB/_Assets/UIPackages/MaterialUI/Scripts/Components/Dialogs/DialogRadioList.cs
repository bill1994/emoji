//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Kyub.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Radio List", 1)]
    public class DialogRadioList : BaseDialogList
    {
        #region Private Variables

        [SerializeField]
        protected bool m_AllowSwitchOff = false;

        protected int _SelectedIndex = 0;

        protected System.Action<int> _onAffirmativeButtonClicked = null;
        #endregion

        #region Callbacks

        public DialogClickableOption.IntUnityEvent onSelectedIndexChanged = new DialogClickableOption.IntUnityEvent();

        #endregion

        #region Properties

        public int selectedIndex
        {
            get { return _SelectedIndex; }
            protected set
            {
                value = value < 0 ? (!m_AllowSwitchOff ? _SelectedIndex : -1) : value;
                value = Mathf.Clamp(value, -1, (_Options != null? _Options.Length : 0) - 1);
                if (_SelectedIndex == value)
                    return;
                _SelectedIndex = value;
                if (onSelectedIndexChanged != null)
                    onSelectedIndexChanged.Invoke(_SelectedIndex);
                if (m_ScrollDataView)
                    m_ScrollDataView.FullReloadAll();
            }
        }

        public override OptionData[] options
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

                selectedIndex = -1;
                if (m_ScrollDataView != null)
                {
                    m_ScrollDataView.DefaultTemplate = m_OptionTemplate != null ? m_OptionTemplate.gameObject : m_ScrollDataView.DefaultTemplate;
                    m_ScrollDataView.Setup(options);
                }
            }
        }

        #endregion

        #region Public Functions

        public virtual void Initialize(string[] optionsStr, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart, bool allowSwitchOff = false)
        {
            OptionData[] options = new OptionData[optionsStr != null? optionsStr.Length : 0];
            for(int i=0; i<optionsStr.Length; i++)
            {
                options[i] = new OptionData(optionsStr[i], null);
            }
            Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexStart);
        }

        public virtual void Initialize(OptionData[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart, bool allowSwitchOff = false)
        {
            _onAffirmativeButtonClicked = onAffirmativeButtonClicked;
            BaseInitialize(options, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            _SelectedIndex = selectedIndexStart < 0 ? (!m_AllowSwitchOff && options.Length > 0? 0 : -1) : selectedIndexStart;
            _SelectedIndex = Mathf.Clamp(_SelectedIndex, -1, (_Options != null ? _Options.Length : 0) - 1);
        }

        public override void AffirmativeButtonClicked()
        {
            var canvasGroup = this.GetAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            if (_onAffirmativeButtonClicked != null)
                _onAffirmativeButtonClicked.InvokeIfNotNull(_SelectedIndex);
            Hide();
        }

        public override bool IsDataIndexSelected(int dataIndex)
        {
            return dataIndex >= 0 && dataIndex < options.Length && dataIndex == selectedIndex;
        }

        #endregion

        #region Receivers

        protected override void HandleOnItemClicked(int dataIndex)
        {
            selectedIndex = selectedIndex == dataIndex? (!m_AllowSwitchOff ? selectedIndex : -1) : dataIndex;
        }

        #endregion

    }
}