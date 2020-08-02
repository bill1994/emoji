using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Async;

namespace Kyub.Async
{
    public static class TextureSerializer
    {
        #region Load From Web
        
        public static ExternImgFile DeserializeFromWeb(string p_url, object p_target, string p_callBackFunctionName, TexSerWebReturnTypeEnum p_returnType = TexSerWebReturnTypeEnum.Texture)
        {
            return DeserializeFromWebInternal(p_url, null, p_target, p_callBackFunctionName, p_returnType);
        }

        public static ExternImgFile DeserializeFromWeb(string p_url, System.Action<Texture2D> p_callback)
        {
            return DeserializeFromWebInternal(p_url, p_callback, null, "", TexSerWebReturnTypeEnum.Texture);
        }

        public static ExternImgFile DeserializeFromWeb(string p_url, System.Action<Sprite> p_callback)
        {
            return DeserializeFromWebInternal(p_url, p_callback, null, "", TexSerWebReturnTypeEnum.Sprite);
        }

        public static ExternImgFile DeserializeFromWeb(string p_url, System.Action<ExternImgFile> p_callback)
        {
            return DeserializeFromWebInternal(p_url, p_callback, null, "", TexSerWebReturnTypeEnum.ExternImgFile);
        }

        public static ExternImgFile DeserializeFromWeb(string p_url)
        {
            return DeserializeFromWebInternal(p_url, null, null, "", TexSerWebReturnTypeEnum.ExternImgFile);
        }

        private static ExternImgFile DeserializeFromWebInternal(string p_url, System.Delegate p_callback, object p_target, string p_callBackFunctionName, TexSerWebReturnTypeEnum p_returnType)
        {
            //Try pick previous downloader
            TextureDownloader v_component = TextureDownloader.GetDownloader(p_url);
            //Create new Downloader (If not downloading yet)
            bool v_needStartRequest = false;
            if (v_component == null)
            {
                GameObject v_dummyObject = new GameObject("RequestImageFromWWW(Dummy)");
                v_component = v_dummyObject.AddComponent<TextureDownloader>();
                v_component.Url = p_url;
                v_needStartRequest = true;
            }
            //Register new Callback
            if (p_callback != null || (p_target != null && !string.IsNullOrEmpty(p_callBackFunctionName)))
            {
                var v_function = new FunctionAndParams();
                v_function.DelegatePointer = p_callback;
                v_function.Target = p_target;
                v_function.StringFunctionName = p_callBackFunctionName;
                v_component.RegisterCallback(p_returnType, v_function);
            }
            //Start Request (if not downloading)
            if(v_needStartRequest)
                v_component.StartRequest();
            return v_component.AsyncRequestOperation;
        }

        #endregion

        #region Helper Functions

        public static Texture2D TextureFromBytes(byte[] p_bytes)
        {
            Texture2D v_tex = null;
            if (p_bytes != null && p_bytes.Length > 0)
            {
                //Prevent Bugs in IOS
#if UNITY_IOS && !UNITY_EDITOR
			v_tex = new Texture2D(4, 4, TextureFormat.PVRTC_RGBA4, false);
#else
                v_tex = new Texture2D(4, 4);
#endif
                v_tex.LoadImage(p_bytes); //..this will auto-resize the texture dimensions.
            }
            return v_tex;
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
                var v_texture = m_sprite != null? m_sprite.texture : null;
                //Destroy SpriteTexture
                if(v_texture != null)
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
