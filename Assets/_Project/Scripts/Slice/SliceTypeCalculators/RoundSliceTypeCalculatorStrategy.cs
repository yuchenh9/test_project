using System.Collections.Generic;
using DynamicMeshCutter;
using UnityEngine;

namespace _Project
{
    public class RoundSliceTypeCalculatorStrategy : ISliceTypeCalculatorStrategy
    {
        public PlaneData Calculate(SliceInfo sliceInfo)
        {
            var step = sliceInfo.SliceIndex + 1;
            var axisNormalized = sliceInfo.SlicingAxis.normalized;
            var absAxis = new Vector3(Mathf.Abs(axisNormalized.x), Mathf.Abs(axisNormalized.y),
                Mathf.Abs(axisNormalized.z));

            var totalLength = Vector3.Dot(sliceInfo.StartBounds.size, absAxis);
            var stepSize = totalLength / sliceInfo.SliceCount;
            var begin = sliceInfo.StartBounds.center - Vector3.Scale(sliceInfo.StartBounds.size / 2, axisNormalized);

            var slicePosition = begin + axisNormalized * (stepSize * step);

            var separationDistance = sliceInfo.Separation * (step - 1);
            slicePosition += axisNormalized * separationDistance;

            return new PlaneData(slicePosition, sliceInfo.SlicingAxis);
        }
        public IEnumerable<MeshTarget> GetNextObjectsForCut(IEnumerable<MeshTarget> slicedObjects)
        {
            return slicedObjects;
        }
    }
}