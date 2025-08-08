using UnityEngine;

/// <summary>
/// Controls object rotation and position using keyboard input
/// Rotation: Q/W/E (X/Y/Z) and A/S/D (negative X/Y/Z)
/// Position: Arrow keys (X and Y only)
/// </summary>
public class KeyboardObjectController : MonoBehaviour
{
    [Header("Rotation Controls")]
    [Tooltip("Rotation speed in degrees per second")]
    [SerializeField] private float rotationSpeed = 90f;
    
    [Tooltip("Enable rotation controls")]
    [SerializeField] private bool enableRotation = true;
    
    [Header("Position Controls")]
    [Tooltip("Movement speed in units per second")]
    [SerializeField] private float movementSpeed = 5f;
    
    [Tooltip("Enable position controls")]
    [SerializeField] private bool enablePosition = true;
    
    [Header("Input Keys")]
    [Tooltip("Rotation keys for X, Y, Z axes")]
    [SerializeField] private KeyCode[] rotationKeys = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Q, KeyCode.E };
    
    [Tooltip("Movement keys for X and Y")]
    [SerializeField] private KeyCode[] movementKeys = { KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.UpArrow };
    
    [Header("Debug Info")]
    [SerializeField] private Vector3 currentRotation;
    [SerializeField] private Vector3 currentPosition;
    [SerializeField] private string lastInput = "None";
    
    private Vector3 originalPosition;
    private Vector3 originalRotation;
    
    void Start()
    {
        // Store original transform values
        originalPosition = transform.position;
        originalRotation = transform.rotation.eulerAngles;
        
        // Initialize debug info
        UpdateDebugInfo();
        
        Debug.Log($"[KeyboardObjectController] {gameObject.name} controller initialized");
        Debug.Log("Rotation: W/S (tilt forward/backward), A/D (tilt left/right), Q/E (rotate horizontally)");
        Debug.Log("Position: Arrow keys (X and Y only)");
    }
    
    void Update()
    {
        HandleRotation();
        HandlePosition();
        UpdateDebugInfo();
    }
    
    /// <summary>
    /// Handle rotation input using intuitive keys for facing negative Z
    /// W/S: Tilt forward/backward (X-axis rotation)
    /// A/D: Tilt left/right (Z-axis rotation) 
    /// Q/E: Rotate horizontally (Y-axis rotation)
    /// </summary>
    void HandleRotation()
    {
        if (!enableRotation) return;
        
        Vector3 rotationDelta = Vector3.zero;
        
        // W/S: Tilt forward/backward (X-axis rotation)
        if (Input.GetKey(rotationKeys[0])) // W - Tilt forward (positive X)
        {
            rotationDelta.x += rotationSpeed * Time.deltaTime;
            lastInput = "W (Tilt Forward)";
        }
        if (Input.GetKey(rotationKeys[1])) // S - Tilt backward (negative X)
        {
            rotationDelta.x -= rotationSpeed * Time.deltaTime;
            lastInput = "S (Tilt Backward)";
        }
        
        // A/D: Tilt left/right (Z-axis rotation)
        if (Input.GetKey(rotationKeys[2])) // A - Tilt left (positive Z)
        {
            rotationDelta.z += rotationSpeed * Time.deltaTime;
            lastInput = "A (Tilt Left)";
        }
        if (Input.GetKey(rotationKeys[3])) // D - Tilt right (negative Z)
        {
            rotationDelta.z -= rotationSpeed * Time.deltaTime;
            lastInput = "D (Tilt Right)";
        }
        
        // Q/E: Rotate horizontally (Y-axis rotation)
        if (Input.GetKey(rotationKeys[4])) // Q - Rotate counterclockwise (negative Y)
        {
            rotationDelta.y -= rotationSpeed * Time.deltaTime;
            lastInput = "Q (Rotate CCW)";
        }
        if (Input.GetKey(rotationKeys[5])) // E - Rotate clockwise (positive Y)
        {
            rotationDelta.y += rotationSpeed * Time.deltaTime;
            lastInput = "E (Rotate CW)";
        }
        
        // Apply rotation
        if (rotationDelta != Vector3.zero)
        {
            transform.Rotate(rotationDelta, Space.World);
        }
    }
    
    /// <summary>
    /// Handle position input using arrow keys (X and Y only)
    /// </summary>
    void HandlePosition()
    {
        if (!enablePosition) return;
        
        Vector3 positionDelta = Vector3.zero;
        
        // Horizontal movement (Left/Right arrows)
        if (Input.GetKey(movementKeys[0])) // Left Arrow - Negative X
        {
            positionDelta.x -= movementSpeed * Time.deltaTime*0.01f;
            lastInput = "Left Arrow (Move X-)";
        }
        if (Input.GetKey(movementKeys[1])) // Right Arrow - Positive X
        {
            positionDelta.x += movementSpeed * Time.deltaTime*0.01f;
            lastInput = "Right Arrow (Move X+)";
        }
        
        // Vertical movement (Down/Up arrows)
        if (Input.GetKey(movementKeys[2])) // Down Arrow - Negative Y
        {
            positionDelta.y -= movementSpeed * Time.deltaTime*0.01f;
            lastInput = "Down Arrow (Move Y-)";
        }
        if (Input.GetKey(movementKeys[3])) // Up Arrow - Positive Y
        {
            positionDelta.y += movementSpeed * Time.deltaTime*0.01f;
            lastInput = "Up Arrow (Move Y+)";
        }
        
        // Apply position change
        if (positionDelta != Vector3.zero)
        {
            transform.position += positionDelta;
        }
    }
    
    /// <summary>
    /// Update debug information
    /// </summary>
    void UpdateDebugInfo()
    {
        currentRotation = transform.rotation.eulerAngles;
        currentPosition = transform.position;
    }
    
    /// <summary>
    /// Reset object to original position and rotation
    /// </summary>
    [ContextMenu("Reset to Original")]
    public void ResetToOriginal()
    {
        transform.position = originalPosition;
        transform.rotation = Quaternion.Euler(originalRotation);
        lastInput = "Reset to Original";
        Debug.Log($"[KeyboardObjectController] {gameObject.name} reset to original transform");
    }
    
    /// <summary>
    /// Toggle rotation controls on/off
    /// </summary>
    [ContextMenu("Toggle Rotation")]
    public void ToggleRotation()
    {
        enableRotation = !enableRotation;
        Debug.Log($"[KeyboardObjectController] Rotation controls {(enableRotation ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Toggle position controls on/off
    /// </summary>
    [ContextMenu("Toggle Position")]
    public void TogglePosition()
    {
        enablePosition = !enablePosition;
        Debug.Log($"[KeyboardObjectController] Position controls {(enablePosition ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Increase rotation speed
    /// </summary>
    [ContextMenu("Increase Rotation Speed")]
    public void IncreaseRotationSpeed()
    {
        rotationSpeed += 30f;
        Debug.Log($"[KeyboardObjectController] Rotation speed increased to {rotationSpeed}°/s");
    }
    
    /// <summary>
    /// Decrease rotation speed
    /// </summary>
    [ContextMenu("Decrease Rotation Speed")]
    public void DecreaseRotationSpeed()
    {
        rotationSpeed = Mathf.Max(10f, rotationSpeed - 30f);
        Debug.Log($"[KeyboardObjectController] Rotation speed decreased to {rotationSpeed}°/s");
    }
    
    /// <summary>
    /// Increase movement speed
    /// </summary>
    [ContextMenu("Increase Movement Speed")]
    public void IncreaseMovementSpeed()
    {
        movementSpeed += 2f;
        Debug.Log($"[KeyboardObjectController] Movement speed increased to {movementSpeed} units/s");
    }
    
    /// <summary>
    /// Decrease movement speed
    /// </summary>
    [ContextMenu("Decrease Movement Speed")]
    public void DecreaseMovementSpeed()
    {
        movementSpeed = Mathf.Max(1f, movementSpeed - 2f);
        Debug.Log($"[KeyboardObjectController] Movement speed decreased to {movementSpeed} units/s");
    }
    
    void OnGUI()
    {
        // Display controls info
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== Keyboard Object Controller ===");
        GUILayout.Label($"Object: {gameObject.name}");
        GUILayout.Label($"Last Input: {lastInput}");
        GUILayout.Space(5);
        
        GUILayout.Label("Rotation Controls:");
        GUILayout.Label("  W/S - Tilt Forward/Backward");
        GUILayout.Label("  A/D - Tilt Left/Right");
        GUILayout.Label("  Q/E - Rotate Horizontally");
        GUILayout.Label($"  Speed: {rotationSpeed:F0}°/s");
        GUILayout.Label($"  Enabled: {enableRotation}");
        
        GUILayout.Space(5);
        
        GUILayout.Label("Position Controls:");
        GUILayout.Label("  ←/→ - Move X-/+");
        GUILayout.Label("  ↓/↑ - Move Y-/+");
        GUILayout.Label($"  Speed: {movementSpeed:F1} units/s");
        GUILayout.Label($"  Enabled: {enablePosition}");
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Reset to Original"))
        {
            ResetToOriginal();
        }
        
        if (GUILayout.Button("Toggle Rotation"))
        {
            ToggleRotation();
        }
        
        if (GUILayout.Button("Toggle Position"))
        {
            TogglePosition();
        }
        
        GUILayout.EndArea();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw movement bounds
        Gizmos.color = Color.green;
        Vector3 center = transform.position;
        float size = 2f;
        
        // Draw X movement line
        Gizmos.DrawLine(center + Vector3.left * size, center + Vector3.right * size);
        
        // Draw Y movement line
        Gizmos.DrawLine(center + Vector3.down * size, center + Vector3.up * size);
        
        // Draw rotation indicator
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        Gizmos.DrawRay(transform.position, transform.up * 1.5f);
        Gizmos.DrawRay(transform.position, transform.right * 1.5f);
    }
} 