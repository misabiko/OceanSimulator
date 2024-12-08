using System;
using System.Collections.Generic;
using UnityEngine;

public class BirdSimulation : MonoBehaviour
{
    public static BirdSimulation instance;
    public GameObject boidPrefab;
    public int boidCount = 20;
    public GameObject[] allBoids;
    public Vector3 goalPos = new Vector3(0, 0, 0);

    [Header("Boid settings")]
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
    public float ObstacleAvoidanceWeight;
    [Range(0f, 100.0f)]
    public float OvercrowdWeight;
    [Range(0f, 100.0f)]
    public float MaxSpeed = 5f;
    [Range(10f, 100.0f)]
    public float SpaceBoundRadius;
    [Range(0f, 100.0f)]
    public float BoundAvoidanceWeight;
    [Range(0f, 100.0f)] 
    public float VelocityLerp;

    public void bake()
    {
        SkinnedMeshRenderer smr = new();
        var bones = smr.bones;
        var weights = new System.Collections.Generic.List<BoneWeight>();
        smr.sharedMesh.GetBoneWeights(weights);
        //smr.bones[0].


        Animator animator = new();
        animator.Play("idle");



        int iLayer = 0;
        //float fNormalizedTime = .5f;
        //Get Current State
        AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(iLayer);
        var info = animator.GetCurrentAnimatorClipInfo(iLayer);

        float length = info[0].clip.length;
        float framerate = 15f;
        int frameCount = (int) Math.Ceiling(length * framerate);
        float adjustedTimePerFrame = length / (frameCount - 1);
        float adjustedFramerate = 1f / adjustedTimePerFrame;

        List<float> pixels = new();
        float currenTime = 0;
        for (int i = 0; i < frameCount; i++)
        {
            //Set Normalized Time
            //animator.Play(aniStateInfo.shortNameHash, iLayer, fNormalizedTime);
            animator.PlayInFixedTime(aniStateInfo.shortNameHash, iLayer, currenTime);
            //Force Update
            animator.Update(0f);
            currenTime += adjustedTimePerFrame;
            foreach(var bone in smr.bones)
            {
                PushBone(pixels, bone);
            }
        }

        smr.sharedMesh.GetBonesPerVertex();
    }

    private void PushBone(List<float> pixels, Transform bone)
    {
        var a = bone.localToWorldMatrix;
        //var b = bone.worldToLocalMatrix;
        // row
        for(int i = 0; i < 4; i++)
        {
            // column
            for(int j = 0; j < 4; j++)
            {
                pixels.Add(a[i, j]);
                pixels.Add(a[i, j]);
                pixels.Add(a[i, j]);
                pixels.Add(a[i, j]);
            }
        }
    }


    // Start is called before the first frame update
    private void Start()
    {
        allBoids = new GameObject[boidCount];
        for (var i = 0; i < boidCount; i++)
        {
            Vector3 pos = this.transform.position + UnityEngine.Random.insideUnitSphere * SpaceBoundRadius;
            allBoids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
        }

        instance = this;
        goalPos = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        if (UnityEngine.Random.Range(0, 100) < 1)
        {
            goalPos = Vector3.Lerp(goalPos, this.transform.position + UnityEngine.Random.insideUnitSphere * SpaceBoundRadius, 0.2f);
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