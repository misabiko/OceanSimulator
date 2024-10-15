using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class Boyancy : MonoBehaviour
{
    [SerializeField] private int nbrOfVoxels = 1;
    [SerializeField] private GameObject voxelsArea;
    [SerializeField] private float padding = 0.1f;
    private List<GameObject> _voxels = new List<GameObject>();
    private float _voxelVolumeHeight;
    private float _voxelVolumeWidth;
    private float _voxelVolumeDepth;
    private float _voxelHeight;
    private float _voxelWidth;
    private float _voxelDepth;
    
    private Collider _collider;
    // Start is called before the first frame update
    void Start()
    {
        _collider = GetComponent<Collider>();
        _voxelVolumeHeight = _collider.bounds.size.y;
        _voxelVolumeWidth = _collider.bounds.size.x;
        _voxelVolumeDepth = _collider.bounds.size.z;
    }

    float calculateVoxelVolume()
    {
        // Find minimum between height, width and depth
        float minValue = Math.Min(_voxelVolumeHeight, Math.Min(_voxelVolumeWidth, _voxelVolumeDepth));
        return minValue;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
