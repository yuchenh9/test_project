
using UnityEngine;

namespace _Project
{
    [System.Serializable]
    public class PlaneData
    {
        public Vector3 Position;
        public Vector3 Normal;

        public PlaneData(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }
    }
}