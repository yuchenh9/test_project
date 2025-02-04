using UnityEngine;
using System;

public class foodMenuController : MonoBehaviour
{
    // Define a public static event for the plus button
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void onCutButtonClick()
    {
        UIController.Instance.HandleChangeUI("foodCutMenu");
    }
}