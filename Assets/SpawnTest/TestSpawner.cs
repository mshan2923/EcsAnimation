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

        //RequireForUpdate<SpawnTargetComponent>();//====================== if Enable this code, not work this system(�̰� �����ϱ� �ý����� �ߴ�) 

        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

        var arch = EntityManager.CreateArchetype(typeof(SpawnTag));
        var e = ecb.CreateEntity(arch);
        //ecb.AddComponent<SpawnTag>(e);

        Debug.Log("Entities : " + EntityManager.GetAllEntities().Length  + "\n SpawnTage : " + GetEntityQuery(typeof(SpawnTag)).CalculateEntityCount());
        
        //--------- ������Ʈ�ؼ� �Ǳ��ѵ� , ������ ��ġ�Ѱ�...�ִµ� ���� ��! ���ڿ���!


        Debug.Log("Spawn in TestSpawner");//================== Never print this Log
        var Target = SystemAPI.GetSingleton<SpawnTargetComponent>();
        

        e = ecb.Instantiate(Target.Target);
        ecb.AddComponent<SpawnTag>(e);

    }
}
