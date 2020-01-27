//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using MaterialUI;
using System.Linq;
using UnityEngine.UI;

public class Example10 : MonoBehaviour
{
	[SerializeField] private Graphic m_VectorImage = null;

	public void OnIconNameButtonClicked()
	{
		m_VectorImage.SetImageData(MaterialIconHelper.GetIcon("volume_off"));
		//m_VectorImage.SetImageData(GetIconFromIconFont("FontAwesome", "gift"));
	}

	public void OnIconEnumButtonClicked()
	{
		m_VectorImage.SetImageData(MaterialIconHelper.GetIcon(MaterialIconEnum.SHOPPING_CART));
	}

	public void OnIconRandomButtonClicked()
	{
		m_VectorImage.SetImageData(MaterialIconHelper.GetRandomIcon());
	}

	// If you want to get the icon from a icon font you downloaded:
	private ImageData GetIconFromIconFont(string fontName, string iconName)
	{
		VectorImageFont vectorFont = VectorImageManager.GetIconFont(fontName);
        if (vectorFont != null)
        {
            var glyph = vectorFont.GetGlyphByName(iconName);
            if (glyph == null)
            {
                Debug.LogError("Could not find an icon with the name: " + name + " inside the " + fontName + " icon font");
                return null;
            }
            return new ImageData(new VectorImageData(glyph, vectorFont));
        }

        return null;
	}
}
