using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class EcsAnimDTMono : MonoBehaviour
{
    [SerializeField] public ECSAnimationDataTable DT;

    public void OnEnable()
    {
        //var temp = ECSAnimationDataTable.Instance;
        {/*
            var _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ECSAnimationDataTable>("Assets/Tool/ECS Animation DT.asset");
            var monoImporter = UnityEditor.AssetImporter.GetAtPath("Assets/Tool/EcsAnimDTMono.cs") as UnityEditor.MonoImporter;


            var names = new string[] { "DT" };
            var valus = new ECSAnimationDataTable[] { _instance };

            monoImporter.SetDefaultReferences(names, valus);
            monoImporter.SaveAndReimport();*///활성화시 계속 로딩
        }
    }
    /*
    public void Update()
    {
        
        if (ECSAnimationDataTable.Instance == null)
        {
            var _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ECSAnimationDataTable>("Assets/Tool/ECS Animation DT.asset");
            var monoImporter = UnityEditor.AssetImporter.GetAtPath("Assets/Tool/EcsAnimDTMono.cs") as UnityEditor.MonoImporter;


            var names = new string[] { "DT" };
            var valus = new ECSAnimationDataTable[] { _instance };

            monoImporter.SetDefaultReferences(names, valus);
            monoImporter.SaveAndReimport();
        }
        
        //--- 됬다!! / 내가 멍청해서 안된거였어!
    }*/
}