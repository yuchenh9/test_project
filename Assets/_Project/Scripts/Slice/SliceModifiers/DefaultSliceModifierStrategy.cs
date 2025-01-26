using System.Collections;
using System.Collections.Generic;
using _Project;
using DynamicMeshCutter;
using Obi;
using Unity.VisualScripting;
using UnityEngine;

public class DefaultSliceModifierStrategy : ISliceModifierStrategy
{
    public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects, GameObject target)
    {
        foreach (var item in objects)
        {
            var meshCollider = item.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            
            var allComponents = item.gameObject.AddAllComponentCopy(target, new []{"GameobjectRoot"});
        }
        yield return null;
    }
}