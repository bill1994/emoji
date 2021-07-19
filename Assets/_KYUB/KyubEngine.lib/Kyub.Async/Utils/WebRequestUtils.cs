using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Kyub.Async
{
    public static class WebRequestUtils
    {
        #region WebRequest Functions (Static)

        public static UnityWebRequest CreatePutWebRequest(string p_url, WWWRequestForm p_formData, int p_timeout)
        {
            p_url = FormatUrl(p_url, p_formData);

            UnityWebRequest www = new UnityWebRequest(p_url, UnityWebRequest.kHttpVerbPUT,
                (DownloadHandler)new DownloadHandlerBuffer(),
                p_formData.BodyData != null && p_formData.BodyData.Length > 0 ?
                (UploadHandler)new UploadHandlerRaw(p_formData.BodyData) : null); //UnityWebRequest.Put(p_url, p_formData.BodyData);
            if (p_timeout > 0)
                www.timeout = p_timeout;

            var v_headerDict = p_formData.Header;
            foreach (var v_pair in v_headerDict)
            {
                www.SetRequestHeader(v_pair.Key, v_pair.Value);
            }

            return www;
        }

        public static UnityWebRequest CreatePostWebRequest(string p_url, WWWRequestForm p_formData, int p_timeout)
        {
            UnityWebRequest www = CreatePutWebRequest(p_url, p_formData, p_timeout);
            www.method = UnityWebRequest.kHttpVerbPOST;

            return www;
        }

        public static UnityWebRequest CreatePatchWebRequest(string p_url, WWWRequestForm p_formData, int p_timeout)
        {
            UnityWebRequest www = CreatePutWebRequest(p_url, p_formData, p_timeout);
            www.method = "PATCH";

            return www;
        }

        public static UnityWebRequest CreateGetWebRequest(string p_url, WWWRequestForm p_formData, int p_timeout)
        {
            p_url = FormatUrl(p_url, p_formData);

            UnityWebRequest www = UnityWebRequest.Get(p_url);
            if (p_timeout > 0)
                www.timeout = p_timeout;

            var v_headerDict = p_formData.Header;
            foreach (var v_pair in v_headerDict)
            {
                www.SetRequestHeader(v_pair.Key, v_pair.Value);
            }

            return www;
        }

        public static UnityWebRequest CreateDeleteWebRequest(string p_url, WWWRequestForm p_formData, int p_timeout)
        {
            p_url = FormatUrl(p_url, p_formData);

            UnityWebRequest www = UnityWebRequest.Delete(p_url);
            www.downloadHandler = new DownloadHandlerBuffer();
            if (p_timeout > 0)
                www.timeout = p_timeout;

            var v_headerDict = p_formData.Header;
            foreach (var v_pair in v_headerDict)
            {
                www.SetRequestHeader(v_pair.Key, v_pair.Value);
            }

            return www;
        }

        public static string FormatUrl(string p_url, WWWRequestForm p_formData)
        {
            //p_url = AuthManager.CombineWithMainRoute(p_url);

            var v_paramsDict = p_formData.ParamsDataAsDict;
            var v_counter = 0;
            foreach (var v_pair in v_paramsDict)
            {
                var v_key = v_pair.Key.Trim();
                List<object> v_valuesToAdd = new List<object>();

                //Special Array Case
                if (v_key.EndsWith("[]") && (v_pair.Value is IEnumerable))
                {
                    var array = v_pair.Value as IEnumerable;
                    foreach (var element in array)
                    {
                        v_valuesToAdd.Add(element);
                    }
                }
                else
                    v_valuesToAdd.Add(v_pair.Value);

                //Add all elements to process (only in special case that this loop will run more than one time)
                foreach (var v_value in v_valuesToAdd)
                {
                    if (v_counter == 0)
                        p_url += "?";
                    else
                        p_url += "&";

                    //Make URL compatible with GET Escape encoding
                    p_url += v_key + "=" + System.Uri.EscapeDataString((v_value != null ? v_value.ToString() : ""));
                    v_counter++;
                }
            }
            return p_url;
        }

        public static string CombineRoutes(string p_route, string p_pathToCombine)
        {
            p_pathToCombine = p_pathToCombine == null ? "" : p_pathToCombine.Trim().Replace("\\", "/");

            //Combine Path with main route
            if (!string.IsNullOrEmpty(p_route) && !p_pathToCombine.StartsWith("http") && !p_pathToCombine.StartsWith("ftp") && !p_pathToCombine.StartsWith("ws") && !p_pathToCombine.StartsWith("www"))
            {
                //Remove Initial Bar because Path Combine cannot start with "/" or will fail to combine both paths
                while (p_pathToCombine.StartsWith("/"))
                {
                    p_pathToCombine = p_pathToCombine.Length > 1 ? p_pathToCombine.Substring(1, p_pathToCombine.Length - 1) : "";
                }
                //Combine with main route
                p_pathToCombine = System.IO.Path.Combine(p_route, p_pathToCombine).Replace("\\", "/");
            }
            return p_pathToCombine;
        }

        #endregion
    }

    #region Request Classes

    /// <summary>
    /// Analog to WWWForm but with support to json, custom headers, etc
    /// </summary>
    public class WWWRequestForm
    {
        #region Private Variables

        protected Dictionary<string, string> m_header = new Dictionary<string, string>() { { "Content-Type", "application/json" } };
        protected Dictionary<string, object> m_paramsData = new Dictionary<string, object>();
        protected Dictionary<string, object> m_bodyData = new Dictionary<string, object>();

        protected byte[] _bodyDataBytes = null;
        protected string _bodyDataJson = null;

        #endregion

        #region Public Properties

        public byte[] BodyData
        {
            get
            {
                if (_bodyDataBytes == null)
                {
                    //Create Request Post Data Bytes From JSON
                    //var bodyAsJson = BodyDataAsJson;
                    _bodyDataBytes = System.Text.Encoding.UTF8.GetBytes(BodyDataAsJson);
                }
                return _bodyDataBytes;
            }
        }

        public Dictionary<string, object> BodyDataAsDict
        {
            get
            {
                return m_bodyData;
            }
        }

        public string BodyDataAsJson
        {
            get
            {
                if (_bodyDataJson == null)
                {
                    //Create Request Post Data JSON
                    _bodyDataJson = m_bodyData == null || m_bodyData.Count == 0 ? "" : Kyub.SerializationUtils.ToJson(m_bodyData);
                }
                return _bodyDataJson;
            }
        }

        public Dictionary<string, string> Header
        {
            get
            {
                return m_header;
            }
            set
            {
                if (m_header == value)
                    return;
                m_header = value;
            }
        }

        public Dictionary<string, object> ParamsDataAsDict
        {
            get
            {
                return m_paramsData;
            }
        }

        #endregion

        #region Helper Functions

        public void ReplaceBodyData<T>(T bodyData)
        {
            _bodyDataBytes = null;
            _bodyDataJson = bodyData == null ? "" : Kyub.SerializationUtils.ToJson<T>(bodyData);
            m_bodyData.Clear();
            if (bodyData is Dictionary<string, object>)
            {
                m_bodyData = new Dictionary<string, object>(bodyData as Dictionary<string, object>);
            }
        }

        public void ReplaceBodyData(object bodyData)
        {
            _bodyDataBytes = null;
            _bodyDataJson = bodyData == null ? "" : Kyub.SerializationUtils.ToJson(bodyData);
            m_bodyData.Clear();
            if (bodyData is Dictionary<string, object>)
            {
                m_bodyData = new Dictionary<string, object>(bodyData as Dictionary<string, object>);
            }
        }

        public void AddBodyField(string p_key, object p_value)
        {
            //Reset byte array
            m_bodyData[p_key] = p_value;
            _bodyDataJson = null;
            _bodyDataBytes = null;
        }

        public void AddBodyObject<T>(T p_data, Serialization.Serializer p_customSerializer = null)
        {
            if (p_data != null)
            {
                Serialization.JsonObject v_data;
                if (p_customSerializer == null)
                    p_customSerializer = SerializationUtils.DefaultSerializer;

                p_customSerializer.TrySerialize(typeof(T), p_data, out v_data, null);

                var v_dataDict = v_data.AsDictionary;
                var v_sucess = false;

                foreach (var v_pair in v_dataDict)
                {
                    v_sucess = true;
                    m_bodyData[v_pair.Key] = v_pair.Value;
                }

                if (v_sucess)
                {
                    _bodyDataJson = null;
                    _bodyDataBytes = null;
                }
            }
        }

        public void AddParamField(string p_key, object p_value)
        {
            m_paramsData[p_key] = p_value;
        }

        public void AddParamObject<T>(T p_data, Serialization.Serializer p_customSerializer = null)
        {
            if (p_data != null)
            {
                Serialization.JsonObject v_data;
                if (p_customSerializer == null)
                    p_customSerializer = SerializationUtils.DefaultSerializer;

                p_customSerializer.TrySerialize(typeof(T), p_data, out v_data, null);

                var v_dataDict = v_data.AsDictionary;
                //var v_sucess = false;

                foreach (var v_pair in v_dataDict)
                {
                    //v_sucess = true;
                    m_paramsData[v_pair.Key] = v_pair.Value;
                }

            }
        }

        #endregion
    }

    #endregion
}
