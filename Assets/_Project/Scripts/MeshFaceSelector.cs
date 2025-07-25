using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum SelectionCriteria
{
    ByMaterial,
    ByAngle,
    ByPosition,
    ByVertexColor,
    Manual
}

[System.Serializable]
public class FaceSelectionSettings
{
    public SelectionCriteria criteria = SelectionCriteria.ByMaterial;
    
    [Header("Material Selection")]
    public Material targetMaterial;
    public int materialIndex = 0;
    
    [Header("Angle Selection")]
    [Range(0f, 180f)]
    public float angleThreshold = 45f;
    public Vector3 referenceDirection = Vector3.up;
    
    [Header("Position Selection")]
    public Bounds selectionBounds = new Bounds(Vector3.zero, Vector3.one);
    public bool useWorldSpace = false;
    
    [Header("Vertex Color Selection")]
    public Color targetColor = Color.red;
    [Range(0f, 1f)]
    public float colorTolerance = 0.1f;
    
    [Header("Manual Selection")]
    public int[] manualTriangleIndices;
}

public class MeshFaceSelector : MonoBehaviour
{
    [Header("Selection Settings")]
    public FaceSelectionSettings settings = new FaceSelectionSettings();
    
    [Header("Visualization")]
    public bool showSelectedFaces = true;
    public Color selectionColor = Color.yellow;
    public Material previewMaterial;
    
    private HashSet<int> selectedTriangles = new HashSet<int>();
    private Mesh mesh;
    private GameObject previewObject;
    
    void Start()
    {
        mesh = GetComponent<MeshFilter>()?.mesh;
        if (mesh == null)
        {
            Debug.LogError("No mesh found on MeshFaceSelector!");
        }
    }
    
    [ContextMenu("Select Faces")]
    public void SelectFaces()
    {
        if (mesh == null) return;
        
        selectedTriangles.Clear();
        
        switch (settings.criteria)
        {
            case SelectionCriteria.ByMaterial:
                SelectByMaterial();
                break;
            case SelectionCriteria.ByAngle:
                SelectByAngle();
                break;
            case SelectionCriteria.ByPosition:
                SelectByPosition();
                break;
            case SelectionCriteria.ByVertexColor:
                SelectByVertexColor();
                break;
            case SelectionCriteria.Manual:
                SelectManually();
                break;
        }
        
        Debug.Log($"Selected {selectedTriangles.Count} triangles for subdivision");
        
        if (showSelectedFaces)
        {
            CreatePreview();
        }
        
        // Apply selection to MeshSubdivision component if present
        MeshSubdivision subdivision = GetComponent<MeshSubdivision>();
        if (subdivision != null)
        {
            subdivision.subdivideEntireMesh = false;
            // Convert triangle indices to submesh format (simplified)
            subdivision.targetSubmeshes = new int[] { 0 }; // You'd need to map this properly
        }
    }
    
    void SelectByMaterial()
    {
        if (mesh.subMeshCount <= settings.materialIndex) return;
        
        int[] submeshTriangles = mesh.GetTriangles(settings.materialIndex);
        for (int i = 0; i < submeshTriangles.Length; i += 3)
        {
            selectedTriangles.Add(i / 3);
        }
    }
    
    void SelectByAngle()
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3 refDir = settings.referenceDirection.normalized;
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Calculate triangle normal
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            
            // Check angle with reference direction
            float angle = Vector3.Angle(normal, refDir);
            if (angle <= settings.angleThreshold)
            {
                selectedTriangles.Add(i / 3);
            }
        }
    }
    
    void SelectByPosition()
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Bounds bounds = settings.selectionBounds;
        
        if (settings.useWorldSpace)
        {
            // Convert bounds to local space
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            bounds.center = worldToLocal.MultiplyPoint3x4(bounds.center);
            bounds.size = worldToLocal.MultiplyVector(bounds.size);
        }
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Check if triangle center is within bounds
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            
            Vector3 center = (v0 + v1 + v2) / 3f;
            
            if (bounds.Contains(center))
            {
                selectedTriangles.Add(i / 3);
            }
        }
    }
    
    void SelectByVertexColor()
    {
        if (mesh.colors == null || mesh.colors.Length == 0) return;
        
        Color[] colors = mesh.colors;
        int[] triangles = mesh.triangles;
        Color targetColor = settings.targetColor;
        float tolerance = settings.colorTolerance;
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];
            
            // Check if any vertex has the target color
            bool hasTargetColor = false;
            
            if (v0 < colors.Length && ColorDistance(colors[v0], targetColor) <= tolerance) hasTargetColor = true;
            if (v1 < colors.Length && ColorDistance(colors[v1], targetColor) <= tolerance) hasTargetColor = true;
            if (v2 < colors.Length && ColorDistance(colors[v2], targetColor) <= tolerance) hasTargetColor = true;
            
            if (hasTargetColor)
            {
                selectedTriangles.Add(i / 3);
            }
        }
    }
    
    void SelectManually()
    {
        if (settings.manualTriangleIndices == null) return;
        
        foreach (int triangleIndex in settings.manualTriangleIndices)
        {
            if (triangleIndex >= 0 && triangleIndex < mesh.triangles.Length / 3)
            {
                selectedTriangles.Add(triangleIndex);
            }
        }
    }
    
    float ColorDistance(Color a, Color b)
    {
        return Mathf.Sqrt(
            Mathf.Pow(a.r - b.r, 2) +
            Mathf.Pow(a.g - b.g, 2) +
            Mathf.Pow(a.b - b.b, 2)
        );
    }
    
    void CreatePreview()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
        
        if (selectedTriangles.Count == 0) return;
        
        // Create preview mesh with only selected triangles
        Mesh previewMesh = CreateSelectedTrianglesMesh();
        
        previewObject = new GameObject("Selected Faces Preview");
        previewObject.transform.SetParent(transform);
        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localScale = Vector3.one;
        
        MeshFilter mf = previewObject.AddComponent<MeshFilter>();
        MeshRenderer mr = previewObject.AddComponent<MeshRenderer>();
        
        mf.mesh = previewMesh;
        
        if (previewMaterial != null)
        {
            mr.material = previewMaterial;
        }
        else
        {
            // Create simple colored material
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = selectionColor;
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
    }
    
    Mesh CreateSelectedTrianglesMesh()
    {
        Vector3[] originalVertices = mesh.vertices;
        int[] originalTriangles = mesh.triangles;
        
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>();
        
        foreach (int triangleIndex in selectedTriangles)
        {
            int baseIndex = triangleIndex * 3;
            
            for (int i = 0; i < 3; i++)
            {
                int originalVertexIndex = originalTriangles[baseIndex + i];
                
                if (!vertexMapping.ContainsKey(originalVertexIndex))
                {
                    vertexMapping[originalVertexIndex] = newVertices.Count;
                    newVertices.Add(originalVertices[originalVertexIndex]);
                }
                
                newTriangles.Add(vertexMapping[originalVertexIndex]);
            }
        }
        
        Mesh result = new Mesh();
        result.vertices = newVertices.ToArray();
        result.triangles = newTriangles.ToArray();
        result.RecalculateNormals();
        result.RecalculateBounds();
        
        return result;
    }
    
    public HashSet<int> GetSelectedTriangles()
    {
        return selectedTriangles;
    }
    
    [ContextMenu("Clear Selection")]
    public void ClearSelection()
    {
        selectedTriangles.Clear();
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
        Debug.Log("Selection cleared");
    }
    
    void OnDrawGizmosSelected()
    {
        if (settings.criteria == SelectionCriteria.ByPosition)
        {
            Gizmos.color = Color.yellow;
            
            if (settings.useWorldSpace)
            {
                Gizmos.DrawWireCube(settings.selectionBounds.center, settings.selectionBounds.size);
            }
            else
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(settings.selectionBounds.center, settings.selectionBounds.size);
                Gizmos.matrix = oldMatrix;
            }
        }
        
        if (settings.criteria == SelectionCriteria.ByAngle)
        {
            Gizmos.color = Color.blue;
            Vector3 worldPos = transform.position;
            Vector3 worldDir = settings.useWorldSpace ? 
                settings.referenceDirection : 
                transform.TransformDirection(settings.referenceDirection);
            
            Gizmos.DrawRay(worldPos, worldDir.normalized * 2f);
        }
    }
} 