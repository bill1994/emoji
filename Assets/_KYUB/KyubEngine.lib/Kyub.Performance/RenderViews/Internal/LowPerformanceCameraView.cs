using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace Kyub.Performance
{
    [RequireComponent(typeof(Camera))]
    public sealed class LowPerformanceCameraView : MonoBehaviour
    {
        #region Private Variables

        bool _isViewActive = false;

        Texture2D _frameBuffer = null;

        #endregion

        #region Public Properties

        Camera _cachedCamera = null;
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

        #endregion

        #region Unity Functions

        private void Awake()
        {
            if (_cachedCamera == null)
            {
                _cachedCamera = GetComponent<Camera>();
                if (_cachedCamera == null)
                    _cachedCamera = this.gameObject.AddComponent<Camera>();
            }
        }

        private void OnEnable()
        {
            if (IsViewActive())
                UpdateFrameBuffer();
        }

        private void OnDestroy()
        {
            ClearFrameBuffer();
        }

        void OnPostRender()
        {
            if (Application.isPlaying && _isViewActive)
            {
                if (_frameBuffer != null)
                    Graphics.Blit(_frameBuffer, null as RenderTexture);
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

                if (active)
                    UpdateFrameBuffer();
                else if (Application.isEditor)
                {
                    ConfigureCamera();
                }
            }
        }

        internal void UpdateFrameBuffer()
        {
            ConfigureCamera();

            if (_cachedCamera != null && SustainedPerformanceManager.UseSimulatedFrameBuffer)
            {
                var frameBufferRT = RenderTexture.GetTemporary(Screen.width, Screen.height);

                //Prevent invalidate after draw camera
                var instance = SustainedPerformanceManager.Instance;
                if (instance != null && instance.enabled && instance.gameObject.activeInHierarchy)
                    instance.UnregisterEvents();

                var cameraViewsToDrawOnScreen = SustainedCameraView.FindAllActiveCameraViewsWithRenderBufferState(false);

                Camera lastCamera = null;
                for (int i = 0; i < cameraViewsToDrawOnScreen.Count; i++)
                {
                    var camera = cameraViewsToDrawOnScreen[i].Camera;
                    if (i == cameraViewsToDrawOnScreen.Count - 1)
                        lastCamera = camera;
                    else
                    {
                        camera.targetTexture = frameBufferRT;
                        camera.Render();
                        camera.targetTexture = null;
                    }
                }
                if (lastCamera == null)
                    lastCamera = this.Camera;

                //Set all canvas to last camera
                var canvasViewsToDraw = SustainedCanvasView.FindAllActiveCanvasViewOverlay();
                foreach (var canvasView in canvasViewsToDraw)
                {
                    var canvasGroup = canvasView.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        canvasGroup.alpha = canvasView._cachedAlphaValue;
                    canvasView.Canvas.worldCamera = lastCamera;
                    canvasView.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                }

                //Render Last Camera
                lastCamera.targetTexture = frameBufferRT;
                lastCamera.Render();
                lastCamera.targetTexture = null;

                //Revert Canvas
                foreach (var canvasView in canvasViewsToDraw)
                {
                    var canvasGroup = canvasView.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        canvasGroup.alpha = canvasView.IsViewActive() ? canvasView._cachedAlphaValue : 0;
                    canvasView.Canvas.worldCamera = null;
                    canvasView.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                //Copy Render texture to a texture
                var oldRT = RenderTexture.active;
                RenderTexture.active = frameBufferRT;
                //Only recreate texture if size changed
                if (_frameBuffer == null || _frameBuffer.width != frameBufferRT.width || _frameBuffer.height != frameBufferRT.height)
                {
                    ClearFrameBuffer();
                    _frameBuffer = new Texture2D(frameBufferRT.width, frameBufferRT.height, TextureFormat.RGB24, false);
                }
                _frameBuffer.ReadPixels(new Rect(0, 0, frameBufferRT.width, frameBufferRT.height), 0, 0);
                _frameBuffer.Apply();
                RenderTexture.active = oldRT;
                
                //Destroy temporary render texture
                RenderTexture.ReleaseTemporary(frameBufferRT);

                //Register events again in Sustained Performance Manager
                if (instance != null && instance.enabled && instance.gameObject.activeInHierarchy)
                    instance.RegisterEvents();
            }
        }

        internal void ClearFrameBuffer()
        {
            if (_frameBuffer != null)
                Texture2D.Destroy(_frameBuffer);
        }

        internal void ConfigureCamera()
        {
            if (Application.isPlaying)
            {
                if (_cachedCamera == null)
                {
                    _cachedCamera = GetComponent<Camera>();
                    if (_cachedCamera == null)
                        _cachedCamera = gameObject.AddComponent<Camera>();
                }

                if (Application.isEditor)
                    gameObject.hideFlags = HideFlags.NotEditable; //s_lowPerformanceCamera.gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
                _cachedCamera.backgroundColor = Color.blue;
                _cachedCamera.clearStencilAfterLightingPass = false;
                _cachedCamera.allowHDR = false;
                _cachedCamera.allowMSAA = false;
                _cachedCamera.allowDynamicResolution = false;
                _cachedCamera.cullingMask = 0;
                _cachedCamera.orthographic = true;
                _cachedCamera.farClipPlane = 0.1f;
                _cachedCamera.nearClipPlane = 0;
                _cachedCamera.enabled = true;
                _cachedCamera.depth = -999999;
                _cachedCamera.clearFlags = SustainedPerformanceManager.UseSimulatedFrameBuffer && _isViewActive ? CameraClearFlags.SolidColor : CameraClearFlags.Nothing;
            }
        }

        #endregion
    }
}

