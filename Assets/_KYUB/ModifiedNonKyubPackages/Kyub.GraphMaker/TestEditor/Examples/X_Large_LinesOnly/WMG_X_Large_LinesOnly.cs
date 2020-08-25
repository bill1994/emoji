using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Kyub.GraphMaker
{

	public class WMG_X_Large_LinesOnly : MonoBehaviour
	{

		public GameObject emptyGraphPrefab;
		public Canvas parentCanvas;

		int numPointsToCreate = 2000;
		int numPointsPerCanvas = 100;
		WMG_Axis_Graph graph;
		WMG_Series series1;
		List<GameObject> pointCanvases = new List<GameObject>();
		List<GameObject> lineCanvases = new List<GameObject>();
		bool useComputeShader = true;
		bool drawAreaShading = false;

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
			series1.hidePoints = true;
			series1.lineScale = 0.4f;
			if (useComputeShader)
			{
				series1.areaShadingTextureResolution = 4096;
				if (drawAreaShading)
				{
					series1.areaShadingType = WMG_Series.areaShadingTypes.Gradient;
					series1.areaShadingUsesComputeShader = true;
					series1.areaShadingColor = Color.blue;
					series1.areaShadingAxisValue = 0;
				}
				series1.linesUseComputeShader = true;
				series1.neverCreateLines = true;
				series1.neverCreatePoints = true;
			}
			else
			{
				series1.LineCreated += groupLinesInCanvases;
				series1.PointCreated += groupPointsInCanvases;
			}
			series1.pointValues.SetList(WMG_Util.GenRandomY(numPointsToCreate, 0, 100, 0, 100));

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
					newCanvas.AddComponent<Canvas>();
					//				newCanvas.AddComponent<GraphicRaycaster>();
					pointCanvases.Add(newCanvas);
					point.transform.SetParent(newCanvas.transform, false);
				}
			}
			else
			{
				point.transform.SetParent(pointCanvases[canvasNumForThisPoint - 1].transform, false);
			}
		}

		void groupLinesInCanvases(WMG_Series series, GameObject line, int lineIndex)
		{
			int currentNumCanvases = lineCanvases.Count;
			int canvasNumForThisPoint = 1 + lineIndex / numPointsPerCanvas;
			if (lineIndex % numPointsPerCanvas == 0)
			{
				if (canvasNumForThisPoint > currentNumCanvases)
				{
					GameObject newCanvas = new GameObject();
					newCanvas.name = "Line Canvas " + canvasNumForThisPoint;
					newCanvas.transform.SetParent(series.linkParent.transform, false);
					newCanvas.AddComponent<RectTransform>();
					newCanvas.AddComponent<Canvas>();
					//				newCanvas.AddComponent<GraphicRaycaster>();
					lineCanvases.Add(newCanvas);
					line.transform.SetParent(newCanvas.transform, false);
				}
			}
			else
			{
				line.transform.SetParent(lineCanvases[canvasNumForThisPoint - 1].transform, false);
			}
		}

		void Update()
		{
			if (EventSystems.InputCompat.GetKeyDown(KeyCode.A))
			{
				List<float> randomList = WMG_Util.GenRandomList(200, 0, 100);
				for (int i = 0; i < randomList.Count; i++)
				{
					int xIndex = Random.Range(0, numPointsToCreate);
					series1.pointValues[xIndex] = new Vector2(series1.pointValues[xIndex].x, randomList[i]);
				}
			}
			if (EventSystems.InputCompat.GetKeyDown(KeyCode.B))
			{
				List<float> randomList = WMG_Util.GenRandomList(1, 0, 100);
				for (int i = 0; i < randomList.Count; i++)
				{
					int xIndex = Random.Range(0, numPointsToCreate);
					series1.pointValues[xIndex] = new Vector2(series1.pointValues[xIndex].x, randomList[i]);
				}
			}
		}
	}
}