using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;
using Random = UnityEngine.Random;

public class BirdSimulation : MonoBehaviour
{
    public static BirdSimulation instance;
    public GameObject boidPrefab;
    public GameObject[] allBoids;
    public Vector3 goalPos = new Vector3(0, 0, 0);

    public int boidCount = 20;
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

    [Range(0f, 1.0f)]
    public float GoalFollowersPercent = 0.3f;
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

    private BoatController _boatController;
    [SerializeField] private BirdController _birdController;

    private void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    private void Start()
    {
        //_birdController = GameObject.FindAnyObjectByType<BirdController>();
        _boatController = GameObject.FindAnyObjectByType<BoatController>();
        allBoids = new GameObject[boidCount];
        for (var i = 0; i < boidCount; i++)
        {
            
            Vector3 pos = this.transform.position + RandomInsideBound();
            var instance = Instantiate(boidPrefab, pos, Quaternion.identity);
            var boid = instance.GetComponent<Boid>();
            boid.id = i;
            allBoids[i] = instance;
        }

        instance = this;
        goalPos = this.transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        //if (UnityEngine.Random.Range(0, 100) < 1)
        //{
        //    goalPos = Vector3.Lerp(goalPos, this.transform.position + RandomInsideBound(), 0.2f);
        //}
        if (PlayerStateManager.GetState() == PlayerState.Bird)
            goalPos = _birdController.transform.position;
        else 
        if (PlayerStateManager.GetState() == PlayerState.Boat)
            goalPos = new Vector3(_boatController.transform.position.x, 15, _boatController.transform.position.z);
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
