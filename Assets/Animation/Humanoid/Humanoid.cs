using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEditor;
using Unity.Physics.Authoring;
using Unity.Physics;
using Unity.Mathematics;
using static Unity.Physics.Math;
using UnityEngine.Rendering;

namespace Animation
{
    [SerializeField]
    public enum HumanoidStructure
    {
        Hips, Spine, Chest, UpperChest, Head,
        LeftShoulder, LeftUpperArm, LeftLowerArm, LeftHand,
        RightShoulder, RightUpperArm, RightLowerArm, RightHand,
        LeftUpperLeg, LeftLowerLeg, LeftFoot, LeftToes,
        RightUpperLeg, RightLowerLeg, RightFoot, RightToes
    }
    [SerializeField]
    public struct HumanoidPart
    {
        public HumanoidStructure Part;
        public Entity PartEntity;
        public Vector3 SorketPosition;

        public Vector3 LocalPosition;//=== 스폰될때 의 위치
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }
    [System.Serializable]
    public struct HumanoidPart_Mono
    {
        public HumanoidStructure Part;
        public GameObject PartObj;
        public Vector3 SorketPosition;
    }
    [System.Serializable]
    public struct RagdollJointParameter
    {
        public Vector3 PositionLocal;
        public Vector3 PositionInConnectedEntity;
        public Vector3 MaxImpulse;
        public bool EnableCollision;

        public Vector3 TwistAxisLocal;// - 관절 비틀기(Yaw)축
        public Vector3 TwistAxisInConnectedEntity;
        public Vector3 PerpendicularAxisLocal;// - 관절 Roll 회전축
        public Vector3 PerpendicularAxisInConnectedEntity;

        [Space(10)] public float MaxConeAngle;

        public float MinPerpendicularAngle;
        public float MaxPerpendicularAngle;
        public float MinTwistAngle;
        public float MaxTwistAngle;

        public void SetUp()
        {
            MaxImpulse = Vector3.positiveInfinity;
            TwistAxisLocal = Vector3.forward;
            TwistAxisInConnectedEntity = Vector3.forward;
            PerpendicularAxisLocal = Vector3.right;
            PerpendicularAxisInConnectedEntity = Vector3.right;
        }
    }

    public class Humanoid : MonoBehaviour
    {
        public static readonly int NumOfHumanoidStructure = 21;

        [NonReorderable] public HumanoidPart_Mono[] HumanoidParts = new HumanoidPart_Mono[NumOfHumanoidStructure];

        public RagdollJointParameter SpineJointParameter 
            = new() { MaxImpulse = Vector3.positiveInfinity, TwistAxisLocal = Vector3.forward, TwistAxisInConnectedEntity = Vector3.forward,
                        PerpendicularAxisLocal = Vector3.right, PerpendicularAxisInConnectedEntity = Vector3.right};
        //------ 허리 , 팔, 다리 관절 파라미터

        void Start()
        {

        }

        private void OnEnable()
        {
            for (int i = 0; i < NumOfHumanoidStructure; i++)
            {
                //if (HumanoidParts[i].PartObj.TryGetComponent<PhysicsBodyAuthoring>(out var PB))
                {

                }//else
                {
                    //var AddPB = HumanoidParts[i].PartObj.AddComponent<PhysicsBodyAuthoring>();
                    //AddPB.MotionType = BodyMotionType.Dynamic;
                }                
            }
        }//Empty

        private void OnValidate()
        {
            //HumanoidParts = new HumanoidPart_Mono[20]; // 초기화 대신 추가-제거로 크기 고정시키고
            //부위 부분을 자동으로 할당

            HumanoidPart_Mono[] TempParts;
            if (HumanoidParts.Length != NumOfHumanoidStructure)
            {
                TempParts = new HumanoidPart_Mono[NumOfHumanoidStructure];

                for (int i = 0; i < Mathf.Min(TempParts.Length, HumanoidParts.Length); i++)
                    TempParts[i] = HumanoidParts[i];

                HumanoidParts = TempParts;
            }

            for (int i = 0; i < HumanoidParts.Length; i++)
            {
                HumanoidParts[i].Part = (HumanoidStructure) i;
            }
        }

        public TransformData[] HumanoidPartsToTransforms()
        {
            var vaule = new TransformData[HumanoidParts.Length];

            for (int i = 0; i < HumanoidParts.Length; i++)
            {
                if (HumanoidParts[i].PartObj != null)
                {
                    vaule[i] = new TransformData()
                    {
                        Position = HumanoidParts[i].PartObj.transform.localPosition,
                        Rotation = HumanoidParts[i].PartObj.transform.localRotation,
                        LocalScale = HumanoidParts[i].PartObj.transform.localScale.x,
                        WorldScale = HumanoidParts[i].PartObj.transform.lossyScale.x
                    };
                }
            }

            return vaule;
        }

        public void ApplyTransform(int index, Vector3 pos, quaternion rotation, float scale)
        {
            if (HumanoidParts[index].PartObj != null)
            {
                HumanoidParts[index].PartObj.transform.localPosition = pos;
                HumanoidParts[index].PartObj.transform.localRotation = rotation;
                HumanoidParts[index].PartObj.transform.localScale = Vector3.one * scale;
            }
        }
        public void ApplyTransform(int index, TransformData trans)
        {
            ApplyTransform(index, trans.Position, trans.Rotation, trans.LocalScale);
        }
    }

    [CustomPropertyDrawer(typeof(HumanoidPart_Mono))]
    public class HumanoidPartDrawer : PropertyDrawer
    {
        Rect DrawRect;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * (property.FindPropertyRelative("Part").isExpanded ? 2 : 1);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            DrawRect.width = 50f;
            DrawRect.height = EditorGUIUtility.singleLineHeight * (property.FindPropertyRelative("Part").isExpanded ? 2 : 1);
            property.FindPropertyRelative("Part").isExpanded = EditorGUI.Foldout(DrawRect, property.FindPropertyRelative("Part").isExpanded, "", true);
            DrawRect.x += DrawRect.width;


            DrawRect.width = (position.width - 50f) * 0.5f;
            DrawRect.height = EditorGUIUtility.singleLineHeight;
            var EnumData = EditorGUI.EnumPopup(DrawRect, GUIContent.none, (HumanoidStructure)property.FindPropertyRelative("Part").enumValueIndex);
            System.Enum.ToObject(typeof(HumanoidStructure), property.FindPropertyRelative("Part").enumValueIndex);
            property.FindPropertyRelative("Part").enumValueIndex = (int)System.Convert.ChangeType(EnumData, typeof(int));
            DrawRect.x += DrawRect.width;

            EditorGUI.ObjectField(DrawRect, property.FindPropertyRelative("PartObj"), GUIContent.none);

            if (property.FindPropertyRelative("Part").isExpanded)
            {
                DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                DrawRect.width = (position.width - 50f);
                DrawRect.x += 50f;
                DrawRect.y += EditorGUIUtility.singleLineHeight;

                property.FindPropertyRelative("SorketPosition").vector3Value = 
                    EditorGUI.Vector3Field(DrawRect, "SorketPosition", property.FindPropertyRelative("SorketPosition").vector3Value);
            }
        }
    }

    public struct HumanoidStructureComponent : IComponentData
    {
        public BlobAssetReference<HumanoidParts> HumanoidParts;
        public static Entity instance;
    }
    public struct HumanoidParts : IBufferElementData
    {
        //public BlobArray<HumanoidPart> Array;
        public HumanoidStructure part;
        public Entity entity;
    }

    public struct HasRagdoll : IComponentData { }
    public struct IsHumanoidPart : ISharedComponentData
    {
        public Entity parent;
    }
    public struct WorldTransform : IComponentData
    {
        public TransformData data;
    }
    public struct MoveTransform : ISharedComponentData 
    {
        public TransformData data;
    }
    public struct HumanoidPartIndex : IComponentData { public int index; }

    public struct HumanoidPartData : IComponentData
    {
        //public int index;
        public DynamicBuffer<HumanoidParts> data;
    }

    public class HumanoidStructureBaker : Baker<Humanoid>
    {
        public override void Bake(Humanoid authoring)
        {

            {
                /*

BlobBuilderArray<HumanoidPart> arrayBuilder = builder.Allocate(ref parts.Array, Humanoid.NumOfHumanoidStructure);
//Intialize HumanoidParts

var e = CreateAdditionalEntity(TransformUsageFlags.Dynamic, true, "Child");
var db = AddBuffer<Unity.Transforms.Child>(e);//===================================================== 안되는데...
db.Capacity = authoring.HumanoidParts.Length;

for (int i = 0; i < authoring.HumanoidParts.Length; i++)
{
    arrayBuilder[i].Part = authoring.HumanoidParts[i].Part;
    arrayBuilder[i].PartEntity = authoring.HumanoidParts[i].PartObj == null ? Entity.Null : GetEntity(authoring.HumanoidParts[i].PartObj, TransformUsageFlags.Dynamic);
    //*****************  실제 보이는 엔티티가 아닌 새로운 엔티티, 배치안된 상태로 생성


    arrayBuilder[i].SorketPosition = authoring.HumanoidParts[i].SorketPosition;

    if (authoring.HumanoidParts[i].PartObj != null)
    {
        arrayBuilder[i].LocalPosition = authoring.HumanoidParts[i].PartObj.transform.position;
        arrayBuilder[i].LocalRotation = authoring.HumanoidParts[i].PartObj.transform.rotation;
        arrayBuilder[i].LocalScale = authoring.HumanoidParts[i].PartObj.transform.localScale;

        //AddComponent<HasRagdoll>(GetEntity(authoring.HumanoidParts[i].PartObj));//========= 추가가 안됨
    }


    db.Add(new Child() { Value = authoring.HumanoidParts[i].PartObj == null ? Entity.Null : GetEntity(authoring.HumanoidParts[i].PartObj, TransformUsageFlags.Dynamic) });
}
*/
            }//Disable

            {
                /*
                arrayBuilder[0] = new HumanoidPart { Part = HumanoidStructure.Hips, PartEntity = authoring.HumanoidParts[0].PartObj == null ? Entity.Null : GetEntity(authoring.Hips) };
                arrayBuilder[1] = new HumanoidPart { Part = HumanoidStructure.Spine, PartEntity = authoring.Spine == null ? Entity.Null : GetEntity(authoring.Spine) };
                arrayBuilder[2] = new HumanoidPart { Part = HumanoidStructure.Chest, PartEntity = authoring.Chest == null ? Entity.Null : GetEntity(authoring.Chest) };
                arrayBuilder[3] = new HumanoidPart { Part = HumanoidStructure.UpperChest, PartEntity = authoring.UpperChest == null ? Entity.Null : GetEntity(authoring.UpperChest) };

                arrayBuilder[4] = new HumanoidPart { Part = HumanoidStructure.LeftShoulder, PartEntity = authoring.LeftShoulder == null ? Entity.Null : GetEntity(authoring.LeftShoulder) };
                arrayBuilder[5] = new HumanoidPart { Part = HumanoidStructure.LeftUpperArm, PartEntity = authoring.LeftUpperArm == null ? Entity.Null : GetEntity(authoring.LeftUpperArm) };
                arrayBuilder[6] = new HumanoidPart { Part = HumanoidStructure.LeftLowerArm, PartEntity = authoring.LeftLowerArm == null ? Entity.Null : GetEntity(authoring.LeftLowerArm) };
                arrayBuilder[7] = new HumanoidPart { Part = HumanoidStructure.LeftHand, PartEntity = authoring.LeftHand == null ? Entity.Null : GetEntity(authoring.LeftHand) };

                arrayBuilder[8] = new HumanoidPart { Part = HumanoidStructure.RightShoulder, PartEntity = authoring.RightShoulder == null ? Entity.Null : GetEntity(authoring.RightShoulder) };
                arrayBuilder[9] = new HumanoidPart { Part = HumanoidStructure.RightUpperArm, PartEntity = authoring.RightUpperArm == null ? Entity.Null : GetEntity(authoring.RightUpperArm) };
                arrayBuilder[10] = new HumanoidPart { Part = HumanoidStructure.RightLowerArm, PartEntity = authoring.RightLowerArm == null ? Entity.Null : GetEntity(authoring.RightLowerArm) };
                arrayBuilder[11] = new HumanoidPart { Part = HumanoidStructure.RightHand, PartEntity = authoring.RightHand == null ? Entity.Null : GetEntity(authoring.RightHand) };

                arrayBuilder[12] = new HumanoidPart { Part = HumanoidStructure.LeftUpperLeg, PartEntity = authoring.LeftUpperLeg == null ? Entity.Null : GetEntity(authoring.LeftUpperLeg) };
                arrayBuilder[13] = new HumanoidPart { Part = HumanoidStructure.LeftLowerLeg, PartEntity = authoring.LeftLowerLeg == null ? Entity.Null : GetEntity(authoring.LeftLowerLeg) };
                arrayBuilder[14] = new HumanoidPart { Part = HumanoidStructure.LeftFoot, PartEntity = authoring.LeftFoot == null ? Entity.Null : GetEntity(authoring.LeftFoot) };
                arrayBuilder[15] = new HumanoidPart { Part = HumanoidStructure.LeftToes, PartEntity = authoring.LeftTeos == null ? Entity.Null : GetEntity(authoring.LeftTeos) };

                arrayBuilder[16] = new HumanoidPart { Part = HumanoidStructure.RightUpperLeg, PartEntity = authoring.RightUpperLeg == null ? Entity.Null : GetEntity(authoring.RightUpperLeg) };
                arrayBuilder[17] = new HumanoidPart { Part = HumanoidStructure.RightLowerLeg, PartEntity = authoring.RightLowerLeg == null ? Entity.Null : GetEntity(authoring.RightLowerLeg) };
                arrayBuilder[18] = new HumanoidPart { Part = HumanoidStructure.RightFoot, PartEntity = authoring.RightFoot == null ? Entity.Null : GetEntity(authoring.RightFoot) };
                arrayBuilder[19] = new HumanoidPart { Part = HumanoidStructure.RightToes, PartEntity = authoring.RightTeos == null ? Entity.Null : GetEntity(authoring.RightTeos) };
                */
            }//Disable


            //-----------------------------------

            AddComponent<HasRagdoll>();

            AddComponent(new WorldTransform { data = TransformData.FormTransData(authoring.transform)});
            //AddSharedComponent(new MoveTransform { data = new TransformData(float3.zero, quaternion.identity)});


            var buffer = AddBuffer<HumanoidParts>();

            for (int i = 0; i < authoring.HumanoidParts.Length; i++)
            {
                if (authoring.HumanoidParts[i].PartObj == null)
                    continue;

                var temp = new HumanoidParts() 
                { 
                    part = authoring.HumanoidParts[i].Part ,
                    entity = GetEntity(authoring.HumanoidParts[i].PartObj, TransformUsageFlags.Dynamic)
                };

                buffer.Add(temp);
            }
            
        }

        /*
        void AddRagdollJoint(RagdollJointParameter parameter , HumanoidPart ParentPart, HumanoidPart ChildPart, PhysicsBodyAuthoring PB)
        {
            var bFromA = math.mul(math.inverse(new RigidTransform(ChildPart.LocalRotation, ChildPart.LocalPosition)),
                                    new RigidTransform(ParentPart.LocalRotation, ParentPart.LocalPosition));

            PhysicsJoint.CreateRagdoll
                (
                    new BodyFrame
                    {
                        Axis = parameter.TwistAxisLocal,
                        PerpendicularAxis = parameter.PerpendicularAxisLocal,
                        Position = ParentPart.LocalPosition - ChildPart.SorketPosition
                    },
                    new BodyFrame
                    {
                        Axis = math.mul(bFromA.rot, parameter.TwistAxisLocal),
                        PerpendicularAxis = math.mul(bFromA.rot, parameter.PerpendicularAxisLocal),
                        Position = parameter.PositionInConnectedEntity
                    },
                    math.radians(parameter.MaxConeAngle),
                    math.radians(new FloatRange(parameter.MinPerpendicularAngle, parameter.MaxPerpendicularAngle)),
                    math.radians(new FloatRange(parameter.MinTwistAngle, parameter.MaxTwistAngle)),
                    out var primaryCone,
                    out var perpendicularCone
                );
            primaryCone.SetImpulseEventThresholdAllConstraints(parameter.MaxImpulse);
            perpendicularCone.SetImpulseEventThresholdAllConstraints(parameter.MaxImpulse);

            //

            var constrainedBodyPair = new PhysicsConstrainedBodyPair
                (
                    GetEntity(TransformUsageFlags.Dynamic),
                    ParentPart.PartEntity,
                    parameter.EnableCollision
                );
            var entities = new NativeList<Entity>(1, Allocator.TempJob);
            //var worldIndex = 
            CreateJointEntities(PB.WorldIndex, constrainedBodyPair, 
                new NativeArray<PhysicsJoint>(2, Allocator.Temp) { [0] = primaryCone, [1] = perpendicularCone}, entities);

            entities.Dispose();
        }
        public void CreateJointEntities(uint worldIndex, PhysicsConstrainedBodyPair constrainedBodyPair, NativeArray<PhysicsJoint> joints, NativeList<Entity> newJointEntities)
        {
            if (!joints.IsCreated || joints.Length == 0)
                return;

            if (newJointEntities.IsCreated)
                newJointEntities.Clear();
            else
                newJointEntities = new NativeList<Entity>(joints.Length, Allocator.Temp);

            // create all new joints
            var multipleJoints = joints.Length > 1;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            for (var i = 0; i < joints.Length; ++i)
            {
                var jointEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                AddSharedComponent(jointEntity, new PhysicsWorldIndex(worldIndex));

                AddComponent(jointEntity, constrainedBodyPair);
                AddComponent(jointEntity, joints[i]);

                newJointEntities.Add(jointEntity);

                if (GetComponent<ModifyJointLimitsAuthoring>() != null)
                {
                    AddComponent(jointEntity, new JointEntityBaking()
                    {
                        Entity = entity
                    });
                    AddSharedComponentManaged(jointEntity, new ModifyJointLimits());
                }
            }

            if (multipleJoints)
            {
                // set companion buffers for new joints
                for (var i = 0; i < joints.Length; ++i)
                {
                    var companions = AddBuffer<PhysicsJointCompanion>(newJointEntities[i]);
                    for (var j = 0; j < joints.Length; ++j)
                    {
                        if (i == j)
                            continue;
                        companions.Add(new PhysicsJointCompanion { JointEntity = newJointEntities[j] });
                    }
                }
            }
        }
        *///AddRagdollJoint

    }

}