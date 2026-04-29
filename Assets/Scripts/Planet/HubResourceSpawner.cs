using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class HubResourceSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class ResourceConfig
        {
            public Item item;
            public int count = 3;
            [Tooltip("Fallback boja ako item nema prefab")]
            public Color color = Color.gray;
        }

        [SerializeField] private List<ResourceConfig> resources = new();
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
            foreach (var cfg in resources)
            {
                if (cfg.item == null) continue;
                for (int i = 0; i < cfg.count; i++)
                    SpawnOne(cfg, hub);
            }
        }

        private void SpawnOne(ResourceConfig cfg, Transform hub)
        {
            Vector3 normal = Random.onUnitSphere;
            // Raycast od van prema centru planete da nađemo točnu površinu
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
                // Fallback ako raycast ne pogodi
                spawnPos = hub.position + normal * (hub.localScale.x * 0.5f + surfaceOffset);
                spawnRot = Quaternion.FromToRotation(Vector3.up, normal);
            }

            // Spawn u world spaceu (bez parenta) da se izbjegnu scale problemi
            GameObject go = cfg.item.prefab != null
                ? Instantiate(cfg.item.prefab, spawnPos, spawnRot)
                : CreateFallbackCube(spawnPos, spawnRot, cfg.color);

            go.name = cfg.item.displayName;
            go.transform.localScale = cfg.item.worldScale;

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (!go.TryGetComponent<ItemInteractable>(out var interactable))
                interactable = go.AddComponent<ItemInteractable>();

            interactable.Init(cfg.item);
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
