using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow;
using Random = UnityEngine.Random;

public class Boid : MonoBehaviour
{
    //float speed;
    const float Speed = 5f;
    public Vector3 velocity;

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
                close_d += -dist.normalized;
            }
            else
            if (d <= BirdSimulation.instance.DetectRadius && Vector3.Dot(vel0.normalized, dist) > -0.5f)
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

            var cohesion = (avgPos - pos0) * BirdSimulation.instance.Cohesion;
            var align = (avgVel - vel0) * BirdSimulation.instance.Alignment;
            vel0 += cohesion + align;
        }
        if (countInAvoidance > 0)
        {
            close_d /= countInAvoidance;
            vel0 += close_d * BirdSimulation.instance.Separation;
        }

        vel0 += wander * BirdSimulation.instance.WanderWeight;
        vel0 += (BirdSimulation.instance.goalPos - this.transform.position) * BirdSimulation.instance.GoalWeight;

        var speed = Math.Clamp(vel0.magnitude, 1, Speed);
        vel0 = vel0.normalized * speed;

        this.velocity = vel0;

        this.transform.LookAt(this.transform.position + velocity);
        this.transform.Translate(velocity * Time.deltaTime);
    }

}

