using UnityEngine;

public class BirdSimulation : MonoBehaviour
{
    public static BirdSimulation instance;
    public GameObject boidPrefab;
    public int boidCount = 20;
    public GameObject[] allBoids;
    public Vector3 spaceBounds = new(5, 5, 5);
    public Vector3 goalPos = new(0, 0, 0);

    [Header("Boid settings")]
    //[Range(0f, 100.0f)]
    //public float minSpeed;
    //[Range(0f, 100.0f)]
    //public float maxSpeed;
    //[Range(0f, 100.0f)]
    //public float rotationSpeed;
    [Range(0f, 100.0f)]
    public float DetectRadius;

    [Range(0f, 100.0f)] public float AvoidanceRadius;

    [Range(0f, 100.0f)] public float Cohesion;

    [Range(0f, 100.0f)] public float Alignment;

    [Range(0f, 100.0f)] public float Separation;

    [Range(0f, 100.0f)] public float GoalWeight;

    [Range(0f, 100.0f)] public float WanderWeight;

    [Range(0f, 100.0f)] public float VelocityLerp;


    // Start is called before the first frame update
    private void Start()
    {
        allBoids = new GameObject[boidCount];
        for (var i = 0; i < boidCount; i++)
        {
            var pos = transform.position + new Vector3(
                Random.Range(-spaceBounds.x, spaceBounds.x),
                Random.Range(-spaceBounds.y, spaceBounds.y),
                Random.Range(-spaceBounds.z, spaceBounds.z)
            );
            allBoids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
        }

        instance = this;
        goalPos = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Random.Range(0, 100) < 10)
            goalPos = transform.position + new Vector3(
                Random.Range(-spaceBounds.x, spaceBounds.x),
                Random.Range(-spaceBounds.y, spaceBounds.y),
                Random.Range(-spaceBounds.z, spaceBounds.z)
            );
    }
}