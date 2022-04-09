// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using MaterialUI;

public class Example13 : MonoBehaviour
{
	public void OnSimpleToastButtonClicked()
	{
		ToastManager.Show("Simple toast!");
	}

	public void OnCustomToastButtonClicked()
	{
		ToastManager.Show("Custom toast", 2.0f, GetRandomColor(), GetRandomColor(), Random.Range(12, 36));
	}

	public void OnSimpleSnackbarButtonClicked()
	{
        ToastManager.ShowSnackbar("Simple snackbar", "Action", () => { Debug.Log("Action clicked"); });
	}

	public void OnCustomSnackbarButtonClicked()
	{
        ToastManager.ShowSnackbar("Simple snackbar", 2.0f, GetRandomColor(), GetRandomColor(), Random.Range(12, 36), "Custom", () => { Debug.Log("Action clicked"); });
	}

	private Color GetRandomColor()
	{
		return new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
	}
}
