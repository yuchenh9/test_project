using System.Collections.Generic;
using DynamicMeshCutter;

namespace _Project
{
    public interface ISliceTypeCalculatorStrategy
    {
        PlaneData Calculate(SliceInfo sliceInfo);
        IEnumerable<MeshTarget> GetNextObjectsForCut(IEnumerable<MeshTarget> slicedObjects);
    }
}