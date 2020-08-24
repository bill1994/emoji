using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Kyub.GraphMaker
{


	[RequireComponent(typeof(RawImage))]
	/// <summary>
	/// Class used to represent a texture that is procedurally generated using a Compute Shader.
	/// </summary>
	public class WMG_Compute_Shader : MonoBehaviour
	{

		public ComputeShader computeShader;

		int texSize;
		int kernelHandle;
		RenderTexture renderTexture;
		RawImage rawImg;
		bool hasInit = false;

		// --NOTE-- If you modify these numbers, you must also modify the .compute files, since the thread group sizes are hardcoded there as well
		// compute shader model 5.0 maximum threads (X * Y * Z) = 1024 = 32^2
		int threadGroupSizeX = 8;
		int threadGroupSizeY = 8;

		public int getKernelHandle()
		{
			return kernelHandle;
		}

		/// <summary>
		/// Initializes by creating a render texture with the specified resolution.
		/// </summary>
		/// <param name="textureResolution">Texture resolution.</param>
		public void Init(int textureResolution)
		{
			if (hasInit) return;
			texSize = textureResolution;
			hasInit = true;
			kernelHandle = computeShader.FindKernel("CSMain");
			rawImg = this.gameObject.GetComponent<RawImage>();
			renderTexture = new RenderTexture(texSize, texSize, 0);
			renderTexture.enableRandomWrite = true;
			renderTexture.filterMode = FilterMode.Bilinear;
			renderTexture.Create();
		}

		/// <summary>
		/// Runs the compute shader and updates the texture.
		/// </summary>
		public void dispatchAndUpdateImage()
		{
			computeShader.SetInt("texSize", texSize);
			computeShader.SetTexture(kernelHandle, "Result", renderTexture);
			computeShader.Dispatch(kernelHandle, texSize / threadGroupSizeX, texSize / threadGroupSizeY, 1);
			rawImg.texture = (Texture)renderTexture;
		}

		void OnDestroy()
		{
			if (renderTexture != null)
			{
				renderTexture.Release();
				DestroyImmediate(renderTexture);
			}
		}
	}
}