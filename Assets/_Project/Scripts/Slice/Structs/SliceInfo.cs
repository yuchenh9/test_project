using UnityEngine;

namespace _Project.Scripts.Slice.Structs
{
    public class SliceInfo
    {
        public int SliceCount;
        public int SliceIndex;
        public Vector3 SlicingAxis;
        public Bounds StartBounds;
        public float Separation;
    }
}