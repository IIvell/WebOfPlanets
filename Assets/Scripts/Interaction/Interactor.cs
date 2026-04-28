using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Transform interactorSource;
        [SerializeField] private float interactRange = 3f;

        void Start()
        {
            if (interactorSource == null)
                interactorSource = transform;
        }

        void Update()
        {
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            Collider[] nearby = Physics.OverlapSphere(interactorSource.position, interactRange);

            IInteractable closest = null;
            float closestDist = Mathf.Infinity;

            foreach (var col in nearby)
            {
                if (col.TryGetComponent(out IInteractable interactable))
                {
                    float dist = Vector3.Distance(interactorSource.position, col.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = interactable;
                    }
                }
            }

            if (closest != null)
                closest.Interact();
        }
    }
}
