using System.Collections;
using _Project.Scripts.Slice.SliceModifiers;
using _Project.Scripts.Slice.SliceTypeCalculators;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

namespace _Project.Scripts.Slice
{
    public class SliceManager : MonoBehaviour
    {
        [SerializeField] private CustomSlicerBehaviour defaultSlicer;

        public IEnumerator Slice(MeshTarget target, int sliceCount, Vector3 axis, ISliceTypeCalculatorStrategy planeCalculator)
        {
            yield return StartCoroutine(defaultSlicer.Cut(target, sliceCount, axis, planeCalculator));
            Debug.Log("Objects have been sliced");

            yield return StartCoroutine(SlicedObjectsModify(target.gameObject));
        }
    
        public IEnumerator RoundSlice(MeshTarget target, int sliceCount)
        {
            //TODO: Realize Method
            yield return null;
        }
        private IEnumerator SlicedObjectsModify(GameObject target)
        {
            var cutStrategy = GetStrategy(target.gameObject);
            yield return cutStrategy.Modify(this, defaultSlicer.SlicedObjects, target);
            Debug.Log("Sliced objects modified");
        }

        private ISliceModifierStrategy GetStrategy(GameObject target)
        {
            if (target.TryGetComponent<ObiSoftbody>(out _))
            {
                return new ObiSoftbodySliceModifierStrategy();
            }
            if (target.TryGetComponent<ObiCloth>(out _))
            {
                return new ObiClothSliceModifierStrategy();
            }
            else
            {
                return new DefaultSliceModifierStrategy();
            }
        }
    }
}