using UnityEngine;
using System.Collections.Generic;

public class cylinder_controller : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject cylinder_preset;
    public FloatTweener floatTweener;
    public float y_distance;
    public List<cylinder_data> cylinder_list = new List<cylinder_data>();
    public bool button=true;
    private bool old_button_state=true;
    public void Button(){
        if(button!=old_button_state){
            parseFromJson(json);
            old_button_state=button;
        }
    }

    [System.Serializable]
    public class Cylinder_JsonItem
    {
        public string name;
        public string color; // HEX string (e.g., "#FF0000")
        public float value;
    }
    //string list_of_nutrient_names=["Fiber","Vitamin A (Beta-Carotene)","Vitamin C","Potassium","Folate (Vitamin B9)"];
    string json = @"{
    ""cylinders"": [
        {
        ""name"": ""Fiber"",
        ""color"": ""#2E8B57"",
        ""value"": 0.27
        },
        {
        ""name"": ""Vitamin A"",
        ""color"": ""#FFA500"",
        ""value"": 0.01
        },
        {
        ""name"": ""Vitamin C"",
        ""color"": ""#FF0000"",
        ""value"": 0.30
        },
        {
        ""name"": ""Potassium"",
        ""color"": ""#800080"",
        ""value"": 0.26
        },
        {
        ""name"": ""Vitamin B9"",
        ""color"": ""#FFD700"",
        ""value"": 0.18
        }
    ]
    }";

    [System.Serializable]
    public class Cylinder_JsonData
    {
        public List<Cylinder_JsonItem> cylinders;
    }

    void parseFromJson(string json){
        Cylinder_JsonData data = JsonUtility.FromJson<Cylinder_JsonData>(json);
        cylinder_list.Clear();
        foreach (Cylinder_JsonItem cylinder in data.cylinders)
        {
            Color color = HexToColor(cylinder.color);
            cylinder_list.Add(new cylinder_data(cylinder_name:cylinder.name,cylinder_color:color,cylinder_ratio:cylinder.value));
        }
        updataCylinders();
    }

    public static Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');

        // Parse HEX to RGB
        float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        return new Color(r, g, b);

    }


    void Start()
    {
        parseFromJson(json);
    }
    
    private void OnValidate()
    {   Button();
        updataCylinders();
    }

    void updataCylinders()
    {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);

        }
        for (int i = 0; i < cylinder_list.Count; i++)
        {
            Debug.Log(transform);
            GameObject new_gameobject = Instantiate(cylinder_preset);
            new_gameobject.transform.SetParent(transform);
            new_gameobject.transform.localPosition=new Vector3(0f,-y_distance*i,0f);


            
            new_gameobject.name = $"Cylinder_{i}";
            DynamicCapsule capsule=new_gameobject.GetComponent<DynamicCapsule>();
            capsule.capsuleColor=cylinder_list[i].color;
            capsule.max_ratio=cylinder_list[i].max_length;
            capsule.ratio=cylinder_list[i].ratio;
            capsule.Nutrient_name.text=cylinder_list[i].name;
            FloatTweener floatTweener=capsule.GetComponent<FloatTweener>();
            floatTweener.StartTweenFloat();
        }
        
    }
    

}
