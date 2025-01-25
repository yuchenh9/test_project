using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace _Project
{
    public class UtilityHelper
    {
        public static IEnumerator RunAllCoroutines(List<IEnumerator> coroutines)
        {
            var coroutineHandles = coroutines
                .Select(coroutine => CoroutineRunner.instance.StartCoroutine(coroutine)).ToList();

            foreach (var handle in coroutineHandles)
            {
                yield return handle;
            }
        }
        
        public static Bounds GetObjectBounds(GameObject obj)
        {
            if (obj.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                return meshRenderer.bounds;
            }
            else if (obj.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
            {
                return skinnedMeshRenderer.bounds;
            }
            else
            {
                Debug.LogError($"Object {obj.name} does not have a MeshRenderer or SkinnedMeshRenderer");
                return new Bounds();
            }
        }
        
        public static ISliceTypeCalculatorStrategy GetCalculator(SliceType calculator)
        {
            return calculator switch
            {
                SliceType.Linear=> new LinearSliceTypeCalculatorStrategy(),
                SliceType.Radial => null,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}