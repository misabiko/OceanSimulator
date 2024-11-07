using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Boid : MonoBehaviour
{
    public Vector3 velocity;
    public Vector3 centroid;

    private BirdSimulation Simulation => BirdSimulation.instance;

    // Start is called before the first frame update
    void Start()
    {
        velocity = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)).normalized * Random.Range(1, Simulation.MaxSpeed);
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
        Vector3 avoidance = Vector3.zero;

        var previousVelocity = this.velocity;
        var pos0 = this.transform.position;
        var vel0 = this.velocity;

        // Flocking
        int countInProximity = 0;
        int countInAvoidance = 0;
        foreach (var boid in Simulation.allBoids)
        {
            var b = boid.GetComponent<Boid>();
            if (b == this)
                continue;

            var pos1 = boid.transform.position;
            var deltaPos = pos1 - pos0;
            var dist = deltaPos.magnitude;
            if (dist <= Simulation.AvoidanceRadius)
            {
                countInAvoidance++;
                avoidance += -deltaPos.normalized * (Simulation.AvoidanceRadius - deltaPos.magnitude);
            }
            else
            if (dist <= Simulation.DetectRadius) // && Vector3.Dot(vel0.normalized, dist) > -0.5f)
            {
                countInProximity++;
                avgPos += pos1;
                avgVel += b.velocity;
            }
        }

        // Wander
        Vector3 wander = vel0 * (float) Math.Tan(2f * Math.PI / 12f);
        wander *= (float) Math.Sin(DateTime.Now.Ticks / TimeSpan.TicksPerSecond);
        vel0 += wander * Simulation.WanderWeight;


        Vector3 cohesion = Vector3.zero;
        Vector3 align = Vector3.zero;
        // Cohesion + Alignment
        if (countInProximity > 0)
        {
            avgPos /= countInProximity;
            avgVel /= countInProximity;
            centroid = avgPos;

            cohesion = (avgPos - pos0) * Simulation.Cohesion;
            align = (avgVel - vel0) * Simulation.Alignment;
            Debug.DrawLine(this.transform.position, this.transform.position + cohesion, Color.green);
            Debug.DrawLine(this.transform.position, this.transform.position + align, Color.blue);
            vel0 += cohesion + align;
        }
        // Avoidance
        if (countInAvoidance > 0)
        {
            //close_d /= countInAvoidance;
            vel0 += avoidance * Simulation.Separation;
        }

        // Goal
        vel0 += (Simulation.goalPos - this.transform.position).normalized * Simulation.GoalWeight;

        // Avoid Bounds
        var forwardDetection = this.transform.position + vel0.normalized * Simulation.DetectRadius;
        if(forwardDetection.magnitude > Simulation.SpaceBoundRadius)
        {
            var c = Vector3.Cross(forwardDetection, this.transform.position);
            //vel0 += c * Simulation.BoundAvoidanceWeight;
            vel0 += -this.transform.position.normalized * Simulation.BoundAvoidanceWeight;
        }

        // Clamp speed
        var speed = Math.Clamp(vel0.magnitude, 1, Simulation.MaxSpeed);
        vel0 = vel0.normalized * speed;
        vel0 = Vector3.Lerp(previousVelocity, vel0, Simulation.VelocityLerp);
        this.velocity = vel0;


        // Transform
        Debug.DrawLine(this.transform.position, this.transform.position + velocity, Color.red);
        this.transform.LookAt(this.transform.position + velocity);
        this.transform.Translate(velocity * Time.deltaTime, Space.World);
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 1);
        Gizmos.DrawWireSphere(this.centroid, Simulation.DetectRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1);
        Gizmos.DrawWireSphere(this.transform.position, Simulation.DetectRadius);
        Gizmos.DrawWireSphere(this.transform.position, Simulation.AvoidanceRadius);
    }

}