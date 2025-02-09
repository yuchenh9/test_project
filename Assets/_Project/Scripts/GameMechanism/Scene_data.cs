using UnityEngine;
using System.Collections.Generic;
using System;
using _Project;
using DynamicMeshCutter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
public class SceneData : MonoBehaviour
{
    public static SceneData Instance { get; private set; }
    [SerializeField] private SliceManager sliceManager;
    [SerializeField] public List<MeshTarget> selectedObjects;
    public GameObject vessels;
    public GameObject selectedGameobject;
    public Camera camera;
    [SerializeField] private ToggleGroup selectedPrefabToggleGroup;
    [SerializeField] private ToggleGroup sliceTypeToggleGroup;
    [SerializeField] private TextMeshProUGUI sliceCountField;
    [SerializeField] private Slider slider;    
    public float distanceBetweenScenes=1f;
    public int selectedSceneIndex=1;
    public List<GameObject> scenes=new List<GameObject>();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        
        foreach(MeshTarget selectedObject in selectedObjects){
            //selectedObject.transform.position=new Vector3(0f,0f,0f);

            int scenesCount=scenes.Count;
            GameObject newScene = new GameObject($"scene{scenesCount}");
            scenes.Add(newScene);
            newScene.transform.parent=gameObject.transform;
            newScene.transform.position=new Vector3(distanceBetweenScenes*scenesCount, 0f, 0f);

            GameObject foods= new GameObject("foods");
            foods.transform.parent=newScene.transform;
            foods.transform.localPosition=new Vector3(0f,0f,0f);

            //selectedObject.transform.parent=foods.transform;
            //selectedObject.transform.localPosition=new Vector3(0f,0f,0f);

            GameObject Scene_vessels=Instantiate(vessels, 
                                  newScene.transform.position, 
                                  newScene.transform.rotation);
            Scene_vessels.transform.parent=newScene.transform;
            Scene_vessels.transform.localPosition=new Vector3(0f,0f,0f);
            ChangeCameraPosition(selectedSceneIndex);
            
        }
    }

    public void ChangeCameraPosition(int selectedSceneIndex){

        Vector3 camPos = camera.transform.position;
        
        camPos.x = scenes[selectedSceneIndex].transform.position.x;

        camera.transform.position = camPos;
    }
    public void ButtonCutClicked()
    {
        //List<MeshTarget> selectedPrefabs=selectedObjects.Select(i=>i.GetComponent<MeshTarget>()).ToList();
        MeshTarget selectedPrefab = selectedObjects[selectedSceneIndex];



        //MeshTarget selectedPrefab=scenes[selectedSceneIndex].transform.Find("foods").transform.GetChild(0).GetComponent<MeshTarget>();
        //var sliceType = sliceTypeToggleGroup.GetFirstActiveToggle().GetComponent<SliceTypeToggleInfoElementUI>().SliceType;
        if (sliceManager == null)
        Debug.LogError("sliceManager is null.");
        if (selectedPrefab == null)
            Debug.LogError("selectedPrefab is null.");
        if (SliceCount <= 0)
            Debug.LogError("SliceCount is invalid.");
        if (new Vector3(1, 0, 0).normalized == null)
            Debug.LogError("Direction vector is null.");
        if (UtilityHelper.GetCalculator() == null)
        Debug.LogError("Calculator for sliceType is null.");
        if (selectedPrefab!=null)
        {
            Transform foodsTransform=scenes[selectedSceneIndex].transform.Find("foods");
            StartCoroutine(sliceManager.Slice(foodsTransform,selectedPrefab, SliceCount, new Vector3(1, 0, 0).normalized, UtilityHelper.GetCalculator()));
    
        } else {

            Debug.LogError("selectedPrefab is null!");
        }
        
    }
    
    private int SliceCount => int.Parse(sliceCountField.text);
    private Vector3 CutAngle => UtilityHelper.AngleToAxis(slider.value);
}
