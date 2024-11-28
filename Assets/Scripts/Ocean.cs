using UnityEngine;

public class Ocean : MonoBehaviour {
	//TODO Replace prefab with creating and populating component here
	public GameObject oceanTilePrefab;
	[Min(0)]
	public int tileRadius;

	void Awake() {
		var firstTileGO= Instantiate(oceanTilePrefab, Vector3.zero, Quaternion.identity, transform);
		//TODO Move properties to Ocean
		var firstOcean = firstTileGO.GetComponent<OceanMeshGenerator>();

		for (int x = -tileRadius; x <= tileRadius; ++x)
			for (int z = -tileRadius; z <= tileRadius; ++z) {
				if (x == 0 && z == 0)
					continue;
				Instantiate(oceanTilePrefab, new Vector3(x * firstOcean.size, 0f, z * firstOcean.size), Quaternion.identity, transform);
			}
	}
}