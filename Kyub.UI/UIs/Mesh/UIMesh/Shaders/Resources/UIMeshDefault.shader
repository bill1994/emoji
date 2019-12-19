Shader "UIMesh/UI Mesh Default" 
{
	Properties {
		//Default Properties
		[PerRendererData] _MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)

		//Add Custom Properties HERE!!

		//End Custom Properties

		//Stencil Properties
		[HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask("Color Mask", Float) = 15
	}
	SubShader 
	{
		//Stencil Check
		Tags
		{ 
			"Queue" = "Transparent" 
			"RenderType" = "Transparent" 
			"IgnoreProjector" = "True" 
		}
		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}
		Lighting Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]
			
		CGPROGRAM

		#include "UnityUI.cginc"
			
		#pragma surface surf Unlit vertex:vert finalcolor:colorclip keepalpha 

		//Default Properties
		sampler2D _MainTex;
		fixed4 _Color;
		fixed4 _TextureSampleAdd;
		float4 _ClipRect;

		//Add Custom Properties HERE!!

		//End Custom Properties

		struct Input {
			float2 uv_MainTex;
			fixed4 color;
			float4 worldPosition;

			//Add Custom Struct Fields Here!!

			//End Custom Struct Fields
		};

		//Custom Vertex Shader to precalculate clipping position (Dont Change this Function!)
		void vert(inout appdata_full v, out Input o) 
		{
			float4 v_worldPos = v.vertex;
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.worldPosition = v_worldPos;
			o.color = v.color * _Color;
		}

		//Apply clipping to alpha (Dont Change this Function!)
		void colorclip(Input IN, SurfaceOutput o, inout fixed4 color)
		{
			color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
		}

		//Custom light model to make it unlit
		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten) {
			half4 c;
			c.rgb = s.Albedo * 0.5f;
			c.a = s.Alpha;
			return c;
		}

		void surf(Input IN, inout SurfaceOutput o) {
			fixed4 tex = (tex2D(_MainTex, IN.uv_MainTex) + _TextureSampleAdd) * IN.color;
			fixed4 c = tex * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;

			//Implement your own custom Surface Calcs
			
			//End Custom Surface
		}

		ENDCG
	}
	FallBack "Diffuse"
}