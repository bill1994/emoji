using UnityEngine;
using UnityEngine.UI;

namespace Kyub.UI
{
	[AddComponentMenu("Kyub.UI/Gradation UI")]
	public sealed class GradationUI : BaseMeshEffect
	{
		public Color[] colors = new Color[4]
		{
			Color.black,
			Color.white,
			Color.red,
			Color.black
		};

		public override void ModifyMesh(VertexHelper help)
		{
			_ModifyMesh(help);
		}

		private void _ModifyMesh(VertexHelper help)
		{
			if (this.IsActive() && help != null)
			{
				Rect pixelAdjustedRect = this.graphic.GetPixelAdjustedRect();
				UIVertex simpleVert = UIVertex.simpleVert;
				for (int i = 0; i < help.currentVertCount; i++)
				{
					help.PopulateUIVertex(ref simpleVert, i);
					Vector2 vector = Rect.PointToNormalized(pixelAdjustedRect, simpleVert.position);
					simpleVert.color = Color.Lerp(Color.Lerp(colors[0], colors[1], vector.y), Color.Lerp(colors[3], colors[2], vector.y), vector.x);
					simpleVert.color.a = byte.MaxValue;
					help.SetUIVertex(simpleVert, i);
				}
			}
		}

		public void UpdateColors()
		{
			_UpdateColors();
		}

		private void _UpdateColors()
		{
			this.graphic.SetVerticesDirty();
		}

		public GradationUI()
		{
		}
	}
}
