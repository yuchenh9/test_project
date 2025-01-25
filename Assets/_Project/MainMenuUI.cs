using System;
using _Project;
using DynamicMeshCutter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private SliceManager sliceManager;
    [SerializeField] private MeshTarget targetObject;
    [SerializeField] private SliceType planeCalculator;
    [SerializeField] private TMP_InputField sliceCountField;
    [SerializeField] private Slider slider;
    
    private int SliceCount => int.Parse(sliceCountField.text);
    private Vector3 CutAngle => AngleToAxis(slider.value);

    public void ButtonCutClicked()
    {
        StartCoroutine(sliceManager.Slice(targetObject, SliceCount, CutAngle, UtilityHelper.GetCalculator(planeCalculator)));
    }

    private Vector3 AngleToAxis(float cutAngle)
    {
        var radians = cutAngle * Mathf.Deg2Rad;

        var x = Mathf.Cos(radians);
        var y = Mathf.Sin(radians);

        return new Vector3(x, y, 0).normalized;
    }
}
