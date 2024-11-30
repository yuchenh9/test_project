using UnityEngine;

public class PrintResolution : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Game view resolution: " + Screen.width + "x" + Screen.height);
    }
}
