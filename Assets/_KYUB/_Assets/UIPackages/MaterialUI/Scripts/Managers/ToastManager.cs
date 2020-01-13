//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using System.Collections.Generic;

namespace MaterialUI
{
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [AddComponentMenu("MaterialUI/Managers/Toast Manager")]
    public class ToastManager : MonoBehaviour
    {
        #region Singleton

        protected static ToastManager m_Instance;

        protected static ToastManager instance
        {
            get
            {
                if (m_Instance == null)
                {
                    GameObject go = new GameObject();
                    go.name = "ToastManager";

                    m_Instance = go.AddComponent<ToastManager>();
                }

                return m_Instance;
            }
        }

        #endregion

        #region Private Variables

        [SerializeField]
        protected bool m_KeepBetweenScenes = true;
        [Space]
        [SerializeField]
        protected Canvas m_ParentCanvas = null;
		[SerializeField]
        protected int m_MaxQueueSize = int.MaxValue;

        [Header("Default Toasts parameters")]
        [SerializeField]
        protected float m_DefaultDuration = 2f;
        //[SerializeField]
        //protected Color m_DefaultPanelColor = Color.white;
        //[SerializeField]
        //protected Color m_DefaultTextColor = new Color32(74, 74, 74, 255);
        //[SerializeField]
        //protected int m_DefaultFontSize = 14;

        protected Queue<KeyValuePair<Toast, Canvas>> m_ToastQueue;

        protected Dictionary<string, ToastAnimator> _AnimatorsCache = new Dictionary<string, ToastAnimator>();

        protected Toast m_CurrentToast;

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            if (!m_Instance)
            {
                m_Instance = this;

                if (m_KeepBetweenScenes)
                {
                    DontDestroyOnLoad(this);
                }
            }
            else if(m_Instance != this)
            {
                //Debug.LogWarning("More than one ToastManager exist in the scene, destroying one.");
                Destroy(gameObject);
                return;
            }

            InitSystem();
        }

        protected virtual void OnDestroy()
        {
            if (m_Instance == this)
                m_Instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            if (m_Instance == this)
                m_Instance = null;
		}

        #endregion

        #region Internal Helper Functions (Instance)

        protected virtual void InitSystem()
        {
            SetCanvas(null);
            //m_CurrentAnimator = InstantiateToastAnimator();
            m_ToastQueue = new Queue<KeyValuePair<Toast, Canvas>>();
        }

        protected ToastAnimator InstantiateSnackbarAnimator(string assetPath, out string cacheKey)
        {
            var pair = InstantiateAnimator_Internal(assetPath, PrefabManager.ResourcePrefabs.snackbar);

            cacheKey = pair.Key;
            return pair.Value;
        }

        protected ToastAnimator InstantiateToastAnimator(string assetPath, out string cacheKey)
        {
            var pair = InstantiateAnimator_Internal(assetPath, PrefabManager.ResourcePrefabs.toast);

            cacheKey = pair.Key;
            return pair.Value;
        }

        private KeyValuePair<string, ToastAnimator> InstantiateAnimator_Internal(string assetPath, PrefabAddress defaultAdress)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = defaultAdress != null ? defaultAdress.Name : null;
            }

            ToastAnimator currentAnimator = null;
            _AnimatorsCache.TryGetValue(assetPath, out currentAnimator);
            if (currentAnimator == null)
            {
                var instance = PrefabManager.InstantiateGameObject(assetPath, transform);
                if (instance == null)
                    instance = PrefabManager.InstantiateGameObject(defaultAdress != null ? defaultAdress.Name : null, transform);

                currentAnimator = instance.GetComponent<ToastAnimator>();

                if (!string.IsNullOrEmpty(assetPath))
                    _AnimatorsCache[assetPath] = currentAnimator;
            }

            return new KeyValuePair<string,ToastAnimator>(assetPath, currentAnimator);
        }

        protected void StartQueue()
        {
            if (m_ToastQueue.Count <= 0 || m_CurrentToast != null) return;

            if (m_ToastQueue.Count > 0)
            {
                KeyValuePair<Toast, Canvas> pair = m_ToastQueue.Dequeue();
                m_CurrentToast = pair.Key;
                SetCanvas(pair.Value);

                var cacheKey = "";
                var assetPath = m_CurrentToast != null ? m_CurrentToast.assetPath : "";
                var currentAnimator = m_CurrentToast is Snackbar? 
                    InstantiateSnackbarAnimator(assetPath, out cacheKey) : 
                    InstantiateToastAnimator(assetPath, out cacheKey);

                if (currentAnimator != null)
                {
                    //Dont cache custom toasts
                    if (m_CurrentToast != null && m_CurrentToast.IsCustomToast())
                        _AnimatorsCache.Remove(cacheKey);

                    currentAnimator.Show(pair.Key, pair.Value, OnToastCompleted);
                }
                else
                    m_CurrentToast = null;
            }
        }

        protected void SetCanvas(Canvas canvas)
        {
            if (canvas != null)
            {
                m_ParentCanvas = canvas;
            }

            if (m_ParentCanvas == null)
            {
                m_ParentCanvas = GetComponentInParent<Canvas>();
                if (m_ParentCanvas == null)
                {
                    //Find canvas in scene
                    var canvasArray = FindObjectsOfType<Canvas>();
                    foreach (var canvasMember in canvasArray)
                    {
                        if (canvasMember != null && 
                            canvasMember.gameObject.scene.IsValid() && 
                            canvasMember.enabled && 
                            canvasMember.gameObject.activeInHierarchy)
                        {
                            m_ParentCanvas = canvasMember;
                            break;
                        }
                    }
                }

                if (m_ParentCanvas != null)
                    m_ParentCanvas = m_ParentCanvas.rootCanvas;
            }

            if (m_ParentCanvas != null)
            {
                CanvasSafeArea safeArea = m_ParentCanvas.GetComponent<CanvasSafeArea>();
                transform.SetParent(safeArea != null && safeArea.Content != null ? safeArea.Content : m_ParentCanvas.transform, false);
            }

            transform.localPosition = Vector3.zero;
        }

        #endregion

        #region Public Functions (Static)

        public static void Show(string content, Transform canvasHierarchy = null, string customAssetPath = null)
		{
			Show(content, instance.m_DefaultDuration, null, null, null, canvasHierarchy, customAssetPath);
		}

		public static void Show(string content, float duration, Transform canvasHierarchy = null, string customAssetPath = null)
		{
			Show(content, duration, null, null, null, canvasHierarchy, customAssetPath);
		}

        public static void Show(string content, float duration, Color? panelColor, Color? textColor, int? fontSize, Transform canvasHierarchy = null, string customAssetPath = null)
        {
			int toastTotalCount = instance.m_ToastQueue.Count + (instance.m_CurrentToast != null ? 1 : 0);
			if (toastTotalCount >= instance.m_MaxQueueSize)
			{
				return;
			}

            Canvas canvas = null;
            if (canvasHierarchy != null)
            {
                canvas = canvasHierarchy.GetRootCanvas();
                if (canvas != null)
                {
                    instance.m_ParentCanvas = canvas;
                }
            }

            instance.m_ToastQueue.Enqueue(new KeyValuePair<Toast, Canvas>(new Toast(content, duration, panelColor, textColor, fontSize, customAssetPath), canvas));
            instance.StartQueue();
        }

        public static void ShowSnackbar(string content, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            ShowSnackbar(content, "Ok", null, canvasHierarchy, customAssetPath);
        }

        public static void ShowSnackbar(string content, string actionName, System.Action onActionButtonClicked, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            ShowSnackbar(content, instance.m_DefaultDuration, null, null, null, actionName, onActionButtonClicked, canvasHierarchy, customAssetPath);
        }

        public static void ShowSnackbar(string content, float duration, string actionName, System.Action onActionButtonClicked, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            ShowSnackbar(content, duration, null, null, null, actionName,  onActionButtonClicked, canvasHierarchy, customAssetPath);
        }

        public static void ShowSnackbar(string content, float duration, Color? panelColor, Color? textColor, int? fontSize, string actionName, System.Action onActionButtonClicked, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            int toastTotalCount = instance.m_ToastQueue.Count + (m_Instance.m_CurrentToast != null ? 1 : 0);
            if (toastTotalCount >= instance.m_MaxQueueSize)
            {
                return;
            }

            Canvas canvas = null;
            if (canvasHierarchy != null)
            {
                canvas = canvasHierarchy.GetRootCanvas();
                if (canvas != null)
                {
                    instance.m_ParentCanvas = canvas;
                }
            }

            instance.m_ToastQueue.Enqueue(new KeyValuePair<Toast, Canvas>(new Snackbar(content, duration, panelColor, textColor, fontSize, actionName, onActionButtonClicked, customAssetPath), canvas));
            instance.StartQueue();
        }

        public static bool IsRunning(string content)
        {
            if (instance.m_CurrentToast != null && instance.m_CurrentToast.content == content)
                return true;
            else
            {
                foreach (var pair in instance.m_ToastQueue)
                {
                    if (pair.Key != null && pair.Key.content == content)
                        return true;
                }
            }
            return false;
        }

        public static bool IsRunningAsSnackbar(string content)
        {
            if (instance.m_CurrentToast is Snackbar && instance.m_CurrentToast.content == content)
                return true;
            else
            {
                foreach (var pair in instance.m_ToastQueue)
                {
                    if (pair.Key is Snackbar && pair.Key.content == content)
                        return true;
                }
            }
            return false;
        }

        #endregion

        #region Receivers

        protected internal static bool OnToastCompleted(Toast toast, ToastAnimator toastAnimator)
        {
            var isCustomToast = toast == null || toast.IsCustomToast();
            var finalize = isCustomToast || instance.m_ToastQueue.Count <= 0;

            //Force Destory Custom Toasts
            if (toastAnimator != null && !toastAnimator.CanDestroyToast && isCustomToast)
                toastAnimator.CanDestroyToast = true;

            instance.m_CurrentToast = null;
            instance.StartQueue();

            return finalize;
        }

        #endregion
    }
}