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

	public float F = 1400000;
	public float U10 = 20;
	public float gamma = 3.3f;

	public float heightTest = 20;

	[SerializeField] ComputeShader computeShader;
	[SerializeField] ComputeShader spectrumComputeShader;
	RenderTexture displacement;
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

		heightSpectrumTexture = CreateRenderTexture(xSize, zSize, 24);
		spectrumComputeShader.SetTexture(0, "Result", heightSpectrumTexture);
		waveNumberTexture = CreateRenderTexture(xSize, zSize, 24);
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
		
		displacement = new RenderTexture(xSize, zSize, 24);
		displacement.enableRandomWrite = true;
		displacement.Create();
		computeShader.SetTexture(0, "Result", displacement);
		computeShader.SetTexture(0, "HeightSpectrum", heightSpectrumTexture);
		computeShader.SetFloat("Resolution", xSize);
		computeShader.SetFloat("PI", Mathf.PI);
		computeShader.SetFloat("g", -Physics.gravity.y);
		SetupComputeShader();

		material.SetTexture("_Displacement", displacement);

		//TODO Try rendering texture to UI?
		GameObject.Find("RenderTextureDisplay").GetComponent<Renderer>().material.mainTexture = displacement;
		GameObject.Find("RenderTextureNoise").GetComponent<Renderer>().material.mainTexture = noiseTexture;
		GameObject.Find("RenderTextureWaveNumber").GetComponent<Renderer>().material.mainTexture = waveNumberTexture;
		GameObject.Find("RenderTextureHeight").GetComponent<Renderer>().material.mainTexture = heightSpectrumTexture;
	}

	void SetupComputeShader() {
		spectrumComputeShader.SetFloat("time", Time.time);
		spectrumComputeShader.SetFloat("L", size);
		spectrumComputeShader.SetFloat("F", F);
		spectrumComputeShader.SetFloat("U10", U10);
		spectrumComputeShader.SetFloat("gamma", gamma);
		spectrumComputeShader.SetFloat("heightTest", heightTest);
		spectrumComputeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
		
		computeShader.SetVector("waveVector", waveVector);
		computeShader.SetFloat("amplitude", amplitude);
		computeShader.SetFloat("angularFrequency", angularFrequency);
		computeShader.SetFloat("time", Time.time);
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

	static RenderTexture CreateRenderTexture(int width, int height, int depth) {
		var rt = new RenderTexture(width, height, depth) {
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