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
        public enum AudioUnloadModeEnum { CancelDownload, SoftUnload, FullUnload }

        [System.Serializable]
        public class ClipUnityEvent : UnityEvent<AudioClip> { }

        #region Private Variables

        [SerializeField]
        AudioSource m_audioSourceComponent = null;
        [SerializeField]
        bool m_forceUnloadWhenRefCountEmpty = true;
        [SerializeField]
        AudioUnloadModeEnum m_unloadMode = AudioUnloadModeEnum.FullUnload;
        [Space]
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
                var cachedKey = m_key;
                base.Key = value;
                if (m_key != cachedKey)
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

        public AudioUnloadModeEnum UnloadMode
        {
            get
            {
                return m_unloadMode;
            }
            set
            {
                if (m_unloadMode == value)
                    return;
                m_unloadMode = value;
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

        protected void SetClipToAudioSourceComponent(AudioClip audioClip)
        {
            var oldClip = _audioClip;
            if (m_audioSourceComponent != null)
            {
                m_audioSourceComponent.clip = audioClip;
                _audioClip = audioClip;
            }
            else
                _audioClip = null;

            //Apply Sprite/Texture Changed
            if (oldClip != _audioClip)
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

        protected virtual void HandleOnAudioLoaded(ExternAudioFile audio)
        {
            if ((string.IsNullOrEmpty(Key) && (audio == null || string.IsNullOrEmpty(audio.Url))) ||
                (audio != null && string.Equals(audio.Url, Key)))
            {
                if (AudioSourceComponent != null && !string.IsNullOrEmpty(Key) && audio.Clip != null)
                {
                    Clip = audio.Clip;
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

        protected virtual void CalculateRawImageUVRect(RawImage rawImage)
        {
            var textureSize = rawImage != null && rawImage.texture != null ? new Vector2(rawImage.texture.width, rawImage.texture.height) : new Vector2(0, 0);
            if (textureSize.x == 0 || textureSize.y == 0)
            {
                var normalizedRect = new Rect(0, 0, 1, 1);
                if (rawImage != null)
                    rawImage.uvRect = normalizedRect;
            }
            else
            {
                var localRect = new Rect(Vector2.zero, new Vector2(Mathf.Abs(rawImage.rectTransform.rect.width), Mathf.Abs(rawImage.rectTransform.rect.height)));
                var normalizedRect = new Rect(0, 0, 1, 1);

                if (localRect.width > 0 && localRect.height > 0)
                {
                    var textureProportion = textureSize.x / textureSize.y;
                    var localRectProportion = localRect.width / localRect.height;
                    if (localRectProportion > textureProportion)
                    {
                        var mult = localRect.width > 0 ? textureSize.x / localRect.width : 0;
                        normalizedRect = new Rect(0, 0, 1, (localRect.height * mult) / textureSize.y);
                        normalizedRect.y = Mathf.Max(0, (1 - normalizedRect.height) / 2);
                    }
                    else if (localRectProportion < textureProportion)
                    {
                        var mult = localRect.height > 0 ? textureSize.y / localRect.height : 0;
                        normalizedRect = new Rect(0, 0, (localRect.width * mult) / textureSize.x, 1);
                        normalizedRect.x = Mathf.Max(0, (1 - normalizedRect.width) / 2);
                    }
                }
                rawImage.uvRect = normalizedRect;
            }
        }

        protected override void UnregisterReceiver()
        {
            base.UnregisterReceiver();
            var key = Key;
            if (ForceUnloadWhenRefCountEmpty && ExternalResources.IsUselessResources(key))
            {
                if (m_unloadMode == AudioUnloadModeEnum.CancelDownload)
                {
                    if (ExternalResources.IsDownloading(key))
                    {
                        if (!ExternalResources.IsLoaded(key))
                            ExternalResources.UnloadAsset(key, false, ExternalResources.UnloadMode.SkipDestroyStep);
                        else
                            AudioDownloader.CancelAllRequestsWithUrl(key);
                    }
                }
                else
                {
                    var resourcesUnloadMode = m_unloadMode == AudioUnloadModeEnum.SoftUnload ?
                                                                ExternalResources.UnloadMode.SkipDestroyStep :
                                                                ExternalResources.UnloadMode.DestroyIfNeeded;

                    ExternalResources.UnloadAsset(key, false, resourcesUnloadMode);
                }
            }
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

        protected virtual void ApplyClip(AudioClip audioClip)
        {
            Clip = audioClip;
        }

        public virtual void ApplyEmptyClipInAudioSourceComponent()
        {
            Clip = null;
        }

        public virtual void SetExternAudioFile(ExternAudioFile externAudio)
        {
            if (externAudio != null)
            {
                Key = externAudio.Url;
                Clip = externAudio.Clip;
            }
        }

        protected bool _canUnregisterOnDisable = true;
        protected override void Apply()
        {
            if (AudioSourceComponent != null)
            {
                var isDownloading = ExternalResources.IsDownloading(Key);
                if (string.IsNullOrEmpty(Key) || !isDownloading ||
                    (_canUnregisterOnDisable && isDownloading)) //someone called this request before this external image
                {
                    ExternAudioFile callback = !ExternalResources.IsLoaded(Key) ?
                        ExternalResources.ReloadClipAsync(Key, ApplyClip) : ExternalResources.LoadClipAsync(Key, ApplyClip);
                    if (callback != null)
                    {
                        if (callback.IsProcessing())
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
                            HandleOnAudioLoaded(callback);
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
