using UnityEngine;

public class DynamicCapsule : MonoBehaviour
{

    public Transform Cylinder;
    public Transform LeftSphere;
    public Transform RightSphere;
    public string name;
    public float max_ratio=3.7f;
    public float ratio=1f;
    public FloatTweener floatTweener;
    private float _ratio;
    public Color capsuleColor = Color.white;
    private void OnValidate()
    {
        if (ratio!=_ratio){
            _ratio=ratio;
            UpdateCapsule(ratio);
        }
    }
    void Start()
    {
    }
    void Update(){

        if (ratio!=_ratio){
            _ratio=ratio;
            UpdateCapsule(ratio);
        }
    }
    private void ApplyColorToAllParts()
    {
        SetColor(Cylinder);
        SetColor(LeftSphere);
        SetColor(RightSphere);
    }

    private void SetColor(Transform part)
    {
        var partRenderer = part.GetComponent<Renderer>();
        if (partRenderer != null)
        {
            partRenderer.material.color = capsuleColor;
        }
    }
    // Call this method whenever CylinderLength changes
    public void UpdateCapsule(float ratio)
    {
        Vector3 newscale=new Vector3(1f,ratio*max_ratio,1f);
        Cylinder.transform.localScale=newscale;
        Renderer renderer = Cylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            //Debug.Log("Bounds center: " + bounds.center);
            RightSphere.transform.position=new Vector3(bounds.max.x,RightSphere.transform.position.y,RightSphere.transform.position.z);
        }

        ApplyColorToAllParts();
    }
}


