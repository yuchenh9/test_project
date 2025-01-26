using _Project.Scripts.Slice;
using DynamicMeshCutter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private SliceManager sliceManager;
        [SerializeField] private MeshTarget selectedPrefab;
        [SerializeField] private ToggleGroup selectedPrefabToggleGroup;
        [SerializeField] private ToggleGroup sliceTypeToggleGroup;
        [SerializeField] private TMP_InputField sliceCountField;
        [SerializeField] private Slider slider;
    
        public void SetSelectedPrefab(MeshTarget prefab)
        {
            selectedPrefab = prefab;
        }
    
        public void ButtonCutClicked()
        {
            var sliceType = sliceTypeToggleGroup.GetFirstActiveToggle().GetComponent<SliceTypeToggleInfoElementUI>().SliceType;
            StartCoroutine(sliceManager.Slice(selectedPrefab, SliceCount, CutAngle, UtilityHelper.GetCalculator(sliceType)));
        }
    
        private int SliceCount => int.Parse(sliceCountField.text);
        private Vector3 CutAngle => UtilityHelper.AngleToAxis(slider.value);
    }
}