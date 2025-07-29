using UnityEngine;
using System.Collections;
using System.Reflection;

namespace AutoLODRuntimeTest
{
    /// <summary>
    /// Runtime tester for AutoLOD's runtime decimation API
    /// Uses reflection to access AutoLOD classes from DLL
    /// </summary>
    public class AutoLODRuntimeTester : MonoBehaviour
    {
        [Header("Decimation Settings")]
        [Tooltip("Target triangle count (0 = auto-calculate)")]
        public int targetTriangleCount = 0;
        
        [Tooltip("Reduction rate (1.1 = 10% reduction, 2.0 = 50% reduction)")]
        [Range(1.1f, 6f)]
        public float reductionRate = 2f;
        
        [Tooltip("Use high quality decimation (slower but better results)")]
        public bool useHighQuality = false;
        
        [Tooltip("Use async decimation to avoid frame drops")]
        public bool useAsyncDecimation = true;
        
        [Header("Runtime Controls")]
        [Tooltip("Key to trigger fast decimation")]
        public KeyCode fastDecimateKey = KeyCode.F;
        
        [Tooltip("Key to trigger high quality decimation")]
        public KeyCode qualityDecimateKey = KeyCode.Q;
        
        [Tooltip("Key to reset to original mesh")]
        public KeyCode resetKey = KeyCode.R;
        
        [Header("Results")]
        [SerializeField] private int originalTriangleCount;
        [SerializeField] private int decimatedTriangleCount;
        [SerializeField] private float reductionPercentage;
        [SerializeField] private string lastDecimationType = "None";
        [SerializeField] private string autoLODStatus = "Unknown";
        
        private Mesh originalMesh;
        private MeshFilter meshFilter;
        private SkinnedMeshRenderer skinnedMeshRenderer;
        private bool isDecimating = false;
        
        // AutoLOD types accessed via reflection
        private System.Type fastDecimatorType;
        private System.Type qualityDecimatorType;
        private System.Type meshDecimatorInterfaceType;
        private bool autoLODAvailable = false;
        
        void Start()
        {
            // Get mesh components
            meshFilter = GetComponent<MeshFilter>();
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            
            // Store original mesh info
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                originalMesh = meshFilter.sharedMesh;
                originalTriangleCount = originalMesh.triangles.Length / 3;
            }
            else if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                originalMesh = skinnedMeshRenderer.sharedMesh;
                originalTriangleCount = originalMesh.triangles.Length / 3;
            }
            
            // Try to load AutoLOD types via reflection
            LoadAutoLODTypes();
            
            //Debug.Log($"AutoLOD Runtime Tester initialized on {gameObject.name} with {originalTriangleCount} triangles. AutoLOD Available: {autoLODAvailable}");
        }
        
        /// <summary>
        /// Load AutoLOD types via reflection
        /// </summary>
        private void LoadAutoLODTypes()
        {
            try
            {
                // Try to find AutoLOD assembly
                System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                System.Reflection.Assembly autoLODAssembly = null;
                
                foreach (var assembly in assemblies)
                {
                    if (assembly.FullName.Contains("MeshDecimator") || assembly.FullName.Contains("AutoLOD"))
                    {
                        autoLODAssembly = assembly;
                        break;
                    }
                }
                
                if (autoLODAssembly != null)
                {
                    // Try to get the types
                    fastDecimatorType = autoLODAssembly.GetType("AutoLOD.MeshDecimator.CFastMeshDecimator");
                    qualityDecimatorType = autoLODAssembly.GetType("AutoLOD.MeshDecimator.CQualityMeshDecimator");
                    meshDecimatorInterfaceType = autoLODAssembly.GetType("AutoLOD.MeshDecimator.IMeshDecimator");
                    
                    autoLODAvailable = (fastDecimatorType != null && qualityDecimatorType != null);
                    autoLODStatus = autoLODAvailable ? "Available" : "Types not found";
                }
                else
                {
                    autoLODStatus = "Assembly not found";
                }
            }
            catch (System.Exception e)
            {
                autoLODStatus = $"Error: {e.Message}";
                Debug.LogError($"Failed to load AutoLOD types: {e.Message}");
            }
        }
        
        void Update()
        {
            if (isDecimating) return; // Don't allow multiple decimations at once
            
            // Manual decimation triggers
            if (Input.GetKeyDown(fastDecimateKey))
            {
                useHighQuality = false;
                StartCoroutine(DecimateMeshAsync());
            }
            
            if (Input.GetKeyDown(qualityDecimateKey))
            {
                useHighQuality = true;
                StartCoroutine(DecimateMeshAsync());
            }
            
            if (Input.GetKeyDown(resetKey))
            {
                ResetToOriginalMesh();
            }
        }
        
        /// <summary>
        /// Decimate mesh using AutoLOD's runtime API via reflection
        /// </summary>
        public void DecimateMesh()
        {
            if (originalMesh == null)
            {
                Debug.LogError("No original mesh found for decimation!");
                return;
            }
            
            if (!autoLODAvailable)
            {
                Debug.LogError("AutoLOD is not available at runtime!");
                return;
            }
            
            // Calculate target triangle count if not specified
            if (targetTriangleCount <= 0)
            {
                targetTriangleCount = Mathf.RoundToInt(originalTriangleCount / reductionRate);
            }
            
            string decimationType = useHighQuality ? "High Quality" : "Fast";
            //Debug.Log($"Starting AutoLOD {decimationType} decimation: {originalTriangleCount} → {targetTriangleCount} triangles");
            
            try
            {
                // Create decimator instance via reflection
                System.Type decimatorType = useHighQuality ? qualityDecimatorType : fastDecimatorType;
                object decimator = System.Activator.CreateInstance(decimatorType);
                
                // Call Initialize method
                MethodInfo initializeMethod = decimatorType.GetMethod("Initialize");
                initializeMethod?.Invoke(decimator, null);
                
                // Call DecimateMesh method
                MethodInfo decimateMethod = decimatorType.GetMethod("DecimateMesh");
                object[] parameters = new object[] { originalMesh, targetTriangleCount, originalMesh.blendShapeCount > 0 };
                Mesh decimatedMesh = (Mesh)decimateMethod.Invoke(decimator, parameters);
                
                // Apply decimated mesh
                ApplyDecimatedMesh(decimatedMesh);
                
                // Update statistics
                decimatedTriangleCount = decimatedMesh.triangles.Length / 3;
                reductionPercentage = (float)(originalTriangleCount - decimatedTriangleCount) / originalTriangleCount * 100f;
                lastDecimationType = decimationType;
                
                //Debug.Log($"AutoLOD {decimationType} decimation complete: {originalTriangleCount} → {decimatedTriangleCount} triangles ({reductionPercentage:F1}% reduction)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AutoLOD decimation failed: {e.Message}");
                lastDecimationType = "Error";
            }
        }
        
        /// <summary>
        /// Async decimation to avoid frame drops
        /// </summary>
        public IEnumerator DecimateMeshAsync()
        {
            if (originalMesh == null)
            {
                Debug.LogError("No original mesh found for decimation!");
                yield break;
            }
            
            if (!autoLODAvailable)
            {
                Debug.LogError("AutoLOD is not available at runtime!");
                yield break;
            }
            
            isDecimating = true;
            
            // Calculate target triangle count if not specified
            if (targetTriangleCount <= 0)
            {
                targetTriangleCount = Mathf.RoundToInt(originalTriangleCount / reductionRate);
            }
            
            string decimationType = useHighQuality ? "High Quality" : "Fast";
            //Debug.Log($"Starting AutoLOD {decimationType} async decimation: {originalTriangleCount} → {targetTriangleCount} triangles");
            
            bool success = false;
            System.Exception error = null;
            
            // Create decimator instance via reflection
            System.Type decimatorType = useHighQuality ? qualityDecimatorType : fastDecimatorType;
            object decimator = null;
            
            try
            {
                decimator = System.Activator.CreateInstance(decimatorType);
                
                // Call Initialize method
                MethodInfo initializeMethod = decimatorType.GetMethod("Initialize");
                initializeMethod?.Invoke(decimator, null);
            }
            catch (System.Exception e)
            {
                error = e;
                Debug.LogError($"Failed to create AutoLOD decimator: {e.Message}");
            }
            
            if (decimator != null)
            {
                // Try to call DecimateMeshAsync method
                MethodInfo decimateAsyncMethod = decimatorType.GetMethod("DecimateMeshAsync");
                if (decimateAsyncMethod != null)
                {
                    object[] parameters = new object[] { originalMesh, targetTriangleCount, originalMesh.blendShapeCount > 0 };
                    object task = null;
                    
                    try
                    {
                        task = decimateAsyncMethod.Invoke(decimator, parameters);
                    }
                    catch (System.Exception e)
                    {
                        error = e;
                        Debug.LogError($"Failed to start AutoLOD async decimation: {e.Message}");
                    }
                    
                    if (task != null)
                    {
                        // Wait for completion outside try-catch
                        while (!(bool)task.GetType().GetProperty("IsCompleted").GetValue(task))
                        {
                            yield return null; // Wait one frame
                        }
                        
                        try
                        {
                            Mesh decimatedMesh = (Mesh)task.GetType().GetProperty("Result").GetValue(task);
                            
                            // Apply decimated mesh
                            ApplyDecimatedMesh(decimatedMesh);
                            
                            // Update statistics
                            decimatedTriangleCount = decimatedMesh.triangles.Length / 3;
                            reductionPercentage = (float)(originalTriangleCount - decimatedTriangleCount) / originalTriangleCount * 100f;
                            lastDecimationType = decimationType;
                            
                            //Debug.Log($"AutoLOD {decimationType} async decimation complete: {originalTriangleCount} → {decimatedTriangleCount} triangles ({reductionPercentage:F1}% reduction)");
                            success = true;
                        }
                        catch (System.Exception e)
                        {
                            error = e;
                            Debug.LogError($"AutoLOD async decimation failed: {e.Message}");
                        }
                    }
                }
                else
                {
                    // Fallback to sync decimation
                    try
                    {
                        DecimateMesh();
                        success = true;
                    }
                    catch (System.Exception e)
                    {
                        error = e;
                        Debug.LogError($"AutoLOD sync decimation failed: {e.Message}");
                    }
                }
            }
            
            if (!success)
            {
                lastDecimationType = "Error";
                if (error != null)
                {
                    Debug.LogError($"AutoLOD decimation failed: {error.Message}");
                }
            }
            
            isDecimating = false;
        }
        
        /// <summary>
        /// Apply the decimated mesh to the appropriate component
        /// </summary>
        private void ApplyDecimatedMesh(Mesh decimatedMesh)
        {
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = decimatedMesh;
            }
            else if (skinnedMeshRenderer != null)
            {
                // Preserve bone weights for skinned meshes
                decimatedMesh.bindposes = originalMesh.bindposes;
                skinnedMeshRenderer.sharedMesh = decimatedMesh;
            }
        }
        
        /// <summary>
        /// Reset to original mesh
        /// </summary>
        [ContextMenu("Reset to Original Mesh")]
        public void ResetToOriginalMesh()
        {
            if (originalMesh != null)
            {
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = originalMesh;
                }
                else if (skinnedMeshRenderer != null)
                {
                    skinnedMeshRenderer.sharedMesh = originalMesh;
                }
                
                decimatedTriangleCount = originalTriangleCount;
                reductionPercentage = 0f;
                lastDecimationType = "Reset";
                
                //Debug.Log("Reset to original mesh");
            }
        }
        
        /// <summary>
        /// Test decimation with custom settings
        /// </summary>
        public void TestDecimationWithSettings(int targetTriangles, bool highQuality = false, bool async = true)
        {
            targetTriangleCount = targetTriangles;
            useHighQuality = highQuality;
            useAsyncDecimation = async;
            
            if (async)
            {
                StartCoroutine(DecimateMeshAsync());
            }
            else
            {
                DecimateMesh();
            }
        }
        
        void OnGUI()
        {
            if (Application.isPlaying)
            {
                GUILayout.BeginArea(new Rect(10, 10, 350, 220));
                GUILayout.Label("AutoLOD Runtime Tester");
                GUILayout.Label($"Original: {originalTriangleCount} triangles");
                GUILayout.Label($"Current: {decimatedTriangleCount} triangles");
                GUILayout.Label($"Reduction: {reductionPercentage:F1}%");
                GUILayout.Label($"Last Type: {lastDecimationType}");
                GUILayout.Label($"Decimating: {isDecimating}");
                GUILayout.Label($"AutoLOD Status: {autoLODStatus}");
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Fast Decimation (F)"))
                {
                    useHighQuality = false;
                    if (useAsyncDecimation)
                        StartCoroutine(DecimateMeshAsync());
                    else
                        DecimateMesh();
                }
                
                if (GUILayout.Button("Quality Decimation (Q)"))
                {
                    useHighQuality = true;
                    if (useAsyncDecimation)
                        StartCoroutine(DecimateMeshAsync());
                    else
                        DecimateMesh();
                }
                
                if (GUILayout.Button("Reset to Original (R)"))
                {
                    ResetToOriginalMesh();
                }
                
                GUILayout.EndArea();
            }
        }
    }
} 