Shader "Unlit/SomeUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BoneTransformTex("Bone Transform Texture",2D)="white"{}
        _AnimationTime("Animation Time",Float)=0.
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            uniform float4 _BoneTransformPixels[2048]; // Adjust size as needed

            struct appdata
            {
                // float4 vertex : POSITION;
                // float2 uv : TEXCOORD0;
                float4 vertex:POSITION;
                float4 normal:NORMAL;
                float4 tangent:TANGENT;
                int4 boneIndices:BLENDINDICES;// Bone indices.
                float4 boneWeights:BLENDWEIGHTS;// Bone weights.
                float2 uv:TEXCOORD0;
            };

            struct v2f
            {
                // float4 vertex : SV_POSITION;
                // float2 uv : TEXCOORD0;
                // UNITY_FOG_COORDS(1)
                float4 vertex:SV_POSITION;
                float2 uv:TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BoneTransformTex;
            float _AnimationTime;// Custom uniform for animation control.
			
            // get a pixel color from an index
            float4 sample_lib_data(int index){
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

            float4x4 get_bone_transform(int lib_index,float4 anim_data,int bone_count,int bone_id,int frame){
                float anim_length=anim_data.r;
                float loop_mode=anim_data.g;
                float anim_frame_count=anim_data.b;
                float anim_pixel_index=anim_data.a;
                int anim_index = lib_index + int(anim_pixel_index) / 4;
                // go to the first bone
                anim_index += 1;
				// go to the right frame (each bone has 4 pixels x 4 floats)
				anim_index += frame * bone_count * 4;
				// go to the right bone
				anim_index += bone_id * 4;

                // get all rows of the bone transform
                float4 col0 = sample_lib_data(anim_index+0);
                float4 col1 = sample_lib_data(anim_index+1);
                float4 col2 = sample_lib_data(anim_index+2);
                float4 col3 = sample_lib_data(anim_index+3);
                //return float4x4(row0,row1,row2,float4(0.,0.,0.,1.)); // this is by row, unity is by columns?
                return float4x4(col0, col1, col2, col3);
            }

            float4x4 get_matrix(int lib_index,float4 anim_data,int bone_count,int frame,int4 bone_indices,float4 bone_weights){
                float4x4 bone_transform_0 = get_bone_transform(lib_index,anim_data,bone_count,bone_indices.x,frame);
                float4x4 bone_transform_1 = get_bone_transform(lib_index,anim_data,bone_count,bone_indices.y,frame);
                float4x4 bone_transform_2 = get_bone_transform(lib_index,anim_data,bone_count,bone_indices.z,frame);
                float4x4 bone_transform_3 = get_bone_transform(lib_index,anim_data,bone_count,bone_indices.w,frame);
                float4x4 total=bone_transform_0*bone_weights.x+
								bone_transform_1*bone_weights.y+
								bone_transform_2*bone_weights.z+
								bone_transform_3*bone_weights.w;
                return total;
            }

            v2f vert (appdata v)
            {
                // v2f o;
                // o.vertex = UnityObjectToClipPos(v.vertex);
                // o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // UNITY_TRANSFER_FOG(o,o.vertex);
				
                int lib_index=0;
                int anim_index=1;
                float4 lib_data=sample_lib_data(lib_index);
				
				int lib_count = int(lib_data.r);
				int anim_count = int(lib_data.g);
				int bone_count = int(lib_data.b);
				float lib_end_index = lib_data.a;
                
                float4 anim_data=sample_lib_data(anim_index);

                float anim_length=anim_data.r;
                float loop_mode=anim_data.g;
                float anim_frame_count=anim_data.b;
                float anim_pixel_index=anim_data.a;
                float progress=(_AnimationTime/anim_length)*anim_frame_count;
                float frame0=floor(progress);
                float frame1=frame0+1.0;
                float fraction=frac(progress);
                if(frame1>=anim_frame_count){
                    frame1=0.;
                }
                

				float4 world_v = UnityObjectToClipPos(v.vertex);

				// frame0 = 0;

                float4x4 mat0 = get_matrix(lib_index, anim_data, bone_count, int(frame0), v.boneIndices, v.boneWeights);
                float4x4 mat1 = get_matrix(lib_index, anim_data, bone_count, int(frame1), v.boneIndices, v.boneWeights);
                float4 vertex0 = mul(mat0, v.vertex); //  v.vertex * mat0; //
                float4 vertex1 = mul(mat1, v.vertex); //  v.vertex * mat1; //
                float4 body_pos = vertex0 * (1.0 - fraction) + vertex1 * (fraction);
                
                v2f o;
                //o.vertex = UnityObjectToClipPos(body_pos); //v.vertex); //body_pos); // UnityObjectToClipPos(body_pos) * 3; // TransformObjectToHClip(body_pos) * 3;; //
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex); // v.uv; // TRANSFORM_TEX(v.uv, _MainTex);

				// float4 scaledVertex = v.vertex * float4(_AnimationTime, _AnimationTime, _AnimationTime, 1.0);
				// float4 scaledVertex = v.vertex * float4(anim_length, anim_length, anim_length, 1.0);
				// float4 scaledVertex = v.vertex * float4(frame0, frame0, frame0, 1.0);
				// float4 scaledVertex = v.vertex * float4(anim_count, anim_count, anim_count, 1.0);
				// float4 scaledVertex = v.vertex * float4(fraction + 1, fraction + 1, fraction + 1, 1.0);
				// float4 scaledVertex = v.vertex * float4(progress, progress, progress, 1.0);
                // o.vertex = UnityObjectToClipPos(scaledVertex);

				// float4 vertex2 = mul(mat0, UnityObjectToClipPos(v.vertex)); 

                // o.vertex = body_pos;
                // o.vertex = UnityObjectToClipPos(body_pos);
                o.vertex = UnityObjectToClipPos(body_pos * float4(0.1, 0.1, 0.1, 1.0));
                // o.vertex = UnityObjectToClipPos(mul(mat0, v.vertex));
                // o.vertex = vertex2;
				// o.vertex = mul(mat0,  v.vertex);

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
                // return fixed4(255, 0, 0, 255);
            }
            ENDCG
        }
    }
}
