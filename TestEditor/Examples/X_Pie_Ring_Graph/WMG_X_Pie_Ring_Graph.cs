using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kyub.GraphMaker
{

	public class WMG_X_Pie_Ring_Graph : WMG_GUI_Functions
	{

		public Object ringGraphPrefab;
		private WMG_Ring_Graph graph;
		private Color hoverColor = new Color(1, 1, 0, 0.3f);
		private Color mouseDownColor = new Color(1, 1, 0, 0.6f);

		void Start()
		{
			GameObject ringGO = GameObject.Instantiate(ringGraphPrefab) as GameObject;
			changeSpriteParent(ringGO, this.gameObject);
			graph = ringGO.GetComponent<WMG_Ring_Graph>();
			graph.Init(); // always initialize first (ensures Start() method on the graph gets called first)
			graph.pieMode = true;
			graph.pieModePaddingDegrees = 1; // the degree spacing between each slice
			graph.pieModeDegreeOffset = 90; // the degree rotational offset of the entire graph
			graph.innerRadiusPercentage = 0.75f; // the percentage of the graph that is empty
			graph.autoUpdateBandAlphaReverse = true; // reverses the order of how the bandcolors are updated 
			graph.labelStartCenteredOnBand = true;
			graph.animateData = false;
			graph.useComputeShader = true; // makes for smoother anti-aliased edges on the slices, and better perfomance (but doesn't work on all platforms)

			graph.values.Clear();
			graph.values.Add(275);
			graph.values.Add(240);
			graph.values.Add(210);
			graph.values.Add(200);
			graph.values.Add(160);
			graph.values.Add(130);
			graph.values.Add(100);
			graph.values.Add(50);

			changeSpriteSize(graph.gameObject, 700, 600);

			graph.WMG_Ring_Click += MyCoolRingClickFunction;
			graph.WMG_Ring_MouseEnter += MyCoolRingHoverFunction;
			graph.WMG_Ring_MouseDownUp += MyCoolRingDownUpFunction;
		}

		void MyCoolRingClickFunction(WMG_Ring ring, UnityEngine.EventSystems.PointerEventData pointerEventData)
		{
			Debug.Log("Ring: " + ring.ringIndex + " value: " + ring.graph.values[ring.ringIndex] + " label: " + ring.graph.labels[ring.ringIndex]);
		}

		void MyCoolRingHoverFunction(WMG_Ring ring, bool hover)
		{
			//		Debug.Log ("Hover: " + hover + " Ring: " + ring.ringIndex + " value: " + ring.graph.values[ring.ringIndex] + " label: " + ring.graph.labels[ring.ringIndex]);
			graph.changeSpriteColor(ring.band, hover ? hoverColor : graph.bandColors[ring.ringIndex]);
		}

		void MyCoolRingDownUpFunction(WMG_Ring ring, bool down, UnityEngine.EventSystems.PointerEventData pointerEventData)
		{
			//		Debug.Log ("Down: " + down + " Ring: " + ring.ringIndex + " value: " + ring.graph.values[ring.ringIndex] + " label: " + ring.graph.labels[ring.ringIndex]);
			WMG_Ring hoverRing = null;
			if (pointerEventData.pointerEnter != null && pointerEventData.pointerEnter.transform.parent != null)
			{
				hoverRing = pointerEventData.pointerEnter.transform.parent.gameObject.GetComponent<WMG_Ring>();
			}
			graph.changeSpriteColor(ring.band, down ? mouseDownColor : (hoverRing == ring ? hoverColor : graph.bandColors[ring.ringIndex]));
		}
	}
}
