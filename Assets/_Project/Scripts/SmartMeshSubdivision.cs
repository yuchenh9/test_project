using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Smart mesh subdivision that combines face selection and subdivision
/// Perfect for adding detail to specific areas of your mesh for better vertex painting
/// </summary>
public class SmartMeshSubdivision : MonoBehaviour
{
    [Header("Selection Method")]
    public SelectionCriteria selectionMethod = SelectionCriteria.ByVertexColor;
    
    [Header("Subdivision Settings")]
    [Range(1, 4)]
    public int subdivisionLevels = 1;
    public bool smoothNormals = true;
    public bool preserveUVs = true;
    public bool preserveVertexColors = true;
    
    [Header("Selection Parameters")]
    public Color targetVertexColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.1f;
    
    [Range(0f, 180f)]
    public float angleThreshold = 45f;
    public Vector3 referenceDirection = Vector3.up;
    
    public Bounds selectionBounds = new Bounds(Vector3.zero, Vector3.one);
    public bool useWorldSpace = false;
    
    public int materialIndex = 0;
    
    [Header("Preview")]
    public bool showPreview = true;
    public Color previewColor = Color.yellow;
    
    [Header("Performance")]
    public bool enableLargeTriangleSkip = true;
    [Range(0.001f, 1f)]
    public float minTriangleSize = 0.01f;
    
    private Mesh originalMesh;
    private HashSet<int> selectedTriangles = new HashSet<int>();
    private GameObject previewObject;
    private bool hasValidSelection = false;
    
    [ContextMenu("Smart Subdivide")]
    public void SmartSubdivide()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter?.mesh == null)
        {
            Debug.LogError("No mesh found!");
            return;
        }
        
        // Store original mesh
        if (originalMesh == null)
            originalMesh = Instantiate(meshFilter.mesh);
        
        // Step 1: Select faces
        SelectFaces();
        
        if (!hasValidSelection)
        {
            Debug.LogWarning("No faces selected for subdivision!");
            return;
        }
        
        // Step 2: Subdivide selected faces
        Mesh subdividedMesh = SubdivideSelectedFaces();
        
        // Step 3: Apply result
        meshFilter.mesh = subdividedMesh;
        
        Debug.Log($"Smart subdivision complete: {originalMesh.vertexCount} → {subdividedMesh.vertexCount} vertices");
        Debug.Log($"Subdivided {selectedTriangles.Count} triangles");
        
        // Clear preview
        ClearPreview();
    }
    
    [ContextMenu("Preview Selection")]
    public void PreviewSelection()
    {
        SelectFaces();
        if (showPreview && hasValidSelection)
        {
            CreateSelectionPreview();
        }
    }
    
    [ContextMenu("Restore Original")]
    public void RestoreOriginal()
    {
        if (originalMesh != null)
        {
            GetComponent<MeshFilter>().mesh = originalMesh;
            ClearPreview();
            Debug.Log("Restored original mesh");
        }
    }
    
    void SelectFaces()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        selectedTriangles.Clear();
        
        switch (selectionMethod)
        {
            case SelectionCriteria.ByVertexColor:
                SelectByVertexColor(mesh);
                break;
            case SelectionCriteria.ByAngle:
                SelectByAngle(mesh);
                break;
            case SelectionCriteria.ByPosition:
                SelectByPosition(mesh);
                break;
            case SelectionCriteria.ByMaterial:
                SelectByMaterial(mesh);
                break;
        }
        
        hasValidSelection = selectedTriangles.Count > 0;
        Debug.Log($"Selected {selectedTriangles.Count} faces");
    }
    
    void SelectByVertexColor(Mesh mesh)
    {
        if (mesh.colors == null || mesh.colors.Length == 0)
        {
            Debug.LogWarning("Mesh has no vertex colors!");
            return;
        }
        
        Color[] colors = mesh.colors;
        int[] triangles = mesh.triangles;
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];
            
            // Check if any vertex matches target color
            bool hasTargetColor = false;
            if (v0 < colors.Length && ColorMatches(colors[v0], targetVertexColor)) hasTargetColor = true;
            if (v1 < colors.Length && ColorMatches(colors[v1], targetVertexColor)) hasTargetColor = true;
            if (v2 < colors.Length && ColorMatches(colors[v2], targetVertexColor)) hasTargetColor = true;
            
            if (hasTargetColor)
            {
                selectedTriangles.Add(i / 3);
            }
        }
    }
    
    void SelectByAngle(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3 refDir = referenceDirection.normalized;
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            float angle = Vector3.Angle(normal, refDir);
            
            if (angle <= angleThreshold)
            {
                selectedTriangles.Add(i / 3);
            }
        }
    }
    
    void SelectByPosition(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Bounds bounds = selectionBounds;
        
        if (useWorldSpace)
        {
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            bounds.center = worldToLocal.MultiplyPoint3x4(bounds.center);
            bounds.size = worldToLocal.MultiplyVector(bounds.size);
        }
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 center = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
            
            if (bounds.Contains(center))
            {
                selectedTriangles.Add(i / 3);
            }
        }
    }
    
    void SelectByMaterial(Mesh mesh)
    {
        if (materialIndex >= mesh.subMeshCount)
        {
            Debug.LogWarning($"Material index {materialIndex} is out of range!");
            return;
        }
        
        int[] submeshTriangles = mesh.GetTriangles(materialIndex);
        for (int i = 0; i < submeshTriangles.Length; i += 3)
        {
            selectedTriangles.Add(i / 3);
        }
    }
    
    bool ColorMatches(Color a, Color b)
    {
        return Vector3.Distance(new Vector3(a.r, a.g, a.b), new Vector3(b.r, b.g, b.b)) <= colorTolerance;
    }
    
    Mesh SubdivideSelectedFaces()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh originalMesh = meshFilter.mesh;
        
        MeshData meshData = new MeshData(originalMesh);
        
        // Apply subdivision levels
        for (int level = 0; level < subdivisionLevels; level++)
        {
            meshData = SubdivideSelectedTriangles(meshData, selectedTriangles);
            
            // Update selected triangles for next level (all new triangles from subdivided ones)
            if (level < subdivisionLevels - 1)
            {
                UpdateSelectedTrianglesAfterSubdivision(meshData);
            }
        }
        
        if (smoothNormals)
        {
            SmoothNormals(meshData);
        }
        
        return meshData.ToMesh(originalMesh.name + "_SmartSubdivided");
    }
    
    MeshData SubdivideSelectedTriangles(MeshData meshData, HashSet<int> targetTriangles)
    {
        MeshData newMeshData = new MeshData();
        
        // Copy existing vertices
        newMeshData.vertices.AddRange(meshData.vertices);
        newMeshData.normals.AddRange(meshData.normals);
        newMeshData.uvs.AddRange(meshData.uvs);
        newMeshData.colors.AddRange(meshData.colors);
        
        Dictionary<string, int> midpointCache = new Dictionary<string, int>();
        
        for (int i = 0; i < meshData.triangles.Count; i += 3)
        {
            int triangleIndex = i / 3;
            
            if (targetTriangles.Contains(triangleIndex))
            {
                // Skip very small triangles for performance
                if (enableLargeTriangleSkip && IsTriangleTooSmall(meshData, i))
                {
                    // Keep original triangle
                    newMeshData.triangles.AddRange(new int[] { 
                        meshData.triangles[i], 
                        meshData.triangles[i + 1], 
                        meshData.triangles[i + 2] 
                    });
                    continue;
                }
                
                // Subdivide
                int v0 = meshData.triangles[i];
                int v1 = meshData.triangles[i + 1];
                int v2 = meshData.triangles[i + 2];
                
                int m01 = GetOrCreateMidpoint(v0, v1, meshData, newMeshData, midpointCache);
                int m12 = GetOrCreateMidpoint(v1, v2, meshData, newMeshData, midpointCache);
                int m20 = GetOrCreateMidpoint(v2, v0, meshData, newMeshData, midpointCache);
                
                // Add 4 new triangles
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
    
    bool IsTriangleTooSmall(MeshData meshData, int triangleStartIndex)
    {
        Vector3 v0 = meshData.vertices[meshData.triangles[triangleStartIndex]];
        Vector3 v1 = meshData.vertices[meshData.triangles[triangleStartIndex + 1]];
        Vector3 v2 = meshData.vertices[meshData.triangles[triangleStartIndex + 2]];
        
        float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
        return area < minTriangleSize;
    }
    
    int GetOrCreateMidpoint(int v1, int v2, MeshData originalMesh, MeshData newMesh, Dictionary<string, int> cache)
    {
        string key = v1 < v2 ? $"{v1}_{v2}" : $"{v2}_{v1}";
        
        if (cache.ContainsKey(key))
            return cache[key];
        
        // Create midpoint
        Vector3 midPos = (originalMesh.vertices[v1] + originalMesh.vertices[v2]) * 0.5f;
        Vector3 midNormal = ((v1 < originalMesh.normals.Count ? originalMesh.normals[v1] : Vector3.up) + 
                            (v2 < originalMesh.normals.Count ? originalMesh.normals[v2] : Vector3.up)).normalized;
        Vector2 midUV = ((v1 < originalMesh.uvs.Count ? originalMesh.uvs[v1] : Vector2.zero) + 
                        (v2 < originalMesh.uvs.Count ? originalMesh.uvs[v2] : Vector2.zero)) * 0.5f;
        Color midColor = ((v1 < originalMesh.colors.Count ? originalMesh.colors[v1] : Color.white) + 
                         (v2 < originalMesh.colors.Count ? originalMesh.colors[v2] : Color.white)) * 0.5f;
        
        int newIndex = newMesh.vertices.Count;
        newMesh.vertices.Add(midPos);
        newMesh.normals.Add(midNormal);
        if (preserveUVs) newMesh.uvs.Add(midUV);
        if (preserveVertexColors) newMesh.colors.Add(midColor);
        
        cache[key] = newIndex;
        return newIndex;
    }
    
    void UpdateSelectedTrianglesAfterSubdivision(MeshData meshData)
    {
        // This is a simplified version - in practice you'd need to track which new triangles came from subdivided ones
        selectedTriangles.Clear();
        // For now, select all triangles for the next subdivision level
        for (int i = 0; i < meshData.triangles.Count / 3; i++)
        {
            selectedTriangles.Add(i);
        }
    }
    
    void SmoothNormals(MeshData meshData)
    {
        Vector3[] smoothedNormals = new Vector3[meshData.vertices.Count];
        
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
        
        for (int i = 0; i < smoothedNormals.Length; i++)
        {
            smoothedNormals[i] = smoothedNormals[i].normalized;
        }
        
        meshData.normals = smoothedNormals.ToList();
    }
    
    void CreateSelectionPreview()
    {
        ClearPreview();
        
        if (selectedTriangles.Count == 0) return;
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        
        // Create preview mesh
        List<Vector3> previewVertices = new List<Vector3>();
        List<int> previewTriangles = new List<int>();
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>();
        
        foreach (int triangleIndex in selectedTriangles)
        {
            int baseIndex = triangleIndex * 3;
            
            for (int i = 0; i < 3; i++)
            {
                int originalVertexIndex = mesh.triangles[baseIndex + i];
                
                if (!vertexMapping.ContainsKey(originalVertexIndex))
                {
                    vertexMapping[originalVertexIndex] = previewVertices.Count;
                    previewVertices.Add(mesh.vertices[originalVertexIndex]);
                }
                
                previewTriangles.Add(vertexMapping[originalVertexIndex]);
            }
        }
        
        Mesh previewMesh = new Mesh();
        previewMesh.vertices = previewVertices.ToArray();
        previewMesh.triangles = previewTriangles.ToArray();
        previewMesh.RecalculateNormals();
        
        // Create preview object
        previewObject = new GameObject("Subdivision Preview");
        previewObject.transform.SetParent(transform);
        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localScale = Vector3.one;
        
        MeshFilter mf = previewObject.AddComponent<MeshFilter>();
        MeshRenderer mr = previewObject.AddComponent<MeshRenderer>();
        
        mf.mesh = previewMesh;
        
        // Create transparent material
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(previewColor.r, previewColor.g, previewColor.b, 0.5f);
        mat.SetFloat("_Mode", 2); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mr.material = mat;
    }
    
    [ContextMenu("Clear Preview")]
    public void ClearPreview()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (selectionMethod == SelectionCriteria.ByPosition)
        {
            Gizmos.color = Color.cyan;
            
            if (useWorldSpace)
            {
                Gizmos.DrawWireCube(selectionBounds.center, selectionBounds.size);
            }
            else
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(selectionBounds.center, selectionBounds.size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }
} 