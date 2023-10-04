Shader "Debug/Vert Color"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		[Toggle]
		_ShowAlpha("Show Alpha", Float) = 1
	}
	SubShader
	{
		Blend SrcAlpha OneMinusSrcAlpha

		Tags { "RenderType"="Transparent" "Queue" = "Transparent"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			half _ShowAlpha;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 color = i.color;
				if(_ShowAlpha == 0)
					color.a = 1;

				return color;
			}
			ENDCG
		}
	}
}
