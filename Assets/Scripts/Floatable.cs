using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Floatable : MonoBehaviour
{
    [SerializeField] float fluidDensity;
    [SerializeField] Collider waterCollider;
    float submergedVolume = 0;
    float gravity = 9.81f;
    float sphereRadius = 0;
    SphereCollider sphereCollider;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereRadius = sphereCollider.radius;
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
       CalculateFluidVolume();
        if (submergedVolume > 0)
        {
            rb.AddForce(Vector3.up * ApplyBuoyancy());
        }
    }

    private void CalculateFluidVolume()
    {
        float maxSphereY = sphereCollider.transform.position.y + sphereRadius;
        float minSphereY = sphereCollider.transform.position.y - sphereRadius;
        if (waterCollider != null && waterCollider.tag == "water")
        {
            float waterHeight = waterCollider.transform.position.y;
            if (maxSphereY > waterHeight && waterHeight > minSphereY)
            {
                float submergedHeight = waterHeight - minSphereY;
                Debug.Log("submergedHeight " + submergedHeight);
                submergedVolume = (Mathf.PI * submergedHeight * submergedHeight * (3 * sphereRadius - submergedHeight)) / 3;  
            }

        }
        else
        {
            Debug.Log("Forgot to add the water collider!");
        }
    }

    private float ApplyBuoyancy()
    {
        // Buoyancy force
        return fluidDensity * gravity * submergedVolume;
    }
}
