using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Performance
{
    public static class ScreenRectUtils
    {
        #region Screen

        /// <summary>
        /// Upwards corners (0: BottomLeft, 1: TopLeft, 2: TopRight, 3: BottomRight)
        /// </summary>
        public static void GetScreenCorners(this RectTransform p_rectTransform, out Vector2[] p_fourCornersArray)
        {
            p_fourCornersArray = new Vector2[4];
            if (p_rectTransform != null)
            {
                var v_canvas = p_rectTransform.GetComponentInParent<Canvas>();
                var v_camera = v_canvas != null && v_canvas.renderMode != RenderMode.ScreenSpaceOverlay ? v_canvas.worldCamera : null;
                if (v_canvas != null && (v_camera != null || v_canvas.renderMode != RenderMode.WorldSpace))
                {
                    if (v_camera == null && v_canvas.renderMode == RenderMode.WorldSpace)
                        v_camera = CameraUtils.CachedMainCamera;
                    //Get World Corners
                    Vector3[] v_worldCorners = new Vector3[p_fourCornersArray.Length];
                    p_rectTransform.GetWorldCorners(v_worldCorners);

                    //Convert World to ScreenSpace
                    for (int i = 0; i < v_worldCorners.Length; i++)
                    {
                        var v_worldCorner = v_worldCorners[i];
                        if (v_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                            p_fourCornersArray[i] = RectTransformUtility.WorldToScreenPoint(v_camera, v_worldCorner);
                        else //Overlay Render Mode will have world space as ScreenSpace
                            p_fourCornersArray[i] = RectTransformUtility.WorldToScreenPoint(null, v_worldCorner);
                    }
                }
            }
        }

        /// <summary>
        /// Upward Rect
        /// </summary>
        /// <param name="p_rectTransform"></param>
        /// <returns></returns>
        public static Rect GetScreenRect(this RectTransform p_rectTransform)
        {
            var v_rect = new Rect(0, 0, 0, 0);
            //Get Canvas Camera
            if (p_rectTransform != null)
            {
                Vector2[] v_screenCorners = new Vector2[4];
                GetScreenCorners(p_rectTransform, out v_screenCorners);
                //Calculate Rect
                v_rect = new Rect((int)(v_screenCorners[0].x), (int)(v_screenCorners[0].y), (int)Mathf.Abs(v_screenCorners[2].x - v_screenCorners[0].x), (int)Mathf.Abs(v_screenCorners[2].y - v_screenCorners[0].y));
            }
            return v_rect;
        }

        /// <summary>
        /// Downward Rect
        /// </summary>
        /// <param name="p_rectTransform"></param>
        /// <returns></returns>
        public static Rect GetDownwardScreenRect(this RectTransform p_rectTransform)
        {
            var v_rect = new Rect(0, 0, 0, 0);
            //Get Canvas Camera
            if (p_rectTransform != null)
            {
                Vector2[] v_screenCorners = new Vector2[4];
                GetScreenCorners(p_rectTransform, out v_screenCorners);
                //Calculate Inverse Y (must be reverted because GetWorldCorners is Upwards Y Direction and ReadPixels is Downwards
                v_rect = new Rect((int)(v_screenCorners[0].x), Screen.height - v_screenCorners[2].y, (int)Mathf.Abs(v_screenCorners[2].x - v_screenCorners[0].x), (int)Mathf.Abs(v_screenCorners[2].y - v_screenCorners[0].y));
            }
            return v_rect;
        }

        #endregion
    }
}
