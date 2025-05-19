using UnityEngine;
using System;
using System.Collections.Generic;
using MPUIKIT;
public class sliding_icons : MonoBehaviour
{
    public static sliding_icons Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<GameObject> icons;
    // Assuming 'buttonObject' is your UI Button GameObject
    float width=0f;
    
private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        if(icons.Count!=0){

            RectTransform rectTransform = icons[0].GetComponent<RectTransform>();
            width = rectTransform.rect.width;

        }
        display(0);
    }
    public void display(int index){
        for (int i = 0; i < icons.Count; i++){
            icons[i].transform.parent=gameObject.transform;
            icons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2((i-index)*width, 0f);
            icons[i].SetActive(true);
            float x=icons[i].GetComponent<RectTransform>().anchoredPosition.x;
            //setAlpha(icons[i],((Math.Cos(y/(2*width)*Math.PI)+1)/2));
            //Debug.Log(Math.Cos(x/(2f*width)*Math.PI));
            setAlpha(icons[i],0.5f);
        }
    }
    private void setAlpha(GameObject icon,float alpha){

        if (icon.TryGetComponent<MPImage>(out MPImage mpimage))
        {            
            //Debug.Log(mpimage.color);
            mpimage.color = new Color(
                mpimage.color.r, 
                mpimage.color.g, 
                mpimage.color.b, 
                alpha
            );
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
