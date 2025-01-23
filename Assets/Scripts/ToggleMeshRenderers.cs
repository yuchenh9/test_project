using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleMeshRenderers : MonoBehaviour
{
    // Public field to control MeshRenderers on or off
    public bool renderersActive = true;

    // Function to toggle all child MeshRenderers on or off
    public void SetMeshRenderersActive(bool isActive)
    {
        // Get all MeshRenderer components in children of this GameObject
        MeshRenderer[] childMeshRenderers = GetComponentsInChildren<MeshRenderer>();

        // Loop through and set their enabled state
        foreach (MeshRenderer meshRenderer in childMeshRenderers)
        {
            meshRenderer.enabled = isActive;
        }
    }

    // Update MeshRenderers state based on the public field
    private void Update()
    {
        SetMeshRenderersActive(renderersActive);
    }
}
