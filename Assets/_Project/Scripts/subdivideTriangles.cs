using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public static class SubdivideTriangles
{       
    public class Triangle
    {
        public Vector3[] vertices = new Vector3[3];
        public BoneWeight[] boneWeights = new BoneWeight[3];
        public Vector2[] uvs = new Vector2[3];
        public Color[] colors = new Color[3];
    }
    
    public static List<Triangle> Subdivide(List<Triangle> triangles, int level, bool hasBoneWeights, System.Func<Vector3, Vector2> calculateUV)
    {
        List<Triangle> finalTriangles = new List<Triangle>();
        //Debug.Log($"[FillFace] Starting with {triangles.Count} original triangles");
        foreach (Triangle tri in triangles)
        {
            var subdivided = SubdivideTriangle1Plus2N(tri, level, hasBoneWeights, calculateUV);
            finalTriangles.AddRange(subdivided);
        }
        //Debug.Log($"[FillFace] Final triangle count: {finalTriangles.Count}");
        return finalTriangles;
    }
    
    public static List<Triangle> SubdivideTriangle1Plus2N(Triangle original, int n, bool hasBoneWeights, System.Func<Vector3, Vector2> calculateUV)
    {
        List<Triangle> result = new List<Triangle>();
        //Debug.Log($"[SubdivideTriangle1Plus2N] n={n}, expected result count: {1 + 2 * n}");
        
        if (n == 0)
        {
            // No subdivision, return original triangle
            //Debug.Log($"[SubdivideTriangle1Plus2N] n=0, returning original triangle");
            result.Add(original);
            return result;
        }

        // Get original vertices
        Vector3 center = original.vertices[0]; // center
        Vector3 v1 = original.vertices[1]; // boundary vertex 1
        Vector3 v2 = original.vertices[2]; // boundary vertex 2

        //Debug.Log($"[SubdivideTriangle1Plus2N] Original triangle: Center({center.x:F2},{center.y:F2},{center.z:F2}) V1({v1.x:F2},{v1.y:F2},{v1.z:F2}) V2({v2.x:F2},{v2.y:F2},{v2.z:F2})");

        // Create subdivision points along the radial edges (center to boundary vertices)
        // Each radial edge is divided into (n+1) segments, creating n intermediate points
        List<Vector3> edge1Points = new List<Vector3>(); // Points along center->v1
        List<Vector3> edge2Points = new List<Vector3>(); // Points along center->v2
        List<BoneWeight> edge1BoneWeights = new List<BoneWeight>();
        List<BoneWeight> edge2BoneWeights = new List<BoneWeight>();
        List<Color> edge1Colors = new List<Color>();
        List<Color> edge2Colors = new List<Color>();

        // Add center point
        edge1Points.Add(center);
        edge2Points.Add(center);
        edge1BoneWeights.Add(original.boneWeights[0]);
        edge2BoneWeights.Add(original.boneWeights[0]);
        edge1Colors.Add(original.colors[0]);
        edge2Colors.Add(original.colors[0]);

        // Add n intermediate points along each radial edge
        for (int i = 1; i <= n; i++)
        {
            float t = (float)i / (n + 1); // t goes from 1/(n+1) to n/(n+1)
            
            // Points along center->v1
            Vector3 p1 = Vector3.Lerp(center, v1, t);
            edge1Points.Add(p1);
            edge1BoneWeights.Add(hasBoneWeights ? InterpolateBoneWeight(original.boneWeights[0], original.boneWeights[1], t) : new BoneWeight());
            edge1Colors.Add(Color.Lerp(original.colors[0], original.colors[1], t));

            // Points along center->v2
            Vector3 p2 = Vector3.Lerp(center, v2, t);
            edge2Points.Add(p2);
            edge2BoneWeights.Add(hasBoneWeights ? InterpolateBoneWeight(original.boneWeights[0], original.boneWeights[2], t) : new BoneWeight());
            edge2Colors.Add(Color.Lerp(original.colors[0], original.colors[2], t));
        }

        // Add boundary vertices
        edge1Points.Add(v1);
        edge2Points.Add(v2);
        edge1BoneWeights.Add(original.boneWeights[1]);
        edge2BoneWeights.Add(original.boneWeights[2]);
        edge1Colors.Add(original.colors[1]);
        edge2Colors.Add(original.colors[2]);

        //Debug.Log($"[SubdivideTriangle1Plus2N] Created {edge1Points.Count} points along each radial edge (center + {n} intermediate + 1 boundary)");

        // Now create triangles using the grid pattern
        // For n=1: 3 triangles (1 + 2*1)
        // For n=2: 5 triangles (1 + 2*2)
        
        for (int i = 0; i < n + 1; i++)
        {
            if (i == 0)
            {
                // First triangle: center -> edge1[1] -> edge2[1]
                Triangle tri = new Triangle();
                tri.vertices = new Vector3[] { edge1Points[0], edge1Points[1], edge2Points[1] };
                tri.boneWeights = new BoneWeight[] { edge1BoneWeights[0], edge1BoneWeights[1], edge2BoneWeights[1] };
                tri.uvs = new Vector2[] { calculateUV(edge1Points[0]), calculateUV(edge1Points[1]), calculateUV(edge2Points[1]) };
                tri.colors = new Color[] { edge1Colors[0], edge1Colors[1], edge2Colors[1] };
                result.Add(tri);
                //Debug.Log($"[SubdivideTriangle1Plus2N] Triangle {result.Count}: Center({edge1Points[0].x:F2},{edge1Points[0].y:F2},{edge1Points[0].z:F2}) Edge1[1]({edge1Points[1].x:F2},{edge1Points[1].y:F2},{edge1Points[1].z:F2}) Edge2[1]({edge2Points[1].x:F2},{edge2Points[1].y:F2},{edge2Points[1].z:F2})");
            }
            else
            {
                // Two triangles for each subsequent ring
                // Triangle A: edge1[i] -> edge2[i] -> edge1[i+1]
                Triangle triA = new Triangle();
                triA.vertices = new Vector3[] { edge1Points[i], edge2Points[i], edge1Points[i + 1] };
                triA.boneWeights = new BoneWeight[] { edge1BoneWeights[i], edge2BoneWeights[i], edge1BoneWeights[i + 1] };
                triA.uvs = new Vector2[] { calculateUV(edge1Points[i]), calculateUV(edge2Points[i]), calculateUV(edge1Points[i + 1]) };
                triA.colors = new Color[] { edge1Colors[i], edge2Colors[i], edge1Colors[i + 1] };
                result.Add(triA);
                //Debug.Log($"[SubdivideTriangle1Plus2N] Triangle {result.Count}: Edge1[{i}]({edge1Points[i].x:F2},{edge1Points[i].y:F2},{edge1Points[i].z:F2}) Edge2[{i}]({edge2Points[i].x:F2},{edge2Points[i].y:F2},{edge2Points[i].z:F2}) Edge1[{i + 1}]({edge1Points[i + 1].x:F2},{edge1Points[i + 1].y:F2},{edge1Points[i + 1].z:F2})");

                // Triangle B: edge2[i] -> edge2[i+1] -> edge1[i+1]
                Triangle triB = new Triangle();
                triB.vertices = new Vector3[] { edge2Points[i], edge2Points[i + 1], edge1Points[i + 1] };
                triB.boneWeights = new BoneWeight[] { edge2BoneWeights[i], edge2BoneWeights[i + 1], edge1BoneWeights[i + 1] };
                triB.uvs = new Vector2[] { calculateUV(edge2Points[i]), calculateUV(edge2Points[i + 1]), calculateUV(edge1Points[i + 1]) };
                triB.colors = new Color[] { edge2Colors[i], edge2Colors[i + 1], edge1Colors[i + 1] };
                result.Add(triB);
                //Debug.Log($"[SubdivideTriangle1Plus2N] Triangle {result.Count}: Edge2[{i}]({edge2Points[i].x:F2},{edge2Points[i].y:F2},{edge2Points[i].z:F2}) Edge2[{i + 1}]({edge2Points[i + 1].x:F2},{edge2Points[i + 1].y:F2},{edge2Points[i + 1].z:F2}) Edge1[{i + 1}]({edge1Points[i + 1].x:F2},{edge1Points[i + 1].y:F2},{edge1Points[i + 1].z:F2})");
            }
        }
        
        //Debug.Log($"[SubdivideTriangle1Plus2N] SUMMARY: n={n}, Created {result.Count} triangles (expected: {1 + 2 * n})");
        return result;
    }
    
    // Add the missing InterpolateBoneWeight method
    private static BoneWeight InterpolateBoneWeight(BoneWeight bw1, BoneWeight bw2, float t)
    {
        BoneWeight result = new BoneWeight();
        
        result.boneIndex0 = bw1.boneIndex0;
        result.boneIndex1 = bw1.boneIndex1;
        result.boneIndex2 = bw1.boneIndex2;
        result.boneIndex3 = bw1.boneIndex3;
        
        result.weight0 = Mathf.Lerp(bw1.weight0, bw2.weight0, t);
        result.weight1 = Mathf.Lerp(bw1.weight1, bw2.weight1, t);
        result.weight2 = Mathf.Lerp(bw1.weight2, bw2.weight2, t);
        result.weight3 = Mathf.Lerp(bw1.weight3, bw2.weight3, t);
        
        return result;
    }
}