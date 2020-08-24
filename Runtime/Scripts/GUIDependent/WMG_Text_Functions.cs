using UnityEngine;
using System.Collections;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

namespace Kyub.GraphMaker
{

	public class WMG_Text_Functions : MonoBehaviour
	{

		public enum WMGpivotTypes { Bottom, BottomLeft, BottomRight, Center, Left, Right, Top, TopLeft, TopRight };

#if TMP_PRESENT
		public void changeLabelText(GameObject obj, string aText)
		{
			TextMeshProUGUI theLabel = obj.GetComponent<TextMeshProUGUI>();
			theLabel.text = aText;
			refreshTextRectTransformSize(obj); // graph maker code should always change text after fontSize / fontStyle / font, so refresh called here only once
		}

		public void changeLabelFontSize(GameObject obj, int newFontSize)
		{
			TextMeshProUGUI theLabel = obj.GetComponent<TextMeshProUGUI>();
			theLabel.fontSize = newFontSize;
		}

		public void changeLabelFontStyle(GameObject obj, FontStyle newFontStyle)
		{
			TextMeshProUGUI theLabel = obj.GetComponent<TextMeshProUGUI>();
			if (newFontStyle == FontStyle.Bold)
			{
				theLabel.fontStyle = TMPro.FontStyles.Bold;
			}
			else if (newFontStyle == FontStyle.Italic)
			{
				theLabel.fontStyle = TMPro.FontStyles.Italic;
			}
			else
			{
				theLabel.fontStyle = TMPro.FontStyles.Normal;
			}
		}

		public void changeLabelFont(GameObject obj, TMP_FontAsset newFont)
		{
			TextMeshProUGUI theLabel = obj.GetComponent<TextMeshProUGUI>();
			theLabel.font = newFont;
		}

		public void refreshTextRectTransformSize(GameObject obj)
		{
			TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
			RectTransform textRt = obj.GetComponent<RectTransform>();
			text.ForceMeshUpdate();
			textRt.sizeDelta = new Vector2(text.preferredWidth, text.preferredHeight);
			text.ForceMeshUpdate();
			textRt.sizeDelta = new Vector2(text.preferredWidth, text.preferredHeight);
			text.ForceMeshUpdate();
		}

		public void changeSpritePivot(GameObject obj, WMGpivotTypes theType)
		{
			RectTransform theSprite = obj.GetComponent<RectTransform>();
			TextMeshProUGUI theText = obj.GetComponent<TextMeshProUGUI>();
			if (theSprite == null) return;
			if (theType == WMGpivotTypes.Bottom)
			{
				theSprite.pivot = new Vector2(0.5f, 0f);
				if (theText != null) theText.alignment = TextAlignmentOptions.Bottom;
			}
			else if (theType == WMGpivotTypes.BottomLeft)
			{
				theSprite.pivot = new Vector2(0f, 0f);
				if (theText != null) theText.alignment = TextAlignmentOptions.BottomLeft;
			}
			else if (theType == WMGpivotTypes.BottomRight)
			{
				theSprite.pivot = new Vector2(1f, 0f);
				if (theText != null) theText.alignment = TextAlignmentOptions.BottomRight;
			}
			else if (theType == WMGpivotTypes.Center)
			{
				theSprite.pivot = new Vector2(0.5f, 0.5f);
				if (theText != null) theText.alignment = TextAlignmentOptions.Center;
			}
			else if (theType == WMGpivotTypes.Left)
			{
				theSprite.pivot = new Vector2(0f, 0.5f);
				if (theText != null) theText.alignment = TextAlignmentOptions.Left;
			}
			else if (theType == WMGpivotTypes.Right)
			{
				theSprite.pivot = new Vector2(1f, 0.5f);
				if (theText != null) theText.alignment = TextAlignmentOptions.Right;
			}
			else if (theType == WMGpivotTypes.Top)
			{
				theSprite.pivot = new Vector2(0.5f, 1f);
				if (theText != null) theText.alignment = TextAlignmentOptions.Top;
			}
			else if (theType == WMGpivotTypes.TopLeft)
			{
				theSprite.pivot = new Vector2(0f, 1f);
				if (theText != null) theText.alignment = TextAlignmentOptions.TopLeft;
			}
			else if (theType == WMGpivotTypes.TopRight)
			{
				theSprite.pivot = new Vector2(1f, 1f);
				if (theText != null) theText.alignment = TextAlignmentOptions.TopRight;
			}
		}

		public void changeLabelColor(GameObject obj, Color newColor)
		{
			TextMeshProUGUI theLabel = obj.GetComponent<TextMeshProUGUI>();
			theLabel.color = newColor;
		}

#else

	public void changeLabelText(GameObject obj, string aText) {
		Text theLabel = obj.GetComponent<Text>();
		theLabel.text = aText;
		refreshTextRectTransformSize (obj); // graph maker code should always change text after fontSize / fontStyle / font, so refresh called here only once
	}
	
	public void changeLabelFontSize(GameObject obj, int newFontSize) {
		Text theLabel = obj.GetComponent<Text>();
		theLabel.fontSize = newFontSize;
	}

	public void changeLabelFontStyle(GameObject obj, FontStyle newFontStyle) {
		Text theLabel = obj.GetComponent<Text>();
		theLabel.fontStyle = newFontStyle;
	}
	
	public void changeLabelFont(GameObject obj, Font newFont) {
		Text theLabel = obj.GetComponent<Text>();
		theLabel.font = newFont;
	}

	public void refreshTextRectTransformSize(GameObject obj) {
		RectTransform textRt = obj.GetComponent<RectTransform>();
		textRt.sizeDelta = getTextSizePreferred(obj);
	}

	//http://answers.unity3d.com/questions/921726/how-to-get-the-size-of-a-unityengineuitext-for-whi.html
	Vector2 getTextSizePreferred (GameObject obj) {
		Text textComp = obj.GetComponent<Text> ();
		return new Vector2 (textComp.cachedTextGeneratorForLayout.GetPreferredWidth (
			textComp.text, textComp.GetGenerationSettings (textComp.GetComponent<RectTransform> ().rect.size)),
		                    textComp.cachedTextGeneratorForLayout.GetPreferredHeight (
			textComp.text, textComp.GetGenerationSettings (textComp.GetComponent<RectTransform> ().rect.size)));
	}

	public void changeSpritePivot(GameObject obj, WMGpivotTypes theType) {
		RectTransform theSprite = obj.GetComponent<RectTransform>();
		Text theText = obj.GetComponent<Text>();
		if (theSprite == null) return;
		if (theType == WMGpivotTypes.Bottom) {
			theSprite.pivot = new Vector2(0.5f, 0f);
			if (theText != null) theText.alignment = TextAnchor.LowerCenter;
		}
		else if (theType == WMGpivotTypes.BottomLeft) {
			theSprite.pivot = new Vector2(0f, 0f);
			if (theText != null) theText.alignment = TextAnchor.LowerLeft;
		}
		else if (theType == WMGpivotTypes.BottomRight) {
			theSprite.pivot = new Vector2(1f, 0f);
			if (theText != null) theText.alignment = TextAnchor.LowerRight;
		}
		else if (theType == WMGpivotTypes.Center) {
			theSprite.pivot = new Vector2(0.5f, 0.5f);
			if (theText != null) theText.alignment = TextAnchor.MiddleCenter;
		}
		else if (theType == WMGpivotTypes.Left) {
			theSprite.pivot = new Vector2(0f, 0.5f);
			if (theText != null) theText.alignment = TextAnchor.MiddleLeft;
		}
		else if (theType == WMGpivotTypes.Right) {
			theSprite.pivot = new Vector2(1f, 0.5f);
			if (theText != null) theText.alignment = TextAnchor.MiddleRight;
		}
		else if (theType == WMGpivotTypes.Top) {
			theSprite.pivot = new Vector2(0.5f, 1f);
			if (theText != null) theText.alignment = TextAnchor.UpperCenter;
		}
		else if (theType == WMGpivotTypes.TopLeft) {
			theSprite.pivot = new Vector2(0f, 1f);
			if (theText != null) theText.alignment = TextAnchor.UpperLeft;
		}
		else if (theType == WMGpivotTypes.TopRight) {
			theSprite.pivot = new Vector2(1f, 1f);
			if (theText != null) theText.alignment = TextAnchor.UpperRight;
		}
	}

	public void changeLabelColor(GameObject obj, Color newColor) {
		Text theLabel = obj.GetComponent<Text>();
		theLabel.color = newColor;
	}
#endif
	}
}
