using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
//using static Animation.EcsAnimationController;
using Animation;
using UnityEditor.UIElements;
using System.Linq;
using System.Collections.Generic;
using Unity.Transforms;

public class ECSAnimation : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    ECSAnimationDataTable TargetDT;
    Humanoid PawnHumanoid;

    bool IsForce = false;
    bool IsEdited = false;

    UnityEditor.UIElements.ObjectField DataTableField;
    ObjectField PawnField;

    ScrollView AnimList;
    VisualElement AnimListContainer;

    VisualElement root;

    int SelectAnimListIndex = -1;
    int SelectKeyPointIndex = -1;
    int SelectKeyPointAnimData = -1;

    bool IsPlaying = false;
    DateTime StartPlayTime;
    float PlayTime = 0;
    bool IsUpated = false;
    //public List<Animation.AnimationInfo> AnimInfo;

    Label KeyPointLabel;
    ScrollView AnimTimeline;
    Label AnimLengthLabel;
    Button TimeLineButton;
    Slider ZoomSlider;
    VisualElement KeyPointPanel;
    FloatField TimelineLength;
    VisualElement OutOfTimePanel;
    FloatField KeyPointTime;
    Button RemoveKeyPoint;
    Toggle AutoKeying;
    Button GetPrePose;
    Button PoseUpload;
    Label StateLabel;
    Button PlayButton;

    Toggle Looping;
    FloatField FadeTime;

    //https://blog.naver.com/mshan2923/222693509204  참고

    [MenuItem("Window/ECSAnimation")]
    public static void ShowExample()
    {
        ECSAnimation wnd = GetWindow<ECSAnimation>();
        wnd.titleContent = new GUIContent("ECSAnimation");

        {
            var _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ECSAnimationDataTable>("Assets/Tool/EcsAnimDTMono.asset");//Data
            var monoImporter = UnityEditor.AssetImporter.GetAtPath("Assets/Tool/EcsAnimDTMono.cs") as UnityEditor.MonoImporter;//Target

            var names = new string[] { "DT" };
            var valus = new UnityEngine.Object[] { _instance };
            monoImporter.SetDefaultReferences(names, valus);
        }
    }

    public void CreateGUI()
    {
        minSize = new Vector2(400, 450);

        root = rootVisualElement;
        // Each editor window contains a root VisualElement object

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        //VisualElement label = new Label("Hello World! From C#");
        //root.Add(label);

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();// 포커스 풀리면 다시 생성? 
        root.Add(labelFromUXML);
        /*
        selectLabel = new Label("None") { name = "SelectLabel"};
        selectLabel.StretchToParentSize();

        rootVisualElement.Q<VisualElement>("TargetPanel").hierarchy.Add(selectLabel);
        */



        {
            DataTableField = rootVisualElement.Q<UnityEditor.UIElements.ObjectField>("DataTable");

            PawnField = rootVisualElement.Q<ObjectField>("PawnField");

            AnimList = rootVisualElement.Q<ScrollView>("AnimationList");
            AnimListContainer = AnimList.Q<VisualElement>("unity-content-container");

            KeyPointLabel = rootVisualElement.Q<Label>("KeyPointVaule");
            AnimLengthLabel = rootVisualElement.Q<Label>("AnimLengthVaule");

            TimeLineButton = rootVisualElement.Q<Button>("TimelineButton");

            ZoomSlider = rootVisualElement.Q<Slider>("ZoomSlider");

            KeyPointPanel = rootVisualElement.Q<VisualElement>("KeyPointPanel");
            TimelineLength = rootVisualElement.Q<FloatField>("TimelineLength");

            OutOfTimePanel = rootVisualElement.Q<VisualElement>("OutOfTimePanel");
            AnimTimeline = rootVisualElement.Q<ScrollView>("AnimTimeline");

            RemoveKeyPoint = rootVisualElement.Q<Button>("RemoveKeyPoint");

            KeyPointTime = rootVisualElement.Q<FloatField>("KeyPointTime");

            AutoKeying = rootVisualElement.Q<Toggle>("AutoKeying");
            GetPrePose = rootVisualElement.Q<Button>("GetPrePose");
            PoseUpload = rootVisualElement.Q<Button>("PoseUpload");

            StateLabel = rootVisualElement.Q<Label>("State");

            PlayButton = rootVisualElement.Q<Button>("Play");

            Looping = rootVisualElement.Q<Toggle>("Looping");
            FadeTime = rootVisualElement.Q<FloatField>("FadeTime");
            //======= 콜백함수 연결
        }//Query

        //StartPlayTime = DateTime.Now;
        PlayTime = 0;

        SaveLoad.CryptionLoad<List<Animation.AnimationInfo>>("Data", "ECS_AnimationData_Cryption", "data", out var data);
        if (TargetDT != null)
        {
            IsUpated = true;
            TargetDT.AnimInfo = data;
        }
        ChangedAnimation();//필드값 적용

        {
            DataTableField.RegisterValueChangedCallback(OnChangeDataTableField);
            PawnField.RegisterValueChangedCallback(OnChangePawnField);
            ZoomSlider.RegisterValueChangedCallback(OnChangeZoomScale);
            ZoomSlider.Q<TextField>("unity-text-field").RegisterValueChangedCallback(OnChangeZoomScale);
            TimelineLength.RegisterValueChangedCallback(OnChangeTimelineMaxLength);
            KeyPointTime.RegisterValueChangedCallback(OnChangeKeyPointTime);
            AutoKeying.RegisterValueChangedCallback(OnChangeAutoKeying);
            Looping.RegisterValueChangedCallback(OnLooping);
            FadeTime.RegisterValueChangedCallback(OnFadeTime);
            rootVisualElement.Q<Button>("AddKeyPoint").RegisterCallback<PointerDownEvent>(OnResetKeyPoint, TrickleDown.TrickleDown);
            TimeLineButton.RegisterCallback<PointerMoveEvent>(OnTimelineHover, TrickleDown.TrickleDown);
            TimeLineButton.RegisterCallback<PointerDownEvent>(OnTimelineDown, TrickleDown.TrickleDown);
            RemoveKeyPoint.RegisterCallback<PointerDownEvent>(OnRemoveKeyPoint, TrickleDown.TrickleDown);
            GetPrePose.RegisterCallback<PointerDownEvent>(OnGetPrePose, TrickleDown.TrickleDown);
            PoseUpload.RegisterCallback<PointerDownEvent>(OnPoseUpdate, TrickleDown.TrickleDown);
            rootVisualElement.Q<Button>("GoToStart").RegisterCallback<PointerDownEvent>(OnStartAnimation, TrickleDown.TrickleDown);
            PlayButton.RegisterCallback<PointerDownEvent>(OnPlayAnimation, TrickleDown.TrickleDown);
            rootVisualElement.Q<Button>("GoToEnd").RegisterCallback<PointerDownEvent>(OnEndAnimation, TrickleDown.TrickleDown);

        }//RegisterValue
    }

    private void OnFocus()
    {
        IsForce = true;

    }
    private void OnLostFocus()
    {
        IsForce = false;
    }
    private void OnInspectorUpdate()
    {
        if (IsUpated == false && TargetDT != null)
        {
            SaveLoad.Load<List<Animation.AnimationInfo>>("Data", "ECS_AnimationData", "data", out var data);
            TargetDT.AnimInfo = data;
        }

        {
            if (TargetDT == null)
            {
                SetStateLabel("Not Select DataTable", Color.yellow * 0.5f);
                return;
            }
            else if (PawnHumanoid == null)
            {
                SetStateLabel("Not Select Pawn", Color.yellow * 0.5f);
            }
            else if (SelectAnimListIndex < 0)
            {
                SetStateLabel("Not Select Animation", Color.yellow * 0.5f);
            }
            else if (AutoKeying.value)
            {
                SetStateLabel("--Auto Keying--", Color.green * 0.75f);
            }
        }//Set StateLable

        if (AnimListContainer.hierarchy.childCount
            < TargetDT.AnimInfo.Count)
        {
            var LButton = new Button() { name = "Anim_" + AnimListContainer.hierarchy.childCount };
            LButton.style.alignContent = new StyleEnum<Align>() { keyword = StyleKeyword.Auto, value = Align.Stretch };
            LButton.style.maxHeight = 25;
            LButton.style.height = 25;
            LButton.text = TargetDT.AnimInfo[AnimListContainer.hierarchy.childCount].name;
            AnimList.Add(LButton);

            //LButton.clicked += new System.Action(AnimListContainerClicked);//-- 이벤트만줌 정보X
            //LButton.clickable.clickedWithEventInfo += Clickable_clickedWithEventInfo;//-- 되긴할껀데
            LButton.RegisterCallback<PointerDownEvent>(OnChangeAnimation, TrickleDown.TrickleDown);
        }//Add AnimationList Button

        if (SelectAnimListIndex >= 0)
        {
            var animData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData;
            KeyPointLabel.text = animData.Count.ToString();

            if (animData.Count > 0)
            {
                var last = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count - 1;
                AnimLengthLabel.text = TargetDT.AnimInfo[SelectAnimListIndex]
                    .AnimationData[last].time.ToString();
            }
            else
            {
                AnimLengthLabel.text = "None(KeyPoint is Less then 1)";
            }

            {
                var last = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count - 1;

                if (last >= 0)
                {
                    float animLength = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[last].time;//Sort 되니까
                    var ReverZoomScale = (1 / ZoomSlider.value);

                    if (KeyPointPanel.hierarchy.childCount != TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count)
                    {
                        KeyPointPanel.hierarchy.Clear();

                        foreach (var v in TargetDT.AnimInfo[SelectAnimListIndex].AnimationData)
                        {
                            CreateKeyPoint(v.time * 100f * ReverZoomScale);
                        }

                        OutOfTimePanel.style.left = animLength * 100f * ReverZoomScale;
                    }

                    AnimTimeline.horizontalScroller.highValue = Mathf.Max(0, TimelineLength.value * 100f * ReverZoomScale - this.position.width);
                    //AnimTimeline.StretchToParentSize();//문제 발생 (패널 증발)
                }

            }//Position Setup
        }//AnimLengthLabel , Position Setup

        {
            var vaule = DateTime.Now.Subtract(StartPlayTime);
            float Spend = vaule.Minutes * 60 + vaule.Seconds + vaule.Milliseconds * 0.001f;

            if (IsPlaying)
            {
                if (SelectAnimListIndex >= 0 && PawnHumanoid != null)
                {

                    bool isNotEmpty = TargetDT.AnimInfo[SelectAnimListIndex].GetLastAnimData(out var last);

                    if (isNotEmpty)
                    {
                        var percent = (PlayTime / last.time) * 100;

                        if (percent < 100)
                        {
                            PlayTime += Spend * ZoomSlider.value;

                            PlayButton.text = $"  > [{PlayTime:N2} ({percent:F2}%)]";
                            StartPlayTime = DateTime.Now;
                        }
                        else
                        {
                            IsPlaying = false;
                            PlayButton.text = $"  > [{last.time:N2} (100%)]";
                        }
                    }

                }
                else
                {
                    PlayButton.text = "[  X  ]";
                }
            }else
            {
                if (SelectAnimListIndex < 0 || PawnHumanoid == null)
                {
                    PlayButton.text = "[  X  ]";
                }else
                {
                    bool isNotEmpty = TargetDT.AnimInfo[SelectAnimListIndex].GetLastAnimData(out var last);
                    var percent = (PlayTime / last.time) * 100;

                    if (percent < 100)
                        PlayButton.text = $"  || [{PlayTime:N2} ({percent:F2}%)]";
                    else
                        PlayButton.text = $"  > [{last.time:N2} (100%)]";
                }
            }
        }//PlayButton State

        {
            //AnimData들 모두 Loop 돌면서 , (대상 - 현제 시간)을 비교하고 늘어나면 break  

            if (SelectAnimListIndex >= 0)
            {
                if (TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count > 0 && IsPlaying)
                {
                    int AnimPart = 0;

                    for (int i = 0; i < TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count; i++)
                    {
                        if (TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[i].time >= PlayTime)
                        {
                            break;
                        }
                        else
                        {
                            AnimPart = i + 1;
                        }
                    }

                    float AnimPartRate = 0;
                    AnimationPose AnimPartData = default;
                    AnimationPose PreAnimPartData = default;

                    if (AnimPart == 0)
                    {
                        AnimPartData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[AnimPart];
                        AnimPartRate = PlayTime / AnimPartData.time;
                    }
                    else if (AnimPart >= TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count)
                    {
                        AnimPartRate = 1;
                        AnimPartData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Last();
                    }
                    else
                    {
                        PreAnimPartData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[AnimPart - 1];
                        AnimPartData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[AnimPart];

                        AnimPartRate = (PlayTime - PreAnimPartData.time) / (AnimPartData.time - PreAnimPartData.time);
                    }

                    float curvedRate = 0;
                    if (AnimPartData.LerpCurve == null)
                    {
                        curvedRate = AnimPartRate;
                    }
                    else
                    {
                        curvedRate = AnimPartData.LerpCurve.Evaluate(AnimPartRate);
                    }

                    for (int i = 0; i < PawnHumanoid.HumanoidParts.Length; i++)
                    {
                        if (PawnHumanoid.HumanoidParts[i].PartObj != null)
                        {
                            if (AnimPart < TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count && AnimPart > 0)
                            {
                                var pastData = PreAnimPartData.transform[i];
                                var currectData = AnimPartData.transform[i];

                                var resultData = new TransformData
                                {
                                    Position = Vector3.Lerp(pastData.Position, currectData.Position, Mathf.Clamp01(curvedRate)),
                                    Rotation = Quaternion.Lerp(pastData.Rotation, currectData.Rotation, Mathf.Clamp01(curvedRate)),
                                    LocalScale = Mathf.Lerp(pastData.LocalScale, currectData.LocalScale, Mathf.Clamp01(curvedRate)),
                                    WorldScale = Mathf.Lerp(pastData.WorldScale, currectData.WorldScale, Mathf.Clamp01(curvedRate))
                                };

                                PawnHumanoid.ApplyTransform(i, resultData);
                            }
                        }
                    }

                }
            }

        }//Editor Animation Preview

        if (IsEdited)
        {
            Debug.Log("Edited");

            SaveLoad.Save(TargetDT.AnimInfo, "Data", "ECS_AnimationData", "data");
            SaveLoad.CryptionSave(TargetDT.AnimInfo, "Data", "ECS_AnimationData_Cryption", "data");

            IsEdited = false;
        }

    }

    private void OnValidate()
    {

    }//Empthy
    private void OnHierarchyChange()
    {
        //Debug.Log("Hierarchy Change");//구조가 바뀔때
    }//Empthy
    private void OnProjectChange()
    {
        //Debug.Log("Project Change");
    }//Empthy
    private void OnDestroy()
    {
        OnEdited();
    }

    #region RegisterCallback
    void OnChangeDataTableField(ChangeEvent<UnityEngine.Object> evt)
    {
        if (evt.newValue is ECSAnimationDataTable)
        {
            TargetDT = evt.newValue as ECSAnimationDataTable;
        }
        else
        {
            TargetDT = null;
        }
    }
    void OnChangePawnField(ChangeEvent<UnityEngine.Object> evt)
    {
        Humanoid humanoid = null;
        if (evt.newValue is Humanoid)
        {
            humanoid = evt.newValue as Humanoid;

            if (humanoid.gameObject.scene.isLoaded)
            {
                PawnHumanoid = humanoid;
                return;
            }
        }
        PawnHumanoid = null;
    }

    void OnChangeAnimation(PointerDownEvent evt)
    {
        //UnityEngine.UIElements.Event

        //Debug.Log("PointerDown : " + evt.position + " / " + evt.localPosition + "\n" + evt.currentTarget.ToString() + " / ");

        var button = evt.currentTarget as VisualElement;

        //Debug.Log($"Receive : {button.name}  / Min : {button.worldBound.position} ~ Max : {button.worldBound.position + button.worldBound.size}" +
        //    $"\n Mouse : {evt.position}");

        for (int i = 0; i < AnimListContainer.childCount; i++)
        {
            if (AnimListContainer.ElementAt(i).Equals(button))
            {
                SelectAnimListIndex = i;
                break;
            }
        }

        ChangedAnimation();

        KeyPointPanel.Clear();
        OutOfTimePanel.style.left = 0;
        DeselectKeyPointIndex();
        SetStateLabel("Changed Animation");
    }
    void OnResetKeyPoint(PointerDownEvent evt)
    {
        if (SelectAnimListIndex >= 0)
        {
            //var animData = Target.TargetDT.AnimInfo[SelectAnimListIndex].AnimationData;
            Debug.Log("AddKeyPoint / ");



            TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Clear();
            KeyPointPanel.Clear();
            OutOfTimePanel.style.left = 0;

            DeselectKeyPointIndex();
        }
    }

    void OnTimelineDown(PointerDownEvent evt)
    {
        if (SelectAnimListIndex >= 0)
        {
            float SelectTime = evt.localPosition.x * 0.01f * ZoomSlider.value;
            var animData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData;
            TargetDT.AnimInfo[SelectAnimListIndex]
                .AnimationData.Add(new Animation.AnimationPose(SelectTime));

            TargetDT.AnimInfo[SelectAnimListIndex]
                .AnimationData.Sort((AnimationPose x, AnimationPose y) => x.time.CompareTo(y.time));//Sort

            var Lbutton = CreateKeyPoint(evt.localPosition.x);

            {
                var last = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count - 1;
                float animLength = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[last].time;//Sort 되니까
                OutOfTimePanel.style.left = animLength * 100f * (1 / ZoomSlider.value);
            }

            SelectKeyPoint(SelectTime);
            SetStateLabel("Add KeyPoint");

            OnGetPrePose(evt);

            OnEdited();
        }
    }
    void OnTimelineHover(PointerMoveEvent evt)
    {


        float SelectTime = evt.localPosition.x * 0.01f * ZoomSlider.value;
        var sizeRate = new Vector2
            (
            evt.localPosition.x / TimeLineButton.worldBound.size.x,
            evt.localPosition.y / TimeLineButton.worldBound.size.y
            );

        //Debug.Log("Hover");

        TimeLineButton.tooltip = "SelectTime : " + SelectTime + " / " + TimeLineButton.LocalToWorld(Vector2.zero);//sizeRate.ToString();


        //ZoomSlider.value = 100Pixel 당 ZoomSlider.value초 

    }

    void OnChangeZoomScale(ChangeEvent<float> evt)
    {
        //값 변화에 따라 TimeLineButton Size.x  변경

        UpdateTimeline(evt.newValue);
    }
    void OnChangeZoomScale(ChangeEvent<string> evt)
    {
        UpdateTimeline(float.Parse(evt.newValue));
    }

    void OnChangeTimelineMaxLength(ChangeEvent<float> evt)
    {
        UpdateTimeline(ZoomSlider.value);

        if (SelectAnimListIndex < 0)
            return;
        var last = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count - 1;
        if (last < 0)
            return;
        float animLength = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[last].time;//Sort 되니까
        OutOfTimePanel.style.left = animLength * 100f * (1 / ZoomSlider.value);

    }

    void OnKeyPoint(PointerDownEvent evt)
    {
        var button = evt.currentTarget as VisualElement;

        float selectTime = 0;

        if (SelectKeyPointIndex >= 0 && SelectKeyPointIndex < KeyPointPanel.hierarchy.childCount)
        {
            KeyPointPanel.ElementAt(SelectKeyPointIndex).style.backgroundColor = new StyleColor(new Color(0.345f, 0.345f, 0.345f));
        }

        for (int i = 0; i < KeyPointPanel.childCount; i++)
        {
            if (KeyPointPanel.ElementAt(i).Equals(button))//evt.cuttectTarget은 안됨
            {
                selectTime = (KeyPointPanel.ElementAt(i).style.left.value.value + 5f) * ZoomSlider.value * 0.01f;
                SelectKeyPointIndex = i;
                break;
            }
        }

        // 시간으로 Target.TargetDT.AnimInfo[SelectAnimListIndex].AnimationData 의 N번째인지 검색
        SelectKeyPointAnimData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData
            .FindIndex(t => Mathf.Approximately(t.time, selectTime));


        KeyPointPanel.ElementAt(SelectKeyPointIndex).style.backgroundColor = new StyleColor(new Color(0.75f, 0, 0, 1));

        KeyPointTime.value = selectTime;

        {
            //TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimDat;a]


            var applyPose = ApplyLocalTrans(TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].transform);
            if (applyPose == false)
                SetStateLabel($"Fail Apply Pose / " +
                    $"SelectAnim : {SelectAnimListIndex} / SelectKeypoint : {SelectKeyPointAnimData} /" +
                    $" Data : {TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].transform}");
            else
                SetStateLabel("Apply Pose maybe");

        }//Pose
    }
    void OnRemoveKeyPoint(PointerDownEvent evt)
    {
        if (SelectAnimListIndex >= 0 && SelectKeyPointIndex >= 0)
        {
            //KeyPointPanel.ElementAt(SelectKeyPointIndex).RemoveFromHierarchy();
            TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.RemoveAt(SelectKeyPointAnimData);

            KeyPointPanel.Clear();
            OutOfTimePanel.style.left = 0;

            DeselectKeyPointIndex();

            SetStateLabel("Removed KeyPoint");

            OnEdited();
        }
    }
    void OnChangeKeyPointTime(ChangeEvent<float> evt)
    {
        //============== 위치 바뀌었을때 선택유지

        //값을 변경시키고 Sort 시킨다음, 이동시키고 , 선택 인덱스 재계산
        if (SelectAnimListIndex >= 0 && SelectKeyPointAnimData >= 0)
        {
            if (Mathf.Approximately(TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].time, evt.newValue))
            {
                return;
            }

            if (SelectKeyPointIndex >= 0)
                KeyPointPanel.ElementAt(SelectKeyPointIndex).style.backgroundColor = new StyleColor(new Color(0.345f, 0.345f, 0.345f));

            //Debug.Log(SelectKeyPointAnimData + " / " + Target.TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count);
            TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData] =
                TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].Time(evt.newValue);

            TargetDT.AnimInfo[SelectAnimListIndex]
                .AnimationData.Sort((AnimationPose x, AnimationPose y) => x.time.CompareTo(y.time));//Sort


            // 시간으로 Target.TargetDT.AnimInfo[SelectAnimListIndex].AnimationData 의 N번째인지 검색
            SelectKeyPointAnimData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData
                .FindIndex(t => Mathf.Approximately(t.time, evt.newValue));

            //SelectKeyPointIndex = SelectKeyPointAnimData;

            KeyPointPanel.ElementAt(SelectKeyPointIndex).style.left = evt.newValue * 100f * (1 / ZoomSlider.value) - 5f;
            KeyPointPanel.ElementAt(SelectKeyPointIndex).style.backgroundColor = new StyleColor(new Color(0.75f, 0, 0, 1));

            int last = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count - 1;
            OutOfTimePanel.style.left = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[last].time * 100f * (1 / ZoomSlider.value);

            SetStateLabel($"Changed {SelectKeyPointAnimData} KeyPoint Time");

            OnEdited();
        }
    }

    void OnChangeAutoKeying(ChangeEvent<bool> evt)
    {
        //KeyPoint가 바뀌어도 AutoKeying 꺼지게
        if (evt.newValue)
        {

        }
        else
        {
            SetStateLabel("Auto Keying Off");

        }
    }
    void OnGetPrePose(PointerDownEvent evt)
    {
        //이전 KeyPoint가 있으면 가져오고 / 없으면 기본값 + KeyPoint가 추가될때도 실행
        if (SelectAnimListIndex >= 0 && SelectKeyPointIndex >= 0)
        {
            if (SelectKeyPointAnimData > 0)
            {
                SetStateLabel("Get Pre Pose");


                float Ltime = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].time;
                var preData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData - 1];

                TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData] =
                    new AnimationPose()
                    {
                        time = Ltime,
                        transform = preData.transform
                    };
                //깊은 복사 되야 하는데 - 참조 복사 안되야함 안돼겠지?

                ApplyLocalTrans(preData.transform);

                OnEdited();
            }
            else
            {
                SetStateLabel("Can't Get Pre Pose / Set Now Pose");
                //KeyPoint가 없는경우 현제 포즈를 데이터에 전달
                /*
                float Ltime = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].time;

                //var applyPose = ApplyLocalTrans(TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].transform);
                TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData] =
                new AnimationPose
                {
                    time = Ltime,
                    transform = PawnHumanoid.HumanoidPartsToTransforms()
                };
                //PawnHumanoid.HumanoidPartsToTransforms();
                Debug.Log($"OnGetPrePose : Call in FirstIndex - Set Data : ");
                */
            }

        }
    }
    void OnPoseUpdate(PointerDownEvent evt)
    {
        if (SelectAnimListIndex >= 0 && SelectKeyPointIndex >= 0)
        {
            if (PawnHumanoid == null)
            {
                SetStateLabel("Not Select TargetPawn", Color.red);
                Debug.LogWarning($"Can't Upload Pose (Not Select TargetPawn) / Anim : {SelectAnimListIndex} , KeyPoint {SelectKeyPointAnimData}");
                return;
            }

            SetStateLabel($"Pose Upload [ {DateTime.Now:hh:mm:ss} ]");
            //TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData]
            //PawnHumanoid 에서 부위 LoclaTransform[]을 가져와서 DT에 전달
            //HumanoidPartsToTransforms


            float Ltime = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].time;
            TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData] = new AnimationPose()
            {
                time = Ltime,
                transform = PawnHumanoid.HumanoidPartsToTransforms()
            };
            

            OnEdited();
        }
        else
        {
            SetStateLabel("Not Select Anim OR KeyPoint", Color.red);
        }
    }

    void OnStartAnimation(PointerDownEvent evt)
    {
        if (SelectAnimListIndex >= 0 && PawnHumanoid != null)
        {
            //StartPlayTime = DateTime.Now;
            IsPlaying = false;
            PlayTime = 0;
            PlayButton.text = $"  || [00:00 (00%)]";

            if (SelectKeyPointAnimData >= 0)
                ApplyLocalTrans(TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[0].transform);
        }
    }
    void OnPlayAnimation(PointerDownEvent evt)
    {
        if (SelectAnimListIndex >= 0 && PawnHumanoid != null)
        {
            IsPlaying = !IsPlaying;

            //var vaule = DateTime.Now.Subtract(StartPlayTime);
            //float Spend = vaule.Minutes * 60 + vaule.Seconds + vaule.Milliseconds * 0.001f;


            if (IsPlaying == false)
            {
                PlayButton.text = $"  || [{PlayTime:N2} (00%)]";//$"  || [00:00 (000%)]";
            }
            else
            {
                StartPlayTime = DateTime.Now;
                PlayButton.text = $"  > [{PlayTime:N2} (00%)]";
            }
        }

    }
    void OnEndAnimation(PointerDownEvent evt)
    {
        if (SelectAnimListIndex >= 0 && PawnHumanoid != null)
        {
            IsPlaying = false;
            //TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[TargetDT.AnimInfo[SelectAnimListIndex].GetLastAnimData()].time
            bool isNotEmpty = TargetDT.AnimInfo[SelectAnimListIndex].GetLastAnimData(out var last);

            if (isNotEmpty)
                PlayTime = last.time;
            PlayButton.text = $"  > [{PlayTime:N2} (100%)]";

            if (SelectKeyPointAnimData >= 0)
                ApplyLocalTrans(TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count - 1].transform);
        }
    }
    void OnLooping(ChangeEvent<bool> evt)
    {
        var temp = TargetDT.AnimInfo[SelectAnimListIndex];
        
        if (evt.newValue)
        {
            temp.NextAnim = SelectAnimListIndex;
        }else
        {
            temp.NextAnim = -1;
        }
        TargetDT.AnimInfo[SelectAnimListIndex] = temp;

        OnEdited();
    }
    void OnFadeTime(ChangeEvent<float> evt)
    {
        if (FadeTime.value < 0)
            FadeTime.value = 0;

        var temp = TargetDT.AnimInfo[SelectAnimListIndex];
        temp.FadeTime = FadeTime.value;

        TargetDT.AnimInfo[SelectAnimListIndex] = temp;

        OnEdited();
    }
    #endregion


    #region Fuction

    bool SelectKeyPoint(float timeVaule)
    {
        if (SelectKeyPointIndex >= 0 && SelectKeyPointIndex < KeyPointPanel.hierarchy.childCount)
        {
            KeyPointPanel.ElementAt(SelectKeyPointIndex).style.backgroundColor = new StyleColor(new Color(0.345f, 0.345f, 0.345f));
        }

        // 시간으로 Target.TargetDT.AnimInfo[SelectAnimListIndex].AnimationData 의 N번째인지 검색
        SelectKeyPointAnimData = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData
            .FindIndex(t => Mathf.Approximately(t.time, timeVaule));

        if (SelectAnimListIndex < 0)
        {
            return false;
        }

        float zoomScaled = 1 / ZoomSlider.value;
        for (int i = 0; i < KeyPointPanel.hierarchy.childCount; i++)
        {
            if (Mathf.Approximately(KeyPointPanel.ElementAt(i).style.left.value.value, timeVaule * 100f * zoomScaled - 5f))
            {
                SelectKeyPointIndex = i;
                break;
            }
        }

        if (SelectKeyPointIndex >= 0)
        {
            KeyPointPanel.ElementAt(SelectKeyPointIndex).style.backgroundColor = new StyleColor(new Color(0.75f, 0, 0, 1));
            KeyPointTime.value = timeVaule;
        }

        {
            SetStateLabel("Get Pose Data");
            ApplyLocalTrans(TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[SelectKeyPointAnimData].transform);
        }

        return true;
    }
    void DeselectKeyPointIndex()
    {
        SelectKeyPointIndex = -1;
        SelectKeyPointAnimData = -1;
        if (KeyPointTime != null)
            KeyPointTime.value = -1;
        OnEdited();
    }
    Button CreateKeyPoint(float LocalPosX)
    {
        var LButton = new Button() { name = "KeyPint" + KeyPointPanel.hierarchy.childCount };
        LButton.style.position = Position.Absolute;//------- Relative 이면 자동 배치 기준

        LButton.style.left = new StyleLength(LocalPosX - 5f);//중앙으로
        //LButton.layout.position = new Vector2(evt.localPosition.x, evt.localPosition.y);
        //LButton.style.translate
        //  = new StyleTranslate(new Translate(new Length(evt.localPosition.x), new Length(0)));//별 차이 없음

        LButton.style.alignContent = new StyleEnum<Align>() { keyword = StyleKeyword.Auto, value = Align.Auto };
        LButton.style.maxHeight = 30;
        LButton.style.height = 30;
        LButton.style.maxWidth = 10;
        LButton.style.width = 10;
        LButton.text = "";

        KeyPointPanel.hierarchy.Add(LButton);
        LButton.RegisterCallback<PointerDownEvent>(OnKeyPoint, TrickleDown.TrickleDown);

        //TimelineButton.style 이 Null 인데?

        if (SelectKeyPointIndex >= 0 && SelectKeyPointIndex < KeyPointPanel.hierarchy.childCount)
        {
            KeyPointPanel.ElementAt(SelectKeyPointIndex).style.backgroundColor = new StyleColor(new Color(0.345f, 0.345f, 0.345f));
        }

        DeselectKeyPointIndex();

        return LButton;
    }//KeyPoint
    void UpdateTimeline(float scale)
    {



        float Llength = (TimelineLength.value / scale) * 100f;
        TimeLineButton.style.maxWidth = Llength;
        TimeLineButton.style.width = Llength;
        KeyPointPanel.style.maxWidth = Llength;
        KeyPointPanel.style.width = Llength;

        for (int i = 0; i < KeyPointPanel.hierarchy.childCount; i++)
        {
            // N초 * 100 * ZoomScale
            float Scaled = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[i].time * 100 * (1 / ZoomSlider.value);
            KeyPointPanel.hierarchy.ElementAt(i).style.left = Scaled - 5f;
        }


        //float animLength = 0;


        if (SelectAnimListIndex < 0)
            return;
        var last = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData.Count - 1;
        if (last < 0)
            return;
        float animLength = TargetDT.AnimInfo[SelectAnimListIndex].AnimationData[last].time;//Sort 되니까
        OutOfTimePanel.style.left = animLength * 100f * (1 / ZoomSlider.value);


        AnimTimeline.style.width = Llength;
    }
    void SetStateLabel(string vaule, Color color = new Color())
    {
        StateLabel.text = vaule;
        StateLabel.style.backgroundColor = new StyleColor(color);
    }
    bool ApplyLocalTrans(TransformData[] trans)
    {
        if (PawnHumanoid == null || trans == null)
        {
            Debug.LogAssertion($"PawnHumanoid : {PawnHumanoid} / Transform : {trans}");
            return false;
        }

        for (int i = 0; i < PawnHumanoid.HumanoidParts.Length; i++)
        {
            var obj = PawnHumanoid.HumanoidParts[i].PartObj;

            if (obj != null)
            {
                obj.transform.localPosition = trans[i].Position;
                obj.transform.localRotation = trans[i].Rotation;
                obj.transform.localScale = Vector3.one * trans[i].LocalScale;
            }
        }
        //PawnHumanoid.HumanoidParts[0].PartObj
        return true;
    }

    #endregion

    void OnEdited()
    {
        IsEdited = true;
    }
    void ChangedAnimation()
    {

        if (SelectAnimListIndex < 0)
            return;

        FadeTime.value = TargetDT.AnimInfo[SelectAnimListIndex].FadeTime;
        Looping.value = (TargetDT.AnimInfo[SelectAnimListIndex].NextAnim == SelectAnimListIndex);

    }

    //===================   이전포즈 ----> 콜백함수 연결
    //====== KeyPoint 선택시 해당 포즈로 하고 , 처음의 KeyPoint만 자동으로 현제포즈를 저장
    //============= 이후엔 AutoKeying 켜있을때만 현제포즈로 저장

    //================== DataTableField가 변경되면 선택인덱스들 초기화

}
