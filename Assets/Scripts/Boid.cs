using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class Boid : MonoBehaviour
{
    private SkinnedMeshRenderer mesh;
    private SphereCollider collider;

    public Vector3 velocity;
    public Vector3 centroid;

    public int id = 0;

    private BirdSimulation Simulation => BirdSimulation.instance;

    // Start is called before the first frame update
    void Start()
    {

        mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        collider = GetComponentInChildren<SphereCollider>();
        velocity = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)).normalized * Random.Range(1, Simulation.MaxSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        ApplyRulesBoids();
    }


    void ApplyRulesBoids()
    {
        if (Simulation == null) return; // because this runs in the editor, where the simulation isnt started yet
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
            if (dist <= Simulation.DetectRadius && Vector3.Dot(vel0.normalized, deltaPos) > 0.5f) // && countInProximity < Simulation.MaxCountInProximity)
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

            cohesion = (centroid - pos0) * Simulation.Cohesion;
            align = (avgVel - vel0) * Simulation.Alignment;
            //Debug.DrawLine(pos0, pos0 + cohesion, Color.green);
            //Debug.DrawLine(pos0, pos0 + align, Color.blue);
            vel0 += cohesion + align;
        }
        // Avoidance
        if (countInAvoidance > 0)
        {
            //close_d /= countInAvoidance;
            vel0 += avoidance * Simulation.Separation;
        }
        // Flocking size limitation
        if (countInProximity > Simulation.MaxCountInProximity)
        {
            vel0 -= (centroid - pos0) * Simulation.OvercrowdWeight; //  * (Simulation.MaxCountInProximity - countInProximity);
        }

        // Goal, if within a portion of the boids
        if(id < Simulation.boidCount * Simulation.GoalFollowersPercent)
            vel0 += (Simulation.goalPos - pos0).normalized * Simulation.GoalWeight;

        // Avoid Bounds
        var vectorToOrigin = (pos0 - Simulation.transform.position);
        var forwardDetection = vectorToOrigin + vel0.normalized * Simulation.DetectRadius;
        var boundDelta = forwardDetection.Abs() - (Simulation.SpaceBoundSizeRadius);
        if (boundDelta.x > 0)
        {
            vel0 += -boundDelta.x * Simulation.BoundAvoidanceWeight * vectorToOrigin.normalized.x * Vector3.right;
        }
        if (boundDelta.y > 0)
        {
            vel0 += -boundDelta.y * Simulation.BoundAvoidanceWeight * vectorToOrigin.normalized.y * Vector3.up;
        }
        if (boundDelta.z > 0)
        {
            vel0 += -boundDelta.z * Simulation.BoundAvoidanceWeight * vectorToOrigin.normalized.z * Vector3.forward;
        }

        // Avoid Obstacles
        LayerMask collisionLayer = LayerMask.GetMask("Obstacles");

        Collider[] hitColliders = Physics.OverlapSphere(pos0, Simulation.DetectRadius, collisionLayer);
        foreach (var hitCollider in hitColliders)
        {
            var p = hitCollider.ClosestPoint(pos0);
            var dpos = p - pos0;
            //vel0 += -dpos * Simulation.ObstacleAvoidanceWeight * (Simulation.DetectRadius - dpos.magnitude);
            //Debug.DrawLine(pos0, p, new Color(1, 0, 1, 1f), 1);


            Ray ray = new Ray(pos0, dpos.normalized);
            float rayLength = Simulation.DetectRadius;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, rayLength, collisionLayer))
            {

                // Get the normal of the surface at the hit point
                Vector3 hitNormal = hit.normal;

                //var dpos = hit.point - pos0;
                var obstacleAvoidanceVector = hitNormal * Simulation.ObstacleAvoidanceWeight * (rayLength - dpos.magnitude);
                vel0 += obstacleAvoidanceVector;

                // Draw the ray in green if it hits
                Debug.DrawLine(ray.origin, hit.point, new Color(1, 0, 1, 1f));
                //Debug.DrawLine(ray.origin, obstacleAvoidanceVector, Color.blue, 0.5f);
                // Draw the face normal
                Debug.DrawRay(hit.point, hitNormal, Color.green);
            }

        }
        
        var speed1 = Math.Clamp(vel0.magnitude, 1, Simulation.MaxSpeed);
        // Going up should be slower than going down
        //var dotup = Vector3.Dot(vel0, Vector3.down); // [1, -1] = [down, up]
        //dotup /= 4; // [0.25, -0.25]
        //dotup += 1f; // [1.25, 0.75] 
        //vel0 *= dotup;

        // Clamp speed
        var speed = Math.Clamp(vel0.magnitude, 1, Simulation.MaxSpeed);
        vel0 = vel0.normalized * speed;
        vel0 = Vector3.Lerp(previousVelocity, vel0, Simulation.VelocityLerp);
        this.velocity = vel0;

        // Transform
        //Debug.DrawLine(pos0, pos0 + velocity, Color.red);
        this.transform.LookAt(pos0 + velocity, Vector3.up);
        this.transform.Translate(velocity * Time.deltaTime, Space.World);
    }


    private void OnDrawGizmos()
    {
        if (Simulation == null) return; // because this runs in the editor, where the simulation isnt started yet
        //Gizmos.color = new Color(1, 0, 1, 0.1f);
        //Gizmos.DrawWireSphere(this.centroid, Simulation.DetectRadius);
    }

    private void OnDrawGizmosSelected()
    {
        if (Simulation == null) return; // because this runs in the editor, where the simulation isnt started yet
        Gizmos.color = new Color(0, 1, 1, 0.1f);
        Gizmos.DrawWireSphere(this.transform.position, Simulation.DetectRadius);
        Gizmos.DrawWireSphere(this.transform.position, Simulation.AvoidanceRadius);
        Debug.DrawLine(this.transform.position, this.transform.position + velocity.normalized, Color.red);
    }

}