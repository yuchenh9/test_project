using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class FPSDisplay2 : MonoBehaviour
{
    public TextMeshProUGUI fpsText; 
    //public Text fpsText; // Assign a UI Text element to display FPS (optional, for UI display)
    private float deltaTime = 0.0f;
    void start(){
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        // Optional: Set the fixed timestep for physics updates
        Time.fixedDeltaTime = 1.0f / 60.0f;
    }
    void Update()
    {
        // Calculate the delta time
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        Application.targetFrameRate = 60;
    }

    void OnGUI()
    {
        // Calculate FPS
        int fps = Mathf.CeilToInt(1.0f / deltaTime);
        string fpsDisplay = fps + " FPS";

        // Display FPS using GUI.Label
        GUI.Label(new Rect(10, 10, 150, 20), fpsDisplay);

        // If using a UI Text element, update it
        if (fpsText != null)
        {
            fpsText.text = fpsDisplay;
        }
    }
}
