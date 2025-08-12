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
    [SerializeField]
    private List<MeshTarget> slicedObjects = new();
    public List<MeshTarget> SlicedObjects => slicedObjects;

    private ISliceTypeCalculatorStrategy _planeCalculator;
    private bool _isFinished;
    private bool _anyCutSucceeded; // For restoring original object if all cuts fail
    
    private List<PlaneData> _allCuttingPlanes = new List<PlaneData>(); // for pre-calculating planes
    
    [System.Serializable]
    public class MeshTargetList {
        public string name;
        public List<DynamicMeshCutter.MeshTarget> items = new List<DynamicMeshCutter.MeshTarget>();
    }
    [System.Serializable]
    public class DebugCutInfo {
        public GameObject cuttingPlane;
        public List<DynamicMeshCutter.MeshTarget> createdObjects = new List<DynamicMeshCutter.MeshTarget>();
    }
    [SerializeField] private List<DebugCutInfo> debugCuts = new List<DebugCutInfo>();
    [SerializeField] private List<GameObject> lastCuttingPlanes = new List<GameObject>();

    public void setContainer(Transform set_container){
        container=set_container;
    }
    //
    public IEnumerator Cut(MeshTarget target, int sliceCount, Vector3 slicingAxis, ISliceTypeCalculatorStrategy planeCalculator)
    {
        _anyCutSucceeded = false; // For restoring original object if all cuts fail
        if (!planeCalculator.ValidateInputValues(target, sliceCount, slicingAxis))
        {
            //Debug.LogWarning("ISliceTypeCalculatorStrategy.ValidateInputValues() is false");
            _isFinished = true;
            yield break;
        }
        
        _isFinished = false;
        _planeCalculator = planeCalculator;

        //var target = Instantiate(targetObject, container);
        ////Debug.Log("target name:"+target.gameObject.name);

        //this constructs the public SliceInfo
        SliceInfo = new SliceInfo
        {
            SliceCount = sliceCount,
            SlicingAxis = slicingAxis,
            Separation = Separation,
            StartBounds = UtilityHelper.GetObjectBounds(target.gameObject)
        };
        
        Refresh();
        
        slicedObjects.Add(target.GetComponent<MeshTarget>());
        if (sliceCount < 2)
        {
            //Debug.LogWarning("SliceCount is less than 2");
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
    public IEnumerator CutWithPlanes(MeshTarget target, List<PlaneData> planes, ISliceTypeCalculatorStrategy planeCalculator)
    {
       //Debug.Log("CutWithPlanes: Start");
        RefreshSlicedObjects(); // <-- Add this line
        _planeCalculator = planeCalculator;
       //Debug.Log("CutWithPlanes: Set planeCalculator");
        List<MeshTarget> toCutObjects = new List<MeshTarget> { target };
       //Debug.Log("CutWithPlanes: Initialized toCutObjects");

        // Store the planes for inspector
        debugCuts.Clear();
       //Debug.Log("CutWithPlanes: Cleared debugCuts");
        lastCuttingPlanes.Clear();
       //Debug.Log("CutWithPlanes: Cleared lastCuttingPlanes");

        for (int i = 0; i < planes.Count; i++)
        {
           //Debug.Log($"CutWithPlanes: Plane loop i={i}");
            PlaneData plane = planes[i];
           //Debug.Log($"CutWithPlanes: Got plane {i}");
            List<MeshTarget> nextToCut = new List<MeshTarget>();
           //Debug.Log("CutWithPlanes: Initialized nextToCut");
            int cutsPending = toCutObjects.Count;
           //Debug.Log($"CutWithPlanes: cutsPending={cutsPending}");
            List<List<MeshTarget>> createdObjectsPerCut = new List<List<MeshTarget>>(toCutObjects.Count);
           //Debug.Log("CutWithPlanes: Initialized createdObjectsPerCut");
            for (int j = 0; j < toCutObjects.Count; j++)
            {
                createdObjectsPerCut.Add(new List<MeshTarget>());
               //Debug.Log($"CutWithPlanes: createdObjectsPerCut.Add for j={j}");
            }

            for (int j = 0; j < toCutObjects.Count; j++)
            {
               //Debug.Log($"CutWithPlanes: Inner object loop j={j}");
                int idx = j;
               //Debug.Log($"CutWithPlanes: idx={idx}");
                MeshTarget obj = toCutObjects[j];
               //Debug.Log($"CutWithPlanes: obj={obj}");
                bool cutSuccess = false;
               //Debug.Log("CutWithPlanes: cutSuccess=false");
                bool cutFinished = false;
               //Debug.Log("CutWithPlanes: cutFinished=false");
                List<MeshTarget> createdTargets = new List<MeshTarget>();
               //Debug.Log("CutWithPlanes: createdTargets initialized");

                void OnCutLocal(bool success, Info info)
                {
                   //Debug.Log($"CutWithPlanes: OnCutLocal called, success={success}");
                    cutSuccess = success;
                    cutFinished = true; // Always set cutFinished, even if success is false
                }
                void OnCreatedLocal(Info info, MeshCreationData cData)
                {
                    //Debug.Log("CutWithPlanes: OnCreatedLocal called");
                    if (cData.CreatedTargets != null && cData.CreatedTargets.Length > 0)
                    {
                        //Debug.Log($"CutWithPlanes: OnCreatedLocal adding {cData.CreatedTargets.Length} targets");
                        createdTargets.AddRange(cData.CreatedTargets);
                    }
                    cutFinished = true;
                    //Debug.Log("CutWithPlanes: cutFinished=true");
                }
                void OnCreated(Info info, MeshCreationData cData)
                {
                    //Debug.Log("OnCreated called");
                    //Debug.Log($"OnCreated: cData.CreatedObjects length = {(cData.CreatedObjects != null ? cData.CreatedObjects.Length : -1)}");
                    if (cData.CreatedTargets != null && cData.CreatedTargets.Length > 0)
                    {
                        //Debug.Log($"CutWithPlanes: OnCreatedLocal adding {cData.CreatedTargets.Length} targets");
                        createdTargets.AddRange(cData.CreatedTargets);
                    }
                    foreach (var t in cData.CreatedObjects)
                    {
                        t.transform.SetParent(container);
                        // Also parent the wrapper GameObject (if any) to the container
                        if (t.transform.parent != null)
                            t.transform.parent.SetParent(container);
                        // Log the parent tree for this object
                        string parentTree = t.name;
                        Transform current = t.transform.parent;
                        while (current != null)
                        {
                            parentTree = current.name + "/" + parentTree;
                            current = current.parent;
                        }
                        //Debug.Log($"Created object parent tree: {parentTree}");
                    }
                    //Debug.Log("OnCreated: Set parents");
                    foreach (var item in cData.CreatedTargets)
                        slicedObjects.Add(item);
                    //Debug.Log("OnCreated: Added to SlicedObjects");
                    //Debug.Log("OnCreated: Successfully");
                   
                    cutFinished = true;
                    //Debug.Log("OnCreated: Calling MakeNextCut");
                }
                string planeName = $"cutPlane({plane.Position},{plane.Normal})";
               //Debug.Log($"CutWithPlanes: Drawing plane {planeName}");
                //GameObject debugPlane = DebugPlaneDrawer.DrawPlane(plane.Position, plane.Normal, 1f, planeName);
               //Debug.Log($"CutWithPlanes: Plane drawn {debugPlane}");
                //lastCuttingPlanes.Add(debugPlane);
               //Debug.Log("CutWithPlanes: Plane added to lastCuttingPlanes");
                Cut(obj, plane.Position, plane.Normal, OnCutLocal, OnCreated);
               //Debug.Log("CutWithPlanes: Cut called");

                IEnumerator WaitForCut()
                {
                   //Debug.Log("CutWithPlanes: Enter WaitForCut");
                    while (!cutFinished)
                    {
                        yield return null;
                    }
                   //Debug.Log("CutWithPlanes: WaitForCut finished");
                    if (cutSuccess && createdTargets.Count > 0)
                    {
                       //Debug.Log($"CutWithPlanes: Adding {createdTargets.Count} created targets to createdObjectsPerCut");
                        createdObjectsPerCut[idx].AddRange(createdTargets);
                    }
                    else
                    {
                       //Debug.Log("CutWithPlanes: Adding obj to createdObjectsPerCut");
                        createdObjectsPerCut[idx].Add(obj);
                    }
                    cutsPending--;
                   //Debug.Log($"CutWithPlanes: cutsPending now {cutsPending}");

                    // Log using parent name if it matches the pattern, otherwise use object name
                    string objNames = string.Join(", ", createdObjectsPerCut[idx].ConvertAll(o => {
                        if (o != null && o.transform.parent != null) {
                            string parentName = o.transform.parent.name;
                            if (!string.IsNullOrEmpty(parentName) && parentName.Contains("steak") && parentName.StartsWith("("))
                                return parentName;
                        }
                        return o != null ? o.name : "null";
                    }));
                   //Debug.Log($"plane:({plane.Position}, {plane.Normal}) objects:[{objNames}]");

                    DebugCutInfo debugInfo = new DebugCutInfo {
                        cuttingPlane = null, // debugPlane is commented out
                        createdObjects = new List<DynamicMeshCutter.MeshTarget>(createdObjectsPerCut[idx])
                    };
                    debugCuts.Add(debugInfo);
                   //Debug.Log("CutWithPlanes: debugInfo added to debugCuts");
                }
                yield return WaitForCut();
               //Debug.Log("CutWithPlanes: WaitForCut coroutine finished");
            }

            // Wait until all cuts for this plane are finished
           //Debug.Log("CutWithPlanes: Waiting for all cuts for this plane to finish");
            while (cutsPending > 0)
            {
                yield return null;
            }
           //Debug.Log("CutWithPlanes: All cuts for this plane finished");

            nextToCut.Clear();
           //Debug.Log("CutWithPlanes: nextToCut cleared");
            foreach (var list in createdObjectsPerCut)
            {
                nextToCut.AddRange(list);
               //Debug.Log("CutWithPlanes: Added list to nextToCut");
            }

            toCutObjects = nextToCut;
           //Debug.Log("CutWithPlanes: toCutObjects updated");
        }
        // At this point, toCutObjects contains all final pieces
        // Do whatever you need with them (parenting, storing, etc.)
       //Debug.Log("CutWithPlanes: End");
    }
    
    // Iterative version: cuts all objects by all planes in order, no recursion/callback chain
    // [CutWithPlanes function deleted]
    
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
        
        //Debug.Log($"Calculated {_allCuttingPlanes.Count} cutting planes at the beginning"); // for pre-calculating planes
    }
    
    //Refresh() sets SliceIndex to 0 and clears SlicedObjects
    private void Refresh()
    {
        var targetRoots = slicedObjects
            .Where(item => item != null && item.GameobjectRoot != null)
            .Select(x => x.GameobjectRoot);
        foreach (var item in targetRoots)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
            else
            {
                Debug.LogWarning("CustomSlicerBehaviour.Refresh: Encountered null item or item.gameObject while destroying.");
            }
        }
        slicedObjects.Clear();
        SliceInfo.SliceIndex = 0;
        _allCuttingPlanes.Clear(); // Clear the pre-calculated planes - for pre-calculating planes
    }

    /// <summary>
    /// Clears the slicedObjects list and logs the operation, but does not destroy any GameObjects or reset other state.
    /// </summary>
    public void RefreshSlicedObjects()
    {
        slicedObjects.Clear();
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
    {
        //Debug.Log("CalculatedCut: Start");
        if (SliceInfo.SliceIndex < _allCuttingPlanes.Count)
        {
            //Debug.Log("CalculatedCut: In range");
            PlaneData plane = _allCuttingPlanes[SliceInfo.SliceIndex];
            //Debug.Log("CalculatedCut: Got plane");
            Cut(nextObject, plane.Position, plane.Normal, OnCut, OnCreated);
            //Debug.Log("CalculatedCut: Called Cut");
            string planeName = $"cutPlane({plane.Position},{plane.Normal})";
            //Debug.Log("CalculatedCut: Created planeName");
            //GameObject debugPlane = DebugPlaneDrawer.DrawPlane(plane.Position, plane.Normal, 1f, planeName);
            //Debug.Log("CalculatedCut: Drew debug plane");
            //lastCuttingPlanes.Add(debugPlane);
            //Debug.Log("CalculatedCut: Added to lastCuttingPlanes");
        }
        else
        {
            //Debug.Log("CalculatedCut: Out of range");
            //Debug.LogError("No pre-calculated plane found for slice index");
        }
    }
    
    private void OnCut(bool success, Info info)
    {
        //Debug.Log("OnCut: Start");
        if (success)
        {
            //Debug.Log("OnCut: Success");
            _anyCutSucceeded = true;
            slicedObjects.Remove(info.MeshTarget);
            //Debug.Log("OnCut: Removed from SlicedObjects");
        }
        else
        {
            //Debug.Log("OnCut: Failed");
            MakeNextCut(new[] { info.MeshTarget });
            //Debug.Log("OnCut: Called MakeNextCut");
        }
    }
    
    private void OnCreated(Info info, MeshCreationData cData)
    {
       //Debug.Log("OnCreated called");
        //Debug.Log($"OnCreated: cData.CreatedObjects length = {(cData.CreatedObjects != null ? cData.CreatedObjects.Length : -1)}");
        foreach (var t in cData.CreatedObjects)
        {
            t.transform.SetParent(container);
            // Also parent the wrapper GameObject (if any) to the container
            if (t.transform.parent != null)
                t.transform.parent.SetParent(container);
            // Log the parent tree for this object
            string parentTree = t.name;
            Transform current = t.transform.parent;
            while (current != null)
            {
                parentTree = current.name + "/" + parentTree;
                current = current.parent;
            }
            //Debug.Log($"Created object parent tree: {parentTree}");
        }
        //Debug.Log("OnCreated: Set parents");
        foreach (var item in cData.CreatedTargets)
            slicedObjects.Add(item);
        //Debug.Log("OnCreated: Added to SlicedObjects");
        //Debug.Log("OnCreated: Successfully");
        if (cData.CreatedTargets == null)
        {
            //Debug.LogError("OnCreated: ERROR: cData.CreatedTargets is null in OnCreated, aborting MakeNextCut");
            return;
        }
        //Debug.Log("OnCreated: Calling MakeNextCut");
    }

    //increment slice index, than iterate through all meshtarget objects, and call calcula
    private void MakeNextCut(IEnumerable<MeshTarget> objects)
    {
        //Debug.Log("MakeNextCut: Start");
        SliceInfo.SliceIndex++;
        //Debug.Log("MakeNextCut: Incremented SliceIndex");
        if (SliceInfo.SliceIndex < SliceInfo.SliceCount - 1)
        {
            //Debug.Log("MakeNextCut: In range");
            foreach (var item in objects)
            {
                CalculatedCut(item);
                //Debug.Log("MakeNextCut: Called CalculatedCut");
            }
        }
        else
        {
            //Debug.Log("MakeNextCut: Out of range");
            _isFinished = true;
            //Debug.Log("MakeNextCut: Finished");
        }
    }
}