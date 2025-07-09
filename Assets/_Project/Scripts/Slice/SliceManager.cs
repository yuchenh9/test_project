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
        yield return StartCoroutine(defaultSlicer.Cut(newtarget, sliceCount, axis, planeCalculator));
        
        
        
            

        Debug.Log("Objects have been sliced");
        
        yield return StartCoroutine(SlicedObjectsModify(target.gameObject));//only the slicedObject gets generated blueprints, not the target
         

    }
    
    public IEnumerator RoundSlice(MeshTarget target, int sliceCount)
    {
        //TODO: Realize Method
        yield return null;
    }
    private IEnumerator SlicedObjectsModify(GameObject target)
    //polymorphism
    //target is only used for getting the strategy, defaultSlicer.SlicedObjects is used for odify
    {
        var cutStrategy = GetStrategy(target.gameObject);
        yield return cutStrategy.Modify(this, defaultSlicer.SlicedObjects, target);
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

    // 3D cutting: cut in X, then Y, then Z, using the same sliceCount and planeCalculator
    public IEnumerator Slice3D(Transform container, MeshTarget target, int sliceCount, ISliceTypeCalculatorStrategy planeCalculator)
    {
        // Step 1: Cut along X axis
        List<MeshTarget> xResults = new List<MeshTarget>();
        defaultSlicer.setContainer(container);
        var xTarget = Instantiate(target, container);
        yield return StartCoroutine(defaultSlicer.Cut(xTarget, sliceCount, Vector3.right, planeCalculator)); // Cut along X
        xResults.AddRange(defaultSlicer.SlicedObjects);

        // Step 2: For each result, cut along Y axis
        List<MeshTarget> yResults = new List<MeshTarget>();
        foreach (var obj in xResults)
        {
            defaultSlicer.setContainer(container);
            yield return StartCoroutine(defaultSlicer.Cut(obj, sliceCount, Vector3.up, planeCalculator)); // Cut along Y
            yResults.AddRange(defaultSlicer.SlicedObjects);
        }

        // Step 3: For each result, cut along Z axis
        List<MeshTarget> zResults = new List<MeshTarget>();
        foreach (var obj in yResults)
        {
            defaultSlicer.setContainer(container);
            yield return StartCoroutine(defaultSlicer.Cut(obj, sliceCount, Vector3.forward, planeCalculator)); // Cut along Z
            zResults.AddRange(defaultSlicer.SlicedObjects);
        }

        // Optionally, do something with zResults (all final cut pieces)
        Debug.Log($"3D slicing complete. Final pieces: {zResults.Count}");
    }
}