using System;
using UnityEngine;
using Random = System.Random;

public class CloudSpawner : MonoBehaviour
{
    [SerializeField] private float innerRadius = 100f;
    public float outerRadius = 300f;
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
        float theta = (90 - range) * Mathf.Deg2Rad;
        
        float heightRange =(float) rand.NextDouble() * (20 - -20) + -20;
        
        //v(cos(theta), 0, SinTheta)
        Vector3 rotationVector = new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta));

        Vector3 startPosition = transform.position + (rotationVector * innerRadius) + new Vector3(0, heightRange, 0);
        Vector3 endPosition = transform.position + (rotationVector * outerRadius)+ new Vector3(0, heightRange, 0); 
        
        return GetRandomPositionBetweenTwoPoints(startPosition, endPosition);
    }

    Vector3 GetRandomPositionBetweenTwoPoints(Vector3 a, Vector3 b)
    {
        var rand = new Random();
        float t = (float) rand.NextDouble(); // Random value between 0 and 1
        return Vector3.Lerp(a, b, t);
    }
}
