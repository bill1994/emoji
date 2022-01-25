using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Checkbox List", 1)]
    public class DialogCheckboxList : BaseDialogList
    {
        #region Private Variables

        private HashSet<int> m_SelectedIndexes = new HashSet<int>();
        Action<int[]> _onAffirmativeButtonClicked = null;

        #endregion

        #region Callbacks

        public DialogClickableOption.IntUnityEvent onSelectedIndexChanged = new DialogClickableOption.IntUnityEvent();

        #endregion

        #region Public Properties

        public HashSet<int> selectedIndexes
        {
            get
            {
                if (m_SelectedIndexes == null)
                    m_SelectedIndexes = new HashSet<int>();
                return m_SelectedIndexes;
            }
            set
            {
                if (m_SelectedIndexes == value)
                    return;

                m_SelectedIndexes = value;
                if (m_SelectedIndexes != null)
                {
                    foreach (var index in m_SelectedIndexes)
                    {
                        if (onSelectedIndexChanged != null)
                            onSelectedIndexChanged.Invoke(index);
                    }
                }
                if (m_ScrollDataView != null)
                    m_ScrollDataView.FullReloadAll();
            }
        }

        #endregion

        #region Helper Functions

        public virtual void Initialize(IList<string> optionsStr, Action<int[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, ICollection<int> selectedIndexesStart)
        {
            OptionData[] options = new OptionData[optionsStr != null ? optionsStr.Count : 0];
            if (optionsStr != null)
            {
                for (int i = 0; i < optionsStr.Count; i++)
                {
                    options[i] = new OptionData(optionsStr[i], null);
                }
            }
            Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexesStart);
        }

        public virtual void Initialize<TOptionData>(IList<TOptionData> options, Action<int[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, ICollection<int> selectedIndexesStart) where TOptionData : OptionData, new()
        {
            if (options == null)
                options = new TOptionData[0];

            _onAffirmativeButtonClicked = onAffirmativeButtonClicked;
            BaseInitialize(options, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            selectedIndexes = selectedIndexesStart == null ? new HashSet<int>() : new HashSet<int>(selectedIndexesStart);
        }

        public override bool IsDataIndexSelected(int dataIndex)
        {
            return selectedIndexes.Contains(dataIndex);
        }

        #endregion

        #region Receivers

        protected override void HandleOnAffirmativeButtonClicked()
        {
            var canvasGroup = this.GetAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            if (_onAffirmativeButtonClicked != null)
                _onAffirmativeButtonClicked.InvokeIfNotNull(selectedIndexes.ToArray());
            Hide();
        }

        protected override void HandleOnItemClicked(int dataIndex)
        {
            if (selectedIndexes.Contains(dataIndex))
            {
                selectedIndexes.Remove(dataIndex);
                if (onSelectedIndexChanged != null)
                    onSelectedIndexChanged.Invoke(dataIndex);
                if (m_ScrollDataView)
                    m_ScrollDataView.FullReloadAll();
            }
            else if (dataIndex >= 0 && dataIndex < options.Count && !selectedIndexes.Contains(dataIndex))
            {
                selectedIndexes.Add(dataIndex);
                if (onSelectedIndexChanged != null)
                    onSelectedIndexChanged.Invoke(dataIndex);
                if (m_ScrollDataView)
                    m_ScrollDataView.FullReloadAll();
            }
        }

        #endregion
    }
}