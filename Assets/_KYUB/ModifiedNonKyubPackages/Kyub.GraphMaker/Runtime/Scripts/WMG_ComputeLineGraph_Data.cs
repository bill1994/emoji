using UnityEngine;
using System.Collections;


namespace Kyub.GraphMaker
{

	/// <summary>
	/// Helper class used to hold data for area shading when WMG_Series::areaShadingUsesComputeShader = true or WMG_Series::linesUseComputeShader = true.
	/// </summary>
	public class WMG_ComputeLineGraph_Data : MonoBehaviour
	{
		public float[] xVals;
		public float[] yVals;
		public uint[] pixelIndexToPointMap;
		public float[] multiSeriesLineColors;
		public ComputeBuffer pointValsBufferX;
		public ComputeBuffer pointValsBufferY;
		public ComputeBuffer pixelIndexToPointMapBuffer;
		public ComputeBuffer multiSeriesLineColorsBuffer;

		int currentCapacity = 0;
		int currentTexSize = 0;
		int currentNumSeriesCapacity = 0;

		void OnDestroy()
		{ // called when program terminates, release GPU resources otherwise error thrown in console
			if (pointValsBufferX != null)
			{
				pointValsBufferX.Release();
			}
			if (pointValsBufferY != null)
			{
				pointValsBufferY.Release();
			}
			if (pixelIndexToPointMapBuffer != null)
			{
				pixelIndexToPointMapBuffer.Release();
			}
			if (multiSeriesLineColorsBuffer != null)
			{
				multiSeriesLineColorsBuffer.Release();
			}
		}

		public void setupPixelIndexMap(int texSize)
		{
			if (currentTexSize != texSize)
			{
				currentTexSize = texSize;
				pixelIndexToPointMap = new uint[texSize];
				if (pixelIndexToPointMapBuffer != null)
				{
					pixelIndexToPointMapBuffer.Release();
				}
				pixelIndexToPointMapBuffer = new ComputeBuffer(texSize, 4);
			}
		}

		public bool EnsureCapacity(int numPoints)
		{
			int originalCapacity = currentCapacity;
			while (numPoints > currentCapacity)
			{ // double capacity until fits
				if (currentCapacity == 0)
				{
					currentCapacity++;
				}
				else
				{
					currentCapacity *= 2;
				}
			}
			if (originalCapacity != currentCapacity)
			{ // capacity changed
				if (pointValsBufferX != null)
				{
					pointValsBufferX.Release();
				}
				if (pointValsBufferY != null)
				{
					pointValsBufferY.Release();
				}
				xVals = new float[currentCapacity];
				yVals = new float[currentCapacity];
				pointValsBufferX = new ComputeBuffer(currentCapacity, 4);
				pointValsBufferY = new ComputeBuffer(currentCapacity, 4);
				return true;
			}
			return false;
		}

		public void setupMultiSeriesColors(int numSeries)
		{
			int originalCapacity = currentNumSeriesCapacity;
			while (numSeries > currentNumSeriesCapacity)
			{ // double capacity until fits
				if (currentNumSeriesCapacity == 0)
				{
					currentNumSeriesCapacity++;
				}
				else
				{
					currentNumSeriesCapacity *= 2;
				}
			}
			if (originalCapacity != currentNumSeriesCapacity)
			{ // capacity changed
				if (multiSeriesLineColorsBuffer != null)
				{
					multiSeriesLineColorsBuffer.Release();
				}
				multiSeriesLineColors = new float[currentNumSeriesCapacity * 4];
				multiSeriesLineColorsBuffer = new ComputeBuffer(currentNumSeriesCapacity * 4, 4);
			}
		}
	}
}
