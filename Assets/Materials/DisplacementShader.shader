Shader "Custom/DisplacementShader"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_Resolution ("Resolution", Vector) = (1, 1, 0, 0)
		_Height ("Height", Float) = 3

		_Displacement ("Displacement", 2D) = "black" {}
		_NormalMap ("NormalMap", 2D) = "black" {}
		_ApproximateNormalMap ("ApproximateNormalMap", 2D) = "black" {}
	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// The structure definition defines which variables it contains.
			// This example uses the Attributes structure as an input structure in
			// the vertex shader.
			struct Attributes
			{
				// The positionOS variable contains the vertex positions in object
				// space.
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				// The positions in this struct must have the SV_POSITION semantic.
				float4 positionHCS : SV_POSITION;
				//Not a great name
				float2 positionCoord : TEXCOORD0;
			};

			float2 _Resolution;
			half4 _Color;
			float _Height;

			sampler2D _Displacement;
			sampler2D _NormalMap;
			sampler2D _ApproximateNormalMap;

			Varyings vert(Attributes IN)
			{
				Varyings OUT;
				float2 coords = mul(unity_ObjectToWorld, IN.positionOS).xz - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xz;

				float3 d = tex2Dlod(_Displacement, float4(coords / _Resolution, 0, 0)).rgb;
				OUT.positionHCS = TransformObjectToHClip(IN.positionOS + float4(d.x, d.y, d.z, 0));
				OUT.positionCoord = coords;

				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target
			{
				// half4 customColor;
				// customColor = _Color;

				// float4(nyx real, nyx imaginary, nyz real, nyz imaginary)
				float4 ny = tex2Dlod(_NormalMap, float4(IN.positionCoord / _Resolution, 0, 0));
				float3 normalYTangentX = float3(1, length(ny.xy), 0);
				float3 normalYTangentZ = float3(0, length(ny.zw), 1);
				float3 n = normalize(cross(normalYTangentX, normalYTangentZ));
				return half4(n, 1);

				// float3 approximateNormal = tex2Dlod(_ApproximateNormalMap, float4(IN.positionCoord / _Resolution, 0, 0)).xyz;
				// return half4(approximateNormal, 1);
			}
			ENDHLSL
		}
	}
}