using UnityEngine;
using System.Collections;

public class CaptureFrames : MonoBehaviour 
{
    public string folder = "ScreenshotFolder";
    public int frameRate = 25;

    void Start() 
    {
        Time.captureFramerate = frameRate;
        System.IO.Directory.CreateDirectory(folder);
    }

    void Update() 
    {
        string name = string.Format("{0}/{1:D04} shot.png", folder, Time.frameCount );
        ScreenCapture.CaptureScreenshot(name,4);
    }
}
