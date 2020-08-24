using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kyub.GraphMaker
{

	public class WMG_X_Stacked_Line : MonoBehaviour
	{

		public GameObject emptyGraphPrefab;
		public Canvas parentCanvas;

		WMG_Axis_Graph graph;
		int numSeries = 40;
		int maxNumPoints = 20;

		// Use this for initialization
		void Start()
		{
			GameObject graphGO = GameObject.Instantiate(emptyGraphPrefab);
			graphGO.transform.SetParent(this.transform, false);
			graph = graphGO.GetComponent<WMG_Axis_Graph>();

			graph.stretchToParent(graphGO);

			graph.Init();
			graph.graphType = WMG_Axis_Graph.graphTypes.line_stacked;
			graph.useGroups = true;

			List<string> groups = new List<string>() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };
			graph.groups.SetList(groups);

			for (int i = 0; i < numSeries; i++)
			{
				WMG_Series series = graph.addSeries();
				series.lineColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
				series.pointColor = series.lineColor;
				series.UseXDistBetweenToSpace = true;
				int numPoints = Random.Range(maxNumPoints / 2, maxNumPoints);
				series.pointValues.SetList(WMG_Util.GenRandomY(numPoints, maxNumPoints - numPoints + 1, maxNumPoints, graph.yAxis.AxisMinValue / numSeries, graph.yAxis.AxisMaxValue / numSeries));
			}
		}
	}
}
