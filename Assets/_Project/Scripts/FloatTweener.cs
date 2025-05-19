using UnityEngine;
using System.Collections;

public class FloatTweener : MonoBehaviour
{
    [SerializeField] private float duration = 1f;
    [SerializeField] private float startValue = 0f;
    [SerializeField] private float targetValue = 1f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    public bool button=true;
    private bool old_button_state=true;
    private DynamicCapsule dynamicCapsule;
    public void Start(){
        dynamicCapsule = GetComponent<DynamicCapsule>();
        Debug.Log(dynamicCapsule);
        targetValue=dynamicCapsule.ratio;
    }
    public void OnValidate(){
        if(button!=old_button_state){
            StartTweenFloat();
            old_button_state=button;
        }
    }
    public void StartTweenFloat()
    {
        Debug.Log(dynamicCapsule);
        StartCoroutine(TweenFloat());
    }

    IEnumerator TweenFloat()
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);
            dynamicCapsule.ratio = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }
        
        dynamicCapsule.ratio = targetValue;
    }
}