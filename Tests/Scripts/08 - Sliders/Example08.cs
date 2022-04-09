// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.UI;

public class Example08 : MonoBehaviour
{
	[SerializeField] private Image m_RgbImage = null;
	[SerializeField] private Slider m_SliderR = null;
	[SerializeField] private Slider m_SliderG = null;
	[SerializeField] private Slider m_SliderB = null;
	[SerializeField] private Slider m_SliderA = null;

	void Awake()
	{
		OnSliderValueChanged();
	}

	public void OnSliderValueChanged()
	{
		UpdateRGBImage();
	}

	private void UpdateRGBImage()
	{
		m_RgbImage.color = new Color(m_SliderR.value/255f, m_SliderG.value/255f, m_SliderB.value/255f, m_SliderA.value);
	}
}
