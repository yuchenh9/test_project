using System;
using System.Collections;
using DynamicMeshCutter;
using Obi;
using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
[RequireComponent(typeof(ObiSoftbody))]
[RequireComponent(typeof(ObiSoftbodySkinner))]
public class SoftbodyGenerator : MonoBehaviour
{
    private ObiSoftbody _obiSoftbody;
    private SkinnedMeshRenderer _meshRenderer;
    
    private void Awake()
    {
        _obiSoftbody = GetComponent<ObiSoftbody>();
        _meshRenderer = GetComponent<SkinnedMeshRenderer>();
    }

    public IEnumerator Start()
    {
        _obiSoftbody.enabled = false;

        var blueprint = ScriptableObject.CreateInstance<ObiSoftbodySurfaceBlueprint>();
        blueprint.inputMesh = _meshRenderer.sharedMesh;
        
        yield return StartCoroutine(blueprint.Generate());
        
        _obiSoftbody.softbodyBlueprint = blueprint;
        _obiSoftbody.enabled = true;
    }
    
    public void CopySoftbodyProperties(GameObject target)
    {
        _obiSoftbody.enabled = false;

        var oldSoftbody = target.GetComponent<ObiSoftbody>();
        var meshRenderer = gameObject.GetComponent<MeshFilter>();
        
        _obiSoftbody.deformationResistance = oldSoftbody.deformationResistance;
        _obiSoftbody.softbodyBlueprint = oldSoftbody.softbodyBlueprint;
        _obiSoftbody.plasticRecovery = oldSoftbody.plasticRecovery;
        _obiSoftbody.maxDeformation = oldSoftbody.maxDeformation;
        _obiSoftbody.plasticCreep = oldSoftbody.plasticCreep;
        _obiSoftbody.plasticYield = oldSoftbody.plasticYield;
        _meshRenderer.sharedMesh = meshRenderer.mesh;
        _obiSoftbody.enabled = true;
    }
}