using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
    public enum PreserveAspectRatioModeEnum { Default, Envelop, Auto };

    [AddComponentMenu("UI Commons/Texture Image", 12)]
    public class TextureImage : RawImage
    {
        #region Private Variables

        [SerializeField]
        protected bool m_PreserveAspect = false;
        [SerializeField]
        PreserveAspectRatioModeEnum m_PreserveAspectMode = PreserveAspectRatioModeEnum.Default;

        #endregion

        #region Public Properties

        public bool preserveAspect
        {
            get
            {
                return m_PreserveAspect;
            }
            set
            {
                if (m_PreserveAspect == value)
                    return;
                m_PreserveAspect = value;

                SetVerticesDirty();
            }
        }

        public PreserveAspectRatioModeEnum preserveAspectMode
        {
            get
            {
                return m_PreserveAspectMode;
            }
            set
            {
                if (m_PreserveAspectMode == value)
                    return;
                m_PreserveAspectMode = value;

                SetVerticesDirty();
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (texture != null &&
                m_PreserveAspect &&
                !Mathf.Approximately(texture.width * uvRect.width, 0) &&
                !Mathf.Approximately(texture.height * uvRect.height, 0))
            {
                GenerateSimpleMesh(toFill, m_PreserveAspect);
            }
            else
            {
                base.OnPopulateMesh(toFill);
            }
        }

        #endregion

        #region Preserve Aspect Calc

        /// <summary>
        /// Generate vertices for a simple Image.
        /// </summary>
        protected virtual void GenerateSimpleMesh(VertexHelper vh, bool shouldPreserveAspect)
        {
            var size = texture == null ? Vector2.zero : new Vector2(texture.width * uvRect.width, texture.height * uvRect.height);
            var preserveAspectMode = GetAspectRatioMode(size);

            Vector4 v = GetDrawingDimensions(shouldPreserveAspect, preserveAspectMode);
            var uv = GetDrawingUV(size, shouldPreserveAspect, preserveAspectMode);

            var color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        protected virtual PreserveAspectRatioModeEnum GetAspectRatioMode(Vector2 size)
        {
            var preserveAspectMode = m_PreserveAspectMode;

            //We adjust to envolop when image and drawing space has same orientation (Portrait/ Widescreen)
            if (preserveAspectMode == PreserveAspectRatioModeEnum.Auto)
            {
                var drawingRect = GetPixelAdjustedRect();
                if ((size.x > size.y && drawingRect.size.x >= drawingRect.size.y) ||
                    (size.x < size.y && drawingRect.size.x < drawingRect.size.y))
                {
                    preserveAspectMode = PreserveAspectRatioModeEnum.Envelop;
                }
                else
                {
                    preserveAspectMode = PreserveAspectRatioModeEnum.Default;
                }
            }

            return preserveAspectMode;
        }

        /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
        protected virtual Vector4 GetDrawingDimensions(bool shouldPreserveAspect, PreserveAspectRatioModeEnum aspectMode)
        {
            var padding = Vector4.zero;
            var size = texture == null ? Vector2.zero : new Vector2(texture.width * uvRect.width, texture.height * uvRect.height);

            Rect r = GetPixelAdjustedRect();
            // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

            int spriteW = Mathf.RoundToInt(size.x);
            int spriteH = Mathf.RoundToInt(size.y);

            var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

            if (shouldPreserveAspect && size.sqrMagnitude > 0.0f && aspectMode == PreserveAspectRatioModeEnum.Default)
            {
                PreserveRectAspectRatio(ref r, size);
            }

            v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
            );

            return v;
        }

        protected virtual Vector4 GetDrawingUV(Vector2 textureSize, bool shouldPreserveAspect, PreserveAspectRatioModeEnum aspectMode)
        {
            var uv = new Vector4(this.uvRect.xMin, this.uvRect.yMin, this.uvRect.xMax, this.uvRect.yMax);

            if (shouldPreserveAspect && aspectMode == PreserveAspectRatioModeEnum.Envelop && textureSize.sqrMagnitude > 0)
            {
                var localRect = new Rect(Vector2.zero, new Vector2(Mathf.Abs(rectTransform.rect.width), Mathf.Abs(rectTransform.rect.height)));
                var minMaxRect = uvRect;
                var normalizedRect = Rect.MinMaxRect(minMaxRect.xMin, minMaxRect.yMin, minMaxRect.xMax, minMaxRect.yMax);

                var pivot = rectTransform.pivot;

                if (localRect.width > 0 && localRect.height > 0)
                {
                    var textureProportion = textureSize.x / textureSize.y;
                    var localRectProportion = localRect.width / localRect.height;
                    if (localRectProportion > textureProportion)
                    {
                        var mult = localRect.width > 0 ? textureSize.x / localRect.width : 0;
                        normalizedRect = new Rect(minMaxRect.xMin, minMaxRect.yMin, minMaxRect.width, ((localRect.height * mult) / textureSize.y) * minMaxRect.height);
                        normalizedRect.y = Mathf.Max(minMaxRect.yMin, (minMaxRect.yMax - normalizedRect.height) * pivot.y);
                    }
                    else if (localRectProportion < textureProportion)
                    {
                        var mult = localRect.height > 0 ? textureSize.y / localRect.height : 0;
                        normalizedRect = new Rect(minMaxRect.xMin, minMaxRect.yMin, ((localRect.width * mult) / textureSize.x) * minMaxRect.width, minMaxRect.height);
                        normalizedRect.x = Mathf.Max(minMaxRect.xMin, (1 - normalizedRect.width) * pivot.x);
                    }
                }

                uv = new Vector4(normalizedRect.xMin, normalizedRect.yMin, normalizedRect.xMax, normalizedRect.yMax);
            }

            return uv;
        }

        protected virtual void PreserveRectAspectRatio(ref Rect rect, Vector2 spriteSize)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
            }
        }

        #endregion
    }
}
