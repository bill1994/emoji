// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;

namespace MaterialUI
{
    public class Toast
    {
        private string m_AssetPath;
        public string assetPath
        {
            get { return m_AssetPath; }
            set { m_AssetPath = value; }
        }

        private string m_Content;
		public string content
		{
			get { return m_Content; }
			set { m_Content = value; }
		}

        private float m_Duration;
		public float duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

        private Color? m_PanelColor;
		public Color? panelColor
		{
			get { return m_PanelColor; }
			set { m_PanelColor = value; }
		}

        private Color? m_TextColor;
		public Color? textColor
		{
			get { return m_TextColor; }
			set { m_TextColor = value; }
		}

        private int? m_FontSize;
		public int? fontSize
		{
			get { return m_FontSize; }
			set { m_FontSize = value; }
		}

        public Toast(string content, float duration, string assetPath = "")
        {
            m_AssetPath = assetPath;
            m_Content = content;
            m_Duration = duration;
            m_PanelColor = null;
            m_TextColor = null;
            m_FontSize = null;
        }

        public Toast(string content, float duration, Color? panelColor, Color? textColor, int? fontSize, string assetPath = "")
        {
            m_AssetPath = assetPath;
            m_Content = content;
            m_Duration = duration;
            m_PanelColor = panelColor;
            m_TextColor = textColor;
            m_FontSize = fontSize;
        }

        public bool IsCustomToast()
        {
            return m_PanelColor != null || m_TextColor != null || m_FontSize != null;
        }
    }
}