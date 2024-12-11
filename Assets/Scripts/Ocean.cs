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
	[HideInInspector] public RenderTexture waveNumberTexture;
	[HideInInspector] public Texture2D noiseTexture;

	[Min(0)] public float noiseResolution = 10f;

	[Min(0)] public float F = 1400000;
	[Min(0)] public float U10 = 20;
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
	// [SerializeField] float height = .02f;

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

		// var renderTextureDisplay = GameObject.Find("RenderTextureDisplay");
		// if (renderTextureDisplay) {
		// 	renderTextureDisplay.GetComponent<Renderer>().material.mainTexture = displacement;
		// 	GameObject.Find("RenderTextureNoise").GetComponent<Renderer>().material.mainTexture = noiseTexture;
		// 	GameObject.Find("RenderTextureWaveNumber").GetComponent<Renderer>().material.mainTexture = waveNumberTexture;
		// 	GameObject.Find("RenderTextureHY").GetComponent<Renderer>().material.mainTexture = HY;
		// 	GameObject.Find("RenderTextureHX").GetComponent<Renderer>().material.mainTexture = HX;
		// 	GameObject.Find("RenderTextureHZ").GetComponent<Renderer>().material.mainTexture = HZ;
		// 	GameObject.Find("RenderTextureHY2").GetComponent<Renderer>().material.mainTexture = HY2;
		// 	GameObject.Find("RenderTextureHX2").GetComponent<Renderer>().material.mainTexture = HX2;
		// 	GameObject.Find("RenderTextureHZ2").GetComponent<Renderer>().material.mainTexture = HZ2;
		// }

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
		displacementComputeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);

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
			tiles.Add(new Vector2Int(x, z), oceanTile);
		}
	}

	public OceanMeshGenerator GetOceanMeshGenerator(Vector3 shipPosition) => tiles[ConvertVector(shipPosition)];

	Vector2Int ConvertVector(Vector3 vector) => new(
		Mathf.FloorToInt(vector.x / tileSize),
		Mathf.FloorToInt(vector.z / tileSize)
	);

	void InitTextures() {
		waveNumberTexture = CreateRenderTexture();
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

		waveNumberTexture = CreateRenderTexture();
		spectrumComputeShader.SetTexture(0, "WaveNumber", waveNumberTexture);
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
	// 	simpleSinusoid.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);
	// }

	RenderTexture CreateRenderTexture() {
		var rt = new RenderTexture(tileSideVertexCount, tileSideVertexCount, 24) {
			enableRandomWrite = true,
			filterMode = FilterMode.Point,
			graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
		};
		rt.Create();
		return rt;
	}

	Texture2D CreateTexture() => new(tileSideVertexCount, tileSideVertexCount) {
		filterMode = FilterMode.Point,
	};
}