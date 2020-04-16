using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kyub.UI
{
	[AddComponentMenu("Kyub.UI/Color Input Field")]
	[RequireComponent(typeof(InputField))]
	public sealed class ColorInputField : MonoBehaviour
	{
		public enum ColorType
		{
			Red,
			Green,
			Blue
		}

		public ColorType Property;

		private InputField inputField;

		private ColorPicker _colorPicker;

		private ColorPicker colorPicker
		{
			get
			{
				if (_colorPicker == null)
				{
					_colorPicker = GetComponentInParent<ColorPicker>();
				}
				return _colorPicker;
			}
		}

		private void Awake()
		{
			inputField = GetComponent<InputField>();
			if (colorPicker == null)
			{
				Debug.LogError("There is no ColorPicker in parents.");
			}
		}

		private void Start()
		{
			colorPicker.OnChange_HSV.AddListener(UpdateUI);
			((UnityEvent<string>)inputField.onEndEdit).AddListener((UnityAction<string>)endEdit);
		}

		private void endEdit(string s)
		{
			if (!string.IsNullOrEmpty(s))
			{
				float num = Convert.ToSingle(s);
				num /= 255f;
				num = Mathf.Clamp01(num);
				Color color = colorPicker.color;
				switch (Property)
				{
				case ColorType.Red:
					color.r = num;
					break;
				case ColorType.Green:
					color.g = num;
					break;
				case ColorType.Blue:
					color.b = num;
					break;
				}
				colorPicker.color = color;
				colorPicker.UpdateUI();
			}
		}

		private void UpdateUI(HSV hsv)
		{
			switch (Property)
			{
			case ColorType.Red:
				inputField.text = (((Color)hsv).r * 255f).ToString("F0");
				break;
			case ColorType.Green:
                    inputField.text = (((Color)hsv).g * 255f).ToString("F0");
				break;
			case ColorType.Blue:
                    inputField.text = (((Color)hsv).b * 255f).ToString("F0");
				break;
			}
		}
	}
}
