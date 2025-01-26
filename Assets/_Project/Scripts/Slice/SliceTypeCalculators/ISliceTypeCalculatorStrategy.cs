using System.Collections.Generic;
using _Project.Scripts.Slice.Structs;
using DynamicMeshCutter;
using UnityEngine;

namespace _Project.Scripts.Slice.SliceTypeCalculators
{
    public interface ISliceTypeCalculatorStrategy
    {
        PlaneData Calculate(SliceInfo sliceInfo);
        IEnumerable<MeshTarget> GetNextObjectsForCut(IEnumerable<MeshTarget> slicedObjects);
        bool ValidateInputValues(MeshTarget targetObject, int sliceCount, Vector3 slicingAxis);
    }
}