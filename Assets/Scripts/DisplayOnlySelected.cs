using System.Collections;
using UnityEngine;
//using cakeslice;

namespace DynamicMeshCutter
{

public class DisplayOnlySelected : MonoBehaviour
{
    private GameObject selectedObject;  // Drag the GameObject or parent GameObject you want to display here in the Inspector


    private Vector2 startTouchPosition;
    private Vector2 currentTouchPosition;
    private Vector2 deltaTouchPosition;
    private Quaternion originalRotation;
    private bool isResetting = false;
    private Quaternion initialRotation;
    private Quaternion afterRotation;
    private Coroutine undoCoroutine = null;
    private Coroutine returnCoroutine = null;
    private int sliceNumber=2;
    //private bool gamecontrollerInited=false;
    void Start()
    {
        selectedObject=gamecontroller.Instance.selectedObject;
        ShowOnlySelected();
        hideAxis();
        //sliceNumber=gamecontroller.Instance.SliceNumber;
        //selectedObject=MouseBehaviour.Instance.selectedObject;
        originalRotation = selectedObject.transform.rotation;
        
    }
    void hideAxis(){
        //if(selectedObject.transform.childCount==3){
        Transform axises=selectedObject.transform.Find("axises");
        int axisesCount=axises.transform.childCount;
        for (int i = 0; i < axisesCount; i++)
        {
            Renderer childRenderer = axises.transform.GetChild(i).GetChild(0).GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.enabled = false; // Disable the Renderer
            }
            Renderer childRenderer2 = axises.transform.GetChild(i).GetChild(1).GetComponent<Renderer>();
            if (childRenderer2 != null)
            {
                childRenderer2.enabled = false; // Disable the Renderer
            }
        }
        //}
    }
    void Update()
    {
        HandleMouseInput();
        if (Input.GetKeyDown(KeyCode.P))
        {    
            //MouseBehaviour.Instance.SliceByAxis(gamecontroller.Instance.SliceNumber);
            Debug.Log("slice");
        }

    }
    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (undoCoroutine != null)
                {
                    StopCoroutine(undoCoroutine);
                    undoCoroutine = null;
                }

                if (returnCoroutine != null)
                {
                    StopCoroutine(returnCoroutine);
                    returnCoroutine = null;
                }

            isResetting = false;
            startTouchPosition = Input.mousePosition;//startTouchPosition is where the mouse first clicked
            initialRotation = selectedObject.transform.rotation;  // Store the rotation before any transformations
            }

        }
        else if (Input.GetMouseButton(0))
        {
            RotateObjectBasedOnInput(Input.mousePosition);//currentTouchPosition is Input.mousePosition
        }
        else if (Input.GetMouseButtonUp(0))
        {
            undoCoroutine = StartCoroutine(UndoRotationSmoothly(0.35f));
            returnCoroutine = StartCoroutine(ReturnToOriginalRotation());
        }

    }

    void RotateObjectBasedOnInput(Vector2 currentPosition)
    {
        

        currentTouchPosition = currentPosition;
        deltaTouchPosition = currentTouchPosition - startTouchPosition;

        // Apply the rotations as you previously did
        selectedObject.transform.Rotate(Vector3.down * deltaTouchPosition.x * 0.1f, Space.World);//where you set the rotate speed
        selectedObject.transform.Rotate(Vector3.right * deltaTouchPosition.y * 0.1f, Space.World);

        afterRotation = selectedObject.transform.rotation;  // Store the rotation after transformations

        startTouchPosition = currentTouchPosition; 
    }
    IEnumerator UndoRotationSmoothly(float duration)
    {
        float elapsedTime = 0;
        float percentageToReverse = 0.15f;  // 15% reversal where you set the reversal amount
        
        Quaternion targetRotation = Quaternion.Slerp(afterRotation, initialRotation, percentageToReverse);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;  // Normalized time
            float easeOut = 1 - (1 - t) * (1 - t);  // Ease out formula

            selectedObject.transform.rotation = Quaternion.Slerp(afterRotation, targetRotation, easeOut);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        undoCoroutine = null; 

    }

    IEnumerator ReturnToOriginalRotation()
    {
        if (isResetting) yield break;

        isResetting = true;

        yield return new WaitForSeconds(0.25f);  // Brief delay after the undo effect where you set the wait time

        // Now smoothly rotate back to the original rotation
        float elapsed = 0f;
        float duration = 0.5f;
        Quaternion currentRotation = selectedObject.transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            selectedObject.transform.rotation = Quaternion.Lerp(currentRotation, originalRotation, elapsed / duration);
            yield return null;
        }

        selectedObject.transform.rotation = originalRotation;
        isResetting = false;
        returnCoroutine = null;
    }
    public void ShowOnlySelected()
    {
        // Loop through each child of the root parent (this script should be attached to the root parent)
        foreach (Transform child in transform)
        {
            if (child.gameObject == selectedObject)
            {
                // Activate the selected GameObject or parent GameObject
                child.gameObject.SetActive(true);
                    // If the child itself has children (meaning it's a parent GameObject), check them too
                foreach (Transform grandchild in child)
                {
                    if (grandchild.gameObject == selectedObject)
                    {
                        grandchild.gameObject.SetActive(true);
                        child.gameObject.SetActive(true);  // Make sure the parent of the selected grandchild is also active
                    }
                    else if(child.gameObject != selectedObject) // Only deactivate if the main child wasn't the selected object
                    {
                        grandchild.gameObject.SetActive(false);
                    }
                }
                selectedObject.transform.position = Vector3.zero;

                // Make the camera look at the center of the scene
                //camera adjustment
                Camera.main.transform.position = new Vector3(0f,-0.33f,-15.2f);
                Camera.main.fieldOfView=50f;
                Camera.main.transform.LookAt(Vector3.zero);

            }
            else
            {
                // Deactivate other GameObjects or parent GameObjects
                child.gameObject.SetActive(false);
            }

            
        }
    }
    
}
}