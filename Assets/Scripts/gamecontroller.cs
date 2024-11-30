using System.Collections.Generic;
using UnityEngine;

namespace DynamicMeshCutter
{
    //[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData", order = 1)]

    public class gamecontroller : MonoBehaviour
    {
        // Singleton instance
        public  static gamecontroller Instance { get; private set; }
        public GameObject selectedObject;
        public bool addPlane=false;
        public bool cutCompleteFlag=false;
        [SerializeField]
        public  List<GameObject> currentlist;
        private int sliceNumber=2;
        public string[] axises=new string[]{"x"};
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Destroy(gameObject);
            }
        }
        // Public property to access the sliceNumber
        public int SliceNumber
        {
            get { return sliceNumber; }
            set { sliceNumber = value; }
        }

        // Start is called before the first frame update
        void Start()
        {
            currentlist=new List<GameObject>();
        }
        public void clearCurrentList(){
            for (int i = currentlist.Count - 1; i >= 0; i--)
                {
                    // Destroy the GameObject
                    Destroy(currentlist[i]);
                }
        }
        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
