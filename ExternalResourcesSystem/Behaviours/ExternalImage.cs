using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using Kyub.Async;

namespace Kyub.UI
{
    public class ExternalImage : ExternalResourcesReceiver
    {
        public enum ImageUnloadModeEnum { CancelDownload, SoftUnload, FullUnload }

        [System.Serializable]
        public class SpriteUnityEvent : UnityEvent<Sprite> {}

        [System.Serializable]
        public class TextureUnityEvent : UnityEvent<Texture2D> { }

        #region Private Variables

        [SerializeField]
		Sprite m_defaultSprite = null;
		[SerializeField]
		MaskableGraphic m_imageComponent = null;
		[SerializeField]
		bool m_forceUnloadWhenRefCountEmpty = true;
        [SerializeField]
        ImageUnloadModeEnum m_unloadMode = ImageUnloadModeEnum.SoftUnload;
        [Space]
        [SerializeField]
        bool m_unregisterOnDisable = true;

        //aux
        Sprite _sprite = null;
		
		#endregion

		#region Callbacks

		public UnityEvent OnRequestDownloadImageCallback;
		public UnityEvent OnApplyImageCallback;
        public SpriteUnityEvent OnSpriteChangedCallback;
        public TextureUnityEvent OnTextureChangedCallback;

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
                if(m_key != cachedKey)
                    _canUnregisterOnDisable = true;
            }
        }

        public Sprite DefaultSprite
		{
			get
			{
				return m_defaultSprite;
			}
			set
			{
				if(m_defaultSprite == value)
					return;
                var sprite = Sprite;
                var needReapply = sprite == m_defaultSprite || sprite == null;
                m_defaultSprite = value;
                if (needReapply)
                    Sprite = m_defaultSprite;
            }
		}

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Sprite Sprite
		{
			get
			{
				return GetSpriteFromImageComponent();
			}
			set
			{
				SetSpriteToImageComponent(value);
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
				if(m_forceUnloadWhenRefCountEmpty == value)
					return;
				m_forceUnloadWhenRefCountEmpty = value;
			}
		}

        public ImageUnloadModeEnum UnloadMode
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

        public MaskableGraphic ImageComponent
		{
			get
			{
                if (m_imageComponent == null)
                {
                    m_imageComponent = GetComponent<MaskableGraphic>();
                    _sprite = GetSpriteFromImageComponent();
                }
				return m_imageComponent;
			}
			set
			{
				if(m_imageComponent == value)
					return;
				m_imageComponent = value;
				_sprite = GetSpriteFromImageComponent();
			}
		}

        #endregion

        #region Internal Helper Functions

        protected Sprite GetSpriteFromImageComponent()
		{
			if(m_imageComponent != null)
			{
				Image image = m_imageComponent as Image;
				if(image != null)
				{
					_sprite = image.sprite;
				}
				else
				{
					RawImage rawImage = m_imageComponent as RawImage;
					if(rawImage != null)
					{
						Texture2D texture = _sprite != null? _sprite.texture : null;
						if(rawImage.texture != texture)
							_sprite = texture != null? Sprite.Create(texture, new Rect(0,0, texture.width, texture.height), new Vector2(0.5f, 0.5f)) : null;
					}
					else
					{
						m_imageComponent = null;
						_sprite = null;
					}
				}
			}
			else
				_sprite = null;
			return _sprite;
		}

		protected void SetSpriteToImageComponent(Sprite sprite)
		{
            var oldSprite = _sprite;
			if(m_imageComponent != null)
			{
				Image image = m_imageComponent as Image;
				if(image != null)
				{
					image.sprite = sprite;
					_sprite = sprite;
				}
				else
				{
					RawImage rawImage = m_imageComponent as RawImage;
					if(rawImage != null)
					{
						Texture2D texture = sprite != null? sprite.texture : null;
						rawImage.texture = texture;
                        CalculateRawImageUVRect(rawImage);
                        _sprite = sprite;
					}
					else
					{
						m_imageComponent = null;
						_sprite = null;
					}
				}
			}
			else
				_sprite = null;

            //Apply Sprite/Texture Changed
            if (oldSprite != _sprite)
            {
                if (OnTextureChangedCallback != null)
                    OnTextureChangedCallback.Invoke(_sprite != null ? _sprite.texture : null);
                if (OnSpriteChangedCallback != null)
                    OnSpriteChangedCallback.Invoke(_sprite);
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
            MarkToUnloadAssets();

            base.OnDestroy();
		}

        protected virtual void OnRectTransformDimensionsChange()
        {
            RawImage rawImage = m_imageComponent as RawImage;
            if (rawImage != null)
                CalculateRawImageUVRect(rawImage);
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnImageLoaded (ExternImgFile image)
		{
            if ((string.IsNullOrEmpty(Key) && (image == null || string.IsNullOrEmpty(image.Url))) ||
                (image != null && string.Equals(image.Url, Key)))
            {
                if (ImageComponent != null && !string.IsNullOrEmpty(Key) && (image.Sprite != null || string.IsNullOrEmpty(image.Error)))
                {
                    Sprite = image.Sprite;
                }
                if (Sprite == null || !string.IsNullOrEmpty(image.Error))
                {
                    ApplyDefaultSpriteInImageComponent();
                }
                if (OnApplyImageCallback != null)
                    OnApplyImageCallback.Invoke();
                if (!_canUnregisterOnDisable)
                {
                    _canUnregisterOnDisable = true;
                    if (!gameObject.activeInHierarchy || !gameObject.activeSelf || !enabled)
                        UnregisterEvents();
                }
                _isDirty = false;
            }
            else if(Sprite == null && DefaultSprite != null)
                ApplyDefaultSpriteInImageComponent();
        }

        #endregion

        #region Helper Functions

        protected virtual void CalculateRawImageUVRect(RawImage rawImage)
        {
            var textureSize = rawImage != null && rawImage.texture != null ? new Vector2(rawImage.texture.width, rawImage.texture.height) : new Vector2(0, 0);
            if (textureSize.x == 0 || textureSize.y == 0)
            {
                var normalizedRect = new Rect(0, 0, 1, 1);
                if(rawImage != null)
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
		
		protected override void UnregisterReceiver () 
		{
			base.UnregisterReceiver();
            var key = Key;
            if (ForceUnloadWhenRefCountEmpty && ExternalResources.IsUselessResources(key))
            {
                if (m_unloadMode == ImageUnloadModeEnum.CancelDownload)
                {
                    if (ExternalResources.IsDownloading(key))
                    {
                        if (!ExternalResources.IsLoaded(key))
                            ExternalResources.UnloadAsset(key, false, ExternalResources.UnloadMode.SkipDestroyStep);
                        else
                            TextureDownloader.CancelAllRequestsWithUrl(key);
                    }
                }
                else
                {
                    var resourcesUnloadMode = m_unloadMode == ImageUnloadModeEnum.SoftUnload ?
                                                                ExternalResources.UnloadMode.SkipDestroyStep :
                                                                ExternalResources.UnloadMode.DestroyIfNeeded;

                    ExternalResources.UnloadAsset(key, false, resourcesUnloadMode);
                }
            }
		}
		
		protected virtual void RegisterEvents()
		{
			UnregisterEvents();
			ExternalResources.OnImageLoaded += HandleOnImageLoaded;
		}
		
		protected virtual void UnregisterEvents() 
		{
			ExternalResources.OnImageLoaded -= HandleOnImageLoaded;
		}

        protected virtual void MarkToUnloadAssets()
        {
            DefaultSprite = null;
        }

        protected virtual void ApplySprite(Sprite sprite)
		{
			Sprite = sprite;
		}

        public virtual void ApplyEmptySpriteInImageComponent()
        {
            Sprite = null;
        }

        public virtual void ApplyDefaultSpriteInImageComponent()
        {
            Sprite = DefaultSprite;
        }

        public virtual void SetExternImgFile(ExternImgFile externImg)
        {
            if (externImg != null)
            {
                Key = externImg.Url;
                Sprite = externImg.Sprite;
            }
        }
		
		protected bool _canUnregisterOnDisable = true;
        protected override void Apply()
        {
            if (ImageComponent != null)
            {
                var isDownloading = ExternalResources.IsDownloading(Key);
                if (string.IsNullOrEmpty(Key) || !isDownloading || 
                    (_canUnregisterOnDisable && isDownloading)) //someone called this request before this external image
                {
                    ExternImgFile callback = !ExternalResources.IsLoaded(Key) ?
                        ExternalResources.ReloadSpriteAsync(Key, ApplySprite) : ExternalResources.LoadSpriteAsync(Key, ApplySprite);
                    if (callback != null)
                    {
                        if (callback.IsProcessing())
                        {
                            //Reset image
                            if (/*DefaultSprite != null &&*/ Sprite != DefaultSprite)
                                ApplyDefaultSpriteInImageComponent();

                            _canUnregisterOnDisable = false;
                            if (OnRequestDownloadImageCallback != null)
                                OnRequestDownloadImageCallback.Invoke();
                        }
                        else
                        {
                            HandleOnImageLoaded(callback);
                        }
                    }
                } 
                else
                {
                    if (/*DefaultSprite != null &&*/ Sprite != DefaultSprite)
                        ApplyDefaultSpriteInImageComponent();
                    SetDirty();
                }
            }
        }
		
		#endregion
	}
}
