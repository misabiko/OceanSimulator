using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
//using MathNet.Numerics.LinearAlgebra.Single;

public class Buoyancy : MonoBehaviour
{
    /*Voxel Spawner*/
    [SerializeField] private GameObject voxelPrefab;
    [SerializeField] private float voxelSize = 0.1f;
    [SerializeField] private GameObject voxelsBorder;
    private float _voxelBorderDepth;
    private float _voxelBorderHeight;
    private float _voxelBorderWidth;
    private BoxCollider _voxelCollider;
    private AsyncGPUReadbackRequest _voxelReadbackRequest;
    private readonly List<GizmosData> _voxels = new();

    /*Buoyancy data*/
    [SerializeField] private float fluidDensity = 1.0f;
    [SerializeField] private GameObject ocean;
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
    private OceanMeshGenerator _oceanMeshGenerator;
    public float buoyancyAdjustment = 10f;

    [SerializeField]
    private class GizmosData
    {
        public GizmosData(Vector3 position, Color color, Vector3 size)
        {
            position = Position;
            color = Color;
            size = Size;
        }

        public Vector3 Position { get; }

        public Color Color { get; }

        public Vector3 Size { get; }
    }
    
    // Start is called before the first frame update
    private void Start()
    {
        _voxelCollider = voxelsBorder.GetComponent<BoxCollider>();
        _oceanMeshGenerator = ocean.GetComponent<OceanMeshGenerator>(); 
        _displacementTexture = _oceanMeshGenerator.displacement;
        _voxelBorderWidth = _voxelCollider.bounds.size.x;
        _voxelBorderHeight = _voxelCollider.bounds.size.y;
        _voxelBorderDepth = _voxelCollider.bounds.size.z;
        _oceanPosition = ocean.transform.position;
        CalculateNbrVoxels();
        PlaceVoxels();
    }

    // Update is called once per frame
    private void Update()
    {
        float buoyancy = CalculateBuoyancy();
       GetComponent<Rigidbody>().AddForce(Vector3.up * buoyancy, ForceMode.Impulse);
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
                                voxelSize / 2 - _voxelBorderDepth / 2)
                            + _voxelCollider.transform.position;
        for (var x = 0; x < _gridSizeX; x++)
        for (var y = 0; y < _gridSizeY; y++)
        for (var z = 0; z < _gridSizeZ; z++)
        {
            var position = startPosition + new Vector3(x * voxelSize,
                y * voxelSize,
                z * voxelSize);
            //Debug.DrawLine(position, position + voxelSize, Color.yellow);
            
           // Gizmos.DrawCube(position, new Vector3(voxelSize, voxelSize, voxelSize));
            _voxels.Add(new GizmosData(position, Color.red, Vector3.one * voxelSize));
            voxelCount++;
        }
    }

    private float CalculateSubmergedVolume()
    {
        float totalVolume = 0;
        if (!_isRequestSent)
        {
            StartGPURequest();
        }

        if (_oceanCachedData != null)
        {
            float x_OceanPosition = _oceanPosition.x;
            float y_OceanPosition = _oceanPosition.y;
            float z_OceanPosition = _oceanPosition.z;
            int i = 0;
            float step = _oceanMeshGenerator.size / _oceanMeshGenerator.xSize;
            foreach (var data in _oceanCachedData)
            {
                float x_basePosition = i % _oceanMeshGenerator.xSize * step;
                float z_basePosition = Mathf.Floor(i / _oceanMeshGenerator.xSize) * step;
                
                float x_dataPosition = data.g + x_basePosition + x_OceanPosition;
                float y_dataPosition = data.r + y_OceanPosition;
                float z_dataPosition = data.b + z_basePosition + z_OceanPosition; 
                foreach (var voxel  in _voxels)
                {
                  float x_voxelPositon = voxel.Position.x + transform.position.x;
                  float y_voxelPositon = voxel.Position.y + transform.position.y;
                  float z_voxelPositon = voxel.Position.z + transform.position.z;
                    if (x_dataPosition <= x_voxelPositon + voxelSize && x_dataPosition > x_voxelPositon - voxelSize 
                                                                        && z_dataPosition <= z_voxelPositon + voxelSize 
                                                                        && z_dataPosition > z_voxelPositon - voxelSize)
                    {
                        float submergedHeight = 0;
                        if (y_dataPosition > y_voxelPositon + voxelSize)
                        {
                            Debug.Log("sphere is underwater");
                            submergedHeight = voxelSize;
                        }
                        else if (y_dataPosition <= y_voxelPositon + voxelSize &&
                            y_dataPosition > y_voxelPositon - voxelSize)
                        {
                            submergedHeight = y_dataPosition - (y_voxelPositon - voxelSize);
                            
                        }
                        float voxel_submergedVolume = Mathf.Abs(voxelSize * voxelSize * (submergedHeight));
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
    
    private void CalculatePlane()
    {
    }

    private float CalculateBuoyancy()
    {
        float submergedVolume = CalculateSubmergedVolume();
        return fluidDensity * submergedVolume * gravity * 1 / voxelCount;
    }

    private async void StartGPURequest()
    {
        _isRequestSent = true;
        try
        {
            AsyncGPUReadbackRequest request = await AsyncGPUReadback.RequestAsync(_displacementTexture, 0);
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
    
}