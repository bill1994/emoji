using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MaterialUI
{
    public class MaterialTooltip : BaseSpinner<EmptyStyleProperty>
    {
        #region Helper Classes

        [System.Serializable]
        public class DialogAlertAddress : ComponentPrefabAddress<DialogAlert>
        {
            public static explicit operator DialogAlertAddress(string s)
            {
                return new DialogAlertAddress() { AssetPath = s };
            }
        }

        [System.Serializable]
        public class AlertUnityEvent : UnityEvent<DialogAlert> { }

        #endregion

        #region Private Variables

        [SerializeField, TextArea]
        string m_TipText = "";
        [SerializeField]
        VectorImageData m_TipImageData = null;
        [SerializeField]
        DialogAlertAddress m_CustomFramePrefabAddress = null;

        protected DialogAlert _CacheDialogFrame = null;
        protected PrefabAddress _CachedPrefabAdress = null;

        #endregion

        #region Callbacks

        public AlertUnityEvent OnShowTooltipCallback = new AlertUnityEvent();

        #endregion

        #region Public Properties

        public override bool OpenDialogAsync
        {
            get
            {
                if (m_OpenDialogAsync)
                    m_OpenDialogAsync = false;
                return m_OpenDialogAsync;
            }
            set
            {
            }
        }

        /*public override SpinnerUIEventTriggerMode uiShowTriggerMode
        {
            get
            {
                if (m_UIShowTriggerMode != SpinnerUIEventTriggerMode.OnPointerEnter)
                    m_UIShowTriggerMode = SpinnerUIEventTriggerMode.OnPointerEnter;
                return m_UIShowTriggerMode;
            }
            set
            {
            }
        }

        public override SpinnerUIEventTriggerMode uiHideTriggerMode
        {
            get
            {
                if (m_UIHideTriggerMode != SpinnerUIEventTriggerMode.OnPointerExit)
                    m_UIHideTriggerMode = SpinnerUIEventTriggerMode.OnPointerExit;
                return m_UIHideTriggerMode;
            }
            set
            {
            }
        }*/

        public string tipText
        {
            get
            {
                return m_TipText;
            }
            set
            {
                if (m_TipText == value)
                    return;
                m_TipText = value;
            }
        }

        public VectorImageData tipImageData
        {
            get
            {
                return m_TipImageData;
            }
            set
            {
                if (m_TipImageData == value)
                    return;
                m_TipImageData = value;
            }
        }

        protected PrefabAddress cachedPrefabAddress
        {
            get
            {
                if (_CachedPrefabAdress == null)
                    _CachedPrefabAdress = (PrefabAddress)m_CustomFramePrefabAddress;
                return _CachedPrefabAdress;
            }
        }

        public virtual DialogAlertAddress customFramePrefabAddress
        {
            get
            {
                return m_CustomFramePrefabAddress;
            }
            set
            {
                if (m_CustomFramePrefabAddress == value)
                    return;
                ClearCache(false);
                m_CustomFramePrefabAddress = value;
                _CachedPrefabAdress = (PrefabAddress)m_CustomFramePrefabAddress;
            }
        }

        #endregion

        #region Contructors

        public MaterialTooltip()
        {
            m_OpenDialogAsync = false;
            m_UIShowTriggerMode = SpinnerUIEventTriggerMode.OnPointerEnter;
            m_UIHideTriggerMode = SpinnerUIEventTriggerMode.OnPointerExit;
        }

        #endregion

        #region Unity Functions

        protected override void OnDestroy()
        {
            ClearCache(true);
            base.OnDestroy();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ClearCache(false);
            m_OpenDialogAsync = false;
            //m_UIShowTriggerMode = SpinnerUIEventTriggerMode.OnPointerEnter;
            //m_UIHideTriggerMode = SpinnerUIEventTriggerMode.OnPointerExit;
        }
#endif

        #endregion

        #region Overriden Functions

        protected virtual void ClearCache(bool hideIfExpanded)
        {
            if (_CachedPrefabAdress != null)
            {
                _CachedPrefabAdress.ClearCache();
                _CachedPrefabAdress = null;
            }
            if (m_CustomFramePrefabAddress != null)
                m_CustomFramePrefabAddress.ClearCache();
            if (hideIfExpanded && IsExpanded())
            {
                _CacheDialogFrame.activity.destroyOnHide = true;
                _CacheDialogFrame.Hide();
            }
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
        }

        public override bool IsExpanded()
        {
            return (_CacheDialogFrame != null && _CacheDialogFrame.gameObject.activeSelf);
        }

        public override void Show()
        {
            var prefabAddress = cachedPrefabAddress == null || cachedPrefabAddress.IsEmpty() || !cachedPrefabAddress.IsResources() ?
                PrefabManager.ResourcePrefabs.dialogTooltip : cachedPrefabAddress;
            if (prefabAddress == null ||
                (string.IsNullOrEmpty(m_TipText) && (m_TipImageData == null || !m_TipImageData.ContainsData())))
            {
                if (IsExpanded())
                    Hide();
                return;
            }

            ShowFrameActivity(_CacheDialogFrame, prefabAddress, (dialog, isDialog) =>
            {
                _CacheDialogFrame = dialog;
                if (dialog != null)
                {
                    dialog.Initialize("", null, null, m_TipText, new ImageData(m_TipImageData), null, null);

                    if (this != null && OnShowTooltipCallback != null)
                        OnShowTooltipCallback.Invoke(dialog);
                }
            });
        }

        public override void Hide()
        {
            if (_CacheDialogFrame != null && _CacheDialogFrame.gameObject.activeSelf)
                _CacheDialogFrame.Hide();

            HandleOnHide();
        }

        #endregion
    }
}
