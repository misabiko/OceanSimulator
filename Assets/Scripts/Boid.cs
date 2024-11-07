using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Boid : MonoBehaviour
{
    //float speed;
    private const float Speed = 5f;
    public Vector3 velocity;
    public Vector3 centroid;

    // Start is called before the first frame update
    private void Start()
    {
        velocity = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)).normalized *
                   Random.Range(1, Speed);
    }

    // Update is called once per frame
    private void Update()
    {
        ApplyRulesBoids();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 1);
        Gizmos.DrawWireSphere(centroid, BirdSimulation.instance.DetectRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1);
        Gizmos.DrawWireSphere(transform.position, BirdSimulation.instance.DetectRadius);
        Gizmos.DrawWireSphere(transform.position, BirdSimulation.instance.AvoidanceRadius);
    }


    private void ApplyRulesBoids()
    {
        var avgPos = Vector3.zero;
        var avgVel = Vector3.zero;
        var close_d = Vector3.zero;

        var previousVelocity = velocity;

        var pos0 = transform.position;
        var vel0 = velocity;

        var wander = vel0 * (float)Math.Tan(2f * Math.PI / 12f);
        wander *= (float)Math.Sin(DateTime.Now.Ticks / TimeSpan.TicksPerSecond);

        // Flocking
        var countInProximity = 0;
        var countInAvoidance = 0;
        foreach (var boid in BirdSimulation.instance.allBoids)
        {
            var b = boid.GetComponent<Boid>();
            if (b == this)
                continue;

            var pos1 = boid.transform.position;
            var vel1 = b.velocity;
            var dist = pos1 - pos0;
            var d = dist.magnitude;
            if (d <= BirdSimulation.instance.AvoidanceRadius)
            {
                countInAvoidance++;
                close_d += -dist.normalized * (BirdSimulation.instance.AvoidanceRadius - dist.magnitude);
            }
            else if (d <= BirdSimulation.instance.DetectRadius) // && Vector3.Dot(vel0.normalized, dist) > -0.5f)
            {
                countInProximity++;
                avgPos += pos1;
                avgVel += vel1;
            }
        }

        // Apply
        if (countInProximity > 0)
        {
            avgPos /= countInProximity;
            avgVel /= countInProximity;
            centroid = avgPos;

            var cohesion = (avgPos - pos0) * BirdSimulation.instance.Cohesion;
            var align = (avgVel - vel0) * BirdSimulation.instance.Alignment;
            Debug.DrawLine(transform.position, transform.position + cohesion, Color.green);
            Debug.DrawLine(transform.position, transform.position + align, Color.blue);
            vel0 += cohesion + align;
        }

        if (countInAvoidance > 0)
            //close_d /= countInAvoidance;
            vel0 += close_d * BirdSimulation.instance.Separation;

        vel0 += wander * BirdSimulation.instance.WanderWeight;
        vel0 += (BirdSimulation.instance.goalPos - transform.position).normalized * BirdSimulation.instance.GoalWeight;

        // Clamp
        var speed = Math.Clamp(vel0.magnitude, 1, Speed);
        vel0 = vel0.normalized * speed;
        //vel0 = (vel0 + previousVelocity) / 2.0f;
        vel0 = Vector3.Lerp(previousVelocity, vel0, BirdSimulation.instance.VelocityLerp);
        velocity = vel0;

        // Transform
        Debug.DrawLine(transform.position, transform.position + velocity, Color.red);
        transform.LookAt(transform.position + velocity);
        transform.Translate(velocity * Time.deltaTime, Space.World);
    }
}