using UnityEngine;

public class vertex_display : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var m = GetComponent<SkinnedMeshRenderer>()?.sharedMesh ??
                GetComponent<MeshFilter>()?.sharedMesh;

        Debug.Log($"src v:{m.vertexCount}  colors:{m.colors.Length}  colors32:{m.colors32.Length}");
        for (int i = 0; i < 4 && i < m.vertexCount; i++)
            Debug.Log($"{i}: Color={m.colors[i]}  Color32={m.colors32[i]}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
