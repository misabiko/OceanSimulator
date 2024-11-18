using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatRaycaster : MonoBehaviour
{
    private Camera mainCamera;
    private BoidSelection currentSelectedBoid;
    
    private void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        RaycastHit hit;

        Vector3 viewToWorldPoint = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        
        Debug.DrawRay(viewToWorldPoint, mainCamera.transform.forward*200f, Color.green);

        if (Physics.Raycast(viewToWorldPoint, mainCamera.transform.forward, out hit, 200))
        {
            if (hit.collider.CompareTag("BirdBoid"))
            {
                currentSelectedBoid = hit.collider.GetComponent<BoidSelection>().SelectBoid();
                Debug.Log("Hit Bird");
            }
            else
            {
                if (currentSelectedBoid == null) 
                    return;

                currentSelectedBoid.SetIsSelected(false);
                currentSelectedBoid = null;
            }
        }
        else
        {
            if (currentSelectedBoid == null) 
                return;
            
            currentSelectedBoid.SetIsSelected(false);
            currentSelectedBoid = null;
        }
    }
}
