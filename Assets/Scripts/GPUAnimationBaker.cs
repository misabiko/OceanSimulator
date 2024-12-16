using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.InputSystem.HID.HID;
using Random = UnityEngine.Random;

public class GPUAnimationBaker : MonoBehaviour
{
    public GameObject targetObject; // eagle_normal
    public GameObject otherObject; // eaglle_normal
    public SkinnedMeshRenderer skin; // eaglle_normal
    public Animator animator;

    public float framerate = 15f;

    private Matrix4x4[] posesFrame0;
    private int stateNameHash;

    public float animationTime = 0;
    public static float animationLength = 3;
    // Start is called before the first frame update
    void Start()
    {
        var animationPixels = this.ReadMatricesFromFile();
        animationLength = animationPixels[1][0]; // 1st value in animation #1 = length
        animationTime = Random.Range(0, animationLength);
        skin.material.SetVectorArray("_BoneTransformPixels", animationPixels);
    }
    // Update is called once per frame
    void Update()
    {
        // Increment AnimationTime based on time and speed.
        //float animationTime = Time.time * speed;
        animationTime += Time.deltaTime;
        if (animationTime > animationLength)
        {
            animationTime -= animationLength; // loop animation
        }
        //foreach (var mat in skin.materials)
        //    mat.SetFloat("_AnimationTime", animationTime);
        skin.material.SetFloat("_AnimationTime", animationTime);
    }

    public Mesh bakeMesh()
    {
        Mesh baked = new Mesh();
        skin.BakeMesh(baked);
        return baked;
    }

    public void debugParents(Transform bone)
    {
        var parent = bone.parent;
        var bone2 = bone;
        while ((parent = bone2.parent) != null)
        {
            Debug.Log($"Bone parent: {bone2.name}: {bone2.localToWorldMatrix} -> parent {parent.name} :\n{parent.localToWorldMatrix}");
            bone2 = parent;
        }
    }

    public Matrix4x4 getPose(int boneId)
    {
        return posesFrame0[boneId];
        //return skin.sharedMesh.bindposes[boneId];
    }

    private void test()
    {
        int iLayer = 0;
        //Get Current State
        AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(iLayer);
        AnimatorClipInfo[] infos = animator.GetCurrentAnimatorClipInfo(iLayer);

        var objL = targetObject.transform.localToWorldMatrix;
        var objW = targetObject.transform.worldToLocalMatrix;

        var obj2L = otherObject.transform.localToWorldMatrix;
        var obj2W = otherObject.transform.worldToLocalMatrix;

        var obj2LL = obj2L * objW;

        float length = infos[0].clip.length;
        float framerate = 15f;
        int frameCount = (int) Math.Ceiling(length * framerate);
        float adjustedTimePerFrame = length / (frameCount - 1);

        int vertexId = 300;
        int boneId = 24;
        int frameId = 7;
        var weights = skin.sharedMesh.boneWeights[vertexId];

        infos[0].clip.SampleAnimation(targetObject, 0);
        //animator.PlayInFixedTime("Idle", iLayer, 0);
        //animator.Update(0f);
        var mesh0 = bakeMesh();
        var vert0 = mesh0.vertices[vertexId];
        var vert0b = skin.sharedMesh.vertices[vertexId];

        var bone0 = skin.bones[boneId].localToWorldMatrix;
        var bone0w = skin.bones[boneId].worldToLocalMatrix;
        var bone0inverse = bone0.inverse;
        var bind0Bad = skin.sharedMesh.bindposes[boneId];
        var bind0 = getPose(boneId);
        var bind0inverse = bind0.inverse;
        var transform0 = getBoneTransform(weights.boneIndex0);
        debugParents(skin.bones[1]);


        var globalPos0 = getPosition(weights, vert0);
        var globalPos0b = getPosition(weights, vert0b);

        infos[0].clip.SampleAnimation(targetObject, adjustedTimePerFrame * 7);
        //animator.PlayInFixedTime("Idle", iLayer, adjustedTimePerFrame * frameId);
        //animator.Update(0f);
        var mesh1 = bakeMesh();
        var vert1 = mesh1.vertices[vertexId];
        var vert1b = skin.sharedMesh.vertices[vertexId];
        var bone1 = skin.bones[boneId].localToWorldMatrix;
        var bind1Bad = skin.sharedMesh.bindposes[boneId];
        var bind1 = getPose(boneId);
        var transform1 = getBoneTransform(weights.boneIndex0);
        var globalPos1 = getPosition(weights, vert1);
        var globalPos1b = getPosition(weights, vert1b);

        var truc = new Vector4[] { globalPos0, globalPos0b, globalPos1, globalPos1b };

        Debug.Log($"Vertex 0: {vert0}, Vertex 0.5: {vert1}");

        var globalVert0 = transform0 * vert0;
        var globalVert0b = transform0 * vert0b;
        var globalVert1 = transform1 * vert1;
        var globalVert1b = transform1 * vert1b;

        bool isVertexEqual = vert0 == vert1;
        bool isBakeEqual = vert0 == vert0b;
        bool isRegularEqual = vert0b == vert1b;
        bool isBoneEqual = bone0 == bone1;
        bool isBindEqual = bind0 == bind1;
        bool isTransformEqual = transform0 == transform1;

        if (true)
        {

        }
    }

    public Vector4 getPosition(BoneWeight bw, Vector3 vertex)
    {
        var pos = getBoneTransform(bw.boneIndex0) * vertex * bw.weight0 +
                  getBoneTransform(bw.boneIndex1) * vertex * bw.weight1 +
                  getBoneTransform(bw.boneIndex2) * vertex * bw.weight2 +
                  getBoneTransform(bw.boneIndex3) * vertex * bw.weight3;
        return pos;
    }
    public Matrix4x4 getBoneTransform(int boneId)
    {
        var bone0 = skin.bones[boneId].localToWorldMatrix;
        var bind0 = getPose(boneId);
        var bindPoseBad = skin.sharedMesh.bindposes[boneId];
        var transform0 = bone0 * bind0;
        return transform0;
    }

    public void Bake()
    {
        List<float> pixels = new();

        int iLayer = 0;
        AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(iLayer);
        AnimatorClipInfo[] infos = animator.GetCurrentAnimatorClipInfo(iLayer);
        stateNameHash = aniStateInfo.fullPathHash; // "Idle";

        // Bindposes
        animator.PlayInFixedTime(stateNameHash, 0, 0);
        animator.Update(0f);
        posesFrame0 = skin.bones.Select(b => b.worldToLocalMatrix).ToArray();


        // all librairies
        for (int libId = 0; libId < 1; libId++)
        {
            // lib meta data
            int libIndex = pixels.Count;
            setLibraryMetadata(pixels);

            // all animations
            for (int anim_id = 0; anim_id < infos.Length; anim_id++)
            {
                var anim = infos[anim_id];
                bakeAnimation(pixels, anim);
            }

            // index de fin de la librairie
            pixels[libIndex + 3] = pixels.Count;
        }

        // Float count = 7 688
        // 7 688 / 4 floats = 1922 pixels
        // 1922 - 2 metadata = 1920
        // 1920 / 32 bones = 60
        // 60 / 15 frames = 4 pixels per bone per frame = 4 columns of a bone matrix

        int pixelCount = pixels.Count / 4;
        int width = 2048;
        int height = 1 + pixelCount / width;
        Vector4[] bakedVec4s = new Vector4[width * height];
        for (int i = 0; i < pixels.Count; i += 4)
        {
            bakedVec4s[i / 4] = new Vector4(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
        }

        SaveToTexture(pixels);
        WritePixelsToFile(bakedVec4s);
    }

    private void setLibraryMetadata(List<float> pixels)
    {
        pixels.Add(1); // number of libraries
        pixels.Add(1); // number of animations
        pixels.Add(skin.bones.Length); // number of bones
        pixels.Add(0); // index of the end of the library
    }
    private void setAnimationMetadata(List<float> pixels, int frameCount, float length)
    {
        var anim_pixel_index = pixels.Count;
        pixels.Add(length); // time length of the animation
        pixels.Add(0); // loop mode
        pixels.Add(frameCount); // number of frames in the animation
        pixels.Add(anim_pixel_index); // index of the start of the animation
    }
    private void bakeAnimation(List<float> pixels, AnimatorClipInfo anim)
    {

        float length = anim.clip.length;
        int frameCount = (int) Math.Ceiling(length * framerate);
        float adjustedTimePerFrame = length / (frameCount - 1);
        setAnimationMetadata(pixels, frameCount, length);
        float currenTime = 0;
        for (int frame = 0; frame < frameCount; frame++)
        {
            //Set Normalized Time
            animator.PlayInFixedTime(stateNameHash, 0, currenTime);
            //Force Update
            animator.Update(0f);
            bakeFrame(pixels, frame);
            currenTime += adjustedTimePerFrame;
        }

        Debug.Log($"Anim {anim.clip.name}, Bones {skin.bones.Length}, Length {length}, Framecount {frameCount}, AdjustedTimePerFrame {adjustedTimePerFrame}, Float count {pixels.Count}");
    }
    private void bakeFrame(List<float> pixels, int frame)
    {
        for (int b = 0; b < skin.bones.Length; b++)
        {
            var bone = skin.bones[b];
            var bindpose = getPose(b);
            var transform = bone.localToWorldMatrix * bindpose;
            PushBone(pixels, transform);
        }
    }
    private void PushBone(List<float> pixels, Matrix4x4 transform)
    {
        // row
        for (int i = 0; i < 4; i++)
        {
            // column
            for (int j = 0; j < 4; j++)
            {
                var val = transform[i, j];
                val = (float) Math.Round(val, 5);
                pixels.Add(val);
            }
        }
    }


    private void SaveToTexture(List<float> pixels)
    {
        // Convert float array to Color array.
        var pixel_count = pixels.Count / 4;
        var tex_width = 2048;
        var tex_height = pixel_count / tex_width + 1;
        var totalSize = tex_width * tex_height;
        var bakedPixels = new Color[totalSize];
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
            }
        }
        var bakedTexture = new Texture2D(tex_width, tex_height, TextureFormat.RGBAFloat, false);
        bakedTexture.filterMode = FilterMode.Point;
        bakedTexture.SetPixels(bakedPixels);
        bakedTexture.Apply();
        byte[] bytes = bakedTexture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/BoneTransforms.png", bytes);
    }

    private string filePath = Application.dataPath + "/gpuAnimationTransforms.txt"; // Path to save the file

    public void WritePixelsToFile(Vector4[] pixels)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var pixel in pixels)
            {
                writer.WriteLine($"{pixel.x} {pixel.y} {pixel.z} {pixel.w}");
            }
            writer.Flush();
            writer.Close();
        }
        Debug.Log($"Pixels {pixels.Length} written to {filePath}");
    }

    public List<Vector4> ReadMatricesFromFile()
    {
        List<Vector4> pixels = new();

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
                var pixel = new Vector4(
                    float.Parse(values[0]),
                    float.Parse(values[1]),
                    float.Parse(values[2]),
                    float.Parse(values[3])
                );
                pixels.Add(pixel);
            }
        }
        return pixels;
    }

}
