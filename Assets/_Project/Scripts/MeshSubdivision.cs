using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SubdivisionSettings
{
    [Range(1, 5)]
    public int subdivisionLevels = 1;
    public bool smoothNormals = true;
    public bool preserveUVs = true;
    public bool preserveVertexColors = true;
}

public class MeshSubdivision : MonoBehaviour
{
    [Header("Subdivision Settings")]
    public SubdivisionSettings settings = new SubdivisionSettings();
    
    [Header("Target Selection")]
    public bool subdivideEntireMesh = true;
    public int[] targetSubmeshes = { 0 }; // Which submeshes to subdivide
    
    [Header("Debug")]
    public bool showOriginalMesh = false;
    
    private Mesh originalMesh;
    private Mesh subdividedMesh;
    
    [ContextMenu("Subdivide Mesh")]
    public void SubdivideMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("No MeshFilter or mesh found!");
            return;
        }
        
        if (originalMesh == null)
            originalMesh = Instantiate(meshFilter.mesh);
        
        subdividedMesh = subdivideEntireMesh ? 
            SubdivideEntireMesh(originalMesh) : 
            SubdivideSelectedSubmeshes(originalMesh, targetSubmeshes);
        
        meshFilter.mesh = subdividedMesh;
        
        Debug.Log($"Subdivision complete: {originalMesh.vertexCount} → {subdividedMesh.vertexCount} vertices");
    }
    
    [ContextMenu("Restore Original")]
    public void RestoreOriginal()
    {
        if (originalMesh != null)
        {
            GetComponent<MeshFilter>().mesh = showOriginalMesh ? originalMesh : subdividedMesh;
        }
    }
    
    /// <summary>
    /// Subdivides the entire mesh
    /// </summary>
    Mesh SubdivideEntireMesh(Mesh mesh)
    {
        MeshData meshData = new MeshData(mesh);
        
        for (int level = 0; level < settings.subdivisionLevels; level++)
        {
            meshData = SubdivideTriangles(meshData);
        }
        
        return meshData.ToMesh(mesh.name + "_Subdivided");
    }
    
    /// <summary>
    /// Subdivides only selected submeshes
    /// </summary>
    Mesh SubdivideSelectedSubmeshes(Mesh mesh, int[] submeshIndices)
    {
        MeshData meshData = new MeshData(mesh);
        
        // Create a set of triangle indices that belong to target submeshes
        HashSet<int> targetTriangles = new HashSet<int>();
        
        foreach (int submeshIndex in submeshIndices)
        {
            if (submeshIndex >= 0 && submeshIndex < mesh.subMeshCount)
            {
                int[] submeshTriangles = mesh.GetTriangles(submeshIndex);
                for (int i = 0; i < submeshTriangles.Length; i += 3)
                {
                    targetTriangles.Add(i / 3); // Triangle index
                }
            }
        }
        
        for (int level = 0; level < settings.subdivisionLevels; level++)
        {
            meshData = SubdivideSelectedTriangles(meshData, targetTriangles);
        }
        
        return meshData.ToMesh(mesh.name + "_PartialSubdivided");
    }
    
    /// <summary>
    /// Core subdivision algorithm - splits each triangle into 4 triangles
    /// </summary>
    MeshData SubdivideTriangles(MeshData meshData)
    {
        MeshData newMeshData = new MeshData();
        
        // Copy existing vertices
        newMeshData.vertices.AddRange(meshData.vertices);
        newMeshData.normals.AddRange(meshData.normals);
        newMeshData.uvs.AddRange(meshData.uvs);
        newMeshData.colors.AddRange(meshData.colors);
        
        // Dictionary to store midpoint vertices (to avoid duplicates)
        Dictionary<string, int> midpointCache = new Dictionary<string, int>();
        
        // Process each triangle
        for (int i = 0; i < meshData.triangles.Count; i += 3)
        {
            int v0 = meshData.triangles[i];
            int v1 = meshData.triangles[i + 1];
            int v2 = meshData.triangles[i + 2];
            
            // Get or create midpoint vertices
            int m01 = GetOrCreateMidpoint(v0, v1, meshData, newMeshData, midpointCache);
            int m12 = GetOrCreateMidpoint(v1, v2, meshData, newMeshData, midpointCache);
            int m20 = GetOrCreateMidpoint(v2, v0, meshData, newMeshData, midpointCache);
            
            // Create 4 new triangles from the original triangle
            // Original triangle becomes 4 triangles:
            //       v2
            //      /  \
            //    m20--m12
            //   /  \  /  \
            //  v0--m01--v1
            
            // Triangle 1: v0, m01, m20
            newMeshData.triangles.AddRange(new int[] { v0, m01, m20 });
            
            // Triangle 2: m01, v1, m12  
            newMeshData.triangles.AddRange(new int[] { m01, v1, m12 });
            
            // Triangle 3: m20, m12, v2
            newMeshData.triangles.AddRange(new int[] { m20, m12, v2 });
            
            // Triangle 4: m01, m12, m20 (center triangle)
            newMeshData.triangles.AddRange(new int[] { m01, m12, m20 });
        }
        
        if (settings.smoothNormals)
        {
            SmoothNormals(newMeshData);
        }
        
        return newMeshData;
    }
    
    /// <summary>
    /// Subdivides only selected triangles
    /// </summary>
    MeshData SubdivideSelectedTriangles(MeshData meshData, HashSet<int> targetTriangles)
    {
        MeshData newMeshData = new MeshData();
        
        // Copy existing vertices
        newMeshData.vertices.AddRange(meshData.vertices);
        newMeshData.normals.AddRange(meshData.normals);
        newMeshData.uvs.AddRange(meshData.uvs);
        newMeshData.colors.AddRange(meshData.colors);
        
        Dictionary<string, int> midpointCache = new Dictionary<string, int>();
        
        // Process each triangle
        for (int i = 0; i < meshData.triangles.Count; i += 3)
        {
            int triangleIndex = i / 3;
            
            if (targetTriangles.Contains(triangleIndex))
            {
                // Subdivide this triangle
                int v0 = meshData.triangles[i];
                int v1 = meshData.triangles[i + 1];
                int v2 = meshData.triangles[i + 2];
                
                int m01 = GetOrCreateMidpoint(v0, v1, meshData, newMeshData, midpointCache);
                int m12 = GetOrCreateMidpoint(v1, v2, meshData, newMeshData, midpointCache);
                int m20 = GetOrCreateMidpoint(v2, v0, meshData, newMeshData, midpointCache);
                
                // Add 4 subdivided triangles
                newMeshData.triangles.AddRange(new int[] { v0, m01, m20 });
                newMeshData.triangles.AddRange(new int[] { m01, v1, m12 });
                newMeshData.triangles.AddRange(new int[] { m20, m12, v2 });
                newMeshData.triangles.AddRange(new int[] { m01, m12, m20 });
            }
            else
            {
                // Keep original triangle
                newMeshData.triangles.AddRange(new int[] { 
                    meshData.triangles[i], 
                    meshData.triangles[i + 1], 
                    meshData.triangles[i + 2] 
                });
            }
        }
        
        return newMeshData;
    }
    
    /// <summary>
    /// Gets or creates a midpoint vertex between two vertices
    /// </summary>
    int GetOrCreateMidpoint(int v1, int v2, MeshData originalMesh, MeshData newMesh, Dictionary<string, int> cache)
    {
        // Create consistent key (smaller index first)
        string key = v1 < v2 ? $"{v1}_{v2}" : $"{v2}_{v1}";
        
        if (cache.ContainsKey(key))
        {
            return cache[key];
        }
        
        // Create new midpoint vertex
        Vector3 pos1 = originalMesh.vertices[v1];
        Vector3 pos2 = originalMesh.vertices[v2];
        Vector3 midPos = (pos1 + pos2) * 0.5f;
        
        Vector3 normal1 = v1 < originalMesh.normals.Count ? originalMesh.normals[v1] : Vector3.up;
        Vector3 normal2 = v2 < originalMesh.normals.Count ? originalMesh.normals[v2] : Vector3.up;
        Vector3 midNormal = (normal1 + normal2).normalized;
        
        Vector2 uv1 = v1 < originalMesh.uvs.Count ? originalMesh.uvs[v1] : Vector2.zero;
        Vector2 uv2 = v2 < originalMesh.uvs.Count ? originalMesh.uvs[v2] : Vector2.zero;
        Vector2 midUV = (uv1 + uv2) * 0.5f;
        
        Color color1 = v1 < originalMesh.colors.Count ? originalMesh.colors[v1] : Color.white;
        Color color2 = v2 < originalMesh.colors.Count ? originalMesh.colors[v2] : Color.white;
        Color midColor = (color1 + color2) * 0.5f;
        
        int newIndex = newMesh.vertices.Count;
        newMesh.vertices.Add(midPos);
        newMesh.normals.Add(midNormal);
        if (settings.preserveUVs) newMesh.uvs.Add(midUV);
        if (settings.preserveVertexColors) newMesh.colors.Add(midColor);
        
        cache[key] = newIndex;
        return newIndex;
    }
    
    /// <summary>
    /// Smooths normals by averaging adjacent face normals
    /// </summary>
    void SmoothNormals(MeshData meshData)
    {
        Vector3[] smoothedNormals = new Vector3[meshData.vertices.Count];
        
        // Calculate face normals and accumulate at vertices
        for (int i = 0; i < meshData.triangles.Count; i += 3)
        {
            int v0 = meshData.triangles[i];
            int v1 = meshData.triangles[i + 1];
            int v2 = meshData.triangles[i + 2];
            
            Vector3 faceNormal = Vector3.Cross(
                meshData.vertices[v1] - meshData.vertices[v0],
                meshData.vertices[v2] - meshData.vertices[v0]
            ).normalized;
            
            smoothedNormals[v0] += faceNormal;
            smoothedNormals[v1] += faceNormal;
            smoothedNormals[v2] += faceNormal;
        }
        
        // Normalize accumulated normals
        for (int i = 0; i < smoothedNormals.Length; i++)
        {
            smoothedNormals[i] = smoothedNormals[i].normalized;
        }
        
        meshData.normals = smoothedNormals.ToList();
    }
}

/// <summary>
/// Helper class to manage mesh data during subdivision
/// </summary>
[System.Serializable]
public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    public List<Vector2> uvs = new List<Vector2>();
    public List<Color> colors = new List<Color>();
    public List<int> triangles = new List<int>();
    
    public MeshData() { }
    
    public MeshData(Mesh mesh)
    {
        vertices.AddRange(mesh.vertices);
        normals.AddRange(mesh.normals);
        uvs.AddRange(mesh.uv);
        colors.AddRange(mesh.colors);
        triangles.AddRange(mesh.triangles);
    }
    
    public Mesh ToMesh(string name = "SubdividedMesh")
    {
        Mesh mesh = new Mesh();
        mesh.name = name;
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        
        if (normals.Count > 0)
            mesh.normals = normals.ToArray();
        else
            mesh.RecalculateNormals();
            
        if (uvs.Count > 0)
            mesh.uv = uvs.ToArray();
            
        if (colors.Count > 0)
            mesh.colors = colors.ToArray();
        
        mesh.RecalculateBounds();
        return mesh;
    }
} 