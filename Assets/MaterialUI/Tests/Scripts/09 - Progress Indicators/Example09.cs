// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.UI;
using MaterialUI;

public class Example09 : MonoBehaviour
{
	[SerializeField] private ProgressIndicator[] m_ProgressIndicatorArray = null;

	public void OnButtonShowClicked()
	{
		foreach (ProgressIndicator indicator in m_ProgressIndicatorArray)
		{
			indicator.Show();
		}
	}

	public void OnButtonHideClicked()
	{
		foreach (ProgressIndicator indicator in m_ProgressIndicatorArray)
		{
			indicator.Hide();
		}
	}

	public void OnButtonIndeterminateClicked()
	{
		foreach (ProgressIndicator indicator in m_ProgressIndicatorArray)
		{
			indicator.StartIndeterminate();
		}
	}

	public void OnButtonRandomProgressClicked()
	{
		float progress = Random.Range(0.0f, 1.0f);

		foreach (ProgressIndicator indicator in m_ProgressIndicatorArray)
		{
			indicator.SetProgress(progress, false);
		}
	}

	public void OnButtonRandomProgressAnimateClicked()
	{
		float progress = Random.Range(0.0f, 1.0f);

		foreach (ProgressIndicator indicator in m_ProgressIndicatorArray)
		{
			indicator.SetProgress(progress);
		}
	}

	public void OnButtonRandomColorClicked()
	{
		Color color = GetRandomColor();

		foreach (ProgressIndicator indicator in m_ProgressIndicatorArray)
		{
			indicator.SetColor(color);
		}
	}

	private Color GetRandomColor()
	{
		return new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
	}
}
