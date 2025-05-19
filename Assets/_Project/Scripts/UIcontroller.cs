using UnityEngine;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public Transform child; // Assignable via Inspector
    public Transform canvas;
    public void HandleChangeUI(string message)
    {
        Transform targetUI=canvas.Find(message);
        RefreshAndSelect(targetUI);
    }
    private void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Uncomment if you want this controller to persist across scenes.
        // DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        if (child == null) // Check if child is assigned in Inspector
        {
            Debug.LogError("UIController: 'child' Transform is not assigned in the Inspector!");
            return; // Prevent execution if no child is assigned
        }

        RefreshAndSelect(child);
    }

    void DisableChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
        }
    }

    void RefreshAndSelect(Transform selectedUI)
    {
        DisableChildren(canvas);
        if (selectedUI != null)
        {
            selectedUI.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("UIController: Attempted to select a null Transform!");
        }
    }
    
}
