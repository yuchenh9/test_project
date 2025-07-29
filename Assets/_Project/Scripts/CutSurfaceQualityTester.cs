using UnityEngine;
using DynamicMeshCutter;

namespace CutSurfaceTesting
{
    /// <summary>
    /// Test script to demonstrate improved cut surface triangulation
    /// Shows the difference between original and subdivided cut surfaces
    /// </summary>
    public class CutSurfaceQualityTester : MonoBehaviour
    {
        [Header("Cut Surface Quality Settings")]
        [Tooltip("Subdivision level for cut surfaces (1=original, 2=1+2*1=3 triangles, 3=1+2*2=5 triangles, 4=1+2*3=7 triangles)")]
        [Range(1, 4)]
        public int subdivisionLevel = 2;
        
        [Tooltip("Test object to slice")]
        public GameObject testObject;
        
        [Tooltip("Cut plane position")]
        public Vector3 cutPosition = Vector3.zero;
        
        [Tooltip("Cut plane normal")]
        public Vector3 cutNormal = Vector3.up;
        
        [Header("Runtime Controls")]
        [Tooltip("Key to perform test cut")]
        public KeyCode testCutKey = KeyCode.C;
        
        [Tooltip("Key to cycle through subdivision levels")]
        public KeyCode cycleSubdivisionKey = KeyCode.S;
        
        [Header("Results")]
        [SerializeField] private int lastCutTriangleCount = 0;
        [SerializeField] private float lastCutTime = 0f;
        
        private CutterBehaviour cutter;
        private MeshFilter meshFilter;
        private SkinnedMeshRenderer skinnedMeshRenderer;
        
        void Start()
        {
            // Get or create cutter
            cutter = FindObjectOfType<CutterBehaviour>();
            if (cutter == null)
            {
                GameObject cutterObj = new GameObject("CutterBehaviour");
                cutter = cutterObj.AddComponent<CutterBehaviour>();
            }
            
            // Set subdivision level
            cutter.CutSurfaceSubdivisionLevel = subdivisionLevel;
            
            // Get test object
            if (testObject == null)
            {
                testObject = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("steak");
            }
            
            if (testObject != null)
            {
                meshFilter = testObject.GetComponent<MeshFilter>();
                skinnedMeshRenderer = testObject.GetComponent<SkinnedMeshRenderer>();
            }
            
            //Debug.Log($"CutSurfaceQualityTester initialized. Subdivision Level: {subdivisionLevel}");
        }
        
        void Update()
        {
            // Test cut
            if (Input.GetKeyDown(testCutKey))
            {
                PerformTestCut();
            }
            
            // Cycle subdivision level
            if (Input.GetKeyDown(cycleSubdivisionKey))
            {
                subdivisionLevel = (subdivisionLevel % 4) + 1; // Cycle between 1, 2, 3, 4
                cutter.CutSurfaceSubdivisionLevel = subdivisionLevel;
                int n = Mathf.Max(0, subdivisionLevel - 1);
                int triangleCount = 1 + 2 * n;
                Debug.Log($"Subdivision level changed to: {subdivisionLevel} (n={n}, triangles per face: {triangleCount})");
            }
        }
        
        /// <summary>
        /// Perform a test cut with current settings
        /// </summary>
        public void PerformTestCut()
        {
            if (testObject == null)
            {
                Debug.LogError("No test object found!");
                return;
            }
            
            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            // Get mesh target
            MeshTarget meshTarget = testObject.GetComponent<MeshTarget>();
            if (meshTarget == null)
            {
                meshTarget = testObject.AddComponent<MeshTarget>();
            }
            
            // Perform cut
            cutter.Cut(meshTarget, cutPosition, cutNormal, 
                (success, info) => {
                    if (success)
                    {
                        watch.Stop();
                        lastCutTime = watch.ElapsedMilliseconds;
                        
                        // Count triangles in cut surfaces
                        int totalTriangles = 0;
                        if (info.CreatedMeshes != null)
                        {
                            foreach (var mesh in info.CreatedMeshes)
                            {
                                totalTriangles += mesh.Triangles.Length / 3;
                            }
                        }
                        lastCutTriangleCount = totalTriangles;
                        
                        //Debug.Log($"Cut successful! Created {info.CreatedMeshes?.Length ?? 0} meshes with {totalTriangles} triangles. Time: {lastCutTime}ms");
                        //Debug.Log($"Subdivision level used: {subdivisionLevel}");
                    }
                    else
                    {
                        Debug.LogError("Cut failed!");
                    }
                },
                (info, creationData) => {
                    //Debug.Log($"Cut objects created successfully");
                }
            );
        }
        
        /// <summary>
        /// Test different subdivision levels on the same object
        /// </summary>
        [ContextMenu("Test All Subdivision Levels")]
        public void TestAllSubdivisionLevels()
        {
            StartCoroutine(TestSubdivisionLevels());
        }
        
        private System.Collections.IEnumerator TestSubdivisionLevels()
        {
            Debug.Log("Testing all subdivision levels...");
            
            for (int level = 1; level <= 4; level++)
            {
                subdivisionLevel = level;
                cutter.CutSurfaceSubdivisionLevel = level;
                
                Debug.Log($"Testing subdivision level {level}...");
                PerformTestCut();
                
                // Wait a bit between tests
                yield return new WaitForSeconds(1f);
            }
            
            Debug.Log("Subdivision level testing complete!");
        }
        
        void OnGUI()
        {
            if (Application.isPlaying)
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 220));
                GUILayout.Label("Cut Surface Quality Tester");
                int n = Mathf.Max(0, subdivisionLevel - 1);
                int trianglesPerFace = 1 + 2 * n;
                GUILayout.Label($"Subdivision Level: {subdivisionLevel} (n={n})");
                GUILayout.Label($"Triangles per Face: {trianglesPerFace}");
                GUILayout.Label($"Last Cut Triangles: {lastCutTriangleCount}");
                GUILayout.Label($"Last Cut Time: {lastCutTime:F1}ms");
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Test Cut (C)"))
                {
                    PerformTestCut();
                }
                
                if (GUILayout.Button("Cycle Subdivision (S)"))
                {
                    subdivisionLevel = (subdivisionLevel % 4) + 1; // Cycle between 1, 2, 3, 4
                    cutter.CutSurfaceSubdivisionLevel = subdivisionLevel;
                    int newN = Mathf.Max(0, subdivisionLevel - 1);
                    int triangleCount = 1 + 2 * newN;
                    Debug.Log($"Subdivision level changed to: {subdivisionLevel} (n={newN}, triangles per face: {triangleCount})");
                }
                
                if (GUILayout.Button("Test All Levels"))
                {
                    TestAllSubdivisionLevels();
                }
                
                GUILayout.EndArea();
            }
        }
    }
} 