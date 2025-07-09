using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicMeshCutter;
using UnityEngine;
using _Project;

public class CustomSlicerBehaviour : CutterBehaviour
{
    
    /*
    Scene_data
        CutterBehaviour
            ->Update()//Cut is called as an IEnumerator, but updata() is a main function of the parent class, 
                        so updata() is not called by the tree but called by the system itself
                ->Info invoke onCut  
                    CustomSlicerBehaviour
                        ->MakeNextCut()//increment sliceIndex
                            ->CalculatedCut() 
                               
                ->CreateGameObjects()
                    ->Info invoke onCreated
                    MeshCreation
                        ->CreateObjects()//was not called
        Scene_data
            ->ButtonCutClicked()
            SliceManager
                ->IEnumerator Slice()
                    *CustomSlicerBehaviour
                        onCut()
                        onCreated()
                        ->IEnumerator Cut(
                            -> new SliceInfo//
                            ->CalculatedCut() (not a)recursion
                                LinearSliceTypeCalculatorStrategy
                                    ->Calculate(SliceInfo)
                                CutterBehaviour
                                    ->Cut(//takes the data of a single plane, world position and world normal
                                        ->DrawPlane()
                                        ->new Info(onCut,onCreated)
                                        ->OnCut()
                                            add Info //to be poped by the update()
                                    
                        ObiSoftbodySliceModifierStrategy 
                            ->IEnumerator Modify()// 
        */


    [SerializeField] private Transform container;

    public SliceInfo SliceInfo { private set; get; }

    //SlicedObjects will be actively added newly cut objects, and its objects will be cut
    public List<MeshTarget> SlicedObjects { get; } = new();

    private ISliceTypeCalculatorStrategy _planeCalculator;
    private bool _isFinished;
    private bool _anyCutSucceeded; // For restoring original object if all cuts fail
    
    // Store all cutting planes calculated at the beginning
    private List<PlaneData> _allCuttingPlanes = new List<PlaneData>();
    
    public void setContainer(Transform set_container){
        container=set_container;
    }
    //
    public IEnumerator Cut(MeshTarget target, int sliceCount, Vector3 slicingAxis, ISliceTypeCalculatorStrategy planeCalculator)
    {
        _anyCutSucceeded = false; // For restoring original object if all cuts fail
        if (!planeCalculator.ValidateInputValues(target, sliceCount, slicingAxis))
        {
            Debug.LogWarning("ISliceTypeCalculatorStrategy.ValidateInputValues() is false");
            _isFinished = true;
            yield break;
        }
        
        _isFinished = false;
        _planeCalculator = planeCalculator;

        //var target = Instantiate(targetObject, container);
        //Debug.Log("target name:"+target.gameObject.name);

        //this constructs the public SliceInfo
        SliceInfo = new SliceInfo
        {
            SliceCount = sliceCount,
            SlicingAxis = slicingAxis,
            Separation = Separation,
            StartBounds = UtilityHelper.GetObjectBounds(target.gameObject)
        };
        
        Refresh();
        
        SlicedObjects.Add(target.GetComponent<MeshTarget>());
        if (sliceCount < 2)
        {
            Debug.LogWarning("SliceCount is less than 2");
            _isFinished = true;
            yield break;
        }
        
        // Calculate all cutting planes at the beginning
        CalculateAllCuttingPlanes();
        
        // Start the first cut with the first plane
        CalculatedCut(target);
        
        while (!_isFinished)
        {
            yield return null;
        }
        if (!_anyCutSucceeded) // For restoring original object if all cuts fail
        {
            var renderer = target.GetComponent<Renderer>(); // For restoring original object if all cuts fail
            if (renderer != null)
                renderer.enabled = true; // For restoring original object if all cuts fail
        }
    }
    
    // Calculate all cutting planes at the beginning and store them
    private void CalculateAllCuttingPlanes()
    {
        _allCuttingPlanes.Clear();
        
        // Calculate all planes for the entire cutting operation
        for (int i = 0; i < SliceInfo.SliceCount - 1; i++)
        {
            // Temporarily set the slice index to calculate each plane
            int originalIndex = SliceInfo.SliceIndex;
            SliceInfo.SliceIndex = i;
            
            PlaneData plane = _planeCalculator.Calculate(SliceInfo);
            _allCuttingPlanes.Add(plane);
            
            // Restore original index
            SliceInfo.SliceIndex = originalIndex;
        }
        
        Debug.Log($"Calculated {_allCuttingPlanes.Count} cutting planes at the beginning");
    }
    
    //Refresh() sets SliceIndex to 0 and clears SlicedObjects
    private void Refresh()
    {
        var targetRoots = SlicedObjects
            .Where(item => item != null)
            .Select(x => x.GameobjectRoot);
        
        foreach (var item in targetRoots)
            Destroy(item.gameObject);
        SlicedObjects.Clear();
        SliceInfo.SliceIndex = 0;
        _allCuttingPlanes.Clear(); // Clear the pre-calculated planes
    }

/*
MakeNextCut(MeshTarget[])
    SliceInfo.index++
    ->CalculatedCut(MeshTarget)
        Get pre-calculated plane
        ->Cut(MeshTarget,normal,position)

->Cut(MeshTarget,normal,position)

create planes
iterate plane
    iterate object
        cut(meshtarget, plane)//should be cutting newly created meshes instead of the old ones after the first cut
*/
//takes a meshtarget object, and get the pre-calculated plane, and call cut
    private void CalculatedCut(MeshTarget nextObject)
    {   // Use pre-calculated plane instead of calculating it each time
        Debug.Log("CalculatedCut"+SliceInfo.SliceIndex);
        
        // Get the pre-calculated plane for current slice index
        if (SliceInfo.SliceIndex < _allCuttingPlanes.Count)
        {
            PlaneData plane = _allCuttingPlanes[SliceInfo.SliceIndex];
            Cut(nextObject, plane.Position, plane.Normal, OnCut, OnCreated);
            DebugPlaneDrawer.DrawPlane(plane.Position, plane.Normal, 1f);
        }
        else
        {
            Debug.LogError($"No pre-calculated plane found for slice index {SliceInfo.SliceIndex}");
        }
        //DebugPlaneDrawer.DrawPlane(SliceInfo.StartBounds.min, plane.Normal, 1f);
        //DebugPlaneDrawer.DrawPlane(SliceInfo.StartBounds.max, plane.Normal, 1f);
        //DebugPlaneDrawer.CreateBoundsCube(SliceInfo.StartBounds);
    }
    
    private void OnCut(bool success, Info info)
    {
        if (success)
        {
            _anyCutSucceeded = true; // For restoring original object if all cuts fail
            SlicedObjects.Remove(info.MeshTarget);
        }
        else
        {
            Debug.Log("failed");
            MakeNextCut(new[] { info.MeshTarget });
        }
    }
    
    private void OnCreated(Info info, MeshCreationData cData)
    {
        //MeshCreation.TranslateCreatedObjects(info, cData.CreatedObjects, cData.CreatedTargets, Separation);
        foreach (var t in cData.CreatedObjects) 
            t.transform.SetParent(container);

        foreach (var item in cData.CreatedTargets) 
            SlicedObjects.Add(item);
        Debug.Log("Successfully");
        MakeNextCut(_planeCalculator.GetNextObjectsForCut(cData.CreatedTargets));
    }

    //increment slice index, than iterate through all meshtarget objects, and call calcula
    private void MakeNextCut(IEnumerable<MeshTarget> objects)
    {
        SliceInfo.SliceIndex++;
        Debug.Log(SliceInfo.SliceIndex);
        //Debug.Log("making next cut"+SliceInfo.SliceIndex);
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
            Debug.Log("finished");
        }
    }
}
