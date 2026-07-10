using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [RequireComponent(typeof(Collider))]
    public class VolcanicHazardZone : MonoBehaviour
    {
        private const float TickInterval = 1f;

        [Tooltip("Šteta po sekundi dok se igrač nalazi unutar zone.")]
        [SerializeField] private float damagePerSecond = 15f;

        private float _tickTimer;

        public void Init(float dps) => damagePerSecond = dps;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (other.attachedRigidbody == null) return;
            if (!other.attachedRigidbody.TryGetComponent(out PlayerHealth health)) return;

            _tickTimer += Time.fixedDeltaTime;
            if (_tickTimer < TickInterval) return;

            health.TakeDamage(damagePerSecond * _tickTimer);
            _tickTimer = 0f;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.TryGetComponent(out PlayerHealth _))
                _tickTimer = 0f;
        }
    }
}
