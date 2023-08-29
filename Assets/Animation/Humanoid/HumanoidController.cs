using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Animation
{
    /// <summary>
    /// ������ ��ƼƼ�� Humanoid.cs > HumanoidStructureBaker ���� �߰� / �̰�...�ڽ� ������Ʈ�� �����ϰ� , �����߻� ��Ű��...(�Ƹ�)
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
        //public DynamicBuffer<DynamicBuffer<EcsAnimationSlot>> data;// ======== �̰� �߰� �����ʰ� ���縸�ص� ����
    //}
    public struct EcsAnimationSlot : IBufferElementData
    {
        public float time;
        //----- ������ Ʈ������
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
            //Unity.Transforms.Child �����ϸ� �ȉ뤻����


            //var animBuffer = AddBuffer<EcsAnimationData>();// ���� ���� �ȵǳ�...

            Debug.Log(System.Enum.GetValues(typeof(HumanoidStructure)).Length + "/ obj : "
                + System.Enum.GetValues(typeof(HumanoidStructure)).GetValue(1).ToString() + "\n"
                + ((int)System.Enum.GetValues(typeof(HumanoidStructure)).GetValue(1)));
            // ���ڿ� ���� boxing ������ ���⵵?  / ����ȯ�ؼ� �ٷ� blobArray�� ����

            //----------------- buffer.add() ���õ� �κ� ���� ����
        }
    }
}