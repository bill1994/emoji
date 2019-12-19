using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using Kyub.Async;

namespace Kyub.UI
{
    public class ExternalAudio : ExternalResourcesReceiver
    {
        [System.Serializable]
        public class ClipUnityEvent : UnityEvent<AudioClip> { }

        #region Private Variables

        [SerializeField]
        AudioSource m_audioSourceComponent = null;
        [SerializeField]
        bool m_forceUnloadWhenRefCountEmpty = true;
        [SerializeField]
        bool m_unregisterOnDisable = true;

        //aux
        AudioClip _audioClip = null;

        #endregion

        #region Callbacks

        public UnityEvent OnRequestDownloadClipCallback;
        public UnityEvent OnApplyClipCallback;
        public ClipUnityEvent OnClipChangedCallback;

        #endregion

        #region Public Properties

        public override string Key
        {
            get
            {
                return base.Key;
            }
            set
            {
                var v_cachedKey = m_key;
                base.Key = value;
                if (m_key != v_cachedKey)
                    _canUnregisterOnDisable = true;
            }
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public AudioClip Clip
        {
            get
            {
                return GetClipFromAudioSourceComponent();
            }
            set
            {
                SetClipToAudioSourceComponent(value);
            }
        }

        public bool ForceUnloadWhenRefCountEmpty
        {
            get
            {
                return m_forceUnloadWhenRefCountEmpty;
            }
            set
            {
                if (m_forceUnloadWhenRefCountEmpty == value)
                    return;
                m_forceUnloadWhenRefCountEmpty = value;
            }
        }

        public AudioSource AudioSourceComponent
        {
            get
            {
                if (m_audioSourceComponent == null)
                {
                    m_audioSourceComponent = GetComponent<AudioSource>();
                    _audioClip = GetClipFromAudioSourceComponent();
                }
                return m_audioSourceComponent;
            }
            set
            {
                if (m_audioSourceComponent == value)
                    return;
                m_audioSourceComponent = value;
                _audioClip = GetClipFromAudioSourceComponent();
            }
        }

        #endregion

        #region Internal Helper Functions

        protected AudioClip GetClipFromAudioSourceComponent()
        {
            if (m_audioSourceComponent != null)
            {
                _audioClip = m_audioSourceComponent.clip;
            }
            else
                _audioClip = null;
            return _audioClip;
        }

        protected void SetClipToAudioSourceComponent(AudioClip p_audioClip)
        {
            var v_oldClip = _audioClip;
            if (m_audioSourceComponent != null)
            {
                m_audioSourceComponent.clip = p_audioClip;
                _audioClip = p_audioClip;
            }
            else
                _audioClip = null;

            //Apply Sprite/Texture Changed
            if (v_oldClip != _audioClip)
            {
                if (OnClipChangedCallback != null)
                    OnClipChangedCallback.Invoke(_audioClip);
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            RegisterReceiver();
            RegisterEvents();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (m_unregisterOnDisable)
            {
                RegisterReceiver(); //Force Update Receiver Ref Counter (Prevent bugs when changing key via inspector)
                if (_canUnregisterOnDisable)
                    UnregisterEvents();
            }
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            UnregisterEvents();

            base.OnDestroy();
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnAudioLoaded(ExternAudioFile p_audio)
        {
            if ((string.IsNullOrEmpty(Key) && (p_audio == null || string.IsNullOrEmpty(p_audio.Url))) ||
                (p_audio != null && string.Equals(p_audio.Url, Key)))
            {
                if (AudioSourceComponent != null && !string.IsNullOrEmpty(Key) && p_audio.Clip != null)
                {
                    Clip = p_audio.Clip;
                }
                if (OnApplyClipCallback != null)
                    OnApplyClipCallback.Invoke();
                if (!_canUnregisterOnDisable)
                {
                    _canUnregisterOnDisable = true;
                    if (!gameObject.activeInHierarchy || !gameObject.activeSelf || !enabled)
                        UnregisterEvents();
                }
                _isDirty = false;
            }
        }

        #endregion

        #region Helper Functions

        protected virtual void CalculateRawImageUVRect(RawImage p_rawImage)
        {
            var v_textureSize = p_rawImage != null && p_rawImage.texture != null ? new Vector2(p_rawImage.texture.width, p_rawImage.texture.height) : new Vector2(0, 0);
            if (v_textureSize.x == 0 || v_textureSize.y == 0)
            {
                var v_normalizedRect = new Rect(0, 0, 1, 1);
                if (p_rawImage != null)
                    p_rawImage.uvRect = v_normalizedRect;
            }
            else
            {
                var v_localRect = new Rect(Vector2.zero, new Vector2(Mathf.Abs(p_rawImage.rectTransform.rect.width), Mathf.Abs(p_rawImage.rectTransform.rect.height)));
                var v_normalizedRect = new Rect(0, 0, 1, 1);

                if (v_localRect.width > 0 && v_localRect.height > 0)
                {
                    var v_textureProportion = v_textureSize.x / v_textureSize.y;
                    var v_localRectProportion = v_localRect.width / v_localRect.height;
                    if (v_localRectProportion > v_textureProportion)
                    {
                        var v_mult = v_localRect.width > 0 ? v_textureSize.x / v_localRect.width : 0;
                        v_normalizedRect = new Rect(0, 0, 1, (v_localRect.height * v_mult) / v_textureSize.y);
                        v_normalizedRect.y = Mathf.Max(0, (1 - v_normalizedRect.height) / 2);
                    }
                    else if (v_localRectProportion < v_textureProportion)
                    {
                        var v_mult = v_localRect.height > 0 ? v_textureSize.y / v_localRect.height : 0;
                        v_normalizedRect = new Rect(0, 0, (v_localRect.width * v_mult) / v_textureSize.x, 1);
                        v_normalizedRect.x = Mathf.Max(0, (1 - v_normalizedRect.width) / 2);
                    }
                }
                p_rawImage.uvRect = v_normalizedRect;
            }
        }

        protected override void UnregisterReceiver()
        {
            base.UnregisterReceiver();
            if (ForceUnloadWhenRefCountEmpty && ExternalResources.IsUselessResources(Key))
                ExternalResources.UnloadAsset(Key);
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            ExternalResources.OnAudioLoaded += HandleOnAudioLoaded;
        }

        protected virtual void UnregisterEvents()
        {
            ExternalResources.OnAudioLoaded -= HandleOnAudioLoaded;
        }

        protected virtual void ApplyClip(AudioClip p_audioClip)
        {
            Clip = p_audioClip;
        }

        public virtual void ApplyEmptyClipInAudioSourceComponent()
        {
            Clip = null;
        }

        public virtual void SetExternAudioFile(ExternAudioFile p_externAudio)
        {
            if (p_externAudio != null)
            {
                Key = p_externAudio.Url;
                Clip = p_externAudio.Clip;
            }
        }

        protected bool _canUnregisterOnDisable = true;
        protected override void Apply()
        {
            if (AudioSourceComponent != null)
            {
                var v_isDownloading = ExternalResources.IsDownloading(Key);
                if (string.IsNullOrEmpty(Key) || !v_isDownloading ||
                    (_canUnregisterOnDisable && v_isDownloading)) //someone called this request before this external image
                {
                    ExternAudioFile v_callback = !ExternalResources.IsLoaded(Key) ?
                        ExternalResources.ReloadClipAsync(Key, ApplyClip) : ExternalResources.LoadClipAsync(Key, ApplyClip);
                    if (v_callback != null)
                    {
                        if (v_callback.IsProcessing())
                        {
                            //Reset image
                            if (Clip != null)
                                ApplyEmptyClipInAudioSourceComponent();

                            _canUnregisterOnDisable = false;
                            if (OnRequestDownloadClipCallback != null)
                                OnRequestDownloadClipCallback.Invoke();
                        }
                        else
                        {
                            HandleOnAudioLoaded(v_callback);
                        }
                    }
                }
                else
                {
                    if (Clip != null)
                        ApplyEmptyClipInAudioSourceComponent();
                    SetDirty();
                }
            }
        }

        #endregion
    }
}
