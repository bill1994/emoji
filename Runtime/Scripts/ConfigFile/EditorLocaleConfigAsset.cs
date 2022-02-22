using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;
using System.Globalization;
using System.Collections.Specialized;
using Kyub.Localization;

namespace KyubEditor.Localization
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadAttribute]
#endif
    [CreateAssetMenu(fileName = "EditorLocaleConfigAsset.asset", menuName = "Locale/EditorLocaleConfigAsset")]
    public class EditorLocaleConfigAsset : ScriptableObject
    {
#if UNITY_EDITOR
        internal class AssetPostProcessorListener : UnityEditor.AssetPostprocessor
        {
            public static event System.Action OnAssetsReload;

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (OnAssetsReload != null)
                    OnAssetsReload.Invoke();
            }
        }
#endif

        #region Instance

        static EditorLocaleConfigAsset()
        {
#if UNITY_EDITOR
            AssetPostProcessorListener.OnAssetsReload -= OnAssetsReload;
            AssetPostProcessorListener.OnAssetsReload += OnAssetsReload;
#endif
        }

        private static void OnAssetsReload()
        {
            //Force Refresh
            s_instance = null;
            if (Instance != null) { }
        }

        const string MAIN_PATH = "EditorLocaleConfigAsset";

        static EditorLocaleConfigAsset s_instance = null;
        public static EditorLocaleConfigAsset Instance
        {
            get
            {
                if (Application.isPlaying)
                {
                    s_instance = null;
                }
                else if (s_instance == null)
                {
                    s_instance = Resources.Load<EditorLocaleConfigAsset>(MAIN_PATH);
                    if (s_instance == null)
                        s_instance = ScriptableObject.CreateInstance<EditorLocaleConfigAsset>();

                    s_instance.Init();
                }
                return s_instance;
            }
        }

        #endregion

        #region Variables

        [SerializeField]
        bool m_enabled = true;

        [Header("General Config Fields")]
        [SerializeField]
        string m_startingLanguage = "en-US";
        [Space]
        [SerializeField]
        bool m_enableVisualDebug = true;
        [SerializeField]
        Color m_debugColor = new Color(1, 0, 0, 0.25f);
        [Space]
        [SerializeField]
        List<LocalizationData> m_localizationDatas = new List<LocalizationData>() { new LocalizationData("en-US", "English") };

        int _index = 0;

        #endregion

        #region Properties

        public bool EnableVisualDebug
        {
            get
            {
                return m_enableVisualDebug;
            }
            set
            {
                if (m_enableVisualDebug == value)
                    return;
                m_enableVisualDebug = value;
                SetLocaleDirtyAndReapplyToAll();
            }
        }
        public Color DebugColor
        {
            get
            {
                return m_debugColor;
            }
            set
            {
                if (m_debugColor == value)
                    return;
                m_debugColor = value;
                SetLocaleDirtyAndReapplyToAll();
            }
        }

        public bool Enabled
        {
            get
            {
                return m_enabled;
            }
            set
            {
                if (m_enabled == value)
                    return;
                m_enabled = value;
                SetLocaleDirtyAndReapplyToAll();
            }
        }

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
                SetLocaleDirtyAndReapplyToAll();
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
                SetLocaleDirty();
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
                SetLocaleDirty();
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
                SetLocaleDirty();
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
                LocaleManager.CallOnLocalizedChanged(true);
            }
        }

        #endregion

        #region Helper Functions

        public bool IsCurrentDataLoaded()
        {
            var loc = LocalizationDatas.Count > _index ? LocalizationDatas[_index] : null;
            return loc != null && loc.IsLoaded;
        }

        public void DrawDebugRect(RectTransform rectTransform)
        {
#if UNITY_EDITOR
            if (m_enableVisualDebug && rectTransform != null && IsCurrentDataLoaded())
            {
                Vector3[] worldPos = new Vector3[4];
                rectTransform.GetWorldCorners(worldPos);

                Vector2 min = worldPos[0];
                Vector2 max = worldPos[0];
                for (int i = 1; i < worldPos.Length; i++)
                {
                    var currentPos = worldPos[i];
                    if (currentPos.x < min.x)
                        min.x = currentPos.x;
                    if (currentPos.y < min.y)
                        min.y = currentPos.y;

                    if (currentPos.x > max.x)
                        max.x = currentPos.x;
                    if (currentPos.y > max.y)
                        max.y = currentPos.y;
                }
                DrawDebugRectInternal(Rect.MinMaxRect(min.x, min.y, max.x, max.y), m_debugColor);
            }
#endif
        }

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
                for (int i = 0; i < m_localizationDatas.Count; i++)
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

        public virtual void Init()
        {
            if (this == null || Application.isPlaying)
                return;

            CurrentLanguage = StartingLanguage;

            //Unload all other localization datas
            for (int i = 0; i < LocalizationDatas.Count; i++)
            {
                var data = LocalizationDatas[i];
                if (data != null)
                {
                    data.Unload();
                }
            }

            GetOrLoadCurrentData();
            SetLocaleDirtyAndReapplyToAll();
        }

        protected virtual void ApplySystemLanguageAsInitial()
        {
            if (this == null || Application.isPlaying)
                return;

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
            if (this == null || Application.isPlaying)
                return null;

            var loc = LocalizationDatas.Count > _index ? LocalizationDatas[_index] : null;
            if (loc != null)
            {
                if (!loc.IsLoaded && loc.CanLoadFromResources())
                    loc.LoadFromCachedResourcesPath();
            }

            return loc;
        }

        protected bool _isDirty = false;
        public virtual void SetLocaleDirty()
        {
            if (this == null || Application.isPlaying)
                return;

            _isDirty = true;

            Kyub.ApplicationContext.RunOnMainThread(() =>
            {
                if (this == null)
                    return;

                TryApply();
            });
        }

        protected bool _forceReapplyToAll = false;
        public virtual void SetLocaleDirtyAndReapplyToAll()
        {
            if (this == null || Application.isPlaying)
                return;

            _isDirty = true;
            _forceReapplyToAll = true;

            Kyub.ApplicationContext.RunOnMainThread(() =>
            {
                if (this == null)
                    return;

                TryApply();
            });
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
            if (this == null || Application.isPlaying)
                return;

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
            LocaleManager.CallOnLocalizedChanged(forceReapply);

        }

        protected internal virtual bool TryGetLocalizedText_Internal(string key, out string val)
        {
            if (this == null || Application.isPlaying)
            {
                val = key;
                return false;
            }

            key = key != null ? Kyub.RegexUtils.BulkReplace(key, LocaleManager.s_uselessCharsDict).Trim() : "";
            var dictionary = GetCurrentDictionaryData();

            var sucess = TryGetLocalizedText_Internal(key, dictionary, out val);

            if (val == null)
                val = key;
            return sucess;
        }

        protected internal virtual bool TryGetLocalizedText_Internal(string key, Dictionary<string, string> dict, out string val)
        {
            if (this == null || Application.isPlaying)
            {
                val = key;
                return false;
            }

            key = key != null ? Kyub.RegexUtils.BulkReplace(key, LocaleManager.s_uselessCharsDict).Trim() : "";

            bool sucess = false;
            val = key;
            if (dict != null && dict.TryGetValue(key, out val))
                sucess = true;

            if (val == null)
                val = key;
            return sucess;
        }

        protected internal bool TryClearLocaleTags_Internal(string text, out string outText)
        {
            if (this == null || Application.isPlaying)
            {
                outText = text;
                return false;
            }

            bool sucess = false;

            outText = Kyub.RegexUtils.BulkReplace(text, LocaleManager.s_localeClearTagsDict);
            if (text != outText)
                sucess = true;

            return sucess;
        }

        protected internal virtual bool TryLocalizeByLocaleTag_Internal(string text, out string localizedValue)
        {
            if (this == null || Application.isPlaying)
            {
                localizedValue = text;
                return false;
            }

            if (text.StartsWith(LocaleManager.LOCALE_SKIP_TAG, System.StringComparison.InvariantCultureIgnoreCase))
            {
                TryClearLocaleTags_Internal(text, out localizedValue);
                return true;
            }

            var sucess = false;
            Dictionary<string, string> paramsDict = null;
            localizedValue = LocaleManager.s_localeRegexCompiled.Replace(text,
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
            if (this == null || Application.isPlaying)
                return false;

            CultureInfo[] availableCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

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
            if (!Application.isPlaying && Instance != null && Instance.Enabled)
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

        protected static void DrawDebugRectInternal(Rect worldRect, Color color)
        {
#if UNITY_EDITOR
            var cachedGizmoColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawCube(worldRect.center, new Vector3(worldRect.width, worldRect.height, 1));
            Gizmos.color = cachedGizmoColor;

            //Debug.DrawLine(new Vector3(worldRect.x, worldRect.y), new Vector3(worldRect.x + worldRect.width, worldRect.y), color);
            //Debug.DrawLine(new Vector3(worldRect.x, worldRect.y), new Vector3(worldRect.x, worldRect.y + worldRect.height), color);
            //Debug.DrawLine(new Vector3(worldRect.x + worldRect.width, worldRect.y + worldRect.height), new Vector3(worldRect.x + worldRect.width, worldRect.y), color);
            //Debug.DrawLine(new Vector3(worldRect.x + worldRect.width, worldRect.y + worldRect.height), new Vector3(worldRect.x, worldRect.y + worldRect.height), color);
#endif
        }

        #endregion

        #region Editor Functions

#if UNITY_EDITOR
        [ContextMenu("Refresh")]
        protected virtual void RefreshInstance()
        {
            if (Application.isPlaying)
            {
                s_instance = null;
                return;
            }

            s_instance = this;
            s_instance.Init();
        }
#endif

        #endregion
    }
}