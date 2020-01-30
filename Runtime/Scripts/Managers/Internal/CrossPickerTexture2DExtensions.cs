// Only works on ARGB32, RGB24 and Alpha8 textures that are marked readable

#if !UNITY_WEBGL
using System.Threading;
#endif
using UnityEngine;

namespace Kyub.PickerServices.Extensions
{
    public static class CrossPickerTexture2DExtensions
    {
        #region Helper Classes

        public class ThreadData
        {
            public int start;
            public int end;
            public ThreadData(int s, int e)
            {
                start = s;
                end = e;
            }
        }

        #endregion

        #region Variables

        private static Color[] texColors;
        private static Color[] newColors;
        private static int w;
        private static float ratioX;
        private static float ratioY;
        private static int w2;
#if !UNITY_WEBGL
        private static int finishCount;
        private static Mutex mutex;
#endif

        #endregion

        #region Public Helper Functions

        public static void PointScale(this Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, false);
        }

        public static void BilinearScale(this Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, true);
        }

        public static void Crop(this Texture2D texture, int x, int y, int width, int height)
        {
            Color[] c = texture.GetPixels(x, y, width, height);
            texture.Resize(width, height);
            texture.SetPixels(c);
            texture.Apply();
        }

        public static void FormatToProfilePicture(this Texture2D texture, int avatarSize)
        {
            float smallestSide = texture.height > texture.width ? texture.width : texture.height;
            float biggestSide = texture.height > texture.width ? texture.height : texture.width;

            float biggestSideTargetSize = Mathf.Ceil(Mathf.Max((avatarSize / smallestSide) * biggestSide, avatarSize));
            ClampSize(texture, biggestSideTargetSize, biggestSideTargetSize);

            int startX = texture.height > texture.width ? 0 : (texture.width - avatarSize) / 2;
            int startY = texture.height > texture.width ? (texture.height - avatarSize) / 2 : 0;
            texture.Crop(startX, startY, avatarSize, avatarSize);
        } 

        public static bool ClampSize(Texture2D p_texture, float p_minSize = 4, float p_maxSize = 512)
        {
            bool v_return = false;
            p_maxSize = Mathf.Max(2, p_maxSize);
            p_minSize = Mathf.Max(2, p_minSize);
            if (p_texture != null)
            {
                bool v_needApplyMin = p_texture.width < p_minSize || p_texture.height < p_minSize;
                bool v_needApplyMax = p_texture.width > p_maxSize || p_texture.height > p_maxSize;
                if (v_needApplyMin || v_needApplyMax)
                {
                    float v_sizeToApply = v_needApplyMin ? p_minSize : p_maxSize;
                    float v_widthRatio = (float)v_sizeToApply / p_texture.width;
                    float v_heightRatio = (float)v_sizeToApply / p_texture.height;
                    if (v_widthRatio < v_heightRatio)
                        p_texture.BilinearScale(Mathf.RoundToInt(p_texture.width * v_widthRatio), Mathf.RoundToInt(p_texture.height * v_widthRatio));
                    else
                        p_texture.BilinearScale(Mathf.RoundToInt(p_texture.width * v_heightRatio), Mathf.RoundToInt(p_texture.height * v_heightRatio));
                    v_return = true;
                }
            }
            return v_return;
        }

        #endregion

        #region Internal Helper Functions

        private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight, bool useBilinear)
        {
            texColors = tex.GetPixels();
            newColors = new Color[newWidth * newHeight];
            if (useBilinear)
            {
                ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
                ratioY = 1.0f / ((float)newHeight / (tex.height - 1));
            }
            else
            {
                ratioX = ((float)tex.width) / newWidth;
                ratioY = ((float)tex.height) / newHeight;
            }
            w = tex.width;
            w2 = newWidth;
            var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
            var slice = newHeight / cores;

#if !UNITY_WEBGL
            finishCount = 0;
            if (mutex == null)
            {
                mutex = new Mutex(false);
            }
#endif
            if (cores > 1)
            {
                int i = 0;
                ThreadData threadData;
                for (i = 0; i < cores - 1; i++)
                {
                    threadData = new ThreadData(slice * i, slice * (i + 1));
#if !UNITY_WEBGL
                    ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart(BilinearScaleInternal) : new ParameterizedThreadStart(PointScaleInternal);
                    Thread thread = new Thread(ts);
                    thread.Start(threadData);
#else
                    if (useBilinear)
                        BilinearScaleInternal(threadData);
                    else
                        PointScaleInternal(threadData);
#endif
                }
                threadData = new ThreadData(slice * i, newHeight);
                if (useBilinear)
                    BilinearScaleInternal(threadData);
                else
                    PointScaleInternal(threadData);
#if !UNITY_WEBGL
                while (finishCount < cores)
                {
                    Thread.Sleep(1);
                }
#endif
            }
            else
            {
                ThreadData threadData = new ThreadData(0, newHeight);
                if (useBilinear)
                    BilinearScaleInternal(threadData);
                else
                    PointScaleInternal(threadData);
            }

            tex.Resize(newWidth, newHeight);
            tex.SetPixels(newColors);
            tex.Apply();

            texColors = null;
            newColors = null;
        }

        private static void BilinearScaleInternal(object obj)
        {
            ThreadData threadData = obj as ThreadData;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * w;
                var y2 = (yFloor + 1) * w;
                var yw = y * w2;

                for (var x = 0; x < w2; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
                                                           ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
                                                           y * ratioY - yFloor);
                }
            }
#if !UNITY_WEBGL
            mutex.WaitOne();
            finishCount++;
            mutex.ReleaseMutex();
#endif
        }

        private static void PointScaleInternal(object obj)
        {
            ThreadData threadData = obj as ThreadData;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                var thisY = (int)(ratioY * y) * w;
                var yw = y * w2;
                for (var x = 0; x < w2; x++)
                {
                    newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                }
            }
#if !UNITY_WEBGL
            mutex.WaitOne();
            finishCount++;
            mutex.ReleaseMutex();
#endif
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value,
                              c1.g + (c2.g - c1.g) * value,
                              c1.b + (c2.b - c1.b) * value,
                              c1.a + (c2.a - c1.a) * value);
        }

        #endregion
    }
}