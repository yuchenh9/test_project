using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class addplane : MonoBehaviour
{   
    
    public static addplane Instance { get; private set; }
    
    public GameObject planeToAdd;
    
    // Start is called before the first frame update
    public void addPlane(Vector3 position){
        Quaternion rotation = Quaternion.identity; // No rotation

        // Instantiate the prefab at the specified position and rotation
        GameObject myObject = Instantiate(planeToAdd, position, rotation);

    }

    // Update is called once per frame
    
    
}
