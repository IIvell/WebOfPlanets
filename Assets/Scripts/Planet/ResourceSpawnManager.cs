using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ResourceSpawnManager : MonoBehaviour
    {
        [SerializeField] private PlanetResourceSettings settings;
        [Tooltip("Razmak dna resursa od površine planeta (može biti negativan za lagano ukopavanje).")]
        [SerializeField] private float surfaceOffset = 0.1f;

        private readonly HashSet<Transform> _processed = new();

        void OnEnable()  => GameEventBus.OnPlanetDiscovered += OnPlanetDiscovered;
        void OnDisable() => GameEventBus.OnPlanetDiscovered -= OnPlanetDiscovered;

        private IEnumerator Start()
        {
            yield return null; // wait for PlanetCreator.Start() to finish spawning
            foreach (var planet in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                OnPlanetDiscovered(planet.transform);
        }

        private void OnPlanetDiscovered(Transform planetTransform)
        {
            if (_processed.Contains(planetTransform)) return;
            _processed.Add(planetTransform);

            if (settings == null) return;

            Planet planet = planetTransform.GetComponent<Planet>();
            if (planet == null || planet.IsHub) return;

            PlanetResourceSettings.PlanetTypeConfig config = settings.GetConfig(planet.Type);
            if (config == null) return;

            Renderer rend = planetTransform.GetComponentInChildren<Renderer>();
            float scale = rend != null ? rend.bounds.size.x : planetTransform.localScale.x;
            foreach (var entry in config.resources)
            {
                if (entry.item == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(Random.Range(entry.minDensity, entry.maxDensity) * scale));
                for (int i = 0; i < count; i++)
                    SpawnOne(entry, planetTransform);
            }
        }

        private void SpawnOne(PlanetResourceSettings.ResourceEntry entry, Transform planet)
        {
            Vector3 normal = Random.onUnitSphere;
            float castStart = planet.localScale.x;
            Vector3 rayOrigin = planet.position + normal * castStart;

            Vector3 hitPoint;
            Vector3 hitNormal;

            if (SurfacePlacement.TryRaycastSurface(planet, rayOrigin, -normal, castStart * 2f, out RaycastHit hit))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
            }
            else
            {
                hitPoint = planet.position + normal * (planet.localScale.x * 0.5f);
                hitNormal = normal;
            }

            Vector3 spawnPos = hitPoint + hitNormal * surfaceOffset;
            Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, hitNormal);

            bool isPickup = Random.value < entry.pickupChance;
            GameObject prefab = isPickup ? entry.item.pickupPrefab : entry.item.miningPrefab;

            GameObject go = prefab != null
                ? Instantiate(prefab, spawnPos, spawnRot)
                : CreateFallbackCube(spawnPos, spawnRot, entry.fallbackColor);

            go.name = entry.item.displayName;
            go.transform.localScale = isPickup ? entry.item.pickupWorldScale : entry.item.miningWorldScale;

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (!go.TryGetComponent<ItemInteractable>(out var interactable))
                interactable = go.AddComponent<ItemInteractable>();

            interactable.Init(entry.item, isPickup);
        }

        private GameObject CreateFallbackCube(Vector3 pos, Quaternion rot, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetPositionAndRotation(pos, rot);
            go.GetComponent<Renderer>().material.color = color;
            return go;
        }
    }
}
