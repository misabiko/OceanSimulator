using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanMeshGenerator : MonoBehaviour {
	Mesh mesh;

	Vector3[] vertices;
	int[] triangles;

	[HideInInspector] public int sideVertexCount = 256;
	[HideInInspector] public float size = 100f;

	Material material;

	void Start() {
		mesh = new Mesh {
			indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
		};
		GetComponent<MeshFilter>().mesh = mesh;
		material = GetComponent<Renderer>().material;

		CreateShape();
		UpdateMesh();
	}

	float? lastSize;

	void Update() {
		if (vertices.Length != (sideVertexCount + 1) * (sideVertexCount + 1) || lastSize != size) {
			CreateShape();
			UpdateMesh();
		}
	}

	void CreateShape() {
		vertices = new Vector3[sideVertexCount * sideVertexCount];

		for (int i = 0, z = 0; z < sideVertexCount; ++z)
		for (int x = 0; x < sideVertexCount; ++x) {
			vertices[i] = new Vector3(x, 0f, z) * size / (sideVertexCount - 1);
			++i;
		}

		triangles = new int[sideVertexCount * sideVertexCount * 6];

		int vert = 0;
		int tris = 0;
		for (int z = 0; z < sideVertexCount - 1; ++z) {
			for (int x = 0; x < sideVertexCount - 1; ++x) {
				triangles[tris + 0] = vert + 0;
				triangles[tris + 1] = vert + sideVertexCount;
				triangles[tris + 2] = vert + 1;
				triangles[tris + 3] = vert + 1;
				triangles[tris + 4] = vert + sideVertexCount;
				triangles[tris + 5] = vert + sideVertexCount + 1;

				++vert;
				tris += 6;
			}

			++vert;
		}

		lastSize = size;
	}

	void UpdateMesh() {
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
	}
}