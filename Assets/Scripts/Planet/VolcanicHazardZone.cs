using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [RequireComponent(typeof(Collider))]
    public class VolcanicHazardZone : MonoBehaviour
    {
        private const float TickInterval = 1f;

        // Zajednički tick za SVE zone (igra je single-player): u preklopu više zona
        // šteta ide samo iz one koja prva odradi tick — svaka zona s vlastitim
        // timerom je u preklopu duplirala štetu.
        private static float _nextTickTime;
        private static float _lastContactTime;

        [Tooltip("Šteta po sekundi dok se igrač nalazi unutar zone.")]
        [SerializeField] private float damagePerSecond = 15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _nextTickTime = 0f;
            _lastContactTime = float.NegativeInfinity;
        }

        public void Init(float dps) => damagePerSecond = dps;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (other.attachedRigidbody == null) return;
            if (!other.attachedRigidbody.TryGetComponent(out PlayerHealth health)) return;

            // Igrač je bio izvan svih zona — grace period od punog intervala kreće ispočetka.
            if (Time.time - _lastContactTime > TickInterval)
                _nextTickTime = Time.time + TickInterval;
            _lastContactTime = Time.time;

            if (Time.time < _nextTickTime) return;
            _nextTickTime = Time.time + TickInterval;

            health.TakeDamage(damagePerSecond * TickInterval);
        }
    }
}
