using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 8f;
    public float acceleration = 60f;
    public float jumpForce = 10f;
    public float gravity = 25f;

    [Header("Rotation")]
    public float alignSpeed = 10f;
    public float visualTurnSpeed = 15f;

    [Header("Input Smoothing")]
    public float inputSmoothing = 12f;

    [Header("References")]
    public Transform currentPlanet;
    public Transform playerVisual;
    public Transform cameraTransform;

    Rigidbody rb;
    PlayerInputActions input;
    Vector3 gravityUp;
    Vector2 smoothedInput;
    bool grounded;
    bool jumpQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        input = new PlayerInputActions();
        input.PlayerActionMap.Jump.performed += _ => { if (grounded) jumpQueued = true; };
    }

    void OnEnable()  => input.Enable();
    void OnDisable() => input.Disable();

    void FixedUpdate()
    {
        if (currentPlanet == null) return;

        UpdateGravityUp();
        ApplyGravity();
        AlignToSurface();
        Move();

        if (jumpQueued) DoJump();
    }

    void UpdateGravityUp()
    {
        gravityUp = (transform.position - currentPlanet.position).normalized;
    }

    void ApplyGravity()
    {
        rb.AddForce(-gravityUp * gravity, ForceMode.Acceleration);
    }

    void AlignToSurface()
    {
        Quaternion target = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, target, alignSpeed * Time.fixedDeltaTime));
    }

    void Move()
    {
        Vector2 raw = input.PlayerActionMap.Movement.ReadValue<Vector2>();
        smoothedInput = Vector2.Lerp(smoothedInput, raw, inputSmoothing * Time.fixedDeltaTime);

        Vector3 verticalVel   = Vector3.Project(rb.linearVelocity, gravityUp);
        Vector3 horizontalVel = rb.linearVelocity - verticalVel;

        // Kad je igrač na tlu, odbaci nakupljenu brzinu prema planetu kako bi se spriječilo glitchanje
        if (grounded && Vector3.Dot(verticalVel, gravityUp) < 0f)
            verticalVel = Vector3.zero;

        Vector3 targetHorizontal = Vector3.zero;
        if (smoothedInput.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 camRight   = cameraTransform != null ? cameraTransform.right   : transform.right;
            Vector3 forward    = Vector3.ProjectOnPlane(camForward, gravityUp).normalized;
            Vector3 right      = Vector3.ProjectOnPlane(camRight,   gravityUp).normalized;
            Vector3 moveDir    = (forward * smoothedInput.y + right * smoothedInput.x).normalized;

            targetHorizontal = moveDir * speed;

            Quaternion targetRot = Quaternion.LookRotation(moveDir, gravityUp);
            playerVisual.rotation = Quaternion.Slerp(
                playerVisual.rotation, targetRot, visualTurnSpeed * Time.fixedDeltaTime);
        }

        Vector3 newHorizontal = Vector3.MoveTowards(
            horizontalVel, targetHorizontal, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = verticalVel + newHorizontal;
    }

    void DoJump()
    {
        jumpQueued = false;
        grounded   = false;

        Vector3 vel = rb.linearVelocity;
        vel -= Vector3.Project(vel, gravityUp);
        rb.linearVelocity = vel;

        rb.AddForce(gravityUp * jumpForce, ForceMode.Impulse);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform != currentPlanet) return;
        grounded = true;
        GameEventBus.Raise(currentPlanet, landed: true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform != currentPlanet) return;
        grounded = false;
        GameEventBus.Raise(currentPlanet, landed: false);
    }
}
