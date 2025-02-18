using UnityEngine;

public class VertexColorModifier : MonoBehaviour
{
    public Color targetColor = Color.red; // Change this to any color you want
    public float bottomRange = 0.4f; // Range from bottom (0-1)

    private Color _lastTargetColor; // Track the last target color
    private float _lastBottomRange;


    public enum Axis { X, Y, Z } // Enum to define the axis options
    public Axis selectedAxis = Axis.Y; 
    private float GetAxisValue(Vector3 worldVertex)
    {
        switch (selectedAxis)
        {
            case Axis.X:
                return worldVertex.x;
            case Axis.Y:
                return worldVertex.y;
            case Axis.Z:
                return worldVertex.z;
            default:
                return worldVertex.y; // Default to Y-axis
        }
    }
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

        float minAxisValue = float.MaxValue;
        float maxAxisValue = float.MinValue;

        // Find min and max Y positions
         for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(vertices[i]); // Convert to world space
            float axisValue = GetAxisValue(worldVertex); // Get the value for the selected axis

            if (axisValue < minAxisValue) minAxisValue = axisValue;
            if (axisValue > maxAxisValue) maxAxisValue = axisValue;
        }

        float threshold = minAxisValue + (maxAxisValue - minAxisValue) * bottomRange;

        for (int i = 0; i < colors.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(vertices[i]); // Convert to world space
            float axisValue = GetAxisValue(worldVertex); // Get the value for the selected axis

            if (axisValue <= threshold)
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