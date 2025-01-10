using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class VertexColorEditor : MonoBehaviour
{
    [Header("Vertex Color Settings")]
    public Color vertexColor = Color.white; // Default to white

    private MeshFilter meshFilter;

    private void OnValidate()
    {
        ApplyVertexColors();
    }

    private void ApplyVertexColors()
    {
        // Get the MeshFilter component
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("MeshFilter or Mesh is missing!");
            return;
        }

        // Get the mesh and its vertices
        Mesh mesh = meshFilter.sharedMesh;

        // Initialize vertex colors array if needed
        Color[] colors = new Color[mesh.vertexCount];

        // Assign the selected color to all vertices
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = vertexColor;
        }

        // Update the mesh colors
        mesh.colors = colors;

        Debug.Log("Vertex colors updated!");
    }
}
