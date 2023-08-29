using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Animation;
using static UnityEngine.EventSystems.EventTrigger;
using Unity.Transforms;
using Unity.Entities.UniversalDelegates;
using System.ComponentModel;
using Unity.Jobs;

public class EcsSpanwer : MonoBehaviour
{
    public GameObject Target;
    public float intaval = 1f;
    public int SpawnAmount = 1;
    public int MaxAmount = 10000;
}
public struct C_EcsSpawner : IComponentData
{
    public Entity target;
    public float intaval;
    public int SpawnAmount;
    public int MaxAmount;
    public Unity.Transforms.LocalTransform Transform;
}
public struct C_EcsSpawnTarget : IBufferElementData
{
    public Entity target;
}
public class EcsSpawnerBake : Baker<EcsSpanwer>
{
    public override void Bake(EcsSpanwer authoring)
    {
        AddComponent(new C_EcsSpawner()
        {
            target = GetEntity(authoring.Target, TransformUsageFlags.Dynamic),
            intaval = authoring.intaval,
            SpawnAmount = authoring.SpawnAmount,
            MaxAmount = authoring.MaxAmount,
            Transform = new Unity.Transforms.LocalTransform
            {
                Position = authoring.transform.position,
                Rotation = authoring.transform.rotation,
                Scale = authoring.transform.localScale.x
            }
        });

        var buffer = AddBuffer<C_EcsSpawnTarget>();
        buffer.Capacity = 1;
        buffer.Add(new C_EcsSpawnTarget { target = GetEntity(authoring.Target, TransformUsageFlags.Dynamic) });
    }
}
[UpdateAfter(typeof(BakingSystem))]
public partial class EcsSpawnerSystem : SystemBase
{
    EntityQuery SpawnerQuery;
    Unity.Jobs.JobHandle spawnHandle;
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        SpawnerQuery = GetEntityQuery(typeof(C_EcsSpawner));



    }
    protected override void OnUpdate()
    {
   
        int amount = GetEntityQuery(typeof(Animation.AnimationControllerData)).CalculateEntityCount();
        if (amount <= 1)
        {
            var spawnController = GetEntityQuery(typeof(C_EcsSpawner)).ToComponentDataArray<C_EcsSpawner>(Allocator.Temp);
            int spawnAmount = 0;
            foreach(var e in spawnController)
            {
                spawnAmount += e.SpawnAmount;
            }
            spawnController.Dispose();

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var spawnEntities = new NativeArray<Entity>(spawnAmount, Allocator.TempJob);
            var spawnPosition = new NativeArray<LocalTransform>(spawnAmount, Allocator.TempJob);
            var spawnCon = GetEntityQuery(typeof(C_EcsSpawner)).ToComponentDataArray<C_EcsSpawner>(Allocator.TempJob)[0];

            var SpawnIntiJob = new SpawnIntilize()
            {
                ecb = ecb.AsParallelWriter(),
                SpawnEntities = spawnEntities,
                SpawnPosition = spawnPosition,
                Spacing = 1.2f,
                spanwer = spawnCon
            };
            var SpawnIntiHandle =  SpawnIntiJob.Schedule(spawnAmount, 8, Dependency);
            SpawnIntiHandle.Complete();

            //Debug.Log("Spawned length : >>" + spawnEntities.Length + " / " + spawnPosition[0].Position);

            //***************GetEntityQuery으로 대상찾기 는데 위치값은 Post으로 해도 되... 지 않지 내가 지정해야 되는데
            //******************* spawnEntities 대상으로 Hip Array를 만들어서 위치값 변경

            //---**********> ECB는 설정하는것에 제한이 없으므로 (가져오는건 못해...) 아 썅
            //      WorldTransform을 EcbAnimationSetup과 여기서 추가 시켜야됨 - 가져와야해...


            ecb.Playback(World.EntityManager);
            ecb.Dispose();

            ecb = new EntityCommandBuffer(Allocator.TempJob);
            var Spawned = Entities.WithAll<IsHumanoidPart, PostTransformMatrix>().WithNone<WorldTransform>().ToQuery();
            Debug.Log(">>" + Spawned.CalculateEntityCount() + " / " + GetEntityQuery(typeof(WorldTransform)).CalculateEntityCount());

            var RepositionJob = new RePosition()
            {
                ecb = ecb.AsParallelWriter(),
                SpawnPosition = spawnPosition
            };
            var RepositionHandel = RepositionJob.ScheduleParallel(Spawned, spawnHandle);
            RepositionHandel.Complete();
            ecb.Playback(World.EntityManager);
            ecb.Dispose();

            /*
            foreach(var t in GetEntityQuery(typeof(WorldTransform)).ToComponentDataArray<WorldTransform>(Allocator.Temp))
            {
                Debug.Log(t.data.Position + " / ");
            }*/
        }
        else
        {
            spawnHandle = Dependency;
        }




        //this.Enabled = false;
    }
    partial struct SpawnIntilize : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float Spacing;

        [WriteOnly] public NativeArray<Entity> SpawnEntities;
        [WriteOnly] public NativeArray<LocalTransform> SpawnPosition;
        [Unity.Collections.ReadOnly] public C_EcsSpawner spanwer;

        public void Execute(int index)//[EntityIndexInQuery]
        {
            int side = Mathf.Max(1, Mathf.FloorToInt(Mathf.Sqrt(spanwer.SpawnAmount)));//Mathf.Max(Mathf.FloorToInt(Mathf.Pow(spanwer.SpawnAmount, (1f / 3f))), 1);
            Entity spawned;
            spawned = ecb.Instantiate(index, spanwer.target);
            SpawnEntities[index] = spawned;

            var pos = spanwer.Transform.Position +
                new Unity.Mathematics.float3(((float)index % side) * Spacing, 0, Mathf.Floor((float)index / side) * Spacing);

            Debug.Log(pos);

            var Ltrans = new LocalTransform
            {
                Position = pos,
                Rotation = spanwer.Transform.Rotation,
                Scale = spanwer.Transform.Scale
            };
            SpawnPosition[index] = Ltrans;

            ecb.SetComponent(index, spawned, new WorldTransform { data = TransformData.FormTransData(Ltrans) });
            ecb.SetComponent(index, spawned, Ltrans);
        }
    }
    partial struct Spawn : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float Spacing;
        [Unity.Collections.ReadOnly] NativeArray<Entity> SpawnEntities;

        public void Execute([EntityIndexInQuery]int index, in C_EcsSpawner spanwer)
        {
            //if (spanwer.intaval < 0)
            {
                //int temp = Mathf.Max(Mathf.FloorToInt(Mathf.Pow(spanwer.SpawnAmount, (1f / 3f))), 1);
                int temp = Mathf.Max(1, Mathf.FloorToInt(Mathf.Sqrt(spanwer.SpawnAmount)));

                /*
                for (int i = 0; i < spanwer.SpawnAmount; i++)
                {
                    var spawned = ecb.Instantiate(index, spanwer.target);
                    var pos = spanwer.Transform.Position +
                                new Unity.Mathematics.float3((i % temp) * Spacing, 0, Mathf.Floor(i / temp) * Spacing);
                    ecb.SetComponent<Unity.Transforms.LocalTransform>(index, spawned,
                        new Unity.Transforms.LocalTransform 
                        { 
                            Position = pos,
                            Rotation = spanwer.Transform.Rotation,
                            Scale = spanwer.Transform.Scale
                        });

                    var spawnTrans = TransformData.FormTransData(spanwer.Transform);
                    spawnTrans.Position = pos;

                    //ecb.SetComponentEnabled<AnimationControllerData>(index, spawned, false);
                    //ecb.SetSharedComponent(index, spawned, new WorldTransform { data = spawnTrans });
                }
                */


            }
        }
    }
    partial struct RePosition : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float Spacing;
        //[ReadOnly] NativeArray<Entity> SpawnEntities;
        [Unity.Collections.ReadOnly] public NativeArray<LocalTransform> SpawnPosition;

        public void Execute(Entity entity, [EntityIndexInQuery] int index)//, in DynamicBuffer<HumanoidParts> parts)
        {
            //parts[0]
            if (index <  SpawnPosition.Length)
            {
                ecb.AddComponent(index, entity, new WorldTransform { data = TransformData.FormTransData(SpawnPosition[index]) });// 모든 대상이므로 문제 발생
                Debug.Log(TransformData.FormTransData(SpawnPosition[index]).Position);
            }
        }
    }

}