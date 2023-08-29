using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using Collider = Unity.Physics.Collider;
using Unity.Collections.LowLevel.Unsafe;

public partial class SphereSpawningSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Enabled = false;
        return;

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var renderer = sphere.GetComponent<MeshRenderer>();
        var mesh = sphere.GetComponent<MeshFilter>().mesh;

        var renderMeshDescription = new RenderMeshDescription(renderer);
        var renderMeshArray = new RenderMeshArray(new[] { renderer.material }, new[] { mesh });
        var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);
        Object.DestroyImmediate(sphere);

        var entity = CreateDynamicSphere(EntityManager, renderMeshArray, materialMeshInfo, renderMeshDescription, 1, 0.5f, new float3(0, 10, 0), quaternion.identity);
        EntityManager.SetName(entity, "Sphere");

    }

    private Entity CreateDynamicSphere(EntityManager entityManager, RenderMeshArray renderMeshArray, MaterialMeshInfo materialMeshInfo, RenderMeshDescription renderMeshDescription, int uniformScale, float radius, float3 position, quaternion orientation)
    {
        SphereGeometry sphereGeometry = new SphereGeometry
        {
            Center = float3.zero,
            Radius = radius
        };

        // Sphere with default filter and material. Add to Create() call if you want non default:
        BlobAssetReference<Unity.Physics.Collider> sphereCollider = Unity.Physics.SphereCollider.Create(sphereGeometry, CollisionFilter.Default);
        return CreateBody(entityManager, renderMeshArray, materialMeshInfo, renderMeshDescription, position, orientation, uniformScale, sphereCollider, float3.zero, float3.zero, 1.0f, true);
    }
    
    private unsafe Entity CreateBody(EntityManager entityManager, RenderMeshArray renderMeshArray, MaterialMeshInfo materialMeshInfo, RenderMeshDescription renderMeshDescription, float3 position,
        quaternion orientation, float uniformScale, BlobAssetReference<Collider> collider, float3 linearVelocity,
        float3 angularVelocity, float mass, bool isDynamic)
    {
        ComponentType[] componentTypes = new ComponentType[isDynamic ? 8 : 4];

        componentTypes[0] = typeof(LocalTransform);
        componentTypes[1] = typeof(LocalToWorld);
        componentTypes[2] = typeof(PhysicsCollider);
        componentTypes[3] = typeof(PhysicsWorldIndex);
        if (isDynamic)
        {
            componentTypes[4] = typeof(PhysicsVelocity);
            componentTypes[5] = typeof(PhysicsMass);
            componentTypes[6] = typeof(PhysicsDamping);
            componentTypes[7] = typeof(PhysicsGravityFactor);
        }
        Entity entity = entityManager.CreateEntity(componentTypes);

        RenderMeshUtility.AddComponents(entity, entityManager, renderMeshDescription, renderMeshArray, materialMeshInfo);

        entityManager.SetComponentData(entity, new RenderBounds
        {
            Value = new AABB
            {
                Center = new float3(0, 0, 0),
                Extents = new float3(0.5f, 0.5f, 0.5f)
            }
        });

        entityManager.SetComponentData(entity, new LocalTransform
        {
            Position = position,
            Rotation = orientation,
            Scale = uniformScale
        });

        if (uniformScale != 1.0f)
        {
            entityManager.AddComponentData(entity, new PostTransformMatrix
            {
                Value = new float4x4
                {
                    c0 = new float4(1, 1, 1, 0) * uniformScale,
                    c1 = new float4(1, 1, 1, 0) * uniformScale,
                    c2 = new float4(1, 1, 1, 0) * uniformScale,
                    c3 = new float4(0, 0, 0, 1)
                }
            });
        }//Customed

        entityManager.SetComponentData(entity, new PhysicsCollider
        {
            Value = collider
        });

        //By default Physics simulation happens in PhysicsWorldIndex 0.
        EntityManager.AddSharedComponentManaged(entity, new PhysicsWorldIndex
        {
            Value = 0
        });

        if (!isDynamic) return entity;

        Collider* colliderPtr = (Collider*)collider.GetUnsafePtr();
        entityManager.SetComponentData(entity, PhysicsMass.CreateDynamic(colliderPtr->MassProperties, mass));
        /*entityManager.SetComponentData(entity, new PhysicsMass
        {
            Transform = new RigidTransform
            {
                pos = new float3(0, 0, 0),
                rot = quaternion.identity
            },
            InverseMass = 1,
            InverseInertia = new float3(10, 10, 10),
            AngularExpansionFactor = 0
        });*/

        // Calculate the angular velocity in local space from rotation and world angular velocity
        float3 angularVelocityLocal = math.mul(math.inverse(colliderPtr->MassProperties.MassDistribution.Transform.rot), angularVelocity);
        entityManager.SetComponentData(entity, new PhysicsVelocity()
        {
            Linear = linearVelocity,
            Angular = angularVelocityLocal
        });

        entityManager.SetComponentData(entity, new PhysicsDamping()
        {
            Linear = 0.01f,
            Angular = 0.05f
        });

        entityManager.SetComponentData(entity, new PhysicsGravityFactor
        {
            Value = 1
        });

        return entity;
    }
}