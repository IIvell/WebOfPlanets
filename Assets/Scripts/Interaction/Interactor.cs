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
            if (Keyboard.current == null) return;
            if (!GameManager.IsPlaying) return;

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
                    Vector3 closestPoint = col.ClosestPoint(interactorSource.position);
                    float dist = Vector3.Distance(interactorSource.position, closestPoint);
                    if (dist >= closestDist) continue;
                    if (!HasLineOfSight(col, closestPoint)) continue;

                    closestDist = dist;
                    closest = interactable;
                }
            }
            return closest;
        }

        // Bez ovoga OverlapSphere dopušta minanje/interakciju kroz prepreke
        // (kamenje, strojeve, pa i "iza horizonta" planeta).
        private bool HasLineOfSight(Collider target, Vector3 targetPoint)
        {
            Vector3 origin = interactorSource.position;
            Vector3 toTarget = targetPoint - origin;
            float dist = toTarget.magnitude;
            if (dist < 0.05f) return true;

            Vector3 dir = toTarget / dist;
            foreach (var hit in Physics.RaycastAll(origin, dir, dist - 0.02f,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == target) continue;
                if (hit.collider.transform.IsChildOf(target.transform)) continue; // dijelovi istog objekta
                if (hit.collider.transform.IsChildOf(transform.root)) continue;   // vlastiti collider igrača
                return false;
            }
            return true;
        }

        private void CancelMining()
        {
            GameEventBus.Raise(new MiningProgressEvent { Progress = 0f, IsMining = false });
            _currentTarget = null;
            _holdTimer = 0f;
        }
    }
}
