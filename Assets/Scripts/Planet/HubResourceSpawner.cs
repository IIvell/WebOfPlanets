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

        private void SpawnAll(Transform hub)
        {
            if (settings == null) return;

            if (!hub.TryGetComponent(out Planet planet)) return;

            PlanetResourceSettings.PlanetTypeConfig config = settings.GetConfig(planet.Type);
            if (config == null) return;

            // Oko baze (računalo/skladište/totem) resursi se ne spawnaju — plato ostaje čist.
            bool hasBase = HubBase.TryGetArea(hub, out Vector3 baseCenter, out float baseRadius);
            float exclusionRadius = hasBase ? baseRadius + 4f : 0f;

            float radius = SurfacePlacement.GetPlanetRadius(hub);
            foreach (var entry in config.resources)
            {
                if (entry.item == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(Random.Range(entry.minDensity, entry.maxDensity) * radius));
                for (int i = 0; i < count; i++)
                    SpawnOne(entry, hub, baseCenter, exclusionRadius);
            }
        }

        private void SpawnOne(PlanetResourceSettings.ResourceEntry entry, Transform hub,
            Vector3 baseCenter, float exclusionRadius)
        {
            float radius = SurfacePlacement.GetPlanetRadius(hub);

            Vector3 hitPoint = default;
            Vector3 hitNormal = default;

            // Nasumične točke dok ne padne izvan baze; ako ni nakon svih pokušaja
            // nije (praktički nemoguće za bazu koja je mali dio kugle), preskoči spawn.
            bool found = false;
            for (int attempt = 0; attempt < 8 && !found; attempt++)
            {
                Vector3 normal = Random.onUnitSphere;
                Vector3 rayOrigin = hub.position + normal * radius;

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

                found = exclusionRadius <= 0f
                    || (hitPoint - baseCenter).sqrMagnitude >= exclusionRadius * exclusionRadius;
            }
            if (!found) return;

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

        // Kopija iz ResourceSpawnManager.SnapPivotToBase (privatna je ondje).
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
