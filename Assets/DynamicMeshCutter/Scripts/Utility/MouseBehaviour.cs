using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;

namespace DynamicMeshCutter
{

    [RequireComponent(typeof(LineRenderer))]
    public class MouseBehaviour : CutterBehaviour
    {
        public static MouseBehaviour Instance { get; private set; } 

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optionally keep this object alive when changing scenes
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public LineRenderer LR => GetComponent<LineRenderer>();
        private Vector3 _from;
        private Vector3 _to;
        private bool _isDragging;
        private GameObject selectedObject;  // Drag the GameObject or parent GameObject you want to display here in the Inspector
        public GameObject planeToAdd;
    
        private void start(){
            
        }
    // Start is called before the first frame update
        public void addPlane(Vector3 position,Vector3 normal){
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            // Instantiate the prefab at the specified position and rotation
            GameObject myObject = Instantiate(planeToAdd, position, rotation);

        }
        protected override void Update()
        {
            if(selectedObject==null){
                selectedObject=gamecontroller.Instance.selectedObject;
            }
            base.Update();
        
        }
        public void SliceByAxis(int n,int a){
            while (GameObject.Find("plane")!=null){
                GameObject gameObject=GameObject.Find("plane");
                Destroy(gameObject,0);
            }
            if (n<2){
                //Debug.Log("n smaller than 2");
                return;
            }
            
            Transform axis = selectedObject.transform.Find("axises").GetChild(a);
            Transform axis_point_start = axis.transform.GetChild(0);
            Transform axis_point_end = axis.transform.GetChild(1);
            Vector3 differenceVector = axis_point_end.position - axis_point_start.position;

            List<GameObject> currentlist = gamecontroller.Instance.currentlist;
            //Debug.Log(selectedObject.transform.childCount);
            //currentlist.Add(selectedObject.transform.GetChild(selectedObject.transform.childCount-1).gameObject);//
            int index = -1;
            for (int i = 0; i < currentlist.Count; i++)
            {
                if(currentlist[i] != null){
                    index=i;
                    break;
                }
            }
            if(index==-1) {
                throw new Exception("no gameobject found in currentlist");
            }
           
            //Debug.Log("onCut"+"min:"+minx+"max:"+maxx);
            Vector3 deltaV3=differenceVector/n;
            for (int i = 0; i < n-1; i++)
            {       
                Vector3 point=axis_point_start.position+deltaV3*(i+1);
                Vector3 normal=deltaV3;
                float size=1f;
                if(gamecontroller.Instance.addPlane){addPlane(point,normal);}//if addplane box is checked in the inspector of the gamecontroller, show the cut planes as boxes
                AddCutPlane(point,normal);//this is for adding the plane which cut the mesh
            }
            
        }
        public void SliceByAllAxis(int n,string[] axises){
            while (GameObject.Find("plane")!=null){
                GameObject gameObject=GameObject.Find("plane");
                Destroy(gameObject,0);
            }
            if (n<2){
                //Debug.Log("n smaller than 2");
                return;
            }
            

            List<GameObject> currentlist = gamecontroller.Instance.currentlist;
            //Debug.Log(selectedObject.transform.childCount);
            //currentlist.Add(selectedObject.transform.GetChild(selectedObject.transform.childCount-1).gameObject);//
            int index = -1;
            for (int i = 0; i < currentlist.Count; i++)
            {
                if(currentlist[i] != null){
                    index=i;
                    break;
                }
            }
            if(index==-1) {
                throw new Exception("no gameobject found in currentlist");
            }
           
            //Debug.Log("onCut"+"min:"+minx+"max:"+maxx);
            
            for(int i=0;i<axises.Length;i++){//it will cut in i+1 dimensions, since there are i+1 cutting axises objects.

            Transform axis = selectedObject.transform.Find("axises").Find(axises[i]);
            Transform axis_point_start = axis.transform.GetChild(0);
            Transform axis_point_end = axis.transform.GetChild(1);
            Vector3 differenceVector = axis_point_end.position - axis_point_start.position;
            Vector3 deltaV3=differenceVector/n;
                for (int j = 0; j < n-1; j++)
                {       
                    Vector3 point=axis_point_start.position+deltaV3*(j+1);
                    Vector3 normal=deltaV3;
                    float size=1f;
                    if(gamecontroller.Instance.addPlane){addPlane(point,normal);}//if addplane box is checked in the inspector of the gamecontroller, show the cut planes as boxes
                    AddCutPlane(point,normal);//this is for adding the plane which cut the mesh
                }
            }
            
            
        }
        Vector3 RotateVectorAroundAxis(Vector3 vector, Vector3 axis, float angle)
        {
            // Normalize the axis to ensure proper rotation
            axis.Normalize();

            // Create a quaternion representing the rotation
            Quaternion rotation = Quaternion.AngleAxis(angle, axis);

            // Rotate the vector
            return rotation * vector;
        }
        public void circularSlice(int n){
            Transform y_axis = selectedObject.transform.Find("axises").Find("y");
            Transform z_axis = selectedObject.transform.Find("axises").Find("z");
            Vector3 yVector=y_axis.GetChild(0).position-y_axis.GetChild(1).position;
            Vector3 zVector=z_axis.GetChild(0).position-z_axis.GetChild(1).position;
            Vector3 cutFaceNormal=Vector3.Cross(yVector, zVector);
            Vector3 centerPoint=(y_axis.GetChild(1).position-y_axis.GetChild(0).position)/2+y_axis.GetChild(0).position;
            for(int i =0;i<n;i++){
                AddCutPlane(centerPoint,RotateVectorAroundAxis(cutFaceNormal,yVector,180f/n*i));
            }
        }
        
        private void Cut()
        {
            Plane plane = new Plane(_from, _to, Camera.main.transform.position);

            List<GameObject> currentlist = gamecontroller.Instance.currentlist;
            foreach (GameObject root in currentlist)//
            {
                //Debug.Log("tag:"+root.tag+root);
                if(root==null)
                    continue;
                if (!root.activeInHierarchy)
                    continue;
                var targets = root.GetComponentsInChildren<MeshTarget>();
                foreach (var target in targets)
                { 
                    Cut(target, _to, plane.normal, null,onCreated);
                }
                
            }
        }

            
        private void VisualizeLine(bool value)
        {
            if (LR == null)
                return;

            LR.enabled = value;

            if (value)
            {
                LR.positionCount = 2;
                LR.SetPosition(0, _from);
                LR.SetPosition(1, _to);
            }
        }

    }
}
