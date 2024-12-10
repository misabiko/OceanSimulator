using System.Collections.Generic;
using UnityEngine;

public class Ocean : MonoBehaviour {
	//TODO Replace prefab with creating and populating component here
	public GameObject oceanTilePrefab;
	[Min(0)] public int tileRadius;
	[Min(0)] public int tileVertexCount = 256;
	[Min(0)] public float tileSize = 64;

	readonly Dictionary<Vector2Int, OceanMeshGenerator> tiles = new();

	void Awake() {
		var prefabComponent = oceanTilePrefab.GetComponent<OceanMeshGenerator>();
		prefabComponent.sideVertexCount = tileVertexCount;
		prefabComponent.size = tileSize;

		for (int x = -tileRadius; x <= tileRadius; ++x)
		for (int z = -tileRadius; z <= tileRadius; ++z) {
			var oceanTile = Instantiate(oceanTilePrefab, new Vector3(x, 0, z) * tileSize, Quaternion.identity, transform)
				.GetComponent<OceanMeshGenerator>();
			tiles.Add(new Vector2Int(x, z), oceanTile);
		}
	}

	public OceanMeshGenerator GetOceanMeshGenerator(Vector3 shipPosition) => tiles[ConvertVector(shipPosition)];

	Vector2Int ConvertVector(Vector3 vector) => new(
		Mathf.FloorToInt(vector.x / tileSize),
		Mathf.FloorToInt(vector.z / tileSize)
	);
}