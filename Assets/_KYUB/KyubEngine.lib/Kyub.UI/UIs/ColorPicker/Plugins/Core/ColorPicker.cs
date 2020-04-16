using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kyub.UI
{
	[AddComponentMenu("Kyub.UI/Color Picker")]
	public sealed class ColorPicker : MonoBehaviour
	{
		[Serializable]
		public class UnityEvent_Color : UnityEvent<Color>
		{
		}

		[Serializable]
		public class UnityEvent_HSV : UnityEvent<HSV>
		{
		}

		public UnityEvent_Color OnChange;

		[HideInInspector]
		public UnityEvent_Color OnChange_Color;

		[HideInInspector]
		public UnityEvent_HSV OnChange_HSV;

		public HSV hsv = HSV.red;

		public float outputMultiplier = 1f;

		public Color color
		{
			get
			{
				return hsv;
			}
			set
			{
				HSV hSV = hsv;
				hsv = value;
				if (value == Color.black || value == Color.white)
				{
					hsv.h = hSV.h;
				}
			}
		}

		private void Start()
		{
			UpdateUI();
		}

		public void SetColorByColorCode(string code)
		{
			try
			{
				if (code.Length != 7 || code[0] != '#')
				{
					Debug.LogWarning("Can't get a color code.");
				}
				string value = code.Substring(1, 2);
				string value2 = code.Substring(3, 2);
				string value3 = code.Substring(5, 2);
				int num = Convert.ToInt32(value, 16);
				int num2 = Convert.ToInt32(value2, 16);
				int num3 = Convert.ToInt32(value3, 16);
				int num4 = num / 255;
				int num5 = num2 / 255;
				int num6 = num3 / 255;
				color = new Color(num4, num5, num6);
				UpdateUI();
			}
			catch (Exception ex)
			{
				Debug.Log(ex.Message);
			}
		}

		public void Show(Color color)
		{
			Show((HSV)color);
		}

		public void Show(HSV hsvColor)
		{
			hsv = hsvColor;
			UpdateUI();
			base.gameObject.SetActive(value: true);
		}

		public void UpdateUI()
		{
			Color color = this.color;
			if (OnChange != null)
			{
				OnChange.Invoke(color * outputMultiplier);
			}
			if (OnChange_Color != null)
			{
				OnChange_Color.Invoke(color);
			}
			if (OnChange_HSV != null)
			{
				OnChange_HSV.Invoke(hsv);
			}
		}
	}
}
