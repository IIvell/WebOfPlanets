using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [RequireComponent(typeof(Rigidbody))]
    public class Planet : MonoBehaviour
    {
        public PlanetType Type;
        public bool IsHub;

        void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        void Start()
        {
            GameEventBus.RaisePlanetDiscovered(transform);
        }
    }
}
