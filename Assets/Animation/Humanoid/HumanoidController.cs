using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Animation
{
    /// <summary>
    /// 부위별 엔티티는 Humanoid.cs > HumanoidStructureBaker 에서 추가 / 이건...자식 오브젝트만 지정하고 , 에러발생 시키고...(아마)
    /// </summary>
    public class HumanoidController : MonoBehaviour
    {
        private static HumanoidController _instance;
        public static HumanoidController Instance
        {
            set
            {
                if (_instance == null)
                    _instance = value;
            }
            get => _instance;
        }
        public Humanoid humanoid
        {
            private set => humanoid = value;
            get
            {
                if (humanoid == null)
                {
                    humanoid = GetComponent<Humanoid>();
                }
                return humanoid;
            }
        }

        public bool PhysicsToggle;
        public bool JointToggle;

        private void OnEnable()
        {
            _instance = this;
        }
        void Start(){        }

    }
    public struct ChildBuffer : IBufferElementData
    {
        public Entity entity;
        //public BlobAssetReference<TempPool> pool;
    }
    //public struct EcsAnimationData : IBufferElementData
    //{
        //public DynamicBuffer<DynamicBuffer<EcsAnimationSlot>> data;// ======== 이게 추가 되지않고 존재만해도 에러
    //}
    public struct EcsAnimationSlot : IBufferElementData
    {
        public float time;
        //----- 부위별 트랜스폼
    }

    public struct HumanoidControllerComponent : IComponentData
    {
        public Entity controller;
    }
    public struct TempStruct
    {
        public float time;
    }
    public struct TempPool
    {
        public BlobArray<TempStruct> Pools;
    }

    public class HumanoidControllerBaker : Baker<HumanoidController>
    {
        public override void Bake(HumanoidController authoring)
        {
            if (HumanoidController.Instance == null)
            {
                HumanoidController.Instance = authoring;
            }

            if (HumanoidController.Instance == authoring)
                AddComponent(new HumanoidControllerComponent { controller = GetEntity(authoring) });

            
            var buffer = AddBuffer<ChildBuffer>();
            buffer.Capacity = authoring.transform.childCount;
            for (int i = 0; i < authoring.transform.childCount; i++)
            {

                buffer.Add(new ChildBuffer { entity = GetEntity(authoring.transform.GetChild(i), TransformUsageFlags.None) });
            }
            //Unity.Transforms.Child 으로하면 안됰ㅋㅋㅋ


            //var animBuffer = AddBuffer<EcsAnimationData>();// 이중 버퍼 안되나...

            Debug.Log(System.Enum.GetValues(typeof(HumanoidStructure)).Length + "/ obj : "
                + System.Enum.GetValues(typeof(HumanoidStructure)).GetValue(1).ToString() + "\n"
                + ((int)System.Enum.GetValues(typeof(HumanoidStructure)).GetValue(1)));
            // 문자열 보단 boxing 나은거 같기도?  / 형전환해서 바로 blobArray에 접근

            //----------------- buffer.add() 관련된 부분 빼고 제거
        }
    }
}