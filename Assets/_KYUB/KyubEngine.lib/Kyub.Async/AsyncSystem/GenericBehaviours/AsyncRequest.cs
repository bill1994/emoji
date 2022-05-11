using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub;
using System;
using System.Reflection;
using Kyub.Reflection;

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
			System.Type type = this.GetType();
			gameObject.name = (type != null? this.GetType().Name : "AsyncRequest") + "(Dummy)";
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
            foreach (var func in FunctionsToCallWhenFinish)
            {
                if (func != null)
                {
                    func.Params.Clear();
                    func.Params.Add(AsyncRequestOperation);
                    func.CallFunction();
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
			GameObject go = new GameObject();
			AsyncRequestType requestBehaviour = go.AddComponent<AsyncRequestType>();
			return requestBehaviour;
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

    [System.Serializable]
    public class FunctionAndParams
    {
        #region Private Variables

        [SerializeField]
        object m_target = null;
        [SerializeField]
        System.Type m_functionType = null;
        [SerializeField]
        string m_stringFunctionName = "";
        [SerializeField]
        System.Delegate m_delegatePointer = null;
        [SerializeField]
        List<object> m_params = new List<object>();

        #endregion

        #region Public Properties

        public object Target
        {
            get
            {
                return m_target;
            }
            set
            {
                if (m_target == value)
                    return;
                m_target = value;
            }
        }

        public System.Type FunctionType
        {
            get
            {
                return m_functionType;
            }
            set
            {
                if (m_functionType == value)
                    return;
                m_functionType = value;
            }
        }

        public string StringFunctionName
        {
            get
            {
                return m_stringFunctionName;
            }
            set
            {
                if (m_stringFunctionName == value)
                    return;
                m_stringFunctionName = value;
            }
        }

        public System.Delegate DelegatePointer
        {
            get
            {
                return m_delegatePointer;
            }
            set
            {
                if (m_delegatePointer == value)
                    return;
                m_delegatePointer = value;
            }
        }

        public List<object> Params
        {
            get
            {
                if (m_params == null)
                    m_params = new List<object>();
                return m_params;
            }
            set
            {
                if (m_params == value)
                    return;
                m_params = value;
            }
        }

        #endregion

        #region Helper Methods

        public bool CallFunction()
        {
            if (m_delegatePointer != null)
            {
                return CallDelegateFunction();
            }
            if (string.IsNullOrEmpty(m_stringFunctionName))
            {
                if (m_target != null)
                    return FunctionUtils.CallFunction(m_target, FunctionType, m_stringFunctionName, Params);
                else
                    return FunctionUtils.CallStaticFunction(FunctionType, m_stringFunctionName, Params);
            }
            return false;
        }

        protected bool CallDelegateFunction()
        {
            try
            {
                System.Delegate tempFunctionPointer = DelegatePointer;
                object[] paramsArray = Params.ToArray();
                if (tempFunctionPointer != null)
                {
                    if (Params.Count == 0)
                        tempFunctionPointer.DynamicInvoke(null);
                    else
                        tempFunctionPointer.DynamicInvoke(paramsArray);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public System.Type[] GetFunctionParameterTypes()
        {
            List<System.Type> parameters = new List<System.Type>();
            if (DelegatePointer != null)
            {
                MethodInfo invoke = DelegatePointer.GetType().GetMethod("Invoke");
                if (invoke != null)
                {
                    ParameterInfo[] paramsArray = invoke.GetParameters();
                    foreach (ParameterInfo param in paramsArray)
                    {
                        if (param != null)
                            parameters.Add(param.ParameterType);
                    }
                }
            }
            return parameters.ToArray();
        }

        #endregion
    }

    #endregion
}
