using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

namespace _Project.Scripts.Slice.SliceModifiers
{
    public class ObiSoftbodySliceModifierStrategy : ISliceModifierStrategy
    {
        public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects, GameObject target)
        {
            var coroutines = new List<IEnumerator>();
            var blueprints = new Dictionary<ObiSoftbody, ObiSoftbodySurfaceBlueprint>();

            foreach (var obj in objects.Select(x=>x.gameObject))
            {
                var allComponents = obj.AddAllComponentCopy(target, new []{"softbodyBlueprint", "softbody", "GameobjectRoot"});

                var meshFilter = obj.GetComponent<MeshFilter>();
                var softbody = obj.GetComponent<ObiSoftbody>();

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
}