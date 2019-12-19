//Edited version of code from https://github.com/Avatarchik/unity-sprite-runtime-collider-generator/blob/master/destructibleSprite.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.ExternLibs.ImgToPathLib
{
    public static class ImgToPath
    {
        #region Public Functions

        public static List<List<Vector2>> PathFromTexture(Texture2D texture, Rect uvRect, float alphaTolerance = 0f)
        {
            if (texture != null)
            {
                //		tex = t;
                bool[,] binaryImage;
                BinaryImageFromTex(out binaryImage, ref texture, uvRect, alphaTolerance);
                bool[,] binaryImageOutline = Subtraction(binaryImage, Erosion(ref binaryImage));

                List<List<Vector2>> paths = GetPaths(ref binaryImageOutline);

                return paths;
            }
            return null;
        }

        #endregion

        #region Internal Helper Functions

        // reduces the vert count about 90%
        static List<Vector2> SimplifyPath(ref List<Vector2> path)
        {

            List<Vector2> shortPath = new List<Vector2>();

            Vector2 prevPoint = path[0];
            int x = (int)path[0].x, y = (int)path[0].y;

            shortPath.Add(prevPoint);

            for (int i = 1; i < path.Count; i++)
            {
                // if x||y is the same as the previous x||y then we can skip that point
                if (x != (int)path[i].x && y != (int)path[i].y)
                {
                    shortPath.Add(prevPoint);
                    x = (int)prevPoint.x;
                    y = (int)prevPoint.y;

                    if (shortPath.Count > 3)
                    { // if we have more than 3 points we can start checking if we can remove triangle points
                        Vector2 first = shortPath[shortPath.Count - 1];
                        Vector2 last = shortPath[shortPath.Count - 3];
                        if (first.x == last.x - 1 && first.y == last.y - 1 ||
                           first.x == last.x + 1 && first.y == last.y + 1 ||
                           first.x == last.x - 1 && first.y == last.y + 1 ||
                           first.x == last.x + 1 && first.y == last.y - 1)
                        {
                            shortPath.RemoveAt(shortPath.Count - 2);
                        }
                    }
                    if (shortPath.Count > 3)
                    {
                        Vector2 first = shortPath[shortPath.Count - 1];
                        Vector2 middle = shortPath[shortPath.Count - 2];
                        Vector2 last = shortPath[shortPath.Count - 3];

                        if ((first.x == middle.x + 1 && middle.x + 1 == last.x + 2 && first.y == middle.y + 1 && middle.y + 1 == last.y + 2) ||
                           (first.x == middle.x + 1 && middle.x + 1 == last.x + 2 && first.y == middle.y - 1 && middle.y - 1 == last.y - 2) ||
                           (first.x == middle.x - 1 && middle.x - 1 == last.x - 2 && first.y == middle.y + 1 && middle.y + 1 == last.y + 2) ||
                           (first.x == middle.x - 1 && middle.x - 1 == last.x - 2 && first.y == middle.y - 1 && middle.y - 1 == last.y - 2))
                        {
                            shortPath.RemoveAt(shortPath.Count - 2);
                        }
                    }
                }
                prevPoint = path[i];
            }

            //		for(int i=1; i<shortPath.Count; i++) {
            //			// if x||y is the same as the previous x||y then we can skip that point
            //			if(x!=(int)path[i].x && y!=(int)path[i].y)
            //			{	
            //				shortPath.Add(prevPoint);
            //				x = (int)prevPoint.x;
            //				y = (int)prevPoint.y;
            //			}
            //			prevPoint = path[i];
            //		}

            return shortPath;
        }

        static List<List<Vector2>> GetPaths(ref bool[,] b)
        {
            Vector2 startPoint = Vector2.zero;
            List<List<Vector2>> paths = new List<List<Vector2>>();

            bool[,] temp = b;//(bool[,]) b.Clone();

            while (FindStartPoint(ref temp, ref startPoint))
            {
                List<Vector2> points = new List<Vector2>();

                // Get vertices from outline
                List<Vector2> path = GetPath2(ref temp, ref points, startPoint);

                // remove points from temp
                foreach (Vector2 point in path)
                {
                    temp[(int)point.x, (int)point.y] = false;
                }
                paths.Add(SimplifyPath(ref path));
                //			paths.Add (  path ); //REMOVE

            }

            return paths;
        }

        // returns true if found a start point
        static bool FindStartPoint(ref bool[,] b, ref Vector2 startPoint)
        {
            int w = b.GetLength(0); // width
            int h = b.GetLength(1); // height

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (b[x, y])
                    {
                        startPoint = new Vector2(x, y);
                        return true;
                    }
                }
            }
            return false; // Cannot find any start points.
        }

        static List<Vector2> GetPath2(ref bool[,] b, ref List<Vector2> prevPoints, Vector2 startPoint)
        {

            int[,] dirs = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

            int w = b.GetLength(0); // width
            int h = b.GetLength(1); // height

            Vector2 currPoint = Vector2.zero, newPoint = Vector2.zero;
            bool isOpen = true; // Is the path closed?

            for (int z = 0; z < dirs.GetLength(0); z++)
            {
                int i = (int)startPoint.x + dirs[z, 0];
                int j = (int)startPoint.y + dirs[z, 1];
                if (i < w && i >= 0 && j < h && j >= 0)
                {
                    if (b[i, j])
                    {
                        currPoint = new Vector2(i, j);
                    }
                }
            }

            prevPoints.Add(startPoint);

            int count = 0;

            while (isOpen && count < 500)
            {
                count++;

                prevPoints.Add(currPoint);

                // Check each direction around the start point and repeat for each new point
                for (int z = 0; z < dirs.GetLength(0); z++)
                {
                    int i = (int)currPoint.x + dirs[z, 0];
                    int j = (int)currPoint.y + dirs[z, 1];
                    if (i < w && i >= 0 && j < h && j >= 0)
                    {
                        if (b[i, j])
                        {
                            if (!prevPoints.Contains(new Vector2(i, j)))
                            {
                                newPoint = new Vector2(i, j);
                                break;
                            }
                            else
                            {
                                if (new Vector2(i, j) == startPoint)
                                {
                                    isOpen = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!isOpen) continue;

                // Deadend
                if (newPoint == currPoint)
                {
                    for (int p = prevPoints.Count - 1; p >= 0; p--)
                    {
                        for (int z = 0; z < dirs.GetLength(0); z++)
                        {
                            int i = (int)prevPoints[p].x + dirs[z, 0];
                            int j = (int)prevPoints[p].y + dirs[z, 1];
                            if (i < w && i >= 0 && j < h && j >= 0)
                            {
                                if (b[i, j])
                                {
                                    if (!prevPoints.Contains(new Vector2(i, j)))
                                    {
                                        newPoint = new Vector2(i, j);
                                        break;
                                    }
                                }
                            }
                        }
                        if (newPoint != currPoint) break;
                    }
                }
                currPoint = newPoint;
            }
            return prevPoints;
        }

        // recursive function - its gonna explode
        // Single island vert mapping
        static List<Vector2> GetPath(ref bool[,] b, ref List<Vector2> prevPoints, Vector2 currPoint)
        {
            int[,] dirs = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

            int w = b.GetLength(0); // width
            int h = b.GetLength(1); // height

            // get direction
            if (prevPoints.Count == 0)
            {
                prevPoints.Add(currPoint); // startpoint

                for (int z = 0; z < dirs.GetLength(0); z++)
                {
                    int i = (int)currPoint.x + dirs[z, 0];
                    int j = (int)currPoint.y + dirs[z, 1];
                    if (i < w && i >= 0 && j < h && j >= 0)
                    {
                        if (b[i, j])
                        {
                            return GetPath(ref b, ref prevPoints, new Vector2(i, j));
                        }
                    }
                }
                return prevPoints;
            }

            for (int z = 0; z < dirs.GetLength(0); z++)
            {
                int i = (int)currPoint.x + dirs[z, 0];
                int j = (int)currPoint.y + dirs[z, 1];
                if (i < w && i >= 0 && j < h && j >= 0)
                {
                    if (b[i, j])
                    { // if there is a point
                        Vector2 point = new Vector2(i, j);
                        if (prevPoints.Contains(point))
                        {
                            if (prevPoints[0] == point && prevPoints.Count > 2)
                            {
                                prevPoints.Add(currPoint);
                                return prevPoints;
                            }
                        }
                        else
                        {
                            prevPoints.Add(currPoint);
                            return GetPath(ref b, ref prevPoints, point);
                        }

                        //					if(!(i== prevPoints[prevPoints.Count-1].x && j== prevPoints[prevPoints.Count-1].y)) { // check its not the point we just added
                        //						if(i==prevPoints[0].x && j==prevPoints[0].y && prevPoints[0]!=prevPoints[prevPoints.Count-1]) { // Is it the start point?
                        //							prevPoints.Add (currPoint);
                        //							return prevPoints;
                        //						} else { // Add it and start looking for the next point
                        //							prevPoints.Add (currPoint);
                        //							return GetPath (ref b, ref prevPoints, new Vector2(i,j));
                        //						}
                        //					}
                    }
                }
            }

            // Deadend? backtrack to find another path to take
            for (int p = prevPoints.Count - 1; p >= 0; p--)
            {
                for (int z = 0; z < dirs.GetLength(0); z++)
                {
                    int i = (int)prevPoints[p].x + dirs[z, 0];
                    int j = (int)prevPoints[p].y + dirs[z, 1];
                    if (i < w && i >= 0 && j < h && j >= 0)
                    {
                        if (b[i, j])
                        {
                            if (!prevPoints.Contains(new Vector2(i, j)))
                            {
                                return GetPath(ref b, ref prevPoints, new Vector2(i, j));
                            }
                        }
                    }
                }
            }

            foreach (Vector2 point in prevPoints)
            {
                for (int z = 0; z < dirs.GetLength(0); z++)
                {
                    int i = (int)point.x + dirs[z, 0];
                    int j = (int)point.y + dirs[z, 1];
                    if (i < w && i >= 0 && j < h && j >= 0)
                    {
                        if (b[i, j])
                        {
                            if (!prevPoints.Contains(new Vector2(i, j)))
                            {
                                return GetPath(ref b, ref prevPoints, new Vector2(i, j));
                            }
                        }
                    }
                }
            }

            return prevPoints; // stupid c# all paths must return crap
        }

        static bool[,] Subtraction(bool[,] b1, bool[,] b2)
        {

            int w = b1.GetLength(0); // width
            int h = b1.GetLength(1); // height

            bool[,] temp = new bool[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    temp[x, y] = (b1[x, y] != b2[x, y]);
                }
            }
            return temp;
        }

        // if there is any pixel in a 3x3 grid make the centre one black
        static bool[,] Erosion(ref bool[,] b)
        {

            int[,] dirs = { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 } };

            int w = b.GetLength(0); // width
            int h = b.GetLength(1); // height

            bool[,] temp = new bool[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    temp[x, y] = true;
                    for (int z = 0; z < dirs.GetLength(0); z++)
                    {
                        int i = x + dirs[z, 0];
                        int j = y + dirs[z, 1];
                        if (i < w && i >= 0 && j < h && j >= 0)
                        {
                            if (!b[i, j]) temp[x, y] = false;
                        }
                        else temp[x, y] = false;
                    }
                }
            }

            return temp;
        }

        static bool[,] Dilation(ref bool[,] b)
        {

            bool[,] temp = b; //(bool[,]) b.Clone();
            int[,] dirs = { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 }, { -1, 1 } };

            int w = b.GetLength(0); // width
            int h = b.GetLength(1); // height

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (b[x, y])
                    {
                        for (int z = 0; z < dirs.GetLength(0); z++)
                        {
                            int i = x + dirs[z, 0];
                            int j = y + dirs[z, 1];
                            if (i < w && i >= 0 && j < h && j >= 0)
                                temp[i, j] = true;
                            //						else temp[i,j] = false; // Should already be false when initialsslslslsed
                        }
                    }
                }
            }
            return temp;
        }

        static void BinaryImageFromTex(out bool[,] b, ref Texture2D t, Rect uvRect, float alphaTolerance = 0)
        {

            var v_rect = new Rect(uvRect.position, uvRect.size);
            v_rect.position = new Vector2(uvRect.position.x * t.width, uvRect.position.y * t.height);
            v_rect.size = new Vector2(uvRect.size.x * t.width, uvRect.size.y * t.height);
            b = new bool[
                Mathf.Clamp(Mathf.CeilToInt(v_rect.width), 0, t.width), 
                Mathf.Clamp(Mathf.CeilToInt(v_rect.height), 0, t.height)];

            int xMin = Mathf.Clamp(Mathf.CeilToInt(v_rect.xMin), 0, t.width);
            int yMin = Mathf.Clamp(Mathf.CeilToInt(v_rect.yMin), 0, t.height);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(v_rect.xMax), 0, t.width);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(v_rect.yMax), 0, t.height);
            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    b[x - xMin, y - yMin] = (t.GetPixel(x, y).a > alphaTolerance); // If alpha >0 true then 1 else 0
                }
            }
        }

        #endregion
    }
}
