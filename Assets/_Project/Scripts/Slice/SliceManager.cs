using System.Collections;
using System.Collections.Generic;
using _Project;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

public class SliceManager : MonoBehaviour
{
      /*
    Scene_data
        CutterBehaviour
            ->Update()//Cut is called as an IEnumerator, but updata() is a main function of the parent class, 
                        so updata() is not called by the tree but called by the system itself
                ->Info invoke onCut
                    CustomSlicerBehaviour
                        ->MakeNextCut()
                            ->CalculatedCut()
                        
                ->CreateGameObjects()
                    ->Info invoke onCreated
                    MeshCreation
                        ->CreateObjects()//was not called
        SliceManager
            ->IEnumerator Slice()//onclick()
                *CustomSlicerBehaviour
                    onCut=()
                    onCreated=()
                    ->IEnumerator Cut(//(not a)polymorphism
                        -> new SliceInfo
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
    /*
    slice multiple axises 
    start modify
    */
    [SerializeField] private CustomSlicerBehaviour defaultSlicer;

    public IEnumerator Slice(Transform container, MeshTarget target, int sliceCount, Vector3 axis, ISliceTypeCalculatorStrategy planeCalculator)
    {
        Debug.Log("slicing");
        Debug.Log("target:"+target);
        Debug.Log("sliceCount:"+sliceCount);
        defaultSlicer.setContainer(container);
        var newtarget = Instantiate(target, container);

        // Calculate all cutting planes for the given axis
        Bounds bounds = UtilityHelper.GetObjectBounds(target.gameObject);
        List<PlaneData> planes = CalculateAllCuttingPlanes(bounds, sliceCount, axis);
        yield return StartCoroutine(defaultSlicer.CutWithPlanes(newtarget, planes, planeCalculator));

        Debug.Log("Objects have been sliced");
        yield return StartCoroutine(SlicedObjectsModify(defaultSlicer.SlicedObjects, target.gameObject)); //only the slicedObject gets generated blueprints, not the target
    }
    
    public IEnumerator RoundSlice(MeshTarget target, int sliceCount)
    {
        //TODO: Realize Method
        yield return null;
    }
    private IEnumerator SlicedObjectsModify(List<MeshTarget> objects, GameObject target)
    //polymorphism
    //target is only used for getting the strategy, defaultSlicer.SlicedObjects is used for odify
    {
        var cutStrategy = GetStrategy(target.gameObject);
        yield return cutStrategy.Modify(this, objects, target);
        Debug.Log("Sliced objects modified");
    }

    private ISliceModifierStrategy GetStrategy(GameObject target)
    {
        if (target.TryGetComponent<ObiSoftbody>(out _))
        {
            return new ObiSoftbodySliceModifierStrategy();
        }
        if (target.TryGetComponent<ObiCloth>(out _))
        {
            return new ObiClothSliceModifierStrategy();
        }
        else
        {
            return new DefaultSliceModifierStrategy();
        }
    }

    // Helper to calculate all cutting planes for a given axis - for using pre-calculated planes
    private List<PlaneData> CalculateAllCuttingPlanes(Bounds bounds, int count, Vector3 normal) // for using pre-calculated planes
    {
        List<PlaneData> planes = new List<PlaneData>(); // for using pre-calculated planes
        if (count <= 0) return planes; // for using pre-calculated planes
        float step = 0f; // for using pre-calculated planes
        Vector3 min = bounds.min; // for using pre-calculated planes
        Vector3 max = bounds.max; // for using pre-calculated planes
        Vector3 center = bounds.center; // for using pre-calculated planes
        if (normal == Vector3.right)
            step = bounds.size.x / (count + 1);
        else if (normal == Vector3.up)
            step = bounds.size.y / (count + 1);
        else if (normal == Vector3.forward)
            step = bounds.size.z / (count + 1);
        for (int i = 1; i <= count; i++) // for using pre-calculated planes
        {
            Vector3 pos = min;
            if (normal == Vector3.right)
            {
                pos = min + Vector3.right * (step * i);
                pos.y = center.y; pos.z = center.z;
            }
            else if (normal == Vector3.up)
            {
                pos = min + Vector3.up * (step * i);
                pos.x = center.x; pos.z = center.z;
            }
            else if (normal == Vector3.forward)
            {
                pos = min + Vector3.forward * (step * i);
                pos.x = center.x; pos.y = center.y;
            }
            planes.Add(new PlaneData(pos, normal)); // for using pre-calculated planes
        }
        return planes; // for using pre-calculated planes
    }

    // 3D cutting: cut in X, then Y, then Z, using the same sliceCount and planeCalculator
    public IEnumerator Slice3D(Transform container, MeshTarget target, int int_x, ISliceTypeCalculatorStrategy planeCalculator) // for 3D slicing
    {
        defaultSlicer.setContainer(container); // Ensure correct parenting
        int int_y = int_x;
        int int_z = int_x;
        if (int_x == 0 && int_y == 0 && int_z == 0) yield break; // for 3D slicing

        // Always keep a reference to the original prefab
        MeshTarget originalPrefab = target;

        // X axis cut
        List<MeshTarget> selectedObjects = new List<MeshTarget> { target };
        Bounds bounds = UtilityHelper.GetObjectBounds(target.gameObject);
        List<PlaneData> xPlanes = CalculateAllCuttingPlanes(bounds, int_x, Vector3.right);
        List<PlaneData> yPlanes = CalculateAllCuttingPlanes(bounds, int_y, Vector3.up);
        List<PlaneData> zPlanes = CalculateAllCuttingPlanes(bounds, int_z, Vector3.forward);

        Debug.Log($"[3D Slicing Bug] X planes: {xPlanes.Count}, Y planes: {yPlanes.Count}, Z planes: {zPlanes.Count}");

        // --- X axis cut ---
        Debug.Log($"[3D Slicing Bug] Starting X axis cut on {selectedObjects.Count} objects");
        List<MeshTarget> xResults = new List<MeshTarget>();
        foreach (var obj in selectedObjects)
        {
            MeshTarget fresh = Instantiate(originalPrefab, container);
            fresh.transform.position = obj.transform.position;
            fresh.transform.rotation = obj.transform.rotation;
            fresh.transform.localScale = obj.transform.localScale;
            if (obj != originalPrefab)
                Destroy(obj.gameObject);
            var slicer = defaultSlicer;
            yield return slicer.CutWithPlanes(fresh, xPlanes, planeCalculator);
            Debug.Log($"[3D Slicing Bug] After X cut: {slicer.SlicedObjects.Count} objects");
            if (slicer.SlicedObjects.Count > 0)
            {
                xResults.AddRange(slicer.SlicedObjects);
                // Removed: yield return StartCoroutine(SlicedObjectsModify(xObj.gameObject));
            }
            else
            {
                xResults.Add(fresh);
                // Removed: yield return StartCoroutine(SlicedObjectsModify(fresh.gameObject));
                Debug.Log("[3D Slicing Bug] No cut made, keeping original object");
            }
        }
        Debug.Log($"[3D Slicing Bug] X axis cut produced {xResults.Count} objects");

        // --- Y axis cut ---
        Debug.Log($"[3D Slicing Bug] Starting Y axis cut on {xResults.Count} objects");
        List<MeshTarget> yResults = new List<MeshTarget>();
        foreach (var obj in xResults)
        {
            MeshTarget fresh = Instantiate(originalPrefab, container);
            fresh.transform.position = obj.transform.position;
            fresh.transform.rotation = obj.transform.rotation;
            fresh.transform.localScale = obj.transform.localScale;
            if (obj != originalPrefab)
                Destroy(obj.gameObject);
            var slicer = defaultSlicer;
            yield return slicer.CutWithPlanes(fresh, yPlanes, planeCalculator);
            Debug.Log($"[3D Slicing Bug] After Y cut: {slicer.SlicedObjects.Count} objects");
            if (slicer.SlicedObjects.Count > 0)
            {
                yResults.AddRange(slicer.SlicedObjects);
                // Removed: yield return StartCoroutine(SlicedObjectsModify(yObj.gameObject));
            }
            else
            {
                yResults.Add(fresh);
                // Removed: yield return StartCoroutine(SlicedObjectsModify(fresh.gameObject));
                Debug.Log("[3D Slicing Bug] No Y cut made, keeping original object");
            }
        }
        Debug.Log($"[3D Slicing Bug] Y axis cut produced {yResults.Count} objects");

        // --- Z axis cut ---
        Debug.Log($"[3D Slicing Bug] Starting Z axis cut on {yResults.Count} objects");
        List<MeshTarget> zResults = new List<MeshTarget>();
        foreach (var obj in yResults)
        {
            MeshTarget fresh = Instantiate(originalPrefab, container);
            fresh.transform.position = obj.transform.position;
            fresh.transform.rotation = obj.transform.rotation;
            fresh.transform.localScale = obj.transform.localScale;
            if (obj != originalPrefab)
                Destroy(obj.gameObject);
            var slicer = defaultSlicer;
            yield return slicer.CutWithPlanes(fresh, zPlanes, planeCalculator);
            Debug.Log($"[3D Slicing Bug] After Z cut: {slicer.SlicedObjects.Count} objects");
            if (slicer.SlicedObjects.Count > 0)
            {
                zResults.AddRange(slicer.SlicedObjects);
                // Removed: yield return StartCoroutine(SlicedObjectsModify(zObj.gameObject));
            }
            else
            {
                zResults.Add(fresh);
                // Removed: yield return StartCoroutine(SlicedObjectsModify(fresh.gameObject));
                Debug.Log("[3D Slicing Bug] No Z cut made, keeping original object");
            }
        }
        Debug.Log($"[3D Slicing Bug] Z axis cut produced {zResults.Count} objects");

        // Only add physics components after all cuts are complete
        if (zResults.Count > 0)
            yield return StartCoroutine(SlicedObjectsModify(zResults, zResults[0].gameObject));
        //Debug.Log($"3D slicing complete. Final pieces: {zResults.Count}");
    }

}
