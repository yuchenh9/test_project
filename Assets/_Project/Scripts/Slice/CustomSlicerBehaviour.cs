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
    
    private List<PlaneData> _allCuttingPlanes = new List<PlaneData>(); // for pre-calculating planes
    
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
        
        // Calculate all cutting planes at the beginning - for pre-calculating planes
        CalculateAllCuttingPlanes(); // for pre-calculating planes
        
        // Start the first cut with the first plane - for pre-calculating planes
        CalculatedCut(target); // for pre-calculating planes
        
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
    
    // Variation of Cut function that takes a list of cutting planes instead of sliceCount and slicingAxis - for using pre-calculated planes
    public IEnumerator CutWithPlanes(MeshTarget target, List<PlaneData> cuttingPlanes, ISliceTypeCalculatorStrategy planeCalculator)
    {
        _anyCutSucceeded = false;
        if (cuttingPlanes == null || cuttingPlanes.Count == 0)
        {
            Debug.LogWarning("No cutting planes provided");
            _isFinished = true;
            yield break;
        }
        _isFinished = false;
        _planeCalculator = planeCalculator;

        // Setup SliceInfo for plane calculation
        SliceInfo = new SliceInfo
        {
            SliceCount = cuttingPlanes.Count + 1,
            SlicingAxis = Vector3.zero,
            Separation = Separation,
            StartBounds = UtilityHelper.GetObjectBounds(target.gameObject)
        };

        Refresh();
        // Iterative approach
        List<MeshTarget> objectsToBeCut = new List<MeshTarget> { target };
        for (int i = 0; i < cuttingPlanes.Count; i++)
        {
            PlaneData plane = cuttingPlanes[i];
            List<MeshTarget> nextObjects = new List<MeshTarget>();
            foreach (var obj in objectsToBeCut)
            {
                bool cutSuccess = false;
                List<MeshTarget> createdTargets = new List<MeshTarget>();
                // Use a local OnCreated to capture results synchronously
                void OnCreatedLocal(Info info, MeshCreationData cData)
                {
                    if (cData.CreatedTargets != null && cData.CreatedTargets.Length > 0)
                    {
                        createdTargets.AddRange(cData.CreatedTargets);
                        cutSuccess = true;
                    }
                }
                DebugPlaneDrawer.DrawPlane(plane.Position, plane.Normal,1f);
                // Perform the cut (no recursion)
                Cut(obj, plane.Position, plane.Normal, null, OnCreatedLocal);
                // Wait for the cut to complete (wait for one frame)
                yield return null;
                if (cutSuccess)
                {
                    foreach (var created in createdTargets)
                    {
                        created.transform.SetParent(container);
                        nextObjects.Add(created);
                    }
                    // Destroy the original object after cut
                    if (obj != null && obj.gameObject != null)
                        GameObject.Destroy(obj.gameObject);
                }
                else
                {
                    // If cut failed, keep the original object
                    nextObjects.Add(obj);
                }
            }
            objectsToBeCut = nextObjects;
        }
        // Finalize SlicedObjects
        SlicedObjects.Clear();
        SlicedObjects.AddRange(objectsToBeCut);
        Debug.Log($"[3D Slicing Bug] CutWithPlanes: SlicedObjects count = {SlicedObjects.Count}");
        _isFinished = true;
    }
    
    private void CalculateAllCuttingPlanes() // for pre-calculating planes
    {
        _allCuttingPlanes.Clear(); // for pre-calculating planes
        
        // Calculate all planes for the entire cutting operation - for pre-calculating planes
        for (int i = 0; i < SliceInfo.SliceCount - 1; i++) // for pre-calculating planes
        {
            // Temporarily set the slice index to calculate each plane - for pre-calculating planes
            int originalIndex = SliceInfo.SliceIndex; // for pre-calculating planes
            SliceInfo.SliceIndex = i; // for pre-calculating planes
            
            PlaneData plane = _planeCalculator.Calculate(SliceInfo); // for pre-calculating planes
            _allCuttingPlanes.Add(plane); // for pre-calculating planes
            
            // Restore original index - for pre-calculating planes
            SliceInfo.SliceIndex = originalIndex; // for pre-calculating planes
        }
        
        Debug.Log($"Calculated {_allCuttingPlanes.Count} cutting planes at the beginning"); // for pre-calculating planes
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
        _allCuttingPlanes.Clear(); // Clear the pre-calculated planes - for pre-calculating planes
    }

/*
MakeNextCut(MeshTarget[])
    SliceInfo.index++
    ->CalculatedCut(MeshTarget)
        Get pre-calculated plane - for pre-calculating planes
        ->Cut(MeshTarget,normal,position)

->Cut(MeshTarget,normal,position)

create planes
iterate plane
    iterate object
        cut(meshtarget, plane)//should be cutting newly created meshes instead of the old ones after the first cut
*/
//takes a meshtarget object, and get the pre-calculated plane, and call cut - for pre-calculating planes
    private void CalculatedCut(MeshTarget nextObject)
    {   // Debug.Log("CalculatedCut"+SliceInfo.SliceIndex);
        
        if (SliceInfo.SliceIndex < _allCuttingPlanes.Count) // for pre-calculating planes
        {
            PlaneData plane = _allCuttingPlanes[SliceInfo.SliceIndex]; // for pre-calculating planes
            Cut(nextObject, plane.Position, plane.Normal, OnCut, OnCreated); // for pre-calculating planes
            DebugPlaneDrawer.DrawPlane(plane.Position, plane.Normal, 1f); // for pre-calculating planes
        }
        else // for pre-calculating planes
        {
            Debug.LogError($"No pre-calculated plane found for slice index {SliceInfo.SliceIndex}"); // for pre-calculating planes
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
        // Debugging: log created objects and targets for diagnosing 3D slicing bug
        Debug.Log($"[3D Slicing Bug] OnCreated: cData.CreatedObjects count = {(cData.CreatedObjects != null ? cData.CreatedObjects.Length.ToString() : "null")}");
        if (cData.CreatedObjects != null)
        {
            for (int i = 0; i < cData.CreatedObjects.Length; i++)
            {
                Debug.Log($"[3D Slicing Bug] OnCreated: CreatedObjects[{i}] = {(cData.CreatedObjects[i] != null ? cData.CreatedObjects[i].name : "null")}");
            }
        }
        Debug.Log($"[3D Slicing Bug] OnCreated: cData.CreatedTargets count = {(cData.CreatedTargets != null ? cData.CreatedTargets.Length.ToString() : "null")}");
        if (cData.CreatedTargets != null)
        {
            for (int i = 0; i < cData.CreatedTargets.Length; i++)
            {
                Debug.Log($"[3D Slicing Bug] OnCreated: CreatedTargets[{i}] = {(cData.CreatedTargets[i] != null ? cData.CreatedTargets[i].name : "null")}");
            }
        }
        //MeshCreation.TranslateCreatedObjects(info, cData.CreatedObjects, cData.CreatedTargets, Separation);
        foreach (var t in cData.CreatedObjects) 
            t.transform.SetParent(container);

        foreach (var item in cData.CreatedTargets) 
            SlicedObjects.Add(item);
        Debug.Log("Successfully");
        // Debugging: log _planeCalculator and cData.CreatedTargets before MakeNextCut - for diagnosing 3D slicing bug
        Debug.Log($"[3D Slicing Bug] _planeCalculator is {( _planeCalculator != null ? _planeCalculator.GetType().Name : "null") }");
        Debug.Log($"[3D Slicing Bug] cData.CreatedTargets is {(cData.CreatedTargets != null ? "not null" : "null")}");
        if (cData.CreatedTargets == null)
        {
            Debug.LogError("[3D Slicing Bug] ERROR: cData.CreatedTargets is null in OnCreated, aborting MakeNextCut");
            return;
        }
        MakeNextCut(_planeCalculator.GetNextObjectsForCut(cData.CreatedTargets));
    }

    //increment slice index, than iterate through all meshtarget objects, and call calcula
    private void MakeNextCut(IEnumerable<MeshTarget> objects)
    {
        Debug.Log($"[3D Slicing Bug] MakeNextCut called. objects null? {objects == null}"); // for diagnosing 3D slicing bug
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
