using System.Collections.Generic;
using DynamicMeshCutter;
using UnityEngine;

namespace _Project
{
    public interface ISliceTypeCalculatorStrategy
    {
        PlaneData Calculate(SliceInfo sliceInfo);
        IEnumerable<MeshTarget> GetNextObjectsForCut(IEnumerable<MeshTarget> slicedObjects);
        bool ValidateInputValues(MeshTarget targetObject, int sliceCount, Vector3 slicingAxis);
    }
}