using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public Transform player;
    public Transform planet;

    public float distance = 6f;
    public float height = 4f;
    public float smoothSpeed = 8f;

    public float minHeight = 4f;
    public float maxHeight = 20f;
    public float scrollSpeed = 1.5f;

    private bool _inputEnabled = true;
    private Vector3 _startForward;

    public void SetInputEnabled(bool enabled) => _inputEnabled = enabled;

    void Start()
    {
        if (player != null)
            _startForward = player.forward;
    }

    public void SetPlanet(Transform newPlanet)
    {
        planet = newPlanet;
        if (player == null || planet == null) return;

        Vector3 planetUp = (player.position - planet.position).normalized;
        transform.position = player.position - player.forward * distance + planetUp * height;
        transform.rotation = Quaternion.LookRotation(
            (player.position + planetUp * 1.5f) - transform.position,
            planetUp);
    }

    void LateUpdate()
    {
        if (_inputEnabled)
        {
            float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (Mathf.Abs(scroll) > 0.01f)
                height = Mathf.Clamp(height - Mathf.Sign(scroll) * scrollSpeed, minHeight, maxHeight);

            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
                ResetDirection();
        }

        if (player == null || planet == null) return;

        Vector3 planetUp = (player.position - planet.position).normalized;

        Vector3 targetPos = player.position
            - player.forward * distance
            + planetUp * height;

        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);

        Quaternion targetRot = Quaternion.LookRotation(
            (player.position + planetUp * 1.5f) - transform.position,
            planetUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }

    // Kamera nema vlastiti yaw — uvijek stoji iza player.forward, a taj se smjer
    // "zakreće" hodanjem po kugli. Reset zato okreće igrača: početni world-space
    // forward projicira se na trenutnu tangentnu ravninu planeta.
    private void ResetDirection()
    {
        if (player == null || planet == null) return;

        Vector3 planetUp = (player.position - planet.position).normalized;
        Vector3 dir = Vector3.ProjectOnPlane(_startForward, planetUp);
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, planetUp);
        if (player.TryGetComponent(out Rigidbody rb))
            rb.rotation = target;
        else
            player.rotation = target;
    }
}
