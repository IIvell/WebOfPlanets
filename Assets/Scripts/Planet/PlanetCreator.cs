using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlanetCreator : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCamera playerCamera;

        [SerializeField] private float minScale = 500f;
        [SerializeField] private float maxScale = 1500f;
        [SerializeField] private float minGravity = 10f;
        [SerializeField] private float maxGravity = 40f;
        [SerializeField] private float spawnDistance = 4000f;

        private Transform _currentPlanet;

        void Start()
        {
            _currentPlanet = player.currentPlanet;
        }

        void Update()
        {
            if (Keyboard.current.pKey.wasPressedThisFrame)
                CreatePlanetAndTeleport();
        }

        private void CreatePlanetAndTeleport()
        {
            // Disable attractor on the planet player is leaving
            if (_currentPlanet != null)
            {
                if (_currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            float scale = Random.Range(minScale, maxScale);
            float gravity = Random.Range(minGravity, maxGravity);

            Vector3 origin = _currentPlanet != null ? _currentPlanet.position : Vector3.zero;
            Vector3 spawnDir = Random.onUnitSphere;
            Vector3 planetPos = origin + spawnDir * spawnDistance;

            GameObject planetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetGO.name = "GeneratedPlanet";
            planetGO.transform.position = planetPos;
            planetGO.transform.localScale = Vector3.one * scale;

            Rigidbody rb = planetGO.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            planetGO.AddComponent<Attractor>();

            Planet planet = planetGO.AddComponent<Planet>();
            planet.Gravity = gravity;

            float radius = scale * 0.5f;
            Vector3 surfaceNormal = (planetPos - origin).normalized;
            Vector3 playerPos = planetPos - surfaceNormal * (radius + 2f);

            Quaternion playerRot = Quaternion.FromToRotation(Vector3.up, -surfaceNormal);

            Rigidbody playerRb = player.rig;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = playerPos;
            playerRb.rotation = playerRot;

            _currentPlanet = planetGO.transform;
            player.SetPlanet(_currentPlanet);
            if (playerCamera != null) playerCamera.SetPlanet(_currentPlanet);

            Debug.Log($"Kreiran novi planet: scale={scale:F0}, gravity={gravity:F1}");
        }

        public void TeleportToPlanet(Transform targetPlanet)
        {
            if (_currentPlanet != null)
            {
                if (_currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            if (targetPlanet.TryGetComponent(out Attractor newAttractor))
                newAttractor.enabled = true;

            float radius = targetPlanet.localScale.x * 0.5f;
            Vector3 surfaceNormal = Random.onUnitSphere;
            Vector3 playerPos = targetPlanet.position + surfaceNormal * (radius + 2f);
            Quaternion playerRot = Quaternion.FromToRotation(Vector3.up, surfaceNormal);

            Rigidbody playerRb = player.rig;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = playerPos;
            playerRb.rotation = playerRot;

            _currentPlanet = targetPlanet;
            player.SetPlanet(_currentPlanet);
            if (playerCamera != null) playerCamera.SetPlanet(_currentPlanet);
        }
    }
}
