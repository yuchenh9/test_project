using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderWithTextElementUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI text;

    private void OnEnable()
    {
        slider.onValueChanged.AddListener(OnValueChanged);
    }
    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnValueChanged);
    }
    private void OnValueChanged(float value)
    {
        text.text = value.ToString(CultureInfo.InvariantCulture);
    }
}