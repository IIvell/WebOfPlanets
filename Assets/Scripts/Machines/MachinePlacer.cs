using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    // Kad je u hotbaru selektan stroj (CollectorMachine/StorageMachine), tipka P ga postavlja
    // na trenutnu planetu ispred igrača i troši ga iz hotbar slota.
    public class MachinePlacer : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlanetCreator planetCreator;
        [SerializeField] private float spawnForwardDistance = 4f;

        // Ulaz dvosmjernog teleportera koji čeka postavljanje izlaza na drugoj planeti.
        private TeleporterMachine _pendingTwoWayEntry;

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

                // Podklasa mora ići prije TeleporterMachineData case-a.
                case TwoWayTeleporterMachineData twoWay:
                    if (TryPlaceTwoWayTeleporter(twoWay))
                        QuickSlotInventory.current.RemoveSlot(index);
                    break;

                case TeleporterMachineData teleporter:
                    if (TryPlaceTeleporter(teleporter))
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

        // Teleporter se gradi u paru: ulaz ispred igrača, izlaz na Hubu — na strani
        // Huba okrenutoj prema ovoj planeti, uz bočni pomak da se izlazi ne preklapaju.
        private bool TryPlaceTeleporter(TeleporterMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return false;
            }

            if (planet.TryGetComponent(out Planet planetInfo) && planetInfo.IsHub)
            {
                Debug.Log("[MachinePlacer] Teleporter vodi na Hub — nema smisla postaviti ga na Hubu.");
                return false;
            }

            Transform hub = FindHubPlanet();
            if (hub == null)
            {
                Debug.Log("[MachinePlacer] Hub planeta nije pronađena — teleporter se ne može postaviti.");
                return false;
            }

            if (planetCreator == null)
                planetCreator = FindFirstObjectByType<PlanetCreator>();
            if (planetCreator == null)
            {
                Debug.Log("[MachinePlacer] PlanetCreator nije u sceni — teleporter se ne može postaviti.");
                return false;
            }

            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            // Izlaz ide na stranu Huba okrenutu prema ovoj planeti; nekoliko pokušaja
            // s nasumičnim bočnim pomakom da ne završi na bazi, resursu ili drugom gateu.
            float hubRadius = GetPlanetRadius(hub);
            Vector3 towardPlanet = (planet.position - hub.position).normalized;
            Vector3 exitPos = FindHubSurfacePoint(hub, hubRadius, towardPlanet);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 tangent = Vector3.Cross(towardPlanet, Random.onUnitSphere);
                if (tangent.sqrMagnitude < 0.01f) continue;
                tangent.Normalize();

                Vector3 exitDir = (towardPlanet * hubRadius + tangent * 15f).normalized;
                exitPos = FindHubSurfacePoint(hub, hubRadius, exitDir);
                if (IsExitSpotClear(exitPos, hub)) break;
            }

            Quaternion exitRot = Quaternion.FromToRotation(Vector3.up, (exitPos - hub.position).normalized);

            GameObject entryGo = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.6f, 0.3f, 0.9f),
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true);
            TeleporterMachine entry = entryGo.AddComponent<TeleporterMachine>();
            entry.Init(data, planet, planetCreator);

            GameObject exitGo = SpawnObject(data.prefab, exitPos, exitRot, data.displayName + " (Hub)", new Color(0.6f, 0.3f, 0.9f),
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true);
            TeleporterMachine exit = exitGo.AddComponent<TeleporterMachine>();
            exit.Init(data, hub, planetCreator);

            entry.SetLinkedTeleporter(exit);
            exit.SetLinkedTeleporter(entry);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = hub });
            return true;
        }

        // Dvosmjerni teleporter se postavlja u dva koraka: prvi P postavlja ulaz na
        // trenutnoj planeti, drugi P (na drugoj planeti) postavlja izlaz i povezuje ih.
        // Item se troši iz hotbara tek kad su oba kraja postavljena.
        private bool TryPlaceTwoWayTeleporter(TwoWayTeleporterMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return false;
            }

            if (planetCreator == null)
                planetCreator = FindFirstObjectByType<PlanetCreator>();
            if (planetCreator == null)
            {
                Debug.Log("[MachinePlacer] PlanetCreator nije u sceni — teleporter se ne može postaviti.");
                return false;
            }

            Color gateColor = new Color(1f, 0.6f, 0.1f);
            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            if (_pendingTwoWayEntry == null)
            {
                GameObject entryGo = SpawnObject(data.prefab, pos, rot, data.displayName + " (ulaz)", gateColor,
                    scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true);
                _pendingTwoWayEntry = entryGo.AddComponent<TeleporterMachine>();
                _pendingTwoWayEntry.Init(data, planet, planetCreator);

                GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
                Debug.Log("[MachinePlacer] Ulaz postavljen — otiđi na drugu planetu i pritisni P za izlaz.");
                return false;
            }

            if (_pendingTwoWayEntry.Planet == planet)
            {
                Debug.Log("[MachinePlacer] Izlaz mora biti na drugoj planeti.");
                return false;
            }

            GameObject exitGo = SpawnObject(data.prefab, pos, rot, data.displayName + " (izlaz)", gateColor,
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true);
            TeleporterMachine exit = exitGo.AddComponent<TeleporterMachine>();
            exit.Init(data, planet, planetCreator);

            _pendingTwoWayEntry.SetLinkedTeleporter(exit);
            exit.SetLinkedTeleporter(_pendingTwoWayEntry);
            _pendingTwoWayEntry = null;

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            return true;
        }

        private static Transform FindHubPlanet()
        {
            foreach (var p in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p.transform;
            return null;
        }

        // Hub nije jedinična sfera (Planet.fbx), pa localScale daje višestruko prevelik
        // radijus — čitaj iz renderer boundsa kao HubResourceSpawner. Za planete iz
        // PlanetCreatora (primitivne sfere) rezultat je isti kao localScale * 0.5.
        private static float GetPlanetRadius(Transform planet)
        {
            Renderer rend = planet.GetComponentInChildren<Renderer>();
            return rend != null ? rend.bounds.size.x * 0.5f : planet.localScale.x * 0.5f;
        }

        // Za izlazni gate raycast smije pogoditi samo planetu — običan raycast bi
        // stao na kamenju, bazi ili već postavljenom gateu i gradio na njima.
        private static Vector3 FindHubSurfacePoint(Transform hub, float hubRadius, Vector3 dir)
        {
            Vector3 origin = hub.position + dir * (hubRadius + 20f);

            if (SurfacePlacement.TryRaycastSurface(hub, origin, -dir, hubRadius + 40f, out RaycastHit hit))
                return hit.point;

            return hub.position + dir * hubRadius;
        }

        // Čisto tlo: u radijusu gatea ne smije biti ničeg osim same planete.
        private static bool IsExitSpotClear(Vector3 pos, Transform hub)
        {
            foreach (var col in Physics.OverlapSphere(pos, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (col.transform != hub) return false;
            return true;
        }

        private Vector3 FindSurfacePoint(Transform planet)
        {
            Vector3 playerPos = playerController.rig.position;
            Vector3 targetPos = playerPos + playerController.transform.forward * spawnForwardDistance;
            return FindSurfacePoint(planet, (targetPos - planet.position).normalized);
        }

        private static Vector3 FindSurfacePoint(Transform planet, Vector3 snapDir)
        {
            float   radius = GetPlanetRadius(planet);
            Vector3 origin = planet.position + snapDir * (radius + 20f);

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
