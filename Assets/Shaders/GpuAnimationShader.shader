// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader"Custom/GpuAnimationShader"
{
    Properties
    {
        _MainTex("Base Map",2D)="white"{}
        _BoneTransformTex("Bone Transform Texture",2D)="white"{}
        _AnimationTime("Animation Time",Float)=0.
    }
    SubShader
    {
        Tags{"RenderType"="Opaque"}
        Pass{
            
            HLSLPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            // #pragma surface surf Standard fullforwardshadows
            #pragma vertex vert
            #pragma fragment frag
            
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            sampler2D _BoneTransformTex;
            float _AnimationTime;// Custom uniform for animation control.
            
            struct appdata
            {
                float4 vertex:POSITION;
                float4 normal:NORMAL;
                float4 tangent:TANGENT;
                float4 boneIndices:BLENDINDICES;// Bone indices.
                float4 boneWeights:BLENDWEIGHTS;// Bone weights.
                float4 uv:TEXCOORD0;
            };
            
            struct vertOut
            {
                float4 pos:SV_POSITION;
                float2 uv:TEXCOORD0;
            };
            
            // get a pixel color from an index
            float4 sample_lib_data(int index){
                float f=float(index);
                float h=floor(f/2048.0);
                index=int(fmod(f,2048.0)); // mod
                // return texelFetch(animation_data, ivec2(index, int(h)), 0);
                // return tex2D(_BoneTransformTex,int2(index,int(h)));
				return tex2Dlod(_BoneTransformTex, float4(index, int(h), 0, 0) );
            }
            float4x4 get_bone_transform(int lib_index,float4 anim_data,int bone_count,int bone_id,int frame){
                float anim_length=anim_data.r;
                float loop_mode=anim_data.g;
                float anim_frame_count=anim_data.b;
                float anim_pixel_index=anim_data.a;
                // * 3 because each bone has 3 float4, one for each row of its transform matrix
                int anim_index=lib_index+int(anim_pixel_index)/4;
                // each frame has bonecount * 3 length
                int frame_index=frame*bone_count*3;
                // bone location within the frame
                int bone_index_in_frame=bone_id*3;
                // add 1 to offset from the animation data index
                anim_index+=1+frame_index+bone_index_in_frame;
                // get all rows of the bone transform
                float4 row0=sample_lib_data(anim_index);
                float4 row1=sample_lib_data(anim_index+1);
                float4 row2=sample_lib_data(anim_index+2);
                return float4x4(row0,row1,row2,float4(0.,0.,0.,1.)); // this is by row, unity is by columns?
            }
            
            float4x4 get_matrix(int lib_index,float4 anim_data,int bone_count,int frame,float4 bone_indices,float4 bone_weights){
                float4x4 bone_transform_0=get_bone_transform(lib_index,anim_data,bone_count,bone_indices.x,frame);
                float4x4 bone_transform_1=get_bone_transform(lib_index,anim_data,bone_count,bone_indices.y,frame);
                float4x4 bone_transform_2=get_bone_transform(lib_index,anim_data,bone_count,bone_indices.z,frame);
                float4x4 bone_transform_3=get_bone_transform(lib_index,anim_data,bone_count,bone_indices.w,frame);
                float4x4 total=bone_transform_0*bone_weights.x+
								bone_transform_1*bone_weights.y+
								bone_transform_2*bone_weights.z+
								bone_transform_3*bone_weights.w;
                return total;
            }
            
            vertOut vert(appdata v)
            {
                vertOut o;
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
                
                float4x4 mat0 = get_matrix(lib_index, anim_data, bone_count, int(frame0), v.boneIndices, v.boneWeights);
                float4x4 mat1 = get_matrix(lib_index, anim_data, bone_count, int(frame1), v.boneIndices, v.boneWeights);
                float4 vertex0 = mul(mat0, v.vertex); //v.vertex * mat0;
                float4 vertex1 = mul(mat1, v.vertex); //v.vertex * mat1;
                float4 body_pos = vertex0 * (1.0 - fraction) + vertex1 * (fraction);
                
                o.pos = UnityObjectToClipPos(body_pos);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(vertOut f):SV_Target
            {
				float4 color = float4(tex2D(_MainTex,f.uv), 1.);
                return color;
            }

            ENDHLSL
        }
    }
}
