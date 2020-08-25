using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kyub.UI
{
	[AddComponentMenu("Kyub.UI/Gradation Rect")]
	public sealed class GradationRect : BaseMeshEffect, IPointerDownHandler, IDragHandler, IEventSystemHandler
	{
		public enum ControlType
		{
			SV_both,
			Hue_vertical,
			Red_horizontal,
			Green_horizontal,
			Blue_horizontal
		}

		public ControlType type;

		public Color[] colors = new Color[4]
		{
			Color.black,
			Color.white,
			Color.red,
			Color.black
		};

		public RectTransform colorPointer;

		private ColorPicker _colorPicker;

		private Canvas _canvas;

		private RectTransform rect;

		private ColorPicker colorPicker
		{
			get
			{
				if (_colorPicker == null)
				{
					_colorPicker = base.GetComponentInParent<ColorPicker>();
				}
				return _colorPicker;
			}
		}

		private Canvas canvas
		{
			get
			{
				if (_canvas == null)
				{
					_canvas = base.GetComponentInParent<Canvas>();
				}
				return _canvas;
			}
		}

		protected override void Awake()
		{
			if (Application.isPlaying)
			{
				base.Awake();
				rect = base.GetComponent<RectTransform>();
			}
		}

		protected override void Start()
		{
            base.Start();
			colorPicker.OnChange_HSV.AddListener(UpdateUI);
		}

		private void UpdateUI(HSV hsv)
		{
			switch (type)
			{
			case ControlType.SV_both:
			{
				HSV[] array4 = new HSV[4]
				{
					hsv,
					hsv,
					hsv,
					hsv
				};
				array4[0].s = (array4[1].s = 0f);
				array4[2].s = (array4[3].s = 1f);
				array4[0].v = (array4[3].v = 0f);
				array4[1].v = (array4[2].v = 1f);
				colors = new Color[4]
				{
					array4[0],
					array4[1],
					array4[2],
					array4[3]
				};
				UpdateColors();
				break;
			}
			case ControlType.Red_horizontal:
			{
				Color color3 = hsv;
				Color[] array3 = new Color[4]
				{
					color3,
					color3,
					color3,
					color3
				};
				array3[0].r = (array3[1].r = 0f);
				array3[2].r = (array3[3].r = 1f);
				colors = array3;
				UpdateColors();
				break;
			}
			case ControlType.Green_horizontal:
			{
				Color color2 = hsv;
				Color[] array2 = new Color[4]
				{
					color2,
					color2,
					color2,
					color2
				};
				array2[0].g = (array2[1].g = 0f);
				array2[2].g = (array2[3].g = 1f);
				colors = array2;
				UpdateColors();
				break;
			}
			case ControlType.Blue_horizontal:
			{
				Color color = hsv;
				Color[] array = new Color[4]
				{
					color,
					color,
					color,
					color
				};
				array[0].b = (array[1].b = 0f);
				array[2].b = (array[3].b = 1f);
				colors = array;
				UpdateColors();
				break;
			}
			}
			if (colorPointer != null)
			{
				float num = 0.5f;
				float num2 = 0.5f;
				switch (type)
				{
				case ControlType.SV_both:
					num = hsv.s;
					num2 = hsv.v;
					break;
				case ControlType.Hue_vertical:
					num2 = hsv.h;
					break;
				case ControlType.Red_horizontal:
				{
					Color color6 = hsv;
					num = color6.r;
					break;
				}
				case ControlType.Green_horizontal:
				{
					Color color5 = hsv;
					num = color5.g;
					break;
				}
				case ControlType.Blue_horizontal:
				{
					Color color4 = hsv;
					num = color4.b;
					break;
				}
				}
				num = (num - rect.pivot.x) * rect.rect.width;
				num2 = (num2 - rect.pivot.y) * rect.rect.height;
				colorPointer.localPosition = new Vector2(num, num2);
			}
		}

		public void OnPointerDown(PointerEventData e)
		{
			_OnDrag(e);
		}

		public void OnDrag(PointerEventData e)
		{
			_OnDrag(e);
		}

		private void _OnDrag(PointerEventData e)
		{
			Vector2 localPoint;
			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Kyub.EventSystems.InputCompat.mousePosition, null, out localPoint);
			}
			else if (canvas.worldCamera != null)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Kyub.EventSystems.InputCompat.mousePosition, canvas.worldCamera, out localPoint);
			}
			else
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Kyub.EventSystems.InputCompat.mousePosition, Camera.main, out localPoint);
			}
			localPoint.x += rect.rect.width * rect.pivot.x;
			localPoint.y += rect.rect.height * rect.pivot.y;
			localPoint.x /= rect.rect.width;
			localPoint.y /= rect.rect.height;
			localPoint.x = Mathf.Clamp01(localPoint.x);
			localPoint.y = Mathf.Clamp01(localPoint.y);
			Color color = Color.white;
			switch (type)
			{
			case ControlType.SV_both:
				colorPicker.hsv.s = localPoint.x;
				colorPicker.hsv.v = localPoint.y;
				break;
			case ControlType.Hue_vertical:
				colorPicker.hsv.h = localPoint.y;
				break;
			case ControlType.Red_horizontal:
				color = colorPicker.color;
				color.r = localPoint.x;
				colorPicker.color = color;
				break;
			case ControlType.Green_horizontal:
				color = colorPicker.color;
				color.g = localPoint.x;
				colorPicker.color = color;
				break;
			case ControlType.Blue_horizontal:
				color = colorPicker.color;
				color.b = localPoint.x;
				colorPicker.color = color;
				break;
			}
			colorPicker.UpdateUI();
		}

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

		public GradationRect()
		{
		}
	}
}
