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

    void LateUpdate()
    {
        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (Mathf.Abs(scroll) > 0.01f)
            height = Mathf.Clamp(height - Mathf.Sign(scroll) * scrollSpeed, minHeight, maxHeight);

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
}
