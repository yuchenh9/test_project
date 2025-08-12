using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DynamicMeshCutter
{
    /// <summary>
    /// Static utility class for assigning vertex colors to cap faces created during mesh cutting.
    /// Assigns R values ranging from 0 (center) to 1 (outermost ring).
    /// </summary>
    public static class CapVertexColorAssigner
    {
        /// <summary>
        /// Assigns R values to cap face vertices based on their distance from center to boundary.
        /// Center vertices get R=0, boundary vertices get R=1, with smooth interpolation in between.
        /// </summary>
        /// <param name="vertices">List of vertices in the cap face</param>
        /// <param name="center">Center point of the cap face</param>
        /// <param name="boundaryVertices">List of boundary vertices (outermost ring)</param>
        /// <returns>Array of colors with R values assigned based on distance</returns>
        public static Color[] AssignCapVertexColors(List<Vector3> vertices, Vector3 center, List<Vector3> boundaryVertices)
        {
            if (vertices == null || vertices.Count == 0)
                return new Color[0];

            Color[] colors = new Color[vertices.Count];
            
            // Calculate the maximum distance from center to any boundary vertex
            float maxDistance = 0f;
            foreach (Vector3 boundaryVertex in boundaryVertices)
            {
                float distance = Vector3.Distance(center, boundaryVertex);
                if (distance > maxDistance)
                    maxDistance = distance;
            }

            // If no boundary vertices or max distance is zero, assign uniform colors
            if (maxDistance <= 0f)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color(0.5f, 0f, 0f, 1f); // Default gray-red
                }
                return colors;
            }

            // Assign R values based on distance from center
            for (int i = 0; i < vertices.Count; i++)
            {
                float distance = Vector3.Distance(center, vertices[i]);
                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
                
                // R value: 0 at center, 1 at boundary
                float rValue = normalizedDistance;
                
                colors[i] = new Color(rValue, 0f, 0f, 1f);
            }

            return colors;
        }

        /// <summary>
        /// Extracts boundary vertices from a list of vertices by finding the outermost ring.
        /// Uses convex hull approximation to identify boundary vertices.
        /// </summary>
        /// <param name="vertices">All vertices in the cap face</param>
        /// <param name="center">Center point of the cap face</param>
        /// <returns>List of boundary vertices</returns>
        public static List<Vector3> ExtractBoundaryVertices(List<Vector3> vertices, Vector3 center)
        {
            if (vertices == null || vertices.Count < 3)
                return new List<Vector3>(vertices);

            // Project vertices onto a 2D plane for boundary detection
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.right;
            Vector3 forward = Vector3.forward;

            // Find the best plane to project onto (avoid degenerate cases)
            Vector3 avgNormal = Vector3.zero;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 toVertex = (vertices[i] - center).normalized;
                avgNormal += toVertex;
            }
            avgNormal.Normalize();

            // Create a coordinate system for projection
            Vector3 axis1 = Vector3.Cross(avgNormal, Vector3.up);
            if (axis1.magnitude < 0.1f)
                axis1 = Vector3.Cross(avgNormal, Vector3.right);
            axis1.Normalize();
            
            Vector3 axis2 = Vector3.Cross(avgNormal, axis1);
            axis2.Normalize();

            // Project vertices to 2D
            List<Vector2> projectedPoints = new List<Vector2>();
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 relativePos = vertices[i] - center;
                float x = Vector3.Dot(relativePos, axis1);
                float y = Vector3.Dot(relativePos, axis2);
                projectedPoints.Add(new Vector2(x, y));
            }

            // Find boundary vertices using convex hull approximation
            List<int> boundaryIndices = ComputeConvexHull(projectedPoints);
            
            List<Vector3> boundaryVertices = new List<Vector3>();
            foreach (int index in boundaryIndices)
            {
                boundaryVertices.Add(vertices[index]);
            }

            return boundaryVertices;
        }

        /// <summary>
        /// Computes convex hull of 2D points using Graham scan algorithm.
        /// </summary>
        /// <param name="points">List of 2D points</param>
        /// <returns>Indices of boundary points in counter-clockwise order</returns>
        private static List<int> ComputeConvexHull(List<Vector2> points)
        {
            if (points.Count < 3)
            {
                List<int> result = new List<int>();
                for (int i = 0; i < points.Count; i++)
                    result.Add(i);
                return result;
            }

            // Find the point with lowest y-coordinate (and leftmost if tied)
            int lowestIndex = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].y < points[lowestIndex].y || 
                    (points[i].y == points[lowestIndex].y && points[i].x < points[lowestIndex].x))
                {
                    lowestIndex = i;
                }
            }

            // Sort points by polar angle with respect to the lowest point
            List<int> sortedIndices = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                if (i != lowestIndex)
                    sortedIndices.Add(i);
            }

            sortedIndices.Sort((a, b) => {
                Vector2 va = points[a] - points[lowestIndex];
                Vector2 vb = points[b] - points[lowestIndex];
                float angleA = Mathf.Atan2(va.y, va.x);
                float angleB = Mathf.Atan2(vb.y, vb.x);
                return angleA.CompareTo(angleB);
            });

            // Graham scan
            List<int> hull = new List<int>();
            hull.Add(lowestIndex);
            hull.Add(sortedIndices[0]);

            for (int i = 1; i < sortedIndices.Count; i++)
            {
                while (hull.Count > 1 && !IsLeftTurn(
                    points[hull[hull.Count - 2]], 
                    points[hull[hull.Count - 1]], 
                    points[sortedIndices[i]]))
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(sortedIndices[i]);
            }

            return hull;
        }

        /// <summary>
        /// Determines if three points make a left turn.
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        /// <param name="p3">Third point</param>
        /// <returns>True if the turn is left</returns>
        private static bool IsLeftTurn(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return ((p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x)) > 0;
        }

        /// <summary>
        /// Assigns R values to cap face vertices using a simpler distance-based approach.
        /// This is a fallback method that doesn't require boundary detection.
        /// </summary>
        /// <param name="vertices">List of vertices in the cap face</param>
        /// <param name="center">Center point of the cap face</param>
        /// <returns>Array of colors with R values assigned based on distance from center</returns>
        public static Color[] AssignCapVertexColorsSimple(List<Vector3> vertices, Vector3 center)
        {
            if (vertices == null || vertices.Count == 0)
                return new Color[0];

            Color[] colors = new Color[vertices.Count];
            
            // Find the maximum distance from center
            float maxDistance = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                float distance = Vector3.Distance(center, vertices[i]);
                if (distance > maxDistance)
                    maxDistance = distance;
            }

            // Assign R values based on distance from center
            for (int i = 0; i < vertices.Count; i++)
            {
                float distance = Vector3.Distance(center, vertices[i]);
                float normalizedDistance = maxDistance > 0f ? Mathf.Clamp01(distance / maxDistance) : 0f;
                
                // R value: 0 at center, 1 at maximum distance
                float rValue = normalizedDistance;
                
                colors[i] = new Color(rValue, 0f, 0f, 1f);
            }

            return colors;
        }
    }
}

