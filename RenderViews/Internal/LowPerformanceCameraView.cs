using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kyub.Performance
{
    public sealed class LowPerformanceCameraView : MonoBehaviour
    {
        #region Private Variables

        Texture2D _renderBuffer = null;

        #endregion

        #region Public Properties

        Camera _cachedCamera = null;
        public Camera Camera
        {
            get
            {
                if (this != null && _cachedCamera == null)
                {
                    ConfigureCamera();
                }
                return _cachedCamera;
            }
        }

        #endregion

        #region Unity Functions

        void OnEnable()
        {
            InitRenderBufferDelayed();
        }

        void Start()
        {
            if (Application.isPlaying && Camera == null)
            {
                ClearRenderBuffer();
                Debug.Log("[FakeFrameBufferView] Must contain a camera to work, removing component to prevent instabilities (sender: " + name + ")");
                Component.Destroy(this);
                return;
            }
        }

        void OnDisable()
        {
            ClearRenderBuffer();
        }

        void OnDestroy()
        {
            ClearRenderBuffer();
        }

        void OnPostRender()
        {
            if (_renderBuffer != null)
            {
                if (SustainedPerformanceManager.SimulateKeepFrameBuffer)
                    Graphics.Blit(_renderBuffer, null as RenderTexture);
                else
                    ClearRenderBuffer();
            }
        }

        #endregion

        #region Buffer Helper Functions

        internal void ClearRenderBuffer()
        {
            if (_initRenderBufferCoroutine != null)
            {
                StopCoroutine(_initRenderBufferCoroutine);
                _initRenderBufferCoroutine = null;
            }
            if (_renderBuffer != null)
            {
                if (Application.isPlaying)
                    Texture2D.Destroy(_renderBuffer);
                else
                    Texture2D.DestroyImmediate(_renderBuffer);
            }
            _renderBuffer = null;
        }

        Coroutine _initRenderBufferCoroutine = null;
        void InitRenderBufferDelayed()
        {
            if (enabled && gameObject.activeInHierarchy)
            {
                if (SustainedPerformanceManager.SimulateKeepFrameBuffer)
                    Camera.enabled = false;
                if (_initRenderBufferCoroutine == null)
                    _initRenderBufferCoroutine = StartCoroutine(InitRenderBufferOnEndFrameRoutine());
            }
            else
                InitRenderBuffer();
        }

        void InitRenderBuffer()
        {
            if (SustainedPerformanceManager.SimulateKeepFrameBuffer)
                Camera.enabled = false;
            ClearRenderBuffer();
            if (Application.isPlaying)
            {
                if (SustainedPerformanceManager.SimulateKeepFrameBuffer)
                {
                    //Take Screenshot and Save it
                    var width = Screen.width;
                    var height = Screen.height;
                    _renderBuffer = new Texture2D(width, height, TextureFormat.RGB24, false);

                    _renderBuffer.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    _renderBuffer.Apply();
                }
                ConfigureCamera();
            }
            Camera.enabled = true;
        }

        IEnumerator InitRenderBufferOnEndFrameRoutine()
        {
            yield return new WaitForEndOfFrame();
            InitRenderBuffer();
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

                    if (Application.isEditor)
                        gameObject.hideFlags = HideFlags.NotEditable; //s_lowPerformanceCamera.gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
                    _cachedCamera.backgroundColor = Color.clear;
                    _cachedCamera.clearStencilAfterLightingPass = false;
                    _cachedCamera.allowHDR = false;
                    _cachedCamera.allowMSAA = false;
                    _cachedCamera.allowDynamicResolution = false;
                    _cachedCamera.cullingMask = 0;
                    _cachedCamera.orthographic = true;
                    _cachedCamera.farClipPlane = 0.1f;
                    _cachedCamera.nearClipPlane = 0;
                    _cachedCamera.enabled = enabled;
                    _cachedCamera.depth = -999999;
                }
                _cachedCamera.clearFlags = SustainedPerformanceManager.SimulateKeepFrameBuffer ? CameraClearFlags.SolidColor : CameraClearFlags.Nothing;
            }
        }

        #endregion

        #region Receivers

        private void HandleOnAfterSetPerformance()
        {
            InitRenderBufferDelayed();
        }

        private void HandleOnAfterDrawBuffer(Dictionary<int, RenderTexture> obj)
        {
            InitRenderBuffer();
        }

        #endregion
    }
}

