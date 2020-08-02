//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using System.Collections.Generic;

namespace MaterialUI
{
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [AddComponentMenu("MaterialUI/Managers/Toast Manager")]
    public class ToastManager : Kyub.Singleton<ToastManager>
    {
        #region Private Variables

        [SerializeField]
        protected bool m_KeepBetweenScenes = true;
        [Space]
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

        protected Queue<KeyValuePair<Toast, Canvas>> m_ToastQueue = new Queue<KeyValuePair<Toast, Canvas>>();

        protected Dictionary<string, ToastAnimator> _AnimatorsCache = new Dictionary<string, ToastAnimator>();

        protected Toast m_CurrentToast;

        #endregion

        #region Internal Helper Functions (Instance)

        protected ToastAnimator InstantiateSnackbarAnimator(string assetPath, out string cacheKey, Transform parent)
        {
            var pair = InstantiateAnimator_Internal(assetPath, PrefabManager.ResourcePrefabs.snackbar, parent);

            cacheKey = pair.Key;
            return pair.Value;
        }

        protected ToastAnimator InstantiateToastAnimator(string assetPath, out string cacheKey, Transform parent)
        {
            var pair = InstantiateAnimator_Internal(assetPath, PrefabManager.ResourcePrefabs.toast, parent);

            cacheKey = pair.Key;
            return pair.Value;
        }

        private KeyValuePair<string, ToastAnimator> InstantiateAnimator_Internal(string assetPath, PrefabAddress defaultAdress, Transform parent)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = defaultAdress != null ? defaultAdress.Name : null;
            }

            ToastAnimator currentAnimator = null;
            _AnimatorsCache.TryGetValue(assetPath, out currentAnimator);
            if (currentAnimator == null)
            {
                var Instance = PrefabManager.InstantiateGameObject(assetPath, transform ,false);
                if (Instance == null)
                    Instance = PrefabManager.InstantiateGameObject(defaultAdress != null ? defaultAdress.Name : null, transform, false);

                currentAnimator = Instance.GetComponent<ToastAnimator>();

                if (!string.IsNullOrEmpty(assetPath))
                    _AnimatorsCache[assetPath] = currentAnimator;
            }

            if (currentAnimator != null)
            {
                currentAnimator.transform.SetParent(parent);
                currentAnimator.transform.localScale = Vector3.one;
                currentAnimator.transform.localRotation = Quaternion.identity;
                currentAnimator.transform.SetAsLastSibling();
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

                Transform parent = null;
                if (pair.Value != null)
                {
                    CanvasSafeArea safeArea = pair.Value.GetComponent<CanvasSafeArea>();
                    parent = safeArea != null && safeArea.Content != null ? safeArea.Content : pair.Value.transform;
                }
                if (parent == null)
                    parent = DialogManager.rectTransform;

                var cacheKey = "";
                var assetPath = m_CurrentToast != null ? m_CurrentToast.assetPath : "";
                var currentAnimator = m_CurrentToast is Snackbar? 
                    InstantiateSnackbarAnimator(assetPath, out cacheKey, parent) : 
                    InstantiateToastAnimator(assetPath, out cacheKey, parent);

                if (currentAnimator != null)
                {
                    //Dont cache custom toasts
                    if (m_CurrentToast != null && m_CurrentToast.IsCustomToast())
                        _AnimatorsCache.Remove(cacheKey);

                    currentAnimator.Show(pair.Key, parent as RectTransform, OnToastCompleted);
                }
                else
                    m_CurrentToast = null;
            }
        }

        #endregion

        #region Public Functions (Static)

        public static void Show(string content, Transform canvasHierarchy = null, string customAssetPath = null)
		{
			Show(content, Instance.m_DefaultDuration, null, null, null, canvasHierarchy, customAssetPath);
		}

		public static void Show(string content, float duration, Transform canvasHierarchy = null, string customAssetPath = null)
		{
			Show(content, duration, null, null, null, canvasHierarchy, customAssetPath);
		}

        public static void Show(string content, float duration, Color? panelColor, Color? textColor, int? fontSize, Transform canvasHierarchy = null, string customAssetPath = null)
        {
			int toastTotalCount = Instance.m_ToastQueue.Count + (Instance.m_CurrentToast != null ? 1 : 0);
			if (toastTotalCount >= Instance.m_MaxQueueSize)
			{
				return;
			}

            Canvas canvas = null;
            if (canvasHierarchy != null)
                canvas = canvasHierarchy.GetRootCanvas();
            else
                canvas = DialogManager.parentCanvas;

            Instance.m_ToastQueue.Enqueue(new KeyValuePair<Toast, Canvas>(new Toast(content, duration, panelColor, textColor, fontSize, customAssetPath), canvas));
            Instance.StartQueue();
        }

        public static void ShowSnackbar(string content, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            ShowSnackbar(content, "Ok", null, canvasHierarchy, customAssetPath);
        }

        public static void ShowSnackbar(string content, string actionName, System.Action onActionButtonClicked, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            ShowSnackbar(content, Instance.m_DefaultDuration, null, null, null, actionName, onActionButtonClicked, canvasHierarchy, customAssetPath);
        }

        public static void ShowSnackbar(string content, float duration, string actionName, System.Action onActionButtonClicked, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            ShowSnackbar(content, duration, null, null, null, actionName,  onActionButtonClicked, canvasHierarchy, customAssetPath);
        }

        public static void ShowSnackbar(string content, float duration, Color? panelColor, Color? textColor, int? fontSize, string actionName, System.Action onActionButtonClicked, Transform canvasHierarchy = null, string customAssetPath = null)
        {
            int toastTotalCount = Instance.m_ToastQueue.Count + (s_instance.m_CurrentToast != null ? 1 : 0);
            if (toastTotalCount >= Instance.m_MaxQueueSize)
            {
                return;
            }

            Canvas canvas = null;
            if (canvasHierarchy != null)
                canvas = canvasHierarchy.GetRootCanvas();
            else
                canvas = DialogManager.parentCanvas;

            Instance.m_ToastQueue.Enqueue(new KeyValuePair<Toast, Canvas>(new Snackbar(content, duration, panelColor, textColor, fontSize, actionName, onActionButtonClicked, customAssetPath), canvas));
            Instance.StartQueue();
        }

        public static bool IsRunning(string content)
        {
            if (Instance.m_CurrentToast != null && Instance.m_CurrentToast.content == content)
                return true;
            else
            {
                foreach (var pair in Instance.m_ToastQueue)
                {
                    if (pair.Key != null && pair.Key.content == content)
                        return true;
                }
            }
            return false;
        }

        public static bool IsRunningAsSnackbar(string content)
        {
            if (Instance.m_CurrentToast is Snackbar && Instance.m_CurrentToast.content == content)
                return true;
            else
            {
                foreach (var pair in Instance.m_ToastQueue)
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
            var finalize = isCustomToast || Instance.m_ToastQueue.Count <= 0;

            //Force Destory Custom Toasts
            if (toastAnimator != null && !toastAnimator.CanDestroyToast && isCustomToast)
                toastAnimator.CanDestroyToast = true;

            Instance.m_CurrentToast = null;
            Instance.StartQueue();

            return finalize;
        }

        #endregion
    }
}