using UnityEngine;

public static class DebugPlaneDrawer
{
    private static Material lineMaterial;
    
    private static void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    public static void DrawPlane(Vector3 position, Vector3 normal, float size = 1f)
{
    // Create a thin cube (acts as a plane)
    GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
    plane.name = "CutPlane";
    plane.transform.position = position;
    plane.transform.rotation = Quaternion.LookRotation(normal);
    plane.transform.localScale = new Vector3(size, size, 0.001f); // Thin Z-scale

    // Make it semi-transparent red
    Material material = new Material(Shader.Find("Standard"));
    material.color = new Color(1, 0, 0, 0.5f);
    plane.GetComponent<Renderer>().material = material;

}
}