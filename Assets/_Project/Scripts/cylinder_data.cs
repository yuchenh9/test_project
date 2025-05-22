using UnityEngine;
[System.Serializable]
public class cylinder_data {
    public string name;
    public float max_length=3.7f;
    public float ratio=0.5f;
    public Color color=new Color(0.6698113f, 0.1800908f, 0.1800908f);
    public cylinder_data(string cylinder_name,Color cylinder_color,float cylinder_max_length=3.7f,float cylinder_ratio=0.5f){
        name=cylinder_name;
        max_length=cylinder_max_length;
        ratio=cylinder_ratio;
        color=cylinder_color;
    }
}