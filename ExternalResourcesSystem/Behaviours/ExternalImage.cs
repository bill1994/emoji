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
                var v_cachedKey = m_key;
                base.Key = value;
                if(m_key != v_cachedKey)
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
                var v_sprite = Sprite;
                var v_needReapply = v_sprite == m_defaultSprite || v_sprite == null;
                m_defaultSprite = value;
                if (v_needReapply)
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
				Image v_image = m_imageComponent as Image;
				if(v_image != null)
				{
					_sprite = v_image.sprite;
				}
				else
				{
					RawImage v_rawImage = m_imageComponent as RawImage;
					if(v_rawImage != null)
					{
						Texture2D v_texture = _sprite != null? _sprite.texture : null;
						if(v_rawImage.texture != v_texture)
							_sprite = v_texture != null? Sprite.Create(v_texture, new Rect(0,0, v_texture.width, v_texture.height), new Vector2(0.5f, 0.5f)) : null;
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

		protected void SetSpriteToImageComponent(Sprite p_sprite)
		{
            var v_oldSprite = _sprite;
			if(m_imageComponent != null)
			{
				Image v_image = m_imageComponent as Image;
				if(v_image != null)
				{
					v_image.sprite = p_sprite;
					_sprite = p_sprite;
				}
				else
				{
					RawImage v_rawImage = m_imageComponent as RawImage;
					if(v_rawImage != null)
					{
						Texture2D v_texture = p_sprite != null? p_sprite.texture : null;
						v_rawImage.texture = v_texture;
                        CalculateRawImageUVRect(v_rawImage);
                        _sprite = p_sprite;
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
            if (v_oldSprite != _sprite)
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
            RawImage v_rawImage = m_imageComponent as RawImage;
            if (v_rawImage != null)
                CalculateRawImageUVRect(v_rawImage);
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnImageLoaded (ExternImgFile p_image)
		{
            if ((string.IsNullOrEmpty(Key) && (p_image == null || string.IsNullOrEmpty(p_image.Url))) ||
                (p_image != null && string.Equals(p_image.Url, Key)))
            {
                if (ImageComponent != null && !string.IsNullOrEmpty(Key) && (p_image.Sprite != null || string.IsNullOrEmpty(p_image.Error)))
                {
                    Sprite = p_image.Sprite;
                }
                if (Sprite == null || !string.IsNullOrEmpty(p_image.Error))
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

        protected virtual void CalculateRawImageUVRect(RawImage p_rawImage)
        {
            var v_textureSize = p_rawImage != null && p_rawImage.texture != null ? new Vector2(p_rawImage.texture.width, p_rawImage.texture.height) : new Vector2(0, 0);
            if (v_textureSize.x == 0 || v_textureSize.y == 0)
            {
                var v_normalizedRect = new Rect(0, 0, 1, 1);
                if(p_rawImage != null)
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
		
		protected override void UnregisterReceiver () 
		{
			base.UnregisterReceiver();
			if(ForceUnloadWhenRefCountEmpty && ExternalResources.IsUselessResources(Key))
				ExternalResources.UnloadAsset(Key);
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

        protected virtual void ApplySprite(Sprite p_sprite)
		{
			Sprite = p_sprite;
		}

        public virtual void ApplyEmptySpriteInImageComponent()
        {
            Sprite = null;
        }

        public virtual void ApplyDefaultSpriteInImageComponent()
        {
            Sprite = DefaultSprite;
        }

        public virtual void SetExternImgFile(ExternImgFile p_externImg)
        {
            if (p_externImg != null)
            {
                Key = p_externImg.Url;
                Sprite = p_externImg.Sprite;
            }
        }
		
		protected bool _canUnregisterOnDisable = true;
        protected override void Apply()
        {
            if (ImageComponent != null)
            {
                var v_isDownloading = ExternalResources.IsDownloading(Key);
                if (string.IsNullOrEmpty(Key) || !v_isDownloading || 
                    (_canUnregisterOnDisable && v_isDownloading)) //someone called this request before this external image
                {
                    ExternImgFile v_callback = !ExternalResources.IsLoaded(Key) ?
                        ExternalResources.ReloadSpriteAsync(Key, ApplySprite) : ExternalResources.LoadSpriteAsync(Key, ApplySprite);
                    if (v_callback != null)
                    {
                        if (v_callback.IsProcessing())
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
                            HandleOnImageLoaded(v_callback);
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
