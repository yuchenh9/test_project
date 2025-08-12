using System.Collections.Generic;
using UnityEngine;

namespace DynamicMeshCutter
{
    /*
    Scene_data
        CutterBehaviour
            ->Update()//Cut is called as an IEnumerator, but updata() is a main function of the parent class, 
                        so updata() is not called by the tree but called by the system itself
                ->Info invoke onCut
                    CustomSlicerBehaviour
                        ->MakeNextCut()
                            ->CalculatedCut()
                        
                ->CreateGameObjects()
                    ->Info invoke onCreated
                    MeshCreation
                        ->CreateObjects()//was not called
        SliceManager
            ->IEnumerator Slice()//onclick()
                *CustomSlicerBehaviour
                    onCut()
                    onCreated()
                    ->IEnumerator Cut(
                        -> new SliceInfo
                        ->CalculatedCut() (not a)recursion
                            LinearSliceTypeCalculatorStrategy
                                ->Calculate(SliceInfo)
                            CutterBehaviour
                                ->Cut(//takes the data of a single plane, world position and world normal
                                    ->DrawPlane()
                                    ->new Info(onCut,onCreated)
                                    ->OnCut()
                                        add Info //to be poped by the update()
                                
                    ObiSoftbodySliceModifierStrategy 
                        ->IEnumerator Modify()// 
    */
    /* 
        Use the following to delegates to create callback functions that can be passed into the 
        public void Cut(MeshTarget target, Vector3 worldPosition, Vector3 worldNormal, OnCut onCut = null, OnCreated onCreated = null, object boxedUserData = null)
        function of this script.

        "OnCut" will be invoked immediately before the cutting algorithm has finished, but before any new meshes have been created. You can inspect the details of the cut inside the
        Info class.

        "OnCreated" is invoked in the virtual function "CreateGameObjects" found below. Since it is invoked after the mesh creation, it carries the data of the newle created
        GameObjects inside "MeshCreationData" as well as the "Info" that the "OnCut" callback has access to.

        For most cases you likely want to add your callbacks to the latter, for example if you want to add sound effects or particle effects to the cut.
    */
    public delegate void OnCut(bool success, Info info);
    public delegate void OnCreated(Info info, MeshCreationData creationData);

    /*
        Class that gets populated during the cut and returned in the "OnCut" and "OnCreated" callbacks after the cut has finished.
    */
    public class Info
    {
        //basic info
        public MeshTarget MeshTarget;
        public VirtualPlane Plane;

        //advanced info
        public Mesh TargetOriginalMesh;
        public VirtualMesh TargetVirtualMesh;
        public Matrix4x4[] Bindposes;
        public Info(MeshTarget target, VirtualPlane plane, OnCut onCut, OnCreated onCreated, object boxed)
        {
            this.MeshTarget = target;
            this.Plane = plane;
            OnCutCallback = onCut;
            OnCreatedCallback = onCreated;
            BoxedUserData = boxed;

            MeshCreation.GetMeshInfo(target, out TargetOriginalMesh, out Bindposes);
            TargetVirtualMesh = new VirtualMesh(TargetOriginalMesh);
            //Debug.Log($"[Cutter.Info] VirtualMesh constructed from '{TargetOriginalMesh.name}' vertices={TargetVirtualMesh.Vertices.Length} colorsLen={(TargetVirtualMesh.Colors!=null?TargetVirtualMesh.Colors.Length:0)}");

            if (target.DynamicRagdoll != null) //dynamic ragdoll could be missing
            {
                TargetVirtualMesh.AssignRagdoll(target.DynamicRagdoll);
            }
        }

        //info created during cutting tasks
        public VirtualMesh[] CreatedMeshes;
        public int[] Sides;
        public int[] BT; //buttom (0) or top (1)
        public List<Vector3> LocalFaceCenters = new List<Vector3>();
        public int CutSurfaceSubdivisionLevel = 4;
        //callbacks
        public OnCut OnCutCallback;
        public OnCreated OnCreatedCallback;
        public object BoxedUserData;
        public List<Vector3> GetWorldFaceCenters()
        {
            var worldCenters = new List<Vector3>();
            for (int i = 0; i < LocalFaceCenters.Count; i++)
            {
                worldCenters.Add(MeshTarget.transform.TransformPoint(LocalFaceCenters[i]));
            }
            return worldCenters;
        }
    }

    /*
        This is the entry class to the algorithm. You need one CutterBehaviour in your scene and invoke its "Cut" function to start the algorithm.
    */

    public abstract class CutterBehaviour : MonoBehaviour
    {
        public float Separation = 0.1f;
        [Tooltip("Automatically destroy the original object that is cut, when cut")]
        public bool DestroyTargets = true;
        [Tooltip("Use multiple threads to cut. Drastically reduces lag. Recommend ON")]
        public bool UseAsync = false;
        [Tooltip("Cut objects whose vertices are LESS than this will NOT be created")]
        public int VertexCreationThreshold = 0;
        [Tooltip("Subdivision level for cut surfaces (1=original, 2=1+2*1=3 triangles, 3=1+2*2=5 triangles, 4=1+2*3=7 triangles)")]
        [Range(1, 4)]
        public int CutSurfaceSubdivisionLevel = 2;
        public Material DefaultMaterial;

        private bool _cutterIsEnabled;
        public bool CutterIsEnabled => _cutterIsEnabled;

        public static bool ApplicationHasQuit = true;

        private AsycWorker _asyncWorker; //does the async work
        public AsycWorker AsyncWorker
        {
            get
            {
                if (_asyncWorker == null)
                    InitializeWorker();
                return _asyncWorker;
            }
        }
        private List<Info> _successes = new List<Info>();
        private List<Info> _fails = new List<Info>();
        private Queue<Info> _qSuccesses = new Queue<Info>();
        private Queue<Info> _qFails = new Queue<Info>();

        private bool _isInitialized = false;

        void InitializeWorker()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            _asyncWorker = new AsycWorker(this);
            _asyncWorker.OnCut += OnCut1;
        }

        void Terminate()
        {
            _asyncWorker = null;
        }

        private void OnApplicationQuit()
        {
            ApplicationHasQuit = true;
        }

        private void Awake()
        {
            ApplicationHasQuit = false;
        }
        private void OnEnable()
        {
            _cutterIsEnabled = true;
        }
        private void OnDisable()
        {
            _cutterIsEnabled = false;
            Terminate();
        }
        protected virtual void Update()
        {
            if (_successes.Count != 0)
            {
                lock (_successes)
                {
                    for (int i = 0; i < _successes.Count; i++)
                    {
                        _qSuccesses.Enqueue(_successes[i]);
                    }
                    _successes.Clear();
                }
            }

            while (_qSuccesses.Count != 0)
            {
                Info info = _qSuccesses.Dequeue();
                info.OnCutCallback?.Invoke(true, info);
                CreateGameObjects(info);
            }

            if (_fails.Count != 0)
            {
                lock (_fails)
                {
                    for (int i = 0; i < _fails.Count; i++)
                    {
                        _qFails.Enqueue(_fails[i]);
                    }
                    _fails.Clear();
                }
            }

            while (_qFails.Count != 0)
            {
                Info info = _qFails.Dequeue();
                info.OnCutCallback?.Invoke(false, info);
            }
        }

        public void Cut(MeshTarget target, Vector3 worldPosition, Vector3 worldNormal, OnCut onCut = null, OnCreated onCreated = null, object boxedUserData = null)
        {
            //Debug.Log($"CutterBehaviour.Cut: Start, target={target}, worldPosition={worldPosition}, worldNormal={worldNormal}");
            if (!target.isActiveAndEnabled)
            {
                //Debug.Log("CutterBehaviour.Cut: target not active and enabled, returning");
                return;
            }
            //DebugPlaneDrawer.DrawPlane(worldPosition, worldNormal, 1f);
            Matrix4x4 worldToLocalMatrix = target.transform.worldToLocalMatrix;
            //Debug.Log($"CutterBehaviour.Cut: worldToLocalMatrix={worldToLocalMatrix}");

            if (target.RequireLocal)
            {
                Matrix4x4 scalingMatrix = Matrix4x4.Scale(target.transform.lossyScale);
                //Debug.Log($"CutterBehaviour.Cut: scalingMatrix={scalingMatrix}");
                worldToLocalMatrix = scalingMatrix * worldToLocalMatrix;
                //Debug.Log($"CutterBehaviour.Cut: updated worldToLocalMatrix={worldToLocalMatrix}");
            }

            //Get Local Position
            Vector4 worldP = new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 1f);
            //Debug.Log($"CutterBehaviour.Cut: worldP={worldP}");
            Vector4[] worldPColumn = new Vector4[4];
            Vector3 localP = worldToLocalMatrix * worldP;
            //Debug.Log($"CutterBehaviour.Cut: localP={localP}");

            //Get Local Normal
            Vector3 worldN = new Vector4(worldNormal.x, worldNormal.y, worldNormal.z, 1f);
            //Debug.Log($"CutterBehaviour.Cut: worldN={worldN}");
            Matrix4x4 worldToLocalMatrixNormal = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                var column = worldToLocalMatrix.GetColumn(i);
                if (i == 4)
                    column = new Vector4(0, 0, 0, 1f);
                worldToLocalMatrixNormal.SetColumn(i, column);
            }
            //Debug.Log($"CutterBehaviour.Cut: worldToLocalMatrixNormal before inverse={worldToLocalMatrixNormal}");
            worldToLocalMatrixNormal = worldToLocalMatrixNormal.inverse.transpose;
            //Debug.Log($"CutterBehaviour.Cut: worldToLocalMatrixNormal after inverse={worldToLocalMatrixNormal}");
            Vector3 localN = worldToLocalMatrixNormal * worldN;
            localN.Normalize();
            //Debug.Log($"CutterBehaviour.Cut: localN={localN}");

            VirtualPlane plane = new VirtualPlane(localP, localN, worldPosition, worldNormal);
            //Debug.Log($"CutterBehaviour.Cut: Created VirtualPlane {plane}");
            Info info = new Info(target, plane, onCut, onCreated, boxedUserData);
            info.CutSurfaceSubdivisionLevel = CutSurfaceSubdivisionLevel;
            Debug.Log($"[CutterBehaviour] Setting CutSurfaceSubdivisionLevel to {CutSurfaceSubdivisionLevel}");
            //Debug.Log($"CutterBehaviour.Cut: Created Info {info}");

            if (!UseAsync)
            {
                //Debug.Log("CutterBehaviour.Cut: UseAsync is false");
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                int amount = 0;

                MeshCutting meshcutting = new MeshCutting();
                //Debug.Log("CutterBehaviour.Cut: Created MeshCutting");
                VirtualMesh[] virtualMeshes = meshcutting.Cut(ref info);
                //Debug.Log($"CutterBehaviour.Cut: meshcutting.Cut returned {virtualMeshes}");
                info.CreatedMeshes = virtualMeshes;
                if (virtualMeshes == null)
                {
                    //Debug.Log("CutterBehaviour.Cut: virtualMeshes is null, calling OnCut1(false, info)");
                    OnCut1(false, info);
                }
                else
                {
                    //Debug.Log("CutterBehaviour.Cut: virtualMeshes is not null, calling OnCut1(true, info)");
                    OnCut1(true, info);
                    amount = virtualMeshes.Length;
                }

                watch.Stop();
                //Debug.Log($"Synchronus cut creating {amount} meshes took {watch.ElapsedMilliseconds} ms. Success ? {virtualMeshes != null}");
            }
            else
            {
                //Debug.Log("CutterBehaviour.Cut: UseAsync is true, enqueueing info");
                AsyncWorker.Enqeue(info);
            }
            //Debug.Log("CutterBehaviour.Cut: End");
        }

        protected virtual void CreateGameObjects(Info info)
        {
            //Debug.Log("CutterBehaviour.CreateGameObjects called");
            MeshCreationData creationInfo = MeshCreation.CreateObjects(info, DefaultMaterial, VertexCreationThreshold);
            //Debug.Log("creationInfo"+creationInfo);
            if (DestroyTargets)
            {
                if (info.MeshTarget)
                {
                    // info.MeshTarget.transform.position = new Vector3(0, -10000, 0);
                    // if (info.MeshTarget.GameobjectRoot != null)
                    //     Destroy(info.MeshTarget.GameobjectRoot, 0);
                    // else
                    //     Destroy(info.MeshTarget.gameObject, 0);
                    // Instead of destroying, just disable the object:
                    if (info.MeshTarget.GameobjectRoot != null)
                        info.MeshTarget.GameobjectRoot.SetActive(false);
                    else
                        info.MeshTarget.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < creationInfo.CreatedObjects.Length; i++)
            {
                if (creationInfo.CreatedObjects[i] == null)
                {
                    //Debug.Log("Dynamic Mesh Cutter: Cut supressed creation of object due to VertexCreationThreshold. Make sure you handle NullReferenceExceptions!");
                }
            }

            //Debug.Log("CutterBehaviour: About to invoke OnCreatedCallback");
            info.OnCreatedCallback?.Invoke(info, creationInfo);
        }

        private void OnCut1(bool success, Info info)
        {
            if (success)
            {
                lock (_successes)
                {
                    _successes.Add(info);
                }
            }
            else
            {
                lock (_fails)
                {
                    _fails.Add(info);
                }
            }
        }
    }
}