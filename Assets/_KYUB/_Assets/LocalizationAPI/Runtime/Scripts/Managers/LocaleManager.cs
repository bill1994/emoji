using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;
using System.Globalization;
using System.Collections.Specialized;

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
                var index = Mathf.Max(0, GetLocationDataIndexFromLanguage(value));
                if (index == _index)
                    return;

                _index = index;
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

        public virtual bool HasLanguage(string language)
        {
            return GetLocationDataIndexFromLanguage(language) >= 0;
        }

        public virtual LocalizationData GetLocationDataFromLanguage(string language)
        {
            if (m_localizationDatas != null)
            {
                var index = GetLocationDataIndexFromLanguage(language);
                if (index >= 0)
                    return m_localizationDatas[index];
            }
            return null;
        }

        public virtual int GetLocationDataIndexFromLanguage(string language)
        {
            int selectedIndex = -1;
            if (m_localizationDatas != null && !string.IsNullOrEmpty(language))
            {
                for (int i=0; i< m_localizationDatas.Count; i++)
                {
                    var langData = m_localizationDatas[i];
                    if (langData != null)
                    {
                        //Perfect Match
                        if (string.Equals(langData.Name, language, System.StringComparison.InvariantCultureIgnoreCase) || 
                            string.Equals(langData.FullName, language, System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            selectedIndex = i;
                            break;
                        }
                        //Find possible name (Non-Perfect Match)
                        else if (langData.Name.IndexOf(language, System.StringComparison.InvariantCultureIgnoreCase) != -1 || 
                            langData.FullName.IndexOf(language, System.StringComparison.InvariantCultureIgnoreCase) != -1)
                        {
                            selectedIndex = i;
                        }
                    }
                }
            }
            return selectedIndex;
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
            string sysLanguageString = CultureInfo.CurrentUICulture.Name;
            string unitySysLanguageString = Application.systemLanguage.ToString();
            //Try Match Name
            foreach (var datas in LocalizationDatas)
            {
                if (string.Equals(sysLanguageString, datas.Name, System.StringComparison.InvariantCultureIgnoreCase) || string.Equals(sysLanguageString, datas.FullName, System.StringComparison.InvariantCultureIgnoreCase) ||
                   (string.Equals(unitySysLanguageString, datas.Name, System.StringComparison.InvariantCultureIgnoreCase) || string.Equals(unitySysLanguageString, datas.FullName, System.StringComparison.InvariantCultureIgnoreCase)))
                {
                    StartingLanguage = datas.Name;
                    break;
                }
            }
            CurrentLanguage = StartingLanguage;
        }

        protected virtual Dictionary<string, string> GetCurrentDictionaryData()
        {
            var loc = GetOrLoadCurrentData();
            return loc != null ? loc.Dictionary : null;
        }

        protected virtual LocalizationData GetOrLoadCurrentData()
        {
            var loc = LocalizationDatas.Count > _index ? LocalizationDatas[_index] : null;
            if (loc != null)
            {
                if (!loc.IsLoaded && loc.CanLoadFromResources())
                    loc.LoadFromCachedResourcesPath();
            }

            return loc;
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

        public void TryApply(bool force = false)
        {
            if (force || _isDirty)
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
            //Unload all other localization datas
            for (int i = 0; i < LocalizationDatas.Count; i++)
            {
                var data = LocalizationDatas[i];
                if (i != CurrentIndex && data != null &&
                    data.MemoryConfigOption != LocalizationDataMemoryConfigEnum.Unloadable &&
                    data.IsLoaded)
                {
                    data.Unload();
                }
            }
            var forceReapply = _forceReapplyToAll;
            _forceReapplyToAll = false;

            //Update CurrentCulture to reflect to same name as CurrentLanguage
            TryUpdateCurrentCulture();
            if (OnLocalizeChanged != null)
                OnLocalizeChanged(forceReapply);

        }

        protected internal virtual bool TryGetLocalizedText_Internal(string key, out string val)
        {
            key = key != null ? Kyub.RegexUtils.BulkReplace(key, s_uselessCharsDict).Trim() : "";
            var dictionary = GetCurrentDictionaryData();

            var sucess = TryGetLocalizedText_Internal(key, dictionary, out val);

#if UNITY_EDITOR
            if (m_addInvalidRequestedKeysInEditor && !sucess && !string.IsNullOrEmpty(key) && !key.Equals("\u200B"))
            {
                Debug.Log("[LocaleManager] Insert missing key <b>'" + key + "'</b> to <b>'" + CurrentLanguage + "'</b> Data");
                var loc = LocalizationDatas.Count > _index ? LocalizationDatas[_index] : null;
                if (loc != null)
                {
                    if (loc._keyValueArray == null)
                        loc._keyValueArray = new List<LocalizationPair>();
                    loc._keyValueArray.Add(new LocalizationPair() { Key = key, Value = key });
                }

                dictionary[key] = key;
            }
#endif
            if (val == null)
                val = key;
            return sucess;
        }

        protected internal virtual bool TryGetLocalizedText_Internal(string key, Dictionary<string, string> dict, out string val)
        {
            key = key != null ? Kyub.RegexUtils.BulkReplace(key, s_uselessCharsDict).Trim() : "";

            bool sucess = false;
            if (!Application.isPlaying)
            {
                sucess = false;
                val = key;
            }
            else
            {
                val = key;
                if (dict != null && dict.TryGetValue(key, out val))
                    sucess = true;
            }

            if (val == null)
                val = key;
            return sucess;
        }

        protected internal bool TryClearLocaleTags_Internal(string text, out string outText)
        {
            bool sucess = false;

            outText = Kyub.RegexUtils.BulkReplace(text, s_localeClearTagsDict);
            if (text != outText)
                sucess = true;

            return sucess;
        }

        protected internal virtual bool TryLocalizeByLocaleTag_Internal(string text, out string localizedValue)
        {
            if (text.StartsWith(LOCALE_SKIP_TAG, System.StringComparison.InvariantCultureIgnoreCase))
            {
                TryClearLocaleTags_Internal(text, out localizedValue);
                return true;
            }

            var sucess = false;
            Dictionary<string, string> paramsDict = null;
            localizedValue = s_localeRegexCompiled.Replace(text,
                (m) =>
                {
                    //Match <locale>..</locale>
                    if (m.Groups[1].Success)
                    {
                        sucess = true;
                        var groupLocale = m.Groups[2].Value;
                        TryLocalizeByLocaleTag_Internal(groupLocale, out groupLocale);

                        //return localized locale
                        TryGetLocalizedText_Internal(groupLocale, out groupLocale);
                        return groupLocale;
                    }
                    //Match <localeparam=number>..</localeparam>
                    else if (m.Groups[3].Success)
                    {
                        sucess = true;
                        var groupParamIndex = m.Groups[4].Value; //localeparam Index
                        var groupParam = m.Groups[5].Value; //localeparam Content
                        TryLocalizeByLocaleTag_Internal(groupParam, out groupParam);

                        if (paramsDict == null)
                            paramsDict = new Dictionary<string, string>();

                        paramsDict[@"\{" + groupParamIndex + @"\}"] = groupParam;
                        return "{" + groupParamIndex + "}"; //format to replace after
                    }

                    return m.Value;
                });


            //Localize text and than replace the parameters found
            TryGetLocalizedText_Internal(localizedValue, out localizedValue);
            if (paramsDict != null)
                localizedValue = Kyub.RegexUtils.BulkReplace(localizedValue, paramsDict);

            return sucess;
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

        public static bool TryGetLocalizedText(string key, out string localizedText, bool supportLocaleTag = false)
        {
            if (InstanceExists())
            {
                if (supportLocaleTag && Instance.TryLocalizeByLocaleTag_Internal(key, out localizedText))
                    return true;

                return Instance.TryGetLocalizedText_Internal(key, out localizedText);
            }
            else
                localizedText = key;

            return false;
        }

        public static string GetLocalizedText(string key, bool supportLocaleTag = false)
        {
            string localizedText;
            TryGetLocalizedText(key, out localizedText, supportLocaleTag);

            return localizedText;
        }

        #endregion

        #region Editor Functions

#if UNITY_EDITOR

        [ContextMenu("Export Locale/As JSON", true)]
        [ContextMenu("Export Locale/As CSV", true)]
        protected virtual bool ExportCurrentLocalizationData_Validator()
        {
            return Application.isPlaying;
        }

        [ContextMenu("Export Locale/As CSV")]
        protected virtual void ExportCurrentLocalizationData()
        {
            var loc = GetOrLoadCurrentData();

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine(string.Format("\"//Keys\";\"Text({0})\"", CurrentLanguage));
            if (loc != null && loc._keyValueArray != null)
            {
                foreach (var pair in loc._keyValueArray)
                {
                    //Escape to double quotemark format (used in csv)
                    var key = pair.Key == null ? "" : pair.Key.Trim().Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "");
                    var value = pair.Value == null ? "" : pair.Value.Trim().Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "");
                    if (!string.IsNullOrEmpty(key) && !key.Equals("\u200B"))
                        builder.AppendLine(string.Format("\"{0}\";\"{1}\"", key, value));
                }
            }
            System.IO.File.WriteAllText(Application.dataPath + "/" + CurrentLanguage + "_Exported.csv", builder.ToString());

            UnityEditor.AssetDatabase.Refresh();
        }

        [ContextMenu("Export Locale/As JSON")]
        protected virtual void ExportCurrentLocalizationDataJson()
        {
            var loc = GetOrLoadCurrentData();

            OrderedDictionary dict = new OrderedDictionary();
            if (loc != null && loc._keyValueArray != null)
            {
                
                foreach (var pair in loc._keyValueArray)
                {
                    //Escape to double quotemark format (used in csv)
                    var key = pair.Key == null ? "" : pair.Key.Trim().Replace("\n", "\\n").Replace("\r", "");
                    var value = pair.Value == null ? "" : pair.Value.Trim().Replace("\n", "\\n").Replace("\r", "");
                    if (!string.IsNullOrEmpty(key) && !key.Equals("\u200B"))
                        dict[key] = value;
                }
            }
            System.IO.File.WriteAllText(Application.dataPath + "/" + CurrentLanguage + "_Exported.json", SerializationUtils.ToJson(dict, true));

            UnityEditor.AssetDatabase.Refresh();
        }

#endif

        #endregion
    }

    #region Helper Classes

    public enum LocalizationDataMemoryConfigEnum { Unloadable, Permanent }

    public enum LocalizationDataFileTypeEnum 
    { 
        Csv, 
        Json, 
        [System.Obsolete("Use Json as Dictionary instead this mode")]
        JsonClass 
    }

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

        public LocalizationData(string name, string fullName)
        {
            m_name = name;
            m_fullName = fullName;
        }

        public LocalizationData(string name, IDictionary<string, string> dictionary)
        {
            m_name = name;
            _cachedDict = new Dictionary<string, string>(dictionary);
        }

        #endregion

        #region Helper Functions

        public virtual void LoadFromCachedResourcesPath()
        {
            LoadFromResourcesPath(m_resourcesFilePath, m_fileType);
        }

        protected virtual void LoadFromResourcesPath(string resourcesPath, LocalizationDataFileTypeEnum fileType)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcesPath);
            LoadFromAsset(asset, fileType);
            m_resourcesFilePath = resourcesPath;
            m_fileType = fileType;
        }

        protected virtual void LoadFromAsset(TextAsset asset, LocalizationDataFileTypeEnum fileType)
        {
            m_resourcesFilePath = "";
            if (asset != null)
            {
                if (fileType == LocalizationDataFileTypeEnum.Csv)
                {
                    LocalizationCsvFileReader reader = new LocalizationCsvFileReader(asset);
                    _cachedDict = reader.ReadDictionary();
                }
                else if (fileType == LocalizationDataFileTypeEnum.Json)
                {
                    this.LoadFromJsonDictionary(asset.text);
                    
                    return;
                }
                else
                {
                    this.LoadFromJsonClass(asset.text);
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

        public virtual void LoadFromDictionary(Dictionary<string, string> dict)
        {
            if (dict != null)
            {
                _cachedDict = dict;
                _isLoaded = true;
            }
            else
            {
                _cachedDict = null;
                _isLoaded = false;
            }
            FinishLoad();
        }

        public virtual void LoadFromJsonDictionary(string json)
        {
            var rootData = SerializationUtils.FromJson<Kyub.Serialization.Data>(json);

            Dictionary<string, string> dict = new Dictionary<string, string>();

            List<Kyub.Serialization.Data> datasToProcess = new List<Serialization.Data>();
            if (rootData != null)
                datasToProcess.Add(rootData);

            for (int i = 0; i < datasToProcess.Count; i++)
            {
                var child = datasToProcess[i];
                if (child == null || !child.IsDictionary)
                    continue;

                var childDict = child.AsDictionary;
                foreach (var pair in childDict)
                {
                    if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                        continue;

                    if (pair.Value.IsString)
                        dict[pair.Key] = pair.Value.AsString;
                    else if (pair.Value.IsDictionary)
                        datasToProcess.Add(pair.Value);
                }
            }

            LoadFromDictionary(dict);
        }

        public virtual void LoadFromJsonClass(string json)
        {
            if (!string.IsNullOrEmpty(json))
                SerializationUtils.FromJsonOverwrite(json, this);

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
                foreach (var pair in _keyValueArray)
                {
                    if (_cachedDict.ContainsKey(pair.Key.ToLower()))
                        _cachedDict[pair.Key.ToLower()] = pair.Value;
                    else
                        _cachedDict.Add(pair.Key.ToLower(), pair.Value);
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
            else if (Application.isPlaying)
            {
                var valueArray = new List<LocalizationPair>();
                if (_cachedDict != null)
                {
                    foreach (string key in _cachedDict.Keys)
                    {
                        valueArray.Add(new LocalizationPair() { Key = key, Value = _cachedDict[key] });
                    }
                }
                _keyValueArray = valueArray;
            }
        }

        #endregion

        #region Static Functions

        public static LocalizationData CreateFromJsonClass(string json)
        {
            LocalizationData data = new LocalizationData();
            data.LoadFromJsonClass(json);
            return data;
        }

        public static LocalizationData CreateFromJsonDictionary(string json)
        {
            LocalizationData data = new LocalizationData();
            data.LoadFromJsonDictionary(json);
            return data;
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
