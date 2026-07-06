using System.Collections;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class HubResourceSpawner : MonoBehaviour
    {
        [SerializeField] private PlanetResourceSettings settings;
        [Tooltip("Razmak dna resursa od površine planeta (može biti negativan za lagano ukopavanje).")]
        [SerializeField] private float surfaceOffset = 0.1f;

        private IEnumerator Start()
        {
            yield return null;

            Planet hub = FindHub();
            if (hub == null)
            {
                Debug.LogWarning("HubResourceSpawner: nije pronađen hub planet (IsHub = true).");
                yield break;
            }

            SpawnAll(hub.transform);
        }

        private Planet FindHub()
        {
            foreach (var p in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p;
            return null;
        }

        private float GetPlanetRadius(Transform hub)
        {
            Renderer rend = hub.GetComponentInChildren<Renderer>();
            return rend != null ? rend.bounds.size.x * 0.5f : hub.localScale.x * 0.5f;
        }

        private void SpawnAll(Transform hub)
        {
            if (settings == null) return;

            if (!hub.TryGetComponent(out Planet planet)) return;

            PlanetResourceSettings.PlanetTypeConfig config = settings.GetConfig(planet.Type);
            if (config == null) return;

            float radius = GetPlanetRadius(hub);
            foreach (var entry in config.resources)
            {
                if (entry.item == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(Random.Range(entry.minDensity, entry.maxDensity) * radius));
                for (int i = 0; i < count; i++)
                    SpawnOne(entry, hub);
            }
        }

        private void SpawnOne(PlanetResourceSettings.ResourceEntry entry, Transform hub)
        {
            Vector3 normal = Random.onUnitSphere;
            float radius = GetPlanetRadius(hub);
            Vector3 rayOrigin = hub.position + normal * radius;

            Vector3 hitPoint;
            Vector3 hitNormal;

            if (SurfacePlacement.TryRaycastSurface(hub, rayOrigin, -normal, radius * 2f, out RaycastHit hit))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
            }
            else
            {
                hitPoint = hub.position + normal * radius;
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
