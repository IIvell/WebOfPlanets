using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    // Kad je u hotbaru selektan stroj (CollectorMachine/StorageMachine), tipka P ga postavlja
    // na trenutnu planetu ispred igrača i troši ga iz hotbar slota. X podiže stroj natrag.
    // Sam spawn svjetskih objekata živi u MachineFactory — ovdje je samo input i tok postavljanja.
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
            if (UiFocus.IsAnyPanelOpen) return;

            if (GameKeys.WasPressed(GameKeys.PickupMachine))
            {
                if (_pendingTwoWayEntry != null)
                    CancelPendingTwoWay();
                else
                    TryPickupMachine();
            }

            if (!GameKeys.WasPressed(GameKeys.PlaceMachine)) return;
            if (QuickSlotInventory.Instance == null) return;

            int index = QuickSlotInventory.Instance.SelectedIndex;
            QuickSlotItem item = QuickSlotInventory.Instance.GetSlot(index);

            switch (item)
            {
                // Collector otvara asinkroni UI za odabir veze pa sam troši slot.
                case MachineData collector:
                    TryPlaceCollector(collector, index);
                    break;

                case StorageMachineData storage:
                    if (TryPlaceStorage(storage))
                        QuickSlotInventory.Instance.RemoveSlot(index);
                    break;

                case SmelterMachineData smelter:
                    if (TryPlaceSmelter(smelter))
                        QuickSlotInventory.Instance.RemoveSlot(index);
                    break;

                case ExtractorMachineData extractor:
                    if (TryPlaceExtractor(extractor))
                        QuickSlotInventory.Instance.RemoveSlot(index);
                    break;

                case UplinkMachineData uplink:
                    if (TryPlaceUplink(uplink))
                        QuickSlotInventory.Instance.RemoveSlot(index);
                    break;

                // Podklasa mora ići prije TeleporterMachineData case-a.
                case TwoWayTeleporterMachineData twoWay:
                    if (TryPlaceTwoWayTeleporter(twoWay))
                        QuickSlotInventory.Instance.RemoveSlot(index);
                    break;

                case TeleporterMachineData teleporter:
                    if (TryPlaceTeleporter(teleporter))
                        QuickSlotInventory.Instance.RemoveSlot(index);
                    break;

                case RespawnTotemMachineData totem:
                    if (TryPlaceRespawnTotem(totem))
                        QuickSlotInventory.Instance.RemoveSlot(index);
                    break;

                case ComputerMachineData computer:
                    if (TryPlaceComputer(computer))
                        QuickSlotInventory.Instance.RemoveSlot(index);
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

        // ── Zajednički koraci svih postavljanja ───────────────────────────────

        // Planeta na kojoj igrač stoji, uz poruku kad je nema (svako postavljanje
        // počinje ovom provjerom).
        private Transform RequirePlanet(string what)
        {
            Transform planet = playerController?.currentPlanet;
            if (planet == null)
                Debug.Log($"[MachinePlacer] Igrač nije na planeti — {what} se ne može postaviti.");
            return planet;
        }

        // Točka na površini ispred igrača + rotacija uspravno na radijalu.
        private (Vector3 pos, Quaternion rot) PlacementPose(Transform planet)
        {
            Vector3 pos = FindSurfacePoint(planet);
            return (pos, Quaternion.FromToRotation(Vector3.up, (pos - planet.position).normalized));
        }

        private void RaisePlaced(Transform planet, MachineState state = MachineState.Active)
            => GameEventBus.RaiseMachinePlaced(new MachineEvent { State = state, Planet = planet });

        // ── Postavljanje po tipu stroja ───────────────────────────────────────

        private void TryPlaceCollector(MachineData data, int slotIndex)
        {
            Transform planet = RequirePlanet("stroj");
            if (planet == null) return;

            // Pozicija se računa prije otvaranja UI-a da stroj završi tamo gdje je igrač gledao.
            var (pos, rot) = PlacementPose(planet);

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
            GameObject go = MachineFactory.SpawnMachine(data.prefab, pos, rot, data.displayName,
                MachineFactory.CollectorColor, data.worldScale, planet);
            CollectorMachine collector = go.AddComponent<CollectorMachine>();
            collector.Init(data, planet);
            if (linkedPlanet != null) collector.SetLinkedPlanet(linkedPlanet);
            _lastCollector = collector;

            RaisePlaced(planet);
        }

        // UI je asinkron — slot se u međuvremenu mogao promijeniti, troši ga samo ako još drži isti item.
        private static void ConsumeSlot(int slotIndex, QuickSlotItem expected)
        {
            if (QuickSlotInventory.Instance != null && QuickSlotInventory.Instance.GetSlot(slotIndex) == expected)
                QuickSlotInventory.Instance.RemoveSlot(slotIndex);
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
            Transform planet = RequirePlanet("stroj");
            if (planet == null) return false;

            var (pos, rot) = PlacementPose(planet);

            // Poznata (naslijeđena) razlika prema loadu: postavljeni storage ide
            // kroz SpawnObject s default -90° offsetom i BEZ fitanja collidera,
            // dok ga SaveSystem loada kao standardni stroj (identity + fit).
            // Namjerno se ne ujednačava u refaktoru — promjena bi dirala collider
            // postavljenog storagea (vidi PLAN-KOD §1).
            GameObject go = MachineFactory.SpawnObject(data.prefab, pos, rot, data.displayName,
                MachineFactory.StorageColor, scale: data.worldScale, planet: planet);
            StorageMachine storage = go.AddComponent<StorageMachine>();
            storage.Init(data);
            if (_lastCollector != null)
            {
                _lastCollector.SetOutputStorage(storage);
                Debug.Log($"[MachinePlacer] Storage povezan s '{_lastCollector.Data?.displayName}'.");
            }

            RaisePlaced(planet);
            return true;
        }

        private bool TryPlaceSmelter(SmelterMachineData data)
        {
            Transform planet = RequirePlanet("stroj");
            if (planet == null) return false;

            var (pos, rot) = PlacementPose(planet);
            MachineFactory.SpawnMachine(data.prefab, pos, rot, data.displayName,
                    MachineFactory.SmelterColor, data.worldScale, planet)
                .AddComponent<SmelterMachine>().Init(data, planet);

            RaisePlaced(planet);
            return true;
        }

        private bool TryPlaceExtractor(ExtractorMachineData data)
        {
            Transform planet = RequirePlanet("stroj");
            if (planet == null) return false;

            var (pos, rot) = PlacementPose(planet);
            MachineFactory.SpawnMachine(data.prefab, pos, rot, data.displayName,
                    MachineFactory.ExtractorColor, data.worldScale, planet)
                .AddComponent<ExtractorMachine>().Init(data, planet);

            RaisePlaced(planet);
            return true;
        }

        private bool TryPlaceUplink(UplinkMachineData data)
        {
            Transform planet = RequirePlanet("stroj");
            if (planet == null) return false;

            var (pos, rot) = PlacementPose(planet);
            MachineFactory.SpawnMachine(data.prefab, pos, rot, data.displayName,
                    MachineFactory.UplinkColor, data.worldScale, planet)
                .AddComponent<UplinkMachine>().Init(data, planet);

            RaisePlaced(planet);
            return true;
        }

        // Respawn totem: postavlja se na trenutnoj planeti, E na njemu ga aktivira kao
        // respawn točku (vidi RespawnTotem). Hub već ima glavni totem od starta.
        private bool TryPlaceRespawnTotem(RespawnTotemMachineData data)
        {
            Transform planet = RequirePlanet("totem");
            if (planet == null) return false;

            if (planet.TryGetComponent(out Planet planetInfo) && planetInfo.IsHub)
            {
                Debug.Log("[MachinePlacer] Hub već ima glavni respawn totem.");
                return false;
            }

            var (pos, rot) = PlacementPose(planet);
            RespawnTotem.Spawn(data, planet, pos, rot);

            RaisePlaced(planet);
            Debug.Log($"[MachinePlacer] {data.displayName} postavljen — pritisni E na njemu da postane respawn točka.");
            return true;
        }

        // X pokraj postavljenog stroja: item se vrati u hotbar, stroj nestane iz
        // svijeta, a njegov sadržaj se istrese u inventar igrača. Teleporter par
        // nestaje zajedno (item je potrošen jednom za oba kraja).
        private void TryPickupMachine()
        {
            if (QuickSlotInventory.Instance == null) return;

            if (interactor == null)
                interactor = FindFirstObjectByType<Interactor>();
            if (interactor == null) return;

            // Bez pozicije izvora nema podizanja (fallback na playerController
            // je ranije mogao NRE-ati kad referenca nije postavljena).
            if (interactor.InteractorSource == null && playerController == null) return;
            Vector3 source = interactor.InteractorSource != null
                ? interactor.InteractorSource.position
                : playerController.rig.position;

            // Najbliži stroj u dosegu interakcije (isti doseg kao E).
            Component closest = null;
            float closestDist = float.PositiveInfinity;
            foreach (var col in Physics.OverlapSphere(source, interactor.InteractRange,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                if (!col.TryGetComponent(out BaseInteractable bi)) continue;
                if (bi is not (CollectorMachine or StorageMachine or SmelterMachine or ExtractorMachine
                    or UplinkMachine or TeleporterMachine or RespawnTotem or ComputerMachine)) continue;

                float dist = Vector3.Distance(source, col.ClosestPoint(source));
                if (dist < closestDist) { closestDist = dist; closest = bi; }
            }
            if (closest == null) return;

            QuickSlotItem item = null;
            var stored  = new List<InventoryItem>();
            var destroy = new List<GameObject> { closest.gameObject };

            switch (closest)
            {
                case CollectorMachine m:
                    if (Broken(m.State)) return;
                    item = m.Data; stored.AddRange(m.StoredItems); break;

                case StorageMachine m:
                    item = m.Data; stored.AddRange(m.Inventory); break;

                case SmelterMachine m:
                    if (Broken(m.State)) return;
                    item = m.Data; stored.AddRange(m.InputItems); stored.AddRange(m.OutputItems); break;

                case ExtractorMachine m:
                    if (Broken(m.State)) return;
                    item = m.Data; stored.AddRange(m.StoredItems); break;

                case UplinkMachine m:
                    if (Broken(m.State)) return;
                    item = m.Data; stored.AddRange(m.Buffer); break;

                case TeleporterMachine m:
                    // Nepovezani ulaz je pending dvosmjerni — njega ruši X kroz CancelPendingTwoWay.
                    if (m.Linked == null) return;
                    item = m.Data; destroy.Add(m.Linked.gameObject); break;

                case RespawnTotem m:
                    if (m == RespawnTotem.HubTotem || m.Data == null)
                    {
                        Debug.Log("[MachinePlacer] Glavni hub totem se ne može podići.");
                        return;
                    }
                    item = m.Data; break;

                case ComputerMachine m:
                    item = m.Data; break;
            }

            if (item == null) return;

            if (!QuickSlotInventory.Instance.TryAdd(item, out _))
            {
                Debug.Log("[MachinePlacer] Hotbar je pun — stroj se ne može vratiti.");
                return;
            }

            if (InventorySystem.Instance != null)
                foreach (var s in stored)
                    if (s != null && s.data != null)
                        for (int i = 0; i < s.GetStackSize(); i++)
                            InventorySystem.Instance.Add(s.data);

            foreach (var go in destroy)
            {
                VfxManager.PlayMachinePlaced(go.transform.position, go.transform.up);
                Destroy(go);
            }

            Debug.Log($"[MachinePlacer] {item.displayName} vraćen u hotbar.");
        }

        private static bool Broken(MachineState state)
        {
            if (state != MachineState.Broken) return false;
            Debug.Log("[MachinePlacer] Stroj je polomljen — popravi ga (E) prije podizanja.");
            return true;
        }

        // Računalo: E na njemu otvara isti izbornik kao hub Računalo (mapa mreže,
        // hub pragovi) — s Respawn Totemom čini udaljenu bazu umjesto sekundarnog huba.
        private bool TryPlaceComputer(ComputerMachineData data)
        {
            Transform planet = RequirePlanet("računalo");
            if (planet == null) return false;

            if (planet.TryGetComponent(out Planet planetInfo) && planetInfo.IsHub)
            {
                Debug.Log("[MachinePlacer] Hub već ima Računalo.");
                return false;
            }

            var (pos, rot) = PlacementPose(planet);
            ComputerMachine.Spawn(data, planet, pos, rot);

            RaisePlaced(planet);
            Debug.Log($"[MachinePlacer] {data.displayName} postavljen — pritisni E na njemu za izbornik Računala.");
            return true;
        }

        // Teleporter se gradi u paru: ulaz ispred igrača, izlaz na Hubu — na strani
        // Huba okrenutoj prema ovoj planeti, uz bočni pomak da se izlazi ne preklapaju.
        private bool TryPlaceTeleporter(TeleporterMachineData data)
        {
            Transform planet = RequirePlanet("stroj");
            if (planet == null) return false;

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

            var (pos, rot) = PlacementPose(planet);

            // Izlaz ide na stranu Huba okrenutu prema ovoj planeti; nekoliko pokušaja
            // s nasumičnim bočnim pomakom da ne završi na bazi, resursu ili drugom gateu.
            float hubRadius = SurfacePlacement.GetPlanetRadius(hub);
            Vector3 towardPlanet = (planet.position - hub.position).normalized;
            Vector3 exitPos = FindSurfacePoint(hub, towardPlanet);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 tangent = Vector3.Cross(towardPlanet, Random.onUnitSphere);
                if (tangent.sqrMagnitude < 0.01f) continue;
                tangent.Normalize();

                Vector3 exitDir = (towardPlanet * hubRadius + tangent * 15f).normalized;
                exitPos = FindSurfacePoint(hub, exitDir);
                if (MachineFactory.IsSpotClear(exitPos, hub)) break;
            }

            Quaternion exitRot = Quaternion.FromToRotation(Vector3.up, (exitPos - hub.position).normalized);

            GameObject entryGo = MachineFactory.SpawnMachine(data.prefab, pos, rot, data.displayName,
                MachineFactory.TeleporterColor, data.worldScale, planet);
            TeleporterMachine entry = entryGo.AddComponent<TeleporterMachine>();
            entry.Init(data, planet, planetCreator);

            GameObject exitGo = MachineFactory.SpawnMachine(data.prefab, exitPos, exitRot, data.displayName + " (Hub)",
                MachineFactory.TeleporterColor, data.worldScale, hub);
            TeleporterMachine exit = exitGo.AddComponent<TeleporterMachine>();
            exit.Init(data, hub, planetCreator);

            entry.SetLinkedTeleporter(exit);
            exit.SetLinkedTeleporter(entry);

            RaisePlaced(planet);
            RaisePlaced(hub);
            return true;
        }

        // Dvosmjerni teleporter se postavlja u dva koraka: prvi P postavlja ulaz na
        // trenutnoj planeti, drugi P (na drugoj planeti) postavlja izlaz i povezuje ih.
        // Item se troši iz hotbara tek kad su oba kraja postavljena.
        private bool TryPlaceTwoWayTeleporter(TwoWayTeleporterMachineData data)
        {
            Transform planet = RequirePlanet("stroj");
            if (planet == null) return false;

            if (planetCreator == null)
                planetCreator = FindFirstObjectByType<PlanetCreator>();
            if (planetCreator == null)
            {
                Debug.Log("[MachinePlacer] PlanetCreator nije u sceni — teleporter se ne može postaviti.");
                return false;
            }

            var (pos, rot) = PlacementPose(planet);

            if (_pendingTwoWayEntry == null)
            {
                GameObject entryGo = MachineFactory.SpawnMachine(data.prefab, pos, rot,
                    data.displayName + " (ulaz)", MachineFactory.TwoWayGateColor, data.worldScale, planet);
                _pendingTwoWayEntry = entryGo.AddComponent<TeleporterMachine>();
                _pendingTwoWayEntry.Init(data, planet, planetCreator);

                // Idle dok par nije kompletan — ulaz bez izlaza još ne teleportira.
                RaisePlaced(planet, MachineState.Idle);
                Debug.Log("[MachinePlacer] Ulaz postavljen — otiđi na drugu planetu i pritisni P za izlaz (X za odustajanje).");
                return false;
            }

            if (_pendingTwoWayEntry.Planet == planet)
            {
                Debug.Log("[MachinePlacer] Izlaz mora biti na drugoj planeti (X ruši postavljeni ulaz).");
                return false;
            }

            GameObject exitGo = MachineFactory.SpawnMachine(data.prefab, pos, rot,
                data.displayName + " (izlaz)", MachineFactory.TwoWayGateColor, data.worldScale, planet);
            TeleporterMachine exit = exitGo.AddComponent<TeleporterMachine>();
            exit.Init(data, planet, planetCreator);

            _pendingTwoWayEntry.SetLinkedTeleporter(exit);
            exit.SetLinkedTeleporter(_pendingTwoWayEntry);

            RaisePlaced(_pendingTwoWayEntry.Planet);
            RaisePlaced(planet);
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

        private Vector3 FindSurfacePoint(Transform planet)
        {
            Vector3 playerPos = playerController.rig.position;
            Vector3 targetPos = playerPos + playerController.transform.forward * spawnForwardDistance;
            return FindSurfacePoint(planet, (targetPos - planet.position).normalized);
        }

        // Točka površine u smjeru snapDir. SurfacePlacement filtrira pogodak na
        // samu planetu (običan raycast bi stao na kamenju ili stroju ispred igrača
        // i gradio na njima) i ponavlja ray nakon Physics.SyncTransforms — fix
        // koji su lokalne kopije ove metode bile propustile.
        private static Vector3 FindSurfacePoint(Transform planet, Vector3 snapDir)
        {
            SurfacePlacement.GetSurfacePoint(planet, snapDir, out Vector3 point, out _);
            return point;
        }
    }
}
