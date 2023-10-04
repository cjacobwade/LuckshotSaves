Shader "Debug/Vert Normal"
{
	Properties
	{
		
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
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
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(i.normal, 1);
			}
			ENDCG
		}
	}
}
