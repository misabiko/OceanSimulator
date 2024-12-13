using System.Collections.Generic;
using UnityEngine;

public class Ocean : MonoBehaviour {
	//TODO Replace prefab with creating and populating component here
	public GameObject oceanTilePrefab;
	[Min(0)] public int tileRadius;
	[Min(0)] public int tileSideVertexCount = 256;
	[Min(0)] public float tileSize = 64;

	readonly Dictionary<Vector2Int, OceanMeshGenerator> tiles = new();

	[SerializeField] ComputeShader displacementComputeShaderSource;
	[SerializeField] ComputeShader spectrumComputeShaderSource;
	[SerializeField] ComputeShader rreusserFFTSource;
	ComputeShader displacementComputeShader;
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
	[HideInInspector] public Texture2D noiseTexture;

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

	void Awake() {
		//For now, limiting to 512
		Debug.Assert(tileSideVertexCount <= 512);

		displacementComputeShader = Instantiate(displacementComputeShaderSource);
		spectrumComputeShader = Instantiate(spectrumComputeShaderSource);
		rreusserFFT = Instantiate(rreusserFFTSource);
		InitTextures();

		spectrumComputeShader.SetTexture(0, "Noise", noiseTexture);
		spectrumComputeShader.SetFloat("Resolution", tileSideVertexCount);
		spectrumComputeShader.SetFloat("PI", Mathf.PI);
		spectrumComputeShader.SetFloat("g", -Physics.gravity.y);
		spectrumComputeShader.SetTexture(0, "HX", HX);
		spectrumComputeShader.SetTexture(0, "HY", HY);
		spectrumComputeShader.SetTexture(0, "HZ", HZ);
		spectrumComputeShader.SetTexture(0, "NY", NY);

		displacementComputeShader.SetTexture(0, "Displacement", displacement);
		displacementComputeShader.SetTexture(0, "HX", HX);
		displacementComputeShader.SetTexture(0, "HY", HY);
		displacementComputeShader.SetTexture(0, "HZ", HZ);
		displacementComputeShader.SetTexture(0, "HX2", HX2);
		displacementComputeShader.SetTexture(0, "HY2", HY2);
		displacementComputeShader.SetTexture(0, "HZ2", HZ2);
		displacementComputeShader.SetTexture(0, "approximateNormals", approximateNormals);
		displacementComputeShader.SetFloat("Resolution", tileSideVertexCount);
		displacementComputeShader.SetFloat("PI", Mathf.PI);
		displacementComputeShader.SetFloat("g", -Physics.gravity.y);
		SetupComputeShader();

		var renderTextureDisplay = GameObject.Find("RenderTextureDisplay");
		if (renderTextureDisplay) {
			renderTextureDisplay.GetComponent<Renderer>().material.mainTexture = displacement;
			GameObject.Find("RenderTextureNoise").GetComponent<Renderer>().material.mainTexture = noiseTexture;
			GameObject.Find("RenderTextureHY").GetComponent<Renderer>().material.mainTexture = HY;
			GameObject.Find("RenderTextureHX").GetComponent<Renderer>().material.mainTexture = HX;
			GameObject.Find("RenderTextureHZ").GetComponent<Renderer>().material.mainTexture = HZ;
			GameObject.Find("RenderTextureHY2").GetComponent<Renderer>().material.mainTexture = HY2;
			GameObject.Find("RenderTextureHX2").GetComponent<Renderer>().material.mainTexture = HX2;
			GameObject.Find("RenderTextureHZ2").GetComponent<Renderer>().material.mainTexture = HZ2;
			GameObject.Find("RenderTexturePing").GetComponent<Renderer>().material.mainTexture = pingBuffer;
			GameObject.Find("RenderTexturePong").GetComponent<Renderer>().material.mainTexture = pongBuffer;
		}

		// var uiDocument = GetComponent<UIDocument>();
		// if (uiDocument.enabled) {
		// 	uiDocument.rootVisualElement.Add(new Image { image = displacement });
		// 	// uiDocument.rootVisualElement.Add(new Image { image = noiseTexture });
		// 	// uiDocument.rootVisualElement.Add(new Image { image = waveNumberTexture });
		// 	uiDocument.rootVisualElement.Add(new Image { image = HX });
		// 	uiDocument.rootVisualElement.Add(new Image { image = HY });
		// 	uiDocument.rootVisualElement.Add(new Image { image = HZ });
		// 	uiDocument.rootVisualElement.Add(new Image { image = HX2 });
		// 	uiDocument.rootVisualElement.Add(new Image { image = HY2 });
		// 	uiDocument.rootVisualElement.Add(new Image { image = HZ2 });
		// 	uiDocument.rootVisualElement.Add(new Image { image = NY });
		// 	uiDocument.rootVisualElement.Add(new Image { image = NY2 });
		// 	uiDocument.rootVisualElement.Add(new Image { image = approximateNormals });
		// 	uiDocument.rootVisualElement.Add(new Image { image = pingBuffer });
		// 	uiDocument.rootVisualElement.Add(new Image { image = pongBuffer });
		// }

		SpawnTiles();
	}

	void SetupComputeShader() {
		spectrumComputeShader.SetFloats("test", test2d.x, test2d.y);
		spectrumComputeShader.SetFloats("test2", test2d2.x, test2d2.y);
		spectrumComputeShader.SetFloats("test3", test2d3.x, test2d3.y);
		spectrumComputeShader.SetFloats("test4", test2d4.x, test2d4.y);
		spectrumComputeShader.SetFloats("test5", test2d5.x, test2d5.y);
		spectrumComputeShader.SetFloat("time", Time.time * timeScale + timeOffset);
		spectrumComputeShader.SetFloat("L", tileSize);
		spectrumComputeShader.SetFloat("F", F);
		spectrumComputeShader.SetVector("U10", windArrow != null ? new Vector2(U10.x * windArrow.localPosition.x, U10.y * windArrow.localPosition.z) : U10);
		spectrumComputeShader.SetFloat("gamma", gamma);
		spectrumComputeShader.SetFloat("heightTest", heightTest);
		spectrumComputeShader.SetFloat("phillipsA", phillipsA);
		spectrumComputeShader.SetFloat("phillipsSmallLength", phillipsSmallLength);
		spectrumComputeShader.SetVector("phillipsWindDir", phillipsWindDir.normalized);
		spectrumComputeShader.Dispatch(0, tileSideVertexCount / 8, tileSideVertexCount / 8, 1);

		RunFFT(HY, pingBuffer, pongBuffer, HY2);
		//TODO Merge HX and HZ in the same texture
		RunFFT(HX, HX2, pingBuffer,  HX2);
		RunFFT(HZ, HZ2, pingBuffer,  HZ2);
		RunFFT(NY, NY2, pingBuffer,  NY2);

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

		// simpleSinusoid.SetTexture(0, "HX2", HX2);
		// simpleSinusoid.SetTexture(0, "HY2", HY2);
		// simpleSinusoid.SetTexture(0, "HZ2", HZ2);
	}

	void Update() {
		// foreach (var tile in tiles.Values) {
		// 	var material = tile.GetComponent<Renderer>().material;
		// 	material.SetFloat("_Height", height);
		// 	material.SetVector("_Resolution", new Vector2(size, size));
		// }

		SetupComputeShader();
	}

	void SpawnTiles() {
		var prefabComponent = oceanTilePrefab.GetComponent<OceanMeshGenerator>();
		prefabComponent.sideVertexCount = tileSideVertexCount;
		prefabComponent.size = tileSize;

		for (int x = -tileRadius; x <= tileRadius; ++x)
		for (int z = -tileRadius; z <= tileRadius; ++z) {
			var oceanTile = Instantiate(oceanTilePrefab, new Vector3(x, 0, z) * tileSize, Quaternion.identity, transform)
				.GetComponent<OceanMeshGenerator>();
			var material = oceanTile.GetComponent<Renderer>().material;
			material.SetTexture("_Displacement", displacement);
			material.SetTexture("_NormalMap", NY2);
			material.SetTexture("_ApproximateNormalMap", approximateNormals);
			material.SetVector("_Resolution", new Vector2(tileSize, tileSize));
			tiles.Add(new Vector2Int(x, z), oceanTile);
		}
	}

	public OceanMeshGenerator GetOceanMeshGenerator(Vector3 shipPosition) => tiles[ConvertVector(shipPosition)];

	Vector2Int ConvertVector(Vector3 vector) => new(
		Mathf.FloorToInt(vector.x / tileSize),
		Mathf.FloorToInt(vector.z / tileSize)
	);

	void InitTextures() {
		noiseTexture = CreateTexture();
		InitializingNoise();
		//TODO Create every textures in a block
		HX = CreateRenderTexture();
		HY = CreateRenderTexture();
		HZ = CreateRenderTexture();
		HX2 = CreateRenderTexture();
		HY2 = CreateRenderTexture();
		HZ2 = CreateRenderTexture();
		NY = CreateRenderTexture();
		NY2 = CreateRenderTexture();
		approximateNormals = CreateRenderTexture();
		pingBuffer = CreateRenderTexture();
		pongBuffer = CreateRenderTexture();
		displacement = CreateRenderTexture();
	}

	void InitializingNoise() {
		noiseTexture = CreateTexture();

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
			noiseTexture.SetPixel(x, y, new Color(randNormal.x, randNormal.y, 0, 1));
		}

		noiseTexture.Apply();
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

	class Uniforms {
		public RenderTexture input;
		public string inputName;
		public RenderTexture output;
		public string outputName;
		public bool horizontal;
		public bool forward;
		public Vector2 resolution;
		public float normalization;
		public float subtransformSize;

		public void SetUniforms(ComputeShader shader) {
			shader.SetTexture(0, FFTSrc, input);
			if (output != null)
				shader.SetTexture(0, FFTOutput, output);
			shader.SetBool(FFTHorizontal, horizontal);
			shader.SetBool(FFTForward, forward);
			shader.SetFloats(FFTOneOverResolution, resolution.x, resolution.y);
			shader.SetFloat(FFTNormalization, normalization);
			shader.SetFloat(FFTSubtransformSize, subtransformSize);
		}

		public Uniforms Copy() {
			return new Uniforms {
				input = input,
				inputName = inputName,
				output = output,
				outputName = outputName,
				horizontal = horizontal,
				forward = forward,
				resolution = resolution,
				normalization = normalization,
				subtransformSize = subtransformSize,
			};
		}

		public override string ToString() {
			return $"input: {inputName}, output: {outputName}, horizontal: {horizontal}, forward: {forward}, resolution: {resolution}, normalization: {normalization}, subtransformSize: {subtransformSize}";
		}
	}

	void RunFFT(
		RenderTexture optsInput,
		RenderTexture optsPing,
		RenderTexture optsPong,
		RenderTexture optsOutput
	) {
		int i;
		RenderTexture ping;
		RenderTexture pong;
		Uniforms uniforms = new();
		RenderTexture tmp;
		int width = tileSideVertexCount;
		int height = tileSideVertexCount;

		const bool forward = false;
		const bool splitNormalization = true;

		void Swap() {
			tmp = ping;
			ping = pong;
			pong = tmp;
		}

		// Swap to avoid collisions with the input:
		ping = optsPing;
		if (optsInput == optsPong) {
			ping = optsPong;
		}
		pong = ping == optsPing ? optsPong : optsPing;

		int xIterations = Mathf.RoundToInt(Mathf.Log(width) / Mathf.Log(2));
		int yIterations = Mathf.RoundToInt(Mathf.Log(height) / Mathf.Log(2));
		int iterations = xIterations + yIterations;

		// Swap to avoid collisions with output:
		if (optsOutput == ((iterations % 2 == 0) ? pong : ping))
			Swap();

		// If we've avoiding collision with output creates an input collision,
		// then you'll just have to rework your framebuffers and try again.
		if (optsInput == pong)
			throw new System.Exception("not enough framebuffers to compute without copying data. You may perform the computation with only two framebuffers, but the output must equal the input when an even number of iterations are required.");

		for (i = 0; i < iterations; ++i) {
			uniforms.input = ping;
			uniforms.output = pong;
			uniforms.horizontal = i < xIterations;
			uniforms.forward = forward;
			uniforms.resolution = new Vector2(1f / width, 1f / height);

			if (i == 0) {
				uniforms.input = optsInput;
			}else if (i == iterations - 1) {
				uniforms.output = optsOutput;
			}

			if (i == 0) {
				if (splitNormalization)
					uniforms.normalization = 1f / Mathf.Sqrt(width * height);
				else if (!forward)
					uniforms.normalization = 1f / width / height;
				else
					uniforms.normalization = 1f;
			}
			else
				uniforms.normalization = 1f;

			uniforms.subtransformSize = Mathf.Pow(2, (uniforms.horizontal ? i : (i - xIterations)) + 1);

			uniforms.SetUniforms(rreusserFFT);
			rreusserFFT.Dispatch(0, width / 8, height / 8, 1);

			Swap();
		}
	}

	static readonly int FFTOneOverResolution = Shader.PropertyToID("oneOverResolution");
	static readonly int FFTSrc = Shader.PropertyToID("src");
	static readonly int FFTOutput = Shader.PropertyToID("output");
	static readonly int FFTSubtransformSize = Shader.PropertyToID("subtransformSize");
	static readonly int FFTHorizontal = Shader.PropertyToID("horizontal");
	static readonly int FFTForward = Shader.PropertyToID("forward");
	static readonly int FFTNormalization = Shader.PropertyToID("normalization");
}