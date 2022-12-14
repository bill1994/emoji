// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using System.Linq;

namespace MaterialUI
{
	public static class MaterialIconHelper
	{
		private static VectorImageFont m_Font;
		private static VectorImageSet m_IconSet;
		
		static MaterialIconHelper()
		{
			if (m_Font == null)
			{
				m_Font = VectorImageManager.GetIconFont(VectorImageManager.materialDesignIconsFontName);
			}
			
			if (m_IconSet == null)
			{
				m_IconSet = VectorImageManager.GetIconSet(VectorImageManager.materialDesignIconsFontName);
			}
		}

		public static ImageData GetIcon(MaterialIconEnum iconEnum)
		{
			return GetIcon(iconEnum.ToString().ToLower());
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
			return new ImageData(new VectorImageData(m_IconSet.iconGlyphList[Random.Range(0, m_IconSet.iconGlyphList.Count)], m_Font));
		}
	}
	
}