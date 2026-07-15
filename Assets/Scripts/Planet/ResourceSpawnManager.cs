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

            float radius = SurfacePlacement.GetPlanetRadius(planetTransform);
            foreach (var entry in config.resources)
            {
                if (entry.item == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(Random.Range(entry.minDensity, entry.maxDensity) * radius));
                for (int i = 0; i < count; i++)
                    SpawnOne(entry, planetTransform);
            }
        }

        private void SpawnOne(PlanetResourceSettings.ResourceEntry entry, Transform planet)
        {
            Vector3 normal = Random.onUnitSphere;
            float radius = SurfacePlacement.GetPlanetRadius(planet);
            Vector3 rayOrigin = planet.position + normal * radius;

            Vector3 hitPoint;
            Vector3 hitNormal;

            if (SurfacePlacement.TryRaycastSurface(planet, rayOrigin, -normal, radius * 2f, out RaycastHit hit))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
            }
            else
            {
                hitPoint = planet.position + normal * radius;
                hitNormal = normal;
            }

            Vector3 spawnPos = hitPoint + hitNormal * surfaceOffset;
            Quaternion spawnRot = Quaternion.FromToRotation(entry.item.surfaceUpAxis, hitNormal);

            bool isPickup = Random.value < entry.pickupChance;
            GameObject prefab = isPickup ? entry.item.pickupPrefab : entry.item.miningPrefab;
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, spawnPos, spawnRot);

            go.name = entry.item.displayName;
            go.transform.localScale = isPickup ? entry.item.pickupWorldScale : entry.item.miningWorldScale;

            if (entry.item.pivotAtMeshCenter)
                SnapPivotToBase(go, hitNormal);

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (!go.TryGetComponent<ItemInteractable>(out var interactable))
                interactable = go.AddComponent<ItemInteractable>();

            interactable.Init(entry.item, isPickup);
        }

        // Samo za iteme s pivotAtMeshCenter = true. Pivot im je u sredini mesha umjesto na
        // dnu, pa bi inače pola objekta završilo ukopano u planet. Pomakni objekt van po
        // normali dovoljno da mu najniža stvarna točka mesha (ne AABB kutija, koja je
        // preširoka za nepravilne modele) dođe na razinu površine.
        private static void SnapPivotToBase(GameObject go, Vector3 hitNormal)
        {
            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
            if (filters.Length == 0) return;

            float lowestPointOffset;
            bool usedVertices = TryGetLowestVertexOffset(filters, go.transform.position, hitNormal, out lowestPointOffset);

            if (!usedVertices)
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                float centerOffset = Vector3.Dot(bounds.center - go.transform.position, hitNormal);
                float halfExtentAlongNormal = Mathf.Abs(bounds.extents.x * hitNormal.x)
                                             + Mathf.Abs(bounds.extents.y * hitNormal.y)
                                             + Mathf.Abs(bounds.extents.z * hitNormal.z);
                lowestPointOffset = centerOffset - halfExtentAlongNormal;
                Debug.LogWarning($"SnapPivotToBase: '{go.name}' mesh nije Read/Write Enabled, koristim manje precizan AABB fallback (uključi Read/Write Enabled u import postavkama za točan snap).");
            }

            go.transform.position -= hitNormal * lowestPointOffset;
        }

        // Prolazi kroz stvarne vrhove mesha (ne bounding box) i nalazi najnižu točku po
        // normali. Vraća false ako mesh nije Read/Write Enabled (vertices nisu dostupni).
        private static bool TryGetLowestVertexOffset(MeshFilter[] filters, Vector3 pivot, Vector3 hitNormal, out float lowestOffset)
        {
            lowestOffset = float.MaxValue;
            bool found = false;

            foreach (MeshFilter mf in filters)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;

                Vector3[] vertices = mesh.vertices;
                Transform meshTransform = mf.transform;
                foreach (Vector3 v in vertices)
                {
                    float projection = Vector3.Dot(meshTransform.TransformPoint(v) - pivot, hitNormal);
                    if (projection < lowestOffset) lowestOffset = projection;
                }
                found = true;
            }

            return found;
        }
    }
}
