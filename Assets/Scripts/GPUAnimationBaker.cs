using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class GPUAnimationBaker : MonoBehaviour
{
    public SkinnedMeshRenderer mesh;
    public Animator animator;

    public void Bake()
    {
        List<float> pixels = new();

        int iLayer = 0;
        //Get Current State
        AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(iLayer);
        AnimatorClipInfo[] infos = animator.GetCurrentAnimatorClipInfo(iLayer);

        float length = infos[0].clip.length;
        float framerate = 15f;
        int frameCount = (int) Math.Ceiling(length * framerate);
        float adjustedTimePerFrame = length / (frameCount - 1);

        pixels.Add(1); // number of libraries
        pixels.Add(1); // number of animations
        pixels.Add(mesh.bones.Length); // number of bones
        pixels.Add(0); // index of the end of the library

        var anim_pixel_index = pixels.Count;
        pixels.Add(length); // time length of the animation
        pixels.Add(0); // loop mode
        pixels.Add(frameCount); // number of frames in the animation
        pixels.Add(anim_pixel_index); // index of the start of the animation

        float currenTime = 0;
        for (int i = 0; i < frameCount; i++)
        {
            //Set Normalized Time
            //animator.Play(aniStateInfo.shortNameHash, iLayer, fNormalizedTime);
            animator.PlayInFixedTime("Idle", iLayer, currenTime);
            //Force Update
            animator.Update(0f);
            currenTime += adjustedTimePerFrame;
            foreach (var bone in mesh.bones)
            {
                PushBone(pixels, bone);
            }
        }
        pixels[3] = pixels.Count;

        // Convert float array to Color array.
        var pixel_count = pixels.Count / 4;

        var tex_width = 2048;
        var tex_height = pixel_count / tex_width + 1;
        var totalSize = tex_width * tex_height;
        Color[] colors = new Color[totalSize];
        for (int i = 0; i < totalSize; i++)
        {
            int p = i * 4;
            if (p >= pixels.Count)
            {
                colors[i] = Color.black;
            }
            else
            {
                colors[i] = new Color(pixels[p], pixels[p + 1], pixels[p + 2], pixels[p + 3]);
            }
        }
        Texture2D texture = new Texture2D(tex_width, tex_height, TextureFormat.RGBAFloat, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(colors);
        //texture.SetPixelData(pixels.ToArray(), 0);
        texture.Apply();
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/BoneTransforms.png", bytes);
    }

    private void PushBone(List<float> pixels, Transform bone)
    {
        //var worldp = bone.position;
        //pixels.Add(worldp.x);
        //pixels.Add(worldp.y);
        //pixels.Add(worldp.z);
        //pixels.Add(bone.rotation.x);
        //pixels.Add(bone.rotation.y);
        //pixels.Add(bone.rotation.z);
        //pixels.Add(bone.rotation.w);

        var a = bone.localToWorldMatrix;
        //var b = bone.worldToLocalMatrix;
        // row
        for (int i = 0; i < 3; i++)
        {
            // column
            for (int j = 0; j < 4; j++)
            {
                pixels.Add(a[i, j]);
                pixels.Add(a[i, j]);
                pixels.Add(a[i, j]);
                pixels.Add(a[i, j]);
            }
        }
    }

}
