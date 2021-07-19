using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Kyub.Async
{
    public static class WebRequestUtils
    {
        #region WebRequest Functions (Static)

        public static UnityWebRequest CreatePutWebRequest(string url, WWWRequestForm formData, int timeout)
        {
            url = FormatUrl(url, formData);

            UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT,
                (DownloadHandler)new DownloadHandlerBuffer(),
                formData.BodyData != null && formData.BodyData.Length > 0 ?
                (UploadHandler)new UploadHandlerRaw(formData.BodyData) : null); //UnityWebRequest.Put(url, formData.BodyData);
            if (timeout > 0)
                www.timeout = timeout;

            var headerDict = formData.Header;
            foreach (var pair in headerDict)
            {
                www.SetRequestHeader(pair.Key, pair.Value);
            }

            return www;
        }

        public static UnityWebRequest CreatePostWebRequest(string url, WWWRequestForm formData, int timeout)
        {
            UnityWebRequest www = CreatePutWebRequest(url, formData, timeout);
            www.method = UnityWebRequest.kHttpVerbPOST;

            return www;
        }

        public static UnityWebRequest CreatePatchWebRequest(string url, WWWRequestForm formData, int timeout)
        {
            UnityWebRequest www = CreatePutWebRequest(url, formData, timeout);
            www.method = "PATCH";

            return www;
        }

        public static UnityWebRequest CreateGetWebRequest(string url, WWWRequestForm formData, int timeout)
        {
            url = FormatUrl(url, formData);

            UnityWebRequest www = UnityWebRequest.Get(url);
            if (timeout > 0)
                www.timeout = timeout;

            var headerDict = formData.Header;
            foreach (var pair in headerDict)
            {
                www.SetRequestHeader(pair.Key, pair.Value);
            }

            return www;
        }

        public static UnityWebRequest CreateDeleteWebRequest(string url, WWWRequestForm formData, int timeout)
        {
            url = FormatUrl(url, formData);

            UnityWebRequest www = UnityWebRequest.Delete(url);
            www.downloadHandler = new DownloadHandlerBuffer();
            if (timeout > 0)
                www.timeout = timeout;

            var headerDict = formData.Header;
            foreach (var pair in headerDict)
            {
                www.SetRequestHeader(pair.Key, pair.Value);
            }

            return www;
        }

        public static string FormatUrl(string url, WWWRequestForm formData)
        {
            //url = AuthManager.CombineWithMainRoute(url);

            var paramsDict = formData.ParamsDataAsDict;
            var counter = 0;
            foreach (var pair in paramsDict)
            {
                var key = pair.Key.Trim();
                List<object> valuesToAdd = new List<object>();

                //Special Array Case
                if (key.EndsWith("[]") && (pair.Value is IEnumerable))
                {
                    var array = pair.Value as IEnumerable;
                    foreach (var element in array)
                    {
                        valuesToAdd.Add(element);
                    }
                }
                else
                    valuesToAdd.Add(pair.Value);

                //Add all elements to process (only in special case that this loop will run more than one time)
                foreach (var value in valuesToAdd)
                {
                    if (counter == 0)
                        url += "?";
                    else
                        url += "&";

                    //Make URL compatible with GET Escape encoding
                    url += key + "=" + System.Uri.EscapeDataString((value != null ? value.ToString() : ""));
                    counter++;
                }
            }
            return url;
        }

        public static string CombineRoutes(string route, string pathToCombine)
        {
            pathToCombine = pathToCombine == null ? "" : pathToCombine.Trim().Replace("\\", "/");

            //Combine Path with main route
            if (!string.IsNullOrEmpty(route) && !pathToCombine.StartsWith("http") && !pathToCombine.StartsWith("ftp") && !pathToCombine.StartsWith("ws") && !pathToCombine.StartsWith("www"))
            {
                //Remove Initial Bar because Path Combine cannot start with "/" or will fail to combine both paths
                while (pathToCombine.StartsWith("/"))
                {
                    pathToCombine = pathToCombine.Length > 1 ? pathToCombine.Substring(1, pathToCombine.Length - 1) : "";
                }
                //Combine with main route
                pathToCombine = System.IO.Path.Combine(route, pathToCombine).Replace("\\", "/");
            }
            return pathToCombine;
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

        public void AddBodyField(string key, object value)
        {
            //Reset byte array
            m_bodyData[key] = value;
            _bodyDataJson = null;
            _bodyDataBytes = null;
        }

        public void AddBodyObject<T>(T data, Serialization.Serializer customSerializer = null)
        {
            if (data != null)
            {
                Serialization.JsonObject resultData;
                if (customSerializer == null)
                    customSerializer = SerializationUtils.DefaultSerializer;

                customSerializer.TrySerialize(typeof(T), data, out resultData, null);

                var dataDict = resultData.AsDictionary;
                var sucess = false;

                foreach (var pair in dataDict)
                {
                    sucess = true;
                    m_bodyData[pair.Key] = pair.Value;
                }

                if (sucess)
                {
                    _bodyDataJson = null;
                    _bodyDataBytes = null;
                }
            }
        }

        public void AddParamField(string key, object value)
        {
            m_paramsData[key] = value;
        }

        public void AddParamObject<T>(T data, Serialization.Serializer customSerializer = null)
        {
            if (data != null)
            {
                Serialization.JsonObject resultData;
                if (customSerializer == null)
                    customSerializer = SerializationUtils.DefaultSerializer;

                customSerializer.TrySerialize(typeof(T), data, out resultData, null);

                var dataDict = resultData.AsDictionary;
                //var sucess = false;

                foreach (var pair in dataDict)
                {
                    //sucess = true;
                    m_paramsData[pair.Key] = pair.Value;
                }

            }
        }

        #endregion
    }

    #endregion
}
