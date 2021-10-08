using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialDropdown : BaseSpinner<EmptyStyleProperty>, IOptionDataListContainer
    {
        #region Helper Classes

        [System.Serializable]
        public class DialogRadioAddress : ComponentPrefabAddress<DialogRadioList>
        {
            public static explicit operator DialogRadioAddress(string s)
            {
                return new DialogRadioAddress() { AssetPath = s };
            }
        }

        [System.Serializable]
        public class MaterialDropdownEvent : UnityEvent<int> { }

        [System.Serializable]
        public class DialogRadioUnityEvent : UnityEvent<DialogRadioList> { }

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
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_SupportEmptySelection")]
        protected bool m_AllowSwitchOff = true;
        [SerializeField, Tooltip("Use this property if dropdown require to be just an Action List Button")]
        protected bool m_PreventSelection = false;
        [SerializeField, Tooltip("Will always apply hint option to text/icon or will only show option when selectedIndex is -1?")]
        protected bool m_AlwaysDisplayHintOption = false;
        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_CurrentlySelected")]
        protected int m_SelectedIndex = 0;
        [SerializeField]
        DialogRadioAddress m_CustomFramePrefabAddress = null;
        [Space]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_EmptyOption")]
        protected OptionData m_HintOption = new OptionData() { text = "", imageData = new ImageData((VectorImageData)null) };
        [SerializeField]
        protected OptionDataList m_OptionDataList = null;

        protected DialogRadioList _CacheDialogList = null;
        protected PrefabAddress _CachedPrefabAdress = null;

        #endregion

        #region Callbacks

        public DialogRadioUnityEvent OnShowRadioDialogCallback = new DialogRadioUnityEvent();
        [Space]
        [UnityEngine.Serialization.FormerlySerializedAs("m_OnItemSelected")]
        public MaterialDropdownEvent OnItemSelected = new MaterialDropdownEvent();

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
                    Debug.LogWarning("[MaterialDropdown] CustomTextValidator can only be used when dropdown contains an inputfield");
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

        public virtual DialogRadioAddress customFramePrefabAddress
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

        public bool allowSwitchOff
        {
            get { return m_AllowSwitchOff; }
            set { m_AllowSwitchOff = value; }
        }

        public bool preventSelection
        {
            get { return m_PreventSelection; }
            set 
            {
                if (m_PreventSelection == value)
                    return;
                m_PreventSelection = value;
                Select(m_SelectedIndex);
            }
        }

        public OptionData hintOption
        {
            get {
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
                selectedIndex = -1;
            }
        }

        public int selectedIndex
        {
            get 
            {
                if (m_SelectedIndex >= 0 && m_PreventSelection)
                    m_SelectedIndex = -1;

                return m_SelectedIndex; 
            }
            set
            {
                var clampedValue = Mathf.Clamp(value, -1, options.Count - 1);

                if (clampedValue == m_SelectedIndex)
                    return;
                Select(clampedValue);
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
                selectedIndex = -1;
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

            var prefabAddress = cachedPrefabAddress == null || cachedPrefabAddress.IsEmpty() || !cachedPrefabAddress.IsResources() ? PrefabManager.ResourcePrefabs.dialogSimpleList : cachedPrefabAddress;
            ShowFrameActivity(_CacheDialogList, prefabAddress, (dialog, isDialog) => 
            {
                _CacheDialogList = dialog;
                if (dialog != null)
                {
                    if (isDialog)
                        dialog.Initialize(options.ToArray(), Select, "OK", hintOption.text, hintOption.imageData, HandleOnHide, "Cancel", selectedIndex, allowSwitchOff);
                    //Dont show title in Dropdown Mode
                    else
                        dialog.Initialize(options.ToArray(), Select, "OK", null, null, HandleOnHide, "Cancel", selectedIndex, allowSwitchOff);

                    if (this != null && OnShowRadioDialogCallback != null)
                        OnShowRadioDialogCallback.Invoke(dialog);
                }
            });
        }

        public override void Hide()
        {
            if (_CacheDialogList != null && _CacheDialogList.gameObject.activeSelf)
                _CacheDialogList.Hide();

            HandleOnHide();
        }

        protected override void HandleOnHide()
        {
            ValidateText(true);
            base.HandleOnHide();
        }

        #endregion

        #region Helper Functions

        public OptionData GetCurrentSelectedData()
        {
            return selectedIndex >= 0 && options.Count > selectedIndex ? options[selectedIndex] : null;
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
            options.Remove(data);

            selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Count - 1);
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

            selectedIndex = Mathf.Clamp(m_SelectedIndex, 0, options.Count - 1);
        }

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

        public virtual void Select(int selectedItemIndex)
        {
            if (options.Count == 0)
                selectedItemIndex = -1;
            else
                selectedItemIndex = m_AllowSwitchOff ? selectedItemIndex : Mathf.Clamp(selectedItemIndex, 0, options.Count - 1);

            var option = selectedItemIndex >= 0 && selectedItemIndex < options.Count ? options[selectedItemIndex] : null;
            if (option == null)
                option = m_HintOption;

            m_SelectedIndex = m_PreventSelection ? -1 : selectedItemIndex;

            UpdateLabelState();

            if (IsExpanded())
                Hide();

            if (OnItemSelected != null)
                OnItemSelected.Invoke(selectedItemIndex);

            if (option != null)
                option.onOptionSelected.InvokeIfNotNull();
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
//#if UNITY_EDITOR
//            Resources.UnloadUnusedAssets();
//#endif
        }

        protected virtual void UpdateLabelState()
        {
            var option = selectedIndex >= 0 && selectedIndex < options.Count ? options[selectedIndex] : null;

            if (textComponent != null)
                textComponent.SetGraphicText(option != null && !string.IsNullOrEmpty(option.text) ? option.text : "\u200B");

            if (iconComponent != null)
                iconComponent.SetImageData(option != null ? option.imageData : null);

            //Apply Hint Option
            var hintOption = option == null || m_AlwaysDisplayHintOption ? m_HintOption : null;
            if (hintTextComponent != null && ((textComponent != hintTextComponent) || (hintOption != null && option == null)))
                hintTextComponent.SetGraphicText(hintOption != null && !string.IsNullOrEmpty(hintOption.text)? hintOption.text : "\u200B");

            if (hintIconComponent != null && ((iconComponent != hintIconComponent) || (hintOption != null && option == null)))
                hintIconComponent.SetImageData(hintOption != null ? hintOption.imageData : null);

#if UNITY_EDITOR
            if(!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        #endregion

    }
}
