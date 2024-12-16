Shader "Unlit/SomeUnlit" {
    Properties{
        _MainTex("Texture", 2D) = "white" {} 
		_BoneTransformTex("Bone Transform Texture", 2D) = "white" {} 
		_AnimationTime("Animation Time", Float) = 0.
	} 

	SubShader {
        Tags{"RenderType" = "Opaque"} LOD 100

        Pass {
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

            uniform float4 _BoneTransformPixels[2048]; // Adjust size as needed

            struct appdata {
                float4 vertex : POSITION;
                // float4 normal : NORMAL;
                float4 tangent : TANGENT;
                int4 boneIndices : BLENDINDICES;   // Bone indices.
                float4 boneWeights : BLENDWEIGHTS; // Bone weights.
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                // float2 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BoneTransformTex;
            float _AnimationTime; // Custom uniform for animation control.

            // get a pixel color from an index
            float4 sample_lib_data(int index) {
                /*
                float f=float(index);
                float h=floor(f/2048.0); // remaining = col
                index=int(fmod(f,2048.0)); // mod = row
                // return texelFetch(animation_data, ivec2(index, int(h)), 0);
                // return tex2D(_BoneTransformTex,float2(index,h));
                return tex2Dlod(_BoneTransformTex, float4(index, h, 0, 0) );
                */
                return _BoneTransformPixels[index];
            }

            float4x4 get_bone_transform(int lib_index, float4 anim_data, int bone_count, int bone_id, int frame) {
                float anim_length = anim_data.r;
                float loop_mode = anim_data.g;
                float anim_frame_count = anim_data.b;
                float anim_pixel_index = anim_data.a;
				// start at animation index
                int anim_index = lib_index + int(anim_pixel_index) / 4;
                // go to the first bone
                anim_index += 1;
                // go to the right frame (each bone has 4 pixels x 4 floats)
                anim_index += frame * bone_count * 4;
                // go to the right bone
                anim_index += bone_id * 4;

                // get all rows of the bone transform
                float4 row0 = sample_lib_data(anim_index + 0);
                float4 row1 = sample_lib_data(anim_index + 1);
                float4 row2 = sample_lib_data(anim_index + 2);
                float4 row3 = sample_lib_data(anim_index + 3);
                return float4x4(row0, row1, row2, row3);
            }

            float4x4 get_matrix(int lib_index, float4 anim_data, int bone_count, int frame, int4 bone_indices, float4 bone_weights) {
                float4x4 bone_transform_0 = get_bone_transform(lib_index, anim_data, bone_count, bone_indices.x, frame);
                float4x4 bone_transform_1 = get_bone_transform(lib_index, anim_data, bone_count, bone_indices.y, frame);
                float4x4 bone_transform_2 = get_bone_transform(lib_index, anim_data, bone_count, bone_indices.z, frame);
                float4x4 bone_transform_3 = get_bone_transform(lib_index, anim_data, bone_count, bone_indices.w, frame);
                float4x4 total = bone_transform_0 * bone_weights.x +
                                 bone_transform_1 * bone_weights.y +
                                 bone_transform_2 * bone_weights.z +
                                 bone_transform_3 * bone_weights.w;
                return total;
            }

            float4 simpleMatrix(int frameId, int boneCount, float4 vertex, int4 boneIndices, float4 boneWeights) {
                // Transform vertex position based on bone weights and indices
                float4 z = float4(0.0, 0.0, 0.0, 0.0);
                float4 transformSum = z;

                // Loop through each bone influence
                for (int i = 0; i < 4; i++) // Unity uses up to 4 bones per vertex
                {
                    int boneIndex = boneIndices[i];
                    int transformIndex = 2 +                       // anim
                                         frameId * boneCount * 4 + // frame
                                         boneIndex * 4;            // bone

                    float4 row0 = _BoneTransformPixels[transformIndex + 0];
                    float4 row1 = _BoneTransformPixels[transformIndex + 1];
                    float4 row2 = _BoneTransformPixels[transformIndex + 2];
                    float4 row3 = _BoneTransformPixels[transformIndex + 3];
                    float4x4 transform = float4x4(row0, row1, row2, row3);

                    float weight = boneWeights[i];
                    transformSum += mul(transform, vertex) * weight;
                }
                return transformSum;
            }

            v2f vert(appdata v) {
                int lib_index = 0;
                int anim_index = 1;
                float4 lib_data = sample_lib_data(lib_index);

                int lib_count = int(lib_data.r);
                int anim_count = int(lib_data.g);
                int bone_count = int(lib_data.b);
                float lib_end_index = lib_data.a;

                float4 anim_data = sample_lib_data(anim_index);
                float anim_length = anim_data.r;
                float loop_mode = anim_data.g;
                float anim_frame_count = anim_data.b;
                float anim_pixel_index = anim_data.a;

                float progress = (_AnimationTime / anim_length) * anim_frame_count;
                float frame0 = floor(progress);
                float frame1 = frame0 + 1.0;
                float fraction = frac(progress);
                if (frame1 >= anim_frame_count) {
                    frame1 = 0.;
                }

                float4x4 mat0 = get_matrix(lib_index, anim_data, bone_count, int(frame0), v.boneIndices, v.boneWeights);
                float4x4 mat1 = get_matrix(lib_index, anim_data, bone_count, int(frame1), v.boneIndices, v.boneWeights);
                float4 vertex0 = mul(mat0, v.vertex);
                float4 vertex1 = mul(mat1, v.vertex);
                float4 body_pos = vertex0 * (1.0 - fraction) + vertex1 * (fraction);
                // float4 normal = mul(mat0, v.normal) * (1.0 - fraction) + mul(mat1, v.normal) * (fraction);

				vertex0 = simpleMatrix(int(frame0), bone_count, v.vertex, v.boneIndices, v.boneWeights);
				vertex1 = simpleMatrix(int(frame0), bone_count, v.vertex, v.boneIndices, v.boneWeights);
                body_pos = vertex0 * (1.0 - fraction) + vertex1 * (fraction);

                v2f o;
				// o.normal = normal;
                // o.vertex = mul(UNITY_MATRIX_VP, body_pos);
                o.vertex = UnityObjectToClipPos(body_pos);
                // o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
                // return fixed4(255, 0, 0, 255);
            }
            ENDCG
        }
    }
}
