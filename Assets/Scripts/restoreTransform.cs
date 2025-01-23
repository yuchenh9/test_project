using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicMeshCutter
{
    public class restoreTransform : MonoBehaviour
    {// Start is called before the first frame update
        private Vector3 savedPosition;
        private Quaternion savedRotation;
        private Vector3 savedScale;
                                            
        // Call this method to save the current transform state
        public void SaveTransform()
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
            savedScale = transform.localScale;
        }

        // Call this method to restore the saved transform state
        public void Restore()
        {
            transform.position = savedPosition;
            transform.rotation = savedRotation;
            transform.localScale = savedScale;
        }
    }
}
