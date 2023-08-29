using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Burst.Intrinsics;

namespace Animation
{
    /*
    [UpdateInGroup(typeof(InitializationSystemGroup))]//SimulationSystemGroup , PresentationSystemGroup
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    public partial struct HumanoidTestCommunicater : ISystem
    {
        //HumanoidPart[] array; / managed fields ��� �Ұ�
        Entity SpawnedEntity;
        public void OnCreate(ref SystemState state)
        {

        }
        public void OnDestroy(ref SystemState state)
        {

        }

        public void OnUpdate(ref SystemState state)
        {
            
            if (SystemAPI.HasSingleton<HumanoidStructureComponent>())
            {
                var humanoid = SystemAPI.GetSingleton<HumanoidStructureComponent>();
                var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

                //ref var array = ref humanoid.HumanoidParts.Value.Array;

                //array = humanoid.HumanoidParts.Value.Array.ToArray();
                //Debug.Log("Target Amount : " + array.Length);

                //for (int i = 0; i < array.Length; i++)
                {
                    //if (array[i].PartEntity != Entity.Null)
                    {
                        //var result = state.EntityManager.AddComponent<HasRagdoll>(array[i].PartEntity);//===== �۵�X
                        //ecb.AddComponent<HasRagdoll>(array[i].PartEntity);
                        //Debug.Log($"Added : / {array[i].PartEntity} / is Exist {state.EntityManager.IsEnabled(array[i].PartEntity)}");
                        //state.EntityManager.IsEnabled

                        //---------------------------------- �����ϰ� , Ȱ��ȭ�� �Ǿ������� ������ ����....? / Baker �� ��ƼƼ ��ġ�Ǵ� ���尡 �ٸ�
                    }
                }
                //state.RequireForUpdate<HasRagdoll>();
            }


            var query = state.GetEntityQuery(typeof(HasRagdoll));
            Debug.Log("Query : " + query.CalculateEntityCount() + " / " + state.EntityManager.GetAllEntities().Length);
            
            foreach(var t in state.EntityManager.GetAllEntities())
            {
                int child = -1;
                if (state.EntityManager.HasBuffer<Unity.Transforms.Child>(t))//Unity.Transforms.Child
                {
                    child = state.EntityManager.GetBuffer<Unity.Transforms.Child>(t, true).Length;//LinkedEntityGroup
                }

                if (state.EntityManager.HasComponent<Unity.Transforms.Parent>(t))
                {
                    Debug.Log($"Parent : {state.EntityManager.GetComponentData<Unity.Transforms.Parent>(t).Value}" +
                        $" / This : {t}");
                }
                else
                {
                    //Debug.Log(t + " : Not Has Parent");
                }
                // Unity.Transforms.Parent not work

                //Debug.Log($" {t.Index} / {t.ToString()}  / HasRagdoll : {state.EntityManager.HasComponent<HasRagdoll>(t)} / Exist : {state.EntityManager.Exists(t)}" +
                //    $"\n Has Child : {child}");

                //sHumanoid_Test �� ��ƼƼ �ƴϿ��� �ڽİ����� �����µ�? , ĸ���� �ǰ�... / ���ڱ� ���°� ���� �ߴµ�?

                var childFormEntity = state.GetBufferLookup<Unity.Transforms.Child>(true);
                if (state.EntityManager.HasBuffer<Unity.Transforms.Child>(t))
                {
                    var test = childFormEntity[t];
                    Debug.Log($"{t} --> {test.Length}");
                }else
                {
                    Debug.Log($"{t} Buffer Has : {childFormEntity.HasBuffer(t)}");
                }
            }

        }
    }
    */
    
    //[UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BakingSystemGroup))]
    public partial class HumanoidTestCommunicater_SystemBase : SystemBase
    {
        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            //�̶��� ���۰� �������
        }
        protected override void OnUpdate()
        {
            //SystemAPI.GetSingletonBuffer<HumanoidParts>(true)


            var job = new HumanoidDebug
            {
                manager = EntityManager
            };
            //var handle = job.Schedule(Dependency);
            //handle.Complete();

            Enabled = false;
            //UpdateAfter() ������ ù ������Ʈ�� ��� �ִµ�?
        }
    }

    public partial struct HumanoidDebug : IJobEntity
    {
        public EntityManager manager;
        public void Execute(Entity entity,  in HasRagdoll tag)
        {
            var buffer = manager.GetBuffer<HumanoidParts>(entity);
            //var buffer = SystemAPI.GetBuffer<HumanoidParts>(entity);//SystemAPI ���Ұ�
            
            for (int i = 0; i < buffer.Length; i++)
            {
                Debug.Log($"part : {buffer[i].part} / entity : {buffer[i].entity}");
            }
        }
    }
    
}