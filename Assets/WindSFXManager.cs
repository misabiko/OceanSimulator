using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindSFXManager : MonoBehaviour
{
    [SerializeField] private BirdController birdController;

    private AudioSource audioSource;
    
    private void Awake()
    {
        birdController = GameObject.FindObjectOfType<BirdController>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        audioSource.volume = birdController.GetMomentumFactor();
    }
}
