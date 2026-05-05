using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private PlanetCreator planetCreator;
        [SerializeField] private float maxConnectionRange = 5000f;

        private readonly List<PlanetConnection> _connections = new();

        private IEnumerator Start()
        {
            yield return null; // wait for PlanetCreator.Start() to finish spawning planets
            SpawnPotentialMarkers();
        }

        private void SpawnPotentialMarkers()
        {
            Planet[] all = FindObjectsByType<Planet>(FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < all.Length; i++)
            {
                for (int j = i + 1; j < all.Length; j++)
                {
                    Transform a = all[i].transform;
                    Transform b = all[j].transform;

                    if (Vector3.Distance(a.position, b.position) > maxConnectionRange) continue;
                    if (AlreadyConnected(a, b)) continue;

                    SpawnPotentialPair(a, b);
                    count++;
                }
            }

            Debug.Log($"ConnectionManager: spawnirano {count} potencijalnih veza.");
        }

        private void SpawnPotentialPair(Transform a, Transform b)
        {
            GameObject markerOnA = CreatePotentialMarker(a, b);
            GameObject markerOnB = CreatePotentialMarker(b, a);

            markerOnA.GetComponent<PotentialConnectionInteractable>().SetMirror(markerOnB);
            markerOnB.GetComponent<PotentialConnectionInteractable>().SetMirror(markerOnA);
        }

        private GameObject CreatePotentialMarker(Transform from, Transform toward)
        {
            Vector3 dir = (toward.position - from.position).normalized;
            Vector3 pos = SurfacePoint(from, dir);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = "PotentialConnectionMarker";
            marker.transform.SetParent(transform);
            marker.transform.SetPositionAndRotation(pos, rot);
            marker.transform.localScale = Vector3.one * 3f;
            marker.GetComponent<Collider>().isTrigger = true;

            PotentialConnectionInteractable interactable = marker.AddComponent<PotentialConnectionInteractable>();
            interactable.Init(this, from, toward);

            return marker;
        }

        public void BuildConnection(Transform a, Transform b)
        {
            if (AlreadyConnected(a, b)) return;
            CreateConnection(a, b, ConnectionType.PlayerBuilt);
        }

        private void CreateConnection(Transform a, Transform b, ConnectionType type)
        {
            GameObject go = new GameObject($"Connection_{a.name}_{b.name}");
            go.transform.SetParent(transform);

            PlanetConnection conn = go.AddComponent<PlanetConnection>();
            conn.Init(a, b, type, planetCreator);
            _connections.Add(conn);

            GameEventBus.RaiseConnectionCreated(new ConnectionEvent
            {
                PlanetA = a,
                PlanetB = b,
                ConnectionType = type
            });
        }

        private bool AlreadyConnected(Transform a, Transform b)
        {
            foreach (var c in _connections)
                if (c != null && c.Connects(a, b)) return true;
            return false;
        }

        private static Vector3 SurfacePoint(Transform planet, Vector3 directionFromPlanet)
        {
            float radius = planet.localScale.x * 0.5f;
            Vector3 origin = planet.position + directionFromPlanet * (radius + 5f);

            if (Physics.Raycast(origin, -directionFromPlanet, out RaycastHit hit, radius + 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point;

            return planet.position + directionFromPlanet * radius;
        }

        public IReadOnlyList<PlanetConnection> Connections => _connections;
    }
}
