using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Kyub.GraphMaker
{

	public class WMG_X_Large_Scatter : MonoBehaviour
	{

		public GameObject emptyGraphPrefab;
		public Canvas parentCanvas;

		int numPointsToCreate = 3000;
		int numPointsPerCanvas = 200;
		WMG_Axis_Graph graph;
		WMG_Series series1;
		List<GameObject> pointCanvases = new List<GameObject>();

		// Use this for initialization
		void Start()
		{
			GameObject graphGO = GameObject.Instantiate(emptyGraphPrefab);
			graphGO.transform.SetParent(this.transform, false);
			graph = graphGO.GetComponent<WMG_Axis_Graph>();

			graph.stretchToParent(graphGO);

			graph.xAxis.AxisMinValue = 0;
			graph.yAxis.AxisMinValue = 0;
			graph.xAxis.AxisMaxValue = 100;
			graph.yAxis.AxisMaxValue = 100;
			graph.legend.hideLegend = true;
			graph.xAxis.SetLabelsUsingMaxMin = true;
			graph.xAxis.LabelType = WMG_Axis.labelTypes.ticks;

			graph.autoAnimationsEnabled = true;

			series1 = graph.addSeries();
			series1.pointColor = new Color(210 / 255f, 100 / 255f, 100 / 255f, 1);
			series1.PointCreated += groupPointsInCanvases;
			series1.neverCreateLines = true;
			series1.pointValues.SetList(WMG_Util.GenRandomXY(numPointsToCreate, 0, 100, 0, 100));

			Canvas graphBgCanvas = graph.graphBackground.transform.parent.gameObject.AddComponent<Canvas>();
			graph.graphBackground.transform.parent.gameObject.AddComponent<GraphicRaycaster>();
			graphBgCanvas.overrideSorting = true;
			graphBgCanvas.sortingOrder = 0;

			graph.toolTipPanel.SetActive(true); // for some reason setting canvas override sorting doesn't work for inactive gameobject, so enable and then set back to disable
			Canvas tooltipCanvas = graph.toolTipPanel.AddComponent<Canvas>(); // otherwise tooltip appears behind points which are on their own canvas of higher sorting order
			tooltipCanvas.overrideSorting = true;
			tooltipCanvas.sortingLayerID = 0;
			tooltipCanvas.sortingOrder = 2 + ((numPointsToCreate - 1) / numPointsPerCanvas);
			graph.toolTipPanel.SetActive(false);
		}

		void groupPointsInCanvases(WMG_Series series, GameObject point, int pointIndex)
		{
			int currentNumCanvases = pointCanvases.Count;
			int canvasNumForThisPoint = 1 + pointIndex / numPointsPerCanvas;
			if (pointIndex % numPointsPerCanvas == 0)
			{
				if (canvasNumForThisPoint > currentNumCanvases)
				{
					GameObject newCanvas = new GameObject();
					newCanvas.name = "Point Canvas " + canvasNumForThisPoint;
					newCanvas.transform.SetParent(series.nodeParent.transform, false);
					newCanvas.AddComponent<RectTransform>();
					Canvas canv = newCanvas.AddComponent<Canvas>();
					newCanvas.AddComponent<GraphicRaycaster>();
					canv.overrideSorting = true;
					canv.sortingOrder = canvasNumForThisPoint;
					pointCanvases.Add(newCanvas);
					point.transform.SetParent(newCanvas.transform, false);
				}
			}
			else
			{
				point.transform.SetParent(pointCanvases[canvasNumForThisPoint - 1].transform, false);
			}
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.A))
			{
				List<Vector2> randomList = WMG_Util.GenRandomXY(200, 0, 100, 0, 100);
				for (int i = 0; i < randomList.Count; i++)
				{
					series1.pointValues[Random.Range(0, numPointsToCreate)] = randomList[i];
				}
			}
			if (Input.GetKeyDown(KeyCode.B))
			{
				List<Vector2> randomList = WMG_Util.GenRandomXY(1, 0, 100, 0, 100);
				for (int i = 0; i < randomList.Count; i++)
				{
					series1.pointValues[i] = randomList[i];
				}
			}
		}
	}
}