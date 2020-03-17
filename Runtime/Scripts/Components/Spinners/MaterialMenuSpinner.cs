using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialUI
{
    public class MaterialMenuSpinner : BaseSpinner<EmptyStyleProperty>
    {
        #region Helper Classes

        [System.Serializable]
        public class MaterialDialogFrameAddress : ComponentPrefabAddress<MaterialDialogFrame>
        {
            public static explicit operator MaterialDialogFrameAddress(string s)
            {
                return new MaterialDialogFrameAddress() { AssetPath = s };
            }
        }

        #endregion

        #region Private Variables

        [SerializeField]
        MaterialDialogFrameAddress m_CustomFramePrefabAddress = null;

        protected MaterialDialogFrame _CacheDialogFrame = null;
        protected PrefabAddress _CachedPrefabAdress = null;

        #endregion

        #region Public Properties

        protected PrefabAddress cachedPrefabAddress
        {
            get
            {
                if (_CachedPrefabAdress == null)
                    _CachedPrefabAdress = (PrefabAddress)m_CustomFramePrefabAddress;
                return _CachedPrefabAdress;
            }
        }

        public virtual MaterialDialogFrameAddress customFramePrefabAddress
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
            var prefabAddress = cachedPrefabAddress;
            if (prefabAddress == null)
            {
                if (IsExpanded())
                    Hide();
                return;
            }

            ShowFrameActivity(_CacheDialogFrame, prefabAddress, (dialog, isDialog) =>
            {
                _CacheDialogFrame = dialog;
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
