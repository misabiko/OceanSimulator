using System;
using UnityEngine;
using Random = System.Random;

public class RockSpawner : MonoBehaviour
{
    [SerializeField] private float innerRadius = 100f;
    public float outerRadius = 1000f;
    public int minHeight = 10;
    public int maxHeight = 100;
    [SerializeField] private float angle = 0;
    private BoatController _boatController;
    private BirdController _birdController;

    public void InitializeSpawner()
    {
        _birdController = GameObject.FindAnyObjectByType<BirdController>();
        _boatController = GameObject.FindAnyObjectByType<BoatController>();
        transform.position =
            new Vector3(_boatController.transform.position.x, 100, _boatController.transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerStateManager.GetState() == PlayerState.Bird)
            transform.position = new Vector3(_birdController.transform.position.x, 100,
                _birdController.transform.position.z);
        else if (PlayerStateManager.GetState() == PlayerState.Boat)
            transform.position = new Vector3(_boatController.transform.position.x, 100,
                _boatController.transform.position.z);
    }

    public Vector3 GetRandomSpawnPosition()
    {
        var rand = new Random();
        float range = (float) rand.NextDouble() * (angle - -angle) + -angle;
        float radians = (90 - range) * Mathf.Deg2Rad;
        
        int heightRange = rand.Next(minHeight, maxHeight);
        
        Vector3 rotationVector = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));

        Vector3 startVector = transform.position + (rotationVector * innerRadius);
        Vector3 endVector = transform.position + (rotationVector * outerRadius);
        startVector.y = heightRange;
        endVector.y = heightRange;
        
        return GetRandomPositionBetweenTwoPoints(startVector, endVector);
    }

    Vector3 GetRandomPositionBetweenTwoPoints(Vector3 a, Vector3 b)
    {
        var rand = new Random();
        float t = (float) rand.NextDouble(); // Random value between 0 and 1
        return Vector3.Lerp(a, b, t);
    }
}
