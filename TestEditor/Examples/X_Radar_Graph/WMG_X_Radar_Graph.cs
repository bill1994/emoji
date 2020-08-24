using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kyub.GraphMaker
{

	public class WMG_X_Radar_Graph : MonoBehaviour
	{

		public GameObject radarGraphPrefab;
		public List<float> testData;
		WMG_Radar_Graph graph;

		// Use this for initialization
		void Start()
		{
			GameObject graphGO = GameObject.Instantiate(radarGraphPrefab);
			graphGO.transform.SetParent(this.transform, false);
			graph = graphGO.GetComponent<WMG_Radar_Graph>();
			graph.numPoints = 0;
			graph.Init(); // Important this gets called before setting data

			graph.randomData = false;
			graph.numPoints = testData.Count;
			graph.Refresh(); // ensure series are created

			updateData(); // set data 

			graph.setBackgroundColor(new Color(25 / 255f, 20 / 255f, 50 / 255f, 1));
		}

		void updateData()
		{
			for (int i = 0; i < graph.numDataSeries; i++)
			{ // loop through each radar data series (other series are grids and labels)
				WMG_Series aSeries = graph.lineSeries[i + graph.numGrids].GetComponent<WMG_Series>();
				aSeries.pointValues.SetList(WMG_Util.GenRadar(testData, graph.offset.x, graph.offset.y, graph.degreeOffset));
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (Input.GetKeyDown(KeyCode.A))
			{
				updateData();
			}
		}

	}
}
