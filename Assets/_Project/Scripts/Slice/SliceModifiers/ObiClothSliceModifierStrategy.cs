using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Project;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

public class ObiClothSliceModifierStrategy : ISliceModifierStrategy
{
    public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects, GameObject target)
    {
        var coroutines = new List<IEnumerator>();
        var blueprints = new Dictionary<ObiCloth, ObiClothBlueprint>();

        foreach (var obj in objects.Select(x=>x.gameObject))
        {
            var meshFilter = obj.GetComponent<MeshFilter>();
            var cloth = obj.AddComponent<ObiCloth>();
            var clothRenderer = obj.AddComponent<ObiClothRenderer>();

            var blueprint = ScriptableObject.CreateInstance<ObiClothBlueprint>();
            blueprint.inputMesh = meshFilter.mesh;

            coroutines.Add(blueprint.Generate());
            blueprints.Add(cloth, blueprint);
        }
    
        yield return coroutineHost.StartCoroutine(UtilityHelper.RunAllCoroutines(coroutines));
    
        foreach (var item in blueprints)
        {
            item.Key.clothBlueprint = item.Value;
        }
    }
}