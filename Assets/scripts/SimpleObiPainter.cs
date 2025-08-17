/*using UnityEngine;
using System.Collections.Generic;
using System;

using Obi;
public class SimpleObiPainter : MonoBehaviour
{
    public ObiSoftbody targetSoftbody;
    public float brushRadius = 0.1f;
    public Color paintColor = Color.red;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            PaintAtMousePosition();
        }
    }
    
    void PaintAtMousePosition()
    {
        if (targetSoftbody == null)
        {
            Debug.LogError("SimpleObiPainter: No target softbody assigned!");
            return;
        }

        // Get mouse ray
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.Log($"SimpleObiPainter: Casting ray from {ray.origin} direction {ray.direction}");
        
        // Use Obi's spatial query system for softbodies
        ObiSolver solver = targetSoftbody.solver;
        if (solver == null)
        {
            Debug.LogError("SimpleObiPainter: Target softbody has no solver!");
            return;
        }

        // Enqueue a raycast query using Obi's system
        solver.EnqueueRaycast(ray, out int queryID, maxDistance: 100f);
        
        Debug.Log($"SimpleObiPainter: Enqueued Obi raycast with query ID: {queryID}");
        
        // The result will come in the next frame via OnSpatialQueryResults
        currentQueryID = queryID;
        pendingRay = ray;
    }

    private int currentQueryID = -1;
    private Ray pendingRay;

    void OnSpatialQueryResults(ObiNativeQueryResultList results)
    {
        Debug.Log($"SimpleObiPainter: Got {results.count} Obi raycast results");
        
        for (int i = 0; i < results.count; i++)
        {
            var result = results[i];
            if (result.queryID == currentQueryID)
            {
                Debug.Log($"SimpleObiPainter: Hit at distance {result.distance}");
                
                // Calculate hit point
                Vector3 hitPoint = pendingRay.origin + pendingRay.direction * result.distance;
                Debug.Log($"SimpleObiPainter: Hit point: {hitPoint}");
                
                PaintVertices(hitPoint);
                return;
            }
        }
        
        Debug.Log("SimpleObiPainter: No valid hits from Obi raycast");
    }
    
    void PaintVertices(Vector3 hitPoint)
    {
        // Get the mesh from the softbody
        Mesh mesh = targetSoftbody.GetComponent<MeshFilter>().mesh;
        
        // Paint vertices within radius
        Vector3[] vertices = mesh.vertices;
        Color[] colors = mesh.colors;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = targetSoftbody.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(worldPos, hitPoint);
            
            if (distance <= brushRadius)
            {
                colors[i] = paintColor;
            }
        }
        
        mesh.colors = colors;
        mesh.UploadMeshData(false);
    }
}
*/