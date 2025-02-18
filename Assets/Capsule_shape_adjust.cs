using UnityEngine;

public class Capsule_shape_adjust : MonoBehaviour
{
    public Transform cylinder;
    public Transform left_sphere;
    public Transform right_sphere;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void adjust_positions(){

        Vector3 cylinder_position = cylinder.position;
        Vector3 right_sphere_position=right_sphere.position;
        Vector3 scale = cylinder.localScale;
        float y=scale.y;
        right_sphere_position.y+=y-1;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
