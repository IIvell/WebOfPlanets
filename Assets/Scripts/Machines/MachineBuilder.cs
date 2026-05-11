using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public class MachineBuilder : MonoBehaviour
    {
        [SerializeField] private MachineData collectorData;
        [SerializeField] private StorageMachineData storageData;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Interactor interactor;
        [SerializeField] private float spawnForwardDistance = 4f;

        private CollectorMachine _lastCollector;

        void Update()
        {
            if (Keyboard.current.mKey.wasPressedThisFrame)
                TryBuildCollector();

            if (Keyboard.current.oKey.wasPressedThisFrame)
                TryBuildStorage();
        }

        private void TryBuildCollector()
        {
            if (collectorData == null)
            {
                Debug.LogWarning("[MachineBuilder] Nije postavljen CollectorData asset.");
                return;
            }

            Transform currentPlanet = playerController?.currentPlanet;
            if (currentPlanet == null)
            {
                Debug.LogWarning("[MachineBuilder] Igrač nije na planeti.");
                return;
            }

            // Zapamti spawn poziciju prije UI-a
            Vector3 spawnPos = FindSurfacePoint(currentPlanet);
            Quaternion spawnRot = Quaternion.FromToRotation(
                Vector3.up, (spawnPos - currentPlanet.position).normalized);

            List<Transform> reachable = GetReachablePlanets();

            if (reachable.Count == 0)
            {
                Debug.Log("[MachineBuilder] Nema aktivnih veza na ovoj planeti — stroj nije izgrađen.");
                return;
            }

            if (MachineTeleporterUI.Instance == null)
            {
                Debug.LogWarning("[MachineBuilder] MachineTeleporterUI nije u sceni — stroj nije izgrađen.");
                return;
            }

            Debug.Log($"[MachineBuilder] Prikazujem UI s {reachable.Count} planeta.");
            MachineTeleporterUI.Instance.Show(
                title: "Poveži stroj s teleporterom",
                planets: reachable,
                onPicked: linkedPlanet =>
                {
                    SpawnCollector(currentPlanet, spawnPos, spawnRot, linkedPlanet);
                },
                onCancelled: () =>
                {
                    Debug.Log("[MachineBuilder] Izgradnja otkazana.");
                }
            );
        }

        private void TryBuildStorage()
        {
            if (_lastCollector == null)
            {
                Debug.Log("[MachineBuilder] Najprije izgradi Collector stroj (M).");
                return;
            }

            Transform currentPlanet = playerController?.currentPlanet;
            if (currentPlanet == null) return;

            Vector3 spawnPos = FindSurfacePoint(currentPlanet);
            Quaternion spawnRot = Quaternion.FromToRotation(
                Vector3.up, (spawnPos - currentPlanet.position).normalized);

            GameObject go = SpawnMachineObject(storageData?.prefab, spawnPos, spawnRot,
                "StorageMachine", new Color(0.8f, 0.4f, 0f));
            var storage = go.AddComponent<StorageMachine>();
            storage.Init(storageData, _lastCollector);
            _lastCollector.SetOutputStorage(storage);

            Debug.Log($"[MachineBuilder] Storage izgrađen i povezan s '{_lastCollector.Data?.displayName}'.");
        }

        private CollectorMachine SpawnCollector(Transform planet, Vector3 pos, Quaternion rot,
            Transform linkedPlanet)
        {
            GameObject go = SpawnMachineObject(collectorData?.prefab, pos, rot,
                collectorData.displayName, new Color(0.2f, 0.6f, 1f));
            var collector = go.AddComponent<CollectorMachine>();
            collector.Init(collectorData, planet);
            if (linkedPlanet != null) collector.SetLinkedPlanet(linkedPlanet);

            _lastCollector = collector;
            Debug.Log($"[MachineBuilder] Collector izgrađen na '{planet.name}'" +
                      (linkedPlanet != null ? $" → '{linkedPlanet.name}'" : " (bez veze)"));
            return collector;
        }

        private List<Transform> GetReachablePlanets()
        {
            var result = new List<Transform>();
            if (interactor == null) return result;

            Vector3 source = interactor.InteractorSource != null
                ? interactor.InteractorSource.position
                : playerController.rig.position;

            var hits = new Collider[16];
            int count = Physics.OverlapSphereNonAlloc(
                source, interactor.InteractRange, hits,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var col = hits[i];
                if (!col.TryGetComponent<ConnectionInteractable>(out var ci)) continue;
                if (ci.TargetPlanet != null && !result.Contains(ci.TargetPlanet))
                    result.Add(ci.TargetPlanet);
            }

            return result;
        }

        private Vector3 FindSurfacePoint(Transform planet)
        {
            Vector3 playerPos = playerController.rig.position;
            Vector3 targetPos = playerPos + playerController.transform.forward * spawnForwardDistance;

            Vector3 snapDir = (targetPos - planet.position).normalized;
            float radius    = planet.localScale.x * 0.5f;
            Vector3 origin  = planet.position + snapDir * (radius + 20f);

            if (Physics.Raycast(origin, -snapDir, out RaycastHit hit, radius + 40f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point;

            return planet.position + snapDir * radius;
        }

        private GameObject SpawnMachineObject(GameObject prefab, Vector3 pos, Quaternion rot,
            string fallbackName, Color fallbackColor)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, rot);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(pos, rot);
                go.GetComponent<Renderer>().material.color = fallbackColor;
            }

            go.transform.localScale = Vector3.one * 300f;
            go.transform.rotation = rot * Quaternion.Euler(-90f, 0f, 0f);

            go.name = fallbackName;
            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();
            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            return go;
        }
    }
}
