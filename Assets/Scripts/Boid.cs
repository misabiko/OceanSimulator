using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Boid : MonoBehaviour
{
    //float speed;
    const float Speed = 5f;
    public Vector3 velocity;
    public Vector3 centroid;

    // Start is called before the first frame update
    void Start()
    {
        velocity = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)).normalized * Random.Range(1, Speed);
    }

    // Update is called once per frame
    void Update()
    {
        ApplyRulesBoids();
    }


    void ApplyRulesBoids()
    {
        Vector3 avgPos = Vector3.zero;
        Vector3 avgVel = Vector3.zero;
        Vector3 close_d = Vector3.zero;

        var previousVelocity = this.velocity;

        var pos0 = this.transform.position;
        var vel0 = this.velocity;

        Vector3 wander = vel0 * (float) Math.Tan(2f * Math.PI / 12f);
        wander *= (float) Math.Sin(DateTime.Now.Ticks / TimeSpan.TicksPerSecond);

        // Flocking
        int countInProximity = 0;
        int countInAvoidance = 0;
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
            else
            if (d <= BirdSimulation.instance.DetectRadius) // && Vector3.Dot(vel0.normalized, dist) > -0.5f)
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
            Debug.DrawLine(this.transform.position, this.transform.position + cohesion, Color.green);
            Debug.DrawLine(this.transform.position, this.transform.position + align, Color.blue);
            vel0 += cohesion + align;
        }
        if (countInAvoidance > 0)
        {
            //close_d /= countInAvoidance;
            vel0 += close_d * BirdSimulation.instance.Separation;
        }

        vel0 += wander * BirdSimulation.instance.WanderWeight;
        vel0 += (BirdSimulation.instance.goalPos - this.transform.position).normalized * BirdSimulation.instance.GoalWeight;

        // Clamp
        var speed = Math.Clamp(vel0.magnitude, 1, Speed);
        vel0 = vel0.normalized * speed;
        //vel0 = (vel0 + previousVelocity) / 2.0f;
        vel0 = Vector3.Lerp(previousVelocity, vel0, BirdSimulation.instance.VelocityLerp);
        this.velocity = vel0;

        // Transform
        Debug.DrawLine(this.transform.position, this.transform.position + velocity, Color.red);
        this.transform.LookAt(this.transform.position + velocity);
        this.transform.Translate(velocity * Time.deltaTime, Space.World);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 1);
        Gizmos.DrawWireSphere(this.centroid, BirdSimulation.instance.DetectRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1);
        Gizmos.DrawWireSphere(this.transform.position, BirdSimulation.instance.DetectRadius);
        Gizmos.DrawWireSphere(this.transform.position, BirdSimulation.instance.AvoidanceRadius);
    }

}