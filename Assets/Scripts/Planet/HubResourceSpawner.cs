using System.Collections;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class HubResourceSpawner : MonoBehaviour
    {
        [SerializeField] private PlanetResourceSettings settings;
        // Preimenovano sa surfaceOffset: namjerno odbacuje staru scene vrijednost (0.1)
        // koja je sve resurse držala 0.1 iznad tla. Dno se sada računa iz geometrije.
        [Tooltip("Dodatni razmak dna resursa od površine (0 = dno na tlu, negativno = ukopavanje).")]
        [SerializeField] private float surfaceGap = 0f;

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
            Vector3 hitPoint = default;
            Vector3 hitNormal = default;

            // Nasumične točke dok ne padne izvan baze; ako ni nakon svih pokušaja
            // nije (praktički nemoguće za bazu koja je mali dio kugle), preskoči spawn.
            bool found = false;
            for (int attempt = 0; attempt < 8 && !found; attempt++)
            {
                Vector3 normal = Random.onUnitSphere;
                SurfacePlacement.GetSurfacePoint(hub, normal, out hitPoint, out hitNormal);

                found = exclusionRadius <= 0f
                    || (hitPoint - baseCenter).sqrMagnitude >= exclusionRadius * exclusionRadius;
            }
            if (!found) return;

            Quaternion spawnRot = Quaternion.FromToRotation(entry.item.surfaceUpAxis, hitNormal);

            bool isPickup = Random.value < entry.pickupChance;
            GameObject prefab = isPickup ? entry.item.pickupPrefab : entry.item.miningPrefab;
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, hitPoint, spawnRot);

            go.name = entry.item.displayName;
            go.transform.localScale = isPickup ? entry.item.pickupWorldScale : entry.item.miningWorldScale;

            // Bezuvjetno prizemljenje po stvarnoj geometriji: prije se korigiralo samo
            // uz pivotAtMeshCenter flag, pa su modeli s drugačijim pivotom lebdjeli
            // ili upadali u planet.
            SurfacePlacement.GroundToSurface(go, hub, hitPoint, hitNormal, surfaceGap);

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (!go.TryGetComponent<ItemInteractable>(out var interactable))
                interactable = go.AddComponent<ItemInteractable>();

            interactable.Init(entry.item, isPickup);
        }
    }
}
