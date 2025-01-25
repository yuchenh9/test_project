using System.Collections;
using System.Collections.Generic;
using _Project;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

public class DefaultSliceModifierStrategy : ISliceModifierStrategy
{
    public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects)
    {
        yield return null;
    }
}