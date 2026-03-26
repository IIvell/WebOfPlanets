using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] protected MoveData moveData;
    [SerializeField] protected PlayerInputActions _inputActions;

    public Transform planet;
    protected Vector3 movementVector;
     protected Vector3 gravityDirection;
    protected float gravityStrength = 1f;
    protected Vector3 jumpVector;
    protected Vector3 gravity;

    public float gravitySpeed;
    public float surfaceRotationSpeed = 2f;

    private void OnEnable()
    {
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
    }

    private void Update()
    {
        RotateToSurface();
        Fall();
        Move(_inputActions.PlayerActionMap.Movement.ReadValue<Vector2>());
    }

    void RotateToSurface()
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, -gravity) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, surfaceRotationSpeed * Time.deltaTime);
    }

    private void Fall()
    {
        gravity = (planet.position - transform.position).normalized * gravitySpeed;
    }

    void Move(Vector2 _input)
    {
        movementVector = (transform.forward * _input.y + transform.right * _input.x) * moveData.moveSpeed;
    }
}

