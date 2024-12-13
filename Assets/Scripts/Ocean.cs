using UnityEngine;

public class Ocean : MonoBehaviour {
	[Min(0)] public int tileRadius;
	[Min(0)] public int tileSideVertexCount = 256;
	[Min(0)] public float tileSize = 64;
	[SerializeField] Material tileMaterial;

	// readonly Dictionary<Vector2Int, GameObject> tiles = new();

	[SerializeField] ComputeShader displacementComputeShader;
	[SerializeField] ComputeShader frequencyDomainFieldComputeShader;
	[SerializeField] ComputeShader fftComputeShader;
	[HideInInspector] public RenderTexture displacement;
	public RenderTexture DispFreqX { get; private set; }
	public RenderTexture DispFreqY { get; private set; }
	public RenderTexture DispFreqZ { get; private set; }
	public RenderTexture DispSpatialX { get; private set; }
	public RenderTexture DispSpatialY { get; private set; }
	public RenderTexture DispSpatialZ { get; private set; }
	public RenderTexture NormFreqY { get; private set; }
	public RenderTexture NormSpatialY { get; private set; }
	public RenderTexture ApproximateNormals { get; private set; }
	public RenderTexture PingBuffer { get; private set; }
	public RenderTexture PongBuffer { get; private set; }
	public Texture2D NoiseTexture { get; private set; }

	[Min(0)] public float F = 1400000;
	public Vector2 U10 = new(20, 0);
	public Transform windArrow;
	[Min(0)] public float gamma = 3.3f;

	[Header("Phillips Spectrum")] [Min(0)] public float phillipsA = 1;
	[Min(0)] public float phillipsSmallLength = 0.1f;
	public Vector2 phillipsWindDir = Vector2.right;

	[Header("Bleh")] [Min(0)] public float timeScale = 1;
	public float timeOffset = 1000;
	public float heightTest = 0;
	public Vector2 test2d = new(128, 128);
	public Vector2 test2d2 = new(0, 0);
	public Vector2 test2d3 = new(0, 0.5f);
	public Vector2 test2d4 = new(0, 1);
	public Vector2 test2d5 = new(0, 0);
	public float dtest1 = 0.05f;
	public float dtest2 = 0.2f;
	public float dtest3 = 0.05f;
	public Vector3 normalTestX = Vector3.one;
	public Vector3 normalTestZ = Vector3.one;
	public float normalTest2;
	public Vector3 normalTest3 = Vector3.zero;

	[SerializeField] Vector2 waveVector = Vector2.one;
	[SerializeField] float amplitude = 1;
	[SerializeField] float angularFrequency;

	// [Header("SimpleSinusoid")]
	// [SerializeField] ComputeShader simpleSinusoid;
	// public Vector3 simpleSinusoidAmplitude = Vector3.one;
	// public Vector2 simpleSinusoidFrequency = Vector2.one;
	// public Vector2 simpleSinusoidAngularFrequency = Vector2.one;
	// public Vector2 simpleSinusoidPhase = Vector2.zero;

	[Header("Texture Debug")] [SerializeField]
	Transform textureDebugParent;

	[SerializeField] GameObject textureDebugPrefab;
	[SerializeField] Vector2 textureDebugOffset = new(3, 3);

	void Awake() {
		InitTextures();

		frequencyDomainFieldComputeShader.SetTexture(0, "Noise", NoiseTexture);
		frequencyDomainFieldComputeShader.SetFloat("Resolution", tileSideVertexCount);
		frequencyDomainFieldComputeShader.SetFloat("PI", Mathf.PI);
		frequencyDomainFieldComputeShader.SetFloat("g", -Physics.gravity.y);
		frequencyDomainFieldComputeShader.SetTexture(0, DispFreqXid, DispFreqX);
		frequencyDomainFieldComputeShader.SetTexture(0, DispFreqYid, DispFreqY);
		frequencyDomainFieldComputeShader.SetTexture(0, DispFreqZid, DispFreqZ);
		frequencyDomainFieldComputeShader.SetTexture(0, NormFreqYid, NormFreqY);

		fftComputeShader.SetBool(FFTForward, false);

		displacementComputeShader.SetTexture(0, DisplacementID, displacement);
		displacementComputeShader.SetTexture(0, ApproximateNormalsID, ApproximateNormals);
		displacementComputeShader.SetTexture(0, DispFreqXid, DispFreqX);
		displacementComputeShader.SetTexture(0, DispFreqYid, DispFreqY);
		displacementComputeShader.SetTexture(0, DispFreqZid, DispFreqZ);
		displacementComputeShader.SetTexture(0, DispSpatialXid, DispSpatialX);
		displacementComputeShader.SetTexture(0, DispSpatialYid, DispSpatialY);
		displacementComputeShader.SetTexture(0, DispSpatialZid, DispSpatialZ);
		displacementComputeShader.SetFloat("Resolution", tileSideVertexCount);
		displacementComputeShader.SetFloat("PI", Mathf.PI);
		displacementComputeShader.SetFloat("g", -Physics.gravity.y);
		SetupComputeShader();

		AddTextureDebugs();

		SpawnTiles();
	}

	void SetupComputeShader() {
		frequencyDomainFieldComputeShader.SetFloats("test", test2d.x, test2d.y);
		frequencyDomainFieldComputeShader.SetFloats("test2", test2d2.x, test2d2.y);
		frequencyDomainFieldComputeShader.SetFloats("test3", test2d3.x, test2d3.y);
		frequencyDomainFieldComputeShader.SetFloats("test4", test2d4.x, test2d4.y);
		frequencyDomainFieldComputeShader.SetFloats("test5", test2d5.x, test2d5.y);
		frequencyDomainFieldComputeShader.SetFloat("time", Time.time * timeScale + timeOffset);
		frequencyDomainFieldComputeShader.SetFloat("L", tileSize);
		frequencyDomainFieldComputeShader.SetFloat("F", F);
		frequencyDomainFieldComputeShader.SetVector("U10", windArrow != null ? new Vector2(U10.x * windArrow.localPosition.x, U10.y * windArrow.localPosition.z) : U10);
		frequencyDomainFieldComputeShader.SetFloat("gamma", gamma);
		frequencyDomainFieldComputeShader.SetFloat("heightTest", heightTest);
		frequencyDomainFieldComputeShader.SetFloat("phillipsA", phillipsA);
		frequencyDomainFieldComputeShader.SetFloat("phillipsSmallLength", phillipsSmallLength);
		frequencyDomainFieldComputeShader.SetVector("phillipsWindDir", phillipsWindDir.normalized);
		frequencyDomainFieldComputeShader.Dispatch(0, tileSideVertexCount / 8, tileSideVertexCount / 8, 1);

		RunIFFT(DispFreqY, PingBuffer, PongBuffer, DispSpatialY);
		//TODO Merge dispFreqX and dispFreqZ in the same texture
		RunIFFT(DispFreqX, PingBuffer, PongBuffer, DispSpatialX);
		RunIFFT(DispFreqZ, PingBuffer, PongBuffer, DispSpatialZ);
		RunIFFT(NormFreqY, PingBuffer, PongBuffer, NormSpatialY);

		// SetupSimpleSinusoid();

		displacementComputeShader.SetFloat("dtest1", dtest1);
		displacementComputeShader.SetFloat("dtest2", dtest2);
		displacementComputeShader.SetFloat("dtest3", dtest3);
		displacementComputeShader.SetVector("normalTestX", normalTestX);
		displacementComputeShader.SetVector("normalTestZ", normalTestZ);
		displacementComputeShader.SetFloat("normalTest2", normalTest2);
		displacementComputeShader.SetVector("normalTest3", normalTest3);
		displacementComputeShader.SetVector("waveVector", waveVector);
		displacementComputeShader.SetFloat("amplitude", amplitude);
		displacementComputeShader.SetFloat("angularFrequency", angularFrequency);
		displacementComputeShader.SetFloat("time", Time.time * timeScale + timeOffset);
		displacementComputeShader.SetFloat("L", tileSize);
		displacementComputeShader.Dispatch(0, tileSideVertexCount / 8, tileSideVertexCount / 8, 1);

		// simpleSinusoid.SetTexture(0, "dispSpatialX", dispSpatialX);
		// simpleSinusoid.SetTexture(0, "dispSpatialY", dispSpatialY);
		// simpleSinusoid.SetTexture(0, "dispSpatialZ", dispSpatialZ);
	}

	void Update() => SetupComputeShader();

	void SpawnTiles() {
		var mesh = CreateMesh();

		var materialInstance = new Material(tileMaterial);
		materialInstance.SetTexture(TileMaterialDisplacement, displacement);
		materialInstance.SetTexture(TileMaterialNormalMap, NormSpatialY);
		materialInstance.SetTexture(TileMaterialApproximateNormalMap, ApproximateNormals);
		materialInstance.SetVector(TileMaterialResolution, new Vector2(tileSize, tileSize));

		for (int x = -tileRadius; x <= tileRadius; ++x)
		for (int z = -tileRadius; z <= tileRadius; ++z) {
			//I tried creating the GameObject and set its component before the loop and instantiating it as a prefab, but the prefab is still added to the scene on construction, so it's simpler like this
			var oceanTile = new GameObject($"OceanTile {x} {z}");
			oceanTile.AddComponent<MeshFilter>().mesh = mesh;
			oceanTile.AddComponent<MeshRenderer>().material = materialInstance;

			oceanTile.transform.SetParent(transform);
			oceanTile.transform.position = new Vector3(x, 0, z) * tileSize;
			// tiles.Add(new Vector2Int(x, z), oceanTile);
		}
	}

	Vector2Int ConvertVector(Vector3 vector) => new(
		Mathf.FloorToInt(vector.x / tileSize),
		Mathf.FloorToInt(vector.z / tileSize)
	);

	void InitTextures() {
		NoiseTexture = CreateTexture();
		InitializingNoise();
		//TODO Create every textures in a block
		DispFreqX = CreateRenderTexture();
		DispFreqY = CreateRenderTexture();
		DispFreqZ = CreateRenderTexture();
		DispSpatialX = CreateRenderTexture();
		DispSpatialY = CreateRenderTexture();
		DispSpatialZ = CreateRenderTexture();
		NormFreqY = CreateRenderTexture();
		NormSpatialY = CreateRenderTexture();
		ApproximateNormals = CreateRenderTexture();
		PingBuffer = CreateRenderTexture();
		PongBuffer = CreateRenderTexture();
		displacement = CreateRenderTexture();
	}

	void InitializingNoise() {
		NoiseTexture = CreateTexture();

		//https://stackoverflow.com/a/218600/2692695
		// int greenNoise = Random.Range(0, 10000);
		const float mean = 0;
		const float stdDev = 1;
		for (int x = 0; x < tileSideVertexCount; ++x)
		for (int y = 0; y < tileSideVertexCount; ++y) {
			// noiseTexture.SetPixel(x, y, new Color(
			// 	Mathf.PerlinNoise(x / noiseResolution, y / noiseResolution),
			// 	Mathf.PerlinNoise(x / noiseResolution + greenNoise, y / noiseResolution + greenNoise),
			// 	0,
			// 	1
			// ));
			// noiseTexture.SetPixel(x, y, new Color(.5f, .5f, 0, 0));

			var u1 = Vector2.one - new Vector2(Random.value, Random.value);
			var u2 = Vector2.one - new Vector2(Random.value, Random.value);
			var randStdNormal = new Vector2(
				Mathf.Sqrt(-2f * Mathf.Log(u1.x)) * Mathf.Sin(2f * Mathf.PI * u2.x),
				Mathf.Sqrt(-2f * Mathf.Log(u1.y)) * Mathf.Sin(2f * Mathf.PI * u2.y)
			);
			var randNormal = mean * Vector2.one + stdDev * randStdNormal;
			NoiseTexture.SetPixel(x, y, new Color(randNormal.x, randNormal.y, 0, 1));
		}

		NoiseTexture.Apply();
	}

	// void SetupSimpleSinusoid() {
	// 	simpleSinusoid.SetFloat("time", Time.time * timeScale + timeOffset);
	// 	simpleSinusoid.SetFloat("resolution", xSize);
	// 	simpleSinusoid.SetVector("frequency", simpleSinusoidFrequency);
	// 	simpleSinusoid.SetVector("angularFrequency", simpleSinusoidAngularFrequency);
	// 	simpleSinusoid.SetVector("amplitude", simpleSinusoidAmplitude);
	// 	simpleSinusoid.SetVector("phase", simpleSinusoidPhase);
	// 	simpleSinusoid.Dispatch(0, tileSideVertexCount / 8, tileSideVertexCount / 8, 1);
	// }

	RenderTexture CreateRenderTexture() {
		var rt = new RenderTexture(tileSideVertexCount, tileSideVertexCount, 32) {
			enableRandomWrite = true,
			filterMode = FilterMode.Point,
			graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
			autoGenerateMips = false,
		};
		rt.Create();
		return rt;
	}

	Texture2D CreateTexture() => new(tileSideVertexCount, tileSideVertexCount) {
		filterMode = FilterMode.Point,
	};

	void OnDrawGizmos() {
		if (windArrow != null) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(windArrow.parent.position, windArrow.position);
		}
	}

	void RunIFFT(
		RenderTexture initInput,
		RenderTexture initPing,
		RenderTexture initPong,
		RenderTexture initOutput
	) {
		int i;
		RenderTexture ping;
		RenderTexture pong;

		const bool splitNormalization = true;

		// Swap to avoid collisions with the input:
		ping = initPing;
		if (initInput == initPong) {
			ping = initPong;
		}

		pong = ping == initPing ? initPong : initPing;

		int xIterations = Mathf.RoundToInt(Mathf.Log(tileSideVertexCount) / Mathf.Log(2));
		int iterations = 2 * xIterations;

		// Swap to avoid collisions with output:
		if (initOutput == ((iterations % 2 == 0) ? pong : ping))
			(ping, pong) = (pong, ping);

		// If we've avoiding collision with output creates an input collision,
		// then you'll just have to rework your framebuffers and try again.
		if (initInput == pong)
			throw new System.Exception("not enough framebuffers to compute without copying data. You may perform the computation with only two framebuffers, but the output must equal the input when an even number of iterations are required.");

		for (i = 0; i < iterations; ++i) {
			var input = ping;
			var output = pong;

			float normalization;
			if (i == 0) {
				input = initInput;
				normalization = 1f / tileSideVertexCount;
			}else {
				if (i == iterations - 1) {
					output = initOutput;
					normalization = tileSideVertexCount;
				}
				else
					normalization = 1f;
			}

			bool horizontal = i < xIterations;

			fftComputeShader.SetTexture(0, FFTSrc, input);
			fftComputeShader.SetTexture(0, FFTOutput, output);
			fftComputeShader.SetBool(FFTHorizontal, horizontal);
			fftComputeShader.SetFloats(FFTOneOverResolution, 1f / tileSideVertexCount, 1f / tileSideVertexCount);
			fftComputeShader.SetFloat(FFTNormalization, normalization);
			fftComputeShader.SetFloat(FFTSubtransformSize, Mathf.Pow(2, (horizontal ? i : (i - xIterations)) + 1));
			fftComputeShader.Dispatch(0, tileSideVertexCount / 8, tileSideVertexCount / 8, 1);

			(ping, pong) = (pong, ping);
		}
	}

	Mesh CreateMesh() {
		var vertices = new Vector3[tileSideVertexCount * tileSideVertexCount];
		int[] triangles = new int[tileSideVertexCount * tileSideVertexCount * 6];

		for (int i = 0, z = 0; z < tileSideVertexCount; ++z)
		for (int x = 0; x < tileSideVertexCount; ++x) {
			vertices[i] = new Vector3(x, 0f, z) * tileSize / (tileSideVertexCount - 1);
			++i;
		}

		int vert = 0;
		int tris = 0;
		for (int z = 0; z < tileSideVertexCount - 1; ++z) {
			for (int x = 0; x < tileSideVertexCount - 1; ++x) {
				triangles[tris + 0] = vert + 0;
				triangles[tris + 1] = vert + tileSideVertexCount;
				triangles[tris + 2] = vert + 1;
				triangles[tris + 3] = vert + 1;
				triangles[tris + 4] = vert + tileSideVertexCount;
				triangles[tris + 5] = vert + tileSideVertexCount + 1;

				++vert;
				tris += 6;
			}

			++vert;
		}

		var mesh = new Mesh {
			indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
			//Not sure why this doesn't work
			// vertices = vertices,
			// triangles = triangles,
		};
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		return mesh;
	}

	void AddTextureDebugs() {
		if (!textureDebugParent)
			return;

		AddTextureDebug(displacement, new Vector2Int(0, 0), "Displacement");
		AddTextureDebug(NoiseTexture, new Vector2Int(0, -1), "Noise");
		AddTextureDebug(DispFreqY, new Vector2Int(1, 0), "dispFreqY");
		AddTextureDebug(DispFreqX, new Vector2Int(1, -1), "dispFreqX");
		AddTextureDebug(DispFreqZ, new Vector2Int(1, -2), "dispFreqZ");
		AddTextureDebug(DispSpatialY, new Vector2Int(2, 0), "dispSpatialY");
		AddTextureDebug(DispSpatialX, new Vector2Int(2, -1), "dispSpatialX");
		AddTextureDebug(DispSpatialZ, new Vector2Int(2, -2), "dispSpatialZ");
		AddTextureDebug(NormFreqY, new Vector2Int(3, 0), "normFreqY");
		AddTextureDebug(NormSpatialY, new Vector2Int(3, -1), "normSpatialY");
	}

	void AddTextureDebug(Texture texture, Vector2Int position, string textureName) {
		var renderDebug = Instantiate(textureDebugPrefab, textureDebugParent.transform, true);
		renderDebug.transform.localPosition = new Vector3(
			textureDebugOffset.x * position.x,
			0f,
			textureDebugOffset.y * position.y
		);
		renderDebug.GetComponent<Renderer>().material.mainTexture = texture;
		renderDebug.name = textureName;
	}

	static readonly int FFTOneOverResolution = Shader.PropertyToID("oneOverResolution");
	static readonly int FFTSrc = Shader.PropertyToID("src");
	static readonly int FFTOutput = Shader.PropertyToID("output");
	static readonly int FFTSubtransformSize = Shader.PropertyToID("subtransformSize");
	static readonly int FFTHorizontal = Shader.PropertyToID("horizontal");
	static readonly int FFTForward = Shader.PropertyToID("forward");
	static readonly int FFTNormalization = Shader.PropertyToID("normalization");

	static readonly int TileMaterialDisplacement = Shader.PropertyToID("_Displacement");
	static readonly int TileMaterialNormalMap = Shader.PropertyToID("_NormalMap");
	static readonly int TileMaterialApproximateNormalMap = Shader.PropertyToID("_ApproximateNormalMap");
	static readonly int TileMaterialResolution = Shader.PropertyToID("_Resolution");

	static readonly int DisplacementID = Shader.PropertyToID("Displacement");
	static readonly int ApproximateNormalsID = Shader.PropertyToID("ApproximateNormals");
	static readonly int DispFreqXid = Shader.PropertyToID("DispFreqX");
	static readonly int DispFreqYid = Shader.PropertyToID("DispFreqY");
	static readonly int DispFreqZid = Shader.PropertyToID("DispFreqZ");
	static readonly int DispSpatialXid = Shader.PropertyToID("DispSpatialX");
	static readonly int DispSpatialYid = Shader.PropertyToID("DispSpatialY");
	static readonly int DispSpatialZid = Shader.PropertyToID("DispSpatialZ");
	static readonly int NormFreqYid = Shader.PropertyToID("NormFreqY");
}