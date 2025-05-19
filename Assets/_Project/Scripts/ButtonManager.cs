using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonManager : MonoBehaviour
{
    public List<Button> buttons = new List<Button>(); // List of UI Buttons
    public List<UnityEvent> buttonEvents = new List<UnityEvent>(); // List of corresponding UnityEvents

    void Start()
    {
        // Ensure both lists have the same number of elements
        if (buttons.Count != buttonEvents.Count)
        {
            Debug.LogWarning("Buttons and Events lists are not the same length!");
            return;
        }

        // Assign each button its corresponding UnityEvent
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i; // Local copy to avoid closure issues
            if (buttons[i] != null && buttonEvents[i] != null)
            {
                buttons[i].onClick.AddListener(() => buttonEvents[index].Invoke());
            }
        }
    }

    void OnDestroy()
    {
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}
