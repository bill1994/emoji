// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaterialUI
{
    [Serializable]
    public class VectorImageSet
    {
		[SerializeField]
		private List<Glyph> m_IconGlyphList = null;
		public List<Glyph> iconGlyphList
		{
			get { return m_IconGlyphList; }
			set { m_IconGlyphList = value; }
		}

		public VectorImageSet()
		{
			m_IconGlyphList = new List<Glyph>();
		}
    }
}
