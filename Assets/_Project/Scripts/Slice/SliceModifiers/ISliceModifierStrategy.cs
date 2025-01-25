using System.Collections;
using System.Collections.Generic;
using DynamicMeshCutter;
using UnityEngine;

namespace _Project
{
    public interface ISliceModifierStrategy
    {
        IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects);
    }
}