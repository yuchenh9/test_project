
using UnityEngine;

namespace _Project
{
    public struct PlaneData
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