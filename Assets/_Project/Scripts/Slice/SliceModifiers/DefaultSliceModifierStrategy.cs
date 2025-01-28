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
            item.transform.rotation = target.transform.rotation;
            var meshCollider = item.AddComponent<MeshCollider>();
            var rigidbody = item.AddComponent<Rigidbody>();
            
            meshCollider.convex = true;
        }
        yield return null;
    }
}