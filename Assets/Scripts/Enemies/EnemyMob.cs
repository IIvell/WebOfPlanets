using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Neprijatelj vezan uz jedan planet: stoji na mjestu dok mu se igrač ne
    // približi unutar detekcijskog radijusa, tada ga prati KONSTANTNOM brzinom
    // (malo manjom od igračeve) po površini planeta. Šteta ide pri dodiru kroz
    // PlayerHealth, čiji invulnerability prozor određuje ritam udaraca.
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyMob : MonoBehaviour
    {
        [Tooltip("Brzina kretanja (m/s). Konstantna — bez ubrzanja i usporavanja; namjerno malo manja od igračeve (3).")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Tooltip("Udaljenost na kojoj mob primijeti igrača i krene u potjeru.")]
        [SerializeField] private float detectionRadius = 12f;
        [Tooltip("Udaljenost na kojoj mob odustane od potjere — veća od detekcije da potjera ne treperi na rubu radijusa.")]
        [SerializeField] private float loseRadius = 18f;
        [Tooltip("Šteta igraču po dodiru; učestalost ograničava PlayerHealth invulnerability.")]
        [SerializeField] private float contactDamage = 10f;
        [Tooltip("Koliko brzo se mob okreće prema smjeru kretanja.")]
        [SerializeField] private float turnSpeed = 8f;

        private Transform _planet;
        private Planet _planetComponent;
        private Rigidbody _rig;
        private PlayerController _player;
        private PlayerHealth _playerHealth;
        private bool _chasing;

        public void Init(Transform planet)
        {
            _planet = planet;
            _planetComponent = planet != null ? planet.GetComponent<Planet>() : null;
        }

        void Awake()
        {
            _rig = GetComponent<Rigidbody>();
            _rig.useGravity = false;
            _rig.interpolation = RigidbodyInterpolation.Interpolate;
            // Isti obrazac kao Attractor: fizika ne smije rušiti moba, orijentaciju
            // vodimo sami kroz MoveRotation.
            _rig.constraints = RigidbodyConstraints.FreezeRotation;
        }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null)
                _playerHealth = _player.GetComponent<PlayerHealth>();
        }

        void FixedUpdate()
        {
            if (_planet == null) return;

            Vector3 up = (transform.position - _planet.position).normalized;

            float gravity = _planetComponent != null ? _planetComponent.Gravity : 20f;
            _rig.AddForce(-up * gravity, ForceMode.Acceleration);

            UpdateChaseState();

            // Kao i igrač: horizontala se postavlja direktno svaki korak (konstantna
            // brzina, bez ubrzanja), vertikala (pad) se ne dira.
            Vector3 vertical = Vector3.Project(_rig.linearVelocity, up);
            Vector3 moveDir = Vector3.zero;
            if (_chasing)
                moveDir = Vector3.ProjectOnPlane(_player.transform.position - transform.position, up).normalized;

            _rig.linearVelocity = moveDir * moveSpeed + vertical;

            Orient(up, moveDir);
        }

        // Potjera kreće unutar detectionRadius, a prekida se tek na loseRadius,
        // smrću igrača ili kad igrač ode s ovog planeta.
        private void UpdateChaseState()
        {
            if (_player == null || (_playerHealth != null && _playerHealth.IsDead) || _player.currentPlanet != _planet)
            {
                _chasing = false;
                return;
            }

            float distance = Vector3.Distance(_player.transform.position, transform.position);
            if (_chasing)
                _chasing = distance <= loseRadius;
            else
                _chasing = distance <= detectionRadius;
        }

        private void Orient(Vector3 up, Vector3 moveDir)
        {
            // Alien model gleda u lokalni +z (za razliku od robota igrača koji
            // koristi -direction) — potvrđeno u igri: s minusom je hodao unatrag.
            Quaternion target = moveDir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(moveDir, up)
                : Quaternion.FromToRotation(transform.up, up) * transform.rotation;

            _rig.MoveRotation(Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.fixedDeltaTime));
        }

        void OnCollisionStay(Collision collision)
        {
            if (collision.rigidbody == null) return;
            if (!collision.rigidbody.TryGetComponent(out PlayerHealth health)) return;

            health.TakeDamage(contactDamage);
        }
    }
}
