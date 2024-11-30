using UnityEngine;
using System.Collections;
public class SpinObject : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up; // Default axis is the up axis
    public float rotationSpeed = 30f; // Degrees per second

    private void Start()
    {
        StartCoroutine(SpinCoroutine());
    }

    IEnumerator SpinCoroutine()
    {
        while (true)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }
    }
}
