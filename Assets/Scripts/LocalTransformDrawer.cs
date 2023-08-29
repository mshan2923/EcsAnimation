using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Transforms;
using Unity.Mathematics;

[System.Serializable]
public struct TransformData
{
    public float3 Position;
    public quaternion Rotation;
    public float LocalScale;
    public float WorldScale;//+++++++++++ 추가해서 Lerp 에 적용
                            //ECSAnimSetup에서 부모값이 제거 되서 월드크기으로 적용
                            //위치, 회전값은 원래 로컬기준으로 

    public TransformData(float3 pos, quaternion rot, float localScale = 1, float worldScale = 1 )
    {
        Position = pos;
        Rotation = rot;
        LocalScale = localScale;
        WorldScale = worldScale;
    }
    public LocalTransform ToLocalTrans()
    {
        return new LocalTransform
        {
            Position = this.Position,
            Rotation = this.Rotation,
            Scale = this.LocalScale
        };
    }
    public static TransformData FormTransData(LocalTransform trans)
    {
        return new TransformData
        {
            Position = trans.Position,
            Rotation = trans.Rotation,
            LocalScale = trans.Scale,
            WorldScale = trans.Scale
        };
    }
    public static TransformData FormTransData(Transform trans)
    {
        return new TransformData
        {
            Position = trans.position,
            Rotation = trans.rotation,
            LocalScale = trans.localScale.x,
            WorldScale = trans.lossyScale.x
        };
    }
}
[CustomPropertyDrawer(typeof(TransformData))]//--순서, 위치정보 때문에 상위에서 구현
public class TransformDataDrawer : PropertyDrawer
{
    Rect DrawRect;
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * (property.isExpanded ? 5 : 1);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, label, true);

        

        if (property.isExpanded)
        {
            NextLine(position, 10);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Position"));
            NextLine(position, 10);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Rotation"));
            NextLine(position, 10);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("LocalScale"));
            NextLine(position, 10);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("WorldScale"));
        }
        //else
        {

        }
    }
    void NextLine(Rect pos, float push = 0)
    {
        DrawRect = new Rect(pos.x + push, DrawRect.y + EditorGUIUtility.singleLineHeight, pos.width - push, EditorGUIUtility.singleLineHeight);
    }
}
