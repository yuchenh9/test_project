using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicMeshCutter;
using UnityEngine;
using _Project;

public class CustomSlicerBehaviour : CutterBehaviour
{
    [SerializeField] private Transform container;

    public SliceInfo SliceInfo { private set; get; }
    public List<MeshTarget> SlicedObjects { get; } = new();

    private ISliceTypeCalculatorStrategy _planeCalculator;
    private bool _isFinished;
    
    public IEnumerator Cut(MeshTarget targetObject, int sliceCount, Vector3 slicingAxis, ISliceTypeCalculatorStrategy planeCalculator)
    {
        if (sliceCount < 2)
        {
            Debug.LogWarning("SliceCount is less than 2");
            _isFinished = true;
            yield break;
        }
        
        _isFinished = false;
        _planeCalculator = planeCalculator;
        
        var target = Instantiate(targetObject, container);
        SliceInfo = new SliceInfo
        {
            SliceCount = sliceCount,
            SlicingAxis = slicingAxis,
            Separation = Separation,
            StartBounds = UtilityHelper.GetObjectBounds(target.gameObject)
        };

        Refresh();
        CalculatedCut(target);
        
        while (!_isFinished)
        {
            yield return null;
        }
    }
    
    private void Refresh()
    {
        var targetRoots = SlicedObjects
            .Where(item => item != null)
            .Select(x => x.GameobjectRoot);
        
        foreach (var item in targetRoots)
            Destroy(item.gameObject);
        SlicedObjects.Clear();
        SliceInfo.SliceIndex = 0;
    }

    private void CalculatedCut(MeshTarget nextObject)
    {
        var plane = _planeCalculator.Calculate(SliceInfo);
        Cut(nextObject, plane.Position, plane.Normal, OnCut, OnCreated);
    }
    
    private void OnCut(bool success, Info info)
    {
        if (!success)
            MakeNextCut(new[] { info.MeshTarget });
        else      
            SlicedObjects.Remove(info.MeshTarget);
    }
    
    private void OnCreated(Info info, MeshCreationData cData)
    {
        MeshCreation.TranslateCreatedObjects(info, cData.CreatedObjects, cData.CreatedTargets, Separation);
        
        foreach (var t in cData.CreatedObjects) 
            t.transform.SetParent(container);

        foreach (var item in cData.CreatedTargets) 
            SlicedObjects.Add(item);

        MakeNextCut(_planeCalculator.GetNextObjectsForCut(cData.CreatedTargets));
    }


    private void MakeNextCut(IEnumerable<MeshTarget> objects)
    {
        SliceInfo.SliceIndex++;

        if (SliceInfo.SliceIndex < SliceInfo.SliceCount - 1)
        {
            foreach (var item in objects)
            {
                CalculatedCut(item);
            }
        }
        else
        {
            _isFinished = true;
        }
    }
}
