using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace DynamicMeshCutter
{
    public static class VertexColorLogger
    {
        /// <summary>
        /// Logs all vertex RGBA values from a VirtualMesh
        /// </summary>
        /// <param name="virtualMesh">The virtual mesh to log</param>
        /// <param name="logPrefix">Prefix for the log message</param>
        public static void LogVertexColors(VirtualMesh virtualMesh, string logPrefix = "VirtualMesh")
        {
            if (virtualMesh == null)
            {
                Debug.Log($"{logPrefix}: VirtualMesh is null");
                return;
            }

            if (virtualMesh.Colors == null || virtualMesh.Colors.Length == 0)
            {
                Debug.Log($"{logPrefix}: No vertex colors found (Colors array is null or empty). Vertex count: {virtualMesh.Vertices?.Length ?? 0}");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{logPrefix} - Vertex Colors ({virtualMesh.Colors.Length} vertices):");
            sb.AppendLine("Index | Position | RGBA");
            sb.AppendLine("------|----------|-----");

            for (int i = 0; i < virtualMesh.Colors.Length; i++)
            {
                Color color = virtualMesh.Colors[i];
                Vector3 position = i < virtualMesh.Vertices.Length ? virtualMesh.Vertices[i] : Vector3.zero;
                
                sb.AppendLine($"{i:D4} | {position.x:F2},{position.y:F2},{position.z:F2} | R:{color.r:F3} G:{color.g:F3} B:{color.b:F3} A:{color.a:F3}");
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs all vertex RGBA values from a Unity Mesh
        /// </summary>
        /// <param name="mesh">The Unity mesh to log</param>
        /// <param name="logPrefix">Prefix for the log message</param>
        public static void LogVertexColors(Mesh mesh, string logPrefix = "Mesh")
        {
            if (mesh == null)
            {
                Debug.Log($"{logPrefix}: Mesh is null");
                return;
            }

            Color[] colors = mesh.colors;
            if (colors == null || colors.Length == 0)
            {
                Debug.Log($"{logPrefix}: No vertex colors found (Colors array is null or empty). Vertex count: {mesh.vertexCount}");
                return;
            }

            Vector3[] vertices = mesh.vertices;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{logPrefix} - Vertex Colors ({colors.Length} vertices):");
            sb.AppendLine("Index | Position | RGBA");
            sb.AppendLine("------|----------|-----");

            for (int i = 0; i < colors.Length; i++)
            {
                Color color = colors[i];
                Vector3 position = i < vertices.Length ? vertices[i] : Vector3.zero;
                
                sb.AppendLine($"{i:D4} | {position.x:F2},{position.y:F2},{position.z:F2} | R:{color.r:F3} G:{color.g:F3} B:{color.b:F3} A:{color.a:F3}");
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs vertex colors from multiple VirtualMeshes (typically the cut results)
        /// </summary>
        /// <param name="virtualMeshes">Array of virtual meshes to log</param>
        /// <param name="logPrefix">Prefix for the log message</param>
        public static void LogVertexColors(VirtualMesh[] virtualMeshes, string logPrefix = "Cut Results")
        {
            if (virtualMeshes == null || virtualMeshes.Length == 0)
            {
                Debug.Log($"{logPrefix}: No meshes to log");
                return;
            }

            Debug.Log($"{logPrefix}: Logging {virtualMeshes.Length} cut mesh pieces");
            
            for (int i = 0; i < virtualMeshes.Length; i++)
            {
                LogVertexColors(virtualMeshes[i], $"{logPrefix} - Piece {i + 1}");
            }
        }

        /// <summary>
        /// Logs vertex colors from a GameObject's MeshFilter
        /// </summary>
        /// <param name="gameObject">GameObject with MeshFilter component</param>
        /// <param name="logPrefix">Prefix for the log message</param>
        public static void LogVertexColors(GameObject gameObject, string logPrefix = "GameObject")
        {
            if (gameObject == null)
            {
                Debug.Log($"{logPrefix}: GameObject is null");
                return;
            }

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.Log($"{logPrefix}: No MeshFilter component found on {gameObject.name}");
                return;
            }

            LogVertexColors(meshFilter.sharedMesh, $"{logPrefix} ({gameObject.name})");
        }

        /// <summary>
        /// Compare vertex colors between original and cut results
        /// </summary>
        /// <param name="original">Original VirtualMesh</param>
        /// <param name="cutResults">Array of cut result VirtualMeshes</param>
        public static void CompareVertexColors(VirtualMesh original, VirtualMesh[] cutResults)
        {
            Debug.Log("=== VERTEX COLOR COMPARISON ===");
            
            // Log original
            LogVertexColors(original, "ORIGINAL");
            
            // Log cut results
            LogVertexColors(cutResults, "CUT RESULTS");
            
            // Summary
            int originalColorCount = (original?.Colors?.Length ?? 0);
            int totalCutColorCount = 0;
            
            if (cutResults != null)
            {
                for (int i = 0; i < cutResults.Length; i++)
                {
                    if (cutResults[i]?.Colors != null)
                        totalCutColorCount += cutResults[i].Colors.Length;
                }
            }
            
            Debug.Log($"SUMMARY: Original had {originalColorCount} vertex colors, cut results have {totalCutColorCount} total vertex colors across {cutResults?.Length ?? 0} pieces");
        }
    }
} 