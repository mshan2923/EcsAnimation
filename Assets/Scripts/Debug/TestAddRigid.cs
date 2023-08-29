using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using UnityEditor;
using Unity.Mathematics;
using Unity.Transforms;

public class TestAddRigid : MonoBehaviour
{
    //[TextArea()]
    public bool toggle = false;
    public static bool AddRigid = false;

    private void OnValidate()
    {
        AddRigid = toggle;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

}

public struct Tag_TestAddRigid : IComponentData {}
public class TestAddRigidBake : Baker<TestAddRigid>
{
    public override void Bake(TestAddRigid authoring)
    {
        AddComponent(new Tag_TestAddRigid());
    }
}
[UpdateAfter(typeof(EcsAnimationSystem))]
public partial class TestAddRigidSystem : SystemBase
{
    protected override void OnUpdate()
    {
        //=====> 먼저 분리 시키고 , 리기드바디 적용 시켜야 되는데 흠.. 리기드 바디 실행되며 해...결

        //===== PhysicsCollider 를 임시로 크기 1인 box으로 함 / Parent를 없애거나 , 위치 변경은 영향이 있나?
        //===== 내가 원하는 타이밍에 실행이...  
        //  ECS Animation Controller 키면 크기 초기화


        return;

        Entities.WithoutBurst().ForEach((Entity e, in Animation.AnimationControllerData data) =>
        {
            EntityManager.SetComponentEnabled(e, typeof(Animation.AnimationControllerData), false);
        }).Run();//=== 귀찮아서... Job.Schedle()으로 바꾸기

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var DisAnimController = new DisableAnimController() { ecb = ecb.AsParallelWriter() };
        var DisAnimControllerHandle = DisAnimController.ScheduleParallel(Dependency);
        DisAnimControllerHandle.Complete();

        ecb.Playback(EntityManager);//hip 이하 부위 collider 추가
        ecb.Dispose();

        var ecb2 = new EntityCommandBuffer(Allocator.TempJob);

        var partQuery = GetEntityQuery(typeof(Animation.HumanoidPartIndex), typeof(PhysicsCollider));
        var AddRigidJob = new SwitchRigidAspect.AddRigidJob()        
        {
            ecb = ecb2.AsParallelWriter(),
            collders = partQuery.ToComponentDataArray<PhysicsCollider>(Allocator.TempJob),
            mass = 0.2f,
            GravityFactor = 9.8f,
            phsicsWorldIndex = 0
        };
        var AddRigidHandle = AddRigidJob.ScheduleParallel(partQuery, DisAnimControllerHandle);
        AddRigidHandle.Complete();

        ecb2.Playback(EntityManager);//Rigid 추가
        ecb2.Dispose();

        this.Enabled = false;
        return;
        if (TestAddRigid.AddRigid)
        {
            {
                /*
                //var query = GetEntityQuery(typeof(Tag_TestAddRigid));

                var query = Entities.WithAll<Tag_TestAddRigid>()
                    .WithNone<PhysicsVelocity>().ToQuery();

                var tagEntity = query.ToEntityArray(Allocator.TempJob);
                var ecb = new EntityCommandBuffer(Allocator.TempJob);

                foreach (var e in tagEntity)
                {


                    SphereGeometry sphereGeometry = new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = 1f
                    };

                    // Sphere with default filter and material. Add to Create() call if you want non default:
                    //BlobAssetReference<Unity.Physics.Collider> sphereCollider = Unity.Physics.SphereCollider.Create(sphereGeometry, CollisionFilter.Default);

                    var collider = EntityManager.GetComponentData<Unity.Physics.PhysicsCollider>(e);

                    ecb.AddComponent(e, new PhysicsVelocity() { });
                    //ecb.AddComponent(e, new PhysicsCollider() { Value = sphereCollider});//기존것이 존재 하므로

                    ecb.AddSharedComponent(e, new PhysicsWorldIndex() { Value = 0 });

                    //ecb.AddComponent(e, PhysicsMass.CreateDynamic(sphereCollider.Value.MassProperties, 1f));
                    ecb.AddComponent(e, PhysicsMass.CreateDynamic(collider.MassProperties, 1f));

                    ecb.AddComponent(e, new PhysicsMassOverride() { IsKinematic = 0 });
                    ecb.AddComponent(e, new PhysicsDamping() { Linear = 0.01f, Angular = 0.05f });
                    ecb.AddComponent(e, new PhysicsGravityFactor() { Value = 1 });

                }

                ecb.Playback(EntityManager);
                ecb.Dispose();
                */
            }//테스트용 - Tag_TestAddRigid 가지고 있는 엔티티 리기드바디 적용

            /*
            //AnimationControllerData 비활성화 , EcsAnimationSystem.GetAnimator() 실행
            Entities.WithoutBurst().ForEach((Entity e, in Animation.AnimationControllerData data) =>
            {
                EntityManager.SetComponentEnabled(e, typeof(Animation.AnimationControllerData) ,false);
            }).Run();

            EntityManager.World.GetExistingSystemManaged<EcsAnimationSystem>().GetAnimator();

            var partQuery = GetEntityQuery(typeof(Animation.HumanoidPartIndex), typeof(PhysicsCollider));
            //var result = SwitchRigidAspect.AddRigid(partQuery, EntityManager, Dependency, float3.zero, float3.zero);
            //Debug.Log("Switch is : " + result);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var AddRigidJob = new SwitchRigidAspect.AddRigidJob()
            {
                ecb = ecb.AsParallelWriter(),
                collders = partQuery.ToComponentDataArray<PhysicsCollider>(Allocator.TempJob),
                mass = 0.2f,
                GravityFactor = 9.8f,
                phsicsWorldIndex = 0
            };
            var AddRigidHandle = AddRigidJob.ScheduleParallel(partQuery, Dependency);
            AddRigidHandle.Complete();


            var DisAnimController = new DisableAnimController() { };
            var DisAnimControllerHandle = DisAnimController.ScheduleParallel(AddRigidHandle);
            DisAnimControllerHandle.Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();
            */

            /// ========= 흠... 연결되서... 그런건가? 되긴되는데 흠... 
            /// ++ 충돌이 좀 맛이감
            ///     => hip 말고 PhysicsCollider이 없음!
            ///     
            //========DisAnimController 하면서  부모를 지울때 로컬값이 월드위치로 변함
            // ========== 대체 크기는 왜커짐?-----> EcsAnimationSystem 이전에 실행되서 
            // 보이는 위치롸 리기드 바디 위치가 다른가?
        }
    }

    public partial struct DisableAnimController : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public void Execute(Entity entity, [EntityIndexInQuery]int i, in Animation.HumanoidPartIndex part,
            in LocalToWorld L2W, in Parent parent, in LocalTransform transform)
        {
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
                Size = new float3(1,1,1),
                Orientation = Quaternion.identity
            };
            var collide = Unity.Physics.BoxCollider.Create(box);
            ecb.AddComponent<PhysicsCollider>(i, entity, new PhysicsCollider() { Value = collide });

            //Debug.Log(part.index + " >> " + transform.Position + " | " + L2W.Position);
        }
    }
}
