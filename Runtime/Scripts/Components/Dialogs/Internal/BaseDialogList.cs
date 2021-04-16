using Kyub.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace MaterialUI
{
    public abstract class BaseDialogList : MaterialDialogCompat, ILayoutElement
    {
        public enum SearchInputFieldVisibilityMode { Manual, Auto }

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
        [SerializeField]
        protected MaterialInputField m_SearchInputField = null;
        [SerializeField]
        protected SearchInputFieldVisibilityMode m_SearchVisibilityMode = SearchInputFieldVisibilityMode.Manual;
        [SerializeField]
        protected int m_MinAmountOfElementsToShowSearch = 20;
        [SerializeField]
        protected bool m_SupportLocalizedSearch = true;

        protected IList<OptionData> _Options;
        protected Dictionary<string, string> _OptionsLocaleMap = new Dictionary<string, string>();

        protected Action _onDismissiveButtonClicked = null;

        #endregion

        #region Properties

        public bool SupportLocalizedSearch
        {
            get
            {
                return m_SupportLocalizedSearch;
            }
            set
            {
                if (m_SupportLocalizedSearch == value)
                    return;
                m_SupportLocalizedSearch = value;

                RecreateIndexedLocaleKeys(_Options, ref _OptionsLocaleMap);

                if (m_ScrollDataView != null)
                {
                    m_ScrollDataView.DefaultTemplate = m_OptionTemplate != null ? m_OptionTemplate.gameObject : m_ScrollDataView.DefaultTemplate;
                    var filteredOption = _Options == null ? null : new List<OptionData>(_Options);
                    ApplyFilterInList(filteredOption, m_SearchInputField != null ? m_SearchInputField.text : string.Empty, optionsLocaleMap);
                    m_ScrollDataView.Setup(filteredOption);
                }
            }
        }


        public SearchInputFieldVisibilityMode searchInputFieldVisiblityMode
        {
            get { return m_SearchVisibilityMode; }
            set
            {
                if (m_SearchVisibilityMode == value)
                    return;
                m_SearchVisibilityMode = value;

                var optionsCount = _Options != null ? _Options.Count : 0;
                if (m_SearchInputField != null && m_SearchVisibilityMode == SearchInputFieldVisibilityMode.Auto)
                    m_SearchInputField.gameObject.SetActive(optionsCount >= m_MinAmountOfElementsToShowSearch);
                ClearCachedLayoutValues();
            }
        }

        public int minAmountOfElementsToShowSearch
        {
            get { return m_MinAmountOfElementsToShowSearch; }
            set {

                if (m_MinAmountOfElementsToShowSearch == value)
                    return;
                m_MinAmountOfElementsToShowSearch = value;

                var optionsCount = _Options != null ? _Options.Count : 0;
                if (m_SearchInputField != null && m_SearchVisibilityMode == SearchInputFieldVisibilityMode.Auto)
                    m_SearchInputField.gameObject.SetActive(optionsCount >= m_MinAmountOfElementsToShowSearch);
                ClearCachedLayoutValues();
            }
        }

        public bool isSearchFilterActive
        {
            get
            {
                return m_SearchInputField != null && !string.IsNullOrEmpty(m_SearchInputField.text);
            }
        }

        public MaterialInputField searchInputField
        {
            get { return m_SearchInputField; }
            set { m_SearchInputField = value; }
        }

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

        public virtual IList<OptionData> options
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

                RecreateIndexedLocaleKeys(_Options, ref _OptionsLocaleMap);
                if (m_ScrollDataView != null)
                {
                    m_ScrollDataView.DefaultTemplate = m_OptionTemplate != null ? m_OptionTemplate.gameObject : m_ScrollDataView.DefaultTemplate;
                    var filteredOption = _Options == null ? null : new List<OptionData>(_Options);
                    ApplyFilterInList(filteredOption, m_SearchInputField != null ? m_SearchInputField.text : string.Empty, optionsLocaleMap);
                    m_ScrollDataView.Setup(filteredOption);
                }
            }
        }

        protected Dictionary<string, string> optionsLocaleMap
        {
            get
            {
                return m_SupportLocalizedSearch ? _OptionsLocaleMap : null;
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

        protected virtual void BaseInitialize<TOptionData>(IList<TOptionData> options, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText) where TOptionData : OptionData, new()
        {
            ClearList();

            _Options = options is IList<OptionData> || options == null ? (IList<OptionData>)options : options.ToArray<OptionData>();

            RecreateIndexedLocaleKeys(_Options, ref _OptionsLocaleMap);

            var optionsCount = _Options != null ? _Options.Count : 0;
            if (m_SearchInputField != null && m_SearchVisibilityMode == SearchInputFieldVisibilityMode.Auto)
            {
                ClearCachedLayoutValues();
                m_SearchInputField.gameObject.SetActive(optionsCount >= m_MinAmountOfElementsToShowSearch);
            }

            if (m_ScrollDataView != null)
            {
                m_ScrollDataView.DefaultTemplate = m_OptionTemplate != null ? m_OptionTemplate.gameObject : m_ScrollDataView.DefaultTemplate;
                m_ScrollDataView.OnReloadElement.AddListener(HandleOnReloadElement);
                m_ScrollDataView.Setup(options is IList || options == null ? (IList)options : options.ToArray());
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

            if (m_SearchInputField != null)
                m_SearchInputField.onValueChanged.AddListener(HandleOnSeachValueChanged);

#if UI_LOCALE_DEFINED
            Kyub.Localization.LocaleManager.OnLocalizeChanged += HandleOnLocalizeChanged;
#endif
        }

        protected virtual void UnregisterEvents()
        {
            if (m_ScrollDataView != null)
                m_ScrollDataView.OnReloadElement.RemoveListener(HandleOnReloadElement);

            if (m_SearchInputField != null)
                m_SearchInputField.onValueChanged.RemoveListener(HandleOnSeachValueChanged);

#if UI_LOCALE_DEFINED
            Kyub.Localization.LocaleManager.OnLocalizeChanged -= HandleOnLocalizeChanged;
#endif
        }

        public void AffirmativeButtonClicked()
        {
            var oldDismissAction = _onDismissiveButtonClicked;
            _onDismissiveButtonClicked = null;

            HandleOnAffirmativeButtonClicked();

            _onDismissiveButtonClicked = oldDismissAction;
        }

        public virtual void DismissiveButtonClicked()
        {
            Hide();
        }

        public virtual int ConvertFromUnsafeDataIndex(int unsafeDataIndex)
        {
            var data = unsafeDataIndex >= 0 ? m_ScrollDataView.Data : null;
            var convertedIndex = data != null ? unsafeDataIndex : -1;
            if (convertedIndex >= 0 && options.Count != data.Count)
            {
                var optionData = unsafeDataIndex >= 0 && data.Count > unsafeDataIndex ? data[unsafeDataIndex] as OptionData : null;
                convertedIndex = optionData != null ? options.IndexOf(optionData) : -1;
            }

            return convertedIndex;
        }
        
        public virtual bool IsUnsafeDataIndexSelected(int dataIndex)
        {
            return IsDataIndexSelected(ConvertFromUnsafeDataIndex(dataIndex));
        }

        public abstract bool IsDataIndexSelected(int dataIndex);

        #endregion

        #region Receivers

        protected virtual void HandleOnLocalizeChanged(bool forceReapply)
        {
            RecreateIndexedLocaleKeys(_Options, ref _OptionsLocaleMap);
        }

        protected virtual void HandleOnSeachValueChanged(string value)
        {
            if (m_ScrollDataView != null)
            {
                m_ScrollDataView.DefaultTemplate = m_OptionTemplate != null ? m_OptionTemplate.gameObject : m_ScrollDataView.DefaultTemplate;
                var filteredOption = options == null ? null : new List<OptionData>(options);
                ApplyFilterInList(filteredOption, value, optionsLocaleMap);
                m_ScrollDataView.Setup(filteredOption);
            }
        }

        protected abstract void HandleOnAffirmativeButtonClicked();

        protected virtual void HandleOnReloadElement(ScrollDataView.ReloadEventArgs args)
        {
            var clickableOption = args.LayoutElement != null ? args.LayoutElement.GetComponent<DialogClickableOption>() : null;
            if (clickableOption != null)
            {
                clickableOption.onItemClicked.RemoveListener(HandleUnsafeOnItemClicked);
                clickableOption.onItemClicked.AddListener(HandleUnsafeOnItemClicked);
            }
        }

        protected void HandleUnsafeOnItemClicked(int unsafeDataIndex)
        {
            HandleOnItemClicked(ConvertFromUnsafeDataIndex(unsafeDataIndex));
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
            if (_onDismissiveButtonClicked != null)
                _onDismissiveButtonClicked.InvokeIfNotNull();

            var canvasGroup = this.GetAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            base.OnActivityBeginHide();
        }

        #endregion

        #region Static Helper Functions

        protected static void RecreateIndexedLocaleKeys(IList<OptionData> options, ref Dictionary<string, string> indexedLocaleKeys)
        {
            if (indexedLocaleKeys == null)
                indexedLocaleKeys = new Dictionary<string, string>();
            else
                indexedLocaleKeys.Clear();

#if UI_LOCALE_DEFINED
            if (options != null)
            {
                foreach (var option in options)
                {
                    if (option != null)
                    {
                        string localizedText;
                        if (!string.IsNullOrEmpty(option.text) && Kyub.Localization.LocaleManager.TryGetLocalizedText(option.text, out localizedText, true))
                        {
                            indexedLocaleKeys[option.text] = localizedText;
                        }
                    }
                }
            }
#endif
        }

        protected static void ApplyFilterInList(IList<OptionData> list, string filterKeys, Dictionary<string, string> indexedLocale)
        {
            if (string.IsNullOrEmpty(filterKeys) || list == null || list.Count == 0)
                return;

            var filters = !string.IsNullOrEmpty(filterKeys) ? filterKeys.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null || !IsValidFilter(list[i].text, filters, indexedLocale))
                {
                    list.RemoveAt(i);
                    i--;
                }
            }
        }

        protected static bool IsValidFilter(string key, string[] filters, Dictionary<string, string> indexedLocale)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (filters == null || filters.Length == 0)
                return true;

            string localizedKey = null;
            if(indexedLocale != null)
                indexedLocale.TryGetValue(key, out localizedKey);
            foreach (var filter in filters)
            {
                if (filter != null && 
                    (key.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) < 0 && 
                    (string.IsNullOrEmpty(localizedKey) || localizedKey.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) < 0)))
                    return false;
            }
            return true;
        }

        #endregion

        #region ILayout Implementations

        public float minWidth { get { return -1; } }

        public float preferredWidth
        {
            get
            {
                return m_SearchInputField != null && 
                    m_SearchVisibilityMode == SearchInputFieldVisibilityMode.Auto && 
                    m_activity is MaterialDialogActivity ? _cachedPreferredWidth : -1;
            }
        }

        public float flexibleWidth { get { return -1; } }

        public float minHeight { get { return -1; } }

        public float preferredHeight
        {
            get
            {
                return m_SearchInputField != null && 
                    m_SearchVisibilityMode == SearchInputFieldVisibilityMode.Auto && 
                    m_activity is MaterialDialogActivity ? _cachedPreferredHeight : -1;
            }
        }

        public float flexibleHeight { get { return -1; } }

        public int layoutPriority { get { return 0; } }

        float _cachedPreferredWidth = -1;
        float _cachedPreferredHeight = -1;
        public void CalculateLayoutInputHorizontal()
        {
            float oldValue = _cachedPreferredWidth;
            var newValue = Kyub.UI.LayoutUtilityEx.GetPreferredWidth(this.rectTransform, -1);
            _cachedPreferredWidth = Mathf.Max(oldValue, newValue);
        }

        public void CalculateLayoutInputVertical()
        {
            float oldValue = _cachedPreferredHeight;
            var newValue = Kyub.UI.LayoutUtilityEx.GetPreferredHeight(this.rectTransform, -1);
            _cachedPreferredHeight = Mathf.Max(oldValue, newValue);
        }

        public void ClearCachedLayoutValues()
        {
            _cachedPreferredWidth = -1;
            _cachedPreferredHeight = -1;
            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
        }

        #endregion
    }
}