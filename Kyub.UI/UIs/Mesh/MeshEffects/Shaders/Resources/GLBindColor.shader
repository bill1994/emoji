Shader "GL/GLBindColor" 
{
	SubShader 
	{ 
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
		Pass 
		{
			BindChannels { Bind "Color", color }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off 
			Cull Off 
			Fog { Mode Off }
		} 
	} 
}