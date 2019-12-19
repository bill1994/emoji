using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;
using System.Globalization;

namespace Kyub.Localization
{
    public class LocaleManager : Singleton<LocaleManager>
    {
        #region Consts

        
        public const string LOCALE_SKIP_TAG = @"<skiplocale>";
        public static Dictionary<string, string> s_localeClearTagsDict = new Dictionary<string, string>() {
                    { @"<locale>", "" },
                    { @"<\/locale>", "" },
                    { @"<localeparam=([0-9])+>", "" },
                    { @"<\/localeparam>", "" },
                    { LOCALE_SKIP_TAG, "" }};

        protected internal static Dictionary<string, string> s_uselessCharsDict = new Dictionary<string, string>() {
            { "\n", "\\n" },
            { "\r", "" }
        };

        protected internal const string LOCALE_TAG_PATTERN = @"(<locale>(.*?)<\/locale>)|(<localeparam=([0-9])+>(.*?)<\/localeparam>)";
        protected internal static System.Text.RegularExpressions.Regex s_localeRegexCompiled = new System.Text.RegularExpressions.Regex(LOCALE_TAG_PATTERN);

        protected internal const string LANGUANGE_KEY = "LANGUANGE_KEY";
        #endregion

        #region Static Events

        public static event System.Action<bool> OnLocalizeChanged;

        #endregion

        #region Variables

        [Header("General Config Fields")]
        [SerializeField]
        string m_startingLanguage = "en-US";
        [SerializeField]
        bool m_alwaysPickFromSystemLanguage = true;
        [SerializeField]
        bool m_saveLastLanguageUsed = false;
        [Space]
        [SerializeField]
        List<LocalizationData> m_localizationDatas = new List<LocalizationData>() { new LocalizationData("en-US", "English") };

#if UNITY_EDITOR
        [Header("Debug Fields")]
        [SerializeField, Tooltip("Useful to find missing localization words in CurrentData\n Missing keys will be added to CurrentData (Export the CurrentData CSV in Runtime using method ExportCurrentLocalizationData)")]
        bool m_addInvalidRequestedKeysInEditor = true;
#endif

        int _index = 0;

        #endregion

        #region Properties

        public List<LocalizationData> LocalizationDatas
        {
            get
            {
                if (m_localizationDatas == null)
                    m_localizationDatas = new List<LocalizationData>();
                return m_localizationDatas;
            }
            set
            {
                if (m_localizationDatas == value)
                    return;
                m_localizationDatas = value;
            }
        }

        public string StartingLanguage
        {
            get
            {
                return m_startingLanguage;
            }
            set
            {
                if (m_startingLanguage == value)
                    return;
                m_startingLanguage = value;
            }
        }

        public string CurrentLanguage
        {
            get
            {
                return LocalizationDatas.Count > CurrentIndex ? LocalizationDatas[CurrentIndex].Name : "";
            }
            set
            {
                var v_index = 0;
                if (value != null)
                {
                    var v_value = value.ToUpper();
                    for (int i = 0; i < LocalizationDatas.Count; i++)
                    {
                        var v_data = LocalizationDatas[i];
                        if (v_data != null && (string.Equals(v_data.Name, v_value, System.StringComparison.InvariantCultureIgnoreCase) || string.Equals(v_data.FullName, v_value, System.StringComparison.InvariantCultureIgnoreCase)))
                        {
                            v_index = i;
                            break;
                        }
                    }
                }
                if (v_index == _index)
                    return;
                _index = v_index;
                SetDirty();
            }
        }

        protected int CurrentIndex
        {
            get
            {
                if (_index < 0 || _index > LocalizationDatas.Count)
                    _index = 0;
                return _index;
            }
            set
            {
                if (_index == value)
                    return;
                _index = value;
                SetDirty();
            }
        }

        public bool AlwaysPickFromSystemLanguage
        {
            get
            {
                return m_alwaysPickFromSystemLanguage;
            }
            set
            {
                if (m_alwaysPickFromSystemLanguage == value)
                    return;
                m_alwaysPickFromSystemLanguage = value;
            }
        }

        public bool SaveLastLanguageUsed
        {
            get
            {
                return m_saveLastLanguageUsed;
            }
            set
            {
                if (m_saveLastLanguageUsed == value)
                    return;
                m_saveLastLanguageUsed = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            if (this == s_instance)
            {
                Init();
            }
        }

        protected virtual void OnEnable()
        {
            if (this == s_instance && _started)
            {
                TryApply();
            }
        }

        protected bool _started = false;
        protected virtual void Start()
        {
            if (this == s_instance)
            {
                _started = true;
                TryApply(true);
            }
        }

        protected virtual void Update()
        {
            if (this == s_instance)
            {
                TryApply();
            }
        }

        #endregion

        #region Helper Functions

        public virtual bool HasLanguage(string p_language)
        {
            return GetLocationDataFromLanguage(p_language) != null;
        }

        public virtual LocalizationData GetLocationDataFromLanguage(string p_language)
        {
            if (m_localizationDatas != null)
            {
                foreach (var v_langData in m_localizationDatas)
                {
                    if (v_langData != null && 
                        (string.Equals(v_langData.Name, p_language) || string.Equals(v_langData.FullName, p_language)))
                    {
                        return v_langData;
                    }
                }
            }
            return null;
        }

        protected virtual void Init()
        {
            if (AlwaysPickFromSystemLanguage)
                ApplySystemLanguageAsInitial();
            else
            {
                if (SaveLastLanguageUsed)
                    CurrentLanguage = PlayerPrefs.GetString(LANGUANGE_KEY, m_startingLanguage);
                else
                    CurrentLanguage = StartingLanguage;
            }
            if (AlwaysPickFromSystemLanguage && PlayerPrefs.HasKey(LANGUANGE_KEY))
                PlayerPrefs.DeleteKey(LANGUANGE_KEY);
            GetOrLoadCurrentData();
        }

        protected virtual void ApplySystemLanguageAsInitial()
        {
            if (PlayerPrefs.HasKey(LANGUANGE_KEY))
                PlayerPrefs.DeleteKey(LANGUANGE_KEY);
            string v_sysLanguageString = CultureInfo.CurrentUICulture.Name;
            string v_unitySysLanguageString = Application.systemLanguage.ToString();
            //Try Match Name
            foreach (var v_datas in LocalizationDatas)
            {
                if (string.Equals(v_sysLanguageString, v_datas.Name, System.StringComparison.InvariantCultureIgnoreCase) || string.Equals(v_sysLanguageString, v_datas.FullName, System.StringComparison.InvariantCultureIgnoreCase) ||
                   (string.Equals(v_unitySysLanguageString, v_datas.Name, System.StringComparison.InvariantCultureIgnoreCase) || string.Equals(v_unitySysLanguageString, v_datas.FullName, System.StringComparison.InvariantCultureIgnoreCase)))
                {
                    StartingLanguage = v_datas.Name;
                    break;
                }
            }
            CurrentLanguage = StartingLanguage;
        }

        protected virtual Dictionary<string, string> GetCurrentDictionaryData()
        {
            var v_loc = GetOrLoadCurrentData();
            return v_loc != null? v_loc.Dictionary : null;
        }

        protected virtual LocalizationData GetOrLoadCurrentData()
        {
            var v_loc = LocalizationDatas.Count > _index ? LocalizationDatas[_index] : null;
            if (v_loc != null)
            {
                if (!v_loc.IsLoaded && v_loc.CanLoadFromResources())
                    v_loc.LoadFromCachedResourcesPath();
            }

            return v_loc;
        }

        protected bool _isDirty = false;
        public virtual void SetDirty()
        {
            _isDirty = true;
        }

        protected bool _forceReapplyToAll = false;
        public virtual void SetDirtyAndReapplyToAll()
        {
            _isDirty = true;
            _forceReapplyToAll = true;
        }

        public void TryApply(bool p_force = false)
        {
            if (p_force || _isDirty)
            {
                _isDirty = false;
                Apply();
            }
        }

        protected virtual void Apply()
        {
            if (SaveLastLanguageUsed)
            {
                PlayerPrefs.SetString(LANGUANGE_KEY, CurrentLanguage);
                PlayerPrefs.Save();
            }
            //Unload all other loaclization datas
            for (int i = 0; i < LocalizationDatas.Count; i++)
            {
                var v_data = LocalizationDatas[i];
                if (i != CurrentIndex && v_data != null &&
                    v_data.MemoryConfigOption != LocalizationDataMemoryConfigEnum.Unloadable &&
                    v_data.IsLoaded)
                {
                    v_data.Unload();
                }
            }
            var v_forceReapply = _forceReapplyToAll;
            _forceReapplyToAll = false;

            //Update CurrentCulture to reflect to same name as CurrentLanguage
            TryUpdateCurrentCulture();
            if (OnLocalizeChanged != null)
                OnLocalizeChanged(v_forceReapply);
            
        }

        protected internal virtual bool TryGetLocalizedText_Internal(string p_key, out string p_val)
        {
            p_key = p_key != null ? Kyub.RegexUtils.BulkReplace(p_key, s_uselessCharsDict).Trim() : "";
            var v_dictionary = GetCurrentDictionaryData();

            bool v_sucess = false;
            if (!Application.isPlaying)
            {
                v_sucess = false;
                p_val = p_key;
            }
            else
            {
                p_val = p_key;
                if (v_dictionary != null && v_dictionary.TryGetValue(p_key, out p_val))
                    v_sucess = true;
            }

#if UNITY_EDITOR
            if (m_addInvalidRequestedKeysInEditor && !v_sucess && !string.IsNullOrEmpty(p_key) && !p_key.Equals("\u200B"))
            {
                Debug.Log("[LocaleManager] Insert missing key <b>'" + p_key + "'</b> to <b>'" + CurrentLanguage + "'</b> Data");
                var v_loc = LocalizationDatas.Count > _index ? LocalizationDatas[_index] : null;
                if (v_loc != null)
                {
                    if (v_loc._keyValueArray == null)
                        v_loc._keyValueArray = new List<LocalizationPair>();
                    v_loc._keyValueArray.Add(new LocalizationPair() { Key = p_key, Value = p_key });
                }

                v_dictionary[p_key] = p_key;
            }
#endif
            if (p_val == null)
                p_val = p_key;
            return v_sucess;
        }

        protected internal bool TryClearLocaleTags_Internal(string p_text, out string p_outText)
        {
            bool v_sucess = false;

            p_outText = Kyub.RegexUtils.BulkReplace(p_text, s_localeClearTagsDict);
            if (p_text != p_outText)
                v_sucess = true;

            return v_sucess;
        }

        protected internal virtual bool TryLocalizeByLocaleTag_Internal(string p_text, out string p_localizedValue)
        {
            if (p_text.StartsWith(LOCALE_SKIP_TAG, System.StringComparison.InvariantCultureIgnoreCase))
            {
                TryClearLocaleTags_Internal(p_text, out p_localizedValue);
                return true;
            }

            var v_sucess = false;
            Dictionary<string, string> v_params = null;
            p_localizedValue = s_localeRegexCompiled.Replace(p_text,
                (m) =>
                {
                    //Match <locale>..</locale>
                    if (m.Groups[1].Success)
                    {
                        v_sucess = true;
                        var v_groupLocale = m.Groups[2].Value;
                        TryLocalizeByLocaleTag_Internal(v_groupLocale, out v_groupLocale);

                        //return localized locale
                        TryGetLocalizedText_Internal(v_groupLocale, out v_groupLocale);
                        return v_groupLocale;
                    }
                    //Match <localeparam=number>..</localeparam>
                    else if (m.Groups[3].Success)
                    {
                        v_sucess = true;
                        var v_groupParamIndex = m.Groups[4].Value; //localeparam Index
                        var v_groupParam = m.Groups[5].Value; //localeparam Content
                        TryLocalizeByLocaleTag_Internal(v_groupParam, out v_groupParam);

                        if (v_params == null)
                            v_params = new Dictionary<string, string>();

                        v_params[@"\{" + v_groupParamIndex + @"\}"] = v_groupParam;
                        return "{" + v_groupParamIndex + "}"; //format to replace after
                    }

                    return m.Value;
                });


            //Localize text and than replace the parameters found
            if (v_params != null)
            {
                TryGetLocalizedText_Internal(p_localizedValue, out p_localizedValue);
                p_localizedValue = Kyub.RegexUtils.BulkReplace(p_localizedValue, v_params);
            }

            return v_sucess;
        }

        protected virtual bool TryUpdateCurrentCulture()
        {
            CultureInfo[] availableCultures =
                CultureInfo.GetCultures(CultureTypes.AllCultures);

            foreach (CultureInfo culture in availableCultures)
            {
                if (string.Equals(culture.Name, CurrentLanguage, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    System.Globalization.CultureInfo.CurrentCulture = culture;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Static

        public static bool TryGetLocalizedText(string p_key, out string p_localizedText, bool p_supportLocaleTag = false)
        {
            if (InstanceExists())
            {
                if (p_supportLocaleTag && Instance.TryLocalizeByLocaleTag_Internal(p_key, out p_localizedText))
                    return true;

                return Instance.TryGetLocalizedText_Internal(p_key, out p_localizedText);
            }
            else
                p_localizedText = p_key;

            return false;
        }

        public static string GetLocalizedText(string p_key, bool p_supportLocaleTag = false)
        {
            string localizedText;
            TryGetLocalizedText(p_key, out localizedText, p_supportLocaleTag);

            return localizedText;
        }

        #endregion

        #region Editor Functions

#if UNITY_EDITOR

        [ContextMenu("ExportCurrentLocalizationData", true)]
        protected virtual bool ExportCurrentLocalizationData_Validator()
        {
            return Application.isPlaying;
        }

        [ContextMenu("ExportCurrentLocalizationData")]
        protected virtual void ExportCurrentLocalizationData()
        {
            var v_loc = GetOrLoadCurrentData();

            System.Text.StringBuilder v_builder = new System.Text.StringBuilder();
            v_builder.AppendLine(string.Format("\"//Keys\";\"Text({0})\"" , CurrentLanguage));
            if (v_loc != null && v_loc._keyValueArray != null)
            {
                foreach (var v_pair in v_loc._keyValueArray)
                {
                    //Escape to double quotemark format (used in csv)
                    var v_key = v_pair.Key == null ? "" : v_pair.Key.Trim().Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "");
                    var v_value = v_pair.Value == null ? "" : v_pair.Value.Trim().Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "");
                    if (!string.IsNullOrEmpty(v_key) && !v_key.Equals("\u200B"))
                        v_builder.AppendLine(string.Format("\"{0}\";\"{1}\"", v_key, v_value));
                }
            }
            System.IO.File.WriteAllText(Application.dataPath + "/" + CurrentLanguage + "_Exported.csv", v_builder.ToString());

            UnityEditor.AssetDatabase.Refresh();
        }

#endif

        #endregion
    }

    #region Helper Classes

    public enum LocalizationDataMemoryConfigEnum { Unloadable, Permanent }

    public enum LocalizationDataFileTypeEnum { Csv, Json }

    [System.Serializable]
    public class LocalizationData
    {
        #region Private Variable

        [SerializeField]
        string m_name = "";
        [SerializeField]
        string m_fullName = "";
        [Space]
        [SerializeField, ResourcesAssetPath(typeof(TextAsset))]
        string m_resourcesFilePath = "";
        [SerializeField]
        LocalizationDataFileTypeEnum m_fileType = LocalizationDataFileTypeEnum.Csv;
        [Space]
        [SerializeField]
        LocalizationDataMemoryConfigEnum m_memoryConfigOption = LocalizationDataMemoryConfigEnum.Unloadable;

        bool _isLoaded = false;
        internal List<LocalizationPair> _keyValueArray = null;
        Dictionary<string, string> _cachedDict = new Dictionary<string, string>();

        #endregion

        #region Public Properties

        public string Name
        {
            get
            {
                if (m_name == null)
                    m_name = "";
                return m_name;
            }
            set
            {
                if (m_name == value)
                    return;
                m_name = value;
            }
        }

        public string FullName
        {
            get
            {
                if (m_fullName == null)
                    m_fullName = "";
                return m_fullName;
            }
            set
            {
                if (m_fullName == value)
                    return;
                m_fullName = value;
            }
        }

        public Dictionary<string, string> Dictionary
        {
            get
            {
                TryConvertKeyValueListToDict();
                if (_cachedDict == null)
                    _cachedDict = new Dictionary<string, string>();
                return _cachedDict;
            }
            set
            {
                if (_cachedDict == value)
                    return;
                _cachedDict = value;
                FinishLoad();
            }
        }

        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }
        }

        public LocalizationDataMemoryConfigEnum MemoryConfigOption
        {
            get
            {
                return m_memoryConfigOption;
            }
            set
            {
                if (m_memoryConfigOption == value)
                    return;
                m_memoryConfigOption = value;
            }
        }

        #endregion

        #region Constructors

        public LocalizationData()
        {
        }

        public LocalizationData(string p_name, string p_fullName)
        {
            m_name = p_name;
            m_fullName = p_fullName;
        }

        public LocalizationData(string p_name, IDictionary<string, string> p_dictionary)
        {
            m_name = p_name;
            _cachedDict = new Dictionary<string, string>(p_dictionary);
        }

        #endregion

        #region Helper Functions

        public virtual void LoadFromCachedResourcesPath()
        {
            LoadFromResourcesPath(m_resourcesFilePath, m_fileType);
        }

        protected virtual void LoadFromResourcesPath(string p_resourcesPath, LocalizationDataFileTypeEnum p_fileType)
        {
            TextAsset v_asset = Resources.Load<TextAsset>(p_resourcesPath);
            LoadFromAsset(v_asset, p_fileType);
            m_resourcesFilePath = p_resourcesPath;
            m_fileType = p_fileType;
        }

        protected virtual void LoadFromAsset(TextAsset p_asset, LocalizationDataFileTypeEnum p_fileType)
        {
            m_resourcesFilePath = "";
            if (p_asset != null)
            {
                if (p_fileType == LocalizationDataFileTypeEnum.Csv)
                {
                    LocalizationCsvFileReader v_reader = new LocalizationCsvFileReader(p_asset);
                    _cachedDict = v_reader.ReadDictionary();
                }
                else
                {
                    this.LoadFromJson(p_asset.text);
                    return;
                }
                _isLoaded = true;
            }
            else
            {
                _cachedDict = null;
                _isLoaded = false;
                //Failed to Load asset from path, so we remove the LocalizationLoaderType from this guy
                if (CanLoadFromResources())
                    m_resourcesFilePath = "";
            }
            FinishLoad();
        }

        public virtual void LoadFromDictionary(Dictionary<string, string> p_dict)
        {
            if (p_dict != null)
            {
                _cachedDict = p_dict;
                _isLoaded = true;
            }
            else
            {
                _cachedDict = null;
                _isLoaded = false;
            }
            FinishLoad();
        }

        public virtual void LoadFromJson(string p_json)
        {
            if (!string.IsNullOrEmpty(p_json))
                SerializationUtils.FromJsonOverwrite(p_json, this);

            if (_cachedDict != null || _keyValueArray != null)
            {
                _cachedDict = Dictionary;
                _isLoaded = true;
            }
            else
                _isLoaded = false;
            FinishLoad();
        }

        public virtual void Unload()
        {
            _cachedDict = null;
            _keyValueArray = null;
            _isLoaded = false;
        }

        protected virtual void TryConvertKeyValueListToDict()
        {
            if (_keyValueArray != null && !_isLoaded)
            {
                _isLoaded = true;
                _cachedDict = new Dictionary<string, string>();
                foreach (var v_pair in _keyValueArray)
                {
                    if (_cachedDict.ContainsKey(v_pair.Key.ToLower()))
                        _cachedDict[v_pair.Key.ToLower()] = v_pair.Value;
                    else
                        _cachedDict.Add(v_pair.Key.ToLower(), v_pair.Value);
                }
                if (!Application.isEditor || !Application.isPlaying)
                    _keyValueArray = null;
            }
        }

        public bool CanLoadFromResources()
        {
            return !string.IsNullOrEmpty(m_resourcesFilePath);
        }

        protected virtual void FinishLoad()
        {
            if (!Application.isEditor || !Application.isPlaying)
                _keyValueArray = null;
            else if(Application.isPlaying)
            {
                var v_valueArray = new List<LocalizationPair>();
                if (_cachedDict != null)
                {
                    foreach (string v_key in _cachedDict.Keys)
                    {
                        v_valueArray.Add(new LocalizationPair() { Key = v_key, Value = _cachedDict[v_key] });
                    }
                }
                _keyValueArray = v_valueArray;
            }
        }

        #endregion

        #region Static Functions

        public static LocalizationData CreateFromJson(string p_json)
        {
            LocalizationData v_data = new LocalizationData();
            v_data.LoadFromJson(p_json);
            return v_data;
        }

        #endregion
    }

    [System.Serializable]
    public class LocalizationPair
    {
        #region Private Variables

        [SerializeField, TextArea(1, 3), UnityEngine.Serialization.FormerlySerializedAs("Key")]
        string m_key = "";
        [SerializeField, TextArea(1, 3), UnityEngine.Serialization.FormerlySerializedAs("Value")]
        string m_value = "";

        #endregion

        #region Public Properties

        public string Key
        {
            get
            {
                return m_key;
            }
            set
            {
                if (m_key == value)
                    return;
                m_key = value;
            }
        }

        public string Value
        {
            get
            {
                return m_value;
            }
            set
            {
                if (m_value == value)
                    return;
                m_value = value;
            }
        }

        #endregion
    }

    #endregion
}
