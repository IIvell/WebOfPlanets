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

                case SmelterMachineData smelter:
                    if (TryPlaceSmelter(smelter))
                        QuickSlotInventory.current.RemoveSlot(index);
                    break;

                case ExtractorMachineData extractor:
                    if (TryPlaceExtractor(extractor))
                        QuickSlotInventory.current.RemoveSlot(index);
                    break;

                case UplinkMachineData uplink:
                    if (TryPlaceUplink(uplink))
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

        private bool TryPlaceSmelter(SmelterMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return false;
            }

            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            GameObject go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.9f, 0.2f, 0.1f),
                scale: 3f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true);
            go.AddComponent<SmelterMachine>().Init(data);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            return true;
        }

        private bool TryPlaceExtractor(ExtractorMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return false;
            }

            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            GameObject go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.1f, 0.8f, 0.5f),
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true);
            go.AddComponent<ExtractorMachine>().Init(data);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            return true;
        }

        private bool TryPlaceUplink(UplinkMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return false;
            }

            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            GameObject go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.2f, 0.8f, 0.9f),
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true);
            go.AddComponent<UplinkMachine>().Init(data);

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
            string fallbackName, Color fallbackColor, float scale = 300f, Quaternion? rotationOffset = null,
            bool fitColliderToRenderer = false)
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

            go.transform.localScale = Vector3.one * scale;
            go.transform.rotation   = rot * (rotationOffset ?? Quaternion.Euler(-90f, 0f, 0f));
            go.name = fallbackName;

            if (fitColliderToRenderer)
                FitColliderToRenderer(go);
            else if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            return go;
        }

        // Postavlja jedan BoxCollider koji tačno prati stvarne granice vizuala (renderer bounds),
        // umjesto da se oslanja na default collider primitive kocke ili prefaba koji možda ne
        // odgovara veličini/obliku modela nakon skaliranja i rotacije.
        private static void FitColliderToRenderer(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();

            foreach (var existing in go.GetComponentsInChildren<Collider>())
                Destroy(existing);

            if (renderers.Length == 0)
            {
                go.AddComponent<BoxCollider>();
                return;
            }

            // Renderer.bounds je uvijek axis-aligned u world prostoru. Ako se izmjeri dok je
            // objekt već zarotiran (poravnat s površinom planeta), rezultat ne odgovara stvarnom
            // obliku mesh-a kad se samo "vrati" u lokalni prostor preko InverseTransform — nastaje
            // iskrivljena/necentrirana kutija. Zato rotaciju privremeno nuliramo prije mjerenja.
            Quaternion originalRotation = go.transform.rotation;
            go.transform.rotation = Quaternion.identity;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = go.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize   = go.transform.InverseTransformVector(worldBounds.size);

            go.transform.rotation = originalRotation;

            var box = go.AddComponent<BoxCollider>();
            box.center = localCenter;
            box.size   = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }
    }
}
