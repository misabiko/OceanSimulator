using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Complex32;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanMeshGenerator : MonoBehaviour {
	Mesh mesh;

	Vector3[] vertices;
	int[] triangles;

	[SerializeField, Min(0)] int xSize = 20;
	[SerializeField, Min(0)] int zSize = 20;
	[Min(0)]
	public float size = 100f;
	[Min(0)]
	public float noiseResolution = 10f;

	[Min(0)]
	public float F = 1400000;
	[Min(0)]
	public float U10 = 20;
	[Min(0)]
	public float gamma = 3.3f;

	[Min(0)]
	public float timeScale = 1;
	public float heightTest = 20;
	public Vector2 test2d = new(0, 0);
	public Vector2 test2d2 = new(0, 0);
	public Vector2 test2d3 = new(0, 0);

	[SerializeField] ComputeShader computeShader;
	[SerializeField] ComputeShader spectrumComputeShader;
	[SerializeField] ComputeShader rreusserFFT;
	RenderTexture displacement;
	RenderTexture HX;
	RenderTexture HY;
	RenderTexture HZ;
	RenderTexture HX2;
	RenderTexture HY2;
	RenderTexture HZ2;
	RenderTexture waveNumberTexture;
	Texture2D noiseTexture;
	RenderTexture heightSpectrumTexture;

	[SerializeField] Vector2 waveVector = Vector2.one;
	[SerializeField] float amplitude = 1;
	[SerializeField] float angularFrequency = 1;
	[SerializeField] float height = 1;

	Material material;

	void Start() {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		material = GetComponent<Renderer>().material;

		CreateShape();
		UpdateMesh();

		heightSpectrumTexture = CreateRenderTexture(xSize, zSize);
		spectrumComputeShader.SetTexture(0, "Result", heightSpectrumTexture);
		waveNumberTexture = CreateRenderTexture(xSize, zSize);
		spectrumComputeShader.SetTexture(0, "WaveNumber", waveNumberTexture);
		noiseTexture = CreateTexture(xSize, zSize);
		int greenNoise = Random.Range(0,10000);
		for (int x = 0; x < xSize; ++x)
			for (int y = 0; y < zSize; ++y)
				noiseTexture.SetPixel(x, y, new Color(Mathf.PerlinNoise(x / noiseResolution, y / noiseResolution), Mathf.PerlinNoise(x / noiseResolution + greenNoise, y / noiseResolution + greenNoise), 0, 0));
		noiseTexture.Apply();
		spectrumComputeShader.SetTexture(0, "Noise", noiseTexture);
		spectrumComputeShader.SetFloat("Resolution", xSize);
		spectrumComputeShader.SetFloat("PI", Mathf.PI);
		spectrumComputeShader.SetFloat("g", -Physics.gravity.y);
		HX = CreateRenderTexture(xSize, zSize);
		spectrumComputeShader.SetTexture(0, "HX", HX);
		HY = CreateRenderTexture(xSize, zSize);
		spectrumComputeShader.SetTexture(0, "HY", HY);
		HZ = CreateRenderTexture(xSize, zSize);
		spectrumComputeShader.SetTexture(0, "HZ", HZ);
		
		displacement = CreateRenderTexture(xSize, zSize);
		computeShader.SetTexture(0, "Displacement", displacement);
		computeShader.SetTexture(0, "HeightSpectrum", heightSpectrumTexture);
		// computeShader.SetTexture(0, "HX", HX);
		// computeShader.SetTexture(0, "HY", HY);
		// computeShader.SetTexture(0, "HZ", HZ);
		HX2 = CreateRenderTexture(xSize, zSize);
		computeShader.SetTexture(0, "HX2", HX2);
		HY2 = CreateRenderTexture(xSize, zSize);
		computeShader.SetTexture(0, "HY2", HY2);
		HZ2 = CreateRenderTexture(xSize, zSize);
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
	}

	void SetupComputeShader() {
		spectrumComputeShader.SetFloat("time", Time.time * timeScale);
		spectrumComputeShader.SetFloat("L", size);
		spectrumComputeShader.SetFloat("F", F);
		spectrumComputeShader.SetFloat("U10", U10);
		spectrumComputeShader.SetFloat("gamma", gamma);
		spectrumComputeShader.SetFloat("heightTest", heightTest);
		spectrumComputeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
		
		// // var hx = CreateTexture(displacement.width, displacement.height);
		// // RenderTexture.active = HX;
		// // hx.ReadPixels(new Rect(0, 0, displacement.width, displacement.height), 0, 0);
		// // hx.Apply();
		// var hy = CreateTexture(displacement.width, displacement.height);
		// RenderTexture.active = HY;
		// hy.ReadPixels(new Rect(0, 0, displacement.width, displacement.height), 0, 0);
		// hy.Apply();
		// // var hz = CreateTexture(displacement.width, displacement.height);
		// // RenderTexture.active = HZ;
		// // hz.ReadPixels(new Rect(0, 0, displacement.width, displacement.height), 0, 0);
		// // hz.Apply();
		//
		// // var array = new Complex32[displacement.width, displacement.height];
		// // for (int x = 0; x < displacement.width; ++x)
		// // 	for (int y = 0; y < displacement.height; ++y)
		// // 		array[x, y] = ;
		// var matrix = DenseMatrix.Create(displacement.width, displacement.height, (i, j) => new Complex32(hy.GetPixel(i, j).r, hy.GetPixel(i, j).g));
		// // DenseMatrix.OfArray()
		// Fourier.Inverse2D(matrix);
		// for (int x = 0; x < displacement.width; ++x) {
		// 	for (int y = 0; y < displacement.height; ++y) {
		// 		hy.SetPixel(x, y, new Color(matrix[x, y].Real, matrix[x, y].Imaginary, 0, 0));
		// 	}
		// }
		// hy.Apply();
		// computeShader.SetTexture(0, "HY", HY);
		
		int subtransformSize = displacement.width;
		bool pingpong = true;
		int i = 0;
		while (subtransformSize > 1) {
			rreusserFFT.SetTexture(0, "src", pingpong ? HY : HY2);
			rreusserFFT.SetTexture(0, "output", pingpong ? HY2 : HY);
			rreusserFFT.SetFloats("resolution", displacement.width, displacement.height);
			rreusserFFT.SetFloat("subtransformSize", subtransformSize);
			rreusserFFT.SetBool("horizontal", true);
			rreusserFFT.SetBool("forward", false);
			if (i == 0)
				rreusserFFT.SetFloat("normalization", 1f / displacement.width);
				// rreusserFFT.SetFloat("normalization", 1f / (displacement.width * displacement.height));
			else
				rreusserFFT.SetFloat("normalization", 1f);
			rreusserFFT.SetFloats("test", test2d.x, test2d.y);
			rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
			rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
			rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
			subtransformSize /= 2;
			pingpong = !pingpong;
			++i;
		}
		print("Horizontal: " + i);
		
		subtransformSize = displacement.height;
		pingpong = true;
		i = 0;
		while (subtransformSize > 1) {
			rreusserFFT.SetTexture(0, "src", pingpong ? HY : HY2);
			rreusserFFT.SetTexture(0, "output", pingpong ? HY2 : HY);
			rreusserFFT.SetFloats("resolution", displacement.width, displacement.height);
			rreusserFFT.SetFloat("subtransformSize", subtransformSize);
			rreusserFFT.SetBool("horizontal", false);
			rreusserFFT.SetBool("forward", false);
			if (i == 0)
				rreusserFFT.SetFloat("normalization", 1f / displacement.height);
			// rreusserFFT.SetFloat("normalization", 1f / (displacement.width * displacement.height));
			else
				rreusserFFT.SetFloat("normalization", 1f);
			rreusserFFT.SetFloat("normalization", 1f);
			rreusserFFT.SetFloats("test", test2d.x, test2d.y);
			rreusserFFT.SetFloats("test2", test2d2.x, test2d2.y);
			rreusserFFT.SetFloats("test3", test2d3.x, test2d3.y);
			rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
			subtransformSize /= 2;
			pingpong = !pingpong;
			++i;
		}
		print("Vertical: " + i);
		print(subtransformSize);
		
		
		computeShader.SetTexture(0, "HY", HY2);
		computeShader.SetVector("waveVector", waveVector);
		computeShader.SetFloat("amplitude", amplitude);
		computeShader.SetFloat("angularFrequency", angularFrequency);
		computeShader.SetFloat("time", Time.time * timeScale);
		computeShader.SetFloat("L", size);
		computeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
	}

	void Update() {
		if (vertices.Length != (xSize + 1) * (zSize + 1)) {
			CreateShape();
			UpdateMesh();
		}

		material.SetFloat("_Height", height);
		material.SetVector("_Resolution", new Vector2(size, size));
		SetupComputeShader();
	}

	void CreateShape() {
		vertices = new Vector3[(xSize + 1) * (zSize + 1)];

		for (int i = 0, z = 0; z <= zSize; ++z)
		for (int x = 0; x <= xSize; ++x) {
			vertices[i] = new Vector3((float)x / xSize, 0f, (float)z / zSize) * size;
			++i;
		}
		triangles = new int[xSize * zSize * 6];

		int vert = 0;
		int tris = 0;
		for (int z = 0; z < zSize; ++z) {
			for (int x = 0; x < xSize; ++x) {
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

	void UpdateMesh() {
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateNormals();
	}

	static RenderTexture CreateRenderTexture(int width, int height) {
		var rt = new RenderTexture(width, height, 24) {
			enableRandomWrite = true,
			filterMode = FilterMode.Point,
			graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
		};
		rt.Create();
		return rt;
	}

	static Texture2D CreateTexture(int width, int height) {
		return new Texture2D(width, height) {
			filterMode = FilterMode.Point,
		};
	}
}