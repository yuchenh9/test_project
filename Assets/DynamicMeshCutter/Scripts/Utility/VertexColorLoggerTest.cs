using UnityEngine;

namespace DynamicMeshCutter
{
    /// <summary>
    /// Test script to manually trigger vertex color logging
    /// Attach this to any GameObject with a MeshFilter to test the logging functionality
    /// </summary>
    public class VertexColorLoggerTest : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] private bool logOnStart = true;
        [SerializeField] private KeyCode logKey = KeyCode.L;
        
        void Start()
        {
            if (logOnStart)
            {
                LogCurrentVertexColors();
            }
        }
        
        void Update()
        {
            if (Input.GetKeyDown(logKey))
            {
                LogCurrentVertexColors();
            }
        }
        
        [ContextMenu("Log Vertex Colors")]
        public void LogCurrentVertexColors()
        {
            VertexColorLogger.LogVertexColors(gameObject, "Manual Test");
        }
        
        /// <summary>
        /// Create a simple test mesh with vertex colors for testing
        /// </summary>
        [ContextMenu("Create Test Mesh with Colors")]
        public void CreateTestMeshWithColors()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                // Try to find a default material
                Material defaultMaterial = Resources.Load<Material>("Default-Material");
                if (defaultMaterial == null)
                {
                    // Create a simple unlit material if no default is found
                    defaultMaterial = new Material(Shader.Find("Unlit/Color"));
                    defaultMaterial.color = Color.white;
                }
                meshRenderer.material = defaultMaterial;
            }
            
            // Create a simple quad mesh with vertex colors
            Mesh testMesh = new Mesh();
            testMesh.name = "Test Quad with Colors";
            
            // Define vertices for a quad
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-1, -1, 0), // Bottom left
                new Vector3(1, -1, 0),  // Bottom right
                new Vector3(-1, 1, 0),  // Top left
                new Vector3(1, 1, 0)    // Top right
            };
            
            // Define triangles
            int[] triangles = new int[]
            {
                0, 2, 1,  // First triangle
                2, 3, 1   // Second triangle
            };
            
            // Define vertex colors (different color for each vertex)
            Color[] colors = new Color[]
            {
                Color.red,     // Bottom left - Red
                Color.green,   // Bottom right - Green
                Color.blue,    // Top left - Blue
                Color.yellow   // Top right - Yellow
            };
            
            // Define UVs
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            
            testMesh.vertices = vertices;
            testMesh.triangles = triangles;
            testMesh.colors = colors;
            testMesh.uv = uvs;
            testMesh.RecalculateNormals();
            
            meshFilter.mesh = testMesh;
            
            Debug.Log("Created test mesh with vertex colors. Use the 'Log Vertex Colors' context menu or press L to log the colors.");
        }
    }
} 