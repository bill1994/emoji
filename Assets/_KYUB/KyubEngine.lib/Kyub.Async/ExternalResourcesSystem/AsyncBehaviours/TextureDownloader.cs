using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub;
using Kyub.Extensions;
using Kyub.Collections;
using UnityEngine.Networking;

namespace Kyub.Async
{
	public class TextureDownloader : WWWAsyncRequest<ExternImgFile>
	{
		#region Private Variables

		[SerializeField]
		Texture2D m_textureLoaded = null;

        ArrayDict<TexSerWebReturnTypeEnum, FunctionAndParams> _returnTypePerCallbackDict = new ArrayDict<TexSerWebReturnTypeEnum, FunctionAndParams>();

		#endregion
		
		#region Public Properties
		
		public Texture2D TextureLoaded
		{
			get
			{
				return m_textureLoaded;
			}
			set
			{
				if(m_textureLoaded == value)
					return;
				m_textureLoaded = value;
			}
		}
		
		public ArrayDict<TexSerWebReturnTypeEnum, FunctionAndParams> ReturnTypePerCallbackDict
        {
			get
			{
				if(_returnTypePerCallbackDict == null)
                    _returnTypePerCallbackDict = new ArrayDict<TexSerWebReturnTypeEnum, FunctionAndParams>();
				return _returnTypePerCallbackDict;
			}
			set
			{
				if(_returnTypePerCallbackDict == value)
					return;
                _returnTypePerCallbackDict = value;
			}
		}
		
		#endregion
		
		#region Unity Functions
		
		protected override void OnEnable()
		{
            if(!_downloadersInScene.Contains(this))
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

        public virtual void RegisterCallback(TexSerWebReturnTypeEnum p_returnType, FunctionAndParams p_function)
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
                TextureLoaded = null;

                using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(Url))
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
                    TextureLoaded = ((DownloadHandlerTexture)www.downloadHandler).texture;

                    //Remove Invalid Unity Image
                    if (TextureLoaded != null && TextureLoaded.width <= 8 && TextureLoaded.height <= 8)
                    {
                        GameObject.Destroy(TextureLoaded);
                        TextureLoaded = null;
                    }
                }

                AsyncRequestOperation.Sprite = TextureLoaded != null ? Sprite.Create(TextureLoaded, new Rect(0, 0, TextureLoaded.width, TextureLoaded.height), new Vector2(0.5f, 0.5f)) : null;
                AsyncRequestOperation.Texture = TextureLoaded;
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
                        if (v_returnType == TexSerWebReturnTypeEnum.ExternImgFile)
                            v_function.Params.Add(AsyncRequestOperation);
                        else if (v_returnType == TexSerWebReturnTypeEnum.Sprite)
                            v_function.Params.Add(AsyncRequestOperation.Sprite);
                        else
                            v_function.Params.Add(AsyncRequestOperation.Texture);
                        v_function.CallFunction();
                    }
                }
            }
            catch { }
        }
        
        #endregion

        #region Static Functions

        static List<TextureDownloader> _downloadersInScene = new List<TextureDownloader>();

		public static List<TextureDownloader> DownloadersInScene
		{
			get
			{
				if(_downloadersInScene == null)
					_downloadersInScene = new List<TextureDownloader>();
				return _downloadersInScene;
			}
			private set
			{
				if(_downloadersInScene == value)
					return;
				_downloadersInScene = value;
			}
		}

		public static TextureDownloader GetDownloader(string p_url)
		{
			foreach(TextureDownloader v_downloader in _downloadersInScene)
			{
				if(v_downloader != null && !v_downloader.IsMarkedToDestroy(true) && string.Equals(v_downloader.Url, p_url))
				{
					return v_downloader;
				}
			}
			return null;
		}

		public static bool IsDownloading(string p_url)
		{
			foreach(TextureDownloader v_downloader in _downloadersInScene)
			{
				if(v_downloader != null && !v_downloader.IsMarkedToDestroy(true) && string.Equals(v_downloader.Url, p_url))
				{
					return true;
				}
			}
			return false;
		}

		public static void CancelAllRequestsWithUrl(string p_url)
		{
			foreach(TextureDownloader v_downloader in _downloadersInScene)
			{
				if(v_downloader != null && !v_downloader.IsMarkedToDestroy(true) && string.Equals(v_downloader.Url, p_url))
				{
					v_downloader.CancelRequest();
				}
			}
		}

		#endregion
	}
}

