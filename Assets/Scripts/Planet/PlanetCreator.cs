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

        [SerializeField] private int startingPlanets = 3;

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

            for (int i = 0; i < startingPlanets; i++)
                SpawnPlanet(_currentPlanet != null ? _currentPlanet.position : Vector3.zero);
        }

        void Update()
        {
            if (Keyboard.current.pKey.wasPressedThisFrame)
                CreatePlanetAndTeleport();
        }

        private Transform SpawnPlanet(Vector3 origin)
        {
            float scale = Random.Range(35f, 100f);
            float gravity = Random.Range(minGravity, maxGravity);

            Vector3 planetPos = origin + Random.onUnitSphere * spawnDistance;

            GameObject planetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetGO.name = "GeneratedPlanet";
            planetGO.transform.position = planetPos;
            planetGO.transform.localScale = Vector3.one * scale;

            Rigidbody rb = planetGO.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Attractor attractor = planetGO.AddComponent<Attractor>();
            attractor.enabled = false;

            Planet planet = planetGO.AddComponent<Planet>();
            planet.Gravity = gravity;
            PlanetType[] availableTypes = { PlanetType.Mining, PlanetType.Organic };
            planet.Type = availableTypes[Random.Range(0, availableTypes.Length)];

            Debug.Log($"Kreiran novi planet: scale={scale:F0}, gravity={gravity:F1}, type={planet.Type}");
            return planetGO.transform;
        }

        private void CreatePlanetAndTeleport()
        {
            if (_currentPlanet != null)
            {
                if (_currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            Vector3 origin = _currentPlanet != null ? _currentPlanet.position : Vector3.zero;
            Transform newPlanet = SpawnPlanet(origin);
            if (newPlanet.TryGetComponent(out Attractor newAttractor))
                newAttractor.enabled = true;

            float scale = newPlanet.localScale.x;
            float radius = scale * 0.5f;
            Vector3 surfaceNormal = (newPlanet.position - origin).normalized;
            Vector3 playerPos = newPlanet.position - surfaceNormal * (radius + 2f);
            Quaternion playerRot = Quaternion.FromToRotation(Vector3.up, -surfaceNormal);

            Rigidbody playerRb = player.rig;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = playerPos;
            playerRb.rotation = playerRot;

            _currentPlanet = newPlanet;
            player.SetPlanet(_currentPlanet);
            if (playerCamera != null) playerCamera.SetPlanet(_currentPlanet);
        }

        public void TeleportToPlanet(Transform targetPlanet, Transform fromPlanet = null)
        {
            if (_currentPlanet != null)
            {
                if (_currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            if (targetPlanet.TryGetComponent(out Attractor newAttractor))
                newAttractor.enabled = true;

            Renderer rend = targetPlanet.GetComponentInChildren<Renderer>();
            float radius = rend != null ? rend.bounds.size.x * 0.5f : targetPlanet.localScale.x * 0.5f;

            Vector3 surfaceNormal = fromPlanet != null
                ? (fromPlanet.position - targetPlanet.position).normalized
                : Random.onUnitSphere;

            Vector3 rayOrigin = targetPlanet.position + surfaceNormal * (radius * 1.5f);
            Vector3 playerPos;
            if (Physics.Raycast(rayOrigin, -surfaceNormal, out RaycastHit hit, radius * 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                playerPos = hit.point + hit.normal * 1f;
            else
                playerPos = targetPlanet.position + surfaceNormal * (radius + 1f);

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
