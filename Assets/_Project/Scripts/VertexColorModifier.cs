using UnityEngine;

public class VertexColorModifier : MonoBehaviour
{
    public Color targetColor = Color.red; // Change this to any color you want
    public float bottomRange = 0.4f; // Range from bottom (0-1)

    private Color _lastTargetColor; // Track the last target color
    private float _lastBottomRange;
    private void OnValidate()
    {
        // Check if the targetColor has changed
        if (targetColor != _lastTargetColor || _lastBottomRange!=bottomRange)
        {
            _lastTargetColor = targetColor; // Update the last target color
            _lastBottomRange=bottomRange;
            ApplyVertexColor(targetColor); // Apply the new color
        }
    }

    void ApplyVertexColor(Color color)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        Mesh mesh = null;

        // Get the mesh from SkinnedMeshRenderer or MeshFilter
        if (skinnedMeshRenderer != null)
        {
            mesh = skinnedMeshRenderer.sharedMesh;
        }
        else if (meshFilter != null)
        {
            mesh = meshFilter.sharedMesh;
        }

        if (mesh == null)
        {
            Debug.LogError("Mesh is missing!");
            return;
        }

        // Create a copy of the mesh to avoid modifying the original asset
        Mesh modifiedMesh = Instantiate(mesh);

        Vector3[] vertices = modifiedMesh.vertices;
        Color[] colors = new Color[modifiedMesh.vertexCount];

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        // Find min and max Y positions
        foreach (var vertex in vertices)
        {
            if (vertex.z < minY) minY = vertex.z;
            if (vertex.z > maxY) maxY = vertex.z;
        }

        float threshold = minY + (maxY - minY) * bottomRange;

        for (int i = 0; i < colors.Length; i++)
        {
            if (vertices[i].z <= threshold)
                colors[i] = targetColor;
            else
                colors[i] = Color.white; // Keep other vertices white
        }

        modifiedMesh.colors = colors;

        // Assign the modified mesh back to the renderer
        if (skinnedMeshRenderer != null)
        {
            skinnedMeshRenderer.sharedMesh = modifiedMesh;
        }
        else if (meshFilter != null)
        {
            meshFilter.mesh = modifiedMesh;
        }
    }
}