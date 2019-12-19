using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Sprites;

namespace Kyub.UI
{
	[AddComponentMenu("Kyub/UI/Better Image", 11)]
	public class BetterImage : Image
	{
		#region Private Variables

		[SerializeField]
		bool m_cropImage = false;

		#endregion

		#region Public Properties

		public bool CropImage
		{
			get
			{
				return m_cropImage;
			}
			set
			{
				if(m_cropImage == value)
					return;
				m_cropImage = value;
			}
		}

		#endregion

		#region Helper Functions

		#if !UNITY_5_0 && !UNITY_5_1 && !UNITY_4
		
		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			if(this.overrideSprite == null)
			{
				base.OnPopulateMesh(toFill);
				return;
			}
			else
			{
				switch (type)
				{
				case Type.Simple:
					this.GenerateSimpleSprite (toFill, preserveAspect);
					break;
				case Type.Sliced:
					base.OnPopulateMesh(toFill);
					break;
				case Type.Tiled:
					base.OnPopulateMesh(toFill);
					break;
				case Type.Filled:
					base.OnPopulateMesh(toFill);
					break;
				}
			}
		}

		protected virtual void GenerateSimpleSprite(VertexHelper vh, bool lPreserveAspect)
		{
			Vector4 v = GetDrawingDimensions(lPreserveAspect, CropImage);
			var uv = (overrideSprite != null) ? DataUtility.GetOuterUV(overrideSprite) : Vector4.zero;

			//Recalc Crop
			Vector4 v_lostUv = GetPercentLost(preserveAspect, CropImage);
			Vector4 v_tempUv = Vector4.zero;
			v_tempUv.x += (uv.z - uv.x)* v_lostUv.x;
			v_tempUv.y += (uv.w - uv.y)* v_lostUv.y;
			v_tempUv.z = (uv.z - uv.x) * v_lostUv.z;
			v_tempUv.w = (uv.w - uv.y) * v_lostUv.w;

			//SetBack to vector UV
			uv = v_tempUv;

			var color32 = color;
			vh.Clear();
			vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y), new Vector2(0,0), Vector3.zero, Vector4.zero);
			vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w), new Vector2(0,1), Vector3.zero, Vector4.zero);
			vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w), new Vector2(1,1), Vector3.zero, Vector4.zero);
			vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y), new Vector2(1,0), Vector3.zero, Vector4.zero);
			
			vh.AddTriangle(0, 1, 2);
			vh.AddTriangle(2, 3, 0);
		}
		
		#endif
		
		#if UNITY_5_0 || UNITY_5_1 || UNITY_4
		protected override void OnFillVBO (List<UIVertex> vbo)
		{

			if(this.overrideSprite == null)
			{
				base.OnFillVBO(vbo);
				return;
			}
			else
			{
				switch (type)
				{
					case Type.Simple:
						this.GenerateSimpleSprite (vbo, preserveAspect);
						break;
					case Type.Sliced:
						base.OnFillVBO(vbo);
						break;
					case Type.Tiled:
						base.OnFillVBO(vbo);
						break;
					case Type.Filled:
						base.OnFillVBO(vbo);
						break;
				}
			}
		}

		private void GenerateSimpleSprite (List<UIVertex> vbo, bool preserveAspect)
		{
			UIVertex simpleVert = UIVertex.simpleVert;
			simpleVert.color = base.color;

			Vector4 drawingDimensions = this.GetDrawingDimensions (preserveAspect, CropImage);
			Vector4 uv = (!(this.overrideSprite != null)) ? Vector4.zero : DataUtility.GetOuterUV (this.overrideSprite);

			//Recalc Crop
			Vector4 v_lostUv = GetPercentLost(preserveAspect, CropImage);
			Vector4 v_tempUv = Vector4.zero;
			v_tempUv.x += (uv.z - uv.x)* v_lostUv.x;
			v_tempUv.y += (uv.w - uv.y)* v_lostUv.y;
			v_tempUv.z = (uv.z - uv.x) * v_lostUv.z;
			v_tempUv.w = (uv.w - uv.y) * v_lostUv.w;

			//SetBack to vector UV
			uv = v_tempUv;

			simpleVert.position = new Vector3 (drawingDimensions.x, drawingDimensions.y);
			simpleVert.uv0 = new Vector2 (uv.x, uv.y);
			simpleVert.uv1 = new Vector2(0,0);
			vbo.Add (simpleVert);
			simpleVert.position = new Vector3 (drawingDimensions.x, drawingDimensions.w);
			simpleVert.uv0 = new Vector2 (uv.x, uv.w);
			simpleVert.uv1 = new Vector2(0,1);
			vbo.Add (simpleVert);
			simpleVert.position = new Vector3 (drawingDimensions.z, drawingDimensions.w);
			simpleVert.uv0 = new Vector2 (uv.z, uv.w);
			simpleVert.uv1 = new Vector2(1,1);
			vbo.Add (simpleVert);
			simpleVert.position = new Vector3 (drawingDimensions.z, drawingDimensions.y);
			simpleVert.uv0 = new Vector2 (uv.z, uv.y);
			simpleVert.uv1 = new Vector2(1,0);
			vbo.Add (simpleVert);
		}

		#endif

		protected virtual Vector4 GetPercentLost(bool shouldPreserveAspect, bool shouldCropImage)
		{
			Vector4 v_lost = new Vector4(0,0,1,1);
			var size = overrideSprite == null ? Vector2.zero : new Vector2(overrideSprite.rect.width, overrideSprite.rect.height);
			Rect r = GetPixelAdjustedRect();
			if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
			{
				var rectRatio = r.width / r.height;

				if(shouldCropImage)
				{
					if (size.y > size.x)
					{
						float v_newSizeY =  rectRatio == 0? 0 : size.x / rectRatio;
						float v_percentLost = size.y == 0? 0 : Math.Max(0, (size.y - v_newSizeY)/size.y);
						v_lost.y += v_percentLost*rectTransform.pivot.y; 
						v_lost.w -= v_lost.y;
					}
					else
					{
						float v_newSizeX = rectRatio * size.y;
						float v_percentLost = size.x == 0? 0 : Math.Max(0, (size.x - v_newSizeX)/size.x);
						v_lost.x += v_percentLost*rectTransform.pivot.x; 
						v_lost.z -= v_lost.x;
					}
				}
			}
			return v_lost;
		}

		protected virtual Vector4 GetDrawingDimensions(bool shouldPreserveAspect, bool shouldCropImage)
		{
			var padding = overrideSprite == null ? Vector4.zero : DataUtility.GetPadding(overrideSprite);
			var size = overrideSprite == null ? Vector2.zero : new Vector2(overrideSprite.rect.width, overrideSprite.rect.height);
			
			Rect r = GetPixelAdjustedRect();
			
			int spriteW = Mathf.RoundToInt(size.x);
			int spriteH = Mathf.RoundToInt(size.y);
			
			var v = new Vector4(
				padding.x / spriteW,
				padding.y / spriteH,
				(spriteW - padding.z) / spriteW,
				(spriteH - padding.w) / spriteH);
			
			if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
			{
				var spriteRatio = size.x / size.y;
				var rectRatio = r.width / r.height;
				if(!shouldCropImage)
				{
					if (spriteRatio > rectRatio)
					{
						var oldHeight = r.height;
						r.height = r.width * (1.0f / spriteRatio);
						r.y += (oldHeight - r.height) * rectTransform.pivot.y;
					}
					else
					{
						var oldWidth = r.width;
						r.width = r.height * spriteRatio;
						r.x += (oldWidth - r.width) * rectTransform.pivot.x;
					}
				}
				/*else
				{
					if (size.y > size.x)
					{
						float v_newSizeY =  rectRatio == 0? 0 : size.x / rectRatio;
						float v_percentLost = size.y == 0? 0 : Math.Max(0, (size.y - v_newSizeY)/size.y);
						var oldHeight = r.height;
						r.height = r.height*(1+v_percentLost);
						r.y += (oldHeight - r.height) * rectTransform.pivot.y;
					}
					else
					{
						float v_newSizeX = rectRatio * size.y;
						float v_percentLost = size.x == 0? 0 : Math.Max(0, (size.x - v_newSizeX)/size.x);
						var oldWidth = r.width;
						r.width = r.width*(1+v_percentLost);
						r.x += (oldWidth - r.width) * rectTransform.pivot.x;
					}
				}*/
			}
			
			v = new Vector4(
				r.x + r.width * v.x,
				r.y + r.height * v.y,
				r.x + r.width * v.z,
				r.y + r.height * v.w
				);
			
			return v;
		}

		#endregion
	}
}