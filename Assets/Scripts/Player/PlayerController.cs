using UnityEngine;
using System.Collections;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlayerController : MonoBehaviour
    {
        public Rigidbody rig;
        RaycastHit hit;
        public bool freezeRotation = true;

        public int forceConst = 4;
        private bool canJump;

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
        }

        void Update()
        {
            if (Physics.Raycast(transform.position, -transform.up, out hit))
            {
                Debug.DrawLine(transform.position, hit.point, Color.cyan);
            }
        }

        void FixedUpdate()
        {
            Move();
            Jump();
        }

        private void Ini()
        {
            rig.useGravity = false;
            if (freezeRotation)
                rig.constraints = RigidbodyConstraints.FreezeRotation;
            else
                rig.constraints = RigidbodyConstraints.None;
        }

        private void Jump()
        {
            if (canJump)
            {
                canJump = false;
                rig.AddRelativeForce(0, forceConst, 0, ForceMode.Impulse);
            }
        }

        private void Move()
        {
            Vector2 input = _input.PlayerActionmap.Movement.ReadValue<Vector2>();

            var x = input.x * Time.deltaTime * 3.0f;
            var z = input.y * Time.deltaTime * 3.0f;

            transform.Translate(x, 0, z);
        }
    }
}
