using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub;
using System;

namespace Kyub.Async
{
	public abstract class AsyncRequest<AsyncType> : MonoBehaviour where AsyncType: AsyncRequestOperation, new()
	{
		#region Private Variable
		
		[SerializeField]
		AsyncType m_asyncCallback = null;
		[SerializeField]
		List<FunctionAndParams> m_functionsToCallWhenFinish = new List<FunctionAndParams>();

		bool _started = false;

		#endregion
		
		#region Public Properties

		public List<FunctionAndParams> FunctionsToCallWhenFinish
		{
			get
			{
				if(m_functionsToCallWhenFinish == null)
					m_functionsToCallWhenFinish = new List<FunctionAndParams>();
				return m_functionsToCallWhenFinish;
			}
			set
			{
				if(m_functionsToCallWhenFinish == value)
					return;
				m_functionsToCallWhenFinish = value;
			}
		}
		
		public AsyncType AsyncRequestOperation
		{
			get
			{
				if(m_asyncCallback == null)
					m_asyncCallback = new AsyncType();
				return m_asyncCallback;
			}
			set
			{
				if(m_asyncCallback == value)
					return;
				m_asyncCallback = value;
			}
		}
		
		#endregion

		#region Unity Functions
		
		protected virtual void Awake()
		{
			gameObject.hideFlags = HideFlags.DontSaveInBuild|HideFlags.DontSaveInBuild;
			System.Type v_type = this.GetType();
			gameObject.name = (v_type != null? this.GetType().Name : "AsyncRequest") + "(Dummy)";
		}
		
		protected virtual void OnEnable()
		{
			if(_started)
				StartRequest();
		}
		
		protected virtual void Start()
		{
			_started = true;
			StartRequest();
		}
		
		protected virtual void OnDisable()
		{
			CancelRequest();
            UnregisterEvents();
		}
		
		#endregion

		#region Helper Functions

		public virtual void CancelRequest()
		{
            //var status = AsyncRequestOperation.Status;
            if (AsyncRequestOperation.Status != AsyncStatusEnum.Done)
            {
                AsyncRequestOperation.Status = AsyncStatusEnum.Cancelled;
                RequestStackManager.StopAllRequestsFromSender(this);
            }
            DestroyUtils.Destroy(this.gameObject);
		}

		public virtual void StartRequest()
		{
            if (AsyncRequestOperation.Status != AsyncStatusEnum.Processing)
            {
                var enumerator = ProcessRequestInternal();
                RequestStackManager.RequestRoutine(this, enumerator);
                AsyncRequestOperation.Status = RequestStackManager.IsRequesting(enumerator) ? AsyncStatusEnum.Processing : AsyncStatusEnum.Cancelled;

                if (AsyncRequestOperation.Status == AsyncStatusEnum.Processing)
                {
                    MarkedToDestroy.RemoveMark(this.gameObject);
                    RegisterEvents();
                }
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();

            RequestStackManager.OnCancelRequest += HandleOnCancelRequest;
            RequestStackManager.OnRequestExecutionFinish += HandleOnRequestExecutionFinish;
        }

        protected virtual void UnregisterEvents()
        {
            RequestStackManager.OnCancelRequest -= HandleOnCancelRequest;
            RequestStackManager.OnRequestExecutionFinish -= HandleOnRequestExecutionFinish;
        }

		/// <summary>
		/// Use this function to implement your own request logic
		/// </summary>
		protected virtual IEnumerator ProcessRequest()
		{
			yield return null;
		}

        private IEnumerator ProcessRequestInternal()
        {
            MarkedToDestroy.RemoveMark(this.gameObject);
            AsyncRequestOperation.Status = AsyncStatusEnum.Processing;
            yield return ProcessRequest();
            AsyncRequestOperation.Status = AsyncStatusEnum.Done;
            foreach (var v_func in FunctionsToCallWhenFinish)
            {
                if (v_func != null)
                {
                    v_func.Params.Clear();
                    v_func.Params.Add(AsyncRequestOperation);
                    v_func.CallFunction();
                }
            }
            DestroyUtils.Destroy(this.gameObject);
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnRequestExecutionFinish(MonoBehaviour sender, IEnumerator routine, string hash)
        {
            if (sender == this)
            {
                UnregisterEvents();
                if(AsyncRequestOperation.Status == AsyncStatusEnum.Processing)
                    DestroyUtils.Destroy(this);
            }
        }

        protected virtual void HandleOnCancelRequest(MonoBehaviour sender, IEnumerator routine, string hash)
        {
            if (sender == this)
            {
                UnregisterEvents();
                if (AsyncRequestOperation.Status == AsyncStatusEnum.Processing)
                    DestroyUtils.Destroy(this);
            }
        }

        #endregion

        #region Static Function

        /// <summary>
        /// Instantiate new object of request operation type
        /// </summary>
        protected static AsyncRequestType RequestOperationInternal<AsyncRequestType, AsyncCallbackType>() 
			where AsyncRequestType : AsyncRequest<AsyncCallbackType>
			where AsyncCallbackType : AsyncRequestOperation , new()
		{
			GameObject v_object = new GameObject();
			AsyncRequestType v_requestBehaviour = v_object.AddComponent<AsyncRequestType>();
			return v_requestBehaviour;
		}

		#endregion
	}

	#region Helper Classes

	public enum AsyncStatusEnum {Cancelled, Processing, Done}

	[System.Serializable]
	public class AsyncRequestOperation
	{
		#region Private Variable

		[SerializeField]
		AsyncStatusEnum m_status = AsyncStatusEnum.Cancelled;
		[SerializeField]
		string m_error = null;

		#endregion

		#region Public Properties

		public AsyncStatusEnum Status
		{
			get
			{
				return m_status;
			}
			set
			{
				if(m_status == value)
					return;
				m_status = value;
			}
		}

		public string Error
		{
			get
			{
				return m_error;
			}
			set
			{
				if(m_error == value)
					return;
				m_error = value;
			}
		}

		#endregion

		#region Helper Functions

		public bool IsProcessing()
		{
			return Status == AsyncStatusEnum.Processing;
		}

		#endregion
	}

	#endregion
}
