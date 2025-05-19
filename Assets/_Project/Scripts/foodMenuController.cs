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

    public void onLeft(){
        if(SceneData.Instance.selectedSceneIndex>0){
        SceneData.Instance.selectedSceneIndex-=1;
        SceneData.Instance.ChangeCameraPosition(SceneData.Instance.selectedSceneIndex);
        sliding_icons.Instance.display(SceneData.Instance.selectedSceneIndex);

        }
        //Debug.Log("onLeft clicked");
    }
    public void onRight(){
        if(SceneData.Instance.selectedSceneIndex+1<SceneData.Instance.scenes.Count){
        SceneData.Instance.selectedSceneIndex+=1;
        SceneData.Instance.ChangeCameraPosition(SceneData.Instance.selectedSceneIndex);
        sliding_icons.Instance.display(SceneData.Instance.selectedSceneIndex);

        }
        //Debug.Log("onRight clicked");
        
    }
    public void onCutButtonClick()
    {
        UIController.Instance.HandleChangeUI("foodCutMenu");
        SceneData.Instance.ButtonCutClicked();
    }
}