using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Project;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

public class ObiSoftbodySliceModifierStrategy : ISliceModifierStrategy
{
   
    /*
    Scene_data
        SliceManager
        ->Slice(//was not successful
            CustomSlicerBehaviour
            ->Cut(
                ->CalculatedCut()
                    CutterBehaviour
                    ->Cut(//was not successful
                        ->OnCut,OnCreated
                            ->MakeNextCut
                    ->Update()
                        ->CreateGameObjects
                            MeshCreation
                                ->CreateObjects()//was not called
            *ObiSoftbodySliceModifierStrategy 
                ->Modify()// TODO:solve bug#1
    cut was not successful,and i am not sure where it fail.
    this probably causes modify to not work properly,
    but it does not tell why modify produces the first bug, which indicates that it is trying to add a softbody to an object that already have a softbody.
    *the failure in cut causes the correct object not to be produced, which causes modifying function to be modifying improper object.
    */
     /* bug#1
        Can't add 'ObiSoftbody' to Steak Softbody(Clone) because a 'ObiSoftbody' is already added to the game object!
        A GameObject can only contain one 'ObiActor' component.
        UnityEngine.GameObject:AddComponent<Obi.ObiSoftbody> ()
        ObiSoftbodySliceModifierStrategy/<Modify>d__0:MoveNext () (at Assets/_Project/Scripts/Slice/SliceModifiers/ObiSoftbodySliceModifierStrategy.cs:29)
        UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
        SliceManager/<Slice>d__1:MoveNext () (at Assets/_Project/Scripts/Slice/SliceManager.cs:33)
        UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

        //the failure in adding obisoftbody component causes adding soft body to the dictionary to have null key.
        ArgumentNullException: Value cannot be null.
        Parameter name: key
        System.Collections.Generic.Dictionary`2[TKey,TValue].TryInsert (TKey key, TValue value, System.Collections.Generic.InsertionBehavior behavior) (at <3aacbe608a2f468eb6bb1c15e219490d>:0)
        System.Collections.Generic.Dictionary`2[TKey,TValue].Add (TKey key, TValue value) (at <3aacbe608a2f468eb6bb1c15e219490d>:0)
        ObiSoftbodySliceModifierStrategy+<Modify>d__0.MoveNext () (at Assets/_Project/Scripts/Slice/SliceModifiers/ObiSoftbodySliceModifierStrategy.cs:36)
        UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) (at /Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/Coroutines.cs:17)
        UnityEngine.MonoBehaviour:StartCoroutine(IEnumerator)
        <Slice>d__1:MoveNext() (at Assets/_Project/Scripts/Slice/SliceManager.cs:33)
        UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr) (at /Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/Coroutines.cs:17)

        issue1:does not render any softbody when cut number set to 3 Answer:  every cut plane is outside the boundary, so no cut is performed, resulting in no new mesh getting created
        issue2:only cut to 2 softbodies when cut number set to >3 Answer:the cut planes lie outside the boundary
    */
    public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects, GameObject target)
    {
        var coroutines = new List<IEnumerator>();
        var blueprints = new Dictionary<ObiSoftbody, ObiSoftbodySurfaceBlueprint>();

        foreach (var obj in objects.Select(x=>x.gameObject))
        {
            var meshFilter = obj.GetComponent<MeshFilter>();
            var softbody = obj.AddComponent<ObiSoftbody>();
            var skinner = obj.AddComponent<ObiSoftbodySkinner>();
            var blueprint = ScriptableObject.CreateInstance<ObiSoftbodySurfaceBlueprint>();
            blueprint.inputMesh = meshFilter.mesh;
            
            coroutines.Add(blueprint.Generate());
            //blueprints.Add(softbody, blueprint);//
        }
    
        yield return coroutineHost.StartCoroutine(UtilityHelper.RunAllCoroutines(coroutines));
    
        foreach (var item in blueprints)
        {
            item.Key.softbodyBlueprint = item.Value;
        }
    }
}