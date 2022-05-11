using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Async;

namespace Kyub.Async
{
    public static class TextureSerializer
    {
        #region Load From Web
        
        public static ExternImgFile DeserializeFromWeb(string url, object target, string callBackFunctionName, TexSerWebReturnTypeEnum returnType = TexSerWebReturnTypeEnum.Texture)
        {
            return DeserializeFromWebInternal(url, null, target, callBackFunctionName, returnType);
        }

        public static ExternImgFile DeserializeFromWeb(string url, System.Action<Texture2D> callback)
        {
            return DeserializeFromWebInternal(url, callback, null, "", TexSerWebReturnTypeEnum.Texture);
        }

        public static ExternImgFile DeserializeFromWeb(string url, System.Action<Sprite> callback)
        {
            return DeserializeFromWebInternal(url, callback, null, "", TexSerWebReturnTypeEnum.Sprite);
        }

        public static ExternImgFile DeserializeFromWeb(string url, System.Action<ExternImgFile> callback)
        {
            return DeserializeFromWebInternal(url, callback, null, "", TexSerWebReturnTypeEnum.ExternImgFile);
        }

        public static ExternImgFile DeserializeFromWeb(string url)
        {
            return DeserializeFromWebInternal(url, null, null, "", TexSerWebReturnTypeEnum.ExternImgFile);
        }

        private static ExternImgFile DeserializeFromWebInternal(string url, System.Delegate callback, object target, string callBackFunctionName, TexSerWebReturnTypeEnum returnType)
        {
            //Try pick previous downloader
            TextureDownloader component = TextureDownloader.GetDownloader(url);
            //Create new Downloader (If not downloading yet)
            bool needStartRequest = false;
            if (component == null)
            {
                GameObject dummyObject = new GameObject("RequestImageFromWWW(Dummy)");
                component = dummyObject.AddComponent<TextureDownloader>();
                component.Url = url;
                needStartRequest = true;
            }
            //Register new Callback
            if (callback != null || (target != null && !string.IsNullOrEmpty(callBackFunctionName)))
            {
                var function = new FunctionAndParams();
                function.DelegatePointer = callback;
                function.Target = target;
                function.StringFunctionName = callBackFunctionName;
                component.RegisterCallback(returnType, function);
            }
            //Start Request (if not downloading)
            if(needStartRequest)
                component.StartRequest();
            return component.AsyncRequestOperation;
        }

        #endregion

        #region Helper Functions

        public static Texture2D TextureFromBytes(byte[] bytes)
        {
            Texture2D tex = null;
            if (bytes != null && bytes.Length > 0)
            {
                //Prevent Bugs in IOS
#if UNITY_IOS && !UNITY_EDITOR
			tex = new Texture2D(4, 4, TextureFormat.PVRTC_RGBA4, false);
#else
                tex = new Texture2D(4, 4);
#endif
                tex.LoadImage(bytes); //..this will auto-resize the texture dimensions.
            }
            return tex;
        }

        #endregion
    }

    #region Helper Classes

    public enum TexSerWebReturnTypeEnum { Texture, Sprite, ExternImgFile }

    [System.Serializable]
    public class ExternImgFile : AsyncRequestOperation
    {
        #region Private Variables

        [SerializeField]
        Sprite m_sprite = null;
        [SerializeField]
        Texture2D m_texture = null;
        [SerializeField]
        string m_url = "";

        #endregion

        #region Public Variables

        public Sprite Sprite
        {
            get
            {
                CheckNames();
                CheckIfNeedUnloadImage();
                return m_sprite;
            }
            set
            {
                if (m_sprite == value)
                    return;
                m_sprite = value;
                CheckNames();
            }
        }

        public Texture2D Texture
        {
            get
            {
                CheckNames();
                CheckIfNeedUnloadImage();
                return m_texture;
            }
            set
            {
                if (m_texture == value)
                    return;
                m_texture = value;
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
                if (m_sprite != null && m_sprite.name != m_url)
                    m_sprite.name = m_url;
                if (m_texture != null && m_texture.name != m_url)
                    m_texture.name = m_url;
            }
        }

        private void CheckIfNeedUnloadImage()
        {
            if (!string.IsNullOrEmpty(Error) && (m_sprite != null || m_texture != null))
            {
                var texture = m_sprite != null? m_sprite.texture : null;
                //Destroy SpriteTexture
                if(texture != null)
                    DestroyUtils.DestroyImmediate(m_sprite.texture);
                //Destroy Object Texture
                if(m_texture != null && !MarkedToDestroy.IsMarked(m_texture))
                    DestroyUtils.DestroyImmediate(m_texture);
                if(m_sprite != null)
                    DestroyUtils.DestroyImmediate(m_sprite);
                m_texture = null;
                m_sprite = null;
            }
        }

        #endregion
    }

    #endregion
}
