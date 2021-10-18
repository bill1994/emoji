using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Kyub.EventSystems;
using System;

namespace Kyub.Performance
{
    public class BufferRawImage : RawImage, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [System.Serializable]
        public class OnClickUnityEvent : UnityEvent<PointerEventData, Vector2> { };

        #region Private Variables

        [Header("Buffer Fields")]
        [SerializeField]
        int m_renderBufferIndex = 0;
        [SerializeField, Tooltip("Disable it to make RawImage scale based on aspect ratio. Otherwise will try compare with SelfScreenRect to discover the NormalizedRect inside GlobalScreenRect")]
        bool m_uvBasedOnScreenRect = false;
        [SerializeField,]
        Vector2 m_offsetUV = new Vector2(0, 0);

        #endregion

        #region Callback

        public OnClickUnityEvent OnClick = new OnClickUnityEvent();

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
            {
                //ApplyRenderBufferImmediate();
                InitApplyRenderBuffer(1);
            }
        }

        bool _started = false;
        protected override void Start()
        {
            base.Start();
            _started = true;
            TryCreateClearTexture();
            texture = s_clearTexture;
            ApplyRenderBufferImmediate();
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
            if (IsActive())
                InitApplyRenderBuffer(1);
            else
                RecalculateRectUV(m_uvBasedOnScreenRect);
        }

        #endregion

        #region Unity Pointer Functions

        protected static BufferRawImage s_eventBuffer = null;

        protected bool _isDragging = false;
        protected bool _isMultiTouching = false;
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            s_eventBuffer = this;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            _isDragging = true;
            s_eventBuffer = this;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            //s_eventBuffer = null;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            s_eventBuffer = this;
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            //s_eventBuffer = null;
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            s_eventBuffer = null;
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            s_eventBuffer = this;
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!_isDragging && !_isMultiTouching)
            {
                //s_eventBuffer = this;
                var convertedPosition = ConvertPosition(eventData.position);
                //Debug.Log("OnPointerClick Screen: " + eventData.position + " Converted: " + convertedPosition);
                if (OnClick != null)
                    OnClick.Invoke(eventData, convertedPosition);
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

        protected virtual void RecalculateRectUV(bool basedOnScreenSize)
        {
            if (!Application.isPlaying)
                return;

            _screenRect = ScreenRectUtils.GetScreenRect(this.rectTransform);
            var screenSize = texture != null ? new Vector2(Screen.width, Screen.height) : new Vector2(0, 0);
            if (screenSize.x == 0 || screenSize.y == 0)
            {
                var normalizedRect = new Rect(0, 0, 1, 1);
                normalizedRect.position += m_offsetUV;
                uvRect = normalizedRect;
            }
            else
            {
                //Only supported in Non-Worldspace mode
                if (basedOnScreenSize)
                {
                    var canvas = this.canvas;
                    if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                        basedOnScreenSize = false;
                }

                if (basedOnScreenSize)
                {
                    var normalizedRect = new Rect(_screenRect.x / screenSize.x, _screenRect.y / screenSize.y, _screenRect.width / screenSize.x, _screenRect.height / screenSize.y);
                    normalizedRect.position += m_offsetUV;
                    uvRect = normalizedRect;
                }
                //Based OnViewPort
                else
                {
                    var localRect = new Rect(Vector2.zero, new Vector2(Mathf.Abs(rectTransform.rect.width), Mathf.Abs(rectTransform.rect.height)));
                    var normalizedRect = new Rect(0, 0, 1, 1);

                    var pivot = rectTransform.pivot;

                    if (localRect.width > 0 && localRect.height > 0)
                    {
                        var textureProportion = screenSize.x / screenSize.y;
                        var localRectProportion = localRect.width / localRect.height;
                        if (localRectProportion > textureProportion)
                        {
                            var mult = localRect.width > 0 ? screenSize.x / localRect.width : 0;
                            normalizedRect = new Rect(0, 0, 1, (localRect.height * mult) / screenSize.y);
                            normalizedRect.y = Mathf.Max(0, (1 - normalizedRect.height) * pivot.y);
                        }
                        else if (localRectProportion < textureProportion)
                        {
                            var mult = localRect.height > 0 ? screenSize.y / localRect.height : 0;
                            normalizedRect = new Rect(0, 0, (localRect.width * mult) / screenSize.x, 1);
                            normalizedRect.x = Mathf.Max(0, (1 - normalizedRect.width) * pivot.x);
                        }
                    }

                    normalizedRect.position += m_offsetUV;
                    uvRect = normalizedRect;
                }
            }
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();

            SustainedPerformanceManager.OnAfterDrawBuffer += HandleOnAfterDrawBuffer;
            //SustainedPerformanceManager.OnAfterSetPerformance += HandleOnAfterSetPerformance;
        }

        protected virtual void UnregisterEvents()
        {
            SustainedPerformanceManager.OnAfterDrawBuffer -= HandleOnAfterDrawBuffer;
            //SustainedPerformanceManager.OnAfterSetPerformance -= HandleOnAfterSetPerformance;
        }

        protected virtual void ApplyRenderBufferImmediate()
        {
            TryCreateClearTexture();

            var renderBuffer = SustainedPerformanceManager.GetRenderBuffer(m_renderBufferIndex);
            //Setup RenderBuffer
            if (renderBuffer != null)
            {
                if (renderBuffer != texture)
                    texture = renderBuffer;
            }
            //Apply clear texture
            else
                ClearTexture();

            RecalculateRectUV(m_uvBasedOnScreenRect);
        }

        protected virtual void InitApplyRenderBuffer(int delayFramesCounter = 1)
        {
            if (IsActive())
            {
                StopCoroutine("ApplyRenderBufferRoutine");
                StartCoroutine("ApplyRenderBufferRoutine", delayFramesCounter);
            }
            else
            {
                ApplyRenderBufferImmediate();
            }
        }

        static Texture2D s_clearTexture = null;
        protected virtual IEnumerator ApplyRenderBufferRoutine(int delayFramesCounter)
        {
            TryCreateClearTexture();

            //Wait amount of frames
            while (delayFramesCounter > 0)
            {
                delayFramesCounter -= 1;
                yield return null;
            }

            /*while (SustainedPerformanceManager.IsWaitingRenderBuffer)
            {
                ClearTexture();
                yield return null;
            }*/

            if (!SustainedPerformanceManager.IsEndOfFrame)
                yield return new WaitForEndOfFrame();

            ApplyRenderBufferImmediate();
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

        private void HandleOnAfterDrawBuffer(Dictionary<int, RenderTexture> dict)
        {
            ApplyRenderBufferImmediate();
            //InitApplyRenderBuffer();
        }

        //protected virtual void HandleOnAfterSetPerformance()
        //{
        //InitApplyRenderBuffer();
        //}

        #endregion

        #region Helper Functions

        /// <summary>
        /// Last Buffer that grabbed the Mouse/Touch Event
        /// </summary>
        /// <returns></returns>
        public static BufferRawImage GetEventBuffer()
        {
            return s_eventBuffer;
        }

        /// <summary>
        /// Use this function to detect if position is valid for interaction (click not blocked by other Non-BufferRawImage UIs)
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <returns></returns>
        public static bool IsValidInteractionPosition(Vector2 screenPosition)
        {
            var isValid = true;

            var unitySystem = UnityEngine.EventSystems.EventSystem.current;
            if (unitySystem != null)
            {
                var buffer = BufferRawImage.GetEventBuffer();

                var eventDataCurrentPosition = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                unitySystem.RaycastAll(eventDataCurrentPosition, results);

                if (results.Count > 0 && (buffer == null || results[0].gameObject != buffer.gameObject))
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Convert UI screen position to real mapped screen position.
        /// </summary>
        /// <param name="uiScreenPosition"></param>
        /// <returns></returns>
        public static Vector2 ConvertPosition(Vector2 uiScreenPosition)
        {
            if (s_eventBuffer != null)
            {
                var normalizedPosition = Rect.PointToNormalized(s_eventBuffer.ScreenRect, uiScreenPosition);
                var convertedNormalizedPosition = Rect.NormalizedToPoint(s_eventBuffer.uvRect, normalizedPosition); //normalized relative to screen (with conversion applied)
                return Rect.NormalizedToPoint(new Rect(0, 0, Screen.width, Screen.height), convertedNormalizedPosition);
            }
            return uiScreenPosition;
        }

        #endregion
    }
}