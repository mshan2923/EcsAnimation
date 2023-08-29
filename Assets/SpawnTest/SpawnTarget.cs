using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class SpawnTarget : MonoBehaviour
{
    private static SpawnTarget _instance;
    public static SpawnTarget instance
    {
        get
        {
            return _instance;
        }
        set
        {
            if (_instance == null)
                _instance = value;
        }
    }
    public GameObject Target;
    public float intaval = -1;

    [Space(10)]
    public GameObject Spawner;
    public GameObject SpawnerParent;
    public int HasParent = 0;

    private void OnEnable()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Mono Start");
        if (HasParent == 0)
            GameObject.Instantiate(this, SpawnerParent.transform);// ECS 활성화시 작동 X
    }

}

public struct SpawnTag : IComponentData { }
public struct SpawnTargetComponent : IComponentData
{
    public Entity Target;
    public float intaval;
}
public class SpawnTargetBaker : Baker<SpawnTarget>
{
    public override void Bake(SpawnTarget authoring)
    {
        if (SpawnTarget.instance == null)
        {
            SpawnTarget.instance = authoring;
        }

        if (SpawnTarget.instance == authoring)
        {
            AddComponent(new SpawnTargetComponent
            {
                Target = GetEntity(authoring.Target, TransformUsageFlags.Dynamic),
                intaval = authoring.intaval
            });
        }
    }
}