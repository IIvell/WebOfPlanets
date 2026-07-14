using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlanetCreator : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCamera playerCamera;

        [SerializeField] private float minGravity = 10f;
        [SerializeField] private float maxGravity = 40f;

        private Transform _currentPlanet;

        [SerializeField] private int startingPlanets = 30;
        [SerializeField] private float minSpawnDistance = 1500f;
        [SerializeField] private float maxSpawnDistance = 5000f;
        [SerializeField] private float minPlanetSeparation = 200f;
        [SerializeField] private int maxPlacementAttempts = 30;

        [SerializeField] private Material iceMaterial;
        [SerializeField] private Material miningMaterial;
        [SerializeField] private Material volcanicMaterial;
        [SerializeField] private Material gaseousMaterial;
        [SerializeField] private Material abandonedMaterial;
        [SerializeField] private Material organicMaterial;

        private readonly System.Collections.Generic.List<Vector3> _spawnedPositions = new();

        private static readonly PlanetType[] AllTypes =
        {
            PlanetType.Mining, PlanetType.Organic, PlanetType.Ice, PlanetType.Volcanic, PlanetType.Gaseous,
            PlanetType.Abandoned
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
            if (Keyboard.current == null || !Keyboard.current.tKey.wasPressedThisFrame) return;
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
            else if (planet.Type == PlanetType.Mining && miningMaterial != null)
                planetGO.GetComponent<Renderer>().material = miningMaterial;
            else if (planet.Type == PlanetType.Volcanic && volcanicMaterial != null)
                planetGO.GetComponent<Renderer>().material = volcanicMaterial;
            else if (planet.Type == PlanetType.Gaseous && gaseousMaterial != null)
                planetGO.GetComponent<Renderer>().material = gaseousMaterial;
            else if (planet.Type == PlanetType.Abandoned)
            {
                if (abandonedMaterial != null)
                    planetGO.GetComponent<Renderer>().material = abandonedMaterial;
                else
                    // Fallback tint dok materijal nije dodijeljen u Inspectoru.
                    planetGO.GetComponent<Renderer>().material.color = new Color(0.35f, 0.32f, 0.30f);
            }
            else if (planet.Type == PlanetType.Organic)
            {
                if (organicMaterial != null)
                    planetGO.GetComponent<Renderer>().material = organicMaterial;
                else
                    // Fallback tint dok materijal nije dodijeljen u Inspectoru.
                    planetGO.GetComponent<Renderer>().material.color = new Color(0.30f, 0.55f, 0.25f);
            }

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

        public void TeleportToPlanet(Transform targetPlanet, Transform fromPlanet = null, Transform destinationMarker = null)
        {
            if (_currentPlanet != null)
            {
                if (_currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            if (targetPlanet.TryGetComponent(out Attractor newAttractor))
                newAttractor.enabled = true;

            Vector3 playerPos;
            Vector3 playerUp;

            if (destinationMarker != null)
            {
                // Sletimo blizu stvarne pozicije markera/totema (umjesto da ponovno
                // računamo površinu iz centra planeta, što na nesferičnim meshovima
                // zna promašiti stvarnu točku markera).
                Vector3 markerUp = destinationMarker.up;
                Vector3 tangent = Vector3.Cross(markerUp, Vector3.up);
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(markerUp, Vector3.right);
                tangent.Normalize();

                // Totemi veza su mali triggeri pa je 2 m dovoljno. Teleporter gate ima
                // veliki solid collider: sleti ispred njegovog otvora (duž forward osi),
                // tik uz rub collidera — izvan njega (inače fizika izbaci igrača), ali
                // ne dalje nego što je nužno.
                float lateral = 2f;
                if (destinationMarker.TryGetComponent(out Collider markerCollider) && !markerCollider.isTrigger)
                {
                    tangent = destinationMarker.forward;

                    float probe = markerCollider.bounds.extents.magnitude + 2f;
                    Ray edgeRay = new Ray(destinationMarker.position + markerUp * 1f + tangent * probe, -tangent);
                    lateral = markerCollider.Raycast(edgeRay, out RaycastHit edgeHit, probe)
                        ? (probe - edgeHit.distance) + 1.5f
                        : markerCollider.bounds.extents.magnitude + 1.5f;
                }

                Vector3 rayOrigin = destinationMarker.position + markerUp * 2f + tangent * lateral;
                if (Physics.Raycast(rayOrigin, -markerUp, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    playerPos = hit.point + hit.normal * 1f;
                    playerUp = hit.normal;
                }
                else
                {
                    playerPos = destinationMarker.position + tangent * lateral;
                    playerUp = markerUp;
                }
            }
            else
            {
                float radius = targetPlanet.localScale.x * 0.5f;

                Vector3 surfaceNormal = fromPlanet != null
                    ? (fromPlanet.position - targetPlanet.position).normalized
                    : Random.onUnitSphere;

                Vector3 tangent = Vector3.Cross(surfaceNormal, Vector3.up);
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(surfaceNormal, Vector3.right);
                tangent.Normalize();

                float lateralOffset = Mathf.Min(6f, radius * 0.5f);
                Vector3 aimDirection = (surfaceNormal * radius + tangent * lateralOffset).normalized;

                Vector3 rayOrigin = targetPlanet.position + aimDirection * (radius * 1.5f);
                if (Physics.Raycast(rayOrigin, -aimDirection, out RaycastHit hit, radius * 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    playerPos = hit.point + hit.normal * 1f;
                    playerUp = hit.normal;
                }
                else
                {
                    playerPos = targetPlanet.position + aimDirection * (radius + 1f);
                    playerUp = aimDirection;
                }
            }

            Quaternion playerRot = Quaternion.FromToRotation(Vector3.up, playerUp);

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
