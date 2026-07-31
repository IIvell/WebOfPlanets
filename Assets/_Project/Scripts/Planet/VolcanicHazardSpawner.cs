using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    // Kružna putanja zone po površini planeta: rotacija oko fiksne osi kroz centar
    // planeta. Vulkanski planeti su uniformno skalirane primitivne sfere, pa je
    // udaljenost od centra duž cijele kružnice konstantna — zona ostaje jednako
    // ukopana bez raycasta po frameu. Dodaje se runtime (AddComponent); spawner
    // je jedini od tri vulkanska razreda serijaliziran u sceni pa je nositelj
    // datoteke. Konsolidirano iz VolcanicHazardOrbit.cs (srpanj 2026.).
    public class VolcanicHazardOrbit : MonoBehaviour
    {
        private Transform _planet;
        private Vector3 _axis;
        private float _angularSpeed; // stupnjevi u sekundi

        public void Init(Transform planet, Vector3 axis, float angularSpeed)
        {
            _planet = planet;
            _axis = axis;
            _angularSpeed = angularSpeed;
        }

        void Update()
        {
            if (_planet == null)
            {
                enabled = false;
                return;
            }

            transform.RotateAround(_planet.position, _axis, _angularSpeed * Time.deltaTime);
        }
    }

    // Konsolidirano iz VolcanicHazardZone.cs (srpanj 2026.); dodaje se runtime.
    [RequireComponent(typeof(Collider))]
    public class VolcanicHazardZone : MonoBehaviour
    {
        private const float TickInterval = 1f;

        // Zajednički tick za SVE zone (igra je single-player): u preklopu više zona
        // šteta ide samo iz one koja prva odradi tick — svaka zona s vlastitim
        // timerom je u preklopu duplirala štetu.
        private static float _nextTickTime;
        private static float _lastContactTime;

        [Tooltip("Šteta po sekundi dok se igrač nalazi unutar zone.")]
        [SerializeField] private float damagePerSecond = 15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _nextTickTime = 0f;
            _lastContactTime = float.NegativeInfinity;
        }

        public void Init(float dps) => damagePerSecond = dps;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (other.attachedRigidbody == null) return;
            if (!other.attachedRigidbody.TryGetComponent(out PlayerHealth health)) return;

            // Igrač je bio izvan svih zona — grace period od punog intervala kreće ispočetka.
            if (Time.time - _lastContactTime > TickInterval)
                _nextTickTime = Time.time + TickInterval;
            _lastContactTime = Time.time;

            if (Time.time < _nextTickTime) return;
            _nextTickTime = Time.time + TickInterval;

            health.TakeDamage(damagePerSecond * TickInterval);
        }
    }

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
