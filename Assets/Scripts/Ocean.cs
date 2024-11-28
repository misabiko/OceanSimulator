using System.Collections.Generic;
using UnityEngine;

public class Ocean : MonoBehaviour
{
	//TODO Replace prefab with creating and populating component here
	public GameObject oceanTilePrefab;
	[Min(0)] public int tileRadius;
	private Dictionary<Vector2Int, OceanMeshGenerator> tiles = new Dictionary<Vector2Int, OceanMeshGenerator>();
	private float _tileSize = 0;
	void Awake()
	{
		var firstTileGO = Instantiate(oceanTilePrefab, Vector3.zero, Quaternion.identity, transform);
		//TODO Move properties to Ocean
		var firstOcean = firstTileGO.GetComponent<OceanMeshGenerator>();
		_tileSize = firstOcean.size;
		tiles.Add(new Vector2Int(0,0), firstOcean);

		for (int x = -tileRadius; x <= tileRadius; ++x)
		for (int z = -tileRadius; z <= tileRadius; ++z)
		{
			if (x == 0 && z == 0)
				continue;
			GameObject oceanTile = Instantiate(oceanTilePrefab, new Vector3(x * firstOcean.size, 0f, z * firstOcean.size), Quaternion.identity,
				transform);
			tiles.Add(new Vector2Int(x, z), oceanTile.GetComponent<OceanMeshGenerator>());
		}
	}

	public OceanMeshGenerator getOceanMeshGenerator(Vector3 shipPosition)
	{
		return tiles[convertVector(shipPosition)];
	}

	private Vector2Int convertVector(Vector3 vector)
	{
		int x = Mathf.FloorToInt(vector.x / _tileSize);
		int z = Mathf.FloorToInt(vector.z / _tileSize);
		return new Vector2Int(x, z);
	}
}
