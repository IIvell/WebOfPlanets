using UnityEngine;

/// Postavi ovaj script na Camera objekt.
/// Povuci Player i Planet transform u Inspector.
[RequireComponent(typeof(Camera))]
public class PlanetCameraController : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform planetCenter;

    [Header("Orbit")]
    public float distance = 5f;
    public float heightOffset = 20f;

    [Header("Smoothing")]
    public float followSmoothTime  = 0.25f;
    public float rotationSmoothing = 10f;

    static Vector3 s_CameraForward;
    static Vector3 s_CameraRight;

    public static Vector3 GetForward(Vector3 gravityUp)
        => Vector3.ProjectOnPlane(s_CameraForward, gravityUp).normalized;

    public static Vector3 GetRight(Vector3 gravityUp)
        => Vector3.ProjectOnPlane(s_CameraRight, gravityUp).normalized;

    Vector3 camBack;
    Vector3 posVelocity;
    bool initialized;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (player == null || planetCenter == null) return;

        Vector3 gravityUp = (player.position - planetCenter.position).normalized;

        if (!initialized)
        {
            camBack = -player.forward;
            camBack = Vector3.ProjectOnPlane(camBack, gravityUp).normalized;
            initialized = true;
        }

        camBack = Vector3.ProjectOnPlane(camBack, gravityUp).normalized;

        Vector3 targetPos = player.position
            + camBack * distance
            + gravityUp * heightOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref posVelocity, followSmoothTime);

        Vector3 lookTarget = player.position + gravityUp * 0.5f;
        Vector3 lookDir = lookTarget - transform.position;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir, gravityUp);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSmoothing * Time.deltaTime);
        }

        s_CameraForward = -camBack;
        s_CameraRight   = Vector3.Cross(gravityUp, s_CameraForward).normalized;
    }
}
