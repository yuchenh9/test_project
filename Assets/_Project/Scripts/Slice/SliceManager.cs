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

    public IEnumerator Slice(Transform container, MeshTarget target, int[] sliceCounts, Vector3[] axises, ISliceTypeCalculatorStrategy planeCalculator)
    {
        Debug.Log("slicing");
        Debug.Log("target:"+target);
        Debug.Log("sliceCount:"+sliceCount);
        defaultSlicer.setContainer(container);
        for (int i = 0; i < axises.Count; i++)
        {
            SliceInfo SliceInfo = new SliceInfo
            {
                SliceCount = sliceCounts[i],
                SlicingAxis = axises[i],
                Separation = 0.02f,
                StartBounds = UtilityHelper.GetObjectBounds(target.gameObject)
            };
            yield return StartCoroutine(defaultSlicer.Cut(target, sliceCount, axis, planeCalculator,SliceInfo));
        
        }
        
            

        Debug.Log("Objects have been sliced");
        
        yield return StartCoroutine(SlicedObjectsModify(target.gameObject));
         

    }
    
    public IEnumerator RoundSlice(MeshTarget target, int sliceCount)
    {
        //TODO: Realize Method
        yield return null;
    }
    private IEnumerator SlicedObjectsModify(GameObject target)
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
}