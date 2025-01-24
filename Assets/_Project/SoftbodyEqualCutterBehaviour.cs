using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicMeshCutter;
using UnityEngine;
using Obi;

public class SoftbodyEqualCutterBehaviour : CutterBehaviour
{
    public Transform container;
    public MeshTarget targetObject;

    private int _sliceIndex;
    private Bounds _startBounds;
    private readonly List<GameObject> _slicedObjects = new();
    private MeshTarget _targetCopy;
    private int _sliceCount = 2;
    public Vector3 _slicingAxis = Vector3.right;
    
    public void Refresh()
    {
        foreach (var item in _slicedObjects.Where(item => item != null))
            Destroy(item.gameObject);
        _slicedObjects.Clear();
        _sliceIndex = 0;
        
        _startBounds = targetObject.GetComponent<SkinnedMeshRenderer>().bounds;
        _targetCopy = Instantiate(targetObject, container);
        _targetCopy.gameObject.SetActive(true);
        targetObject.gameObject.SetActive(false);
    }

    public void Cut(int sliceCount, Vector3 slicingAxis)
    {
        Refresh();
        _sliceCount = sliceCount;
        _slicingAxis = slicingAxis;
        SliceIntoEqualParts(_targetCopy);
    }

    private void SliceIntoEqualParts(MeshTarget nextObject)
    {
        Vector3 planePosition = CalculateSlicePlane(_sliceIndex + 1);
        Cut(nextObject, planePosition, _slicingAxis, OnCut, OnCreated);
    }
    
    private void OnCut(bool success, Info info)
    {
        var result = success ? "Success" : "Fail";
        Debug.Log($"Result: {result} SliceIndex: {_sliceIndex}\nInfo: {info}");
    }


    private void OnCreated(Info info, MeshCreationData cData)
    {
        MeshCreation.TranslateCreatedObjects(info, cData.CreatedObjects, cData.CreatedTargets, Separation);

        var firstPart = cData.CreatedTargets[1];
        var secondPart = cData.CreatedTargets[0];

        SetupPart(firstPart.gameObject);
        SetupPart(secondPart.gameObject);

        _sliceIndex++;

        if (_sliceIndex < _sliceCount - 1)
        {
            // this part will be destroyed (Softbody Optimization)
            SliceIntoEqualParts(secondPart);
        }
        else
        {
            // Last Part should be with softbody
            _slicedObjects.Add(cData.CreatedObjects[0].gameObject);
            StartCoroutine(CreateSoftbody(secondPart.gameObject));
        }
        
        // Add softbody to small part
        _slicedObjects.Add(cData.CreatedObjects[1].gameObject);
        StartCoroutine(CreateSoftbody(firstPart.gameObject));
    }

    private Vector3 CalculateSlicePlane(int step)
    {
        Vector3 projectedAxis = Vector3.Project(_startBounds.size, _slicingAxis.normalized);
        float stepSize = projectedAxis.magnitude / _sliceCount;
        Vector3 begin = _startBounds.center - projectedAxis / 2;
        var separationDistance = Separation * (step - 1);

        return begin + _slicingAxis.normalized * (stepSize * step + separationDistance);
    }

    private void SetupPart(GameObject part)
    {
        if (container != null)
        {
            part.transform.parent.parent = container;
        }

        var skinnedMeshRenderer = part.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = part.AddComponent<SkinnedMeshRenderer>();
        }

        skinnedMeshRenderer.bounds = _startBounds;
    }

    private IEnumerator CreateSoftbody(GameObject obj)
    {
        var softbody = obj.AddComponent<ObiSoftbody>();
        var skinner = obj.AddComponent<ObiSoftbodySkinner>();

        var blueprint = ScriptableObject.CreateInstance<ObiSoftbodySurfaceBlueprint>();
        blueprint.inputMesh = obj.GetComponent<MeshFilter>().mesh;

        yield return StartCoroutine(blueprint.Generate());

        softbody.softbodyBlueprint = blueprint;
    }
}
