using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanMeshGenerator : MonoBehaviour {
	Mesh mesh;

	Vector3[] vertices;
	int[] triangles;

	public int xSize = 20;
	public int zSize = 20;

	[SerializeField] ComputeShader computeShader;
	RenderTexture displacement;

	void Start() {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		CreateShape();
		UpdateMesh();

		displacement = new RenderTexture(256, 256, 24);
		displacement.enableRandomWrite = true;
		displacement.Create();
		computeShader.SetTexture(0, "Result", displacement);
		computeShader.Dispatch(0, displacement.width / 8, displacement.height / 8, 1);

		var material = GetComponent<Renderer>().material;
		material.SetTexture("_Displacement", displacement);

		//TODO Try rendering texture to UI?
		GameObject.Find("RenderTextureDisplay").GetComponent<Renderer>().material.mainTexture = displacement;
	}

	// void Update() {
	// 	if (vertices.Length != (xSize + 1) * (zSize + 1)) {
	// 		CreateShape();
	// 		UpdateMesh();
	// 	}
	// }

	void CreateShape() {
		vertices = new Vector3[(xSize + 1) * (zSize + 1)];

		for (int i = 0, z = 0; z <= zSize; ++z)
			for (int x = 0; x <= xSize; ++x) {
				vertices[i] = new Vector3(x, 0f, z);
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
	}

	void UpdateMesh() {
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateNormals();
	}
}