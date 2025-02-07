using System;
using _Project;
using DynamicMeshCutter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    
    [SerializeField] private SliceManager sliceManager;
    [SerializeField] public GameObject selectedObject;
    [SerializeField] private ToggleGroup selectedPrefabToggleGroup;
    [SerializeField] private ToggleGroup sliceTypeToggleGroup;
    [SerializeField] private TMP_InputField sliceCountField;
    [SerializeField] private Slider slider;     
    
    public void ButtonCutClicked()
    {
        MeshTarget selectedPrefab=selectedObject.GetComponent<MeshTarget>();
        var sliceType = sliceTypeToggleGroup.GetFirstActiveToggle().GetComponent<SliceTypeToggleInfoElementUI>().SliceType;
        StartCoroutine(sliceManager.Slice(selectedPrefab, SliceCount, CutAngle, UtilityHelper.GetCalculator(sliceType)));
    }
    
    private int SliceCount => int.Parse(sliceCountField.text);
    private Vector3 CutAngle => UtilityHelper.AngleToAxis(slider.value);
    
}
