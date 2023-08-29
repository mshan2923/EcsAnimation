using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using UnityEditor;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Physics.Aspects;
using Collider = Unity.Physics.Collider;
using Unity.Jobs;
using Animation;

public partial class EcsSelectSphere : SystemBase
{
    RenderMeshDescription renderMeshDescription;
    RenderMeshArray renderMeshArray;
    MaterialMeshInfo materialMeshInfo;

    Entity SelectSphere;
    public bool IsSelecting = false;

    public float SelectRadius = 2;

    protected override void OnStartRunning()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var renderer = sphere.GetComponent<MeshRenderer>();
        var mesh = sphere.GetComponent<MeshFilter>().mesh;

        renderMeshDescription = new RenderMeshDescription(renderer);
        renderMeshArray = new RenderMeshArray(new[] { renderer.material }, new[] { mesh });
        materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);
        Object.DestroyImmediate(sphere);



        var temp = ECSAnimationDataTable.Instance;
    }
    protected override void OnUpdate()
    {
        //마우스 위치 > 월드 위치 변환 
        //Camera.main.ScreenPointToRay

        //Camera.Screen - 게임뷰 / Camera.currect - 에디터뷰


        if (Input.GetMouseButton(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

            //var hitResult = Physics.Raycast(ray, out var hit, 100f);

            PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            var tray = new RaycastInput()
            {
                Start = Camera.main.transform.position,//ray.origin,
                End = Camera.main.transform.position + ray.direction * 100f,//ray.direction * 100f,
                Filter = new CollisionFilter
                {
                    GroupIndex = 0,
                    BelongsTo = ~0u,//All Layer
                    CollidesWith = ~0u
                }
            };
            var Tresult = physicsWorld.CastRay(tray, out var Thit);
            //Debug.DrawLine(tray.Start, tray.End, Color.blue, 1f);

            if (Tresult)
            {
                if (!IsSelecting)
                {
                    if (SelectSphere == Entity.Null)
                    {
                        SelectSphere = CreateSphere(EntityManager, renderMeshArray, materialMeshInfo, renderMeshDescription,
                        1, SelectRadius, Thit.Position, Quaternion.identity);
                    }
                    else
                    {
                        EntityManager.SetEnabled(SelectSphere, true);
                    }

                    IsSelecting = true;
                }

                /*
                var ecb = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

                ecb.SetComponent(SelectSphere,//EntityManager.SetComponentData(SelectSphere, 
                    new LocalTransform 
                    { 
                        Position = Thit.Position,
                        Rotation = quaternion.identity,
                        Scale = 1
                    });*///-- SphereCastAll 에서 값 써야 하므로 오류

                EntityManager.SetComponentData(SelectSphere,
                                        new LocalTransform
                                        {
                                            Position = Thit.Position,
                                            Rotation = quaternion.identity,
                                            Scale = 1
                                        });

                
                var onHit = new NativeList<ColliderCastHit>(10, Allocator.TempJob);
                /*
                EntityManager.GetAspect<ColliderAspect>(SelectSphere).SphereCastAll(Thit.Position, 1f, new float3(0,-1,0), 0,
                    ref onHit, new CollisionFilter
                    {
                        GroupIndex = 0,
                        BelongsTo = ~0u,//All Layer
                        CollidesWith = ~0u
                    });//안되는데?
                Debug.Log(EntityManager.GetAspect<ColliderAspect>(SelectSphere).Collider.ToString());*/

                physicsWorld.SphereCastAll(Thit.Position, SelectRadius, float3.zero, 0, ref onHit, new CollisionFilter
                {
                    GroupIndex = 0,
                    BelongsTo = ~0u,//All Layer
                    CollidesWith = ~0u
                });//이건 작동함

                //onHit을 IJobParallel 으로


                /*
                if (onHit.Length > 0)
                    Debug.Log($"Length : {onHit.Length} /  First : {onHit[0].Position} <- {onHit[0].Entity}");
                else
                    Debug.Log("No Collision");
                */

                var SelectedPawn = new NativeList<Entity>(Allocator.TempJob);
                var FillteringSelectJob = new FillteringSelect()
                {
                    hits = onHit,
                    manager = EntityManager,
                    selected = SelectedPawn
                };
                var fillteringHandle = FillteringSelectJob.Schedule(onHit.Length, Dependency);
                fillteringHandle.Complete();

                //Debug.Log("Select Pawn" + ((SelectedPawn.Length > 0) ? SelectedPawn[0] : "Empty"));

                {
                    for (int i = 0; i < SelectedPawn.Length; i++)
                    {
                        if (EntityManager.HasComponent<AnimationControllerData>(SelectedPawn[i]))
                        {
                            if (EntityManager.IsComponentEnabled<AnimationControllerData>(SelectedPawn[i]))
                            {
                                EntityManager.SetComponentEnabled<AnimationControllerData>(SelectedPawn[i], false);

                                Debug.Log($"{i} : {SelectedPawn[i]}  | Pos : {EntityManager.GetComponentData<LocalTransform>(SelectedPawn[i]).Position}");
                            }
                            else
                            {
                                //Remove
                                SelectedPawn[i] = Entity.Null;
                            }
                        }
                        else
                        {
                            if (EntityManager.HasComponent<PhysicsVelocity>(EntityManager.GetBuffer<HumanoidParts>(SelectedPawn[i])[0].entity))
                            {
                                //Remove / 자식 엔티티가 PhysicsVelocity 소유하는지 확인
                                SelectedPawn[i] = Entity.Null;
                            }
                            else
                            {

                            }

                        }
                    }//중복적용 방지

                    var ecb = new EntityCommandBuffer(Allocator.TempJob);
                    //var colliders = new NativeList<PhysicsCollider>(Allocator.TempJob);

                    var DisAnimController = new DisableAnimController() 
                    {
                        ecb = ecb.AsParallelWriter() ,
                        selectPawn = SelectedPawn.AsReadOnly(),
                    };
                    var DisAnimControllerHandle = DisAnimController.ScheduleParallel(fillteringHandle);//==== IJobFor으로 할수 없거나 , 제거용 태그 달거나
                    DisAnimControllerHandle.Complete();

                    ecb.Playback(EntityManager);//hip 이하 부위 collider 추가
                    ecb.Dispose();

                    if (SelectedPawn.Length > 0)
                    {
                        //var query = Entities.WithSharedComponentFilter(EntityManager.GetSharedComponent<IsHumanoidPart>(SelectedPawn[0])).ToQuery();
                        //Debug.Log($"Pawn has Entity : {query.CalculateEntityCount()}");
                        var ecb2 = new EntityCommandBuffer(Allocator.TempJob);

                        foreach (var v in SelectedPawn)
                        {
                            if (v != Entity.Null)
                            {
                                var query = Entities.WithSharedComponentFilter(EntityManager.GetSharedComponent<IsHumanoidPart>(v))
                                            .WithAll<PhysicsCollider>().ToQuery();


                                //var partQuery = GetEntityQuery(typeof(Animation.HumanoidPartIndex), typeof(PhysicsCollider));
                                var AddRigidJob = new SwitchRigidAspect.AddRigidJob()
                                {
                                    ecb = ecb2.AsParallelWriter(),
                                    collders = query.ToComponentDataArray<PhysicsCollider>(Allocator.TempJob),
                                    mass = 0.2f,
                                    GravityFactor = 9.8f,
                                    phsicsWorldIndex = 0
                                };
                                var AddRigidHandle = AddRigidJob.ScheduleParallel(query, DisAnimControllerHandle);
                                AddRigidHandle.Complete();

                                //Debug.Log("ragdoll " + v);
                            }
                        }

                        ecb2.Playback(EntityManager);//Rigid 추가//OverFlow!!!
                        ecb2.Dispose();
                    }

                    SelectedPawn.Dispose();
                    //NativeArray<PhysicsCollider> 필요 , DisAnimController 의 값 필요 / 엔티티리스트는 부위의 엔티티로





                }

            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (SelectSphere == Entity.Null)
            {

            }
            else
            {
                EntityManager.SetEnabled(SelectSphere, false);
            }
            IsSelecting = false;
        }
    }

    Entity CreateSphere(EntityManager entityManager, RenderMeshArray renderMeshArray, MaterialMeshInfo materialMeshInfo, RenderMeshDescription renderMeshDescription,
        int uniformScale, float radius, float3 position, quaternion orientation)
    {
        SphereGeometry sphereGeometry = new SphereGeometry
        {
            Center = float3.zero,
                        Radius = radius
        };
        // Sphere with default filter and material. Add to Create() call if you want non default:
        BlobAssetReference<Unity.Physics.Collider> sphereCollider 
            = Unity.Physics.SphereCollider.Create(sphereGeometry, CollisionFilter.Default);
        //sphereCollider.Value.MassProperties 이거 쓰면 되지 않을까? / (Collider*)collider.GetUnsafePtr(); 대신에?


        return CreateBody(entityManager, renderMeshArray, materialMeshInfo, renderMeshDescription,
            position, orientation, uniformScale, sphereCollider, float3.zero , float3.zero, 1.0f, false);
    }
    Entity CreateBody(EntityManager entityManager, RenderMeshArray renderMeshArray, MaterialMeshInfo materialMeshInfo, RenderMeshDescription renderMeshDescription, float3 position,
        quaternion orientation, float uniformScale, BlobAssetReference<Collider> collider, float3 linearVelocity,
        float3 angularVelocity, float mass, bool isDynamic)
    {


        var archeType =
            isDynamic ?
            entityManager.CreateArchetype
            (
            typeof(LocalTransform), typeof(LocalToWorld),
            typeof(PhysicsCollider), typeof(PhysicsWorldIndex),
            typeof(PhysicsVelocity), typeof(PhysicsMass),
            typeof(PhysicsDamping), typeof(PhysicsGravityFactor)
            ) :
            entityManager.CreateArchetype
            (
                typeof(LocalTransform), typeof(LocalToWorld),
                typeof(PhysicsCollider), typeof(PhysicsWorldIndex)
            );


        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var Spawned = new NativeArray<Entity>(1, Allocator.TempJob);

        var e = EntityManager.CreateEntity(archeType);

        RenderMeshUtility.AddComponents(e, EntityManager, renderMeshDescription, renderMeshArray, materialMeshInfo);

        var AddRigidJob = new AddRigidComponent
        {
            ecb = ecb,
            archetype = archeType,
            Spawned = e,

            position = position,
            rotation = orientation,
            uniformScale = uniformScale,

            collider = collider,
            linearVelocity = linearVelocity,
            angularVelocity = angularVelocity,
            Mass = mass,

            isDynamic = false,
            IsTrigger = true
        };

        var AddRigidHandle = AddRigidJob.Schedule(Dependency);
        AddRigidHandle.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();

        Debug.Log(e);

        return e;

        // ECB를 스폰후 , 컴포넌트 추가/설정 해야함

    }

    public partial struct AddRigidComponent : IJob
    {
        public EntityCommandBuffer ecb;
        public EntityArchetype archetype;
        public Entity Spawned;

        public float3 position;
        public quaternion rotation;

        public float uniformScale;
        public BlobAssetReference<Collider> collider;
        public bool isDynamic;

        public float3 linearVelocity;
        public float3 angularVelocity;
        public float Mass;
        public bool IsTrigger;

        public void Execute()
        {

            ecb.AddComponent(Spawned, new RenderBounds
            {
                Value = new AABB
                {
                    Center = new float3(0, 0, 0),
                    Extents = new float3(0.5f, 0.5f, 0.5f)// * uniformScale                    
                }
            });

            ecb.SetComponent(Spawned, new LocalTransform
            {
                Position = position,
                Rotation = rotation,
                Scale = uniformScale
            });

            if (Mathf.Approximately(uniformScale, 1) == false)
            {
                ecb.AddComponent(Spawned, new PostTransformMatrix
                {
                    Value = new float4x4
                    {
                        c0 = new float4(1, 1, 1, 0) * uniformScale,
                        c1 = new float4(1, 1, 1, 0) * uniformScale,
                        c2 = new float4(1, 1, 1, 0) * uniformScale,
                        c3 = new float4(0, 0, 0, 1)
                    }
                });
            }

            if (!IsTrigger)
            {
                ecb.SetComponent(Spawned, new PhysicsCollider
                {
                    Value = collider
                });
            }

            /*
            if (isDynamic) return;
            
            ecb.SetComponent(e, PhysicsMass.CreateDynamic(collider.Value.MassProperties, Mass));//======

            ecb.SetComponent(e, new PhysicsVelocity()
            {
                Linear = linearVelocity,
                Angular = math.mul(math.inverse(collider.Value.MassProperties.MassDistribution.Transform.rot), angularVelocity)
            });

            ecb.SetComponent(e, new PhysicsDamping()
            {
                Linear = 0.01f,
                Angular = 0.05f
            });

            ecb.SetComponent(e, new PhysicsGravityFactor
            {
                Value = 1
            });*///------ 여기서 문제 발생
        }
    }
    public partial struct AddRigidComponent_Parallel : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public EntityArchetype archetype;
        [WriteOnly] public NativeArray<Entity> Spawned;

        public float3 position;
        public quaternion rotation;
        public float Scale;

        public float uniformScale;
        public BlobAssetReference<Collider> collider;
        public bool isDynamic;

        public float3 linearVelocity;
        public float3 angularVelocity;

        public void Execute([EntityIndexInQuery] int i)
        {
            var e = ecb.CreateEntity(i, archetype);
            Spawned[i] = e;

            ecb.AddComponent(i, e, new RenderBounds
            {
                Value = new AABB
                {
                    Center = new float3(0, 0, 0),
                    Extents = new float3(0.5f, 0.5f, 0.5f)// * uniformScale                    
                }
            });

            ecb.SetComponent(i, e, new LocalTransform 
            { Position = position, 
              Rotation = rotation, 
              Scale = Scale
            });

            if (Mathf.Approximately(uniformScale, 1) == false)
            {
                ecb.AddComponent(i, e, new PostTransformMatrix
                {
                    Value = new float4x4
                    {
                        c0 = new float4(1, 1, 1, 0) * uniformScale,
                        c1 = new float4(1, 1, 1, 0) * uniformScale,
                        c2 = new float4(1, 1, 1, 0) * uniformScale,
                        c3 = new float4(0, 0, 0, 1)
                    }
                });
            }

            ecb.SetComponent(i, e, new PhysicsCollider
            {
                Value = collider
            });

            if (isDynamic) return;

            ecb.SetComponent(i, e, PhysicsMass.CreateDynamic(collider.Value.MassProperties, 1));//======

            ecb.SetComponent(i, e, new PhysicsVelocity()
            {
                Linear = linearVelocity,
                Angular = math.mul(math.inverse(collider.Value.MassProperties.MassDistribution.Transform.rot), angularVelocity)
            });

            ecb.SetComponent(i, e, new PhysicsDamping()
            {
                Linear = 0.01f,
                Angular = 0.05f
            });

            ecb.SetComponent(i, e, new PhysicsGravityFactor
            {
                Value = 1
            });
        }
    }//갯수를 주고 병렬 실행

    public struct FillteringSelect : IJobFor
    {
        public NativeList<ColliderCastHit> hits;
        public EntityManager manager;

        [WriteOnly] public NativeList<Entity> selected;
        public void Execute(int i)
        {
            if (manager.HasComponent<Animation.HumanoidPartIndex>(hits[i].Entity))
            {
                if (manager.HasComponent<PostTransformMatrix>(hits[i].Entity))
                {
                    selected.Add(manager.GetSharedComponent<Animation.IsHumanoidPart>(hits[i].Entity).parent);
                    //Debug.Log($"{hits[i].Entity} => {manager.GetSharedComponent<Animation.IsHumanoidPart>(hits[i].Entity).parent}");
                }
            }
        }
    }
    public partial struct DisableAnimController : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public NativeArray<Entity>.ReadOnly selectPawn;

        public void Execute(Entity entity, [EntityIndexInQuery] int i, in Animation.HumanoidPartIndex part,
            in LocalToWorld L2W, in Parent parent, in LocalTransform transform, in Animation.IsHumanoidPart pawn)
        {
            if (!selectPawn.Contains(pawn.parent))
                return;

            //colliders.AddNoResize();
            var pos = L2W.Position;
            //parent.Value = Entity.Null;//-- 이걸론 안됨
            ecb.SetComponent<Parent>(i, entity, new Parent());//==== 값 변경도 안되고 , 삭제도 안되고
            //ecb.SetComponent<PreviousParent>(i, entity, new PreviousParent());

            //transform.Position = pos;
            ecb.SetComponent(i, entity, new LocalTransform()
            {
                Position = L2W.Position,
                Rotation = L2W.Rotation,
                Scale = L2W.Value.Scale().x
            });

            Unity.Physics.BoxGeometry box = new Unity.Physics.BoxGeometry()
            {
                Center = float3.zero,
                Size = new float3(1, 1, 1),
                Orientation = Quaternion.identity
            };
            var collide = Unity.Physics.BoxCollider.Create(box);
            ecb.AddComponent<PhysicsCollider>(i, entity, new PhysicsCollider() { Value = collide });

            //Debug.Log(part.index + " >> " + transform.Position + " | " + L2W.Position);
        }
    }//======== 임시로 크기가 1인 BoxCollider 으로
    public partial struct AddRigidJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        public NativeArray<PhysicsCollider> collders;
        public NativeArray<Entity> pawn;

        public float3 LinearVelocity;
        public float3 AngularVelocity;
        public uint phsicsWorldIndex;
        public float mass;
        public byte isKinematic;
        public float LinearDamping;
        public float AngularDamping;
        public float GravityFactor;

        public void Execute(int i)//(Entity entity, [EntityIndexInQuery] int i)
        {
            ecb.AddComponent(i, pawn[i], new PhysicsVelocity() { Linear = LinearVelocity, Angular = AngularVelocity });
            ecb.AddSharedComponent(i, pawn[i], new PhysicsWorldIndex() { Value = phsicsWorldIndex });
            ecb.AddComponent(i, pawn[i], PhysicsMass.CreateDynamic(collders[i].MassProperties, mass));

            ecb.AddComponent(i, pawn[i], new PhysicsMassOverride() { IsKinematic = isKinematic });
            ecb.AddComponent(i, pawn[i], new PhysicsDamping() { Linear = LinearDamping, Angular = AngularDamping });
            ecb.AddComponent(i, pawn[i], new PhysicsGravityFactor() { Value = GravityFactor });
        }
    }//IJobFor으로 바꾸기
}
