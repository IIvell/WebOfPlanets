using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlanetConnection : MonoBehaviour
    {
        public Transform PlanetA { get; private set; }
        public Transform PlanetB { get; private set; }
        public float Health { get; private set; } = 100f;
        public ConnectionType Type { get; private set; }

        private GameObject _cylinder;
        private Material _material;
        private float _lifespan;
        private GameObject _markerPrefab;
        private float _markerScale;
        private float _markerHeight;
        private float _thickness;

        private static readonly Color HealthColorHigh = Color.green;
        private static readonly Color HealthColorMid = Color.yellow;
        private static readonly Color HealthColorLow = new Color(1f, 0.5f, 0f);
        private static readonly Color HealthColorCritical = Color.red;
        private const float FlickerThreshold = 20f;

        public void Init(Transform a, Transform b, ConnectionType type, PlanetCreator planetCreator, float lifespan = 0f, GameObject markerPrefab = null, float markerScale = 3f, float markerHeight = 3f, float thickness = 0.6f)
        {
            PlanetA = a;
            PlanetB = b;
            Type = type;
            _lifespan = lifespan;
            _markerPrefab = markerPrefab;
            _markerScale = markerScale;
            _markerHeight = markerHeight;
            _thickness = thickness;

            _cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _cylinder.transform.SetParent(transform);
            Destroy(_cylinder.GetComponent<Collider>());
            _material = _cylinder.GetComponent<Renderer>().material;

            UpdateHealthColor();
            UpdateVisual();

            GameObject markerA = SpawnMarker(from: a, toward: b, planetCreator);
            GameObject markerB = SpawnMarker(from: b, toward: a, planetCreator);

            if (markerA != null && markerB != null)
            {
                markerA.GetComponent<ConnectionInteractable>().SetDestinationMarker(markerB.transform);
                markerB.GetComponent<ConnectionInteractable>().SetDestinationMarker(markerA.transform);
            }
        }

        void Update()
        {
            UpdateHealthColor();

            if (_lifespan <= 0f) return;
            float damagePerSecond = 100f / _lifespan;
            ApplyDamage(damagePerSecond * Time.deltaTime);
        }

        private void UpdateHealthColor()
        {
            Color color = HealthToColor(Health);

            if (Health < FlickerThreshold)
            {
                float flickerSpeed = Mathf.Lerp(4f, 12f, 1f - Health / FlickerThreshold);
                float flicker = (Mathf.Sin(Time.time * flickerSpeed) + 1f) * 0.5f;
                color *= Mathf.Lerp(0.35f, 1f, flicker);
            }

            _material.color = color;
            _material.SetColor("_BaseColor", color);
        }

        private static Color HealthToColor(float health)
        {
            float t = Mathf.Clamp01(health / 100f);

            if (t > 2f / 3f)
                return Color.Lerp(HealthColorMid, HealthColorHigh, (t - 2f / 3f) * 3f);
            if (t > 1f / 3f)
                return Color.Lerp(HealthColorLow, HealthColorMid, (t - 1f / 3f) * 3f);

            return Color.Lerp(HealthColorCritical, HealthColorLow, t * 3f);
        }

        private GameObject SpawnMarker(Transform from, Transform toward, PlanetCreator planetCreator)
        {
            if (_markerPrefab == null)
            {
                Debug.LogError("PlanetConnection: markerPrefab nije postavljen.");
                return null;
            }

            Vector3 dir = (toward.position - from.position).normalized;
            Vector3 pos = SurfacePoint(from, dir, out Vector3 normal);

            // Uspravno po stvarnoj normali pogođene površine (na neravnom Hub meshu
            // se razlikuje od radijalnog smjera) — marker stoji na tlu kao totem, a
            // normala je ujedno i "up" za teleport dolazak (destinationMarker.up).
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);

            GameObject marker = Instantiate(_markerPrefab, pos, rot, transform);
            marker.name = "ConnectionMarker";
            marker.transform.localScale = Vector3.one * _markerScale;

            // Prizemlji po stvarnoj geometriji da dno sjedne na površinu bez obzira
            // na pivot prefaba.
            SurfacePlacement.GroundToSurface(marker, from, pos, normal);

            CapsuleCollider col = marker.AddComponent<CapsuleCollider>();
            col.isTrigger = true;

            ConnectionInteractable interactable = marker.AddComponent<ConnectionInteractable>();
            interactable.Init(planetCreator, sourcePlanet: from, targetPlanet: toward);

            return marker;
        }

        // Zajednički helper umjesto vlastite matematike: localScale kao radijus laže
        // za mesh planete (Hub ima localScale 1000 uz stvarni radijus ~19), pa je
        // fallback markere znao ostaviti stotine jedinica u svemiru pored zrake.
        internal static Vector3 SurfacePoint(Transform planet, Vector3 directionFromPlanet)
            => SurfacePoint(planet, directionFromPlanet, out _);

        internal static Vector3 SurfacePoint(Transform planet, Vector3 directionFromPlanet, out Vector3 surfaceNormal)
        {
            SurfacePlacement.GetSurfacePoint(planet, directionFromPlanet, out Vector3 point, out surfaceNormal);
            return point;
        }

        private void UpdateVisual()
        {
            Vector3 direction = (PlanetB.position - PlanetA.position).normalized;

            Vector3 tipA = SurfacePoint(PlanetA, direction) + direction * _markerHeight;
            Vector3 tipB = SurfacePoint(PlanetB, -direction) - direction * _markerHeight;

            Vector3 midpoint = (tipA + tipB) * 0.5f;
            float distance = Vector3.Distance(tipA, tipB);

            _cylinder.transform.position = midpoint;
            _cylinder.transform.up = direction;
            _cylinder.transform.localScale = new Vector3(_thickness, distance * 0.5f, _thickness);
        }

        public void ApplyDamage(float amount)
        {
            Health = Mathf.Clamp(Health - amount, 0f, 100f);
            GameEventBus.Raise(new ConnectionHealthChangedEvent
            {
                Health = Health,
                PlanetA = PlanetA,
                PlanetB = PlanetB,
                ConnectionType = Type
            });

            if (Health <= 0f)
            {
                GameEventBus.RaiseConnectionDestroyed(new ConnectionEvent
                {
                    PlanetA = PlanetA,
                    PlanetB = PlanetB,
                    ConnectionType = Type
                });
                Destroy(gameObject);
            }
        }

        public bool Connects(Transform a, Transform b) =>
            (PlanetA == a && PlanetB == b) || (PlanetA == b && PlanetB == a);
    }
}
