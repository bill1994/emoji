using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.UI;

namespace Kyub.Performance
{
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(Camera))]
    public sealed class LowPerformanceView : MonoBehaviour
    {
        #region Private Variables

        bool _isViewActive = false;
        Texture2D _frameBuffer = null;
        Camera _cachedCamera = null;
        Canvas _cachedCanvas = null;
        RawImage _cachedRawImage = null;

        bool _bufferIsDirty = true;

        #endregion

        #region Public Properties

        public Camera Camera
        {
            get
            {
                if (this != null && _cachedCamera == null && Application.isPlaying)
                {
                    ConfigureCamera();
                }
                return _cachedCamera;
            }
        }

        public Canvas Canvas
        {
            get
            {
                if (this != null && _cachedCanvas == null && Application.isPlaying)
                {
                    ConfigureCanvas();
                }
                return _cachedCanvas;
            }
        }

        public RawImage RawImage
        {
            get
            {
                if (this != null && _cachedRawImage == null && Application.isPlaying)
                {
                    ConfigureCanvas();
                }
                return _cachedRawImage;
            }
        }

        #endregion

        #region Unity Functions

        private void Awake()
        {
            Configure();
        }

        private void OnDestroy()
        {
            ClearFrameBuffer();
        }

        private void OnPostRender()
        {
            if (_bufferIsDirty)
            {
                _bufferIsDirty = false;
                DrawToFrameBuffer();
                ReadFromFrameBuffer();
            }
        }

        #endregion

        #region Helper Functions

        public bool IsViewActive()
        {
            return this.gameObject.activeSelf && _isViewActive;
        }

        public void SetViewActive(bool active)
        {
            if (Application.isPlaying)
            {
                gameObject.SetActive(active || Application.isEditor);
                _isViewActive = active;
                if (Canvas != null)
                    _cachedCanvas.enabled = active && SustainedPerformanceManager.UseSimulatedFrameBuffer;

                if (active)
                    SetFrameBufferDirty();
                else if (Application.isEditor)
                {
                    Configure();
                }
            }
        }

        public void SetFrameBufferDirty()
        {
            _bufferIsDirty = true;
        }

        internal void DrawToFrameBuffer()
        {
            Configure();

            if (_cachedCamera != null && SustainedPerformanceManager.UseSimulatedFrameBuffer && _isViewActive)
            {
                var cameraViewsToDrawOnScreen = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(false);

                Camera lastCamera = cameraViewsToDrawOnScreen.Count == 0? this.Camera : cameraViewsToDrawOnScreen[cameraViewsToDrawOnScreen.Count-1].Camera;
                if (lastCamera == null)
                    lastCamera = this.Camera;

                //Set all canvas to last camera
                HashSet<Canvas> overlayCanvas = new HashSet<Canvas>();
                var canvasViewsToDraw = SustainedCanvasView.FindAllActiveCanvasView();
                foreach (var canvasView in canvasViewsToDraw)
                {
                    var canvasGroup = canvasView.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        canvasGroup.alpha = canvasView._cachedAlphaValue;
                    if (canvasView.Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        canvasView.Canvas.worldCamera = lastCamera;
                        canvasView.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        overlayCanvas.Add(canvasView.Canvas);
                    }
                }

                //Render all Cameras
                for (int i = 0; i < cameraViewsToDrawOnScreen.Count-1; i++)
                {
                    var camera = cameraViewsToDrawOnScreen[i].Camera;
                    camera.Render();
                }

                //Render Last Camera
                lastCamera.Render();

                //Revert Canvas
                foreach (var canvasView in canvasViewsToDraw)
                {
                    var canvasGroup = canvasView.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        canvasGroup.alpha = canvasView.IsViewActive() ? canvasView._cachedAlphaValue : 0;

                    if (overlayCanvas.Contains(canvasView.Canvas))
                    {
                        canvasView.Canvas.worldCamera = null;
                        canvasView.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                }
            }
        }

        internal void ReadFromFrameBuffer()
        {
            if (_cachedCamera != null && SustainedPerformanceManager.UseSimulatedFrameBuffer && _isViewActive)
            {
                //Copy Render texture to a texture
                Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
                //Only recreate texture if size changed
                if (_frameBuffer == null || _frameBuffer.width != screenSize.x || _frameBuffer.height != screenSize.y)
                {
                    ClearFrameBuffer();
                    _frameBuffer = new Texture2D(screenSize.x, screenSize.y, TextureFormat.RGB24, true);
                }
                _frameBuffer.ReadPixels(new Rect(0, 0, screenSize.x, screenSize.y), 0, 0, true);
                _frameBuffer.Apply();

                //Apply to Screen RawImage Component
                if (RawImage != null)
                {
                    _cachedRawImage.texture = _frameBuffer;
                    _cachedRawImage.enabled = _frameBuffer != null;
                }
            }
        }

        internal void ClearFrameBuffer()
        {
            if (_frameBuffer != null)
                Texture2D.Destroy(_frameBuffer);
        }

        internal void Configure()
        {
            ConfigureCamera();
            ConfigureCanvas();
        }

        internal void ConfigureCamera()
        {
            if (Application.isPlaying)
            {
                if (Application.isEditor)
                    gameObject.hideFlags = HideFlags.NotEditable;

                if (_cachedCamera == null)
                {
                    _cachedCamera = GetComponent<Camera>();
                    if (_cachedCamera == null)
                        _cachedCamera = gameObject.AddComponent<Camera>();

                    _cachedCamera.backgroundColor = Color.clear;
                    //_cachedCamera.clearStencilAfterLightingPass = false;
                    _cachedCamera.allowHDR = true;
                    _cachedCamera.allowMSAA = true;
                    //_cachedCamera.allowDynamicResolution = false;
                    _cachedCamera.orthographic = true;
                    _cachedCamera.farClipPlane = 0.0001f;
                    _cachedCamera.nearClipPlane = 0;
                    _cachedCamera.depth = -999999;
                }

                _cachedCamera.cullingMask = _isViewActive ? 1 << 0 : 0;
                _cachedCamera.enabled = true;
                _cachedCamera.clearFlags = _isViewActive && SustainedPerformanceManager.UseSimulatedFrameBuffer ? CameraClearFlags.SolidColor : CameraClearFlags.Nothing;
            }
        }

        internal void ConfigureCanvas()
        {
            if (Application.isPlaying)
            {
                if (Application.isEditor)
                    gameObject.hideFlags = HideFlags.NotEditable;

                if (_cachedCanvas == null)
                {
                    _cachedCanvas = GetComponent<Canvas>();
                    if (_cachedCanvas == null)
                        _cachedCanvas = gameObject.AddComponent<Canvas>();

                    _cachedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _cachedCanvas.sortingOrder = short.MinValue;
                }

                if (_cachedRawImage == null)
                {
                    _cachedRawImage = GetComponent<RawImage>();
                    if (_cachedRawImage == null)
                        _cachedRawImage = gameObject.AddComponent<RawImage>();
                }

                _cachedRawImage.enabled = _frameBuffer != null;
                _cachedCanvas.enabled = _isViewActive && SustainedPerformanceManager.UseSimulatedFrameBuffer;
            }
        }

        #endregion
    }
}

