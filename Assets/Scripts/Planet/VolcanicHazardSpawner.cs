using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class VolcanicHazardSpawner : MonoBehaviour
    {
        [SerializeField] private int minZonesPerPlanet = 2;
        [SerializeField] private int maxZonesPerPlanet = 5;
        [SerializeField] private float minZoneRadius = 4f;
        [SerializeField] private float maxZoneRadius = 10f;
        [SerializeField] private float damagePerSecond = 15f;
        [Tooltip("Razmak zone od površine planeta (negativno = lagano ukopana lava).")]
        [SerializeField] private float surfaceOffset = -1f;
        [SerializeField] private Material hazardMaterial;

        [Header("Kretanje zona")]
        [Tooltip("Najmanja brzina kretanja zone po površini (m/s).")]
        [SerializeField] private float minMoveSpeed = 2f;
        [Tooltip("Najveća brzina kretanja zone po površini (m/s).")]
        [SerializeField] private float maxMoveSpeed = 5f;

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

            Planet planet = planetTransform.GetComponent<Planet>();
            if (planet == null || planet.IsHub || planet.Type != PlanetType.Volcanic) return;

            int count = Random.Range(minZonesPerPlanet, maxZonesPerPlanet + 1);
            for (int i = 0; i < count; i++)
                SpawnZone(planetTransform);
        }

        private void SpawnZone(Transform planet)
        {
            // Zajednički helper umjesto lokalnog raycasta: zone se spawnaju isti
            // frame kad i planet, pa treba SyncTransforms retry; lokalni fallback na
            // localScale*0.5 bi zonu ostavio na analitičkoj kugli iznad vidljivog mesha.
            Vector3 normal = Random.onUnitSphere;
            SurfacePlacement.GetSurfacePoint(planet, normal, out Vector3 hitPoint, out Vector3 hitNormal);

            float radius = Random.Range(minZoneRadius, maxZoneRadius);
            Vector3 spawnPos = hitPoint + hitNormal * surfaceOffset;

            GameObject zoneGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            zoneGO.name = "VolcanicHazardZone";
            zoneGO.transform.position = spawnPos;
            zoneGO.transform.localScale = Vector3.one * (radius * 2f);

            SphereCollider col = zoneGO.GetComponent<SphereCollider>();
            col.isTrigger = true;

            Renderer rend = zoneGO.GetComponent<Renderer>();
            if (hazardMaterial != null)
                rend.material = hazardMaterial;
            else
                rend.material.color = new Color(1f, 0.25f, 0f, 1f);

            var hazard = zoneGO.AddComponent<VolcanicHazardZone>();
            hazard.Init(damagePerSecond);

            // Predodređeni put: veliki krug kroz točku spawna. Os okomita na smjer
            // prema centru → zona kruži po površini; kutna brzina iz linearne da
            // manje planete zone ne obilaze sporije od većih.
            Vector3 dir = (spawnPos - planet.position).normalized;
            Vector3 axis = Vector3.Cross(dir, Random.onUnitSphere);
            if (axis.sqrMagnitude < 0.001f)
                axis = Vector3.Cross(dir, Vector3.up);
            if (axis.sqrMagnitude < 0.001f)
                axis = Vector3.Cross(dir, Vector3.right);
            axis.Normalize();

            float orbitRadius = Mathf.Max(Vector3.Distance(planet.position, spawnPos), 0.01f);
            float linearSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
            float angularSpeed = Mathf.Rad2Deg * (linearSpeed / orbitRadius);

            var orbit = zoneGO.AddComponent<VolcanicHazardOrbit>();
            orbit.Init(planet, axis, angularSpeed);
        }
    }
}
