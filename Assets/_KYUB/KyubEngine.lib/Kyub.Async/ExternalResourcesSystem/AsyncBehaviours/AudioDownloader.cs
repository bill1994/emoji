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

        public virtual void RegisterCallback(AudioSerWebReturnTypeEnum returnType, FunctionAndParams function)
        {
            if (function != null && (function.DelegatePointer != null || (function.Target != null && !string.IsNullOrEmpty(function.StringFunctionName))))
            {
                ReturnTypePerCallbackDict.Add(returnType, function);
            }
        }

        protected override IEnumerator ProcessRequest()
        {
            if (Url != null && GetDownloader(Url) == this)
            {
                AsyncRequestOperation.Url = Url;
                //MarkedToDestroy.RemoveMark(this.gameObject);
                ClipLoaded = null;

                var externsion = Url != null ? System.IO.Path.GetExtension(Url).ToLower() : "";
                var audioType = AudioType.WAV;
                if (externsion.Contains(".ogg"))
                    audioType = AudioType.OGGVORBIS;
                else if (externsion.Contains(".mp3") || externsion.Contains(".mp2"))
                    audioType = AudioType.MPEG;

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(Url, audioType))
                {
                    www.timeout = RequestStackManager.RequestTimeLimit;
                    yield return www.SendWebRequest();

                    yield return ProcessWWWReturn(www);
                }
            }
        }

        protected override IEnumerator ProcessWWWReturn(UnityWebRequest www)
        {
            try
            {
                var error = www == null ? "Request Unscheduled" : www.error;

                if (www == null || www.isNetworkError || www.isHttpError || !string.IsNullOrEmpty(error))
                    Debug.Log("Download Failed: " + error + " Url: " + Url);
                else
                {
                    ClipLoaded = DownloadHandlerAudioClip.GetContent(www);
                }

                AsyncRequestOperation.Clip = ClipLoaded;
                AsyncRequestOperation.Url = Url;
                AsyncRequestOperation.Error = error;

                //Requests callbacks (based in each parameter)
                foreach (var pair in ReturnTypePerCallbackDict)
                {
                    var returnType = pair.Key;
                    var function = pair.Value;
                    if (function != null)
                    {
                        function.Params.Clear();
                        if (returnType == AudioSerWebReturnTypeEnum.ExternAudioFile)
                            function.Params.Add(AsyncRequestOperation);
                        else
                            function.Params.Add(AsyncRequestOperation.Clip);
                        function.CallFunction();
                    }
                }
            }
            catch { }

            yield break;
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

        public static AudioDownloader GetDownloader(string url)
        {
            foreach (AudioDownloader downloader in _downloadersInScene)
            {
                if (downloader != null && !downloader.IsMarkedToDestroy(true) && string.Equals(downloader.Url, url))
                {
                    return downloader;
                }
            }
            return null;
        }

        public static bool IsDownloading(string url)
        {
            foreach (AudioDownloader downloader in _downloadersInScene)
            {
                if (downloader != null && !downloader.IsMarkedToDestroy(true) && string.Equals(downloader.Url, url))
                {
                    return true;
                }
            }
            return false;
        }

        public static void CancelAllRequestsWithUrl(string url)
        {
            foreach (AudioDownloader downloader in _downloadersInScene)
            {
                if (downloader != null && !downloader.IsMarkedToDestroy(true) && string.Equals(downloader.Url, url))
                {
                    downloader.CancelRequest();
                }
            }
        }

        #endregion
    }
}

