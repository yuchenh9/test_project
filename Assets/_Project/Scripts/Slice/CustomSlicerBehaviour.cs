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
        // calculate all the planes, and then pass a single plane to calculatedcut, and then increment the slice index
        
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
    }

/*
MakeNextCut(MeshTarget[])
    SliceInfo.index++
    ->CalculatedCut(MeshTarget)
        Calculate(SliceInfo)
        ->Cut(MeshTarget,normal,position)

->Cut(MeshTarget,normal,position)

create planes
iterate plane
    iterate object
        cut(meshtarget, plane)//should be cutting newly created meshes instead of the old ones after the first cut
*/
//takes a meshtarget object, and calculate the plane position and normal, and call cut
    private void CalculatedCut(MeshTarget nextObject)
    {   //now it returns a plane to the plane var each time calling the calculate
        //change it so that it alculates all the planes, and returns each plane to the plane var
        Debug.Log("CalculatedCut"+SliceInfo.SliceIndex);
        var plane = _planeCalculator.Calculate(SliceInfo);
        //Debug.Log("sliceInfo:"+SliceInfo.ToString());
        Cut(nextObject, plane.Position, plane.Normal, OnCut, OnCreated);
        DebugPlaneDrawer.DrawPlane(plane.Position, plane.Normal, 1f);
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
