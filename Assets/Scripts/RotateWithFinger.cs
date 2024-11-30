using UnityEngine;
using System.Collections;

public class RotateWithFinger : MonoBehaviour
{
    private Vector2 startTouchPosition;
    private Vector2 currentTouchPosition;
    private Vector2 deltaTouchPosition;
    private Quaternion originalRotation;
    private bool isResetting = false;
    private Quaternion initialRotation;
    private Quaternion afterRotation;
    private Coroutine undoCoroutine = null;
    private Coroutine returnCoroutine = null;


    void Start()
    {
        originalRotation = transform.rotation;
    }

    void Update()
    {
        HandleMouseInput();
    }
/*    void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                startTouchPosition = touch.position;
                break;

            case TouchPhase.Moved:
                RotateObjectBasedOnInput(touch.position);
                break;

            case TouchPhase.Ended:
                UndoLastRotation();
                StartCoroutine(ReturnToOriginalRotation());
                break;
        }
    }
*/
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
            initialRotation = transform.rotation;  // Store the rotation before any transformations
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
        transform.Rotate(Vector3.down * deltaTouchPosition.x * 0.1f, Space.World);//where you set the rotate speed
        transform.Rotate(Vector3.right * deltaTouchPosition.y * 0.1f, Space.World);

        afterRotation = transform.rotation;  // Store the rotation after transformations

        startTouchPosition = currentTouchPosition; 
    }
    /*
    void RotateObjectBasedOnInput(Vector2 currentPosition)
    {
        currentTouchPosition = currentPosition;
        deltaTouchPosition = currentTouchPosition - startTouchPosition;

        // Rotate the object based on the drag distance.
        transform.Rotate(Vector3.down * deltaTouchPosition.x * 0.1f, Space.World);
        transform.Rotate(Vector3.right * deltaTouchPosition.y * 0.1f, Space.World);

        startTouchPosition = currentTouchPosition; // Reset starting position for continuous rotation
    }

    void UndoLastRotation()
    {
        transform.Rotate(Vector3.up * deltaTouchPosition.x * 0.1f, Space.World); // 10% of the last delta in the opposite direction
        transform.Rotate(Vector3.left * deltaTouchPosition.y * 0.1f, Space.World); // 10% of the last delta in the opposite direction
    }
    */
    IEnumerator UndoRotationSmoothly(float duration)
    {
        float elapsedTime = 0;
        float percentageToReverse = 0.15f;  // 15% reversal where you set the reversal amount
        
        Quaternion targetRotation = Quaternion.Slerp(afterRotation, initialRotation, percentageToReverse);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;  // Normalized time
            float easeOut = 1 - (1 - t) * (1 - t);  // Ease out formula

            transform.rotation = Quaternion.Slerp(afterRotation, targetRotation, easeOut);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        undoCoroutine = null; 

    }
    /*
        IEnumerator UndoRotationSmoothly(float duration)
        {
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                transform.rotation = Quaternion.Lerp(afterRotation, initialRotation, (elapsedTime / duration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            
        }
    */

    IEnumerator ReturnToOriginalRotation()
    {
        if (isResetting) yield break;

        isResetting = true;

        yield return new WaitForSeconds(0.25f);  // Brief delay after the undo effect where you set the wait time

        // Now smoothly rotate back to the original rotation
        float elapsed = 0f;
        float duration = 0.5f;
        Quaternion currentRotation = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(currentRotation, originalRotation, elapsed / duration);
            yield return null;
        }

        transform.rotation = originalRotation;
        isResetting = false;
        returnCoroutine = null;
    }
}
