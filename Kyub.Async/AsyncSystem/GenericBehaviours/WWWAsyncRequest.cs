using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Kyub.Async
{
	public class WWWAsyncRequest<AsyncType> : AsyncRequest<AsyncType> where AsyncType: AsyncRequestOperation, new()
	{
		#region Private Variables

		[SerializeField]
		string m_url = "";
		
		#endregion
		
		#region Public Properties
		
		public string Url
		{
			get
			{
				return m_url;
			}
			set
			{
				if(m_url == value)
					return;
				m_url = value;
			}
		}
		
		#endregion
		
		#region Helper Functions
		
		protected override IEnumerator ProcessRequest()
		{
			if(Url != null)
			{
				//MarkedToDestroy.RemoveMark(this.gameObject);
                // Start a download of the given URL
                using (UnityWebRequest www = UnityWebRequest.Get(Url))
                {
                    www.timeout = RequestStackManager.RequestTimeLimit;
                    yield return www.SendWebRequest();

                    ProcessWWWReturn(www);
                }
			}
		}

		/// <summary>
		/// Override this Object for Simple Process Request Logic
		/// </summary>
        protected virtual void ProcessWWWReturn(UnityWebRequest www)
        {
            if (www == null || www.isNetworkError || www.isHttpError || !string.IsNullOrEmpty(www.error))
                Debug.Log("Request Failed: " + www.error + " Url: " + Url);
        }

        #endregion

        #region Receivers

        protected override void HandleOnCancelRequest(MonoBehaviour sender, IEnumerator routine, string hash)
        {
            if(sender == this)
            {
                UnregisterEvents();
                if (AsyncRequestOperation.Status == AsyncStatusEnum.Processing)
                {
                    ProcessWWWReturn(null);
                    DestroyUtils.Destroy(this);
                }
                AsyncRequestOperation.Status = AsyncStatusEnum.Cancelled;
            }
        }

        #endregion
    }
}

