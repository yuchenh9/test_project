using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for accessing UI components

public class xButtonController : MonoBehaviour
{
    
    public Text ybuttonText; // Assign this via the Unity Inspector
    public Text xbuttonText; // Assign this via the Unity Inspector

    public void XButtonClick()
    {
        Debug.Log("Xclicked");
        if (ybuttonText != null)
        {
            Color ycolor = ybuttonText.color;
            ycolor.a = 0.5f; // Set alpha to 0.5 for semi-transparency
            ybuttonText.color = ycolor;
            
            Color xcolor = xbuttonText.color;
            xcolor.a = 1f; // Set alpha to 0.5 for semi-transparency
            xbuttonText.color = xcolor;

            
        }
        else
        {
            Debug.LogError("YText component not found on the button.");
        }
    }
    public void YButtonClick()
    {
        Debug.Log("Xclicked");
        if (xbuttonText != null)
        {
            Color ycolor = ybuttonText.color;
            ycolor.a = 1f; // Set alpha to 0.5 for semi-transparency
            ybuttonText.color = ycolor;
            
            Color xcolor = xbuttonText.color;
            xcolor.a = 0.5f; // Set alpha to 0.5 for semi-transparency
            xbuttonText.color = xcolor;
        }
        else
        {
            Debug.LogError("XText component not found on the button.");
        }
    }
}
