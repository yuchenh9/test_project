using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Project;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

public class ObiSoftbodySliceModifierStrategy : ISliceModifierStrategy
{
    public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects)
    {
        var coroutines = new List<IEnumerator>();
        var blueprints = new Dictionary<ObiSoftbody, ObiSoftbodySurfaceBlueprint>();

        foreach (var obj in objects.Select(x=>x.gameObject))
        {
            var meshFilter = obj.GetComponent<MeshFilter>();
            var softbody = obj.AddComponent<ObiSoftbody>();
            var skinner = obj.AddComponent<ObiSoftbodySkinner>();

            var blueprint = ScriptableObject.CreateInstance<ObiSoftbodySurfaceBlueprint>();
            blueprint.inputMesh = meshFilter.mesh;

            coroutines.Add(blueprint.Generate());
            blueprints.Add(softbody, blueprint);
        }
    
        yield return coroutineHost.StartCoroutine(UtilityHelper.RunAllCoroutines(coroutines));
    
        foreach (var item in blueprints)
        {
            item.Key.softbodyBlueprint = item.Value;
        }
    }
}