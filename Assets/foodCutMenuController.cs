using UnityEngine;
using System;

using TMPro;
public class foodCutMenu : MonoBehaviour
{
    // Define a public static event for the plus button

    [SerializeField] private TextMeshProUGUI text;
    [SerializeField]    private TextMeshProUGUI cutSomethingToText;
    private int cutNumber=2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void onDropdown(int index){
        Debug.Log(index);
    }

    public void onGoBack()
    {
        UIController.Instance.HandleChangeUI("foodMenu");
    }
    public void onPlus()
    {
        cutNumber++;
        text.text=cutNumber.ToString();
    }
    public void onMinus()
    {
        if(cutNumber>2){
        cutNumber--;

        }
        text.text=cutNumber.ToString();;
    }
    public void onCut()
    {
    }
}
