using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class GPUAnimationBaker : MonoBehaviour
{
    public SkinnedMeshRenderer mesh;
    public Animator animator;

    public Color[] bakedPixels;
    public Texture2D bakedTexture;

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
            for (int b = 0; b < mesh.bones.Length; b++)
            //foreach (var bone in mesh.bones)
            {
                var bone = mesh.bones[b];
                var bindpose = mesh.sharedMesh.bindposes[b];
                var transform = bone.localToWorldMatrix * bindpose;
                PushBone(pixels, transform);
                if (b == 14)
                {
                    Debug.Log($"Baking has bone transform 14 {bone.name}:\n{transform}");
                }
            }
        }
        pixels[3] = pixels.Count;

        // Convert float array to Color array.
        var pixel_count = pixels.Count / 4;

        var tex_width = 2048;
        var tex_height = pixel_count / tex_width + 1;
        var totalSize = tex_width * tex_height;
        bakedPixels = new Color[totalSize];
        for (int i = 0; i < totalSize; i++)
        {
            int p = i * 4;
            if (p >= pixels.Count)
            {
                bakedPixels[i] = Color.black;
            }
            else
            {
                bakedPixels[i] = new Color(pixels[p], pixels[p + 1], pixels[p + 2], pixels[p + 3]);
                //if(i == 0)
                //{
                //    Debug.Log($"Color: {colors[i]}");
                //    Debug.Log($"Values: {pixels[p]}, {pixels[p + 1]}, {pixels[p + 2]}, {pixels[p + 3]}");
                //}
            }
        }

        // Float count = 7 688
        // 7 688 / 4 = 1922 pixels
        // 1922 - 2 = 1920 (metadata on animations/libraries)
        // 1920 / 32 bones = 60
        // 60 / 15 frames = 4 pixels per bone per frame = 4 columns of a bone matrix


        Debug.Log($"Bones {mesh.bones.Length}, Length {length}, Framecount {frameCount}, AdjustedTimePerFrame {adjustedTimePerFrame}, Float count {pixels.Count}, PixelCount {pixel_count}, TexHeight {tex_height}");
        bakedTexture = new Texture2D(tex_width, tex_height, TextureFormat.RGBAFloat, false);
        bakedTexture.filterMode = FilterMode.Point;
        bakedTexture.SetPixels(bakedPixels);
        //texture.SetPixelData(pixels.ToArray(), 0);
        bakedTexture.Apply();


        Debug.Log($"Baking has lib: {bakedTexture.GetPixel(0, 0)}");
        Debug.Log($"Baking has anim: {bakedTexture.GetPixel(1, 0)}");
        Debug.Log($"Baking has bone: {bakedTexture.GetPixel(2, 0)}");
        byte[] bytes = bakedTexture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/BoneTransforms.png", bytes);
        WritePixelsToFile(bakedPixels.ToList());
    }

    private string filePath = Application.dataPath + "/gpuAnimationTransforms.txt"; // Path to save the file
    public void WritePixelsToFile(List<Color> pixels)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var pixel in pixels)
            {
                //writer.WriteLine(pixel.ToString());
                writer.WriteLine($"{pixel.r} {pixel.g} {pixel.b} {pixel.a}");
            }
            writer.Flush();
            writer.Close();
        }
        
        Debug.Log($"Pixels {pixels.Count} written to {filePath}");
    }
    public List<Color> ReadMatricesFromFile()
    {
        List<Color> pixels = new();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found at {filePath}");
            return pixels;
        }

        using (StreamReader reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] values = line.Split(' ');
                var pixel = new Color(
                    float.Parse(values[0]),
                    float.Parse(values[1]),
                    float.Parse(values[2]),
                    float.Parse(values[3])
                );
                pixels.Add(pixel);
            }
        }

        Debug.Log($"Pixels {pixels.Count} read from {filePath}");
        return pixels;
    }



    private void PushBone(List<float> pixels, Matrix4x4 transform) //Transform bone)
    {
        //var worldp = bone.position;
        //pixels.Add(worldp.x);
        //pixels.Add(worldp.y);
        //pixels.Add(worldp.z);
        //pixels.Add(bone.rotation.x);
        //pixels.Add(bone.rotation.y);
        //pixels.Add(bone.rotation.z);
        //pixels.Add(bone.rotation.w);
        //bone.parent
        //var a = bone.localToWorldMatrix;
        //var b = bone.worldToLocalMatrix;

        // column
        for (int j = 0; j < 4; j++)
        {
            // row
            for (int i = 0; i < 4; i++)
            {
                // write by column
                //pixels.Add(a[i, j]);
                pixels.Add(transform[i, j]);
            }
        }
    }

}
