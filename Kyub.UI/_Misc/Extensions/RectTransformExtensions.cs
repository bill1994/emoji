using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.Extensions
{
    public static class RectTransformExtensions
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
                var v_camera = v_canvas != null && v_canvas.renderMode != RenderMode.ScreenSpaceOverlay? v_canvas.worldCamera : null;
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

        #region Local

        public static void SetLocalRect(this RectTransform p_rectTransform, Rect p_upwardLocalRect)
        {
            p_rectTransform.SetLocalSize(p_upwardLocalRect.size);
            SetBottomLeftLocalPosition(p_rectTransform, new Vector2(p_upwardLocalRect.xMin, p_upwardLocalRect.yMin));
            
            //var v_oldLocalZ = p_rectTransform.transform.localPosition.z;
            //SetBottomLeftLocalCorner(p_rectTransform, new Vector2(p_upwardLocalRect.xMin, p_upwardLocalRect.yMin));
            //SetTopLeftLocalCorner(p_rectTransform, new Vector2(p_upwardLocalRect.xMin, p_upwardLocalRect.yMax));
            //SetTopRightLocalCorner(p_rectTransform, new Vector2(p_upwardLocalRect.xMax, p_upwardLocalRect.yMax));
            //SetBottomRightLocalCorner(p_rectTransform, new Vector2(p_upwardLocalRect.xMax, p_upwardLocalRect.yMin));
            //p_rectTransform.localPosition = new Vector3(p_rectTransform.localPosition.x, p_rectTransform.localPosition.y, v_oldLocalZ);
        }

        /// <summary>
        /// Upward Rect
        /// </summary>
        /// <param name="p_rectTransform"></param>
        /// <returns></returns>
        public static Rect GetLocalRect(this RectTransform p_rectTransform)
        {
            var v_rect = new Rect(0, 0, 0, 0);
            if (p_rectTransform != null)
            {
                return p_rectTransform.rect;
            }
            return v_rect;
        }

        public static void SetPivotLocalPosition(this RectTransform p_rectTransform, Vector2 p_newPos)
        {
            p_rectTransform.localPosition = new Vector3(p_newPos.x, p_newPos.y, p_rectTransform.localPosition.z);
        }

        public static void SetBottomLeftLocalCorner(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            //Correct size before change anchor
            p_rectTransform.CorrectSizeBeforeChangeCornerPosition(p_localPos, 0);

            //Recalc Again the localPosition based in new DeltaSize
            p_rectTransform.SetBottomLeftLocalPosition(p_localPos);
        }

        public static void SetTopLeftLocalCorner(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            //Correct size before change anchor
            p_rectTransform.CorrectSizeBeforeChangeCornerPosition(p_localPos, 1);

            //Recalc Again the localPosition based in new DeltaSize
            p_rectTransform.SetTopLeftLocalPosition(p_localPos);
        }

        public static void SetTopRightLocalCorner(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            //Correct size before change anchor
            p_rectTransform.CorrectSizeBeforeChangeCornerPosition(p_localPos, 2);

            //Recalc Again the localPosition based in new DeltaSize
            p_rectTransform.SetTopRightLocalPosition(p_localPos);
        }

        public static void SetBottomRightLocalCorner(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            //Correct size before change anchor
            p_rectTransform.CorrectSizeBeforeChangeCornerPosition(p_localPos, 3);

            //Recalc Again the localPosition based in new DeltaSize
            p_rectTransform.SetBottomRightLocalPosition(p_localPos);
        }

        public static void SetBottomLeftLocalPosition(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            var v_localPosition = new Vector3(p_localPos.x + (p_rectTransform.pivot.x * p_rectTransform.rect.width), p_localPos.y + (p_rectTransform.pivot.y * p_rectTransform.rect.height), p_rectTransform.localPosition.z);
            //Convert LocalPosition to Parent LocalPosition
            Vector2 v_worldPos = p_rectTransform.TransformPoint(v_localPosition);
            var v_parentTransform = p_rectTransform.transform.parent;
            var v_parentLocalPos = v_parentTransform != null ? (Vector2)v_parentTransform.InverseTransformPoint(v_worldPos) : v_worldPos;

            p_rectTransform.localPosition = v_parentLocalPos;
        }
        public static void SetTopLeftLocalPosition(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            var v_localPosition = new Vector3(p_localPos.x + (p_rectTransform.pivot.x * p_rectTransform.rect.width), p_localPos.y - ((1f - p_rectTransform.pivot.y) * p_rectTransform.rect.height), p_rectTransform.localPosition.z);
            //Convert LocalPosition to Parent LocalPosition
            Vector2 v_worldPos = p_rectTransform.TransformPoint(v_localPosition);
            var v_parentTransform = p_rectTransform.transform.parent;
            var v_parentLocalPos = v_parentTransform != null ? (Vector2)v_parentTransform.InverseTransformPoint(v_worldPos) : v_worldPos;

            p_rectTransform.localPosition = v_parentLocalPos;
        }

        public static void SetTopRightLocalPosition(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            var v_localPosition = new Vector3(p_localPos.x - ((1f - p_rectTransform.pivot.x) * p_rectTransform.rect.width), p_localPos.y - ((1f - p_rectTransform.pivot.y) * p_rectTransform.rect.height), p_rectTransform.localPosition.z);
            //Convert LocalPosition to Parent LocalPosition
            Vector2 v_worldPos = p_rectTransform.TransformPoint(v_localPosition);
            var v_parentTransform = p_rectTransform.transform.parent;
            var v_parentLocalPos = v_parentTransform != null ? (Vector2)v_parentTransform.InverseTransformPoint(v_worldPos) : v_worldPos;

            p_rectTransform.localPosition = v_parentLocalPos;
        }

        public static void SetBottomRightLocalPosition(this RectTransform p_rectTransform, Vector2 p_localPos)
        {
            var v_localPosition = new Vector3(p_localPos.x - ((1f - p_rectTransform.pivot.x) * p_rectTransform.rect.width), p_localPos.y + (p_rectTransform.pivot.y * p_rectTransform.rect.height), p_rectTransform.localPosition.z);
            //Convert LocalPosition to Parent LocalPosition
            Vector2 v_worldPos = p_rectTransform.TransformPoint(v_localPosition);
            var v_parentTransform = p_rectTransform.transform.parent;
            var v_parentLocalPos = v_parentTransform != null ? (Vector2)v_parentTransform.InverseTransformPoint(v_worldPos) : v_worldPos;

            p_rectTransform.localPosition = v_parentLocalPos;
        }

        private static void CorrectSizeBeforeChangeCornerPosition(this RectTransform p_rectTransform, Vector2 p_newLocalPosition, int p_cornerIndex)
        {
            var v_localCorners = new Vector3[4];
            p_rectTransform.GetLocalCorners(v_localCorners);
            Vector2 v_oldLocalPosition = v_localCorners[p_cornerIndex];
            Vector2 v_newLocalPosition = p_newLocalPosition;

            //Recalc DeltaSize
            var v_delta = (Vector2)(v_newLocalPosition - v_oldLocalPosition);
            //Correct delta based in corner
            if (p_cornerIndex == 0)
                v_delta = -v_delta;
            else if (p_cornerIndex == 1)
                v_delta.x = -v_delta.x;
            else if (p_cornerIndex == 3)
                v_delta.y = -v_delta.y;
            p_rectTransform.SetLocalSize(p_rectTransform.GetLocalSize() + v_delta);
        }

        public static Vector2 GetLocalSize(this RectTransform p_rectTransform)
        {
            return p_rectTransform.rect.size;
        }

        public static float GetLocalWidth(this RectTransform p_rectTransform)
        {
            return p_rectTransform.rect.width;
        }

        public static float GetLocalHeight(this RectTransform p_rectTransform)
        {
            return p_rectTransform.rect.height;
        }

        public static void SetLocalSize(this RectTransform p_rectTransform, Vector2 p_newSize)
        {
            Vector2 v_oldSize = p_rectTransform.rect.size;
            Vector2 v_deltaSize = p_newSize - v_oldSize;
            p_rectTransform.offsetMin = p_rectTransform.offsetMin - new Vector2(v_deltaSize.x * p_rectTransform.pivot.x, v_deltaSize.y * p_rectTransform.pivot.y);
            p_rectTransform.offsetMax = p_rectTransform.offsetMax + new Vector2(v_deltaSize.x * (1f - p_rectTransform.pivot.x), v_deltaSize.y * (1f - p_rectTransform.pivot.y));
        }

        public static void SetLocalWidth(this RectTransform p_rectTransform, float p_newSize)
        {
            SetLocalSize(p_rectTransform, new Vector2(p_newSize, p_rectTransform.rect.size.y));
        }

        public static void SetLocalHeight(this RectTransform p_rectTransform, float p_newSize)
        {
            SetLocalSize(p_rectTransform, new Vector2(p_rectTransform.rect.size.x, p_newSize));
        }

        #endregion

        #region World

        public static void SetWorldRect(this RectTransform p_rectTransform, Rect p_upwardWorldRect)
        {
            var v_currentWorldRect = p_rectTransform.GetWorldRect();
            var v_widthPercentMultiplier = v_currentWorldRect.width == 0 ? 0 : p_upwardWorldRect.width / v_currentWorldRect.width;
            var v_heightPercentMultiplier = v_currentWorldRect.height == 0 ? 0 : p_upwardWorldRect.height / v_currentWorldRect.height;

            if (v_widthPercentMultiplier != 0 && v_heightPercentMultiplier != 0)
            {
                var v_localRect = p_rectTransform.GetLocalRect();
                v_localRect.width *= v_widthPercentMultiplier;
                v_localRect.height *= v_heightPercentMultiplier;
                v_localRect.center = p_rectTransform.InverseTransformPoint(p_upwardWorldRect.center);
                p_rectTransform.SetLocalRect(v_localRect);
            }
            //This method below only works in non rotated triangles but we will try apply if triangle dont have height or width
            else
            {
                var v_oldLocalZ = p_rectTransform.transform.localPosition.z;
                SetBottomLeftWorldCorner(p_rectTransform, new Vector2(p_upwardWorldRect.xMin, p_upwardWorldRect.yMin));
                SetTopLeftWorldCorner(p_rectTransform, new Vector2(p_upwardWorldRect.xMin, p_upwardWorldRect.yMax));
                SetTopRightWorldCorner(p_rectTransform, new Vector2(p_upwardWorldRect.xMax, p_upwardWorldRect.yMax));
                SetBottomRightWorldCorner(p_rectTransform, new Vector2(p_upwardWorldRect.xMax, p_upwardWorldRect.yMin));

                p_rectTransform.localPosition = new Vector3(p_rectTransform.localPosition.x, p_rectTransform.localPosition.y, v_oldLocalZ);
            }
        }

        /// <summary>
        /// Upward Rect
        /// </summary>
        /// <param name="p_rectTransform"></param>
        /// <returns></returns>
        public static Rect GetWorldRect(this RectTransform p_rectTransform)
        {
            var v_rect = new Rect(0, 0, 0, 0);
            if (p_rectTransform != null)
            {
                Vector3[] v_worldCorners = new Vector3[4];
                p_rectTransform.GetWorldCorners(v_worldCorners);
                Vector2 v_min = new Vector2(Mathf.Min(v_worldCorners[0].x, v_worldCorners[1].x, v_worldCorners[2].x, v_worldCorners[3].x), Mathf.Min(v_worldCorners[0].y, v_worldCorners[1].y, v_worldCorners[2].y, v_worldCorners[3].y));
                Vector2 v_max = new Vector2(Mathf.Max(v_worldCorners[0].x, v_worldCorners[1].x, v_worldCorners[2].x, v_worldCorners[3].x), Mathf.Max(v_worldCorners[0].y, v_worldCorners[1].y, v_worldCorners[2].y, v_worldCorners[3].y));
                //Calculate Rect
                v_rect = Rect.MinMaxRect(v_min.x, v_min.y, v_max.x, v_max.y); //new Rect(v_worldCorners[0].x, v_worldCorners[0].y, Mathf.Abs(v_worldCorners[2].x - v_worldCorners[0].x), Mathf.Abs(v_worldCorners[2].y - v_worldCorners[0].y));
            }
            return v_rect;
        }

        public static void SetBottomLeftWorldCorner(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetBottomLeftLocalCorner(v_localPosition);
        }

        public static void SetTopLeftWorldCorner(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetTopLeftLocalCorner(v_localPosition);
        }

        public static void SetBottomRightWorldCorner(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetBottomRightLocalCorner(v_localPosition);
        }

        public static void SetTopRightWorldCorner(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetTopRightLocalCorner(v_localPosition);
        }

        public static void SetBottomLeftWorldPosition(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetBottomLeftLocalPosition(v_localPosition);
        }

        public static void SetTopLeftWorldPosition(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetTopLeftLocalPosition(v_localPosition);
        }

        public static void SetBottomRightWorldPosition(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetBottomRightLocalPosition(v_localPosition);
        }

        public static void SetTopRightWorldPosition(this RectTransform p_rectTransform, Vector2 p_worldPos)
        {
            Vector3 v_localPosition = p_rectTransform.InverseTransformPoint(p_worldPos);
            p_rectTransform.SetTopRightLocalPosition(v_localPosition);
        }


        #endregion

        #region Others

        public static void Inflate(this RectTransform target, bool forceIgnoreLayout)
        {
            if (target == null || target.parent == null)
                return;

            target.localScale = Vector3.one;
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = Vector2.zero;
            target.localPosition = Vector2.zero;
            target.localRotation = Quaternion.identity;

            var layoutElement = target.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = target.gameObject.AddComponent<LayoutElement>();

            if (layoutElement != null)
            {
                if (forceIgnoreLayout)
                    layoutElement.ignoreLayout = true;
                layoutElement.flexibleWidth = 1;
                layoutElement.flexibleHeight = 1;
            }
        }

        public static void SetPivotAndAnchors(this RectTransform p_rectTransform, Vector2 p_anchorVec)
        {
            p_rectTransform.pivot = p_anchorVec;
            p_rectTransform.anchorMin = p_anchorVec;
            p_rectTransform.anchorMax = p_anchorVec;
        }

        public static void SetPivotWithoutMovingRect(this RectTransform p_rectTransform, Vector2 p_pivot)
        {
            if (p_rectTransform == null) return;

            var v_rect = p_rectTransform.GetWorldRect();
            p_rectTransform.pivot = p_pivot;
            p_rectTransform.SetWorldRect(v_rect);
        }

        #endregion
    }
}
