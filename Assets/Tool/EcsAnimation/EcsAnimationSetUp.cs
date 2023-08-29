using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Animation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Transforms;
using Unity.Physics;

[UpdateAfter(typeof(BakingSystem))]
public partial class EcsAnimationSetUp : SystemBase
{
    protected override void OnStartRunning()
    {
        //==== 문제 근원 , 스포너로 이후에 스폰된것은 스포너에서 여기로 접근해서

        base.OnStartRunning();

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var taggingJob = new HumanoidTagging()
        {
            ecb = ecb.AsParallelWriter()
        };
        var taggingHandle = taggingJob.ScheduleParallel(Dependency);
        taggingHandle.Complete();

        //DynamicBuffer<PhysicsColliderKeyEntityPair> , 

        ecb.Playback(World.EntityManager);
        ecb.Dispose();


        {
            ecb = new EntityCommandBuffer(Allocator.TempJob);

            var controller = GetEntityQuery(typeof(AnimationControllerData), typeof(LocalTransform));
            var resetPostTransJob = new ResetPostTransform()
            {
                ecb = ecb.AsParallelWriter(),
                controllerEntity = controller.ToEntityArray(Allocator.TempJob),
                controllerTrans = controller.ToComponentDataArray<LocalTransform>(Allocator.TempJob)
            };
            //var resetPostTransHandle = resetPostTransJob.ScheduleParallel(taggingHandle);
            //resetPostTransHandle.Complete();

            ecb.Playback(World.EntityManager);
            ecb.Dispose();
        }

        SaveLoad.CryptionLoad<List<Animation.AnimationInfo>>("Data", "ECS_AnimationData_Cryption", "data", out var data);
        ECSAnimationDataTable.Instance.AnimInfo = data;

        var setDefaultJob = new SetDefault_AnimControllerData();
        var setDefaultHandle = setDefaultJob.ScheduleParallel(Dependency);
        setDefaultHandle.Complete();


    }
    protected override void OnUpdate()
    {


        this.Enabled = false;
    }

    [Unity.Burst.BurstCompile]
    public partial struct HumanoidTagging : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(Entity entity, [EntityIndexInQuery]int index,
            in DynamicBuffer<HumanoidParts> parts, in LocalTransform trans)
        {
            //ecb.AddComponent

            for (int i = 0; i < parts.Length; i++)
            {
                int partIndex = ((int)parts[i].part);

                ecb.AddSharedComponent(index, parts[i].entity, new IsHumanoidPart() { parent = entity});
                ecb.AddComponent(index, parts[i].entity, new HumanoidPartIndex() { index = partIndex });

                //ecb.SetComponentEnabled<AnimationControllerData>(index, entity, true);
                if (i == 0)
                {
                    ecb.AddComponent(index, parts[i].entity, new WorldTransform { data = TransformData.FormTransData(trans) });
                }
            }
            //            ecb.AddSharedComponent(index, entity, new IsHumanoidPart());

            //

        }
    }
    public partial struct ResetPostTransform : IJobEntity
    {
        //[ReadOnly] public EntityManager manager;//사용불가
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeArray<Entity> controllerEntity;
        [ReadOnly] public NativeArray<LocalTransform> controllerTrans;

        public void Execute(Entity entity, [EntityIndexInQuery]int index, in IsHumanoidPart PartPerent, in LocalTransform trans, in PostTransformMatrix Post)
        {
            //var Ptrans = manager.GetComponentData<LocalTransform>(PartPerent.parent);
            //Debug.Log(Ptrans);

            int ControllerIndex = -1;
            var perant = PartPerent.parent;
            ControllerIndex = System.Array.FindIndex(controllerEntity.ToArray(), (t => t == perant));

            //if (ControllerIndex >= 0)
            {
                //Post.Value = controllerTrans[ControllerIndex].ToMatrix();

                //========== ECB으로 PostTransfom에 Localtrans 값 집어 놓고 , Localtrans은 기본값으로

                Debug.Log(">> " + trans.ToInverseMatrix() + " ++ Fix Point");
                //============= 아 씨발

                //ecb.SetComponent(index, entity, new PostTransformMatrix { Value = trans.ToInverseMatrix() });
                //ecb.SetComponent(index, entity, new LocalTransform {Position = Vector3.zero, Rotation = Quaternion.identity, Scale = (trans.Scale * Post.Value.Scale().x) });
            }
        }
    }
    //[Unity.Burst.BurstCompile]
    public partial struct SetDefault_AnimControllerData : IJobEntity
    {
        public void Execute([EntityIndexInQuery]int index, ref AnimationControllerData data)
        {
            data.ChangeFade = ECSAnimationDataTable.Instance.AnimInfo[data.AnimationIndex].FadeTime;
            data.NextAnimationIndex = ECSAnimationDataTable.Instance.AnimInfo[data.AnimationIndex].NextAnim;
            data.PreAnimationIndex = -1;

        }
    }
}
