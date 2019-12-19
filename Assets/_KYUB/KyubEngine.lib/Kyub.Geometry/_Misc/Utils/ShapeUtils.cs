using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub
{
    
    public class ShapeUtils
    {
        #region Public Functions

        /// <summary>
        /// Slower and unprecise version of unity ShapeFromSprite. If you want to use unity version chek EditorShapeUtils
        /// </summary>
        public static ComplexShape ShapeFromSprite(Sprite p_sprite, float p_detail = 1, float p_alphaTolerance = 0f, bool p_holeDetection = true)
        {
            if (p_sprite != null)
            {
                var v_atlasRect = new Rect(0,0, p_sprite.texture.width, p_sprite.texture.height);

                var v_initial = Rect.PointToNormalized(v_atlasRect, p_sprite.rect.position);
                var v_final = Rect.PointToNormalized(v_atlasRect, p_sprite.rect.max);
                var v_uvRect = new Rect(v_initial, v_final - v_initial);
                return ShapeFromUVTexture(p_sprite.texture, v_uvRect, p_detail, p_alphaTolerance, p_holeDetection);
            }
            return null;
        }

        public static ComplexShape ShapeFromUVTexture(Texture2D p_texture, Rect p_uvRect, float p_detail = 1, float p_alphaTolerance = 0f, bool p_holeDetection = true)
        {
            if (p_texture != null)
            {
                List<List<Vector2>> v_paths = Kyub.ExternLibs.ImgToPathLib.ImgToPath.PathFromTexture(p_texture, p_uvRect, p_alphaTolerance);

                var v_finalShape = new ComplexShape();
                foreach (var v_path in v_paths)
                {
                    var v_internalShape = new ComplexShape(new PolygonShape(v_path));
                    if (p_holeDetection || v_internalShape.IsOrientedClockWise())
                    {
                        v_internalShape.Optimize(true, Mathf.Max(v_internalShape.RectBounds.width, v_internalShape.RectBounds.height) * p_detail);
                        v_finalShape.AddShape(v_internalShape, false);
                    }
                }
                v_finalShape.Optimize(false);

                var v_width = p_uvRect.size.x * p_texture.width;
                var v_height = p_uvRect.size.y * p_texture.height;
                v_finalShape.TransformPosition(new Rect(0, 0, v_width - 1, v_height - 1), new Rect(0, 0, 1, 1));

                return v_finalShape;
            }
            return null;
        }

        public static ComplexShape ShapeFromTexture(Texture2D p_texture, float p_detail = 1, float p_alphaTolerance = 0f, bool p_holeDetection = true)
        {
            return ShapeFromUVTexture(p_texture, new Rect(0, 0, 1, 1), p_detail, p_alphaTolerance, p_holeDetection);
        }

        #endregion
    }
}
