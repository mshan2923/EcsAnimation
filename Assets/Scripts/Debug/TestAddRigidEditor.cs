using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TestAddRigid))]
public class TestAddRigidEditor : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Debug.Log(position);

        TestAddRigid.AddRigid = GUI.Toggle(position, TestAddRigid.AddRigid, "AddRigid");
        GUI.TextArea(position, "---------");
    }
}