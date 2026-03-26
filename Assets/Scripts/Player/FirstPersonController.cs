using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : Movement
{
    CharacterController cc;

    private void OnEnable() 
    {
        cc = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        cc.Move((movementVector + gravity) * Time.fixedDeltaTime);
    }
}
 