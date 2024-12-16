using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;
using Random = UnityEngine.Random;

public class BirdSimulation : MonoBehaviour
{
    public static BirdSimulation instance;
    public GameObject boidPrefab;
    public int boidCount = 20;
    public GameObject[] allBoids;
    public Vector3 goalPos = new Vector3(0, 0, 0);

    [Header("Boid settings")]
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
    public float MaxSpeed = 5f;

    public Vector3 SpaceBoundSizeRadius = new Vector3(60, 60, 60);
    [Range(0f, 100.0f)]
    public float BoundAvoidanceWeight;

    [Range(0f, 100.0f)]
    public float GoalWeight;
    [Range(0f, 100.0f)]
    public float WanderWeight;
    [Range(0f, 100.0f)]
    public float ObstacleAvoidanceWeight;
    [Range(0f, 100.0f)]
    public float OvercrowdWeight;
    [Range(0f, 100)]
    public int MaxCountInProximity;
    [Range(0f, 100.0f)] 
    public float VelocityLerp;

    private int chunkWidth;
    private int chunkHeight;
    private int[] chunks;
    private Vector3[] velocities;
    private Vector3[] positions;
    private float[] animationTimes;

    [SerializeField] public Mesh mesh;               // The mesh to render
    [SerializeField] public Material material;       // The material to use
    //[SerializeField] private int instanceCount = 100; // Number of instances
    //private Matrix4x4[] instanceMatrices;             // Array of instance transforms
    private InstanceData[] instances;
    struct InstanceData
    {
        public Matrix4x4 matrix;
        public float animationTime;
    }
    private RenderParams rp;

    // Start is called before the first frame update
    private void Start()
    {
        allBoids = new GameObject[boidCount];
        for (var i = 0; i < boidCount; i++)
        {
            Vector3 pos = this.transform.position + RandomInsideBound();
            allBoids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
        }

        instance = this;
        goalPos = this.transform.position;
        rp = new RenderParams(material);
        instances = new InstanceData[boidCount];
    }

    // Update is called once per frame
    private void Update()
    {
        //if (Random.Range(0, 100) < 1)
        //{
        //    goalPos = Vector3.Lerp(goalPos, this.transform.position + RandomInsideBound(), 0.2f);
        //}
        // Render all instances using instanced rendering
        //Graphics.DrawMeshInstanced(mesh, 0, material, instanceMatrices);

        for (int i = 0; i < boidCount; ++i)
        {
            instances[i].matrix = Matrix4x4.Translate(new Vector3(-4.5f + i, 0.0f, 5.0f));
            instances[i].animationTime += Time.deltaTime;
            instances[i].animationTime %= 0.956f; // animationLength
        }
        
        Graphics.RenderMeshInstanced(rp, mesh, 0, instances);
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = new Color(0, 1, 0);
        //Gizmos.DrawWireSphere(this.goalPos, 1);
        Gizmos.color = new Color(1, 0, 0);
        Gizmos.DrawWireCube(this.transform.position, SpaceBoundSizeRadius * 2);
    }

    private Vector3 RandomInsideBound()
    {
        return new Vector3(
                    Random.Range(-SpaceBoundSizeRadius.x, SpaceBoundSizeRadius.x),
                    Random.Range(-SpaceBoundSizeRadius.y, SpaceBoundSizeRadius.z),
                    Random.Range(-SpaceBoundSizeRadius.y, SpaceBoundSizeRadius.z)
                );
    }

}
