using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform player;
    public Transform planet;

    public float distance = 6f;
    public float height = 4f;
    public float smoothSpeed = 8f;

    void LateUpdate()
    {
        if (player == null || planet == null) return;

        Vector3 planetUp = (player.position - planet.position).normalized;

        Vector3 targetPos = player.position
            - player.forward * distance
            + planetUp * height;

        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        transform.LookAt(player.position + planetUp * 1.5f, planetUp);
    }
}
