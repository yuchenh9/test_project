using UnityEngine;

public class ToggleMeshRenderers : MonoBehaviour
{
    public bool toggleState = true; // True = Show, False = Hide


    public void SetMeshRenderers(bool state)
    {
        // Get all MeshRenderers in children (including nested)
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);

        // Enable or disable each MeshRenderer
        foreach (MeshRenderer mesh in meshRenderers)
        {
            mesh.enabled = state;
        }

        Debug.Log("All MeshRenderers have been " + (state ? "enabled" : "disabled"));
    }
    void Start()
    {
        SetMeshRenderers(toggleState);
    }
    void Update()
    {
        
    }
}
