using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Planet : MonoBehaviour
{
    [SerializeField] private float gravitationalPull;

    public float GravitationalPull { get => gravitationalPull; }
}
 