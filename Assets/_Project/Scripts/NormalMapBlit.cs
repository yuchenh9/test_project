using UnityEngine;

public class NormalMapBlit : MonoBehaviour
{
    [Header("Textures")]
    public Texture2D normalMap;
    public Vector3 direction=new Vector3(1,0,0);
    
    [Header("Output")]
    public RenderTexture outputTexture;
    public RenderTexture feedbackTexture;
    public bool performBlit;
    
    [Header("Settings")]
    [Range(0f, 2f)]
    public float normalStrength = 1f;
    [Range(0f, 1f)]
    public float blendFactor = 0.5f;
    
    // Shader for combining image and normal map
    private Material blitMaterial;
    private Shader blitShader;
    
    void Start()
    {
            
            // Initialize feedback texture (optional - clear to black)
            Graphics.SetRenderTarget(feedbackTexture);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(null);
        // Create the shader code as a string
        blitShader = Shader.Find("Custom/ImageNormalBlit");
        if (blitShader == null)
        {
            Debug.LogError("Shader not found! Make sure to create the shader file in your project.");
            return;
        }
        
        // Create material
        blitMaterial = new Material(blitShader);
        
        // Set shader properties
        // Create output render texture if not assigned
        if (outputTexture == null)
        {
            outputTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
        }
    }
    
    void OnValidate()
    {
        // Check if the toggle button was pressed
        if (performBlit)
        {
            performBlit = false; // Reset the toggle
            
            // Only perform blit in play mode and if application is playing
            
            PerformBlit();
            Debug.Log("Blit operation performed!");
            
            if (!Application.isPlaying)
            {
                Debug.Log("Blit will be performed when entering Play Mode");
            }
            else
            {
                Debug.LogWarning("Cannot perform blit: Missing textures or material!");
            }
        }
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
    }
    
    public void PerformBlit()
    {
        if (blitMaterial == null)
        {
            Debug.LogError("Blit material is null!");
            return;
        }
        
        blitMaterial.SetTexture("_MainTex", feedbackTexture);
        blitMaterial.SetTexture("_NormalMap", normalMap);
        blitMaterial.SetFloat("_NormalStrength", normalStrength);
        blitMaterial.SetFloat("_BlendFactor", blendFactor);
        blitMaterial.SetVector("_direction", direction);
        // Perform the blit operation
        Graphics.Blit(feedbackTexture, outputTexture, blitMaterial);
        Graphics.CopyTexture(outputTexture, feedbackTexture);
    }
    
    // Manual blit trigger for testing
    [ContextMenu("Perform Blit")]
    public void ManualBlit()
    {
        PerformBlit();
    }
    
    void OnDestroy()
    {
        // Clean up
        if (blitMaterial != null)
        {
            DestroyImmediate(blitMaterial);
        }
        
        if (outputTexture != null)
        {
            outputTexture.Release();
        }
    }
}

// Alternative simpler version that just combines textures
[System.Serializable]
public class SimpleTextureBlit : MonoBehaviour
{
    public Texture2D sourceTexture;
    public Texture2D normalTexture;
    public RenderTexture targetTexture;
    
    // Simple blit that uses built-in blend modes
    public void SimpleBlit()
    {
        if (sourceTexture == null || normalTexture == null || targetTexture == null)
            return;
            
        // First blit the main texture
        Graphics.Blit(sourceTexture, targetTexture);
        
        // Then blit the normal map with a blend mode
        // Note: This requires a material with appropriate blend mode
        // For demonstration, this shows the concept
        Material blendMat = new Material(Shader.Find("UI/Default"));
        Graphics.Blit(normalTexture, targetTexture, blendMat);
    }
}