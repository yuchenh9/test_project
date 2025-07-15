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
        //Debug.Log("slicing");
        //Debug.Log("target:"+target);
        //Debug.Log("sliceCount:"+sliceCount);
        defaultSlicer.setContainer(container);
        var newtarget = Instantiate(target, container);

        // Calculate all cutting planes for the given axis
        Bounds bounds = UtilityHelper.GetObjectBounds(target.gameObject);
        //List<PlaneData> planes = CalculateAllCuttingPlanes(bounds, sliceCount, axis);
        List<PlaneData> planes = CalculateAllCuttingPlanes(bounds, sliceCount, 1, 1);
        foreach (var plane in planes)
        {
            //Debug.Log($"Generated plane: pos={plane.Position}, normal={plane.Normal}");
        }
        yield return StartCoroutine(defaultSlicer.CutWithPlanes(newtarget, planes, planeCalculator));

        //Debug.Log("Objects have been sliced");
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
        //Debug.Log("Sliced objects modified");
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

    // Returns all cutting planes to cut the bounds into x, y, z pieces along X, Y, Z axes
    // If any int <= 1, do not return planes for that axis
    private List<PlaneData> CalculateAllCuttingPlanes(Bounds bounds, int x, int y, int z)
    {
        List<PlaneData> planes = new List<PlaneData>();
        if (x > 1)
        {
            float step = bounds.size.x / x;
            Vector3 min = bounds.min;
            Vector3 center = bounds.center;
            for (int i = 1; i < x; i++)
            {
                Vector3 pos = min + Vector3.right * (step * i);
                pos.y = center.y; pos.z = center.z;
                planes.Add(new PlaneData(pos, Vector3.right));
            }
        }
        if (y > 1)
        {
            float step = bounds.size.y / y;
            Vector3 min = bounds.min;
            Vector3 center = bounds.center;
            for (int i = 1; i < y; i++)
            {
                Vector3 pos = min + Vector3.up * (step * i);
                pos.x = center.x; pos.z = center.z;
                planes.Add(new PlaneData(pos, Vector3.up));
            }
        }
        if (z > 1)
        {
            float step = bounds.size.z / z;
            Vector3 min = bounds.min;
            Vector3 center = bounds.center;
            for (int i = 1; i < z; i++)
            {
                Vector3 pos = min + Vector3.forward * (step * i);
                pos.x = center.x; pos.y = center.y;
                planes.Add(new PlaneData(pos, Vector3.forward));
            }
        }
        return planes;
    }

    // 3D cutting: cut in X, then Y, then Z, using the same sliceCount and planeCalculator
}
