using System.Collections.Generic;
using System.Linq;
using DynamicMeshCutter;
using UnityEngine;

namespace _Project
{
    public class RoundSliceTypeCalculatorStrategy : ISliceTypeCalculatorStrategy
    {
        private MeshTarget _right;
        private MeshTarget _left;
        private bool _isFirst = true;
        private SliceInfo _lastSliceInfo;
        private int _lastIndex;
        private bool _sameIndex;
        
        public PlaneData Calculate(SliceInfo sliceInfo)
        {
            _lastSliceInfo = sliceInfo;
            var step = sliceInfo.SliceIndex + 1;
            var angle = (step - 1) * (360f / sliceInfo.SliceCount);
            if (angle >= 180)
                angle += 360f / sliceInfo.SliceCount;
            
            return new PlaneData(sliceInfo.StartBounds.center, UtilityHelper.AngleToAxis(angle));
        }

        public IEnumerable<MeshTarget> GetNextObjectsForCut(IEnumerable<MeshTarget> slicedObjects)
        {
            var meshTargets = slicedObjects as MeshTarget[] ?? slicedObjects.ToArray();
            if (_isFirst)
            {
                _right = meshTargets[0];
                _isFirst = false;
            }


            if (_lastSliceInfo.SliceIndex + 1 < _lastSliceInfo.SliceCount / 2)
            {
                return new[] { meshTargets[1] };
            }
            else if (_lastSliceInfo.SliceIndex + 1 == _lastSliceInfo.SliceCount / 2)
            {
                return new[] { _right };
            }
            else if (_lastSliceInfo.SliceIndex + 1 > _lastSliceInfo.SliceCount / 2)
            {
                return new[] { meshTargets[1] };
            }
            
            return null;
        }
    }
}