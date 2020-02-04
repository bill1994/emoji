using Kyub.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                value = Mathf.Clamp(value, -1, (_Options != null ? _Options.Count : 0) - 1);
                if (_SelectedIndex == value)
                    return;
                _SelectedIndex = value;
                if (onSelectedIndexChanged != null)
                    onSelectedIndexChanged.Invoke(_SelectedIndex);
                if (m_ScrollDataView)
                    m_ScrollDataView.FullReloadAll();
            }
        }

        public override IList<OptionData> options
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
                    m_ScrollDataView.Setup(options is IList || options == null ? (IList)options : options.ToArray());
                }
            }
        }

        #endregion

        #region Public Functions

        public virtual void Initialize(IList<string> optionsStr, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart, bool allowSwitchOff = false)
        {
            OptionData[] options = new OptionData[optionsStr != null ? optionsStr.Count : 0];
            if (optionsStr != null)
            {
                for (int i = 0; i < optionsStr.Count; i++)
                {
                    options[i] = new OptionData(optionsStr[i], null);
                }
            }
            Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexStart);
        }

        public virtual void Initialize<TOptionData>(IList<TOptionData> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart, bool allowSwitchOff = false) where TOptionData : OptionData, new()
        {
            if (options == null)
                options = new TOptionData[0];

            _onAffirmativeButtonClicked = onAffirmativeButtonClicked;
            BaseInitialize(options, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            _SelectedIndex = selectedIndexStart < 0 ? (!m_AllowSwitchOff && options.Count > 0 ? 0 : -1) : selectedIndexStart;
            _SelectedIndex = Mathf.Clamp(_SelectedIndex, -1, (_Options != null ? _Options.Count : 0) - 1);
        }

        public override bool IsDataIndexSelected(int dataIndex)
        {
            return dataIndex >= 0 && dataIndex < options.Count && dataIndex == selectedIndex;
        }

        #endregion

        #region Receivers

        protected override void HandleOnAffirmativeButtonClicked()
        {
            var canvasGroup = this.GetAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            if (_onAffirmativeButtonClicked != null)
                _onAffirmativeButtonClicked.InvokeIfNotNull(_SelectedIndex);
            Hide();
        }

        protected override void HandleOnItemClicked(int dataIndex)
        {
            selectedIndex = selectedIndex == dataIndex ? (!m_AllowSwitchOff ? selectedIndex : -1) : dataIndex;
        }

        #endregion

    }
}