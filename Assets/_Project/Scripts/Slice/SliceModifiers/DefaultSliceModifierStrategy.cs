using System.Collections;
using System.Collections.Generic;
using _Project;
using DynamicMeshCutter;
using Obi;
using Unity.VisualScripting;
using UnityEngine;

public class DefaultSliceModifierStrategy : ISliceModifierStrategy
{
    public IEnumerator Modify(MonoBehaviour coroutineHost, List<MeshTarget> objects, GameObject target)
    {
        Debug.Log("Modify: Start");
        foreach (var item in objects)
        {
            Debug.Log($"Processing object: {item.gameObject.name}");
            // Skip if parent exists and is not active in hierarchy (for consistency)
            if (item.transform.parent != null && !item.transform.parent.gameObject.activeInHierarchy)
            {
                Debug.Log($"Skipping {item.gameObject.name} because parent is inactive");
                continue;
            }

            item.transform.rotation = target.transform.rotation;
            Debug.Log($"Set rotation for {item.gameObject.name}");

            // Ensure a MeshCollider exists and is convex, but only if mesh is valid
            var meshFilter = item.GetComponent<MeshFilter>();
            Debug.Log($"Got MeshFilter for {item.gameObject.name}: {(meshFilter != null)}");
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            Debug.Log($"Mesh for {item.gameObject.name}: {(mesh != null)}");
            MeshCollider meshCollider = item.GetComponent<MeshCollider>();
            Debug.Log($"MeshCollider exists for {item.gameObject.name}: {(meshCollider != null)}");

            if (mesh != null)
            {
                if (meshCollider == null)
                {
                    meshCollider = item.gameObject.AddComponent<MeshCollider>();
                    Debug.Log($"Added MeshCollider to {item.gameObject.name}");
                }
                meshCollider.sharedMesh = mesh;
                Debug.Log($"Set sharedMesh for MeshCollider on {item.gameObject.name}");
                meshCollider.convex = true;
                Debug.Log($"Set MeshCollider.convex = true for {item.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"No valid mesh found for {item.gameObject.name}, skipping MeshCollider.");
            }

            // Add ObiCollider if not present
            var obiCollider = item.GetComponent<Obi.ObiCollider>();
            Debug.Log($"ObiCollider exists for {item.gameObject.name}: {(obiCollider != null)}");
            if (obiCollider == null)
            {
                obiCollider = item.gameObject.AddComponent<Obi.ObiCollider>();
                Debug.Log($"Added ObiCollider to {item.gameObject.name}");
            }

            // Add Rigidbody if not present
            var rigidbody = item.GetComponent<Rigidbody>();
            Debug.Log($"Rigidbody exists for {item.gameObject.name}: {(rigidbody != null)}");
            if (rigidbody == null)
            {
                rigidbody = item.gameObject.AddComponent<Rigidbody>();
                Debug.Log($"Added Rigidbody to {item.gameObject.name}");
            }
        }
        Debug.Log("Modify: End");
        yield return null;
    }
}