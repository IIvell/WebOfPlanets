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

        public void Init(Transform a, Transform b, ConnectionType type, PlanetCreator planetCreator)
        {
            PlanetA = a;
            PlanetB = b;
            Type = type;

            _cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _cylinder.transform.SetParent(transform);
            Destroy(_cylinder.GetComponent<Collider>());

            UpdateVisual();

            SpawnMarker(from: a, toward: b, planetCreator);
            SpawnMarker(from: b, toward: a, planetCreator);
        }

        private void SpawnMarker(Transform from, Transform toward, PlanetCreator planetCreator)
        {
            Vector3 dir = (toward.position - from.position).normalized;
            Renderer rend = from.GetComponentInChildren<Renderer>();
            float radius = rend != null ? rend.bounds.size.x * 0.5f : from.localScale.x * 0.5f;
            Vector3 pos = from.position + dir * radius;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = "ConnectionMarker";
            marker.transform.SetParent(transform);
            marker.transform.SetPositionAndRotation(pos, rot);
            marker.transform.localScale = Vector3.one * 3f;

            marker.GetComponent<Collider>().isTrigger = true;

            ConnectionInteractable interactable = marker.AddComponent<ConnectionInteractable>();
            interactable.Init(planetCreator, sourcePlanet: from, targetPlanet: toward);
        }

        private void UpdateVisual()
        {
            Vector3 midpoint = (PlanetA.position + PlanetB.position) * 0.5f;
            float distance = Vector3.Distance(PlanetA.position, PlanetB.position);
            Vector3 direction = (PlanetB.position - PlanetA.position).normalized;

            _cylinder.transform.position = midpoint;
            _cylinder.transform.up = direction;
            _cylinder.transform.localScale = new Vector3(2f, distance * 0.5f, 2f);
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
