using UnityEngine;

public class SpinChild : MonoBehaviour
{
    public float spinSpeed = 100f;  // Rotation speed in degrees per second
    public Vector3 spinAxis = Vector3.up; // Default rotation around the Y axis
    private Transform childTransform;

    private void Start()
    {
        // Check if there's a child object
        if (transform.childCount > 0)
        {
            childTransform = transform.GetChild(0); // Get the first child
        }
        else
        {
            Debug.LogWarning("No child object found for " + gameObject.name);
        }
    }

    private void Update()
    {
        if (childTransform != null)
        {
            // Rotate the child object around its local axis
            childTransform.Rotate(spinAxis, spinSpeed * Time.deltaTime);
        }
    }
}
