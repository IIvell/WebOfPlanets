using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlayerController : MonoBehaviour
    {
        public Rigidbody rig;
        public Transform currentPlanet;
        public bool freezeRotation = true;

        [SerializeField] private float moveSpeed = 3f;
        private Planet _planet;

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
            if (freezeRotation)
                rig.constraints = RigidbodyConstraints.FreezeRotation;
            else
                rig.constraints = RigidbodyConstraints.None;
        }

        private void Move()
        {
            Vector2 input = _input.PlayerActionmap.Movement.ReadValue<Vector2>();

            if (input == Vector2.zero)
            {
                // Zadrži samo vertikalnu brzinu (gravitacija), makni horizontal sliding
                rig.linearVelocity = Vector3.Project(rig.linearVelocity, transform.up);
                return;
            }

            Vector3 move = (transform.right * input.x + transform.forward * input.y) * (Time.fixedDeltaTime * moveSpeed);
            rig.MovePosition(rig.position + move);
        }
    }
}
