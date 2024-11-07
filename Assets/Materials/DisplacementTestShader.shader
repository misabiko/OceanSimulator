Shader "Custom/DisplacementTestShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Resolution ("Resolution", Vector) = (1, 1, 0, 0)
        _Height ("Height", Float) = 3

        _Displacement ("Displacement", 2D) = "black" {}
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
            };

            float2 _Resolution;
            half4 _Color;
            float _Height;

            sampler2D _Displacement;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = mul(unity_ObjectToWorld, IN.positionOS);

                float3 d = tex2Dlod(_Displacement, float4(worldPos.xz / _Resolution, 0, 0)).rgb;
                // OUT.positionHCS = TransformObjectToHClip(IN.positionOS + float4(d.x / _Resolution.x, d.y * _Height, d.z / _Resolution.y, 0));
                OUT.positionHCS = TransformObjectToHClip(
                    IN.positionOS + float4(d.x * 2 - 0.5, d.y * 2 - 0.5, d.z * 2 - 0.5, 0));

                return OUT;
            }

            half4 frag() : SV_Target
            {
                half4 customColor;

                customColor = _Color;
                return customColor;
            }
            ENDHLSL
        }
    }
}