// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using System;

namespace MaterialUI
{
    public class Snackbar : Toast
    {
        private string m_ActionName;
		public string actionName
		{
			get { return m_ActionName; }
			set { m_ActionName = value; }
		}

        private Action m_OnActionButtonClicked;
		public Action onActionButtonClicked
		{
			get { return m_OnActionButtonClicked; }
			set { m_OnActionButtonClicked = value; }
		}

        public Snackbar(string content, float duration, Color? panelColor, Color? textColor, int? fontSize, string actionName, Action onActionButtonClicked, string assetPath = "") : base(content, duration, panelColor, textColor, fontSize, assetPath)
        {
            m_ActionName = actionName;
            m_OnActionButtonClicked = onActionButtonClicked;
        }
    }
}