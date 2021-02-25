using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
    [AddComponentMenu("UI Commons/Sprite Image", 11)]
    public class SpriteImage : Image
    {
        #region Private Variables

        [SerializeField]
        PreserveAspectRatioModeEnum m_PreserveAspectMode = PreserveAspectRatioModeEnum.Default;

        #endregion

        #region Public Properties

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
            if (sprite != null && 
                preserveAspect && 
                m_PreserveAspectMode == PreserveAspectRatioModeEnum.Envelop && 
                type == Type.Simple)
            {
                GenerateSimpleMesh(toFill, preserveAspect);
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
            var size = sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);

            Vector4 v = GetDrawingDimensions(shouldPreserveAspect);
            var uv = GetDrawingUV(size, shouldPreserveAspect);

            var color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }


        /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
        protected virtual Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
        {
            var padding = sprite == null ? Vector4.zero : UnityEngine.Sprites.DataUtility.GetPadding(sprite);
            var size = sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);

            Rect r = GetPixelAdjustedRect();
            // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

            int spriteW = Mathf.RoundToInt(size.x);
            int spriteH = Mathf.RoundToInt(size.y);

            var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

            if (shouldPreserveAspect && size.sqrMagnitude > 0.0f && m_PreserveAspectMode == PreserveAspectRatioModeEnum.Default)
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

        protected virtual Vector4 GetDrawingUV(Vector2 textureSize, bool shouldPreserveAspect)
        {
            var uv = (sprite != null) ? UnityEngine.Sprites.DataUtility.GetOuterUV(sprite) : Vector4.zero;

            if (shouldPreserveAspect && m_PreserveAspectMode == PreserveAspectRatioModeEnum.Envelop && textureSize.sqrMagnitude > 0)
            {
                var localRect = new Rect(Vector2.zero, new Vector2(Mathf.Abs(rectTransform.rect.width), Mathf.Abs(rectTransform.rect.height)));
                var minMaxRect = Rect.MinMaxRect(uv.x, uv.y, uv.z, uv.w);
                var normalizedRect = Rect.MinMaxRect(minMaxRect.xMin, minMaxRect.yMin, minMaxRect.xMax, minMaxRect.yMax);

                var pivot = rectTransform.pivot;

                if (localRect.width > 0 && localRect.height > 0)
                {
                    var textureProportion = textureSize.x / textureSize.y;
                    var localRectProportion = localRect.width / localRect.height;
                    if (localRectProportion > textureProportion)
                    {
                        var mult = localRect.width > 0 ? textureSize.x / localRect.width : 0;
                        normalizedRect = new Rect(minMaxRect.xMin, minMaxRect.yMin, minMaxRect.xMax, (localRect.height * mult) / textureSize.y);
                        normalizedRect.y = Mathf.Max(minMaxRect.yMin, (minMaxRect.yMax - normalizedRect.height) * pivot.y);
                    }
                    else if (localRectProportion < textureProportion)
                    {
                        var mult = localRect.height > 0 ? textureSize.y / localRect.height : 0;
                        normalizedRect = new Rect(minMaxRect.xMin, minMaxRect.yMin, (localRect.width * mult) / textureSize.x, minMaxRect.yMax);
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
