using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Planet : MonoBehaviour
{
    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }
}
