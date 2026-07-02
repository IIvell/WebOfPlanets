using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    // Kad je u hotbaru selektan stroj (CollectorMachine/StorageMachine), tipka P ga postavlja
    // na trenutnu planetu ispred igrača i troši ga iz hotbar slota.
    public class MachinePlacer : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private float spawnForwardDistance = 4f;

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.pKey.wasPressedThisFrame) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (QuickSlotInventory.current == null) return;

            int index = QuickSlotInventory.current.SelectedIndex;
            QuickSlotItem item = QuickSlotInventory.current.GetSlot(index);

            switch (item)
            {
                case MachineData collector:
                    if (TryPlaceCollector(collector))
                        QuickSlotInventory.current.RemoveSlot(index);
                    break;

                case StorageMachineData storage:
                    if (TryPlaceStorage(storage))
                        QuickSlotInventory.current.RemoveSlot(index);
                    break;
            }
        }

        private bool TryPlaceCollector(MachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return false;
            }

            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            GameObject go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.2f, 0.6f, 1f));
            go.AddComponent<CollectorMachine>().Init(data, planet);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            return true;
        }

        private bool TryPlaceStorage(StorageMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return false;
            }

            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            GameObject go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.8f, 0.4f, 0f));
            go.AddComponent<StorageMachine>().Init(data, null);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            return true;
        }

        private Vector3 FindSurfacePoint(Transform planet)
        {
            Vector3 playerPos = playerController.rig.position;
            Vector3 targetPos = playerPos + playerController.transform.forward * spawnForwardDistance;
            Vector3 snapDir   = (targetPos - planet.position).normalized;
            float   radius    = planet.localScale.x * 0.5f;
            Vector3 origin    = planet.position + snapDir * (radius + 20f);

            if (Physics.Raycast(origin, -snapDir, out RaycastHit hit, radius + 40f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point;

            return planet.position + snapDir * radius;
        }

        private static GameObject SpawnObject(GameObject prefab, Vector3 pos, Quaternion rot,
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
            go.transform.rotation   = rot * Quaternion.Euler(-90f, 0f, 0f);
            go.name = fallbackName;

            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();
            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            return go;
        }
    }
}
