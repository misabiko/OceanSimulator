using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    [SerializeField] private GameObject voxelPrefab;
    [SerializeField] private float voxelSize = 0.1f;
    [SerializeField] private GameObject voxelsBorder;
    [SerializeField] private float voxelPaddingPercentage;

    private float _voxelBorderDepth;
    private float _voxelBorderWidth;
    private float _voxelBorderHeight;
    private int _gridSizeX = 0;
    private int _gridSizeY = 0;
    private int _gridSizeZ = 0;
    private int voxelCount = 0;
    private ArrayList _voxels = new();
    private BoxCollider _voxelCollider;
    
    // Start is called before the first frame update
    void Start()
    {
        _voxelCollider = voxelsBorder.GetComponent<BoxCollider>();
        _voxelBorderWidth = _voxelCollider.bounds.size.x;
        _voxelBorderHeight = _voxelCollider.bounds.size.y;
        _voxelBorderDepth = _voxelCollider.bounds.size.z;
        CalculateNbrVoxels();
        PlaceVoxels();
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
                }
            }
        }
    }

    void CalculateBuoyancy()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
