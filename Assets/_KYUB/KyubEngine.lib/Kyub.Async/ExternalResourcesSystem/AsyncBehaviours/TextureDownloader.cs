using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub;
using Kyub.Extensions;
using Kyub.Collections;
using UnityEngine.Networking;
using Kyub.Async.Extensions;

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

        public virtual void RegisterCallback(TexSerWebReturnTypeEnum returnType, FunctionAndParams function)
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
                TextureLoaded = null;

                using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(Url))
                {
                    www.timeout = RequestStackManager.RequestTimeLimit;
                    yield return www.SendWebRequest();

                    yield return ProcessWWWReturn(www);
                }
            }
        }

		protected override IEnumerator ProcessWWWReturn(UnityWebRequest www)
		{
			var error = www == null ? "Request Unscheduled" : www.error;

			if (www == null || www.isNetworkError || www.isHttpError || !string.IsNullOrEmpty(error))
				Debug.Log("Download Failed: " + error + " Url: " + Url);
			else
			{
				var processing = true;
				try
				{
					DownloadHandlerExtension.GetTextureContentAsync(www.downloadHandler, (textureResult) =>
					{
						processing = false;
						TextureLoaded = textureResult;
					});
				}
				catch
				{
					processing = false;
				}

				//Wait while async loader process image
				while (processing)
				{
					yield return null;
				}

				//Remove Invalid Unity Image
				if (TextureLoaded != null && TextureLoaded.width <= 8 && TextureLoaded.height <= 8)
				{
					GameObject.Destroy(TextureLoaded);
					TextureLoaded = null;
				}
			}

			try
			{
				AsyncRequestOperation.Sprite = TextureLoaded != null ? Sprite.Create(TextureLoaded, new Rect(0, 0, TextureLoaded.width, TextureLoaded.height), new Vector2(0.5f, 0.5f)) : null;
				AsyncRequestOperation.Texture = TextureLoaded;
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
						if (returnType == TexSerWebReturnTypeEnum.ExternImgFile)
							function.Params.Add(AsyncRequestOperation);
						else if (returnType == TexSerWebReturnTypeEnum.Sprite)
							function.Params.Add(AsyncRequestOperation.Sprite);
						else
							function.Params.Add(AsyncRequestOperation.Texture);
						function.CallFunction();
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

		public static TextureDownloader GetDownloader(string url)
		{
			foreach(TextureDownloader downloader in _downloadersInScene)
			{
				if(downloader != null && !downloader.IsMarkedToDestroy(true) && string.Equals(downloader.Url, url))
				{
					return downloader;
				}
			}
			return null;
		}

		public static bool IsDownloading(string url)
		{
			foreach(TextureDownloader downloader in _downloadersInScene)
			{
				if(downloader != null && !downloader.IsMarkedToDestroy(true) && string.Equals(downloader.Url, url))
				{
					return true;
				}
			}
			return false;
		}

		public static void CancelAllRequestsWithUrl(string url)
		{
			foreach(TextureDownloader downloader in _downloadersInScene)
			{
				if(downloader != null && !downloader.IsMarkedToDestroy(true) && string.Equals(downloader.Url, url))
				{
					downloader.CancelRequest();
				}
			}
		}

		#endregion
	}
}

