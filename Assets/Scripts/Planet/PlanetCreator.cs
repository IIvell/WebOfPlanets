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

        [SerializeField] private int startingPlanets = 30;
        [SerializeField] private float minSpawnDistance = 1500f;
        [SerializeField] private float maxSpawnDistance = 5000f;
        [SerializeField] private float minPlanetSeparation = 200f;
        [SerializeField] private int maxPlacementAttempts = 30;

        [SerializeField] private Material iceMaterial;

        private readonly System.Collections.Generic.List<Vector3> _spawnedPositions = new();

        private static readonly PlanetType[] AllTypes =
        {
            PlanetType.Mining, PlanetType.Organic, PlanetType.Ice
        };

        void Start()
        {
            _currentPlanet = player.currentPlanet;

            if (_currentPlanet == null)
                Debug.LogWarning("PlanetCreator: player.currentPlanet nije postavljen.");

            Vector3 origin = _currentPlanet != null ? _currentPlanet.position : Vector3.zero;
            if (_currentPlanet != null)
                _spawnedPositions.Add(origin);

            for (int i = 0; i < startingPlanets; i++)
                SpawnPlanet(origin, i);
        }

        void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
                CreatePlanetAndTeleport();
        }

        private Transform SpawnPlanet(Vector3 origin, int index = -1)
        {
            float scale   = Random.Range(35f, 100f);
            float gravity = Random.Range(minGravity, maxGravity);
            float minSep  = minPlanetSeparation + scale;

            Vector3 planetPos = FindOpenPosition(origin, minSep);
            _spawnedPositions.Add(planetPos);

            GameObject planetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetGO.name = index >= 0 ? $"Planet_{index:D2}" : "GeneratedPlanet";
            planetGO.layer = LayerMask.NameToLayer("Planet");
            planetGO.transform.position = planetPos;
            planetGO.transform.localScale = Vector3.one * scale;

            Rigidbody rb = planetGO.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Attractor attractor = planetGO.AddComponent<Attractor>();
            attractor.OrientToGravity = false;
            attractor.enabled = false;

            Planet planet = planetGO.AddComponent<Planet>();
            planet.Gravity = gravity;
            planet.Type = AllTypes[Random.Range(0, AllTypes.Length)];

            if (planet.Type == PlanetType.Ice && iceMaterial != null)
                planetGO.GetComponent<Renderer>().material = iceMaterial;

            return planetGO.transform;
        }

        private Vector3 FindOpenPosition(Vector3 origin, float minSep)
        {
            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector3 candidate = origin + Random.onUnitSphere * dist;

                bool tooClose = false;
                foreach (Vector3 existing in _spawnedPositions)
                {
                    if (Vector3.Distance(candidate, existing) < minSep)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) return candidate;
            }

            // All attempts failed — just place at a random far distance
            return origin + Random.onUnitSphere * maxSpawnDistance;
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
