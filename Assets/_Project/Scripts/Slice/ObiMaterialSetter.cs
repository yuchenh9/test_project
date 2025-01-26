using Obi;
using UnityEngine;

namespace _Project.Scripts.Slice
{
    public class ObiMaterialSetter : MonoBehaviour
    {
        [SerializeField] private ObiCollisionMaterial collisionMaterial;

        private void Start()
        {
            if (TryGetComponent<ObiCloth>(out var obiCloth))
                obiCloth.collisionMaterial = collisionMaterial;
            else if (TryGetComponent<ObiSoftbody>(out var obiSoftbody))
                obiSoftbody.collisionMaterial = collisionMaterial;
        }
    }
}
