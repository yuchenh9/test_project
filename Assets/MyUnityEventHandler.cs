using UnityEngine;
using UnityEngine.Events;

public class MyUnityEventHandler : MonoBehaviour
{
    // Exposed UnityEvent for assigning functions in Inspector
    public UnityEvent myEvent;

    void Start()
    {
        // Ensure the event is initialized
        if (myEvent == null)
            myEvent = new UnityEvent();
    }

    public void TriggerEvent()
    {
        // Invoke the event (calls all assigned functions)
        myEvent?.Invoke();
    }
}
