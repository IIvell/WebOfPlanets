using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlayerController : MonoBehaviour
    {
        public Rigidbody rig;
        public Transform currentPlanet;
        public bool freezeRotation = true;

        [SerializeField] private float jumpForce = 20f;
        private Planet _planet;
        private bool canJump;
        private bool isGrounded;

        PlayerInputActions _input;

        void Awake()
        {
            _input = new PlayerInputActions();
            _input.PlayerActionmap.Jump.performed += _ => canJump = true;
        }

        void OnEnable()  => _input.Enable();
        void OnDisable() => _input.Disable();

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

        void OnCollisionStay(Collision col)  => isGrounded = true;
        void OnCollisionExit(Collision col)  => isGrounded = false;

        void FixedUpdate()
        {
            ApplyGravity();
            Move();
            Jump();
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

        private void Jump()
        {
            if (!canJump) return;
            canJump = false;
            if (!isGrounded) return;
            rig.AddRelativeForce(0, jumpForce, 0, ForceMode.Impulse);
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

            Vector3 move = (transform.right * input.x + transform.forward * input.y) * (Time.fixedDeltaTime * 3.0f);
            rig.MovePosition(rig.position + move);
        }
    }
}
