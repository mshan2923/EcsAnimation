using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Animation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Transforms;

[UpdateAfter(typeof(BakingSystem))]
public partial class ECS_DT_Test : SystemBase
{
    EntityQuery Animator;
    EntityQuery objects;


    EntityCommandBuffer.ParallelWriter ecbPaller;
    float TimeforDebug = 0;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        var acessJob = new TempAcess()
        {
            //world = World,
            manager = EntityManager
        };
        var acessHandle = acessJob.Schedule(Dependency);
        acessHandle.Complete();

        Entities.WithAll<AnimationControllerData>()
            .ForEach((Entity e, int entityInQueryIndex, ref LocalTransform trans) =>
            {
                //trans.Position.y = SystemAPI.GetComponentRO<LocalTransform>(e).ValueRO.Scale * 10;
                //SystemAPI.GetComponentRO<AnimationControllerData>(e).ValueRO.AnimationIndex;
                //EntityManager.GetComponentData<AnimationControllerData>(e);//XX

            }).ScheduleParallel();
        

        this.Enabled = false;
        return;

        Debug.Log("In System : " + ECSAnimationDataTable.Instance.name);
        Debug.Log("-->" + ECSAnimationDataTable.Instance.AnimInfo[0].AnimationData[1].time);

        //ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
        //    .CreateCommandBuffer(World.Unmanaged).AsParallelWriter();// 표준 명령 시스템으로 하니... 타이밍 문제로 안됨

        Animator = GetEntityQuery(typeof(AnimationControllerData));
        objects = GetEntityQuery(typeof(Unity.Rendering.WorldToLocal_Tag), typeof(LocalTransform), typeof(LocalToWorld));
        //        (Unity.Rendering)  BlendProbeTag >> WorldToLocal_Tag 으로 바꿈 + 둘다 잘되는데 잘 모르겠음

        //this.EntityManager

    }
    protected override void OnUpdate()
    {
        // 파라미터 -> 선택한 에니메이션 인덱스 / 플레이 시간 / 상태(재생 여부)
        // 내부 변수 -> 에니메이션 구간 / 러프 커브

        this.Enabled = false;
        return;

        AnimationJob animationJob = new AnimationJob 
        {
            delta = SystemAPI.Time.DeltaTime
        };
        var jobHandle = animationJob.ScheduleParallel( Dependency);//Animator 추가 하니깐 에러

        jobHandle.Complete();


        TimeforDebug += SystemAPI.Time.DeltaTime;

         Debug.Log(Animator.CalculateEntityCount() + " / ");

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        ecbPaller = ecb.AsParallelWriter();

        var applyTransJob = new ApplyTransformJob
        {
            animator = Animator,

            ProbeQuery = objects,
            Ts = objects.ToComponentDataArray<Unity.Rendering.WorldToLocal_Tag>(Allocator.TempJob),
            trans = objects.ToComponentDataArray<LocalTransform>(Allocator.TempJob),
            L2W = this.objects.ToComponentDataArray<LocalToWorld>(Allocator.TempJob),

            ecb = this.ecbPaller,
            time = TimeforDebug
        };
        var applyTransHandle = applyTransJob.ScheduleParallel( jobHandle);
        applyTransHandle.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    public partial struct AnimationJob : IJobEntity//Unity.Jobs.IJobParallelFor
    {
        [WriteOnly] public float AnimPartRate;
        [ReadOnly] public float delta;

        public void Execute([EntityIndexInQuery]int index, ref AnimationControllerData data)
        {
            var DT = ECSAnimationDataTable.Instance;
            if (DT == null)
                return;

            int AnimPart = 0;            
            AnimationPose PreAnimPartData;
            AnimationPose AnimPartData;
            int KeyPoints = DT.AnimInfo[data.AnimationIndex].AnimationData.Count;

            if (data.AnimationIndex >= 0)
            {
                if (KeyPoints > 0 && data.IsPlay)
                {
                    for (int i = 0; i < KeyPoints; i++)
                    {
                        if (DT.AnimInfo[data.AnimationIndex].AnimationData[i].time >= data.PlayTime)
                        {
                            break;
                        }
                        else
                        {
                            AnimPart = i + 1;
                        }
                    }

                    {
                        if (AnimPart == 0)
                        {
                            AnimPartData = DT.AnimInfo[data.AnimationIndex].AnimationData[AnimPart];
                            AnimPartRate = data.PlayTime / AnimPartData.time;
                        }else if (AnimPart >= KeyPoints)
                        {
                            AnimPartRate = 1;
                            AnimPartData = DT.AnimInfo[data.AnimationIndex].AnimationData[KeyPoints - 1];
                        }
                        else
                        {
                            PreAnimPartData = DT.AnimInfo[data.AnimationIndex].AnimationData[AnimPart - 1];
                            AnimPartData = DT.AnimInfo[data.AnimationIndex].AnimationData[AnimPart];

                            AnimPartRate = (data.PlayTime - PreAnimPartData.time) / (AnimPartData.time - PreAnimPartData.time);
                        }
                    }//Calculate 'AnimPartRate'

                }
            }

            data.PlayTime += delta;//--------- 따로 분리 하기
            data.AnimPart = AnimPart;
            data.AnimPartRate = AnimPartRate;
        }
    }
    public partial struct ApplyTransformJob : IJobEntity
    {
        [ReadOnly, NativeDisableUnsafePtrRestriction] public EntityQuery animator;

        [ReadOnly, NativeDisableUnsafePtrRestriction] public EntityQuery ProbeQuery;
        public NativeArray<Unity.Rendering.WorldToLocal_Tag> Ts;
        public NativeArray<LocalTransform> trans;
        public NativeArray<LocalToWorld> L2W;

        //public EntityManager manager;//병렬으론 불가 , 있기만해도 안되네 / SystemAPI도 안됨
        public EntityCommandBuffer.ParallelWriter ecb;
        public float time;

        public void Execute(Entity entity, [EntityIndexInQuery]int index, in AnimationControllerData data, in DynamicBuffer<HumanoidParts> parts)
        {
            int animPart = Mathf.Min(data.AnimPart, ECSAnimationDataTable.Instance.AnimInfo[data.AnimationIndex].AnimationData.Count - 1);
            
            var temp = ECSAnimationDataTable.Instance.AnimInfo[data.AnimationIndex]
                .AnimationData[animPart].transform[0].Position;
            //                [Keypoint]            [Part]

            ecb.SetComponent(index, parts[0].entity, new LocalTransform
            {
                Position = new Unity.Mathematics.float3(0, temp.y + time, 0),
                Scale = 1
            });

            var array = ProbeQuery.ToComponentDataArray<Unity.Rendering.WorldToLocal_Tag>(Allocator.Temp);

            Debug.Log($"Array -> {array.Length} / ");
            Debug.Log($"Target : {animPart} / Get : {temp} + {time} \n" +
                $" copyArray : {Ts[0]} >> / Local : {trans[0]} / L2W : {L2W[0].Position}");

            //=======  data.AnimPart 으로 현제 에니메이션 과 목표 에니메이션을 구하고
            //=============  data.AnimPartRate을 Curve에 적용시켜 Lerp된 값 계산

            array.Dispose();
        }
    }

    public partial struct TempAcess : IJobEntity
    {
        
        //public World world;
        //[ReadOnly] 
        public EntityManager manager;//병렬으론 불가

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in AnimationControllerData data)
        {
            //Debug.Log("components : " + manager.GetComponentCount(entity));//이건 됨
                //+ " / " + manager.GetComponentData<LocalTransform>(entity));// 안됨
        }
    }
}
