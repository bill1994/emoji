#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub;
using System.Reflection;

namespace KyubEditor
{
    public class EditorShapeUtils
    {
        #region Public Functions

        /// <summary>
        /// Unity version of Shape from Sprite... this only works in editor but is faster than runtime implementation
        /// </summary>
        public static ComplexShape ShapeFromSprite(Sprite p_sprite, float p_detail = 1, float p_alphaTolerance = 0f, bool p_holeDetection = true)
        {
            if (p_sprite != null)
            {
                var v_method = typeof(UnityEditor.Sprites.SpriteUtility).GetMethod("GenerateOutlineFromSprite", BindingFlags.Static | BindingFlags.NonPublic);
                Vector2[][] v_paths = null;
                var v_args = new object[] { p_sprite, p_detail, (byte)(p_alphaTolerance * 255), p_holeDetection, v_paths };
                v_method.Invoke(null, v_args);
                v_paths = v_args[4] as Vector2[][];

                var v_shape = new ComplexShape(v_paths);
                v_shape.TransformPosition(Rect.MinMaxRect(p_sprite.bounds.min.x, p_sprite.bounds.min.y, p_sprite.bounds.max.x, p_sprite.bounds.max.y), new Rect(0, 0, 1, 1));
                return v_shape;
            }
            return null;
        }

        public static ComplexShape ShapeFromUVTexture(Texture2D p_texture, Rect p_uvRect, float p_detail = 1, float p_alphaTolerance = 0f, bool p_holeDetection = true)
        {
            if (p_texture != null)
            {
                var v_rect = new Rect(p_uvRect.position, p_uvRect.size);
                v_rect.position = new Vector2(p_uvRect.position.x * p_texture.width, p_uvRect.position.y * p_texture.height);
                v_rect.size = new Vector2(p_uvRect.size.x * p_texture.width, p_uvRect.size.y * p_texture.height);

                var v_sprite = Sprite.Create(p_texture, v_rect, new Vector2(0.5f, 0.5f));
                v_sprite.hideFlags = HideFlags.DontSave;
                var v_shape = ShapeFromSprite(v_sprite, p_detail, p_alphaTolerance, p_holeDetection);
                Object.DestroyImmediate(v_sprite);
                return v_shape;
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

#endif
