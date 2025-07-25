using UnityEngine;

[System.Serializable]
public class GradientColorStop
{
    [Range(0f, 1f)]
    public float position = 0f;
    public Color color = Color.white;
    
    public GradientColorStop(float pos, Color col)
    {
        position = pos;
        color = col;
    }
}

/// <summary>
/// Generates gradient textures for use with vertex color red-channel sampling
/// Perfect companion for the modified vertex color shader
/// </summary>
public class GradientTextureGenerator : MonoBehaviour
{
    [Header("Gradient Settings")]
    public GradientColorStop[] colorStops = new GradientColorStop[]
    {
        new GradientColorStop(0f, Color.black),
        new GradientColorStop(0.5f, Color.gray),
        new GradientColorStop(1f, Color.white)
    };
    
    [Header("Texture Settings")]
    [Range(16, 1024)]
    public int textureWidth = 256;
    [Range(1, 16)]
    public int textureHeight = 1;
    public FilterMode filterMode = FilterMode.Bilinear;
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    
    [Header("Output")]
    public string textureName = "GeneratedGradient";
    public bool saveAsAsset = true;
    
    [Header("Preview")]
    public Renderer previewRenderer;
    public string shaderPropertyName = "_GradientRamp";
    
    private Texture2D generatedTexture;
    
    [ContextMenu("Generate Gradient Texture")]
    public void GenerateGradientTexture()
    {
        // Sort color stops by position
        System.Array.Sort(colorStops, (a, b) => a.position.CompareTo(b.position));
        
        // Create texture
        generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        generatedTexture.name = textureName;
        generatedTexture.filterMode = filterMode;
        generatedTexture.wrapMode = wrapMode;
        
        // Generate gradient
        Color[] pixels = new Color[textureWidth * textureHeight];
        
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float t = (float)x / (textureWidth - 1); // 0 to 1
                Color color = SampleGradient(t);
                pixels[y * textureWidth + x] = color;
            }
        }
        
        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply();
        
        Debug.Log($"Generated gradient texture: {textureWidth}x{textureHeight}");
        
        // Apply to preview renderer if available
        if (previewRenderer != null)
        {
            ApplyToRenderer();
        }
        
        // Save as asset if requested
        if (saveAsAsset)
        {
            SaveAsAsset();
        }
    }
    
    Color SampleGradient(float t)
    {
        t = Mathf.Clamp01(t);
        
        // Handle edge cases
        if (colorStops.Length == 0) return Color.white;
        if (colorStops.Length == 1) return colorStops[0].color;
        if (t <= colorStops[0].position) return colorStops[0].color;
        if (t >= colorStops[colorStops.Length - 1].position) return colorStops[colorStops.Length - 1].color;
        
        // Find the two color stops to interpolate between
        for (int i = 0; i < colorStops.Length - 1; i++)
        {
            GradientColorStop current = colorStops[i];
            GradientColorStop next = colorStops[i + 1];
            
            if (t >= current.position && t <= next.position)
            {
                float localT = (t - current.position) / (next.position - current.position);
                return Color.Lerp(current.color, next.color, localT);
            }
        }
        
        return Color.white; // Fallback
    }
    
    void ApplyToRenderer()
    {
        if (previewRenderer == null || generatedTexture == null) return;
        
        Material material = previewRenderer.material;
        if (material != null)
        {
            material.SetTexture(shaderPropertyName, generatedTexture);
            Debug.Log($"Applied gradient texture to {previewRenderer.name}");
        }
    }
    
    void SaveAsAsset()
    {
        if (generatedTexture == null) return;
        
        #if UNITY_EDITOR
        string path = $"Assets/_Project/Textures/{textureName}.png";
        
        // Create directory if it doesn't exist
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // Convert to PNG and save
        byte[] pngData = generatedTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);
        
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"Saved gradient texture to: {path}");
        
        // Configure import settings
        UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null)
        {
            importer.textureType = UnityEditor.TextureImporterType.Default;
            importer.filterMode = filterMode;
            importer.wrapMode = wrapMode;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        #endif
    }
    
    [ContextMenu("Create Preset: Fire")]
    public void CreateFirePreset()
    {
        colorStops = new GradientColorStop[]
        {
            new GradientColorStop(0f, Color.black),
            new GradientColorStop(0.3f, new Color(0.5f, 0f, 0f)), // Dark red
            new GradientColorStop(0.6f, new Color(1f, 0.3f, 0f)), // Red-orange
            new GradientColorStop(0.85f, new Color(1f, 0.8f, 0f)), // Yellow-orange
            new GradientColorStop(1f, Color.yellow)
        };
        GenerateGradientTexture();
    }
    
    [ContextMenu("Create Preset: Ocean")]
    public void CreateOceanPreset()
    {
        colorStops = new GradientColorStop[]
        {
            new GradientColorStop(0f, new Color(0.05f, 0.1f, 0.2f)), // Deep blue
            new GradientColorStop(0.4f, new Color(0.1f, 0.3f, 0.6f)), // Ocean blue
            new GradientColorStop(0.7f, new Color(0.2f, 0.6f, 0.8f)), // Light blue
            new GradientColorStop(0.9f, new Color(0.6f, 0.9f, 0.95f)), // Cyan
            new GradientColorStop(1f, Color.white) // Foam
        };
        GenerateGradientTexture();
    }
    
    [ContextMenu("Create Preset: Forest")]
    public void CreateForestPreset()
    {
        colorStops = new GradientColorStop[]
        {
            new GradientColorStop(0f, new Color(0.1f, 0.05f, 0f)), // Dark brown
            new GradientColorStop(0.3f, new Color(0.3f, 0.2f, 0.1f)), // Brown
            new GradientColorStop(0.6f, new Color(0.2f, 0.4f, 0.1f)), // Dark green
            new GradientColorStop(0.85f, new Color(0.4f, 0.7f, 0.2f)), // Green
            new GradientColorStop(1f, new Color(0.6f, 0.9f, 0.4f)) // Light green
        };
        GenerateGradientTexture();
    }
    
    [ContextMenu("Create Preset: Sunset")]
    public void CreateSunsetPreset()
    {
        colorStops = new GradientColorStop[]
        {
            new GradientColorStop(0f, new Color(0.1f, 0.05f, 0.2f)), // Purple
            new GradientColorStop(0.25f, new Color(0.4f, 0.1f, 0.3f)), // Dark magenta
            new GradientColorStop(0.5f, new Color(0.8f, 0.2f, 0.1f)), // Red
            new GradientColorStop(0.75f, new Color(1f, 0.5f, 0.1f)), // Orange
            new GradientColorStop(1f, new Color(1f, 0.9f, 0.3f)) // Yellow
        };
        GenerateGradientTexture();
    }
    
    void OnDrawGizmosSelected()
    {
        if (generatedTexture == null) return;
        
        // Draw a preview of the gradient in the scene view
        Vector3 pos = transform.position;
        Vector3 size = new Vector3(2f, 0.1f, 0.1f);
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(pos, size);
        
        // Draw gradient samples
        int samples = 20;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / (samples - 1);
            Color color = SampleGradient(t);
            Gizmos.color = color;
            
            Vector3 samplePos = pos + Vector3.left * (size.x * 0.5f) + Vector3.right * (t * size.x);
            Gizmos.DrawCube(samplePos, new Vector3(size.x / samples, size.y * 2f, size.z * 2f));
        }
    }
} 