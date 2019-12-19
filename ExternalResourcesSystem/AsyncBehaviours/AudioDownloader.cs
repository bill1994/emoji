using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub;
using Kyub.Extensions;
using Kyub.Collections;
using UnityEngine.Networking;

namespace Kyub.Async
{
    public class AudioDownloader : WWWAsyncRequest<ExternAudioFile>
    {
        #region Private Variables

        [SerializeField]
        AudioClip m_clipLoaded = null;

        ArrayDict<AudioSerWebReturnTypeEnum, FunctionAndParams> _returnTypePerCallbackDict = new ArrayDict<AudioSerWebReturnTypeEnum, FunctionAndParams>();

        #endregion

        #region Public Properties

        public AudioClip ClipLoaded
        {
            get
            {
                return m_clipLoaded;
            }
            set
            {
                if (m_clipLoaded == value)
                    return;
                m_clipLoaded = value;
            }
        }

        public ArrayDict<AudioSerWebReturnTypeEnum, FunctionAndParams> ReturnTypePerCallbackDict
        {
            get
            {
                if (_returnTypePerCallbackDict == null)
                    _returnTypePerCallbackDict = new ArrayDict<AudioSerWebReturnTypeEnum, FunctionAndParams>();
                return _returnTypePerCallbackDict;
            }
            set
            {
                if (_returnTypePerCallbackDict == value)
                    return;
                _returnTypePerCallbackDict = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            if (!_downloadersInScene.Contains(this))
                _downloadersInScene.Add(this);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (_downloadersInScene.Contains(this))
                _downloadersInScene.Remove(this);
            base.OnDisable();
        }

        #endregion

        #region Helper Functions

        public virtual void RegisterCallback(AudioSerWebReturnTypeEnum p_returnType, FunctionAndParams p_function)
        {
            if (p_function != null && (p_function.DelegatePointer != null || (p_function.Target != null && !string.IsNullOrEmpty(p_function.StringFunctionName))))
            {
                ReturnTypePerCallbackDict.Add(p_returnType, p_function);
            }
        }

        protected override IEnumerator ProcessRequest()
        {
            if (Url != null && GetDownloader(Url) == this)
            {
                AsyncRequestOperation.Url = Url;
                //MarkedToDestroy.RemoveMark(this.gameObject);
                ClipLoaded = null;

                var v_externsion = Url != null ? System.IO.Path.GetExtension(Url).ToLower() : "";
                var v_audioType = AudioType.WAV;
                if (v_externsion.Contains(".ogg"))
                    v_audioType = AudioType.OGGVORBIS;
                else if (v_externsion.Contains(".mp3") || v_externsion.Contains(".mp2"))
                    v_audioType = AudioType.MPEG;

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(Url, v_audioType))
                {
                    www.timeout = RequestStackManager.RequestTimeLimit;
                    yield return www.SendWebRequest();

                    ProcessWWWReturn(www);
                }
            }
        }

        protected override void ProcessWWWReturn(UnityWebRequest www)
        {
            try
            {
                var v_error = www == null ? "Request Unscheduled" : www.error;

                if (www == null || www.isNetworkError || www.isHttpError || !string.IsNullOrEmpty(v_error))
                    Debug.Log("Download Failed: " + v_error + " Url: " + Url);
                else
                {
                    ClipLoaded = DownloadHandlerAudioClip.GetContent(www);
                }

                AsyncRequestOperation.Clip = ClipLoaded;
                AsyncRequestOperation.Url = Url;
                AsyncRequestOperation.Error = v_error;

                //Requests callbacks (based in each parameter)
                foreach (var v_pair in ReturnTypePerCallbackDict)
                {
                    var v_returnType = v_pair.Key;
                    var v_function = v_pair.Value;
                    if (v_function != null)
                    {
                        v_function.Params.Clear();
                        if (v_returnType == AudioSerWebReturnTypeEnum.ExternAudioFile)
                            v_function.Params.Add(AsyncRequestOperation);
                        else
                            v_function.Params.Add(AsyncRequestOperation.Clip);
                        v_function.CallFunction();
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Static Functions

        static List<AudioDownloader> _downloadersInScene = new List<AudioDownloader>();

        public static List<AudioDownloader> DownloadersInScene
        {
            get
            {
                if (_downloadersInScene == null)
                    _downloadersInScene = new List<AudioDownloader>();
                return _downloadersInScene;
            }
            private set
            {
                if (_downloadersInScene == value)
                    return;
                _downloadersInScene = value;
            }
        }

        public static AudioDownloader GetDownloader(string p_url)
        {
            foreach (AudioDownloader v_downloader in _downloadersInScene)
            {
                if (v_downloader != null && !v_downloader.IsMarkedToDestroy(true) && string.Equals(v_downloader.Url, p_url))
                {
                    return v_downloader;
                }
            }
            return null;
        }

        public static bool IsDownloading(string p_url)
        {
            foreach (AudioDownloader v_downloader in _downloadersInScene)
            {
                if (v_downloader != null && !v_downloader.IsMarkedToDestroy(true) && string.Equals(v_downloader.Url, p_url))
                {
                    return true;
                }
            }
            return false;
        }

        public static void CancelAllRequestsWithUrl(string p_url)
        {
            foreach (AudioDownloader v_downloader in _downloadersInScene)
            {
                if (v_downloader != null && !v_downloader.IsMarkedToDestroy(true) && string.Equals(v_downloader.Url, p_url))
                {
                    v_downloader.CancelRequest();
                }
            }
        }

        #endregion
    }
}

