using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlayerController : MonoBehaviour
    {
        public Rigidbody rig;
        public Transform currentPlanet;
        public bool freezeRotation = true;

        [SerializeField] private float moveSpeed = 3f;

        [Tooltip("Koliko brzo brzina teži cilju na ledu (veće = manje sklisko).")]
        [SerializeField] private float iceAcceleration = 10f;
        [Tooltip("Koliko brzo brzina pada na nulu na ledu kad nema inputa (manje = duže klizanje).")]
        [SerializeField] private float iceDeceleration = 1.5f;

        private Planet _planet;
        private float _defaultLinearDamping;

        PlayerInputActions _input;

        void Awake()
        {
            _input = new PlayerInputActions();
        }

        void OnEnable()  => _input?.Enable();
        void OnDisable() => _input?.Disable();

        public void SetInputEnabled(bool enabled)
        {
            if (enabled) _input.Enable();
            else _input.Disable();
        }

        void Start()
        {
            Ini();
            if (currentPlanet != null)
                _planet = currentPlanet.GetComponent<Planet>();
        }

        public void SetPlanet(Transform planet)
        {
            currentPlanet = planet;
            _planet = planet != null ? planet.GetComponent<Planet>() : null;
        }

        void FixedUpdate()
        {
            ApplyGravity();
            Move();
        }

        private void ApplyGravity()
        {
            if (_planet == null) return;
            Vector3 down = (currentPlanet.position - transform.position).normalized;
            rig.AddForce(down * _planet.Gravity, ForceMode.Acceleration);
        }

        private void Ini()
        {
            rig.useGravity = false;
            rig.interpolation = RigidbodyInterpolation.Interpolate;
            _defaultLinearDamping = rig.linearDamping;
            if (freezeRotation)
                rig.constraints = RigidbodyConstraints.FreezeRotation;
            else
                rig.constraints = RigidbodyConstraints.None;
        }

        private void Move()
        {
            Vector2 input = _input.PlayerActionmap.Movement.ReadValue<Vector2>();
            bool onIce = _planet != null && _planet.Type == PlanetType.Ice;

            // rig.linearDamping (10 po defaultu) je namješten za MovePosition kretanje koje ga
            // ignorira. Na ledu kretanje ide kroz linearVelocity pa taj damping guši svako
            // ubrzanje skoro do nule (izgleda kao da se igrač ne može kretati) - zato ga na ledu
            // isključimo i sami kontroliramo usporavanje kroz iceDeceleration.
            rig.linearDamping = onIce ? 0f : _defaultLinearDamping;

            if (onIce)
            {
                MoveOnIce(input);
                return;
            }

            if (input == Vector2.zero)
            {
                // Zadrži samo vertikalnu brzinu (gravitacija), makni horizontal sliding
                rig.linearVelocity = Vector3.Project(rig.linearVelocity, transform.up);
                return;
            }

            Vector3 move = (transform.right * input.x + transform.forward * input.y) * (Time.fixedDeltaTime * moveSpeed);
            rig.MovePosition(rig.position + move);
        }

        private void MoveOnIce(Vector2 input)
        {
            // Umjesto direktnog namještanja pozicije, brzina teži cilju s ograničenim ubrzanjem
            // pa igrač klizi po ledu - ne staje odmah i ne skreće trenutno.
            Vector3 verticalVelocity = Vector3.Project(rig.linearVelocity, transform.up);
            Vector3 horizontalVelocity = rig.linearVelocity - verticalVelocity;

            Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
            float rate = input == Vector2.zero ? iceDeceleration : iceAcceleration;

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.fixedDeltaTime);
            rig.linearVelocity = horizontalVelocity + verticalVelocity;
        }
    }
}
