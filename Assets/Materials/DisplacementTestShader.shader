Shader "Custom/DisplacementTestShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
//		_WaveNumber ("Wave Number", Vector) = (1, 1, 0, 0)
		_WaveNumberAngle ("Wave Number Angle", Float) = 0
		_AngularFrequency ("Angular Frequency", Float) = 1
		_Amplitude ("Amplitude", Float) = 1
	}

	SubShader {
		Tags {
			"RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"
		}

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// The structure definition defines which variables it contains.
			// This example uses the Attributes structure as an input structure in
			// the vertex shader.
			struct Attributes {
				// The positionOS variable contains the vertex positions in object
				// space.
				float4 positionOS : POSITION;
			};

			struct Varyings {
				// The positions in this struct must have the SV_POSITION semantic.
				float4 positionHCS : SV_POSITION;
			};

			// float2 _WaveNumber;
			float _WaveNumberAngle;
			float _AngularFrequency;
			float _Amplitude;
			half4 _Color;

			Varyings vert(Attributes IN) {
				Varyings OUT;

				float2 x0 = IN.positionOS.xz;
				float2 k = float2(cos(_WaveNumberAngle), sin(_WaveNumberAngle));
				float A = _Amplitude;
				float omega = _AngularFrequency;
				float2 x = x0 - normalize(k) * A * sin(dot(k, x0) - omega * _Time.y);
				float y = A * cos(dot(k, x0) - omega * _Time.y);
				OUT.positionHCS = TransformObjectToHClip(float4(x.x, y, x.y, 1));

				return OUT;
			}

			half4 frag() : SV_Target {
				half4 customColor;

				customColor = _Color;
				return customColor;
			}
			ENDHLSL
		}
	}
}