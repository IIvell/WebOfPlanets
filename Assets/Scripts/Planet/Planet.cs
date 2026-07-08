using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [RequireComponent(typeof(Rigidbody))]
    public class Planet : MonoBehaviour
    {
        public PlanetType Type;
        public bool IsHub;
        public float Gravity = 20f;

        [SerializeField] private Material surfaceMaterial;

        void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            if (surfaceMaterial != null)
            {
                Renderer renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                    renderer.material = surfaceMaterial;
            }
        }

        void Start()
        {
            GameEventBus.RaisePlanetDiscovered(transform);
        }
    }
}
