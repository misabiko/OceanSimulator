using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;
using UnityEngine.Rendering;

public class Buoyancy : MonoBehaviour
{
    /*Voxel Spawner*/
    [SerializeField] private GameObject voxelPrefab;
    [SerializeField] private float voxelSize = 0.1f;
    [SerializeField] private GameObject voxelsBorder;
    
    /*Buoyancy data*/
    [SerializeField] private float fluidDensity = 1.0f;
    [SerializeField] private GameObject ocean;
    
    /*Voxel Spawner*/
    private float _voxelBorderDepth;
    private float _voxelBorderWidth;
    private float _voxelBorderHeight;
    private int _gridSizeX = 0;
    private int _gridSizeY = 0;
    private int _gridSizeZ = 0;
    private int voxelCount = 0;
    private ArrayList _voxels = new();
    private BoxCollider _voxelCollider;
    
    /*Buoyancy data*/
    private float gravity = 9.81f;
    private float submergedVolume = 0.0f;
    private RenderTexture _displacementTexture;
    
    // Start is called before the first frame update
    void Start()
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
    
    void CalculateNbrVoxels()
    {
        _gridSizeX = Mathf.FloorToInt(_voxelBorderWidth / voxelSize);
        _gridSizeY = Mathf.FloorToInt(_voxelBorderHeight / voxelSize);
        _gridSizeZ = Mathf.FloorToInt(_voxelBorderDepth / voxelSize);
        float voxelBorderSmallestLength = Mathf.Min(_voxelBorderWidth, Mathf.Min(_voxelBorderHeight, _voxelBorderDepth));
    }

    void PlaceVoxels()
    {
       Vector3 startPosition = new Vector3(voxelSize / 2 - _voxelBorderWidth /2
           , voxelSize / 2 - _voxelBorderHeight /2, 
           voxelSize / 2 - _voxelBorderDepth /2) 
            + _voxelCollider.transform.position;
        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                for (int z = 0; z < _gridSizeZ; z++)
                {
                    Vector3 position = startPosition + new Vector3(x * voxelSize, 
                        y * voxelSize, 
                        z * voxelSize);
                    GameObject voxel = Instantiate(voxelPrefab, position, Quaternion.identity, transform);
                    _voxels.Add(voxel);
                    voxel.transform.localScale = new Vector3(voxelSize, voxelSize, voxelSize);
                    voxelCount++;
                }
            }
        }
    }

    float CalculateSubmergedVolume()
    {
        
        AsyncGPUReadback.Request(_displacementTexture, 0, RenderCallBack);
        // foreach (var voxel in _voxels)
        // {
        //     /*calculate submerged height*/
        //     // float submergedHeight = water.transform.y - voxel.transform.y
        //     // float volume += (voxelSize * voxelSize * submergedHeight)
        // }

        return 0.0f;
    }

    void RenderCallBack(AsyncGPUReadbackRequest request)
    {
        request.GetData<byte>();
    }

    void CalculatePlane()
    {
        
    }

    float CalculateBuoyancy()
    {
        return fluidDensity * submergedVolume * gravity * 1 / voxelCount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
