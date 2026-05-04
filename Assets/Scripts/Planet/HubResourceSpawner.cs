using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class HubResourceSpawner : MonoBehaviour
    {
        [SerializeField] private PlanetResourceSettings settings;
        [SerializeField] private float surfaceOffset = 1.5f;

        void Start()
        {
            Planet hub = FindHub();
            if (hub == null)
            {
                Debug.LogWarning("HubResourceSpawner: nije pronađen hub planet (IsHub = true).");
                return;
            }

            SpawnAll(hub.transform);
        }

        private Planet FindHub()
        {
            foreach (var p in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p;
            return null;
        }

        private void SpawnAll(Transform hub)
        {
            if (settings == null) return;

            if (!hub.TryGetComponent(out Planet planet)) return;

            PlanetResourceSettings.PlanetTypeConfig config = settings.GetConfig(planet.Type);
            if (config == null) return;

            float scale = hub.localScale.x;
            foreach (var entry in config.resources)
            {
                if (entry.item == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(Random.Range(entry.minDensity, entry.maxDensity) * scale));
                for (int i = 0; i < count; i++)
                    SpawnOne(entry, hub);
            }
        }

        private void SpawnOne(PlanetResourceSettings.ResourceEntry entry, Transform hub)
        {
            Vector3 normal = Random.onUnitSphere;
            float castStart = hub.localScale.x;
            Vector3 rayOrigin = hub.position + normal * castStart;

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (Physics.Raycast(rayOrigin, -normal, out RaycastHit hit, castStart * 2f))
            {
                spawnPos = hit.point + hit.normal * surfaceOffset;
                spawnRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                spawnPos = hub.position + normal * (hub.localScale.x * 0.5f + surfaceOffset);
                spawnRot = Quaternion.FromToRotation(Vector3.up, normal);
            }

            GameObject go = entry.item.prefab != null
                ? Instantiate(entry.item.prefab, spawnPos, spawnRot)
                : CreateFallbackCube(spawnPos, spawnRot, entry.fallbackColor);

            go.name = entry.item.displayName;
            go.transform.localScale = entry.item.worldScale;

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (!go.TryGetComponent<ItemInteractable>(out var interactable))
                interactable = go.AddComponent<ItemInteractable>();

            interactable.Init(entry.item);
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
