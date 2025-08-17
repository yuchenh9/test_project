using UnityEngine;

/// <summary>
/// Attach this to the object you want to paint.
/// A separate GameObject (raycaster) will cast a ray towards this object.
/// When the inspector toggle is flipped, this script paints this object's mesh
/// around the hit point using a Polybrush-like vertex color blend.
/// </summary>
public class TargetVertexPainter : MonoBehaviour
{
    [Header("Ray Source")]
    [Tooltip("External object that will cast a ray toward this object.")]
    public GameObject raycaster;
    [Tooltip("Maximum raycast distance")] public float maxDistance = 10f;
    [Tooltip("Layer mask used for raycast")] public LayerMask raycastMask = ~0;

    [Header("Brush Settings")]
    [Tooltip("Brush radius in world units")]
    public float radius = 0.1f;
    [Tooltip("Blend strength per paint action (0..1)")]
    [Range(0f, 1f)] public float strength = 0.5f;
    [Tooltip("Target color to paint towards")] 
    public Color brushColor = Color.red;

    [Header("Trigger")]
    [Tooltip("Flip this toggle to perform one paint action.")]
    public bool paintNow;
    private bool _lastPaintNow;

    [Header("Markers & Debug")] 
    public bool createMarkerOnPaint = true;
    public float markerScale = 0.05f;
    public float markerLifetime = 1.0f;
    public bool enableDebugLogs = true;

    private void Awake()
    {
        _lastPaintNow = paintNow;
    }

    private void Update()
    {
        if (paintNow != _lastPaintNow)
        {
            _lastPaintNow = paintNow;
            TryPaintOnce();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (paintNow != _lastPaintNow)
            {
                _lastPaintNow = paintNow;
                TryPaintOnce();
            }
        }
    }

    private void TryPaintOnce()
    {
        if (raycaster == null)
        {
            if (enableDebugLogs) Debug.LogWarning("TargetVertexPainter: Raycaster is not assigned.");
            return;
        }

        if (TryRaycastAtSelf(out var hit))
        {
            if (enableDebugLogs) Debug.Log($"TargetVertexPainter: Ray hit {name} at {hit.point}");
            PaintAtHit(hit);
            if (createMarkerOnPaint)
                CreateMarker(hit.point);
        }
        else if (enableDebugLogs)
        {
            Debug.Log("TargetVertexPainter: Raycast did not hit this object.");
        }
    }

    private bool TryRaycastAtSelf(out RaycastHit hit)
    {
        var origin = raycaster.transform.position;
        var myCollider = GetComponent<Collider>();
        Vector3 targetPoint = myCollider != null ? myCollider.ClosestPoint(origin) : transform.position;
        var dir = (targetPoint - origin).normalized;

        if (enableDebugLogs)
            Debug.Log($"TargetVertexPainter: Casting from {origin} toward {targetPoint} dir={dir}");

        if (Physics.Raycast(origin, dir, out hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            // accept hits only on this object or its children
            if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
                return true;
        }
        return false;
    }

    private void PaintAtHit(RaycastHit hit)
    {
        // Fetch mesh from this object
        var meshFilter = GetComponent<MeshFilter>();
        var meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null && meshCollider == null)
        {
            if (enableDebugLogs) Debug.LogWarning("TargetVertexPainter: No MeshFilter or MeshCollider on target.");
            return;
        }

        Mesh mesh = null;
        Transform meshTransform = transform;

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            mesh = meshFilter.mesh; // runtime instance
        }
        else if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            mesh = meshCollider.sharedMesh; // note: edits asset; prefer MeshFilter in production
        }

        if (mesh == null)
        {
            if (enableDebugLogs) Debug.LogWarning("TargetVertexPainter: Mesh is null.");
            return;
        }

        var vertices = mesh.vertices;
        if (vertices == null || vertices.Length == 0) return;

        var colors = mesh.colors;
        if (colors == null || colors.Length != vertices.Length)
        {
            colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        }

        Vector3 center = hit.point;
        float r = Mathf.Max(0.0001f, radius);
        int affected = 0;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vWorld = meshTransform.TransformPoint(vertices[i]);
            float d = Vector3.Distance(vWorld, center);
            if (d > r) continue;

            float t = 1f - Mathf.Clamp01(d / r);
            float falloff = t * t * (3f - 2f * t);
            float w = strength * falloff;
            colors[i] = Color.Lerp(colors[i], brushColor, w);
            affected++;
        }

        mesh.colors = colors;
        if (meshCollider != null) meshCollider.sharedMesh = mesh;

        if (enableDebugLogs) Debug.Log($"TargetVertexPainter: Painted {affected} vertices.");
    }

    private void CreateMarker(Vector3 position)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "TargetVertexPainter_HitMarker";
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * Mathf.Max(0.0001f, markerScale);
        var col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);
        var rend = marker.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(rend.sharedMaterial);
            rend.material.color = brushColor;
        }
        if (markerLifetime > 0f) Destroy(marker, markerLifetime);
    }
}



