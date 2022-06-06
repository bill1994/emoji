using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI.Internal
{
	public class EllipseShapeUtils
	{
		[System.Serializable]
		public class EllipseProperties
		{
			public enum EllipseFitting
			{
				Ellipse,
				UniformInner,
				UniformOuter
			}

			public enum ResolutionType
			{
				Calculated,
				Fixed
			}

			public EllipseFitting Fitting = EllipseFitting.UniformInner;

			public float BaseAngle = 0.0f;

			public ResolutionType Resolution = ResolutionType.Calculated;
			public int FixedResolution = 50;
			public float ResolutionMaxDistance = 4.0f;

			public int AdjustedResolution { private set; get; }

			public void OnCheck()
			{
				FixedResolution = Mathf.Max(FixedResolution, 3);
				ResolutionMaxDistance = Mathf.Max(ResolutionMaxDistance, 0.1f);
			}

			public void UpdateAdjusted(Vector2 radius, float offset)
			{
				radius.x += offset;
				radius.y += offset;

				switch (Resolution)
				{
					case ResolutionType.Calculated:
						float circumference;

						if (radius.x == radius.y)
						{
							circumference = UIGeometryUtils.TwoPI * radius.x;
						}
						else
						{
							circumference = Mathf.PI * (
								3.0f * (radius.x + radius.y) -
								Mathf.Sqrt(
									(3.0f * radius.x + radius.y) *
									(radius.x + 3.0f * radius.y)
								)
							);
						}

						AdjustedResolution = Mathf.CeilToInt(circumference / ResolutionMaxDistance);
						break;
					case ResolutionType.Fixed:
						AdjustedResolution = FixedResolution;
						break;
					default:
						throw new System.ArgumentOutOfRangeException ();
				}
			}
		}

		static Vector3 tmpVertPos = Vector3.zero;
		static Vector2 tmpUVPos = Vector2.zero;
		static Vector3 tmpInnerRadius = Vector3.one;
		static Vector3 tmpOuterRadius = Vector3.one;

		public static void SetRadius(
			ref Vector2 radius,
			float width,
			float height,
			EllipseProperties properties
		) {
			width *= 0.5f;
			height *= 0.5f;

			switch (properties.Fitting)
			{
				case EllipseProperties.EllipseFitting.UniformInner:
					radius.x = Mathf.Min(width, height);
					radius.y = radius.x;
					break;
				case EllipseProperties.EllipseFitting.UniformOuter:
					radius.x = Mathf.Max(width, height);
					radius.y = radius.x;
					break;
				case EllipseProperties.EllipseFitting.Ellipse:
					radius.x = width;
					radius.y = height;
					break;
			}
		}

		public static void AddCircle(
			ref VertexHelper vh,
			Rect pixelRect,
			EllipseProperties ellipseProperties,
			Color32 color,
			Rect fullRect,
			Rect uvRect,
			ref UIGeometryUtils.UnitPositionData unitPositionData,
			UIGeometryUtils.EdgeGradientData edgeGradientData
		) {

			UIGeometryUtils.SetUnitPositionData(
				ref unitPositionData,
				ellipseProperties.AdjustedResolution,
				ellipseProperties.BaseAngle
			);

			var center = pixelRect.center;
			var radius = new Vector2(pixelRect.size.x / 2, pixelRect.size.y / 2);
			int numVertices = vh.currentVertCount;

			var normalizedTmpPos = Rect.PointToNormalized(fullRect, center);
			tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
			vh.AddVert(center, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

			// add first circle vertex
			tmpVertPos.x = center.x + unitPositionData.UnitPositions[0].x * (radius.x + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale;
			tmpVertPos.y = center.y + unitPositionData.UnitPositions[0].y * (radius.y + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale;
			tmpVertPos.z = 0.0f;

			normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
			tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
			vh.AddVert(tmpVertPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

			for (int i = 1; i < ellipseProperties.AdjustedResolution; i++)
			{
				tmpVertPos.x = center.x + unitPositionData.UnitPositions[i].x * (radius.x + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale;
				tmpVertPos.y = center.y + unitPositionData.UnitPositions[i].y * (radius.y + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale;
				tmpVertPos.z = 0.0f;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
				tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
				vh.AddVert(tmpVertPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

				vh.AddTriangle(numVertices, numVertices + i, numVertices + i + 1);
			}

			vh.AddTriangle(numVertices, numVertices + ellipseProperties.AdjustedResolution, numVertices + 1);

			if (edgeGradientData.IsActive)
			{
				radius.x += edgeGradientData.ShadowOffset + edgeGradientData.SizeAdd;
				radius.y += edgeGradientData.ShadowOffset + edgeGradientData.SizeAdd;

				int outerFirstIndex = numVertices + ellipseProperties.AdjustedResolution;

				color.a = 0;

				// add first point
				tmpVertPos.x = center.x + unitPositionData.UnitPositions[0].x * radius.x;
				tmpVertPos.y = center.y + unitPositionData.UnitPositions[0].y * radius.y;
				tmpVertPos.z = 0.0f;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
				tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
				vh.AddVert(tmpVertPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

				for (int i = 1; i < ellipseProperties.AdjustedResolution; i++)
				{
					tmpVertPos.x = center.x + unitPositionData.UnitPositions[i].x * radius.x;
					tmpVertPos.y = center.y + unitPositionData.UnitPositions[i].y * radius.y;
					tmpVertPos.z = 0.0f;

					normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
					tmpUVPos = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
					vh.AddVert(tmpVertPos, color, tmpUVPos, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

					vh.AddTriangle(numVertices + i + 1, outerFirstIndex + i, outerFirstIndex + i + 1);
					vh.AddTriangle(numVertices + i + 1, outerFirstIndex + i + 1, numVertices + i + 2);
				}

				vh.AddTriangle(numVertices + 1, outerFirstIndex, outerFirstIndex + 1);
				vh.AddTriangle(numVertices + 2, numVertices + 1, outerFirstIndex + 1);
			}
		}



		public static void AddRing(
			ref VertexHelper vh,
			Rect pixelRect,
			UIGeometryUtils.OutlineProperties outlineProperties,
			EllipseProperties ellipseProperties,
			Color32 color,
			Rect fullRect,
			Rect uvRect,
			ref UIGeometryUtils.UnitPositionData unitPositionData,
			UIGeometryUtils.EdgeGradientData edgeGradientData
		) {
			UIGeometryUtils.SetUnitPositionData(
				ref unitPositionData,
				ellipseProperties.AdjustedResolution,
				ellipseProperties.BaseAngle
			);

			float halfLineWeightOffset = (outlineProperties.HalfLineWeight + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale;
			var center = pixelRect.center;
			var radius = new Vector2(pixelRect.size.x /2, pixelRect.size.y / 2);

			tmpInnerRadius.x = radius.x + outlineProperties.GetCenterDistace() - halfLineWeightOffset;
			tmpInnerRadius.y = radius.y + outlineProperties.GetCenterDistace() - halfLineWeightOffset;

			tmpOuterRadius.x = radius.x + outlineProperties.GetCenterDistace() + halfLineWeightOffset;
			tmpOuterRadius.y = radius.y + outlineProperties.GetCenterDistace() + halfLineWeightOffset;

			int numVertices = vh.currentVertCount;
			int startVertex = numVertices - 1;

			int baseIndex;

			float uvMaxResolution = (float)ellipseProperties.AdjustedResolution;

			Vector2 uv;
			Vector2 normalizedTmpPos;

			for (int i = 0; i < ellipseProperties.AdjustedResolution; i++)
			{
				uv.x = i / uvMaxResolution;

				tmpVertPos.x = center.x + unitPositionData.UnitPositions[i].x * tmpInnerRadius.x;
				tmpVertPos.y = center.y + unitPositionData.UnitPositions[i].y * tmpInnerRadius.y;
				tmpVertPos.z = 0.0f;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
				uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

				vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

				tmpVertPos.x = center.x + unitPositionData.UnitPositions[i].x * tmpOuterRadius.x;
				tmpVertPos.y = center.y + unitPositionData.UnitPositions[i].y * tmpOuterRadius.y;
				tmpVertPos.z = 0.0f;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
				uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);
				vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

				if (i > 0)
				{
					baseIndex = startVertex + i * 2;
					vh.AddTriangle(baseIndex - 1, baseIndex, baseIndex + 1);
					vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 1);
				}
			}

			// add last quad
			{
				tmpVertPos.x = center.x + unitPositionData.UnitPositions[0].x * tmpInnerRadius.x;
				tmpVertPos.y = center.y + unitPositionData.UnitPositions[0].y * tmpInnerRadius.y;
				tmpVertPos.z = 0.0f;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
				uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

				vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

				tmpVertPos.x = center.x + unitPositionData.UnitPositions[0].x * tmpOuterRadius.x;
				tmpVertPos.y = center.y + unitPositionData.UnitPositions[0].y * tmpOuterRadius.y;
				tmpVertPos.z = 0.0f;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
				uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

				vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

				baseIndex = startVertex + ellipseProperties.AdjustedResolution * 2;
				vh.AddTriangle(baseIndex - 1, baseIndex, baseIndex + 1);
				vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 1);
			}

			if (edgeGradientData.IsActive)
			{
				halfLineWeightOffset = outlineProperties.HalfLineWeight + edgeGradientData.ShadowOffset + edgeGradientData.SizeAdd;

				tmpInnerRadius.x = radius.x + outlineProperties.GetCenterDistace() - halfLineWeightOffset;
				tmpInnerRadius.y = radius.y + outlineProperties.GetCenterDistace() - halfLineWeightOffset;

				tmpOuterRadius.x = radius.x + outlineProperties.GetCenterDistace() + halfLineWeightOffset;
				tmpOuterRadius.y = radius.y + outlineProperties.GetCenterDistace() + halfLineWeightOffset;

				color.a = 0;

				int edgesBaseIndex;
				int innerBaseIndex;

				for (int i = 0; i < ellipseProperties.AdjustedResolution; i++)
				{
					tmpVertPos.x = center.x + unitPositionData.UnitPositions[i].x * tmpInnerRadius.x;
					tmpVertPos.y = center.y + unitPositionData.UnitPositions[i].y * tmpInnerRadius.y;
					tmpVertPos.z = 0.0f;

					normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
					uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

					vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

					tmpVertPos.x = center.x + unitPositionData.UnitPositions[i].x * tmpOuterRadius.x;
					tmpVertPos.y = center.y + unitPositionData.UnitPositions[i].y * tmpOuterRadius.y;
					tmpVertPos.z = 0.0f;

					normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
					uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

					vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

					edgesBaseIndex = baseIndex + i * 2;
					innerBaseIndex = startVertex + i * 2;

					if (i > 0)
					{
						// inner quad
						vh.AddTriangle(innerBaseIndex - 1, innerBaseIndex + 1, edgesBaseIndex + 3);
						vh.AddTriangle(edgesBaseIndex + 1, innerBaseIndex - 1, edgesBaseIndex + 3);

						// outer quad
						vh.AddTriangle(innerBaseIndex, edgesBaseIndex + 2, innerBaseIndex + 2);
						vh.AddTriangle(edgesBaseIndex + 2, edgesBaseIndex + 4, innerBaseIndex + 2);
					}
				}

				// add last quads
				{
					tmpVertPos.x = center.x + unitPositionData.UnitPositions[0].x * tmpInnerRadius.x;
					tmpVertPos.y = center.y + unitPositionData.UnitPositions[0].y * tmpInnerRadius.y;
					tmpVertPos.z = 0.0f;

					normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
					uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

					vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

					tmpVertPos.x = center.x + unitPositionData.UnitPositions[0].x * tmpOuterRadius.x;
					tmpVertPos.y = center.y + unitPositionData.UnitPositions[0].y * tmpOuterRadius.y;
					tmpVertPos.z = 0.0f;

					normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpVertPos);
					uv = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

					vh.AddVert(tmpVertPos, color, uv, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

					edgesBaseIndex = baseIndex + ellipseProperties.AdjustedResolution * 2;
					innerBaseIndex = startVertex + ellipseProperties.AdjustedResolution * 2;

					// inner quad
					vh.AddTriangle(innerBaseIndex - 1, innerBaseIndex + 1, edgesBaseIndex + 3);
					vh.AddTriangle(edgesBaseIndex + 1, innerBaseIndex - 1, edgesBaseIndex + 3);

					// outer quad
					vh.AddTriangle(innerBaseIndex, edgesBaseIndex + 2, innerBaseIndex + 2);
					vh.AddTriangle(edgesBaseIndex + 2, edgesBaseIndex + 4, innerBaseIndex + 2);
				}
			}
		}

	}
}
