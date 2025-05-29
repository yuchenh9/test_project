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
        *->SliceManager
        Slice(
            ->CustomSlicerBehaviour
            Cut(
                CalculatedCut()
                    ->CutterBehaviour
                    Cut(
                        OnCut,OnCreated
                            MakeNextCut
            ->ObiSoftbodySliceModifierStrategy 
                Modify()// TODO:solve bug#1
    */
    [SerializeField] private CustomSlicerBehaviour defaultSlicer;

    public IEnumerator Slice(Transform container, MeshTarget target, int sliceCount, Vector3 axis, ISliceTypeCalculatorStrategy planeCalculator)
    {
        Debug.Log("slicing");
        Debug.Log("target:"+target);
        Debug.Log("sliceCount:"+sliceCount);
        defaultSlicer.setContainer(container);
        yield return StartCoroutine(defaultSlicer.Cut(target, sliceCount, axis, planeCalculator));
            

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