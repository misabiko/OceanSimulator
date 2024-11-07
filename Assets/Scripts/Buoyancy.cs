using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
//using MathNet.Numerics.LinearAlgebra.Single;

public class Buoyancy : MonoBehaviour
{
    /*Voxel Spawner*/
    [SerializeField] private GameObject voxelPrefab;
    [SerializeField] private float voxelSize = 0.1f;
    [SerializeField] private GameObject voxelsBorder;

    /*Buoyancy data*/
    [SerializeField] private float fluidDensity = 1.0f;
    [SerializeField] private GameObject ocean;
    private RenderTexture _displacementTexture;
    private int _gridSizeX;
    private int _gridSizeY;
    private int _gridSizeZ;

    /*Voxel Spawner*/
    private float _voxelBorderDepth;
    private float _voxelBorderHeight;
    private float _voxelBorderWidth;
    private BoxCollider _voxelCollider;
    private AsyncGPUReadbackRequest _voxelReadbackRequest;
    private readonly List<GizmosData> _voxels = new();

    /*Buoyancy data*/
    private readonly float gravity = 9.81f;
    private readonly float submergedVolume = 0.0f;
    private int voxelCount;

    // Start is called before the first frame update
    private void Start()
    {
        _voxelCollider = voxelsBorder.GetComponent<BoxCollider>();
        _displacementTexture = ocean.GetComponent<OceanMeshGenerator>().displacement;
        _voxelBorderWidth = _voxelCollider.bounds.size.x;
        _voxelBorderHeight = _voxelCollider.bounds.size.y;
        _voxelBorderDepth = _voxelCollider.bounds.size.z;
        CalculateNbrVoxels();
        PlaceVoxels();
        CalculateSubmergedVolume();
    }

    // Update is called once per frame
    private void Update()
    {
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
        _voxelReadbackRequest = AsyncGPUReadback.Request(_displacementTexture);
        AsyncGPUReadback.WaitAllRequests();
        var shaderData = _voxelReadbackRequest.GetData<Color>();
        Debug.Log("first pixel" + shaderData[0]);
        foreach (var gizmosData in _voxels)
        {
            
        }
        // AsyncGPUReadback.Request(_displacementTexture, 0, request =>
        // {
        //     var shaderData = request.GetData<Color32>().ToArray();
        //     foreach (var data in shaderData)
        //     foreach (var voxel in _voxels)
        //         if (Mathf.Approximately(voxel.Position.x, data.r) && Mathf.Approximately(voxel.Position.z, data.b))
        //         {
        //             /*calculate submerged height*/
        //             var submergedHeight = data.g - voxel.Position.z;
        //             var volume = voxelSize * voxelSize * submergedHeight;
        //         }
        // });

        return 0.0f;
    }
    
    private void CalculatePlane()
    {
    }

    private float CalculateBuoyancy()
    {
        return fluidDensity * submergedVolume * gravity * 1 / voxelCount;
    }

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
}