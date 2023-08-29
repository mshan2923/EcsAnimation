using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Animation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.Burst;

//UpdateAfter(typeof(BakingSystem)), 
[UpdateAfter(typeof(EcsAnimationSetUp))]
public partial class EcsAnimationSystem : SystemBase
{
    EntityQuery Animator;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        GetAnimator();

        var temp = ECSAnimationDataTable.Instance;

    }
    protected override void OnUpdate()
    {

        {
            AnimationJob animationJob = new AnimationJob
            {
                delta = SystemAPI.Time.DeltaTime
            };
            var jobHandle = animationJob.ScheduleParallel(Dependency);//Animator 추가 하니깐 에러

            jobHandle.Complete();

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            //var ecb = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

            //**** 미리 리스트 만들어 놓으면 되긴한데 , 비활성화되면??

            ApplyTransformJob applyJob = new ApplyTransformJob
            {
                //animator = Animator,
                ecb = ecb.AsParallelWriter(),
                AnimationEntities = Animator.ToEntityArray(Allocator.TempJob),
                AnimationDatas = Animator.ToComponentDataArray<AnimationControllerData>(Allocator.TempJob),
                //AnimatorTransform = Animator.ToComponentDataArray<LocalTransform>(Allocator.TempJob)
            };
            var applyHandle = applyJob.ScheduleParallel(jobHandle);//=**************  이것때문에 기다리면서 렉유발 (burstCompile 쓰니깐 해결... 어?)

            applyHandle.Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();//BeginSimulationEntityCommandBufferSystem 은 사용X



            //var query = GetEntityQuery(typeof(WorldTransform));

            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);

            var L2WJob = new LocalToWorldJob()
            {
                //manager = EntityManager,
                ecb = ecb2.AsParallelWriter(),
                AnimatorEntity = Animator.ToEntityArray(Allocator.TempJob),
                AnimatorTransform = Animator.ToComponentDataArray<LocalTransform>(Allocator.TempJob)
            };
            var L2WHandle = L2WJob.ScheduleParallel(jobHandle);
            L2WHandle.Complete();

            ecb2.Playback(EntityManager);
            ecb2.Dispose();
        }//Animation Process


        
    }
    public void GetAnimator()
    {
        Animator = GetEntityQuery(typeof(AnimationControllerData), typeof(LocalTransform));

        this.Enabled = Animator.CalculateEntityCount() > 0;

        Debug.Log("Animator Amount : " + Animator.CalculateEntityCount());
    }
    [BurstCompile]
    public partial struct AnimationJob : IJobEntity//Unity.Jobs.IJobParallelFor
    {
        [WriteOnly] public float AnimPartRate;
        [ReadOnly] public float delta;

        public void Execute([EntityIndexInQuery] int index, ref AnimationControllerData data)
        {
            var DT = ECSAnimationDataTable.Instance;
            bool isEnd = false;

            int AnimPart = 0;
            AnimationPose PreAnimPartData;
            AnimationPose AnimPartData;
            int KeyPoints = DT.AnimInfo[data.AnimationIndex].AnimationData.Count;

            //data.NextAnimationIndex = DT.AnimInfo[data.AnimationIndex].NextAnim;
            //      / 음.... 에니메이션 컨트롤러로 Loop OR 다음 에니메이션 실행하게 값 설정
            //      / 기본값이 -1 이여야 하는데

            data.ChangeFade = DT.AnimInfo[data.AnimationIndex].FadeTime;

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
                        }
                        else if (AnimPart >= KeyPoints)
                        {
                            AnimPartRate = 1;
                            AnimPartData = DT.AnimInfo[data.AnimationIndex].AnimationData[KeyPoints - 1];
                            data.IsChange = true;
                        }
                        else
                        {
                            PreAnimPartData = DT.AnimInfo[data.AnimationIndex].AnimationData[AnimPart - 1];
                            AnimPartData = DT.AnimInfo[data.AnimationIndex].AnimationData[AnimPart];

                            AnimPartRate = (data.PlayTime - PreAnimPartData.time) / (AnimPartData.time - PreAnimPartData.time);
                        }
                    }//Calculate 'AnimPartRate'


                    if (AnimPart < KeyPoints)
                        data.PlayTime += delta;
                    data.AnimPart = AnimPart;
                    data.AnimPartRate = AnimPartRate;
                }
            }else if (data.NextAnimationIndex >=0)
            {
                data.PreAnimationIndex = data.AnimationIndex;
                data.AnimationIndex = data.NextAnimationIndex;
                //data.NextAnimationIndex = -1;//============== 컨트롤러에서 값을 가져옴
                data.PlayTime = 0;
            }


            if (data.IsChange && data.IsPlay)
            {
                //Setup  ChangeFading
                if (data.ChangeFade > 0)
                {
                    if (data.ChangeTime < data.ChangeFade)
                    {
                        data.ChangeTime += delta;
                    }
                    else
                    {
                        isEnd = true;
                    }
                }
                else
                {
                    if (data.NextAnimationIndex >= 0)
                    {
                        isEnd = true;
                    }//즉시 변환
                    //else -> Stop
                }

                if (isEnd)
                {
                    data.PreAnimationIndex = data.AnimationIndex;
                    data.AnimationIndex = data.NextAnimationIndex;
                    //data.NextAnimationIndex = -1;//============== 컨트롤러에서 값을 가져옴
                        // DT에 다음 에니가 지정되면 반복하게 할까 -> 이걸 컨트롤러 에서
                    data.PlayTime = 0;

                    data.IsChange = false;
                    data.ChangeTime = 0;
                }
            }

        }
    }
    [BurstCompile]
    public partial struct ApplyTransformJob : IJobEntity
    {
        //[ReadOnly, NativeDisableUnsafePtrRestriction] public EntityQuery animator;//XX

        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeArray<Entity> AnimationEntities;
        [ReadOnly] public NativeArray<AnimationControllerData> AnimationDatas;
        //[ReadOnly] public NativeArray<LocalTransform> AnimatorTransform;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in IsHumanoidPart PartParent,
            in HumanoidPartIndex partIndex ,in LocalTransform trans)// , in SpawnTransform spawn) - 
        {

            AnimationControllerData data = new();

            var parent = PartParent.parent;
            int FindIndex = System.Array.FindIndex(AnimationEntities.ToArray(), (t => t == parent));

            if (FindIndex < 0)
                return;

            data = AnimationDatas[FindIndex];


            var DT = ECSAnimationDataTable.Instance;
            //int AnimPart = AnimationDatas[animatorIndex].AnimPart;

            if (data.IsPlay)//(AnimationDatas[animatorIndex].IsPlay)
            {
                if (data.IsChange)//(AnimationDatas[animatorIndex].IsChange)
                {
                    //Debug.Log("Chaging : " + AnimationDatas[animatorIndex].ChangeTime);
                }else
                {

                    if (data.AnimPart == 0)//(AnimPart == 0)
                    {
                        if (data.PreAnimationIndex >= 0)//(AnimationDatas[animatorIndex].PreAnimationIndex >= 0)
                        {
                            //int pre = AnimationDatas[animatorIndex].PreAnimationIndex;
                            var PastPose = DT.AnimInfo[data.PreAnimationIndex].AnimationData[^1].transform[partIndex.index];
                            var NextPose = DT.AnimInfo[data.AnimationIndex].AnimationData[data.AnimPart].transform[partIndex.index];

                            AddEcbtoTransform(entity, index, partIndex, PastPose, NextPose, trans, data);
                        }
                        else
                        {
                            // 이전 포즈 데이터가 없음 , 일단 현제 포즈에서 대상으로 일반 Lerp로
                        }
                    }
                    else if (data.AnimPart < DT.AnimInfo[data.AnimationIndex].AnimationData.Count)
                    {
                        //Curve 데이터를 적용해야하는데 ...
                        //   어떤 부위인지 알아야 되는데?

                        var PastPose = DT.AnimInfo[data.AnimationIndex].AnimationData[data.AnimPart - 1].transform[partIndex.index];
                        var NextPose = DT.AnimInfo[data.AnimationIndex].AnimationData[data.AnimPart].transform[partIndex.index];

                        AddEcbtoTransform(entity, index, partIndex, PastPose, NextPose, trans, data);
                    }
                    else
                    {
                        // Change Other Anim

                    }

                }
            }

        }

        public LocalTransform Lerp(LocalTransform trans, TransformData Past , TransformData Next , float Rate)
        {
            var result = new LocalTransform();

            result.Position = Unity.Mathematics.math.lerp(Past.Position, Next.Position, Mathf.Clamp01(Rate));

            //if (Quaternion.Angle(Past.Rotation, Next.Rotation) > 0)
            result.Rotation = Quaternion.Lerp(Past.Rotation, Next.Rotation, Mathf.Clamp01(Rate));

            result.Scale = Unity.Mathematics.math.lerp(Past.LocalScale, Next.LocalScale, Mathf.Clamp01(Rate));
            //result.Scale = Unity.Mathematics.math.lerp(Past.WorldScale, Next.WorldScale, Rate);

            //Debug.Log("Local : " + result.Scale + " / World : " + worldScale);

            return result;

            //====== Lerp값 가져오고 , 선형 보간으로 값입력
        }//---- 간단한 기본 Lerp >> 커스텀 변환
        public void AddEcbtoTransform(Entity entity, int index, HumanoidPartIndex partIndex,
            TransformData PastPose, TransformData NextPose, LocalTransform trans, AnimationControllerData data)
        {
            //if (partIndex.index > 0)
            {
                //Add (Spawn, Lerp(trans, PastPose, NextPose, data.AnimPartRate))
                ecb.SetComponent(index, entity, Lerp(trans, PastPose, NextPose, data.AnimPartRate));
                //AnimationDatas[data.AnimationIndex].AnimPartRate)
                //Debug.Log(partIndex.index + " / " + Lerp(trans, PastPose, NextPose, data.AnimPartRate));

                //ecb.SetSharedComponent(index, entity, new PawnTransform());//이걸로 대체
            }
            //else if (partIndex.index == 0)
            {
                //어..
            }
        }

        public LocalTransform Add (TransformData spawn , LocalTransform local)
        {
            return new LocalTransform
            {
                Position = spawn.Position + local.Position,
                Rotation = math.mul(spawn.Rotation, local.Rotation),
                Scale = math.mul(spawn.LocalScale, local.Scale)
            };
        }
    }
    [BurstCompile]
    public partial struct LocalToWorldJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public NativeArray<Entity> AnimatorEntity;
        [ReadOnly] public NativeArray<LocalTransform> AnimatorTransform;

        public void Execute(Entity entity, [EntityIndexInQuery]int i,
            in IsHumanoidPart part , in PostTransformMatrix post, in LocalTransform trans, in WorldTransform world)
        {

            ecb.SetComponent(i, entity, Add(world.data, trans));
        }
        public LocalTransform Add(LocalTransform World, LocalTransform local)
        {
            return new LocalTransform()
            {
                Position = World.Position + local.Position,
                Rotation = math.mul(World.Rotation, local.Rotation),
                Scale = World.Scale * local.Scale
            };
        }
        public LocalTransform Add(PostTransformMatrix post, LocalTransform transform)
        {
            //Debug.Log((post.Value.Translation() + transform.Position));
            Debug.Log($"Post : {post.Value.Translation()} + Local : {transform.Position}");

            return new LocalTransform()
            {
                Position = post.Value.Translation() + transform.Position,
                Rotation = math.mul(post.Value.Rotation(), transform.Rotation),
                Scale = transform.Scale//(post.Value.Scale() * transform.Scale).x
            };
        }
        public LocalTransform Add(TransformData world, LocalTransform local)
        {
            //Debug.Log($"world : {world.Position} + local : {local.Position} = {(world.Position + local.Position)}");
            return new LocalTransform()
            {
                Position = world.Position + local.Position,
                Rotation = math.mul(world.Rotation, local.Rotation),
                Scale = world.LocalScale * local.Scale
            };
        }

    }
}
