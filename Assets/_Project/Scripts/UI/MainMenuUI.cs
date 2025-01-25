using System;
using _Project;
using DynamicMeshCutter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private SliceManager sliceManager;
    [SerializeField] private MeshTarget selectedPrefab;
    [SerializeField] private ToggleGroup sliceTypeToggleGroup;
    [SerializeField] private TMP_InputField sliceCountField;
    [SerializeField] private Slider slider;
    
    private int SliceCount => int.Parse(sliceCountField.text);
    private Vector3 CutAngle => UtilityHelper.AngleToAxis(slider.value);

    public void ButtonCutClicked()
    {
        var sliceType = sliceTypeToggleGroup.GetFirstActiveToggle().GetComponent<SliceTypeToggleInfoElementUI>().SliceType;
        StartCoroutine(sliceManager.Slice(selectedPrefab, SliceCount, CutAngle, UtilityHelper.GetCalculator(sliceType)));
    }
}