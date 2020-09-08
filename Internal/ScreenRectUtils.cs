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
        public static void GetScreenCorners(this RectTransform rectTransform, out Vector2[] fourCornersArray)
        {
            fourCornersArray = new Vector2[4];
            if (rectTransform != null)
            {
                var canvas = rectTransform.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                if (canvas != null && (camera != null || canvas.renderMode != RenderMode.WorldSpace))
                {
                    if (camera == null && canvas.renderMode == RenderMode.WorldSpace)
                        camera = CameraUtils.CachedMainCamera;
                    //Get World Corners
                    Vector3[] worldCorners = new Vector3[fourCornersArray.Length];
                    rectTransform.GetWorldCorners(worldCorners);

                    //Convert World to ScreenSpace
                    for (int i = 0; i < worldCorners.Length; i++)
                    {
                        var worldCorner = worldCorners[i];
                        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                            fourCornersArray[i] = RectTransformUtility.WorldToScreenPoint(camera, worldCorner);
                        else //Overlay Render Mode will have world space as ScreenSpace
                            fourCornersArray[i] = RectTransformUtility.WorldToScreenPoint(null, worldCorner);
                    }
                }
            }
        }

        /// <summary>
        /// Upward Rect
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <returns></returns>
        public static Rect GetScreenRect(this RectTransform rectTransform)
        {
            var rect = new Rect(0, 0, 0, 0);
            //Get Canvas Camera
            if (rectTransform != null)
            {
                Vector2[] screenCorners = new Vector2[4];
                GetScreenCorners(rectTransform, out screenCorners);
                //Calculate Rect
                rect = new Rect((int)(screenCorners[0].x), (int)(screenCorners[0].y), (int)Mathf.Abs(screenCorners[2].x - screenCorners[0].x), (int)Mathf.Abs(screenCorners[2].y - screenCorners[0].y));
            }
            return rect;
        }

        /// <summary>
        /// Downward Rect
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <returns></returns>
        public static Rect GetDownwardScreenRect(this RectTransform rectTransform)
        {
            var rect = new Rect(0, 0, 0, 0);
            //Get Canvas Camera
            if (rectTransform != null)
            {
                Vector2[] screenCorners = new Vector2[4];
                GetScreenCorners(rectTransform, out screenCorners);
                //Calculate Inverse Y (must be reverted because GetWorldCorners is Upwards Y Direction and ReadPixels is Downwards
                rect = new Rect((int)(screenCorners[0].x), Screen.height - screenCorners[2].y, (int)Mathf.Abs(screenCorners[2].x - screenCorners[0].x), (int)Mathf.Abs(screenCorners[2].y - screenCorners[0].y));
            }
            return rect;
        }

        #endregion
    }
}
