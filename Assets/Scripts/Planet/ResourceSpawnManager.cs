using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ResourceSpawnManager : MonoBehaviour
    {
        [SerializeField] private PlanetResourceSettings settings;
        // Preimenovano sa surfaceOffset: namjerno odbacuje staru scene vrijednost (0.1)
        // koja je sve resurse držala 0.1 iznad tla. Dno se sada računa iz geometrije.
        [Tooltip("Dodatni razmak dna resursa od površine (0 = dno na tlu, negativno = ukopavanje).")]
        [SerializeField] private float surfaceGap = 0f;

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
            SurfacePlacement.GetSurfacePoint(planet, normal, out Vector3 hitPoint, out Vector3 hitNormal);

            // Markeri veza se spawnaju isti frame kao resursi (redoslijed Start
            // korutina nije definiran) — kad su markeri prvi, resurs bi se znao
            // stvoriti unutar totema, pa se smjer ponovno baca dok točka nije
            // slobodna od markera. Namjerno se NE provjeravaju drugi resursi da
            // se ne mijenja gustoća spawna; nakon 8 promašaja spawna se svejedno.
            for (int attempt = 0; attempt < 8 && IsNearConnectionMarker(hitPoint); attempt++)
            {
                normal = Random.onUnitSphere;
                SurfacePlacement.GetSurfacePoint(planet, normal, out hitPoint, out hitNormal);
            }

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
            SurfacePlacement.GroundToSurface(go, planet, hitPoint, hitNormal, surfaceGap);

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (!go.TryGetComponent<ItemInteractable>(out var interactable))
                interactable = go.AddComponent<ItemInteractable>();

            interactable.Init(entry.item, isPickup);
        }

        // Isti radijus kao MachinePlacer.IsSpotClear; cilja se samo na totem
        // markere veza (collider i interactable su im na root objektu —
        // FitColliderToRenderer briše child collidere).
        private static bool IsNearConnectionMarker(Vector3 pos)
        {
            foreach (var col in Physics.OverlapSphere(pos, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (col.TryGetComponent<ConnectionInteractable>(out _) ||
                    col.TryGetComponent<PotentialConnectionInteractable>(out _))
                    return true;
            return false;
        }
    }
}
