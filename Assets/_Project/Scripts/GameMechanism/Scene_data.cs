using UnityEngine;
using System.Collections.Generic;
using System;
using _Project;
using DynamicMeshCutter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SceneData : MonoBehaviour
{
    public static SceneData Instance { get; private set; }
    [SerializeField] private SliceManager sliceManager;
    [SerializeField] public GameObject selectedObject;
    [SerializeField] private ToggleGroup selectedPrefabToggleGroup;
    [SerializeField] private ToggleGroup sliceTypeToggleGroup;
    [SerializeField] private TextMeshProUGUI sliceCountField;
    [SerializeField] private Slider slider;    
    public List<GameObject> foodObjects;
    public List<GameObject> vesselObjects;
    public static Dictionary<string, SceneInfo> scenes = new Dictionary<string, SceneInfo>();
 private void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Uncomment if you want this controller to persist across scenes.
        // DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
    }

    public void ChangeUI(SceneInfo newScene)
    {
    }

    public void ButtonCutClicked()
    {
        MeshTarget selectedPrefab=selectedObject.GetComponent<MeshTarget>();
        var sliceType = sliceTypeToggleGroup.GetFirstActiveToggle().GetComponent<SliceTypeToggleInfoElementUI>().SliceType;
        StartCoroutine(sliceManager.Slice(selectedPrefab, SliceCount, CutAngle, UtilityHelper.GetCalculator(sliceType)));
    }
    
    private int SliceCount => int.Parse(sliceCountField.text);
    private Vector3 CutAngle => UtilityHelper.AngleToAxis(slider.value);
}
