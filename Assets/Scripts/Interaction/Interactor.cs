using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private Transform interactorSource;
        [SerializeField] private float interactRange = 3f;

        public Transform InteractorSource => interactorSource;
        public float InteractRange => interactRange;

        private IInteractable _currentTarget;
        private float _holdTimer;

        void Start()
        {
            if (interactorSource == null)
                interactorSource = transform;
        }

        void Update()
        {
            var eKey = Keyboard.current.eKey;

            if (eKey.wasPressedThisFrame)
            {
                _currentTarget = FindClosest();
                _holdTimer = 0f;

                if (_currentTarget is BaseInteractable bi && !bi.CanInteract)
                {
                    Debug.Log("Potreban je specifičan alat za minanje ovog resursa.");
                    _currentTarget = null;
                    return;
                }

                if (_currentTarget != null && _currentTarget.HoldTime <= 0f)
                {
                    _currentTarget.Interact();
                    _currentTarget = null;
                    return;
                }
            }

            if (_currentTarget == null) return;

            if (!eKey.isPressed)
            {
                CancelMining();
                return;
            }

            if (_currentTarget is MonoBehaviour mb)
            {
                Vector3 closestPoint = mb.TryGetComponent<Collider>(out var col)
                    ? col.ClosestPoint(interactorSource.position)
                    : mb.transform.position;

                if (Vector3.Distance(interactorSource.position, closestPoint) > interactRange)
                {
                    CancelMining();
                    return;
                }
            }

            _holdTimer += Time.deltaTime * PlayerToolSystem.GetSpeedMultiplier();
            float progress = Mathf.Clamp01(_holdTimer / _currentTarget.HoldTime);
            GameEventBus.Raise(new MiningProgressEvent { Progress = progress, IsMining = true });

            if (_holdTimer >= _currentTarget.HoldTime)
            {
                _currentTarget.Interact();
                _currentTarget = null;
                _holdTimer = 0f;
                GameEventBus.Raise(new MiningProgressEvent { Progress = 0f, IsMining = false });
            }
        }

        private IInteractable FindClosest()
        {
            Collider[] nearby = Physics.OverlapSphere(
                interactorSource.position, 
                interactRange, 
                Physics.DefaultRaycastLayers, 
                QueryTriggerInteraction.Collide);
                
            IInteractable closest = null;
            float closestDist = Mathf.Infinity;

            foreach (var col in nearby)
            {
                if (col.TryGetComponent(out IInteractable interactable))
                {
                    float dist = Vector3.Distance(interactorSource.position, col.ClosestPoint(interactorSource.position));
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = interactable;
                    }
                }
            }
            return closest;
        }

        private void CancelMining()
        {
            GameEventBus.Raise(new MiningProgressEvent { Progress = 0f, IsMining = false });
            _currentTarget = null;
            _holdTimer = 0f;
        }
    }
}
