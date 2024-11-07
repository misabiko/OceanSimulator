using UnityEngine;
using static UnityEngine.InputSystem.HID.HID;

public class BirdSimulation : MonoBehaviour
{
    public static BirdSimulation instance;
    public GameObject boidPrefab;
    public int boidCount = 20;
    public GameObject[] allBoids;
    public Vector3 goalPos =  new Vector3(0, 0, 0);

    [Header("Boid settings")]
    //[Range(0f, 100.0f)]
    //public float minSpeed;
    //[Range(0f, 100.0f)]
    //public float maxSpeed;
    //[Range(0f, 100.0f)]
    //public float rotationSpeed;
    [Range(0f, 100)]
    public int MaxCountInProximity;
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
    public float BoundAvoidanceWeight;
    [Range(0f, 100.0f)]
    public float ObstacleAvoidanceWeight;
    [Range(0f, 100.0f)]
    public float OvercrowdWeight;
    [Range(0f, 100.0f)]
    public float VelocityLerp;
    [Range(0f, 100.0f)]
    public float MaxSpeed = 5f;
    [Range(10f, 100.0f)]
    public float SpaceBoundRadius;



    // Start is called before the first frame update
    void Start()
    {
        allBoids = new GameObject[boidCount];
        for (int i = 0; i < boidCount; i++)
        {
            Vector3 pos = this.transform.position + Random.insideUnitSphere * SpaceBoundRadius;
            allBoids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
        }
        instance = this;
        goalPos = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Random.Range(0, 100) < 1)
        {
            goalPos = Vector3.Lerp(goalPos, this.transform.position + Random.insideUnitSphere * SpaceBoundRadius, 0.2f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0);
        Gizmos.DrawWireSphere(this.goalPos, 1);
        Gizmos.color = new Color(1, 0, 0);
        Gizmos.DrawWireSphere(Vector3.zero, SpaceBoundRadius);
    }

}