using UnityEngine;
using Obi;

/// <summary>
/// Allows flipping a softbody object upside down with a button click
/// </summary>
[RequireComponent(typeof(ObiSoftbody))]
public class SoftbodyFlipper : MonoBehaviour
{
    [Header("Flip Controls")]
    [Tooltip("Click this button to flip the softbody upside down")]
    [SerializeField] private bool flipButton = false;
    
    [Tooltip("The rotation to apply when flipping (180 degrees around X-axis)")]
    [SerializeField] private Vector3 flipRotation = new Vector3(180f, 0f, 0f);
    
    [Tooltip("Smooth flip duration (0 = instant flip)")]
    [Range(0f, 2f)]
    [SerializeField] private float flipDuration = 0.5f;
    
    [Header("Debug Info")]
    [SerializeField] private bool isFlipped = false;
    [SerializeField] private Vector3 originalRotation;
    
    private ObiSoftbody softbody;
    private bool isFlipping = false;
    private float flipStartTime;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    
    void Start()
    {
        softbody = GetComponent<ObiSoftbody>();
        originalRotation = transform.rotation.eulerAngles;
        
        if (softbody == null)
        {
            Debug.LogError($"[SoftbodyFlipper] No ObiSoftbody found on {gameObject.name}!");
        }
    }
    
    void Update()
    {
        // Check for button press
        if (flipButton && !isFlipping)
        {
            FlipSoftbody();
            flipButton = false; // Reset the button
        }
        
        // Handle smooth flipping
        if (isFlipping)
        {
            float elapsed = Time.time - flipStartTime;
            float progress = elapsed / flipDuration;
            
            if (progress >= 1f)
            {
                // Flip complete
                transform.rotation = targetRotation;
                isFlipping = false;
                isFlipped = !isFlipped;
                Debug.Log($"[SoftbodyFlipper] {gameObject.name} flip complete!");
            }
            else
            {
                // Smooth interpolation
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            }
        }
    }
    
    /// <summary>
    /// Flip the softbody upside down
    /// </summary>
    [ContextMenu("Flip Softbody")]
    public void FlipSoftbody()
    {
        if (softbody == null)
        {
            Debug.LogError($"[SoftbodyFlipper] Cannot flip {gameObject.name} - no ObiSoftbody component!");
            return;
        }
        
        if (isFlipping)
        {
            Debug.LogWarning($"[SoftbodyFlipper] {gameObject.name} is already flipping!");
            return;
        }
        
        // Calculate target rotation
        Quaternion currentRotation = transform.rotation;
        Quaternion flipQuaternion = Quaternion.Euler(flipRotation);
        
        if (isFlipped)
        {
            // Flip back to original
            targetRotation = Quaternion.Euler(originalRotation);
            Debug.Log($"[SoftbodyFlipper] Flipping {gameObject.name} back to original rotation");
        }
        else
        {
            // Flip upside down
            targetRotation = currentRotation * flipQuaternion;
            Debug.Log($"[SoftbodyFlipper] Flipping {gameObject.name} upside down");
        }
        
        // Start smooth flip
        if (flipDuration > 0f)
        {
            startRotation = currentRotation;
            flipStartTime = Time.time;
            isFlipping = true;
        }
        else
        {
            // Instant flip
            transform.rotation = targetRotation;
            isFlipped = !isFlipped;
            Debug.Log($"[SoftbodyFlipper] {gameObject.name} flipped instantly!");
        }
    }
    
    /// <summary>
    /// Reset the softbody to its original rotation
    /// </summary>
    [ContextMenu("Reset Rotation")]
    public void ResetRotation()
    {
        if (isFlipping)
        {
            Debug.LogWarning($"[SoftbodyFlipper] Cannot reset while flipping!");
            return;
        }
        
        transform.rotation = Quaternion.Euler(originalRotation);
        isFlipped = false;
        Debug.Log($"[SoftbodyFlipper] {gameObject.name} rotation reset to original");
    }
    
    /// <summary>
    /// Flip instantly without smooth animation
    /// </summary>
    [ContextMenu("Flip Instantly")]
    public void FlipInstantly()
    {
        float originalDuration = flipDuration;
        flipDuration = 0f;
        FlipSoftbody();
        flipDuration = originalDuration;
    }
    
    void OnValidate()
    {
        // Auto-trigger flip when button is checked in inspector
        if (flipButton && Application.isPlaying)
        {
            // This will be handled in Update()
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw a visual indicator of the flip direction
        Gizmos.color = isFlipped ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // Draw rotation arrows
        Gizmos.color = Color.yellow;
        Vector3 up = transform.up;
        Gizmos.DrawRay(transform.position, up * 1f);
        
        // Draw target rotation preview
        if (!isFlipped)
        {
            Gizmos.color = Color.red;
            Vector3 flippedUp = (transform.rotation * Quaternion.Euler(flipRotation)) * Vector3.up;
            Gizmos.DrawRay(transform.position, flippedUp * 1f);
        }
    }
} 