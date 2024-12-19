using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.LightTransport;
using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.InputSystem.HID.HID;


    
namespace Assets.Scripts.Boids
{

    public class BoidSimulationECS : MonoBehaviour
    {
        [SerializeField] public Mesh mesh;               // Mesh to render
        [SerializeField] public Material material;       // Material with the custom shader
        [SerializeField] public int instanceCount = 100; // Number of instances

        private EntityManager entityManager;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Create a single entity with the RenderMeshInstanced component
            EntityArchetype archetype = entityManager.CreateArchetype(
                typeof(LocalToWorld),
                typeof(RenderMesh),
                typeof(RenderBounds),
                typeof(PerInstanceCullingTag),
                typeof(AnimationTime),
                typeof(Position),
                typeof(Velocity)
            );

            // Create instances
            for (int i = 0; i < instanceCount; i++)
            {
                Entity entity = entityManager.CreateEntity(archetype);

                // Set the mesh and material
                //entityManager.SetSharedComponentData(entity, new RenderMesh
                //{
                //    mesh = mesh,
                //    material = material
                //});
                entityManager.SetSharedComponentManaged(entity, new RenderMesh
                {
                    mesh = mesh,
                    material = material
                });

                // Set the transform
                float3 position = new float3(
                    UnityEngine.Random.Range(-10f, 10f),
                    UnityEngine.Random.Range(-10f, 10f),
                    UnityEngine.Random.Range(-10f, 10f)
                );

                quaternion rotation = quaternion.EulerXYZ(
                    UnityEngine.Random.Range(0f, math.PI * 2f),
                    UnityEngine.Random.Range(0f, math.PI * 2f),
                    UnityEngine.Random.Range(0f, math.PI * 2f)
                );

                float3 scale = new float3(1f);

                entityManager.SetComponentData(entity, new LocalToWorld
                {
                    Value = float4x4.TRS(position, rotation, scale)
                });

                // Transform
                var transform = entityManager.GetComponentData<LocalToWorld>(entity);
                var pos0 = entityManager.GetComponentData<Position>(entity);
                var vel = entityManager.GetComponentData<Velocity>(entity);

                float4x4.LookAt(pos0, pos0 + vel, Vector3.up);
                transform.LookAt(pos0 + velocity, Vector3.up);
                transform.Translate(velocity * Time.deltaTime, Space.World);
                //Debug.DrawLine(pos0, pos0 + velocity, Color.red);

                // Set the custom float property
                float customFloat = UnityEngine.Random.Range(0.5f, 2f);
                var time = entityManager.GetComponentData<AnimationTime>(entity);
                time.Value = customFloat;
                entityManager.SetComponentData(entity, new AnimationTime { Value = customFloat });
            }
        }
    }
}