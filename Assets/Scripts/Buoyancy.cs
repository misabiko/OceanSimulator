using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Buoyancy : MonoBehaviour
{
    /*Voxel Spawner*/
    [SerializeField] private float voxelSize = 0.1f;
    [SerializeField] private bool showVoxels = false;
    [SerializeField] private bool showWaterGizmos = false;
    [SerializeField, Range(0,1)] private float GizmoSize = 1f;
    private float _voxelBorderDepth;
    private float _voxelBorderHeight;
    private float _voxelBorderWidth;
    private BoxCollider _voxelCollider;
    private AsyncGPUReadbackRequest _voxelReadbackRequest;
    private readonly List<GizmosData> _voxels = new();

    /*Buoyancy data*/
    [SerializeField] private float fluidDensity = 1.0f;
    [SerializeField] private Ocean ocean;
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
    
    [SerializeField]
    private class GizmosData
    {
        public GizmosData(Vector3 position, Color color, Vector3 size)
        {
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
    private void Start()
    {
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
    private void Update()
    {
      rb.mass = _boatDensity * _gridSizeX * voxelSize
                * _gridSizeY * voxelSize * _gridSizeZ 
                * voxelSize;
      float buoyancy = CalculateBuoyancy();
      rb.AddForce(Vector3.up * buoyancy, ForceMode.Force);
    }

    private void CalculateNbrVoxels()
    {
        _gridSizeX = Mathf.FloorToInt(_voxelBorderWidth / voxelSize);
        _gridSizeY = Mathf.FloorToInt(_voxelBorderHeight / voxelSize);
        _gridSizeZ = Mathf.FloorToInt(_voxelBorderDepth / voxelSize);
        var voxelBorderSmallestLength = Mathf.Min(_voxelBorderWidth, Mathf.Min(_voxelBorderHeight, _voxelBorderDepth));
    }

    private void PlaceVoxels()
    {
        var startPosition = new Vector3(voxelSize / 2 - _voxelBorderWidth / 2
                                , voxelSize / 2 - _voxelBorderHeight / 2,
                                voxelSize / 2 - _voxelBorderDepth / 2);
        for (var x = 0; x < _gridSizeX; x++)
        {
            
            for (var y = 0; y < _gridSizeY; y++)
            {
                for (var z = 0; z < _gridSizeZ; z++)
                {
                    var position = startPosition + _voxelCollider.center + new Vector3(x * voxelSize,
                        y * voxelSize,
                        z * voxelSize);
                    _voxels.Add(new GizmosData(position, Color.red, Vector3.one * voxelSize));
                    voxelCount++;
                }
            }
        }
    }

    private float CalculateSubmergedVolume()
    {
        float totalVolume = 0;
        if (!_isRequestSent && ocean != null)
        {
            StartGPURequest();
        }

        if (_oceanCachedData != null)
        {
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
            foreach (var data in _oceanCachedData)
            {
                float x_basePosition = i % ocean.tileSideVertexCount * step;
                float z_basePosition = Mathf.Floor(i / ocean.tileSideVertexCount) * step;
                
                float x_dataPosition = data.r + x_basePosition + x_OceanPosition;
                float y_dataPosition = data.g + y_OceanPosition;
                float z_dataPosition = data.b + z_basePosition + z_OceanPosition; 
                foreach (var voxel  in _voxels)
                {
                  float x_voxelPositon = voxel.Position.x + moduloedPosition.x;
                  float y_voxelPositon = voxel.Position.y + moduloedPosition.y;
                  float z_voxelPositon = voxel.Position.z + moduloedPosition.z;
                    if (x_dataPosition <= x_voxelPositon + voxelSize && x_dataPosition > x_voxelPositon - voxelSize 
                                                                        && z_dataPosition <= z_voxelPositon + voxelSize 
                                                                        && z_dataPosition > z_voxelPositon - voxelSize)
                    {
                        float submergedHeight = 0;
                        if (y_dataPosition > y_voxelPositon + voxelSize)
                        {
                            submergedHeight = voxelSize;
                        }
                        else if (y_dataPosition <= y_voxelPositon + voxelSize &&
                            y_dataPosition > y_voxelPositon - voxelSize)
                        {
                            submergedHeight = y_dataPosition - (y_voxelPositon - voxelSize);
                            
                        }
                        float voxel_submergedVolume = Mathf.Abs(voxelSize * voxelSize * (submergedHeight));
                        voxel.SubmergedVolume = voxel_submergedVolume;
                        totalVolume += voxel_submergedVolume ;
                    }
                }
                i++;
            }
        }
        else
        {
            Debug.LogWarning("Cached data not created");
        }
        return totalVolume;
    }

    private float CalculateBuoyancy()
    {
        // getCurrentOceanMesh();
        float submergedVolume = CalculateSubmergedVolume();
        return -(fluidDensity * submergedVolume * Physics.gravity.y * 1 / voxelCount);
    }

    private async void StartGPURequest()
    {
        _isRequestSent = true;
        try
        {
            AsyncGPUReadbackRequest request = await AsyncGPUReadback.RequestAsync(ocean.displacement, 0);
            if (request.hasError)
            {
                Debug.LogError("GPU readback error");
            }
            else
            {
                _oceanCachedData = request.GetData<Color>().ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("GPU Request callback error: " + e.Message);
            throw;
        }
        finally
        {
            _isRequestSent = false;
        }
    }

      private void OnDrawGizmos()
      {
          float totalVolume = 0;
          if ( showWaterGizmos && ocean != null)
          {
              float x_OceanPosition = _oceanPosition.x;
              float y_OceanPosition = _oceanPosition.y;
              float z_OceanPosition = _oceanPosition.z;
              float step = ocean.tileSize / ocean.tileSideVertexCount;
              int i = 0;
              foreach (var data in _oceanCachedData)
              {
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
              foreach (var voxel in _voxels)
              {
                  Gizmos.color = Color.Lerp(Color.red, Color.yellow, voxel.SubmergedVolume);
                  Gizmos.DrawSphere(transform.position + voxel.Position, GizmoSize * voxelSize);
              } 
      }
}