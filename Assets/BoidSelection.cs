using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSelection : MonoBehaviour
{
    private Outline myOutline;
    [SerializeField] private bool myIsSelected;
    
    private void Awake()
    {
        myOutline = GetComponent<Outline>();
    }

    private void Update()
    {
        myOutline.OutlineColor = myIsSelected ? Color.green : Color.clear;
    }

    public void SetIsSelected(bool aState)
    {
        myIsSelected = aState;
    }

    public BoidSelection SelectBoid()
    {
        myIsSelected = true;
        return this;
    }
}
