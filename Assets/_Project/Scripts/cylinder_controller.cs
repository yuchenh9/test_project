using UnityEngine;
using System.Collections.Generic;

public class cylinder_controller : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject cylinder_preset;
    public FloatTweener floatTweener;
    public float y_distance;
    public List<cylinder_data> cylinder_list = new List<cylinder_data>();
    void Start()
    {
        SyncSceneInstances();
    }
    
    private void OnValidate()
    {
        SyncSceneInstances();
    }

    void SyncSceneInstances()
    {
        foreach (Transform child in transform) {
            Destroy(child.gameObject);

        }
        for (int i = 0; i < cylinder_list.Count; i++)
        {
            
            GameObject new_gameobject = Instantiate(cylinder_preset);
            new_gameobject.transform.position=new Vector3(0f,-y_distance*i,0f);
            new_gameobject.transform.SetParent(transform);
            new_gameobject.name = $"Cylinder_{i}";
            DynamicCapsule capsule=new_gameobject.GetComponent<DynamicCapsule>();
            capsule.floatTweener=floatTweener;
            capsule.capsuleColor=cylinder_list[i].color;
            capsule.max_ratio=cylinder_list[i].max_length;
            capsule.ratio=cylinder_list[i].ratio;
            
        }
        
    }
    

}
