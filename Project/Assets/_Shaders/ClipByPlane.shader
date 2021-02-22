// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Kaima/MeshBoolean/ClipByPlane"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_CullFaceColor("Cull Face Color", Color) = (1,1,0,1)
		_PlanePos("Clip Plane Pos", Vector) = (1,1,1,1)
		_PlaneNormal("Clip Plane Normal", Vector) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _PlanePos;
			float4 _PlaneNormal;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 dir =_PlanePos - i.worldPos;
				float d = dot(dir, _PlaneNormal);
				clip(d);

				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}

		Pass
		{
			Cull Front

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float3 cp : TEXCOORD2;
				float3 cn : TEXCOORD3;
				float3 localPos : TEXCOORD4;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _PlanePos;
			float4 _PlaneNormal;
			fixed4 _CullFaceColor;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 dir = _PlanePos - i.worldPos;
				float d = dot(dir, _PlaneNormal);
				clip(d);

				return _CullFaceColor;
			}
			ENDCG
		}
	}
}
