using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public PlayerInputActions _inputActions;
    public MoveData moveData;
    CharacterController cc;

    Vector3 movementVector;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
    }

    private void Update()
    {
        Move(_inputActions.PlayerActionMap.Movement.ReadValue<Vector2>());
    }

    private void FixedUpdate()
    {
        cc.Move(movementVector * moveData.moveSpeed * Time.deltaTime);
    }

    void Move(Vector2 _input)
    {
        movementVector = transform.forward * _input.y + transform.right * _input.x;
    }
}

