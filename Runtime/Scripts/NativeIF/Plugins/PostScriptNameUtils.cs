using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Kyub.Internal.NativeInputPlugin
{
    public class PostScriptNameUtils
    {
        #region Static Fields

        public const string POST_SCRIPT_TABLE = "root_ps_table.json";
        protected static PostScriptTableData s_loadedPostScriptTable = null;
        protected static bool s_initialized = false;

        #endregion

        #region Internal Init Functions

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            if (Application.isMobilePlatform)
            {
                LoadPostScriptTableAsync_Internal((result) =>
                {
                    s_initialized = true;
                    s_loadedPostScriptTable = result;
                });
            }
            else
                s_initialized = true;
        }

        #endregion

        #region Public Functions

        public static string GetPostScriptName(string entry)
        {
            while (!s_initialized) { };
            return s_loadedPostScriptTable != null ? s_loadedPostScriptTable.GetPostScriptName(entry) : entry;
        }

        public static string GetPostScriptName(TMP_FontAsset tmpfont)
        {
            while (!s_initialized) { };
            return s_loadedPostScriptTable != null ? s_loadedPostScriptTable.GetPostScriptName(tmpfont) : 
                (tmpfont != null ? tmpfont.faceInfo.familyName + "-" + tmpfont.faceInfo.styleName : string.Empty);
        }

        public static string GetPostScriptName(Font font)
        {
            while (!s_initialized) { };
            return s_loadedPostScriptTable != null ? s_loadedPostScriptTable.GetPostScriptName(font) :
                (font != null ? font.name : string.Empty);
        }

        #endregion

        #region Helper Functions

        static string GetPostStriptTableStreamingAssetFilePath()
        {
            var tableFile = System.IO.Path.Combine(Application.streamingAssetsPath, "res/font/" + POST_SCRIPT_TABLE).Replace("\\", "/");
            return tableFile;
        }

        static void LoadPostScriptTableAsync_Internal(System.Action<PostScriptTableData> callback)
        {
            PostScriptTableData table = new PostScriptTableData();
            try
            {
                var tableFilePath = GetPostStriptTableStreamingAssetFilePath();
                ReadAllText_Internal(tableFilePath, (json) =>
                {
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            JsonUtility.FromJsonOverwrite(json, table);
                        }
                        catch (System.Exception)
                        {
                            Debug.LogWarning("Failed to deserialize PostScriptTableData");
                        }
                    }
                    if (callback != null)
                        callback.Invoke(table);
                });
            }
            catch
            {
                if (callback != null)
                    callback.Invoke(table);
            }
        }

        static void ReadAllText_Internal(string url, System.Action<string> callback)
        {
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "MyFile");
            string textResult = "";
            if (filePath.Contains("://"))
            {
                var www = UnityWebRequest.Get(url);
                var operation = www.SendWebRequest();
                operation.completed += (result) =>
                {
                    if (www != null)
                    {
                        if (!www.isNetworkError && !www.isHttpError && string.IsNullOrEmpty(www.error))
                            textResult = www.downloadHandler.text;

                        www.Dispose();
                    }

                    if (callback != null)
                        callback.Invoke(textResult == null ? string.Empty : textResult);
                };
                return;
            }
            else if (System.IO.File.Exists(url))
            {
                try
                {
                    textResult = System.IO.File.ReadAllText(filePath);
                }
                catch { }
            }

            if (callback != null)
                callback.Invoke(textResult == null? string.Empty : textResult);

        }

        #endregion
    }

    [System.Serializable]
    public class PostScriptTableData : ISerializationCallbackReceiver
    {
        #region Helper Classes

        [System.Serializable]
        class PSKeyValuePair
        {
            public string Key;
            public string Value;
        }

        #endregion

        #region Private Variables

        [SerializeField]
        List<PSKeyValuePair> m_serializableTable = new List<PSKeyValuePair>();

        Dictionary<string, string> _table = new Dictionary<string, string>();

        #endregion

        #region Public Properties

        public Dictionary<string, string> Table
        {
            get
            {
                if (_table == null)
                    _table = new Dictionary<string, string>();
                return _table;
            }
        }

        #endregion

        #region Helper Functions

        public void AddEntry(string entry, string postScriptName)
        {
            if (!string.IsNullOrEmpty(entry) && !string.IsNullOrEmpty(postScriptName))
                _table[entry] = postScriptName;
        }

        public void AddEntry(Font font, string postScriptName)
        {
            if (font != null && !string.IsNullOrEmpty(postScriptName))
            {
                AddEntry(font.name, postScriptName);
#if UNITY_EDITOR
                //Special case to fill TMP_FontAsset Family+Style in Editor
                UnityEngine.TextCore.LowLevel.FontEngine.InitializeFontEngine();
                UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(font);
                var faceInfo = UnityEngine.TextCore.LowLevel.FontEngine.GetFaceInfo();
                var familyAndStyle = faceInfo.familyName + "-" + faceInfo.styleName;
                AddEntry(familyAndStyle, postScriptName);
                UnityEngine.TextCore.LowLevel.FontEngine.DestroyFontEngine();
#endif
            }
        }

        public void AddEntry(TMP_FontAsset tmpfont, string postScriptName)
        {
            if (tmpfont != null && !string.IsNullOrEmpty(postScriptName))
            {
                AddEntry(tmpfont.name, postScriptName);
                var familyAndStyle = tmpfont.faceInfo.familyName + "-" + tmpfont.faceInfo.styleName;
                AddEntry(familyAndStyle, postScriptName);
            }
        }

        public string GetPostScriptName(string entry)
        {
            var result = string.Empty;
            if (entry == null)
                entry = string.Empty;

            if (!string.IsNullOrEmpty(entry))
            {
                _table.TryGetValue(entry, out result);
            }
            return string.IsNullOrEmpty(result) ? entry : result;
        }

        public string GetPostScriptName(TMP_FontAsset tmpfont)
        {
            var result = string.Empty;
            if (tmpfont != null)
            {
                if (tmpfont.sourceFontFile != null)
                    return GetPostScriptName(tmpfont.sourceFontFile);
                else
                {
                    var familyAndStyle = tmpfont.faceInfo.familyName + "-" + tmpfont.faceInfo.styleName;
                    if (!string.IsNullOrEmpty(familyAndStyle) && _table.TryGetValue(familyAndStyle, out result))
                        return string.IsNullOrEmpty(result) ? familyAndStyle : result;
                    else if (familyAndStyle != null)
                        return familyAndStyle;
                }
            }
            return result == null ? string.Empty : result;
        }

        public string GetPostScriptName(Font font)
        {
            var result = string.Empty;
            if (font != null)
            {
                if (!string.IsNullOrEmpty(font.name) && _table.TryGetValue(font.name, out result))
                    return string.IsNullOrEmpty(result) ? font.name : result;
                else if(font.name != null)
                    return font.name;
            }
            return result == null ? string.Empty : result;
        }

        public void OnBeforeSerialize()
        {
            m_serializableTable = new List<PSKeyValuePair>();
            if (_table != null)
            {
                foreach (var pair in _table)
                {
                    m_serializableTable.Add(new PSKeyValuePair() { Key = pair.Key, Value = pair.Value });
                }
            }
        }

        public void OnAfterDeserialize()
        {
            _table = new Dictionary<string, string>();
            if (m_serializableTable != null)
            {
                foreach (var pair in m_serializableTable)
                {
                    _table.Add(pair.Key, pair.Value);
                }
            }
        }

        #endregion
    }
}
