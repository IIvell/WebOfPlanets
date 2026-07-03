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
        private float _lifespan;
        private GameObject _markerPrefab;
        private float _markerScale;
        private float _markerHeight;

        public void Init(Transform a, Transform b, ConnectionType type, PlanetCreator planetCreator, float lifespan = 0f, GameObject markerPrefab = null, float markerScale = 3f, float markerHeight = 3f)
        {
            PlanetA = a;
            PlanetB = b;
            Type = type;
            _lifespan = lifespan;
            _markerPrefab = markerPrefab;
            _markerScale = markerScale;
            _markerHeight = markerHeight;

            _cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _cylinder.transform.SetParent(transform);
            Destroy(_cylinder.GetComponent<Collider>());

            ApplyTypeColor();
            UpdateVisual();

            SpawnMarker(from: a, toward: b, planetCreator);
            SpawnMarker(from: b, toward: a, planetCreator);
        }

        void Update()
        {
            if (_lifespan <= 0f) return;
            float damagePerSecond = 100f / _lifespan;
            ApplyDamage(damagePerSecond * Time.deltaTime);
        }

        private void ApplyTypeColor()
        {
            Color color = Type switch
            {
                ConnectionType.Weak   => Color.red,
                ConnectionType.Mid    => new Color(1f, 0.5f, 0f),
                ConnectionType.Strong => Color.green,
                _                     => Color.white
            };

            var mat = _cylinder.GetComponent<Renderer>().material;
            mat.color = color;
            mat.SetColor("_BaseColor", color);
        }

        private void SpawnMarker(Transform from, Transform toward, PlanetCreator planetCreator)
        {
            if (_markerPrefab == null)
            {
                Debug.LogError("PlanetConnection: markerPrefab nije postavljen.");
                return;
            }

            Vector3 dir = (toward.position - from.position).normalized;
            Vector3 pos = SurfacePoint(from, dir);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir);

            GameObject marker = Instantiate(_markerPrefab, pos, rot, transform);
            marker.name = "ConnectionMarker";
            marker.transform.localScale = Vector3.one * _markerScale;

            CapsuleCollider col = marker.AddComponent<CapsuleCollider>();
            col.isTrigger = true;

            ConnectionInteractable interactable = marker.AddComponent<ConnectionInteractable>();
            interactable.Init(planetCreator, sourcePlanet: from, targetPlanet: toward);
        }

        private static readonly int PlanetLayerMask = LayerMask.GetMask("Planet");

        internal static Vector3 SurfacePoint(Transform planet, Vector3 directionFromPlanet)
        {
            float radius = planet.localScale.x * 0.5f;
            Vector3 origin = planet.position + directionFromPlanet * (radius + 5f);

            if (Physics.Raycast(origin, -directionFromPlanet, out RaycastHit hit, radius + 10f, PlanetLayerMask, QueryTriggerInteraction.Ignore))
                return hit.point;

            return planet.position + directionFromPlanet * radius;
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
            _cylinder.transform.localScale = new Vector3(0.6f, distance * 0.5f, 0.6f);
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
