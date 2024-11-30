using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicMeshCutter
{
    public class AccurateLocalBounds : MonoBehaviour
    {
        void Start()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Vector3[] vertices = meshFilter.mesh.vertices;

                // Transform vertices to world space
                Bounds bounds = new Bounds(vertices[0], Vector3.zero);
                for (int i = 1; i < vertices.Length; i++)
                {
                    Vector3 worldVertex = transform.TransformPoint(vertices[i]);
                    bounds.Encapsulate(worldVertex);
                }

                // Now convert the center back to local space
                Bounds localBounds = new Bounds(transform.InverseTransformPoint(bounds.center), bounds.size);

                Debug.Log("Accurate Local Center: " + localBounds.center);
                Debug.Log("Accurate Local Size: " + localBounds.size);
            }
        }
    }
}
