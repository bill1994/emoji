using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialMultiDropdown : BaseSpinner<EmptyStyleProperty>, IOptionDataListContainer
    {
        #region Helper Classes

        [System.Serializable]
        public class DialogCheckboxAddress : ComponentPrefabAddress<DialogCheckboxList>
        {
            public static explicit operator DialogCheckboxAddress(string s)
            {
                return new DialogCheckboxAddress() { AssetPath = s };
            }
        }

        [System.Serializable]
        public class MaterialMultiDropdownEvent : UnityEvent<int[]> { }

        [System.Serializable]
        public class DialogCheckboxUnityEvent : UnityEvent<DialogCheckboxList> { }

        #endregion

        #region Private Variables

        [Space]
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_HintTextComponent = null;
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_HintIconComponent = null;
        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_ButtonTextContent")]
        protected Graphic m_TextComponent = null;
        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_ButtonImageContent")]
        protected Graphic m_IconComponent = null;
        [SerializeField, Tooltip("Will always apply hint option to text/icon or will only show option when selectedIndex is -1?")]
        protected bool m_AlwaysDisplayHintOption = false;
        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_CurrentlySelected")]
        protected int[] m_SelectedIndexes = new int[0];
        [SerializeField]
        DialogCheckboxAddress m_CustomFramePrefabAddress = null;
        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_MixedOption")]
        protected OptionData m_MixedOption = new OptionData() { text = "{0}", imageData = new ImageData((VectorImageData)null) };
        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_EmptyOption")]
        protected OptionData m_HintOption = new OptionData() { text = "", imageData = new ImageData((VectorImageData)null) };
        [SerializeField]
        protected OptionDataList m_OptionDataList = null;

        protected DialogCheckboxList _CacheDialogList = null;
        protected PrefabAddress _CachedPrefabAdress = null;

        #endregion

        #region Callbacks

        public DialogCheckboxUnityEvent OnShowCheckboxDialogCallback = new DialogCheckboxUnityEvent();
        [Space]
        [UnityEngine.Serialization.FormerlySerializedAs("m_OnItemsSelected")]
        public MaterialMultiDropdownEvent OnItemsSelected = new MaterialMultiDropdownEvent();

        #endregion

        #region Properties

        public MaterialInputField inputField
        {
            get
            {
                var inputField = GetComponentInChildren<MaterialInputField>(true);
                return inputField;
            }
        }

        public ITextValidator customTextValidator
        {
            get
            {
                return inputField != null ? inputField.customTextValidator : null;
            }
            set
            {
                if (inputField != null)
                    inputField.customTextValidator = value;
#if UNITY_EDITOR
                else
                {
                    Debug.LogWarning("[MaterialMultiDropdown] CustomTextValidator can only be used when multi-dropdown contains an inputfield");
                }
#endif
            }
        }

        protected PrefabAddress cachedPrefabAddress
        {
            get
            {
                if (_CachedPrefabAdress == null)
                    _CachedPrefabAdress = (PrefabAddress)m_CustomFramePrefabAddress;
                return _CachedPrefabAdress;
            }
        }

        public virtual DialogCheckboxAddress customFramePrefabAddress
        {
            get
            {
                return m_CustomFramePrefabAddress;
            }
            set
            {
                if (m_CustomFramePrefabAddress == value)
                    return;
                ClearCache(false);
                m_CustomFramePrefabAddress = value;
                _CachedPrefabAdress = (PrefabAddress)m_CustomFramePrefabAddress;
            }
        }

        public virtual Graphic hintTextComponent
        {
            get
            {
                return m_HintTextComponent;
            }
        }

        public virtual Graphic hintIconComponent
        {
            get
            {
                return m_HintIconComponent;
            }
        }

        public virtual Graphic textComponent
        {
            get
            {
                return m_TextComponent;
            }
        }

        public virtual Graphic iconComponent
        {
            get
            {
                return m_IconComponent;
            }
        }

        public bool alwaysDisplayHintOption
        {
            get { return m_AlwaysDisplayHintOption; }
            set
            {
                if (m_AlwaysDisplayHintOption == value)
                    return;
                m_AlwaysDisplayHintOption = value;
                UpdateLabelState();
            }
        }

        public OptionData hintOption
        {
            get
            {
                if (m_HintOption == null)
                    m_HintOption = new OptionData();
                return m_HintOption;
            }
            set
            {
                if (m_HintOption == value)
                    return;
                m_HintOption = value;
                UpdateLabelState();
            }
        }

        public List<OptionData> options
        {
            get
            {
                if (m_OptionDataList == null)
                    m_OptionDataList = new OptionDataList() { imageType = ImageDataType.VectorImage };
                return m_OptionDataList.options;
            }
            set
            {
                if (m_OptionDataList == null)
                    m_OptionDataList = new OptionDataList();

                if (m_OptionDataList.options == value)
                    return;
                m_OptionDataList.options = value;
                selectedIndexes = new int[0];
            }
        }

        public int[] selectedIndexes
        {
            get
            {
                if (m_SelectedIndexes == null)
                    m_SelectedIndexes = new int[0];
                return m_SelectedIndexes;
            }
            set
            {
                if (value == m_SelectedIndexes)
                    return;
                Select(value);
            }
        }

        public int selectedBitmaskIndexes
        {
            get
            {
                var mask = 0;
                foreach (int index in selectedIndexes)
                {
                    mask |= (int)Mathf.Pow(2, index);
                }
                return mask;
            }
            set
            {
                var changed = false;
                List<int> valueIndexes = new List<int>();
                for (int i = 0; i < Mathf.Max(options.Count, 32); i++)
                {
                    var currentCheckingBitmask = (int)Mathf.Pow(2, i);
                    if ((value & currentCheckingBitmask) == currentCheckingBitmask)
                    {
                        valueIndexes.Add(i);
                        changed = changed || !selectedIndexes.Contains(i);
                    }
                }
                changed = changed || valueIndexes.Count != selectedIndexes.Length;
                if (!changed)
                    return;
                Select(valueIndexes.ToArray());
            }
        }

        OptionDataList IOptionDataListContainer.optionDataList
        {
            get
            {
                if (m_OptionDataList == null)
                    m_OptionDataList = new OptionDataList();
                return m_OptionDataList;
            }
            set
            {
                if (m_OptionDataList == value)
                    return;
                m_OptionDataList = value;
                selectedIndexes = new int[0];
            }
        }


        #endregion

        #region Unity Functions

        protected override void OnDestroy()
        {
            ClearCache(true);
            base.OnDestroy();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ClearCache(false);
            UpdateLabelState();
        }

#endif

        #endregion

        #region Overriden Functions

        public override bool IsExpanded()
        {
            return (_CacheDialogList != null && _CacheDialogList.gameObject.activeSelf);
        }

        public override void Show()
        {
            if (options.Count == 0)
            {
                if (IsExpanded())
                    Hide();
                return;
            }

            var prefabAddress = cachedPrefabAddress == null || cachedPrefabAddress.IsEmpty() || !cachedPrefabAddress.IsResources() ? PrefabManager.ResourcePrefabs.dialogCheckboxList : cachedPrefabAddress;
            ShowFrameActivity(_CacheDialogList, prefabAddress, (dialog, isDialog) =>
            {
                _CacheDialogList = dialog;
                if (dialog != null)
                {
                    if (isDialog)
                        dialog.Initialize(options.ToArray(), Select, "OK", hintOption.text, hintOption.imageData, HandleOnHide, "Cancel", selectedIndexes);
                    //Dont show title in Dropdown Mode
                    else
                        dialog.Initialize(options.ToArray(), Select, "OK", null, null, HandleOnHide, "Cancel", selectedIndexes);

                    if (this != null && OnShowCheckboxDialogCallback != null)
                        OnShowCheckboxDialogCallback.Invoke(dialog);
                }
            });
        }

        public override void Hide()
        {
            if (_CacheDialogList != null && _CacheDialogList.gameObject.activeSelf)
                _CacheDialogList.Hide();

            HandleOnHide();
        }

        #endregion

        #region Helper Functions

        public virtual void ValidateText()
        {
            ValidateText(false);
        }

        public virtual void ValidateText(bool force)
        {
            var input = inputField;
            if (input != null)
                input.ValidateText(force);
        }

        public IList<OptionData> GetCurrentSelectedDatas()
        {
            List<OptionData> selectedData = new List<OptionData>();
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != null && selectedIndexes.Contains(i))
                {
                    selectedData.Add(options[i]);
                }
            }
            return selectedData;
        }

        public int GetDataIndexWithText(string text)
        {
            return string.IsNullOrEmpty(text) ? -1 : options.FindIndex(0, options.Count, a => a.text == text);
        }

        public void AddData(OptionData data)
        {
            options.Add(data);
        }

        public void AddData(OptionData[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                AddData(data[i]);
            }
        }

        public void RemoveData(OptionData data)
        {
            var index = options.IndexOf(data);
            if (index >= 0 && index < options.Count)
            {
                options.RemoveAt(index);
            }

            if (selectedIndexes.Contains(index))
                selectedIndexes = Array.FindAll(selectedIndexes, (int value) => { return value != index; }).ToArray();
        }

        public void RemoveData(OptionData[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                RemoveData(data[i]);
            }
        }

        public void ClearData()
        {
            options.Clear();

            selectedIndexes = new int[0];
        }

        public virtual void Select(int[] selectedItemIndexes)
        {
            if (selectedItemIndexes == null || options.Count == 0)
                selectedItemIndexes = new int[0];

            m_SelectedIndexes = new HashSet<int>(selectedItemIndexes).ToArray();

            var selectedOptions = GetCurrentSelectedDatas();
            if (selectedOptions.Count > 1 && m_MixedOption != null)
                selectedOptions.Add(m_MixedOption);
            else if (selectedOptions.Count == 0)
                selectedOptions.Add(m_HintOption);

            UpdateLabelState();

            if (IsExpanded())
                Hide();

            ValidateText(true);

            if (OnItemsSelected != null)
                OnItemsSelected.Invoke(m_SelectedIndexes);

            foreach (var option in selectedOptions)
            {
                if (option != null)
                    option.onOptionSelected.InvokeIfNotNull();
            }
        }

        protected virtual void ClearCache(bool hideIfExpanded)
        {
            if (_CachedPrefabAdress != null)
            {
                _CachedPrefabAdress.ClearCache();
                _CachedPrefabAdress = null;
            }
            if (m_CustomFramePrefabAddress != null)
                m_CustomFramePrefabAddress.ClearCache();
            if (hideIfExpanded && IsExpanded())
            {
                _CacheDialogList.activity.destroyOnHide = true;
                _CacheDialogList.Hide();
            }
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
        }

        protected virtual void UpdateLabelState()
        {
            var options = GetCurrentSelectedDatas();

            var optionText = "";
            if (options.Count > 1)
            {
                StringBuilder textBuilder = new StringBuilder();
                foreach (var option in options)
                {
                    if (textBuilder.Length > 0 && !string.IsNullOrEmpty(option.text))
                        textBuilder.Append(", ");
                    textBuilder.Append(option.text);
                }
                optionText = m_MixedOption != null ? string.Format(m_MixedOption.text, textBuilder.ToString().Trim()) : textBuilder.ToString().Trim();
            }
            else if (options.Count == 1)
            {
                optionText = options[0] != null ? options[0].text : string.Empty;
            }

            if (textComponent != null)
                textComponent.SetGraphicText(!string.IsNullOrEmpty(optionText) ? optionText : "\u200B");

            if (iconComponent != null)
                iconComponent.SetImageData(options.Count > 1 ? (m_MixedOption != null ? m_MixedOption.imageData : null) : (options.Count > 0 ? options[0].imageData : null));

            //Apply Hint Option
            var hintOption = options.Count == 0 || m_AlwaysDisplayHintOption ? m_HintOption : null;
            if (hintTextComponent != null && ((textComponent != hintTextComponent) || (hintOption != null && options.Count == 0)))
                hintTextComponent.SetGraphicText(hintOption != null && !string.IsNullOrEmpty(hintOption.text) ? hintOption.text : "\u200B");

            if (hintIconComponent != null && ((iconComponent != hintIconComponent) || (hintOption != null && options.Count == 0)))
                hintIconComponent.SetImageData(hintOption != null ? hintOption.imageData : null);

#if UNITY_EDITOR
            if(!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        #endregion

    }
}
