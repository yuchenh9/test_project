using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMeshCutter
{
    public class UnifiedButtonHandler : MonoBehaviour
    {
        private GameObject selectedObject;
        private int sliceNumber;
        private List<GameObject> currentlist;
        public GameObject target;
        public Text cutNumber;  // Assign this from the inspector
        
        private bool Rigidbodies_are_kinematic=false;

        void Start()
        {
            selectedObject=gamecontroller.Instance.selectedObject;
        
        }

        void Update()
        {

            GameObject controller=GameObject.Find("controller");

            //Enable physics if the cut tast is complete and the rigidbodies are kinematic
            if(Rigidbodies_are_kinematic&&gamecontroller.Instance.cutCompleteFlag){
                Set_the_rigidbodies_in_the_currentlist_nonkinematic();
                Rigidbodies_are_kinematic=false;
            }
        }


        // Method to update the button text
        public void SetCutNumber(string text)
        {
            if (cutNumber != null)
                cutNumber.text = text;
            else
                Debug.LogError("No Text component found on the button.");
        }

        public void PlusButtonClick()
        {
            // this deletes all the objects from CurrentList, which stores the objects we want to cut
            gamecontroller.Instance.clearCurrentList();

            //this decrements the slice number by one
            gamecontroller.Instance.SliceNumber+=1;

            //this updates the displayed slice number
            string textString = String.Format("{0}", gamecontroller.Instance.SliceNumber);
            SetCutNumber(textString);
    

            //this calls the cutting function 
            SliceButtonClick();

            //this set the flag that physics is disabled
            Rigidbodies_are_kinematic=true;


        }
        public void MinusButtonClick()
        
        {
            //this deletes all the objects from CurrentList, which stores the objects we want to cut
            gamecontroller.Instance.clearCurrentList();

            //this decrements the slice number by one
            gamecontroller.Instance.SliceNumber-=1;

            //this updates the displayed slice number
            string textString = String.Format("{0}", gamecontroller.Instance.SliceNumber);
            SetCutNumber(textString);
            
            //this calls the cutting function 
            SliceButtonClick();

            //this set the flag that physics is disabled
            Rigidbodies_are_kinematic=true;


        }
        public void Set_the_rigidbodies_in_the_currentlist_nonkinematic()
        {
            currentlist= gamecontroller.Instance.currentlist;

            foreach (GameObject go in currentlist)
            {
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
                var rb = go.transform.parent.GetComponent<Rigidbody>();
                
                // Check if the Rigidbody component exists
                if (rb != null)
                {
                    rb.isKinematic = false; // Makes the Rigidbody non kinematic, which enables physics interactions
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
        public void setAxis(string[] axises){
            gamecontroller.Instance.axises=axises;
        }
        public void SliceButtonClick()
            {
                currentlist= gamecontroller.Instance.currentlist;

                Transform selected=selectedObject.transform.Find("object");

                //this hides the object that is being cut and instantiates a new object to be displays, 
                //so that when the object is being cut, the user only sees a intact object instead of an object getting 
                //sliced piece by piece, which is ugly
                int childCount = selected.transform.childCount;
                selected.transform.GetChild(childCount-1).gameObject.GetComponent<Renderer>().enabled=false;
                GameObject instantiated = Instantiate(selected.transform.GetChild(childCount-1).gameObject);

                //
                currentlist.Add(instantiated);
                if(gamecontroller.Instance.axises[0]=="cy")
                {
                    MouseBehaviour.Instance.circularSlice(gamecontroller.Instance.SliceNumber);
                }else{
                    MouseBehaviour.Instance.SliceByAllAxis(gamecontroller.Instance.SliceNumber,gamecontroller.Instance.axises);
                }

                Debug.Log("2Button was clicked!");
            }
    
        public void HandleButtonClick(string buttonName)
        {
            switch (buttonName)
            {
                case "plusButton":
                    PlusButtonClick();
                    Debug.Log("plusButton was clicked!");
                    break;
                case "minusButton":
                    MinusButtonClick();
                    Debug.Log("minusButton was clicked!");
                    break;
                case "xButton":
                    Debug.Log("xButton was clicked!");
                    setAxis(new string[]{"x"});
                    break;
                case "yButton":
                    Debug.Log("yButton was clicked!");
                    setAxis(new string[]{"y"});
                    break;
                    
                case "zButton":
                    Debug.Log("zButton was clicked!");
                    setAxis(new string[]{"z"});
                    break;
                    
                case "xyButton":
                    Debug.Log("xButton was clicked!");
                    setAxis(new string[]{"x","y"});
                    break;
                case "yzButton":
                    Debug.Log("yButton was clicked!");
                    setAxis(new string[]{"y","z"});
                    break;
                    
                case "zxButton":
                    Debug.Log("zButton was clicked!");
                    setAxis(new string[]{"z","x"});
                    break;
                    
                case "xyzButton":
                    Debug.Log("xyzButton was clicked!");
                    setAxis(new string[]{"z","x","y"});
                    break;
                case "circularYButton":
                    Debug.Log("circularYButton was clicked!");
                    setAxis(new string[]{"cy"});
                    break;
                default:
                    Debug.LogError("Unknown button");
                    break;
            }
        }
    }
}