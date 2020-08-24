#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Kyub.GraphMaker
{


	[CustomEditor(typeof(WMG_X_Plot_Overtime))]
	public class WMG_X_Plot_Overtime_E : WMG_E_Util
	{
		WMG_X_Plot_Overtime script;
		Dictionary<string, WMG_PropertyField> fields;

		void OnEnable()
		{
			script = (WMG_X_Plot_Overtime)target;
			fields = GetProperties(script);
		}

		public override void OnInspectorGUI()
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update();

			DrawCore();

			if (GUI.changed)
			{
				EditorUtility.SetDirty(script);
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties();
		}

		void DrawCore()
		{
			script.emptyGraphPrefab = EditorGUILayout.ObjectField("Empty Graph Prefab", script.emptyGraphPrefab, typeof(Object), false);
			script.plotOnStart = EditorGUILayout.Toggle("Plot On Start", script.plotOnStart);
			ExposeProperty(fields["plottingData"]);
			script.plotIntervalSeconds = EditorGUILayout.FloatField("Plot Interval Seconds", script.plotIntervalSeconds);
			script.plotAnimationSeconds = EditorGUILayout.FloatField("Plot Animation Seconds", script.plotAnimationSeconds);
			script.xInterval = EditorGUILayout.FloatField("X Interval", script.xInterval);
			script.useAreaShading = EditorGUILayout.Toggle("Use Area Shading", script.useAreaShading);
			if (script.useAreaShading)
			{
				script.useComputeShader = EditorGUILayout.Toggle("Use Compute Shader", script.useComputeShader);
			}
			script.blinkCurrentPoint = EditorGUILayout.Toggle("Blink Current Point", script.blinkCurrentPoint);
			script.displayHorizontalIndicator = EditorGUILayout.Toggle("Display Horizontal Indicator", script.displayHorizontalIndicator);
			script.blinkAnimDuration = EditorGUILayout.FloatField("Blink Anim Duration", script.blinkAnimDuration);
			script.moveXaxisMinimum = EditorGUILayout.Toggle("Move xAxis Minimum", script.moveXaxisMinimum);
			script.indicatorPrefab = EditorGUILayout.ObjectField("Indicator Prefab", script.indicatorPrefab, typeof(Object), false);
			script.indicatorNumDecimals = EditorGUILayout.IntField("Indicator Num Decimals", script.indicatorNumDecimals);
			script.moveVerticalGridLines = EditorGUILayout.Toggle("Move Vertical Grid", script.moveVerticalGridLines);
			script.useDataListInsteadOfRandom = EditorGUILayout.Toggle("Use DataList Instead Of Random", script.useDataListInsteadOfRandom);
			script.circularlyRepeatDataList = EditorGUILayout.Toggle("Circularly Repeat DataList", script.circularlyRepeatDataList);
			script.dataListIgnoreX = EditorGUILayout.Toggle("DataList Ignore X", script.dataListIgnoreX);
			ArrayGUI("DataList", "dataList");
		}
	}
}

#endif
