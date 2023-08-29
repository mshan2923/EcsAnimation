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
        //HumanoidPart[] array; / managed fields 사용 불가
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
                        //var result = state.EntityManager.AddComponent<HasRagdoll>(array[i].PartEntity);//===== 작동X
                        //ecb.AddComponent<HasRagdoll>(array[i].PartEntity);
                        //Debug.Log($"Added : / {array[i].PartEntity} / is Exist {state.EntityManager.IsEnabled(array[i].PartEntity)}");
                        //state.EntityManager.IsEnabled

                        //---------------------------------- 존재하고 , 활성화가 되어있지만 쿼리에 없음....? / Baker 와 엔티티 배치되는 월드가 다름
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

                //sHumanoid_Test 가 엔티티 아니여서 자식갯수를 못세는데? , 캡슐은 되고... / 갑자기 세는걸 포기 했는데?

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

            //이때는 버퍼가 비어있음
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
            //UpdateAfter() 없을땐 첫 업데이트땐 비어 있는데?
        }
    }

    public partial struct HumanoidDebug : IJobEntity
    {
        public EntityManager manager;
        public void Execute(Entity entity,  in HasRagdoll tag)
        {
            var buffer = manager.GetBuffer<HumanoidParts>(entity);
            //var buffer = SystemAPI.GetBuffer<HumanoidParts>(entity);//SystemAPI 사용불가
            
            for (int i = 0; i < buffer.Length; i++)
            {
                Debug.Log($"part : {buffer[i].part} / entity : {buffer[i].entity}");
            }
        }
    }
    
}