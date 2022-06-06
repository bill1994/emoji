using UnityEngine;
using UnityEngine.UI;
using static Kyub.UI.Internal.UIGeometryUtils;

using RoundedCornerProperties = Kyub.UI.Internal.UIGeometryUtils.RoundingProperties;

namespace Kyub.UI.Internal
{
	public class RoundedRectShapeUtils
	{
		[System.Serializable]
		public struct RoundedCornerUnitPositionData
		{
			public Vector2[] TLUnitPositions;
			public Vector2[] TRUnitPositions;
			public Vector2[] BRUnitPositions;
			public Vector2[] BLUnitPositions;
		}

		static void SetCornerUnitPositions(
			RoundedProperties roundedProperties,
			ref RoundedCornerUnitPositionData cornerUnitPositions
		)
		{
			SetUnitPosition(ref cornerUnitPositions.TLUnitPositions, roundedProperties.TLResolution.AdjustedResolution, UIGeometryUtils.HalfPI + Mathf.PI, roundedProperties.TLResolution.MakeSharpCorner);
			SetUnitPosition(ref cornerUnitPositions.TRUnitPositions, roundedProperties.TRResolution.AdjustedResolution, 0.0f, roundedProperties.TRResolution.MakeSharpCorner);
			SetUnitPosition(ref cornerUnitPositions.BRUnitPositions, roundedProperties.BRResolution.AdjustedResolution, UIGeometryUtils.HalfPI, roundedProperties.BRResolution.MakeSharpCorner);
			SetUnitPosition(ref cornerUnitPositions.BLUnitPositions, roundedProperties.BLResolution.AdjustedResolution, Mathf.PI, roundedProperties.BLResolution.MakeSharpCorner);
		}

		static void SetUnitPosition(
			ref Vector2[] unitPositions,
			int resolution,
			float baseAngle,
			bool makeSharpCorner
		)
		{
			bool needsUpdate = false;

			if (
				unitPositions == null ||
				unitPositions.Length != resolution
			)
			{
				unitPositions = new Vector2[resolution];

				for (int i = 0; i < unitPositions.Length; i++)
				{
					unitPositions[i] = UIGeometryUtils.ZeroV2;
				}

				needsUpdate = true;
			}

			if (needsUpdate)
			{
				float angleIncrement = UIGeometryUtils.HalfPI / ((float)resolution - 1.0f);
				float angle;

				if (makeSharpCorner)
				{
					angle = baseAngle + UIGeometryUtils.HalfPI * 0.5f;
					float length = Mathf.Sqrt(2.0f);

					for (int i = 0; i < resolution; i++)
					{
						unitPositions[i].x = Mathf.Sin(angle) * length;
						unitPositions[i].y = Mathf.Cos(angle) * length;
					}
				}
				else
				{
					for (int i = 0; i < resolution; i++)
					{
						angle = baseAngle + angleIncrement * i;

						unitPositions[i].x = Mathf.Sin(angle);
						unitPositions[i].y = Mathf.Cos(angle);
					}
				}
			}
		}

		[System.Serializable]
		public class RoundedProperties
		{
			public enum RoundedType
			{
				None,
				Uniform,
				Individual
			}

			public enum ResolutionType
			{
				Uniform,
				Individual
			}

			public RoundedType Type = RoundedType.None;
			public ResolutionType ResolutionMode = ResolutionType.Uniform;

			public float UniformRadius = 15.0f;
			public bool UseMaxRadius = false;

			public float TLRadius = 15.0f;
			public RoundedCornerProperties TLResolution = new RoundedCornerProperties();

			public float TRRadius = 15.0f;
			public RoundedCornerProperties TRResolution = new RoundedCornerProperties();

			public float BRRadius = 15.0f;
			public RoundedCornerProperties BRResolution = new RoundedCornerProperties();

			public float BLRadius = 15.0f;
			public RoundedCornerProperties BLResolution = new RoundedCornerProperties();

			public RoundedCornerProperties UniformResolution = new RoundedCornerProperties();

			//			public int Resolution = 15;


			public float AdjustedTLRadius { get; private set; }
			public float AdjustedTRRadius { get; private set; }
			public float AdjustedBRRadius { get; private set; }
			public float AdjustedBLRadius { get; private set; }

			public void UpdateAdjusted(Rect rect, float offset)
			{
				switch (Type)
				{
					case RoundedType.Uniform:
						if (UseMaxRadius)
						{
							AdjustedTLRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
							AdjustedTRRadius = AdjustedTLRadius;
							AdjustedBRRadius = AdjustedTLRadius;
							AdjustedBLRadius = AdjustedTLRadius;
						}
						else
						{
							AdjustedTLRadius = UniformRadius;
							AdjustedTRRadius = AdjustedTLRadius;
							AdjustedBRRadius = AdjustedTLRadius;
							AdjustedBLRadius = AdjustedTLRadius;
						}
						break;
					case RoundedType.Individual:
						AdjustedTLRadius = TLRadius;
						AdjustedTRRadius = TRRadius;
						AdjustedBRRadius = BRRadius;
						AdjustedBLRadius = BLRadius;
						break;
					case RoundedType.None:
						AdjustedTLRadius = 0.0f;
						AdjustedTRRadius = AdjustedTLRadius;
						AdjustedBRRadius = AdjustedTLRadius;
						AdjustedBLRadius = AdjustedTLRadius;
						break;
					default:
						throw new System.ArgumentOutOfRangeException();
				}

				if (ResolutionMode == ResolutionType.Uniform)
				{
					TLResolution.UpdateAdjusted(AdjustedTLRadius, offset, UniformResolution, 4.0f);
					TRResolution.UpdateAdjusted(AdjustedTRRadius, offset, UniformResolution, 4.0f);
					BRResolution.UpdateAdjusted(AdjustedBRRadius, offset, UniformResolution, 4.0f);
					BLResolution.UpdateAdjusted(AdjustedBLRadius, offset, UniformResolution, 4.0f);
				}
				else
				{
					TLResolution.UpdateAdjusted(AdjustedTLRadius, offset, 4.0f);
					TRResolution.UpdateAdjusted(AdjustedTRRadius, offset, 4.0f);
					BRResolution.UpdateAdjusted(AdjustedBRRadius, offset, 4.0f);
					BLResolution.UpdateAdjusted(AdjustedBLRadius, offset, 4.0f);
				}
			}

			public void OnCheck(Rect rect)
			{
				float shorterSide = Mathf.Min(rect.width, rect.height);
				float halfShorterSide = shorterSide * 0.5f;

				// check radii don't overlap
				switch (Type)
				{
					case RoundedType.Uniform:
						UniformRadius = Mathf.Clamp(UniformRadius, 0.0f, halfShorterSide);
						break;
					case RoundedType.Individual:
						TLRadius = Mathf.Clamp(TLRadius, 0.0f, halfShorterSide);
						TRRadius = Mathf.Clamp(TRRadius, 0.0f, halfShorterSide);
						BRRadius = Mathf.Clamp(BRRadius, 0.0f, halfShorterSide);
						BLRadius = Mathf.Clamp(BLRadius, 0.0f, halfShorterSide);
						break;
				}

				TLResolution.OnCheck();
				TRResolution.OnCheck();
				BRResolution.OnCheck();
				BLResolution.OnCheck();

				UniformResolution.OnCheck();
			}
		}

		static Vector3 tmpV3 = Vector3.zero;
		static Vector3 tmpPos = Vector3.zero;
		static Vector2 tmpUV = Vector2.zero;

		public static void AddRoundedRect(
			ref VertexHelper vh,
			Rect pixelRect,
			RoundedProperties roundedProperties,
			Color32 color,
			Rect uvRect,
			ref RoundedCornerUnitPositionData cornerUnitPositions,
			EdgeGradientData edgeGradientData
		)
		{
			var center = pixelRect.center;
			var width = pixelRect.width;
			var height = pixelRect.height;

			if (roundedProperties.Type == RoundedProperties.RoundedType.None)
			{
				RectShapeUtils.AddRect(
					ref vh,
					pixelRect,
					color,
					uvRect,
					edgeGradientData
				);

				return;
			}


			SetCornerUnitPositions(
				roundedProperties,
				ref cornerUnitPositions
			);

			int numVertices = vh.currentVertCount;

			tmpUV.x = 0.5f;
			tmpUV.y = 0.5f;

			vh.AddVert(center, color, tmpUV, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);

			float sizeSub = Mathf.Min(height, width);
			sizeSub *= 1.0f - edgeGradientData.InnerScale;

			AddRoundedRectVerticesRing(
				ref vh,
				UIGeometryUtils.RectFromCenter(center, new Vector2(width - sizeSub, height - sizeSub)),
				UIGeometryUtils.RectFromCenter(center, new Vector2(width - sizeSub, height - sizeSub)),
				roundedProperties.AdjustedTLRadius * edgeGradientData.InnerScale,
				(roundedProperties.AdjustedTLRadius + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale,
				roundedProperties.AdjustedTRRadius * edgeGradientData.InnerScale,
				(roundedProperties.AdjustedTRRadius + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale,
				roundedProperties.AdjustedBRRadius * edgeGradientData.InnerScale,
				(roundedProperties.AdjustedBRRadius + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale,
				roundedProperties.AdjustedBLRadius * edgeGradientData.InnerScale,
				(roundedProperties.AdjustedBLRadius + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale,
				cornerUnitPositions,
				color,
				uvRect,
				false
			);


			// set indices
			int numNewVertices = vh.currentVertCount - numVertices;
			for (int i = 0; i < numNewVertices - 1; i++)
			{
				vh.AddTriangle(numVertices, numVertices + i, numVertices + i + 1);
			}

			// set last triangle
			vh.AddTriangle(numVertices, vh.currentVertCount - 1, numVertices + 1);


			if (edgeGradientData.IsActive)
			{
				float outerRadiusMod = 0.0f;// = roundedProperties.AdjustedRadius;
				outerRadiusMod += edgeGradientData.ShadowOffset;
				outerRadiusMod += edgeGradientData.SizeAdd;

				color.a = 0;

				AddRoundedRectVerticesRing(
					ref vh,
					pixelRect,
					pixelRect,
					roundedProperties.AdjustedTLRadius,
					roundedProperties.AdjustedTLRadius + outerRadiusMod,
					roundedProperties.AdjustedTRRadius,
					roundedProperties.AdjustedTRRadius + outerRadiusMod,
					roundedProperties.AdjustedBRRadius,
					roundedProperties.AdjustedBRRadius + outerRadiusMod,
					roundedProperties.AdjustedBLRadius,
					roundedProperties.AdjustedBLRadius + outerRadiusMod,
					cornerUnitPositions,
					color,
					uvRect,
					true
				);
			}
		}

		public static void AddRoundedRectLine(
			ref VertexHelper vh,
			Rect pixelRect,
			UIGeometryUtils.OutlineProperties outlineProperties,
			RoundedProperties roundedProperties,
			Color32 color,
			Rect uvRect,
			ref RoundedCornerUnitPositionData cornerUnitPositions,
			UIGeometryUtils.EdgeGradientData edgeGradientData
		)
		{
			var center = pixelRect.center;
			var width = pixelRect.width;
			var height = pixelRect.height;
			float fullWidth = width + outlineProperties.GetOuterDistace() * 2.0f;
			float fullHeight = height + outlineProperties.GetOuterDistace() * 2.0f;
			var fullRect = UIGeometryUtils.RectFromCenter(center, new Vector2(fullWidth, fullHeight));

			if (roundedProperties.Type == RoundedProperties.RoundedType.None)
			{
				RectShapeUtils.AddRectRing(
					ref vh,
					outlineProperties,
					pixelRect,
					color,
					uvRect,
					edgeGradientData
				);

				return;
			}

			SetCornerUnitPositions(
				roundedProperties,
				ref cornerUnitPositions
			);

			float outerRadiusMod;

			byte alpha = color.a;

			if (edgeGradientData.IsActive)
			{
				color.a = 0;

				outerRadiusMod =
					outlineProperties.GetCenterDistace() - outlineProperties.HalfLineWeight - edgeGradientData.ShadowOffset;
				outerRadiusMod -= edgeGradientData.SizeAdd;

				AddRoundedRectVerticesRing(
					ref vh,
					pixelRect,
					fullRect,
					roundedProperties.AdjustedTLRadius,
					roundedProperties.AdjustedTLRadius + outerRadiusMod,
					roundedProperties.AdjustedTRRadius,
					roundedProperties.AdjustedTRRadius + outerRadiusMod,
					roundedProperties.AdjustedBRRadius,
					roundedProperties.AdjustedBRRadius + outerRadiusMod,
					roundedProperties.AdjustedBLRadius,
					roundedProperties.AdjustedBLRadius + outerRadiusMod,
					cornerUnitPositions,
					color,
					uvRect,
					false
				);

				color.a = alpha;
			}

			outerRadiusMod =
				Mathf.LerpUnclamped(
					outlineProperties.GetCenterDistace(),
					outlineProperties.GetCenterDistace() - outlineProperties.HalfLineWeight - edgeGradientData.ShadowOffset,
					edgeGradientData.InnerScale);

			AddRoundedRectVerticesRing(
				ref vh,
				pixelRect,
				fullRect,
				roundedProperties.AdjustedTLRadius,
				roundedProperties.AdjustedTLRadius + outerRadiusMod,
				roundedProperties.AdjustedTRRadius,
				roundedProperties.AdjustedTRRadius + outerRadiusMod,
				roundedProperties.AdjustedBRRadius,
				roundedProperties.AdjustedBRRadius + outerRadiusMod,
				roundedProperties.AdjustedBLRadius,
				roundedProperties.AdjustedBLRadius + outerRadiusMod,
				cornerUnitPositions,
				color,
				uvRect,
				edgeGradientData.IsActive
			);

			outerRadiusMod =
				outlineProperties.GetCenterDistace() +
				(outlineProperties.HalfLineWeight + edgeGradientData.ShadowOffset) * edgeGradientData.InnerScale;

			AddRoundedRectVerticesRing(
				ref vh,
				pixelRect,
				fullRect,
				roundedProperties.AdjustedTLRadius,
				roundedProperties.AdjustedTLRadius + outerRadiusMod,
				roundedProperties.AdjustedTRRadius,
				roundedProperties.AdjustedTRRadius + outerRadiusMod,
				roundedProperties.AdjustedBRRadius,
				roundedProperties.AdjustedBRRadius + outerRadiusMod,
				roundedProperties.AdjustedBLRadius,
				roundedProperties.AdjustedBLRadius + outerRadiusMod,
				cornerUnitPositions,
				color,
				uvRect,
				true
			);

			if (edgeGradientData.IsActive)
			{
				outerRadiusMod =
					outlineProperties.GetCenterDistace() +
					outlineProperties.HalfLineWeight + edgeGradientData.ShadowOffset;
				outerRadiusMod += edgeGradientData.SizeAdd;

				color.a = 0;

				AddRoundedRectVerticesRing(
					ref vh,
					pixelRect,
					fullRect,
					roundedProperties.AdjustedTLRadius,
					roundedProperties.AdjustedTLRadius + outerRadiusMod,
					roundedProperties.AdjustedTRRadius,
					roundedProperties.AdjustedTRRadius + outerRadiusMod,
					roundedProperties.AdjustedBRRadius,
					roundedProperties.AdjustedBRRadius + outerRadiusMod,
					roundedProperties.AdjustedBLRadius,
					roundedProperties.AdjustedBLRadius + outerRadiusMod,
					cornerUnitPositions,
					color,
					uvRect,
					true
				);
			}
		}

		static void AddRoundedRectVerticesRing(
			ref VertexHelper vh,
			Rect pixelRect,
			Rect fullRect,
			float tlRadius,
			float tlOuterRadius,
			float trRadius,
			float trOuterRadius,
			float brRadius,
			float brOuterRadius,
			float blRadius,
			float blOuterRadius,
			RoundedCornerUnitPositionData cornerUnitPositions,
			Color32 color,
			Rect uvRect,
			bool addIndices
		)
		{
			var center = pixelRect.center;
			var width = pixelRect.width;
			var height = pixelRect.height;

			float xMin = center.x - width * 0.5f;
			float yMin = center.y - height * 0.5f;

			float xMax = center.x + width * 0.5f;
			float yMax = center.y + height * 0.5f;

			// TR
			tmpV3.x = xMax - trRadius;
			tmpV3.y = yMax - trRadius;

			Vector2 normalizedTmpPos = Vector2.zero;
			if (trOuterRadius < 0.0f)
			{
				tmpV3.x += trOuterRadius;
				tmpV3.y += trOuterRadius;

				trOuterRadius = 0.0f;
			}

			for (int i = 0; i < cornerUnitPositions.TRUnitPositions.Length; i++)
			{
				tmpPos.x = tmpV3.x + cornerUnitPositions.TRUnitPositions[i].x * trOuterRadius;
				tmpPos.y = tmpV3.y + cornerUnitPositions.TRUnitPositions[i].y * trOuterRadius;
				tmpPos.z = tmpV3.z;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
				tmpUV = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

				vh.AddVert(tmpPos, color, tmpUV, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);
			}

			// BR
			tmpV3.x = xMax - brRadius;
			tmpV3.y = yMin + brRadius;

			if (brOuterRadius < 0.0f)
			{
				tmpV3.x += brOuterRadius;
				tmpV3.y -= brOuterRadius;

				brOuterRadius = 0.0f;
			}

			for (int i = 0; i < cornerUnitPositions.BRUnitPositions.Length; i++)
			{
				tmpPos.x = tmpV3.x + cornerUnitPositions.BRUnitPositions[i].x * brOuterRadius;
				tmpPos.y = tmpV3.y + cornerUnitPositions.BRUnitPositions[i].y * brOuterRadius;
				tmpPos.z = tmpV3.z;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
				tmpUV = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

				vh.AddVert(tmpPos, color, tmpUV, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);
			}

			// BL
			tmpV3.x = xMin + blRadius;
			tmpV3.y = yMin + blRadius;

			if (blOuterRadius < 0.0f)
			{
				tmpV3.x -= blOuterRadius;
				tmpV3.y -= blOuterRadius;

				blOuterRadius = 0.0f;
			}

			for (int i = 0; i < cornerUnitPositions.BLUnitPositions.Length; i++)
			{
				tmpPos.x = tmpV3.x + cornerUnitPositions.BLUnitPositions[i].x * blOuterRadius;
				tmpPos.y = tmpV3.y + cornerUnitPositions.BLUnitPositions[i].y * blOuterRadius;
				tmpPos.z = tmpV3.z;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
				tmpUV = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

				vh.AddVert(tmpPos, color, tmpUV, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);
			}

			// TL
			tmpV3.x = xMin + tlRadius;
			tmpV3.y = yMax - tlRadius;

			if (tlOuterRadius < 0.0f)
			{
				tmpV3.x -= tlOuterRadius;
				tmpV3.y += tlOuterRadius;

				tlOuterRadius = 0.0f;
			}

			for (int i = 0; i < cornerUnitPositions.TLUnitPositions.Length; i++)
			{
				tmpPos.x = tmpV3.x + cornerUnitPositions.TLUnitPositions[i].x * tlOuterRadius;
				tmpPos.y = tmpV3.y + cornerUnitPositions.TLUnitPositions[i].y * tlOuterRadius;
				tmpPos.z = tmpV3.z;

				normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
				tmpUV = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

				vh.AddVert(tmpPos, color, tmpUV, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);
			}


			// add last circle vertex
			tmpPos.x = tmpV3.x + cornerUnitPositions.TRUnitPositions[0].x * tlOuterRadius;
			tmpPos.y = tmpV3.y + cornerUnitPositions.TRUnitPositions[0].y * tlOuterRadius;
			tmpPos.z = tmpV3.z;

			normalizedTmpPos = Rect.PointToNormalized(fullRect, tmpPos);
			tmpUV = Rect.NormalizedToPoint(uvRect, normalizedTmpPos);

			vh.AddVert(tmpPos, color, tmpUV, UIGeometryUtils.ZeroV2, UIGeometryUtils.UINormal, UIGeometryUtils.UITangent);


			if (addIndices)
			{
				AddRoundedRingIndices(
					ref vh,
					cornerUnitPositions
				);
			}
		}

		static void AddRoundedRingIndices(
			ref VertexHelper vh,
			RoundedCornerUnitPositionData cornerUnitPositions
		)
		{
			int totalResolution =
				cornerUnitPositions.TLUnitPositions.Length +
				cornerUnitPositions.TRUnitPositions.Length +
				cornerUnitPositions.BRUnitPositions.Length +
				cornerUnitPositions.BLUnitPositions.Length;

			int numNewVertices = totalResolution + 1;

			int innerStartIndex = vh.currentVertCount - numNewVertices - numNewVertices - 1;
			int outerStartIndex = vh.currentVertCount - numNewVertices;

			for (int i = 0; i < totalResolution; i++)
			{
				vh.AddTriangle(innerStartIndex + i + 1, outerStartIndex + i, outerStartIndex + i + 1);
				vh.AddTriangle(innerStartIndex + i + 1, outerStartIndex + i + 1, innerStartIndex + i + 2);
			}

			vh.AddTriangle(innerStartIndex + 1, outerStartIndex + totalResolution, outerStartIndex);
			vh.AddTriangle(innerStartIndex + 1, outerStartIndex - 1, outerStartIndex + totalResolution);
		}
	}
}
