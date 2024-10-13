using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugBirdControlUI : MonoBehaviour
{
    [SerializeField] private TMP_Text forwardVector_txt;
    [SerializeField] private TMP_Text eulerAngles_txt;
    [SerializeField] private TMP_Text momentumFactor_txt;
    [SerializeField] private TMP_Text velocity_txt;
    [SerializeField] private TMP_Text forwardSpeed_txt;
    [SerializeField] private GameObject container;
    [SerializeField] private BirdController birdController;
    
    private void Update()
    {
        Transform t_bird = birdController.transform;
        Vector3 eulerAngles = t_bird.eulerAngles;
        
        forwardVector_txt.text = "Forward Vector: "+birdController.GetForwardMovement();
        eulerAngles_txt.text = "Euler Angles: " + eulerAngles;
        momentumFactor_txt.text = "Momentum Factor: " + birdController.GetMomentumFactor();
        velocity_txt.text = "Velocity: " + birdController.GetVelocity();
        forwardSpeed_txt.text = "Forward Speed: " + birdController.ForwardSpeed();
    }

    public void ToggleContainer()
    {
        container.SetActive(!container.activeSelf);
    }
}
