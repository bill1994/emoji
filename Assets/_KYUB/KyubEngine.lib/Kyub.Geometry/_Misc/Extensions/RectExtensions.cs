using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kyub.Extensions
{
    public static class RectExtensions
    {
        public static Rect Union(this Rect value1, Rect value2)
        {
            float x = Mathf.Min(value1.x, value2.x);
            float y = Mathf.Min(value1.y, value2.y);
            return new Rect(x, y,
                                 Mathf.Max(value1.xMax, value2.xMax) - x,
                                     Mathf.Max(value1.yMax, value2.yMax) - y);
        }

        public static Rect Intersection(this Rect value1, Rect value2)
        {
            var result = new Rect(0, 0, 0, 0);
            if (Intersects(value1, value2))
            {
                float right_side = Mathf.Min(value1.x + value1.width, value2.x + value2.width);
                float left_side = Mathf.Max(value1.x, value2.x);
                float top_side = Mathf.Max(value1.y, value2.y);
                float bottom_side = Mathf.Min(value1.y + value1.height, value2.y + value2.height);
                result = Rect.MinMaxRect(right_side, top_side, left_side, bottom_side);
                if (result.width < 0)
                {
                    result.x += result.width;
                    result.width = Mathf.Abs(result.width);
                }
                if (result.height < 0)
                {
                    result.y += result.height;
                    result.height = Mathf.Abs(result.height);
                }
            }
            return result;
        }

        public static bool Intersects(this Rect value1, Rect value2)
        {
            bool result = value1.x < value2.xMax &&
                     value2.x < value1.xMax &&
                     value1.y < value2.yMax &&
                     value2.y < value1.yMax;
            return result;
        }

        // returns the rectangles which are part of rect1 but not part of rect2
        public static List<Rect> RectSubtract(this Rect rect1, Rect rect2)
        {
            List<Rect> results = new List<Rect>();
            if (rect1.width == 0)
            {
                return results;
            }
            Rect intersectedRect = Intersection(rect1, rect2);

            // No intersection
            if (intersectedRect.width == 0 || intersectedRect.height == 0)
            {
                results.Add(rect1);
                return results;
            }

            Rect v_leftRect = new Rect(rect1.x, rect1.y, intersectedRect.x - rect1.x, rect1.height);
            if (v_leftRect.width != 0 && v_leftRect.height != 0)
                results.Add(v_leftRect);
            Rect v_rightRect = new Rect(intersectedRect.xMax, rect1.y, rect1.xMax - intersectedRect.xMax, rect1.height);
            if (v_rightRect.width != 0 && v_rightRect.height != 0)
                results.Add(v_rightRect);
            Rect v_bottomRect = new Rect(rect1.x, rect1.y, rect1.width, intersectedRect.y - rect1.y);
            if (v_bottomRect.width != 0 && v_bottomRect.height != 0)
                results.Add(v_bottomRect);
            Rect v_topRect = new Rect(rect1.x, intersectedRect.yMax, rect1.width, rect1.yMax - intersectedRect.yMax);
            if (v_topRect.width != 0 && v_topRect.height != 0)
                results.Add(v_topRect);

            return results;
        }
    }
}
