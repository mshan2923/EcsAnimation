using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using UnityEditor;

namespace Animation
{
    /// <summary>
    /// Mono , Baker 부분 필요 없음
    /// </summary>
    [System.Serializable]
    public struct AnimationInfo
    {
        public string name;
        //[HideInInspector] 
        public List<AnimationPose> AnimationData;

        public int NextAnim;//연계되는 에니메이션 OR Looping

        public float FadeTime;


        //------------------------ Lerp Type  OR AnimationCurve

        public AnimationInfo(string Name = "")
        {
            name = Name;
            AnimationData = new List<AnimationPose>();
            NextAnim = -1;
            FadeTime = 0;
        }

        public int GetLastAnimData()
        {
            if (AnimationData == null)
            {
                return -1;
            }else
            {
                return AnimationData.Count - 1;
            }
        }

        public bool GetLastAnimData(out AnimationPose vaule)
        {
            if (AnimationData == null)
            {
                vaule = default;
                return false;
            }else
            {
                if (AnimationData.Count > 0)
                {
                    vaule = AnimationData[AnimationData.Count - 1];
                    return true;
                }
                else
                {
                    vaule = default;
                    return false;
                }

            }
        }
    }
    [CustomPropertyDrawer(typeof(AnimationInfo))]
    public class AnimationInfoDrawer : PropertyDrawer
    {
        Rect DrawRect;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //return EditorGUIUtility.singleLineHeight * (property.isExpanded ? 2 : 1) + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("AnimationData"));
            return property.isExpanded ? EditorGUIUtility.singleLineHeight * 4 + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("AnimationData")) :
                EditorGUIUtility.singleLineHeight;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var data = property.FindPropertyRelative("AnimationData");
            SerializedProperty lastData = null;

            string name = property.FindPropertyRelative("name").stringValue + $" - [{data.arraySize}] , 0s";
            string info = $" [ KeyPoint : {data.arraySize} , Length : 0]";

            if (data != null)
            {
                if (data.arraySize > 0)
                {
                    lastData = data.GetArrayElementAtIndex(data.arraySize - 1).FindPropertyRelative("time");

                    name = property.FindPropertyRelative("name").stringValue + $" - [{data.arraySize}] , {lastData.floatValue}s";
                    info = $" [ KeyPoint : {data.arraySize} , Length : {lastData.floatValue}]";
                }
            }else
            {
                name = " - ( Null )";
                info = " [ Data is Null ]";
            }
            //data.GetArrayElementAtIndex()

            DrawRect = new Rect(position.x + 10, position.y, position.width - 10, EditorGUIUtility.singleLineHeight);
            property.FindPropertyRelative("name").stringValue = 
                EditorGUI.TextField(DrawRect, name, property.FindPropertyRelative("name").stringValue);

            DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, GUIContent.none, true);
            
            if (property.isExpanded)
            {
                NextLine(position, 10);
                EditorGUI.LabelField(DrawRect, info);

                NextLine(position, 10);
                EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("NextAnim"), true);
                NextLine(position, 10);
                EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("FadeTime"), true);

                NextLine(position, 10);
                EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("AnimationData"), true);
            }
        }
        void NextLine(Rect pos, float push = 0)
        {
            DrawRect = new Rect(pos.x + push, DrawRect.y + EditorGUIUtility.singleLineHeight, pos.width - push, EditorGUIUtility.singleLineHeight);
        }
    }
    [CustomPropertyDrawer(typeof(AnimationPose))]
    public  class AnimationPoseDrawer : PropertyDrawer
    {
        Rect DrawRect;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //int array = (property.FindPropertyRelative("transform").isExpanded ?  : 0) + 3;
            //System.Enum.GetNames(typeof(HumanoidStructure)).Length + 3;
            //return EditorGUIUtility.singleLineHeight * (property.isExpanded ? array : 1);

            float height = EditorGUIUtility.singleLineHeight;
            var transHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("transform"), true);
            height += (property.isExpanded ? transHeight : 0);
            height += (property.isExpanded ? EditorGUIUtility.singleLineHeight : 0);

            return height;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, GUIContent.none, true);

            DrawRect = new Rect(position.x + 15f, DrawRect.y, position.width - 15f, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("time"));

            if (property.isExpanded)
            {
                NextLine(position);
                //EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("transform"));//Nulls

                property.FindPropertyRelative("transform").isExpanded =
                    EditorGUI.Foldout(DrawRect, property.FindPropertyRelative("transform").isExpanded, new GUIContent { text = "Humanoid Part"}, true);

                float Dataheight = -1;

                if (property.FindPropertyRelative("transform").isExpanded)
                {
                    var humanoidPart = System.Enum.GetNames(typeof(HumanoidStructure));
                    for (int i = 0; i < humanoidPart.Length; i++)
                    {
                        if (Dataheight < 0)
                        {
                            Dataheight = DrawRect.y;
                        }
                        Dataheight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("transform").GetArrayElementAtIndex(i), true);

                        NextLine(position, 10);
                        DrawRect.height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("transform").GetArrayElementAtIndex(i), true);

                        EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("transform").GetArrayElementAtIndex(i),
                            new GUIContent { text = humanoidPart[i]}, true);

                        DrawRect.y = Dataheight;
                    }
                }

                NextLine(position);
                var curve = EditorGUI.CurveField(DrawRect, property.FindPropertyRelative("LerpCurve").animationCurveValue);
                if (curve.length == 2)
                {
                    if (Mathf.Approximately(curve.keys[1].time, 1) && Mathf.Approximately(curve.keys[0].time, 0))
                        property.FindPropertyRelative("LerpCurve").animationCurveValue = curve;
                }else if (curve.length == 0)
                {
                    property.FindPropertyRelative("LerpCurve").animationCurveValue = new AnimationCurve
                    (
                        new Keyframe(0, 0),
                        new Keyframe(1, 1)
                );
                }
            }
        }
        void NextLine(Rect pos, float push = 0)
        {
            DrawRect = new Rect(pos.x + push, DrawRect.y + EditorGUIUtility.singleLineHeight, pos.width - push, EditorGUIUtility.singleLineHeight);
        }
    }

    [System.Serializable]
    public struct AnimationPose
    {
        public AnimationPose(float time, TransformData[] trans = null)
        {
            this.time = time;

            if (trans == null)
            {
                transform = new TransformData[System.Enum.GetNames(typeof(HumanoidStructure)).Length];
            }
            else
            {
                this.transform = trans;
            }

            LerpCurve = new AnimationCurve
                (
                    new Keyframe(0,0),
                    new Keyframe(1,1)
                );//tangent까지 (linear -> [0]0,0 [1]0,0 / smooth -> [0]1,1 [1]1,1)
        }
        public AnimationPose Time(float vaule)
        {
            time = vaule;

            return this;
        }

        public float time;
        public TransformData[] transform;

        public AnimationCurve LerpCurve;
        //-----------Lerp Type 추가
    }

    public class EcsAnimationController : MonoBehaviour
    {
        public int AnimationIndex = 0;
        public float StartPlaytime = 0;
        public bool IsPlay = false;

        void Start()
        {

        }
        private void OnValidate()
        {

        }
    }

    #region Struct (Empty)

    #endregion
    #region Compoenent
    public struct AnimationControllerData : IComponentData, IEnableableComponent
    {
        public int AnimationIndex;
        /// (지금과 같으면 looping, 음수면 중단)
        public int NextAnimationIndex;
        public int PreAnimationIndex;
        public float ChangeFade;

        public bool IsPlay;
        public bool IsChange;

        public float PlayTime;
        public float ChangeTime;

        public int AnimPart;
        public float AnimPartRate;
    }
    #endregion

    public class EcsAninControllerBaker : Baker<EcsAnimationController>
    {
        public override void Bake(EcsAnimationController authoring)
        {
            AddComponent(new AnimationControllerData 
            {
                AnimationIndex = authoring.AnimationIndex,
                PlayTime = authoring.StartPlaytime,
                IsPlay = authoring.IsPlay
            });
        }
    }

}