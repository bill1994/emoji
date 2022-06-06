using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI.Internal
{
	public class RectShapeUtils
	{
		static Vector3 tmpPos = Vector3.zero;
		static Vector2 tmpUVPos = Vector2.zero;

		public static void AddRect(
			ref VertexHelper vh,
			Rect pixelRect,
			Color32 color,
			Rect uvRect
		)
		{
			AddRectVertRing(
				ref vh,
				pixelRect,
				color,
				pixelRect,
				false,
				uvRect
			);

			AddRectQuadIndices(ref vh);
		}

		public static void AddRect(
			ref VertexHelper vh,
			Rect pixelRect,
			Color32 color,
			Rect uvRect,
			UIGeometryUtils.EdgeGradientData edgeGradientData
		)
		{
			var center = pixelRect.center;
			var width = pixelRect.width;
			var height = pixelRect.height;
			width += edgeGradientData.ShadowOffset * 2.0f;
			height += edgeGradientData.ShadowOffset * 2.0f;

			float innerOffset = Mathf.Min(width, height) * (1.0f - edgeGradientData.InnerScale);

			AddRectVertRing(
				ref vh,
				UIGeometryUtils.RectFromCenter(center, new Vector2(width - innerOffset, height - innerOffset)),
				color,
				pixelRect,
				false,
				uvRect
			);

			AddRectQuadIndices(ref vh);

			if (edgeGradientData.IsActive)
			{
				color.a = 0;

				UIGeometryUtils.AddOffset(
					ref width,
					ref height,
					edgeGradientData.SizeAdd
				);

				AddRectVertRing(
					ref vh,
					pixelRect,
					color,
					UIGeometryUtils.RectFromCenter(center, new Vector2(width - edgeGradientData.SizeAdd * 2.0f, height - edgeGradientData.SizeAdd * 2.0f)),
					true,
					uvRect
				);
			}
		}

		public static void AddRectRing(
			ref VertexHelper vh,
			UIGeometryUtils.OutlineProperties OutlineProperties,
			Rect pixelRect,
			Color32 color,
			Rect uvRect,
			UIGeometryUtils.EdgeGradientData edgeGradientData
		)
		{
			var center = pixelRect.center;
			var width = pixelRect.width;
			var height = pixelRect.height;
			byte alpha = color.a;

			float fullWidth = width + OutlineProperties.GetOuterDistace() * 2.0f;
			float fullHeight = height + OutlineProperties.GetOuterDistace() * 2.0f;

			width += OutlineProperties.GetCenterDistace() * 2.0f;
			height += OutlineProperties.GetCenterDistace() * 2.0f;

			float halfLineWeightOffset = OutlineProperties.HalfLineWeight * 2.0f + edgeGradientData.ShadowOffset;
			float halfLineWeightInnerOffset = halfLineWeightOffset * edgeGradientData.InnerScale;

			var fullRect = UIGeometryUtils.RectFromCenter(center, new Vector2(fullWidth, fullHeight));

			if (edgeGradientData.IsActive)
			{
				color.a = 0;

				AddRectVertRing(
					ref vh,
					UIGeometryUtils.RectFromCenter(center, new Vector2(width - halfLineWeightOffset - edgeGradientData.SizeAdd, height - halfLineWeightOffset - edgeGradientData.SizeAdd)),
					color,
					fullRect,
					false,
					uvRect
				);

				color.a = alpha;
			}



			AddRectVertRing(
				ref vh,
				UIGeometryUtils.RectFromCenter(center, new Vector2(width - halfLineWeightInnerOffset, height - halfLineWeightInnerOffset)),
				color,
				fullRect,
				edgeGradientData.IsActive,
				uvRect
			);

			AddRectVertRing(
				ref vh,
				UIGeometryUtils.RectFromCenter(center, new Vector2(width + halfLineWeightInnerOffset, height + halfLineWeightInnerOffset)),
				color,
				fullRect,
				true,
				uvRect
			);

			if (edgeGradientData.IsActive)
			{
				color.a = 0;

				AddRectVertRing(
					ref vh,
					UIGeometryUtils.RectFromCenter(center, new Vector2(width + halfLineWeightOffset + edgeGradientData.SizeAdd, height + halfLineWeightOffset + edgeGradientData.SizeAdd)),
					color,
					fullRect,
					true,
					uvRect
				);
			}
		}

		public static void AddRectVertRing(
			ref VertexHelper vh,
			Rect currentRect,
			Color32 color,
			Rect fullRect,
			bool addRingIndices,
			Rect uvRect)
		{
			var center = currentRect.center;
			var width = currentRect.width;
			var height = currentRect.height;

			// TL
			tmpPos.x = center.x - width * 0.5f;
			tmpPos.y = center.y + height * 0.5f;

			var normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
			tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
			vh.AddVert(tmpPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

			// TR
			tmpPos.x += width;

			normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
			tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
			vh.AddVert(tmpPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

			// BR
			tmpPos.y -= height;

			normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
			tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
			vh.AddVert(tmpPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

			// BL
			tmpPos.x -= width;

			normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
			tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
			vh.AddVert(tmpPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

			if (addRingIndices)
			{
				int baseIndex = vh.currentVertCount - 8;

				vh.AddTriangle(baseIndex + 4, baseIndex + 5, baseIndex);
				vh.AddTriangle(baseIndex, baseIndex + 5, baseIndex + 1);

				vh.AddTriangle(baseIndex + 1, baseIndex + 5, baseIndex + 6);
				vh.AddTriangle(baseIndex + 1, baseIndex + 6, baseIndex + 2);

				vh.AddTriangle(baseIndex + 2, baseIndex + 6, baseIndex + 7);
				vh.AddTriangle(baseIndex + 7, baseIndex + 3, baseIndex + 2);

				vh.AddTriangle(baseIndex + 4, baseIndex + 3, baseIndex + 7);
				vh.AddTriangle(baseIndex + 4, baseIndex, baseIndex + 3);
			}
		}

		public static void AddRectQuadIndices(
			ref VertexHelper vh
		)
		{
			int baseIndex = vh.currentVertCount - 4;

			vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 3);
			vh.AddTriangle(baseIndex + 3, baseIndex + 1, baseIndex + 2);
		}
	}
}
