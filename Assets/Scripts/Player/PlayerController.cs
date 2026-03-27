using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float rayCastLength;
    public float rotationSpeed;
    private float tmpRotationSpeed;
    public float speed;
    public float gravity;
    private float tmpGravity;
    public float jumpForce;
    private Rigidbody rb;
    public Transform currentPlanet;
    public Transform playerVisual;

    RaycastHit[] hits;
    Vector3 planetDir;
    Vector3 normalVector;
    Vector3 input;

    public bool isTouchingPlanetSurface = false;
    private Transform MainCameraTransform;
    public Transform CameraArmTransform;

    PlayerInputActions playerInputActions;
  

    bool CanJump = true;
    bool slowDown = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        MainCameraTransform = Camera.main.transform;
        tmpGravity = gravity;
        tmpRotationSpeed = rotationSpeed;

        playerInputActions = new PlayerInputActions();
        playerInputActions.PlayerActionMap.Jump.performed += Jump;
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void FixedUpdate()
    {
        Movement();
        ApplyGravity();
        ApplyPlanetRotation();
    }

    void Jump(InputAction.CallbackContext context)
    {
        if (!CanJump) return;
        rb.linearVelocity *= 0;
        rb.AddForce(normalVector * jumpForce, ForceMode.Impulse);
        gravity = tmpGravity / 2f;
        Invoke(nameof(RestoreGravity), 1f);
        CanJump = false;
        rotationSpeed = tmpRotationSpeed / 2f;
    }

    void RestoreGravity()
    {
        gravity = tmpGravity;
        CanJump = true;
        slowDown = false;
    }

    public void EnterNewGravityField()
    {
        gravity = tmpGravity / 4f;
        rb.linearVelocity *= .5f;
        rotationSpeed = tmpRotationSpeed / 10f;
        slowDown = true;
        CanJump = false;
        Invoke(nameof(RestoreGravity), .5f);
    }

    void Movement()
    {
        input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 cameraRotation = new Vector3(0, MainCameraTransform.localEulerAngles.y + CameraArmTransform.localEulerAngles.y, 0);
        Vector3 Dir = Quaternion.Euler(cameraRotation) * input;
        Vector3 movement_dir = (transform.forward * Dir.z + transform.right * Dir.x);
        Vector3 currentNormalVelocity = Vector3.Project(rb.linearVelocity, normalVector.normalized);
        rb.linearVelocity = currentNormalVelocity + (movement_dir * speed);

        if (movement_dir != Vector3.zero)
        {
            playerVisual.localRotation = Quaternion.LookRotation(Dir);
        }

        if (slowDown)
            rb.linearVelocity *= .5f;
    }

    void ApplyGravity()
    {
        if (currentPlanet == null) return;

        hits = Physics.RaycastAll(transform.position, -transform.up, rayCastLength);

        if (hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, transform.forward, rayCastLength);
        }

        if (hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, -transform.forward, rayCastLength);
        }

        if (hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, transform.right, rayCastLength);
        }

        if (hits.Length == 0)
        {
            hits = Physics.RaycastAll(transform.position, -transform.right, rayCastLength);
        }

        if (hits.Length == 0)
        {
            planetDir = currentPlanet.position - transform.position;
            hits = Physics.RaycastAll(transform.position, planetDir, rayCastLength);
        }

        GetPlanetNormal();
        rb.AddForce(normalVector.normalized * gravity, ForceMode.Acceleration);
        hits = new RaycastHit[0];
    }

    void ApplyPlanetRotation()
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, normalVector) * transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        if (isTouchingPlanetSurface && CanJump)
            rotationSpeed = tmpRotationSpeed;
    }

    void GetPlanetNormal()
    {
        if (currentPlanet == null) return;
        normalVector = (transform.position - currentPlanet.position).normalized;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == currentPlanet)
            {
                normalVector = hits[i].normal.normalized;
                break;
            }
        }
        return;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == currentPlanet)
        {
            isTouchingPlanetSurface = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == currentPlanet)
        {
            isTouchingPlanetSurface = false;
        }
    }

}