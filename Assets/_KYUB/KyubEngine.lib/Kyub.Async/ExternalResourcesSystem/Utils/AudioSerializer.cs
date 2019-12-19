using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Async;

namespace Kyub.Async
{
    public static class AudioSerializer
    {
        #region Load From Web

        public static ExternAudioFile DeserializeFromWeb(string p_url, object p_target, string p_callBackFunctionName, AudioSerWebReturnTypeEnum p_returnType = AudioSerWebReturnTypeEnum.Clip)
        {
            return DeserializeFromWebInternal(p_url, null, p_target, p_callBackFunctionName, p_returnType);
        }

        public static ExternAudioFile DeserializeFromWeb(string p_url, System.Action<AudioClip> p_callback)
        {
            return DeserializeFromWebInternal(p_url, p_callback, null, "", AudioSerWebReturnTypeEnum.Clip);
        }

        public static ExternAudioFile DeserializeFromWeb(string p_url, System.Action<ExternAudioFile> p_callback)
        {
            return DeserializeFromWebInternal(p_url, p_callback, null, "", AudioSerWebReturnTypeEnum.ExternAudioFile);
        }

        public static ExternAudioFile DeserializeFromWeb(string p_url)
        {
            return DeserializeFromWebInternal(p_url, null, null, "", AudioSerWebReturnTypeEnum.ExternAudioFile);
        }

        private static ExternAudioFile DeserializeFromWebInternal(string p_url, System.Delegate p_callback, object p_target, string p_callBackFunctionName, AudioSerWebReturnTypeEnum p_returnType)
        {
            //Try pick previous downloader
            AudioDownloader v_component = AudioDownloader.GetDownloader(p_url);
            //Create new Downloader (If not downloading yet)
            bool v_needStartRequest = false;
            if (v_component == null)
            {
                GameObject v_dummyObject = new GameObject("RequestAudioFromWWW(Dummy)");
                v_component = v_dummyObject.AddComponent<AudioDownloader>();
                v_component.Url = p_url;
                v_needStartRequest = true;
            }
            //Register new Callback
            if (p_callback != null || (p_target != null && !string.IsNullOrEmpty(p_callBackFunctionName)))
            {
                var v_function = new Kyub.FunctionAndParams();
                v_function.DelegatePointer = p_callback;
                v_function.Target = p_target;
                v_function.StringFunctionName = p_callBackFunctionName;
                v_component.RegisterCallback(p_returnType, v_function);
            }
            //Start Request (if not downloading)
            if (v_needStartRequest)
                v_component.StartRequest();
            return v_component.AsyncRequestOperation;
        }

        #endregion
    }

    #region Helper Classes

    public enum AudioSerWebReturnTypeEnum { Clip, ExternAudioFile }

    [System.Serializable]
    public class ExternAudioFile : AsyncRequestOperation
    {
        #region Private Variables

        [SerializeField]
        AudioClip m_clip = null;
        [SerializeField]
        string m_url = "";

        #endregion

        #region Public Variables

        public AudioClip Clip
        {
            get
            {
                CheckNames();
                CheckIfNeedUnloadAudio();
                return m_clip;
            }
            set
            {
                if (m_clip == value)
                    return;
                m_clip = value;
                CheckNames();
            }
        }

        public string Url
        {
            get
            {
                CheckNames();
                return m_url;
            }
            set
            {
                if (m_url == value)
                    return;
                m_url = value;
                CheckNames();
            }
        }

        #endregion

        #region Helper Functions

        private void CheckNames()
        {
            if (!string.IsNullOrEmpty(m_url))
            {
                if (m_clip != null && m_clip.name != m_url)
                    m_clip.name = m_url;
            }
        }

        private void CheckIfNeedUnloadAudio()
        {
            if (!string.IsNullOrEmpty(Error) && m_clip != null)
            {
                DestroyUtils.DestroyImmediate(m_clip);
                m_clip = null;
            }
        }

        #endregion
    }

    #endregion
}
