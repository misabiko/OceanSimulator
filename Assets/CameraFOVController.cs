using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraFOVController : MonoBehaviour
{
    [SerializeField] private BirdController myBirdController;
    [SerializeField] private float defaultFOV = 40f;
    
    private CinemachineVirtualCamera myCinemachineVirtualCamera;

    private void Awake()
    {
        myCinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    private void Update()
    {
        myCinemachineVirtualCamera.m_Lens.FieldOfView = myBirdController.IsFast() ? defaultFOV + myBirdController.SpeedDifference() : defaultFOV;
    }
}
