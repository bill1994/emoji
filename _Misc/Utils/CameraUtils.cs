using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub
{
    public static class CameraUtils
    {
        static Camera s_cachedMainCamera = null;
        public static Camera CachedMainCamera
        {
            get
            {
                if (s_cachedMainCamera == null || !s_cachedMainCamera.enabled || !s_cachedMainCamera.gameObject.activeSelf || !s_cachedMainCamera.gameObject.activeInHierarchy)
                    s_cachedMainCamera = Camera.main;
                return s_cachedMainCamera;
            }
        }

        public static Camera FindDrawingCamera(GameObject p_object)
        {
            Camera v_camera = null;
            if (p_object != null)
            {
                //Check for canvas if exists
                var v_canvas = p_object.GetComponentInParent<Canvas>();
                if (v_canvas != null)
                {
                    v_camera = v_canvas.worldCamera;
                    if (v_camera == null)
                        v_camera = CachedMainCamera;
                }
                else
                    v_camera = FindDrawingCamera(p_object.layer);
            }
            return v_camera;
        }

        public static Camera FindDrawingCamera(int p_layer)
        {
            Camera v_camera = null;
            var v_allCameras = Camera.allCameras;
            foreach (var v_sceneCamera in Camera.allCameras)
            {
                if ((v_sceneCamera.cullingMask & 1 << p_layer) != 0)
                {
                    v_camera = v_sceneCamera;
                    break;
                }
            }
            return v_camera;
        }

        public static Camera[] FindAllDrawingCameras(GameObject p_object)
        {
            if (p_object != null)
            {
                var v_cameras = new List<Camera>(FindAllDrawingCameras(p_object.layer));

                var v_canvas = p_object.GetComponentInParent<Canvas>();
                if (v_canvas != null)
                {
                    var v_camera = v_canvas.worldCamera;
                    if (v_camera == null)
                        v_camera = CachedMainCamera;
                    if (!v_cameras.Contains(v_camera))
                        v_cameras.Add(v_camera);
                }
                return v_cameras.ToArray();
            }
            return new Camera[0];
        }

        public static Camera[] FindAllDrawingCameras(int p_layer)
        {
            List<Camera> v_return = new List<Camera>();
            var v_allCameras = Camera.allCameras;
            foreach (var v_sceneCamera in Camera.allCameras)
            {
                if ((v_sceneCamera.cullingMask & 1 << p_layer) != 0)
                {
                    v_return.Add(v_sceneCamera);
                    break;
                }
            }
            return v_return.ToArray();
        }
    }
}
