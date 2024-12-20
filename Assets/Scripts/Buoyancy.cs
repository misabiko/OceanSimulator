using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Buoyancy : MonoBehaviour {
	/*Voxel Spawner*/
	[SerializeField] private float voxelSize = 0.1f;
	[SerializeField] private bool showVoxels = false;
	[SerializeField] private bool showWaterGizmos = false;
	[SerializeField] float forceDebugScale = 1f;
	[SerializeField, Range(0, 1)] private float GizmoSize = 1f;
	[SerializeField] private float torqueModifier = 0.5f;
	private float _voxelBorderDepth;
	private float _voxelBorderHeight;
	private float _voxelBorderWidth;
	private BoxCollider _voxelCollider;
	private AsyncGPUReadbackRequest _voxelReadbackRequest;
	private readonly List<GizmosData> _voxels = new();

	/*Buoyancy data*/
	[SerializeField] private float fluidDensity = 1.0f;
	[SerializeField] private Ocean ocean;
	[SerializeField] private float angularFriction = 0.1f;
	[SerializeField, Min(0)] float minCorrectionAngle = 100f;
	[SerializeField] private float lastTotalVolume = 0.0f;
	private int _gridSizeX;
	private int _gridSizeY;
	private int _gridSizeZ;
	private readonly float gravity = 9.81f;
	private int voxelCount;

	/*Ocean data*/
	private RenderTexture _displacementTexture;
	private Vector3 _oceanPosition;

	private bool _isRequestSent = false;
	private Color[] _oceanCachedData;
	[SerializeField] float _boatDensity = 2f;
	[SerializeField] ComputeShader _physicsShader;

	private Rigidbody rb;

	[SerializeField] float horizontalModifier = 1f;

	[SerializeField]
	private class GizmosData {
		public GizmosData(Vector3 position, Color color, Vector3 size) {
			Position = position;
			Color = color;
			Size = size;
		}

		public Vector3 Position { get; set; }

		public Color Color { get; set; }

		public Vector3 Size { get; set; }

		public float SubmergedVolume { get; set; }
	}

	// Start is called before the first frame update
	private void Start() {
		_voxelCollider = GetComponent<BoxCollider>();
		_displacementTexture = ocean.displacement;
		_voxelBorderWidth = _voxelCollider.bounds.size.x;
		_voxelBorderHeight = _voxelCollider.bounds.size.y;
		_voxelBorderDepth = _voxelCollider.bounds.size.z;
		_oceanPosition = ocean.transform.position;
		CalculateNbrVoxels();
		PlaceVoxels();
		rb = GetComponent<Rigidbody>();
	}

	// Update is called once per frame
	private void Update() {
		rb.mass = _boatDensity * voxelSize * voxelSize * voxelSize * voxelCount;

		CalculateSubmergedVolume();

		var angle = Vector3.SignedAngle(transform.up, Vector3.up, transform.forward);
		if (Mathf.Abs(angle) > minCorrectionAngle)
			rb.AddTorque(angularFriction * Mathf.Sign(angle) * transform.forward, ForceMode.Acceleration);
	}

	private void CalculateNbrVoxels() {
		_gridSizeX = Mathf.FloorToInt(_voxelBorderWidth / voxelSize);
		_gridSizeY = Mathf.FloorToInt(_voxelBorderHeight / voxelSize);
		_gridSizeZ = Mathf.FloorToInt(_voxelBorderDepth / voxelSize);
		var voxelBorderSmallestLength = Mathf.Min(_voxelBorderWidth, Mathf.Min(_voxelBorderHeight, _voxelBorderDepth));
	}

	private void PlaceVoxels() {
		var startPosition = new Vector3(voxelSize / 2 - _voxelBorderWidth / 2
			, voxelSize / 2 - _voxelBorderHeight / 2,
			voxelSize / 2 - _voxelBorderDepth / 2);
		for (var x = 0; x < _gridSizeX; x++) {
			for (var y = 0; y < _gridSizeY; y++) {
				for (var z = 0; z < _gridSizeZ; z++) {
					var position = startPosition + _voxelCollider.center + new Vector3(x * voxelSize,
						y * voxelSize,
						z * voxelSize);
					_voxels.Add(new GizmosData(position, Color.red, Vector3.one * voxelSize));
					voxelCount++;
				}
			}
		}
	}

	private float CalculateSubmergedVolume() {
		float totalVolume = 0;
		if (!_isRequestSent && ocean != null) {
			StartGPURequest();
		}

		if (_oceanCachedData != null) {
			float x_OceanPosition = _oceanPosition.x;
			float y_OceanPosition = _oceanPosition.y;
			float z_OceanPosition = _oceanPosition.z;
			int i = 0;
			float step = ocean.tileSize / ocean.tileSideVertexCount;
			var moduloedPosition = new Vector3(
				Mathf.Abs(transform.position.x % ocean.tileSize),
				transform.position.y,
				Mathf.Abs(transform.position.z % ocean.tileSize)
			);
			foreach (var voxel in _voxels) {
				var voxelGlobalPosition = transform.position + rb.rotation * voxel.Position;
				var voxelModuloedPosition = moduloedPosition + rb.rotation * voxel.Position;

				Vector3 closestVertex = Vector3.zero;
				float minDistance = float.MaxValue;
				foreach (var data in _oceanCachedData) {
					Vector2 basePosition = VertexBasePosition(i);
					Vector3 currentVertex = new Vector3(
						data.r + basePosition.x + x_OceanPosition,
						data.g + y_OceanPosition,
						data.b + basePosition.y + z_OceanPosition
					);
					float currentDistance = Vector3.Distance(voxelModuloedPosition, currentVertex);
					if (currentDistance < minDistance) {
						minDistance = currentDistance;
						closestVertex = currentVertex;
					}

					i++;
				}

				i = 0;
				float submergedHeight = 0;
				if (closestVertex.y > voxelGlobalPosition.y + voxelSize) {
					submergedHeight = voxelSize;
				}
				else if (closestVertex.y <= voxelGlobalPosition.y + voxelSize &&
				         closestVertex.y > voxelGlobalPosition.y - voxelSize) {
					submergedHeight = closestVertex.y - (voxelGlobalPosition.y - voxelSize);
				}

				var lowestNeighbor = closestVertex;
				for (int x = -1; x <= 1; x++) {
					for (int z = -1; z <= 1; z++) {
						if (x == 0 && z == 0) continue;
						int neighborIndex = i + x * ocean.tileSideVertexCount + z;
						if (neighborIndex < 0 || neighborIndex >= _oceanCachedData.Length) continue;
						if (_oceanCachedData[neighborIndex].g < lowestNeighbor.y) {
							lowestNeighbor = new Vector3(
								_oceanCachedData[neighborIndex].r + VertexBasePosition(neighborIndex).x + x_OceanPosition,
								_oceanCachedData[neighborIndex].g + y_OceanPosition,
								_oceanCachedData[neighborIndex].b + VertexBasePosition(neighborIndex).y + z_OceanPosition
							);
						}
					}
				}


				voxel.SubmergedVolume = Mathf.Max(0, voxelSize * voxelSize * submergedHeight);
				float buoyancyForce = -voxel.SubmergedVolume * Physics.gravity.y * fluidDensity;
				// Debug.DrawLine(voxelPosition,
				// 	voxelPosition + Vector3.up * submergedHeight,
				// 	Color.red);
				// Debug.DrawLine(voxelPosition, closestVertex, Color.yellow);
				var modifiedPos = Vector3.Lerp(transform.position, voxelGlobalPosition, torqueModifier);
				var tilePosition = new Vector3(
					Mathf.Floor(transform.position.x / ocean.tileSize) * ocean.tileSize,
					0,
					Mathf.Floor(transform.position.z / ocean.tileSize) * ocean.tileSize
				);
				var toLowestNeighbor = lowestNeighbor + tilePosition - voxelGlobalPosition;
				var force = (Vector3.up + new Vector3(toLowestNeighbor.x, 0, toLowestNeighbor.z).normalized * (Mathf.Max(0, closestVertex.y - lowestNeighbor.y) * horizontalModifier)).normalized * buoyancyForce;
				rb.AddForceAtPosition(force, modifiedPos);
				Debug.DrawLine(voxelGlobalPosition, voxelGlobalPosition + force / rb.mass + Physics.gravity / voxelCount, Color.Lerp(Color.yellow
					, Color.red, ((force / rb.mass + Physics.gravity / voxelCount).y * .5f + .5f)) * forceDebugScale);
				rb.AddForceAtPosition(Physics.gravity / voxelCount, modifiedPos, ForceMode.Acceleration);
				totalVolume += voxel.SubmergedVolume;
			}
		}
		else {
			Debug.LogWarning("Cached data not created");
		}

		lastTotalVolume = totalVolume;
		return totalVolume;
	}

	Vector2 VertexBasePosition(int index) => new(
		index % ocean.tileSideVertexCount * ocean.tileSize / ocean.tileSideVertexCount,
		Mathf.Floor(index / ocean.tileSideVertexCount) * ocean.tileSize / ocean.tileSideVertexCount
	);

	private async void StartGPURequest() {
		_isRequestSent = true;
		try {
			AsyncGPUReadbackRequest request = await AsyncGPUReadback.RequestAsync(ocean.displacement, 0);
			if (request.hasError) {
				Debug.LogError("GPU readback error");
			}
			else {
				_oceanCachedData = request.GetData<Color>().ToArray();
			}
		}
		catch (Exception e) {
			Debug.LogError("GPU Request callback error: " + e.Message);
			throw;
		}
		finally {
			_isRequestSent = false;
		}
	}

	private void OnDrawGizmos() {
		float totalVolume = 0;
		if (showWaterGizmos && ocean != null) {
			float x_OceanPosition = _oceanPosition.x;
			float y_OceanPosition = _oceanPosition.y;
			float z_OceanPosition = _oceanPosition.z;
			float step = ocean.tileSize / ocean.tileSideVertexCount;
			int i = 0;
			foreach (var data in _oceanCachedData) {
				float x_basePosition = i % ocean.tileSideVertexCount * step;
				float z_basePosition = Mathf.Floor(i / ocean.tileSideVertexCount) * step;

				float x_dataPosition = data.r + x_basePosition + x_OceanPosition;
				float y_dataPosition = data.g + y_OceanPosition;
				float z_dataPosition = data.b + z_basePosition + z_OceanPosition;
				Gizmos.color = Color.yellow;
				Gizmos.DrawSphere(new Vector3(x_dataPosition, y_dataPosition, z_dataPosition), 0.1f);
				i++;
			}
		}


		if (showVoxels)
			foreach (var voxel in _voxels) {
				Gizmos.color = Color.Lerp(Color.red, Color.yellow, voxel.SubmergedVolume);
				Gizmos.DrawSphere(transform.position + rb.rotation * voxel.Position, GizmoSize * voxelSize);
			}
	}
}