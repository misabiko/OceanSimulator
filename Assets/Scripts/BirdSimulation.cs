using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSimulation : MonoBehaviour
{
    public static BirdSimulation instance;
    public GameObject boidPrefab;
    public int boidCount = 20;
    public GameObject[] allBoids;
    public Vector3 spaceBounds = new Vector3(5, 5, 5);
    public Vector3 goalPos =  new Vector3(0, 0, 0);

    [Header("Boid settings")]
    //[Range(0f, 100.0f)]
    //public float minSpeed;
    //[Range(0f, 100.0f)]
    //public float maxSpeed;
    //[Range(0f, 100.0f)]
    //public float rotationSpeed;
    [Range(0f, 100.0f)]
    public float DetectRadius;
    [Range(0f, 100.0f)]
    public float AvoidanceRadius;
    [Range(0f, 100.0f)]
    public float Cohesion;
    [Range(0f, 100.0f)]
    public float Alignment;
    [Range(0f, 100.0f)]
    public float Separation;
    [Range(0f, 100.0f)]
    public float GoalWeight;
    [Range(0f, 100.0f)]
    public float WanderWeight;
    [Range(0f, 100.0f)]
    public float VelocityLerp;



    // Start is called before the first frame update
    void Start()
    {
        allBoids = new GameObject[boidCount];
        for (int i = 0; i < boidCount; i++)
        {
            Vector3 pos = this.transform.position + new Vector3(
                Random.Range(-spaceBounds.x, spaceBounds.x),
                Random.Range(-spaceBounds.y, spaceBounds.y),
                Random.Range(-spaceBounds.z, spaceBounds.z)
            );
            allBoids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
        }
        instance = this;
        goalPos = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Random.Range(0, 100) < 10)
        {
            goalPos = this.transform.position + new Vector3(
                Random.Range(-spaceBounds.x, spaceBounds.x),
                Random.Range(-spaceBounds.y, spaceBounds.y),
                Random.Range(-spaceBounds.z, spaceBounds.z)
            );
        }
    }
}
