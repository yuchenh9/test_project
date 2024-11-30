using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using UnityEngine.UI;  // Include the UI namespace

using TMPro;  // Namespace for TextMeshPro  

namespace DynamicMeshCutter
{
    public class ButtonController : MonoBehaviour
    {/*
        private GameObject selectedObject;
        private int sliceNumber;
        private List<GameObject> currentlist;
        public GameObject target;
        public Text buttonText;  // Assign this from the inspector
        
        private bool iNeedClickDoneButtonClick=false;
        // Start is called before the first frame update
        void Start()
        {
            selectedObject=gamecontroller.Instance.selectedObject;
        
            //Debug.Log("start");
            //Debug.Log(gamecontroller.Instance.SliceNumber);
        }

        // Update is called once per frame
        void Update()
        {
            //Debug.Log("updates");
            //Debug.Log(gamecontroller.Instance.SliceNumber);
            //ßscoreText.text = sliceNumber.ToString();
            GameObject controller=GameObject.Find("controller");
            if(iNeedClickDoneButtonClick&&gamecontroller.Instance.cutCompleteFlag){
                DoneButtonClick();
                iNeedClickDoneButtonClick=false;
            }
        }


        // Method to update the button text
        public void SetButtonText(string text)
        {
            if (buttonText != null)
                buttonText.text = text;
            else
                Debug.LogError("No Text component found on the button.");
        }

        public void PlusButtonClick()
        {
            gamecontroller.Instance.clearCurrentList();

            gamecontroller.Instance.SliceNumber+=1;
            
            string textString = String.Format("{0}", gamecontroller.Instance.SliceNumber);
            SetButtonText(textString);
            Debug.Log("MinusButton was clicked!");
            SliceButtonClick();
            iNeedClickDoneButtonClick=true;
            Debug.Log("PlusButton was clicked!");
        }
        public void MinusButtonClick()
        {   gamecontroller.Instance.clearCurrentList();

            gamecontroller.Instance.SliceNumber-=1;
            string textString = String.Format("{0}", gamecontroller.Instance.SliceNumber);
            SetButtonText(textString);
            Debug.Log("MinusButton was clicked!");
            SliceButtonClick();
            iNeedClickDoneButtonClick=true;
            Debug.Log("Minus Button was clicked!");
        }
        public void DoneButtonClick()
        {
            Debug.Log("DoneButton was clicked!");
            //currentlist = gamecontroller.Instance.currentlist;
            currentlist= gamecontroller.Instance.currentlist;
            foreach (GameObject go in currentlist)
            {
                // Do something with 'go'
                if (go==null){
                    continue;
                } else {
                    Debug.Log(go);
                    Debug.Log("is go");
                }
                
                var objectCollider = go.GetComponent<MeshCollider>();
                if (objectCollider != null)
                {
                    Debug.Log("collider set active");
                    objectCollider.enabled = true;
                } else {
                    Debug.Log("collider is null");
                }
                Debug.Log("go.transform is "+go.transform);
                Debug.Log("go.transform.parent is "+go.transform.parent);
                var rb = go.transform.parent.GetComponent<Rigidbody>();
                
                // Check if the Rigidbody component exists
                if (rb != null)
                {
                    rb.isKinematic = false; // Makes the Rigidbody non kinematic, which enbles physics interactions
                }
                else
                {
                    Debug.LogError("Rigidbody component not found on this GameObject.");
                }
            }   
        }
        public void flyButtonClick()
        {
            float speed = 3.0f;
            currentlist= gamecontroller.Instance.currentlist;
            IEnumerator MoveTowardsTarget(GameObject go)
            {
                // Continue the loop until the object is very close to the target
                while (Vector3.Distance(go.transform.position, target.transform.position) > 2.01f)
                //while(true)
                {
                    // Move our position a step closer to the target.
                    Vector3 newPosition = Vector3.MoveTowards(go.transform.position, target.transform.position, speed * Time.deltaTime);
                    go.transform.position = newPosition;

                    // Wait until next frame before continuing the loop
                    yield return null;
                }
                //go.transform.parent.GetComponent<Rigidbody>().drag = 1f; 
                // Optionally, perform an action when the target is reached
                Debug.Log("Target reached!");
            }
            foreach (GameObject go in currentlist)
            {
                 if (go==null){
                    continue;
                }
                var rb = go.transform.parent.GetComponent<Rigidbody>();
    
                // Check if the Rigidbody component exists
                if (rb != null)
                {
                    rb.useGravity = false;
                    StartCoroutine(MoveTowardsTarget(go));
                }
                else
                {
                    Debug.LogError("Rigidbody component not found on this GameObject.");
                }

            }
        }
        public void SliceButtonClick()
            {
                currentlist= gamecontroller.Instance.currentlist;
                Transform selected=selectedObject.transform.Find("object");
                int childCount = selected.transform.childCount;
                selected.transform.GetChild(childCount-1).gameObject.GetComponent<Renderer>().enabled=false;
                GameObject instantiated = Instantiate(selected.transform.GetChild(childCount-1).gameObject);
                currentlist.Add(instantiated);//
                MouseBehaviour.Instance.SliceByAllAxis(gamecontroller.Instance.SliceNumber,gamecontroller.Instance.sliceAxises);
            
                Debug.Log("2Button was clicked!");
            }
    */}
}
