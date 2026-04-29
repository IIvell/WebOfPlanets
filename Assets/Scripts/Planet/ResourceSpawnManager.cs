using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ResourceSpawnManager : MonoBehaviour
    {
        [SerializeField] private PlanetResourceSettings settings;
        [SerializeField] private float surfaceOffset = 1.5f;

        void OnEnable()  => GameEventBus.OnPlanetDiscovered += OnPlanetDiscovered;
        void OnDisable() => GameEventBus.OnPlanetDiscovered -= OnPlanetDiscovered;

        private void OnPlanetDiscovered(Transform planetTransform)
        {
            if (settings == null) return;

            Planet planet = planetTransform.GetComponent<Planet>();
            if (planet == null) return;

            PlanetResourceSettings.PlanetTypeConfig config = settings.GetConfig(planet.Type);
            if (config == null) return;

            foreach (var entry in config.resources)
            {
                if (entry.item == null) continue;
                int count = Random.Range(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                    SpawnOne(entry, planetTransform);
            }
        }

        private void SpawnOne(PlanetResourceSettings.ResourceEntry entry, Transform planet)
        {
            Vector3 normal = Random.onUnitSphere;
            float castStart = planet.localScale.x;
            Vector3 rayOrigin = planet.position + normal * castStart;

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (Physics.Raycast(rayOrigin, -normal, out RaycastHit hit, castStart * 2f))
            {
                spawnPos = hit.point + hit.normal * surfaceOffset;
                spawnRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                spawnPos = planet.position + normal * (planet.localScale.x * 0.5f + surfaceOffset);
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
