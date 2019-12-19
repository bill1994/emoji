using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kyub.UI
{
	[ExecuteInEditMode]
	public class SliderColorChanger : MonoBehaviour 
	{
		#region Private Variables

		[SerializeField]
		Color m_emptyBarColor = Color.white;
		[SerializeField]
		Color m_fullBarColor = Color.white;

		#endregion

		#region Public Properties

		public Color EmptyBarColor
		{
			get
			{
				return m_emptyBarColor;
			}
			set
			{
				if(m_emptyBarColor == value)
					return;
				m_emptyBarColor = value;
			}
		}

		public Color FullBarColor
		{
			get
			{
				return m_fullBarColor;
			}
			set
			{
				if(m_fullBarColor == value)
					return;
				m_fullBarColor = value;
			}
		}

		#endregion

		#region Unity Functions
		
		protected virtual void Awake()
		{
			if(Application.isPlaying)
				RegisterEvents();
			else
				_oldSlider = GetSliderValue();
		}
		
		protected virtual void OnDestroy()
		{
			if(Application.isPlaying)
				UnregisterEvents();
		}
		
		protected virtual void Update()
		{
			if(!Application.isPlaying && Application.isEditor)
				ApplyColors();
		}
		
		#endregion
		
		#region Helper Functions
		
		public void RegisterEvents()
		{
			UnregisterEvents();
			Slider v_slider = GetComponent<Slider>();
			if(v_slider != null)
			{
				v_slider.onValueChanged.AddListener(OnSliderChanged);
			}
		}
		
		public void UnregisterEvents()
		{
			Slider v_slider = GetComponent<Slider>();
			if(v_slider != null)
			{
				v_slider.onValueChanged.RemoveListener(OnSliderChanged);
			}
		}

		float _oldSlider = 0;
		public void OnSliderChanged(float p_value)
		{
			ApplyColors(p_value);
		}
		
		protected void ApplyColors()
		{
			Slider v_slider = GetComponent<Slider>();
			if(v_slider != null)
			{
				_oldSlider = GetSliderValue();
				ApplyColors(_oldSlider);
			}
		}
		
		protected virtual void ApplyColors(float p_value)
		{
			Slider v_slider = GetComponent<Slider>();
			MaskableGraphic v_sliderFill = v_slider.fillRect != null? v_slider.fillRect.GetComponent<MaskableGraphic>() : null;
			if(v_sliderFill != null)
			{
				v_sliderFill.color = Color.Lerp(m_emptyBarColor, m_fullBarColor, p_value);
			}
		}
		
		protected virtual float GetSliderValue()
		{
			Slider v_slider = GetComponent<Slider>();
			if(v_slider != null)
				return v_slider.value;
			return 0f;
		}
		
		#endregion
	}
}
