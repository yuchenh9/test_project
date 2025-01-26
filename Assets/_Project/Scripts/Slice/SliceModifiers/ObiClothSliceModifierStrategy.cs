using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

namespace _Project.Scripts.Slice.SliceModifiers
{
    public class ObiClothSliceModifierStrategy : ISliceModifierStrategy
    {
        public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects, GameObject target)
        {
            var coroutines = new List<IEnumerator>();
            var blueprints = new Dictionary<ObiCloth, ObiClothBlueprint>();

            foreach (var obj in objects.Select(x=>x.gameObject))
            {
                var allComponents = obj.AddAllComponentCopy(target, new []{"clothBlueprint", "cloth", "GameobjectRoot"});
                var meshFilter = obj.GetComponent<MeshFilter>();
                var cloth = obj.GetComponent<ObiCloth>();

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
}