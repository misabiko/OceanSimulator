using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanMeshGenerator : MonoBehaviour {
	Mesh mesh;

	Vector3[] vertices;
	int[] triangles;

	[Header("Bleh")]
	//TODO Make square
	[SerializeField, Min(0)]
	int xSize = 20;

	[SerializeField, Min(0)] int zSize = 20;
	[Min(0)] public float size = 100f;
	[Min(0)] public float noiseResolution = 10f;

	[Min(0)] public float F = 1400000;
	[Min(0)] public float U10 = 20;
	[Min(0)] public float gamma = 3.3f;

	[Header("Phillips Spectrum")] [Min(0)] public float phillipsA;
	[Min(0)] public float phillipsSmallLength;
	public Vector2 phillipsWindDir;

	[Header("Bleh")] [Min(0)] public float timeScale = 1;
	public float timeOffset;
	public float heightTest = 20;
	public Vector2 test2d = new(0, 0);
	public Vector2 test2d2 = new(0, 0);
	public Vector2 test2d3 = new(0, 0);
	public Vector2 test2d4 = new(0, 0);
	public Vector2 test2d5 = new(0, 0);
	public float dtest1 = 0;
	public float dtest2 = 0;
	public float dtest3 = 0;
	public Vector3 normalTestX = Vector3.one;
	public Vector3 normalTestZ = Vector3.one;
	public float normalTest2 = 2f;
	public Vector3 normalTest3 = Vector3.zero;

	[SerializeField] ComputeShader computeShaderSource;
	[SerializeField] ComputeShader spectrumComputeShaderSource;
	[SerializeField] ComputeShader rreusserFFTSource;
	ComputeShader computeShader;
	ComputeShader spectrumComputeShader;
	ComputeShader rreusserFFT;
	[HideInInspector] public RenderTexture displacement;
	[HideInInspector] public RenderTexture HX;
	[HideInInspector] public RenderTexture HY;
	[HideInInspector] public RenderTexture HZ;
	[HideInInspector] public RenderTexture HX2;
	[HideInInspector] public RenderTexture HY2;
	[HideInInspector] public RenderTexture HZ2;
	[HideInInspector] public RenderTexture NY;
	[HideInInspector] public RenderTexture NY2;
	[HideInInspector] public RenderTexture approximateNormals;
	[HideInInspector] public RenderTexture pingBuffer;
	[HideInInspector] public RenderTexture pongBuffer;
	[HideInInspector] public RenderTexture waveNumberTexture;
	Texture2D noiseTexture;

	[SerializeField] Vector2 waveVector = Vector2.one;
	[SerializeField] float amplitude = 1;
	[SerializeField] float angularFrequency = 1;
	[SerializeField] float height = 1;

	// [Header("SimpleSinusoid")]
	// [SerializeField] ComputeShader simpleSinusoid;
	// public Vector3 simpleSinusoidAmplitude = Vector3.one;
	// public Vector2 simpleSinusoidFrequency = Vector2.one;
	// public Vector2 simpleSinusoidAngularFrequency = Vector2.one;
	// public Vector2 simpleSinusoidPhase = Vector2.zero;

	Material material;

	void Awake() {
		computeShader = Instantiate(computeShaderSource);
		spectrumComputeShader = Instantiate(spectrumComputeShaderSource);
		rreusserFFT = Instantiate(rreusserFFTSource);
	}

	void Start() {
		//For now, limiting to 512
		Debug.Assert(xSize <= 512 && zSize <= 512);

		mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		GetComponent<MeshFilter>().mesh = mesh;
		material = GetComponent<Renderer>().material;

		CreateShape();
		UpdateMesh();

		waveNumberTexture = CreateRenderTexture(xSize, zSize);
		spectrumComputeShader.SetTexture(0, "WaveNumber", waveNumberTexture);
		noiseTexture = CreateTexture(xSize, zSize);
		int greenNoise = Random.Range(0, 10000);
		for (int x = 0; x < xSize; ++x)
		for (int y = 0; y < zSize; ++y)
			noiseTexture.SetPixel(x, y, new Color(Mathf.PerlinNoise(x / noiseResolution, y / noiseResolution), Mathf.PerlinNoise(x / noiseResolution + greenNoise, y / noiseResolution + greenNoise), 0, 0));
		// noiseTexture.SetPixel(x, y, new Color(.5f, .5f, 0, 0));
		noiseTexture.Apply();
		spectrumComputeShader.SetTexture(0, "Noise", noiseTexture);
		spectrumComputeShader.SetFloat("Resolution", xSize);
		spectrumComputeShader.SetFloat("PI", Mathf.PI);
		spectrumComputeShader.SetFloat("g", -Physics.gravity.y);
		//TODO Create every textures in a block
		HX = CreateRenderTexture(xSize, zSize);
		HY = CreateRenderTexture(xSize, zSize);
		HZ = CreateRenderTexture(xSize, zSize);
		HX2 = CreateRenderTexture(xSize, zSize);
		HY2 = CreateRenderTexture(xSize, zSize);
		HZ2 = CreateRenderTexture(xSize, zSize);
		NY = CreateRenderTexture(xSize, zSize);
		NY2 = CreateRenderTexture(xSize, zSize);
		approximateNormals = CreateRenderTexture(xSize, zSize);
		pingBuffer = CreateRenderTexture(xSize, zSize);
		pongBuffer = CreateRenderTexture(xSize, zSize);
		spectrumComputeShader.SetTexture(0, "HX", HX);
		spectrumComputeShader.SetTexture(0, "HY", HY);
		spectrumComputeShader.SetTexture(0, "HZ", HZ);
		spectrumComputeShader.SetTexture(0, "NY", NY);

		displacement = CreateRenderTexture(xSize, zSize);
		computeShader.SetTexture(0, "Displacement", displacement);
		computeShader.SetTexture(0, "HX", HX);
		computeShader.SetTexture(0, "HY", HY);
		computeShader.SetTexture(0, "HZ", HZ);
		computeShader.SetTexture(0, "HX2", HX2);
		computeShader.SetTexture(0, "HY2", HY2);
		computeShader.SetTexture(0, "HZ2", HZ2);
		computeShader.SetTexture(0, "approximateNormals", approximateNormals);
		computeShader.SetFloat("Resolution", xSize);
		computeShader.SetFloat("PI", Mathf.PI);
		computeShader.SetFloat("g", -Physics.gravity.y);

		// simpleSinusoid.SetTexture(0, "HX2", HX2);
		// simpleSinusoid.SetTexture(0, "HY2", HY2);
		// simpleSinusoid.SetTexture(0, "HZ2", HZ2);

		SetupComputeShader();

		material.SetTexture("_Displacement", displacement);
		material.SetTexture("_NormalMap", NY2);
		material.SetTexture("_ApproximateNormalMap", approximateNormals);

		GameObject.Find("RenderTextureDisplay").GetComponent<Renderer>().material.mainTexture = displacement;
		GameObject.Find("RenderTextureNoise").GetComponent<Renderer>().material.mainTexture = noiseTexture;
		GameObject.Find("RenderTextureWaveNumber").GetComponent<Renderer>().material.mainTexture = waveNumberTexture;
		GameObject.Find("RenderTextureHY").GetComponent<Renderer>().material.mainTexture = HY;
		GameObject.Find("RenderTextureHX").GetComponent<Renderer>().material.mainTexture = HX;
		GameObject.Find("RenderTextureHZ").GetComponent<Renderer>().material.mainTexture = HZ;
		GameObject.Find("RenderTextureHY2").GetComponent<Renderer>().material.mainTexture = HY2;
		GameObject.Find("RenderTextureHX2").GetComponent<Renderer>().material.mainTexture = HX2;
		GameObject.Find("RenderTextureHZ2").GetComponent<Renderer>().material.mainTexture = HZ2;

		var uiDocument = GetComponent<UIDocument>();
		if (uiDocument.enabled) {
			uiDocument.rootVisualElement.Add(new Image { image = displacement });
			// uiDocument.rootVisualElement.Add(new Image { image = noiseTexture });
			// uiDocument.rootVisualElement.Add(new Image { image = waveNumberTexture });
			uiDocument.rootVisualElement.Add(new Image { image = HX });
			uiDocument.rootVisualElement.Add(new Image { image = HY });
			uiDocument.rootVisualElement.Add(new Image { image = HZ });
			uiDocument.rootVisualElement.Add(new Image { image = HX2 });
			uiDocument.rootVisualElement.Add(new Image { image = HY2 });
			uiDocument.rootVisualElement.Add(new Image { image = HZ2 });
			uiDocument.rootVisualElement.Add(new Image { image = NY });
			uiDocument.rootVisualElement.Add(new Image { image = NY2 });
			uiDocument.rootVisualElement.Add(new Image { image = approximateNormals });
			uiDocument.rootVisualElement.Add(new Image { image = pingBuffer });
			uiDocument.rootVisualElement.Add(new Image { image = pongBuffer });
		}
	}

	void SetupComputeShader() {
		spectrumComputeShader.SetFloats("test", test2d.x, test2d.y);
		spectrumComputeShader.SetFloats("test2", test2d2.x, test2d2.y);
		spectrumComputeShader.SetFloats("test3", test2d3.x, test2d3.y);
		spectrumComputeShader.SetFloats("test4", test2d4.x, test2d4.y);
		spectrumComputeShader.SetFloats("test5", test2d5.x, test2d5.y);
		spectrumComputeShader.SetFloat("time", Time.time * timeScale + timeOffset);
		spectrumComputeShader.SetFloat("L", size);
		spectrumComputeShader.SetFloat("F", F);
		spectrumComputeShader.SetFloat("U10", U10);
		spectrumComputeShader.SetFloat("gamma", gamma);
		spectrumComputeShader.SetFloat("heightTest", heightTest);
		spectrumComputeShader.SetFloat("phillipsA", phillipsA);
		spectrumComputeShader.SetFloat("phillipsSmallLength", phillipsSmallLength);
		spectrumComputeShader.SetVector("phillipsWindDir", phillipsWindDir.normalized);
		spectrumComputeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);

		//HY
		{
			int subtransformSize = 2;
			bool pingpong = true;
			int i = 0;
			while (subtransformSize <= displacement.width) {
				if (i == 0) {
					rreusserFFT.SetTexture(0, "src", HY);
					rreusserFFT.SetTexture(0, "output", pongBuffer);
				}
				else {
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
				rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
				subtransformSize *= 2;
				pingpong = !pingpong;
				++i;
			}

			subtransformSize = 2;
			i = 0;
			while (subtransformSize <= displacement.height) {
				if (subtransformSize == displacement.height) {
					rreusserFFT.SetTexture(0, "src", pongBuffer);
					rreusserFFT.SetTexture(0, "output", HY2);
				}
				else {
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

		//HX
		{
			int subtransformSize = 2;
			bool pingpong = true;
			int i = 0;
			while (subtransformSize <= displacement.width) {
				if (i == 0) {
					rreusserFFT.SetTexture(0, "src", HX);
					rreusserFFT.SetTexture(0, "output", pongBuffer);
				}
				else {
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
			while (subtransformSize <= displacement.height) {
				if (subtransformSize == displacement.height) {
					rreusserFFT.SetTexture(0, "src", pongBuffer);
					rreusserFFT.SetTexture(0, "output", HX2);
				}
				else {
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
		//HZ
		{
			int subtransformSize = 2;
			bool pingpong = true;
			int i = 0;
			while (subtransformSize <= displacement.width) {
				if (i == 0) {
					rreusserFFT.SetTexture(0, "src", HZ);
					rreusserFFT.SetTexture(0, "output", pongBuffer);
				}
				else {
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
			while (subtransformSize <= displacement.height) {
				if (subtransformSize == displacement.height) {
					rreusserFFT.SetTexture(0, "src", pongBuffer);
					rreusserFFT.SetTexture(0, "output", HZ2);
				}
				else {
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

		//NY
		{
			int subtransformSize = 2;
			bool pingpong = true;
			int i = 0;
			while (subtransformSize <= displacement.width) {
				if (i == 0) {
					rreusserFFT.SetTexture(0, "src", NY);
					rreusserFFT.SetTexture(0, "output", pongBuffer);
				}
				else {
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
				rreusserFFT.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
				subtransformSize *= 2;
				pingpong = !pingpong;
				++i;
			}

			subtransformSize = 2;
			i = 0;
			while (subtransformSize <= displacement.height) {
				if (subtransformSize == displacement.height) {
					rreusserFFT.SetTexture(0, "src", pongBuffer);
					rreusserFFT.SetTexture(0, "output", NY2);
				}
				else {
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

		// SetupSimpleSinusoid();

		computeShader.SetFloat("dtest1", dtest1);
		computeShader.SetFloat("dtest2", dtest2);
		computeShader.SetFloat("dtest3", dtest3);
		computeShader.SetVector("normalTestX", normalTestX);
		computeShader.SetVector("normalTestZ", normalTestZ);
		computeShader.SetFloat("normalTest2", normalTest2);
		computeShader.SetVector("normalTest3", normalTest3);
		computeShader.SetVector("waveVector", waveVector);
		computeShader.SetFloat("amplitude", amplitude);
		computeShader.SetFloat("angularFrequency", angularFrequency);
		computeShader.SetFloat("time", Time.time * timeScale + timeOffset);
		computeShader.SetFloat("L", size);
		computeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
	}

	// void SetupSimpleSinusoid() {
	// 	simpleSinusoid.SetFloat("time", Time.time * timeScale + timeOffset);
	// 	simpleSinusoid.SetFloat("resolution", xSize);
	// 	simpleSinusoid.SetVector("frequency", simpleSinusoidFrequency);
	// 	simpleSinusoid.SetVector("angularFrequency", simpleSinusoidAngularFrequency);
	// 	simpleSinusoid.SetVector("amplitude", simpleSinusoidAmplitude);
	// 	simpleSinusoid.SetVector("phase", simpleSinusoidPhase);
	// 	simpleSinusoid.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
	// }

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