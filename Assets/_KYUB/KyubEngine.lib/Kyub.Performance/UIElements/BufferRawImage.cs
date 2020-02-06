using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Kyub.EventSystems;

namespace Kyub.Performance
{
    public class BufferRawImage : RawImage, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [System.Serializable]
        public class Vector2UnityEvent : UnityEvent<Vector2> { };

        #region Private Variables

        [Header("Buffer Fields")]
        [SerializeField]
        int m_renderBufferIndex = 0;
        [SerializeField, Tooltip("Disable it to make RawImage scale based on aspect ratio. Otherwise will try compare with SelfScreenRect to discover the NormalizedRect inside GlobalScreenRect")]
        bool m_uvBasedOnScreenRect = true;
        [SerializeField,]
        Vector2 m_offsetUV = new Vector2(0, 0);

        #endregion

        #region Callback

        public Vector2UnityEvent OnClick = new Vector2UnityEvent();

        #endregion

        #region Public Properties

        public virtual int RenderBufferIndex
        {
            get
            {
                return m_renderBufferIndex;
            }
            set
            {
                if (m_renderBufferIndex == value)
                    return;

                m_renderBufferIndex = value;
                RecalculateRectUV(m_uvBasedOnScreenRect);
            }
        }

        public bool UvBasedOnScreenRect
        {
            get
            {
                return m_uvBasedOnScreenRect;
            }

            set
            {
                if (m_uvBasedOnScreenRect == value)
                    return;
                m_uvBasedOnScreenRect = value;
                RecalculateRectUV(m_uvBasedOnScreenRect);
            }
        }

        Rect _screenRect = new Rect(0, 0, 0, 0);
        public Rect ScreenRect
        {
            get
            {
                return _screenRect;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            ResetToDefaultState();

            base.OnEnable();
            RegisterEvents();
            if (_started)
                InitApplyRenderBuffer(1);
        }

        bool _started = false;
        protected override void Start()
        {
            base.Start();
            _started = true;
            RecalculateRectUV(m_uvBasedOnScreenRect);
            TryCreateClearTexture();
            texture = s_clearTexture;
            InitApplyRenderBuffer(1);
        }

        protected override void OnDisable()
        {
            ResetToDefaultState();

            UnregisterEvents();
            base.OnDisable();
        }

        //Prevent OnClick when multiple detected multi-touch
        protected virtual void LateUpdate()
        {
            if (InputCompat.touchSupported && s_eventBuffer == this && InputCompat.touchCount > 1)
            {
                _isMultiTouching = true;
            }
            else if (_isMultiTouching && 
                ((s_eventBuffer == null && InputCompat.touchCount == 0) || (s_eventBuffer != null && s_eventBuffer != this)))
            {
                _isMultiTouching = false;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            RecalculateRectUV(m_uvBasedOnScreenRect);
        }

        #endregion

        #region Unity Pointer Functions

        protected static BufferRawImage s_eventBuffer = null;

        protected bool _isDragging = false;
        protected bool _isMultiTouching = false;
        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            s_eventBuffer = this;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _isDragging = true;
            s_eventBuffer = this;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            //s_eventBuffer = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            s_eventBuffer = this;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //s_eventBuffer = null;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            s_eventBuffer = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            s_eventBuffer = this;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isDragging && !_isMultiTouching)
            {
                //s_eventBuffer = this;
                var v_convertedPosition = ConvertPosition(eventData.position);
                //Debug.Log("OnPointerClick Screen: " + eventData.position + " Converted: " + v_convertedPosition);
                if (OnClick != null)
                    OnClick.Invoke(v_convertedPosition);
                //s_eventBuffer = null;
            }
        }

        #endregion

        #region Helper Functions

        protected virtual void ResetToDefaultState()
        {
            if (s_eventBuffer == this)
                s_eventBuffer = null;

            _isDragging = false;
            _isMultiTouching = false;
        }

        protected virtual void RecalculateRectUV(bool p_basedOnScreenSize)
        {
            if (!Application.isPlaying)
                return;

            _screenRect = ScreenRectUtils.GetScreenRect(this.rectTransform);
            var v_textureSize = texture != null ? new Vector2(texture.width, texture.height) : new Vector2(0, 0);
            if (v_textureSize.x == 0 || v_textureSize.y == 0)
            {
                var v_normalizedRect = new Rect(0, 0, 1, 1);
                v_normalizedRect.position += m_offsetUV;
                uvRect = v_normalizedRect;
            }
            else
            {
                //Only supported in Non-Worldspace mode
                if (p_basedOnScreenSize)
                {
                    var v_canvas = GetComponentInParent<Canvas>();
                    if (v_canvas == null || v_canvas.renderMode == RenderMode.WorldSpace)
                        p_basedOnScreenSize = false;
                }

                if (p_basedOnScreenSize)
                {
                    var v_normalizedRect = new Rect(_screenRect.x / v_textureSize.x, _screenRect.y / v_textureSize.y, _screenRect.width / v_textureSize.x, _screenRect.height / v_textureSize.y);
                    v_normalizedRect.position += m_offsetUV;
                    uvRect = v_normalizedRect;
                }
                //Based OnViewPort
                else
                {
                    var v_localRect = new Rect(Vector2.zero, new Vector2(Mathf.Abs(rectTransform.rect.width), Mathf.Abs(rectTransform.rect.height)));
                    var v_normalizedRect = new Rect(0, 0, 1, 1);

                    var pivot = rectTransform.pivot;

                    if (v_localRect.width > 0 && v_localRect.height > 0)
                    {
                        var v_textureProportion = v_textureSize.x / v_textureSize.y;
                        var v_localRectProportion = v_localRect.width / v_localRect.height;
                        if (v_localRectProportion > v_textureProportion)
                        {
                            var v_mult = v_localRect.width > 0 ? v_textureSize.x / v_localRect.width : 0;
                            v_normalizedRect = new Rect(0, 0, 1, (v_localRect.height * v_mult) / v_textureSize.y);
                            v_normalizedRect.y = Mathf.Max(0, (1 - v_normalizedRect.height) * pivot.y);
                        }
                        else if (v_localRectProportion < v_textureProportion)
                        {
                            var v_mult = v_localRect.height > 0 ? v_textureSize.y / v_localRect.height : 0;
                            v_normalizedRect = new Rect(0, 0, (v_localRect.width * v_mult) / v_textureSize.x, 1);
                            v_normalizedRect.x = Mathf.Max(0, (1 - v_normalizedRect.width) * pivot.x);
                        }
                    }

                    v_normalizedRect.position += m_offsetUV;
                    uvRect = v_normalizedRect;
                }
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            SustainedPerformanceManager.OnAfterSetPerformance += HandleOnAfterSetPerformance;
        }

        protected virtual void UnregisterEvents()
        {
            SustainedPerformanceManager.OnAfterSetPerformance -= HandleOnAfterSetPerformance;
        }

        protected virtual void InitApplyRenderBuffer(int p_delayFramesCounter)
        {
            StopCoroutine("ApplyRenderBufferRoutine");
            StartCoroutine("ApplyRenderBufferRoutine", p_delayFramesCounter);
        }

        static Texture2D s_clearTexture = null;
        protected virtual IEnumerator ApplyRenderBufferRoutine(int p_delayFramesCounter)
        {
            TryCreateClearTexture();

            //Wait amount of frames
            while (p_delayFramesCounter > 0)
            {
                p_delayFramesCounter -= 1;
                yield return null;
            }

            /*while (SustainedPerformanceManager.IsWaitingRenderBuffer)
            {
                ClearTexture();
                yield return null;
            }*/

            if(!SustainedPerformanceManager.IsEndOfFrame)
                yield return new WaitForEndOfFrame();
            
            var v_renderBuffer = SustainedPerformanceManager.GetRenderBuffer(m_renderBufferIndex);
            //Setup RenderBuffer
            if (v_renderBuffer != null)
            {
                if (v_renderBuffer != texture)
                {
                    texture = v_renderBuffer;
                    RecalculateRectUV(m_uvBasedOnScreenRect);
                }
            }
            //Apply clear texture
            else
            {
                ClearTexture();
            }
        }

        protected virtual void ClearTexture()
        {
            if (texture != s_clearTexture)
            {
                texture = s_clearTexture;
                texture = texture;
                RecalculateRectUV(m_uvBasedOnScreenRect);
            }
        }

        protected virtual void TryCreateClearTexture()
        {
            if (s_clearTexture == null)
            {
                s_clearTexture = new Texture2D(4, 4, TextureFormat.ARGB32, false);
                s_clearTexture.SetPixels(new Color[s_clearTexture.width * s_clearTexture.height]);
                s_clearTexture.Apply();
                s_clearTexture.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

                ClearTexture();
            }
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnAfterSetPerformance()
        {
            InitApplyRenderBuffer(0);
        }

        #endregion

        #region Helper Functions

        public static BufferRawImage GetEventBuffer()
        {
            return s_eventBuffer;
        }

        public static Vector2 ConvertPosition(Vector2 p_mousePosition)
        {
            if (s_eventBuffer != null)
            {
                var v_normalizedPosition = Rect.PointToNormalized(s_eventBuffer.ScreenRect, p_mousePosition);
                var v_convertedNormalizedPosition = Rect.NormalizedToPoint(s_eventBuffer.uvRect, v_normalizedPosition); //normalized relative to screen (with conversion applied)
                return Rect.NormalizedToPoint(new Rect(0, 0, Screen.width, Screen.height), v_convertedNormalizedPosition);
            }
            return p_mousePosition;
        }

        #endregion
    }
}