using UnityEngine;
using System.Collections;

namespace Kyub.GraphMaker
{

	public class WMG_X_WorldSpace : MonoBehaviour
	{

		public WMG_Axis_Graph graph;
		public GameObject canvasGO;
		public GameObject tooltipPrefab;

		// Use this for initialization
		void Start()
		{
			graph.Init(); // ensure graph Start() method called before this Start() method
			GameObject toolTipPanel = GameObject.Instantiate(tooltipPrefab);
			toolTipPanel.transform.SetParent(canvasGO.transform, false);
			GameObject toolTipLabel = toolTipPanel.transform.GetChild(0).gameObject;
			graph.theTooltip.SetTooltipObject(toolTipPanel, toolTipLabel);
		}
	}
}
