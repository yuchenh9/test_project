using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    [RequireComponent(typeof(Toggle))]
    public class SelectedPrefabToggleInfoElementUI : MonoBehaviour
    {
        [SerializeField] private MainMenuUI mainMenuUI;
    
        public UnityEvent toggleSelected;
        public UnityEvent toggleDeselected;
    
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            _toggle.onValueChanged.AddListener(OnValueChanged);
        }
        private void OnDisable()
        {
            _toggle.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(bool value)
        {
            if (value)
                toggleSelected?.Invoke();
            else
                toggleDeselected?.Invoke();
        }
    }
}
