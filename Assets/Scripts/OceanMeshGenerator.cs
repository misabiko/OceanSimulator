using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanMeshGenerator : MonoBehaviour
{
    [SerializeField] [Min(0)] private int xSize = 20;
    [SerializeField] [Min(0)] private int zSize = 20;
    [Min(0)] public float size = 100f;
    [Min(0)] public float noiseResolution = 10f;

    [Min(0)] public float F = 1400000;
    [Min(0)] public float U10 = 20;
    [Min(0)] public float gamma = 3.3f;

    [Min(0)] public float timeScale = 1;
    public float heightTest = 20;
    public Vector2 test2d = new(0, 0);
    public Vector2 test2d2 = new(0, 0);
    public Vector2 test2d3 = new(0, 0);
    public Vector2 test2d4 = new(0, 0);
    public Vector2 test2d5 = new(0, 0);
    public float dtest1;
    public float dtest2;
    public float dtest3;

    [Range(0, 15)] public int maxIteractions = 15;

    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private ComputeShader spectrumComputeShader;
    [SerializeField] private ComputeShader rreusserFFT;
    public RenderTexture displacement;

    [SerializeField] private Vector2 waveVector = Vector2.one;
    [SerializeField] private float amplitude = 1;
    [SerializeField] private float angularFrequency = 1;
    [SerializeField] private float height = 1;
    private RenderTexture displacement2;
    private RenderTexture HX;
    private RenderTexture HX2;
    private RenderTexture HY;
    private RenderTexture HY2;
    private RenderTexture HZ;
    private RenderTexture HZ2;

    private Material material;
    private Mesh mesh;
    private Texture2D noiseTexture;
    private RenderTexture pingBuffer;
    private RenderTexture pongBuffer;
    private int[] triangles;

    private Vector3[] vertices;
    private RenderTexture waveNumberTexture;

    private void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        material = GetComponent<Renderer>().material;

        CreateShape();
        UpdateMesh();

        waveNumberTexture = CreateRenderTexture(xSize, zSize);
        spectrumComputeShader.SetTexture(0, "WaveNumber", waveNumberTexture);
        noiseTexture = CreateTexture(xSize, zSize);
        var greenNoise = Random.Range(0, 10000);
        for (var x = 0; x < xSize; ++x)
        for (var y = 0; y < zSize; ++y)
            noiseTexture.SetPixel(x, y,
                new Color(Mathf.PerlinNoise(x / noiseResolution, y / noiseResolution),
                    Mathf.PerlinNoise(x / noiseResolution + greenNoise, y / noiseResolution + greenNoise), 0, 0));
        // noiseTexture.SetPixel(x, y, new Color(.5f, .5f, 0, 0));
        noiseTexture.Apply();
        spectrumComputeShader.SetTexture(0, "Noise", noiseTexture);
        spectrumComputeShader.SetFloat("Resolution", xSize);
        spectrumComputeShader.SetFloat("PI", Mathf.PI);
        spectrumComputeShader.SetFloat("g", -Physics.gravity.y);
        HX = CreateRenderTexture(xSize, zSize);
        HY = CreateRenderTexture(xSize, zSize);
        HZ = CreateRenderTexture(xSize, zSize);
        HX2 = CreateRenderTexture(xSize, zSize);
        HY2 = CreateRenderTexture(xSize, zSize);
        HZ2 = CreateRenderTexture(xSize, zSize);
        pingBuffer = CreateRenderTexture(xSize, zSize);
        pongBuffer = CreateRenderTexture(xSize, zSize);
        spectrumComputeShader.SetTexture(0, "HX", HX);
        spectrumComputeShader.SetTexture(0, "HY", HY);
        spectrumComputeShader.SetTexture(0, "HZ", HZ);
        spectrumComputeShader.SetTexture(0, "HX2", HX2);
        spectrumComputeShader.SetTexture(0, "HY2", pongBuffer);
        spectrumComputeShader.SetTexture(0, "HZ2", HZ2);

        displacement = CreateRenderTexture(xSize, zSize);
        computeShader.SetTexture(0, "Displacement", displacement);
        computeShader.SetTexture(0, "HX", HX);
        computeShader.SetTexture(0, "HY", HY);
        computeShader.SetTexture(0, "HZ", HZ);
        computeShader.SetTexture(0, "HX2", HX2);
        computeShader.SetTexture(0, "HY2", HY2);
        computeShader.SetTexture(0, "HZ2", HZ2);
        computeShader.SetFloat("Resolution", xSize);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("g", -Physics.gravity.y);
        SetupComputeShader();

        material.SetTexture("_Displacement", displacement);

        //TODO Try rendering texture to UI?
        GameObject.Find("RenderTextureDisplay").GetComponent<Renderer>().material.mainTexture = displacement;
        GameObject.Find("RenderTextureNoise").GetComponent<Renderer>().material.mainTexture = noiseTexture;
        GameObject.Find("RenderTextureWaveNumber").GetComponent<Renderer>().material.mainTexture = waveNumberTexture;
        GameObject.Find("RenderTextureHY").GetComponent<Renderer>().material.mainTexture = HY;
        GameObject.Find("RenderTextureHX").GetComponent<Renderer>().material.mainTexture = HX;
        GameObject.Find("RenderTextureHZ").GetComponent<Renderer>().material.mainTexture = HZ;
        GameObject.Find("RenderTextureHY2").GetComponent<Renderer>().material.mainTexture = HY2;
        GameObject.Find("RenderTextureHX2").GetComponent<Renderer>().material.mainTexture = HX2;
        GameObject.Find("RenderTextureHZ2").GetComponent<Renderer>().material.mainTexture = HZ2;
    }

    private void Update()
    {
        if (vertices.Length != (xSize + 1) * (zSize + 1))
        {
            CreateShape();
            UpdateMesh();
        }

        material.SetFloat("_Height", height);
        material.SetVector("_Resolution", new Vector2(size, size));
        SetupComputeShader();
    }

    private void SetupComputeShader()
    {
        spectrumComputeShader.SetFloats("test", test2d.x, test2d.y);
        spectrumComputeShader.SetFloats("test2", test2d2.x, test2d2.y);
        spectrumComputeShader.SetFloats("test3", test2d3.x, test2d3.y);
        spectrumComputeShader.SetFloats("test4", test2d4.x, test2d4.y);
        spectrumComputeShader.SetFloats("test5", test2d5.x, test2d5.y);
        spectrumComputeShader.SetFloat("time", Time.time * timeScale);
        spectrumComputeShader.SetFloat("L", size);
        spectrumComputeShader.SetFloat("F", F);
        spectrumComputeShader.SetFloat("U10", U10);
        spectrumComputeShader.SetFloat("gamma", gamma);
        spectrumComputeShader.SetFloat("heightTest", heightTest);
        spectrumComputeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);

        //Y
        {
            var subtransformSize = 2;
            var pingpong = true;
            var i = 0;
            while (subtransformSize <= displacement.width && i < maxIteractions)
            {
                if (i == 0)
                {
                    rreusserFFT.SetTexture(0, "src", HY);
                    rreusserFFT.SetTexture(0, "output", pongBuffer);
                }
                else
                {
                    rreusserFFT.SetTexture(0, "src", pingpong ? pingBuffer : pongBuffer);
                    rreusserFFT.SetTexture(0, "output", pingpong ? pongBuffer : pingBuffer);
                }

                rreusserFFT.SetFloats("resolution", 1f / displacement.width, 1f / displacement.height);
                rreusserFFT.SetFloat("subtransformSize", subtransformSize);
                rreusserFFT.SetBool("horizontal", true);
                rreusserFFT.SetBool("forward", false);
                rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloats("test", test2d.x, test2d.y);
                rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
                rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
                rreusserFFT.SetFloats("test4", test2d4.x, test2d4.y);
                rreusserFFT.SetFloats("test5", test2d5.x, test2d5.y);
                rreusserFFT.SetFloats("test5", test2d5.x, test2d5.y);
                rreusserFFT.SetFloats("test5", test2d5.x, test2d5.y);
                rreusserFFT.SetFloats("test5", test2d5.x, test2d5.y);
                rreusserFFT.SetFloats("test5", test2d5.x, test2d5.y);
                rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
                subtransformSize *= 2;
                pingpong = !pingpong;
                ++i;
            }

            subtransformSize = 2;
            // pingpong = true;
            i = 0;
            while (subtransformSize <= displacement.height && i < maxIteractions)
            {
                if (subtransformSize == displacement.height || i == maxIteractions - 1)
                {
                    rreusserFFT.SetTexture(0, "src", pongBuffer);
                    rreusserFFT.SetTexture(0, "output", HY2);
                }
                else
                {
                    rreusserFFT.SetTexture(0, "src", pingpong ? pingBuffer : pongBuffer);
                    rreusserFFT.SetTexture(0, "output", pingpong ? pongBuffer : pingBuffer);
                }

                rreusserFFT.SetFloats("resolution", 1f / displacement.width, 1f / displacement.height);
                rreusserFFT.SetFloat("subtransformSize", subtransformSize);
                rreusserFFT.SetBool("horizontal", false);
                rreusserFFT.SetBool("forward", false);
                if (i == Mathf.FloorToInt(Mathf.Log(displacement.height, 2)))
                    rreusserFFT.SetFloat("normalization", 1f / displacement.width / displacement.height);
                else
                    rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloats("test", test2d.x, test2d.y);
                rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
                rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
                rreusserFFT.SetFloats("test4", test2d4.x, test2d4.y);
                rreusserFFT.SetFloats("test5", test2d5.x, test2d5.y);
                rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
                subtransformSize *= 2;
                pingpong = !pingpong;
                ++i;
            }
        }

        //X
        {
            var subtransformSize = 2;
            var pingpong = true;
            var i = 0;
            while (subtransformSize <= displacement.width)
            {
                if (i == 0)
                {
                    rreusserFFT.SetTexture(0, "src", HX);
                    rreusserFFT.SetTexture(0, "output", pongBuffer);
                }
                else
                {
                    rreusserFFT.SetTexture(0, "src", pingpong ? pingBuffer : pongBuffer);
                    rreusserFFT.SetTexture(0, "output", pingpong ? pongBuffer : pingBuffer);
                }

                rreusserFFT.SetFloats("resolution", 1f / displacement.width, 1f / displacement.height);
                rreusserFFT.SetFloat("subtransformSize", subtransformSize);
                rreusserFFT.SetBool("horizontal", true);
                rreusserFFT.SetBool("forward", false);
                rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloats("test", test2d.x, test2d.y);
                rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
                rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
                rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
                subtransformSize *= 2;
                pingpong = !pingpong;
                ++i;
            }

            subtransformSize = 2;
            pingpong = true;
            i = 0;
            while (subtransformSize <= displacement.height)
            {
                if (subtransformSize == displacement.height || i == maxIteractions - 1)
                {
                    rreusserFFT.SetTexture(0, "src", pongBuffer);
                    rreusserFFT.SetTexture(0, "output", HX2);
                }
                else
                {
                    rreusserFFT.SetTexture(0, "src", pingpong ? pingBuffer : pongBuffer);
                    rreusserFFT.SetTexture(0, "output", pingpong ? pongBuffer : pingBuffer);
                }

                rreusserFFT.SetFloats("resolution", 1f / displacement.width, 1f / displacement.height);
                rreusserFFT.SetFloat("subtransformSize", subtransformSize);
                rreusserFFT.SetBool("horizontal", false);
                rreusserFFT.SetBool("forward", false);
                if (i == Mathf.FloorToInt(Mathf.Log(displacement.height, 2)))
                    rreusserFFT.SetFloat("normalization", 1f / displacement.width / displacement.height);
                else
                    rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloats("test", test2d.x, test2d.y);
                rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
                rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
                rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
                subtransformSize *= 2;
                pingpong = !pingpong;
                ++i;
            }
        }

        //TODO Merge HX and HZ in the same texture
        //Z
        {
            var subtransformSize = 2;
            var pingpong = true;
            var i = 0;
            while (subtransformSize <= displacement.width)
            {
                if (i == 0)
                {
                    rreusserFFT.SetTexture(0, "src", HZ);
                    rreusserFFT.SetTexture(0, "output", pongBuffer);
                }
                else
                {
                    rreusserFFT.SetTexture(0, "src", pingpong ? pingBuffer : pongBuffer);
                    rreusserFFT.SetTexture(0, "output", pingpong ? pongBuffer : pingBuffer);
                }

                rreusserFFT.SetFloats("resolution", 1f / displacement.width, 1f / displacement.height);
                rreusserFFT.SetFloat("subtransformSize", subtransformSize);
                rreusserFFT.SetBool("horizontal", true);
                rreusserFFT.SetBool("forward", false);
                rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloats("test", test2d.x, test2d.y);
                rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
                rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
                rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
                subtransformSize *= 2;
                pingpong = !pingpong;
                ++i;
            }

            subtransformSize = 2;
            pingpong = true;
            i = 0;
            while (subtransformSize <= displacement.height)
            {
                if (subtransformSize == displacement.height || i == maxIteractions - 1)
                {
                    rreusserFFT.SetTexture(0, "src", pongBuffer);
                    rreusserFFT.SetTexture(0, "output", HZ2);
                }
                else
                {
                    rreusserFFT.SetTexture(0, "src", pingpong ? pingBuffer : pongBuffer);
                    rreusserFFT.SetTexture(0, "output", pingpong ? pongBuffer : pingBuffer);
                }

                rreusserFFT.SetFloats("resolution", 1f / displacement.width, 1f / displacement.height);
                rreusserFFT.SetFloat("subtransformSize", subtransformSize);
                rreusserFFT.SetBool("horizontal", false);
                rreusserFFT.SetBool("forward", false);
                if (i == Mathf.FloorToInt(Mathf.Log(displacement.height, 2)))
                    rreusserFFT.SetFloat("normalization", 1f / displacement.width / displacement.height);
                else
                    rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloat("normalization", 1f);
                rreusserFFT.SetFloats("test", test2d.x, test2d.y);
                rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
                rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
                rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
                subtransformSize *= 2;
                pingpong = !pingpong;
                ++i;
            }
        }

        computeShader.SetFloat("dtest1", dtest1);
        computeShader.SetFloat("dtest2", dtest2);
        computeShader.SetFloat("dtest3", dtest3);
        computeShader.SetVector("waveVector", waveVector);
        computeShader.SetFloat("amplitude", amplitude);
        computeShader.SetFloat("angularFrequency", angularFrequency);
        computeShader.SetFloat("time", Time.time * timeScale);
        computeShader.SetFloat("L", size);
        computeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
    }

    private void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; ++z)
        for (var x = 0; x <= xSize; ++x)
        {
            vertices[i] = new Vector3((float)x / xSize, 0f, (float)z / zSize) * size;
            ++i;
        }

        triangles = new int[xSize * zSize * 6];

        var vert = 0;
        var tris = 0;
        for (var z = 0; z < zSize; ++z)
        {
            for (var x = 0; x < xSize; ++x)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                ++vert;
                tris += 6;
            }

            ++vert;
        }

        material.SetVector("_Resolution", new Vector2(size, size));
    }

    private void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }

    private static RenderTexture CreateRenderTexture(int width, int height)
    {
        var rt = new RenderTexture(width, height, 24)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat
        };
        rt.Create();
        return rt;
    }

    private static Texture2D CreateTexture(int width, int height)
    {
        return new Texture2D(width, height)
        {
            filterMode = FilterMode.Point
        };
    }
}