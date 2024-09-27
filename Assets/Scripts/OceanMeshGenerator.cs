using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanMeshGenerator : MonoBehaviour {
	Mesh mesh;

	Vector3[] vertices;
	int[] triangles;

	[SerializeField] int xSize = 20;
	[SerializeField] int zSize = 20;
	public float size = 1f;

	[SerializeField] ComputeShader computeShader;
	RenderTexture displacement;

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

		displacement = new RenderTexture(xSize, zSize, 24);
		displacement.enableRandomWrite = true;
		displacement.Create();
		computeShader.SetTexture(0, "Result", displacement);
		computeShader.SetFloat("Resolution", displacement.width);
		SetupComputeShader();

		material.SetTexture("_Displacement", displacement);

		//TODO Try rendering texture to UI?
		GameObject.Find("RenderTextureDisplay").GetComponent<Renderer>().material.mainTexture = displacement;
	}

	void SetupComputeShader() {
		computeShader.SetVector("waveVector", waveVector);
		computeShader.SetFloat("amplitude", amplitude);
		computeShader.SetFloat("angularFrequency", angularFrequency);
		computeShader.SetFloat("time", Time.time);
		computeShader.Dispatch(0, displacement.width / 10, displacement.height / 10, 1);
	}

	void Update() {
		if (vertices.Length != (xSize + 1) * (zSize + 1)) {
			CreateShape();
			UpdateMesh();
		}

		material.SetFloat("_Height", height);
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

		material.SetVector("_Resolution", new Vector2(xSize, zSize));
	}

	void UpdateMesh() {
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateNormals();
	}
}