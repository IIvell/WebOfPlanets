using System.Collections.Generic;
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
        [SerializeField] private Interactor interactor;
        [SerializeField] private NetworkMapUI networkMapUI;
        [SerializeField] private float spawnForwardDistance = 4f;

        // Ulaz dvosmjernog teleportera koji čeka postavljanje izlaza na drugoj planeti.
        private TeleporterMachine _pendingTwoWayEntry;

        // Zadnji postavljeni collector — sljedeći storage se automatski veže na njega.
        private CollectorMachine _lastCollector;

        void Update()
        {
            if (!GameManager.IsPlaying) return;
            if (Keyboard.current == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Keyboard.current.xKey.wasPressedThisFrame)
                CancelPendingTwoWay();

            if (!Keyboard.current.pKey.wasPressedThisFrame) return;
            if (QuickSlotInventory.current == null) return;

            int index = QuickSlotInventory.current.SelectedIndex;
            QuickSlotItem item = QuickSlotInventory.current.GetSlot(index);

            switch (item)
            {
                // Collector otvara asinkroni UI za odabir veze pa sam troši slot.
                case MachineData collector:
                    TryPlaceCollector(collector, index);
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

                case RespawnTotemMachineData totem:
                    if (TryPlaceRespawnTotem(totem))
                        QuickSlotInventory.current.RemoveSlot(index);
                    break;

                // Ručni uređaj — ne postavlja se i ne troši, samo otvara mapu mreže.
                case NetworkMapDeviceData:
                    OpenNetworkMap();
                    break;

                // Oprema — P stavlja odnosno skida masku (ostaje u slotu).
                case GasMaskData mask:
                    GasMaskData.ToggleWorn(mask);
                    break;
            }
        }

        private void OpenNetworkMap()
        {
            if (networkMapUI == null)
                networkMapUI = FindFirstObjectByType<NetworkMapUI>();
            if (networkMapUI == null)
            {
                Debug.Log("[MachinePlacer] NetworkMapUI nije u sceni — mapa se ne može otvoriti.");
                return;
            }

            networkMapUI.Open();
        }

        private void TryPlaceCollector(MachineData data, int slotIndex)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — stroj se ne može postaviti.");
                return;
            }

            // Pozicija se računa prije otvaranja UI-a da stroj završi tamo gdje je igrač gledao.
            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            List<Transform> reachable = GetReachablePlanets();
            if (reachable.Count == 0 || MachineTeleporterUI.Instance == null)
            {
                SpawnCollector(data, planet, pos, rot, linkedPlanet: null);
                ConsumeSlot(slotIndex, data);
                Debug.Log("[MachinePlacer] Nema aktivnih veza — collector postavljen bez transporta u storage.");
                return;
            }

            MachineTeleporterUI.Instance.Show(
                title: "Connect machine to teleporter",
                planets: reachable,
                onPicked: linked =>
                {
                    SpawnCollector(data, planet, pos, rot, linked);
                    ConsumeSlot(slotIndex, data);
                },
                onCancelled: () =>
                {
                    Debug.Log("[MachinePlacer] Izgradnja otkazana — item ostaje u hotbaru.");
                });
        }

        private void SpawnCollector(MachineData data, Transform planet, Vector3 pos, Quaternion rot,
            Transform linkedPlanet)
        {
            GameObject go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.2f, 0.6f, 1f),
                scale: data.worldScale, rotationOffset: Quaternion.identity, fitColliderToRenderer: true,
                planet: planet);
            CollectorMachine collector = go.AddComponent<CollectorMachine>();
            collector.Init(data, planet);
            if (linkedPlanet != null) collector.SetLinkedPlanet(linkedPlanet);
            _lastCollector = collector;

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
        }

        // UI je asinkron — slot se u međuvremenu mogao promijeniti, troši ga samo ako još drži isti item.
        private static void ConsumeSlot(int slotIndex, QuickSlotItem expected)
        {
            if (QuickSlotInventory.current != null && QuickSlotInventory.current.GetSlot(slotIndex) == expected)
                QuickSlotInventory.current.RemoveSlot(slotIndex);
        }

        // Planete dostupne kroz ConnectionInteractable u dosegu interakcije — na njih
        // collector smije slati resurse.
        private List<Transform> GetReachablePlanets()
        {
            var result = new List<Transform>();

            if (interactor == null)
                interactor = FindFirstObjectByType<Interactor>();
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

            GameObject go = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.8f, 0.4f, 0f),
                scale: 150f, planet: planet);
            StorageMachine storage = go.AddComponent<StorageMachine>();
            storage.Init(data);
            if (_lastCollector != null)
            {
                _lastCollector.SetOutputStorage(storage);
                Debug.Log($"[MachinePlacer] Storage povezan s '{_lastCollector.Data?.displayName}'.");
            }

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
                scale: 3f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);
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
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);
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
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);
            go.AddComponent<UplinkMachine>().Init(data);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            return true;
        }

        // Respawn totem: postavlja se na trenutnoj planeti, E na njemu ga aktivira kao
        // respawn točku (vidi RespawnTotem). Hub već ima glavni totem od starta.
        private bool TryPlaceRespawnTotem(RespawnTotemMachineData data)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
            {
                Debug.Log("[MachinePlacer] Igrač nije na planeti — totem se ne može postaviti.");
                return false;
            }

            if (planet.TryGetComponent(out Planet planetInfo) && planetInfo.IsHub)
            {
                Debug.Log("[MachinePlacer] Hub već ima glavni respawn totem.");
                return false;
            }

            Vector3 pos = FindSurfacePoint(planet);
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized);

            RespawnTotem.Spawn(data, planet, pos, rot);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            Debug.Log($"[MachinePlacer] {data.displayName} postavljen — pritisni E na njemu da postane respawn točka.");
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
            float hubRadius = SurfacePlacement.GetPlanetRadius(hub);
            Vector3 towardPlanet = (planet.position - hub.position).normalized;
            Vector3 exitPos = FindHubSurfacePoint(hub, hubRadius, towardPlanet);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 tangent = Vector3.Cross(towardPlanet, Random.onUnitSphere);
                if (tangent.sqrMagnitude < 0.01f) continue;
                tangent.Normalize();

                Vector3 exitDir = (towardPlanet * hubRadius + tangent * 15f).normalized;
                exitPos = FindHubSurfacePoint(hub, hubRadius, exitDir);
                if (IsSpotClear(exitPos, hub)) break;
            }

            Quaternion exitRot = Quaternion.FromToRotation(Vector3.up, (exitPos - hub.position).normalized);

            GameObject entryGo = SpawnObject(data.prefab, pos, rot, data.displayName, new Color(0.6f, 0.3f, 0.9f),
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);
            TeleporterMachine entry = entryGo.AddComponent<TeleporterMachine>();
            entry.Init(data, planet, planetCreator);

            GameObject exitGo = SpawnObject(data.prefab, exitPos, exitRot, data.displayName + " (Hub)", new Color(0.6f, 0.3f, 0.9f),
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: hub);
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
                    scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);
                _pendingTwoWayEntry = entryGo.AddComponent<TeleporterMachine>();
                _pendingTwoWayEntry.Init(data, planet, planetCreator);

                // Idle dok par nije kompletan — ulaz bez izlaza još ne teleportira.
                GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Idle, Planet = planet });
                Debug.Log("[MachinePlacer] Ulaz postavljen — otiđi na drugu planetu i pritisni P za izlaz (X za odustajanje).");
                return false;
            }

            if (_pendingTwoWayEntry.Planet == planet)
            {
                Debug.Log("[MachinePlacer] Izlaz mora biti na drugoj planeti (X ruši postavljeni ulaz).");
                return false;
            }

            GameObject exitGo = SpawnObject(data.prefab, pos, rot, data.displayName + " (izlaz)", gateColor,
                scale: 7f, rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);
            TeleporterMachine exit = exitGo.AddComponent<TeleporterMachine>();
            exit.Init(data, planet, planetCreator);

            _pendingTwoWayEntry.SetLinkedTeleporter(exit);
            exit.SetLinkedTeleporter(_pendingTwoWayEntry);

            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = _pendingTwoWayEntry.Planet });
            GameEventBus.RaiseMachinePlaced(new MachineEvent { State = MachineState.Active, Planet = planet });
            _pendingTwoWayEntry = null;
            return true;
        }

        // X poništava započeti dvosmjerni par: ruši postavljeni ulaz. Item se troši
        // tek kad su oba kraja postavljena, pa odustajanje ne vraća ništa u hotbar.
        // Ulaz uništen izvana (== null zbog Unity fake-null) samo prestaje blokirati.
        private void CancelPendingTwoWay()
        {
            if (_pendingTwoWayEntry == null)
            {
                _pendingTwoWayEntry = null;
                return;
            }

            Destroy(_pendingTwoWayEntry.gameObject);
            _pendingTwoWayEntry = null;
            Debug.Log("[MachinePlacer] Dvosmjerni teleporter otkazan — ulaz uklonjen, item je ostao u hotbaru.");
        }

        private static Transform FindHubPlanet()
        {
            foreach (var p in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p.transform;
            return null;
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

        // Čisto tlo: u radijusu objekta ne smije biti ničeg osim same planete.
        // Javno jer i GameManager (spawn hub totema) traži čisto mjesto.
        public static bool IsSpotClear(Vector3 pos, Transform planet)
        {
            foreach (var col in Physics.OverlapSphere(pos, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (col.transform != planet) return false;
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
            float   radius = SurfacePlacement.GetPlanetRadius(planet);
            Vector3 origin = planet.position + snapDir * (radius + 20f);

            // Filtrirano na planet — običan raycast bi stao na kamenju ili stroju
            // ispred igrača i postavio novi stroj na njih (isti razlog kao
            // FindHubSurfacePoint gore).
            if (SurfacePlacement.TryRaycastSurface(planet, origin, -snapDir, radius + 40f, out RaycastHit hit))
                return hit.point;

            return planet.position + snapDir * radius;
        }

        // Javno jer i spawn izvan placera (RespawnTotem.Spawn) gradi istim putem.
        // pos mora biti točka na površini planeta; uz zadan planet objekt se prizemlji
        // tako da mu dno stvarne geometrije sjedne na pos, bez obzira gdje je pivot prefaba.
        public static GameObject SpawnObject(GameObject prefab, Vector3 pos, Quaternion rot,
            string fallbackName, Color fallbackColor, float scale = 300f, Quaternion? rotationOffset = null,
            bool fitColliderToRenderer = false, Transform planet = null)
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

            // rot je FromToRotation(Vector3.up, normala), pa je rot * up normala površine
            // i kad rotationOffset dodatno zakrene model.
            if (planet != null)
                SurfacePlacement.GroundToSurface(go, planet, pos, rot * Vector3.up);

            // Zajednički put svih postavljanja (strojevi, totemi) — prašina oko baze.
            VfxManager.PlayMachinePlaced(pos, rot * Vector3.up);

            return go;
        }

        // Postavlja jedan BoxCollider koji prati stvarne granice geometrije, umjesto
        // da se oslanja na default collider primitive kocke ili prefaba koji možda ne
        // odgovara veličini/obliku modela nakon skaliranja i rotacije. Mjerenje živi u
        // SurfacePlacementu (isti izvor točaka kao prizemljenje/audit) — stara verzija
        // ovdje je mjerila renderer bounds, što za skinnane modele (bone-frame AABB)
        // daje box pomaknut od vidljive geometrije.
        public static void FitColliderToRenderer(GameObject go)
            => SurfacePlacement.FitBoxColliderToGeometry(go);
    }
}
