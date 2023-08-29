using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEcsAnimDT 
{
    public void SetInstance();
}

[CreateAssetMenu(fileName = "ECS Animation DT",menuName = "DT/ECS Animation")]
public class ECSAnimationDataTable : ScriptableObject, IEcsAnimDT
{
    [Header("if Edit to 'time' in <EditWindow>ECSAnimation\nDon't forget NextAnim default Value is '-1'\n",order = 0)]
    [NonReorderable] public List<Animation.AnimationInfo> AnimInfo;

    static ECSAnimationDataTable _instance;
    public static ECSAnimationDataTable Instance 
    {
        get 
        {            
            //그냥 리소스 폴더 사용... 힘들오
            //Resources.Load 

            if (_instance == null)
            {
                EcsAnimDTMono temp = default;
#if UNITY_EDITOR

                bool isNull = false;

                if (temp == null)
                {
                    isNull = true;
                }else if (temp.DT == null)
                {
                    isNull = true;
                }

                
                if (isNull)
                {
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ECSAnimationDataTable>("Assets/Tool/EcsAnimDTMono.asset");//Data
                    var monoImporter = UnityEditor.AssetImporter.GetAtPath("Assets/Tool/EcsAnimDTMono.cs") as UnityEditor.MonoImporter;//Target

                    var names = new string[] { "DT" };
                    var valus = new Object[] { _instance };
                    monoImporter.SetDefaultReferences(names, valus);
                    //monoImporter.SaveAndReimport();
                }
                //UnityEngine.Load

#else
#endif
                //var TempAssetBundle = System.IO.Path.Combine(Application.dataPath, "Tool");// "Assets/Tool/EcsAnimDTMono.cs"
                //var bundle = AssetBundle.LoadFromFile(TempAssetBundle);
                //Debug.Log("sington : " + bundle.name);

                _instance = temp.DT;

                return _instance;
            }
            else
            {
                return _instance;
            }
        }
        private set
        {
            if (_instance == null)
            {
                _instance = value;
            }
        }
    }

    private void OnEnable()
    {
        Instance = this;

        // Ensure there are no public constructors...  
        //typeof(ECSAnimationDataTable).GetConstructors();
    }

    public void SetInstance()
    {
        Instance = this;
    }
}
