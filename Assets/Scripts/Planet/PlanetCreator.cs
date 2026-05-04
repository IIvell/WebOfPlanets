using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlanetCreator : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCamera playerCamera;

        [SerializeField] private float minScale = 800f;
        [SerializeField] private float maxScale = 1200f;
        [SerializeField] private float minGravity = 10f;
        [SerializeField] private float maxGravity = 40f;
        [SerializeField] private float spawnDistance = 1800f;

        private Transform _currentPlanet;
        private float _planetScale;

        void Start()
        {
            _currentPlanet = player.currentPlanet;

            if (_currentPlanet != null)
            {
                Renderer r = _currentPlanet.GetComponentInChildren<Renderer>();
                _planetScale = r != null ? r.bounds.size.x : _currentPlanet.lossyScale.x;
                Debug.Log($"PlanetCreator: planet skala = {_planetScale}");
            }
            else
            {
                _planetScale = 1000f;
                Debug.LogWarning("PlanetCreator: player.currentPlanet nije postavljen.");
            }
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

            float scale = Random.Range(35f, 100f);
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
            // Add planet types 
            PlanetType[] availableTypes = { PlanetType.Mining, PlanetType.Organic };
            planet.Type = availableTypes[Random.Range(0, availableTypes.Length)];

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
