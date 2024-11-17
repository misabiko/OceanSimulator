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
        _displacementTexture = ocean.GetComponent<OceanMeshGenerator>().displacement;
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
       GetComponent<Rigidbody>().AddForce(Vector3.up * CalculateBuoyancy());
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

            //GameObject voxel = Instantiate(voxelPrefab, position, Quaternion.identity, transform);
           // Gizmos.DrawCube(position, Vector3.one * voxelSize);
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
        
        Debug.Log((_oceanCachedData[2] + " " + _oceanCachedData[3] + " " + _oceanCachedData[4]));
        Debug.Log((_oceanCachedData[255] + " " + _oceanCachedData[256] + " " + _oceanCachedData[257]));
        
        if (_oceanCachedData != null)
        {
            float x_OceanPosition = _oceanPosition.x;
            float y_OceanPosition = _oceanPosition.y;
            float z_OceanPosition = _oceanPosition.z;
            foreach (var data in _oceanCachedData)
            {
                float x_dataPosition = data.g + x_OceanPosition;
                float y_dataPosition = data.r + y_OceanPosition;
                float z_dataPosition = data.b + z_OceanPosition; 
                foreach (var voxel  in _voxels)
                {
                    if (x_dataPosition <= voxel.Position.x + voxelSize && x_dataPosition > voxel.Position.x - voxelSize 
                                                                        && z_dataPosition <= voxel.Position.z + voxelSize 
                                                                        && z_dataPosition > voxel.Position.z - voxelSize)
                    {
                        if (y_dataPosition <= voxel.Position.y + voxelSize &&
                            y_dataPosition > voxel.Position.y - voxelSize)
                        {
                            float voxel_submergedVolume = voxelSize * voxelSize * (y_dataPosition - voxel.Position.y - voxelSize);
                            totalVolume += voxel_submergedVolume;
                        }
                    }
                }
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
        Debug.Log(voxelCount + " voxels");
        Debug.Log(CalculateSubmergedVolume() + " submerged");
        Debug.Log(fluidDensity + " fluid density");
        return fluidDensity * CalculateSubmergedVolume() * gravity * 1 / voxelCount;
    }

    private async void StartGPURequest()
    {
        _isRequestSent = true;
        try
        {
            Debug.Log("Requesting buoyancy");
            await Task.Delay(1000);
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