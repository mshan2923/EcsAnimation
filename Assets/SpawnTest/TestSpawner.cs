using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public partial class TestSpawner : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        if (SystemAPI.HasSingleton<SpawnTargetComponent>() == false)
        {
            Debug.Log("Not Exist 'SpawnTargetComponent'");
            Enabled = false;
        }
    }
    protected override void OnUpdate()
    {
        if (SpawnTarget.instance != null)
        {
            
        }

        if (SystemAPI.HasSingleton<SpawnTargetComponent>() == false)
        {
            Debug.Log("Not Exist 'SpawnTargetComponent'");
            Enabled = false;
            return;
        }

        //RequireForUpdate<SpawnTargetComponent>();//====================== if Enable this code, not work this system(이게 있으니까 시스템이 중단) 

        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

        var arch = EntityManager.CreateArchetype(typeof(SpawnTag));
        var e = ecb.CreateEntity(arch);
        //ecb.AddComponent<SpawnTag>(e);

        Debug.Log("Entities : " + EntityManager.GetAllEntities().Length  + "\n SpawnTage : " + GetEntityQuery(typeof(SpawnTag)).CalculateEntityCount());
        
        //--------- 업데이트해서 되긴한데 , 에디터 배치한건...있는데 없어 와! 양자역학!


        Debug.Log("Spawn in TestSpawner");//================== Never print this Log
        var Target = SystemAPI.GetSingleton<SpawnTargetComponent>();
        

        e = ecb.Instantiate(Target.Target);
        ecb.AddComponent<SpawnTag>(e);

    }
}
