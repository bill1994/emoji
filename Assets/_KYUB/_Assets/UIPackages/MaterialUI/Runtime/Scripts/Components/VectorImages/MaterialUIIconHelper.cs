//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using System.Linq;

namespace MaterialUI
{
	public static class MaterialUIIconHelper
    {
		private static VectorImageFont m_Font;
		
		static MaterialUIIconHelper()
		{
			if (m_Font == null)
			{
				m_Font = VectorImageManager.GetIconFont(VectorImageManager.materialUIIconsFontName);
			}
		}

		public static ImageData GetIcon(string name)
		{
            Glyph glyph = m_Font != null ? m_Font.GetGlyphByName(name) : null;

            if (glyph == null)
			{
				Debug.LogError("Could not find an icon with the name: " + name + " inside the MaterialDesign icon font");
				return null;
			}

			return new ImageData(new VectorImageData(glyph, m_Font));
		}

		public static ImageData GetRandomIcon()
		{
			return new ImageData(new VectorImageData(m_Font.iconSet.iconGlyphList[Random.Range(0, m_Font.iconSet.iconGlyphList.Count)], m_Font));
		}
	}
	
}