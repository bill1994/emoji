using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.UI;

namespace Kyub.Performance
{
    public sealed class LowPerformanceView : MonoBehaviour
    {
        #region Private Variables

        bool _isViewActive = false;
        Texture2D _frameBuffer = null;
        Camera _cachedCamera = null;
        Material _frameBufferMaterial = null;
        Shader _frameBufferShader = null;

        bool _bufferIsDirty = true;

        #endregion

        #region Public Properties

        public Camera Camera
        {
            get
            {
                if (this != null && _cachedCamera == null && Application.isPlaying)
                {
                    Configure();
                }
                return _cachedCamera;
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
            if (_frameBufferMaterial != null)
                Material.Destroy(_frameBufferMaterial);
        }

        private void OnPostRender()
        {
            //if (_frameBuffer != null && _isViewActive)
            //    Graphics.Blit(_frameBuffer, (RenderTexture)null);

            //Force draw a fullcreen texture to frame buffer
            if (_isViewActive && _frameBuffer != null && _frameBufferMaterial != null)
            {
                GL.PushMatrix();
                _frameBufferMaterial.SetPass(0);
                GL.LoadOrtho();
                GL.Begin(GL.QUADS);
                GL.TexCoord2(0, 0);
                GL.Vertex3(0, 0, 0);
                GL.TexCoord2(0, 1);
                GL.Vertex3(0, 1, 0);
                GL.TexCoord2(1, 1);
                GL.Vertex3(1, 1, 0);
                GL.TexCoord2(1, 0);
                GL.Vertex3(1, 0, 0);
                GL.End();
                GL.PopMatrix();
            }
        }

        private void OnGUI()
        {
            if (_bufferIsDirty)
                UpdateFrameBuffer();
        }

        #endregion

        #region Helper Functions

        public bool IsViewActive()
        {
            return this.gameObject.activeSelf && enabled && _isViewActive;
        }

        public void SetViewActive(bool active)
        {
            if (Application.isPlaying)
            {
                if (!Application.isEditor)
                    gameObject.SetActive(active);
                else
                {
                    this.enabled = active;
                    gameObject.SetActive(true);
                }
                _isViewActive = active;

                if (active)
                {
                    if (SustainedPerformanceManager.IsEndOfFrame)
                        UpdateFrameBuffer();
                    else
                        SetFrameBufferDirty();
                }
                else
                {
                    _bufferIsDirty = false;
                    if (Application.isEditor)
                        Configure();
                }
            }
        }

        internal void SetFrameBufferDirty()
        {
            _bufferIsDirty = true;
        }

        internal void UpdateFrameBuffer()
        {
            _bufferIsDirty = false;
            Configure();
            if (SustainedPerformanceManager.UseSimulatedFrameBuffer && _isViewActive)
            {
                Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
                //Only recreate texture if size changed
                if (_frameBuffer == null || _frameBuffer.width != screenSize.x || _frameBuffer.height != screenSize.y)
                {
                    ClearFrameBuffer();
                    _frameBuffer = new Texture2D(screenSize.x, screenSize.y, TextureFormat.RGB24, true);
                }
                //Copy FrameBuffer to a texture
                _frameBuffer.ReadPixels(new Rect(0, 0, screenSize.x, screenSize.y), 0, 0, true);
                _frameBuffer.Apply();
                if (_frameBufferMaterial != null)
                    _frameBufferMaterial.mainTexture = _frameBuffer;
            }
        }

        internal void ClearFrameBuffer()
        {
            if (_frameBuffer != null)
                Texture2D.Destroy(_frameBuffer);
            _frameBuffer = null;

            if (_frameBufferMaterial != null)
                _frameBufferMaterial.mainTexture = _frameBuffer;
        }

        internal void Configure()
        {
            if (Application.isPlaying)
            {
                if (_frameBufferShader == null)
                    _frameBufferShader = Shader.Find("UI/Default");
                if(_frameBufferMaterial == null && _frameBufferShader != null)
                    _frameBufferMaterial = new Material(_frameBufferShader);

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
                    _cachedCamera.farClipPlane = 0.1f;
                    _cachedCamera.nearClipPlane = 0;
                    _cachedCamera.depth = -999999;
                    _cachedCamera.cullingMask = 0;
                    _cachedCamera.enabled = true;
                    _cachedCamera.clearFlags = Application.isEditor? CameraClearFlags.Nothing : CameraClearFlags.SolidColor;
                }
            }
        }

        #endregion
    }
}