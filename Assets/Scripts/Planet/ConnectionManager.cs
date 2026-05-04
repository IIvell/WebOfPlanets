using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private PlanetCreator planetCreator;

        private readonly List<PlanetConnection> _connections = new();

        void Update()
        {
            if (Keyboard.current.cKey.wasPressedThisFrame)
                BuildConnectionsFromHub();
        }

        private void BuildConnectionsFromHub()
        {
            Transform hub = FindHub();
            if (hub == null)
            {
                Debug.LogWarning("ConnectionManager: nema hub planeta.");
                return;
            }

            Planet[] all = FindObjectsByType<Planet>(FindObjectsSortMode.None);
            int built = 0;

            foreach (Planet p in all)
            {
                if (p.IsHub) continue;
if (AlreadyConnected(hub, p.transform)) continue;

                CreateConnection(hub, p.transform, ConnectionType.PlayerBuilt);
                built++;
            }

            Debug.Log($"ConnectionManager: izgrađeno {built} novih veza s huba.");
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

        private Transform FindHub()
        {
            foreach (Planet p in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p.transform;
            return null;
        }

        public IReadOnlyList<PlanetConnection> Connections => _connections;
    }
}
