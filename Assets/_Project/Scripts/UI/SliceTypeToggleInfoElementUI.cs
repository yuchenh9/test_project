using _Project.Scripts.Slice.Structs;
using UnityEngine;

namespace _Project.Scripts.UI
{
    public class SliceTypeToggleInfoElementUI : MonoBehaviour
    {
        [SerializeField] private SliceType sliceType;
    
        public SliceType SliceType => sliceType;
    }
}
