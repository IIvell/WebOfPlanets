using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WebOfPlanets
{
    // Load: rušenje proceduralnog svijeta u mjestu i ponovna izgradnja iz save
    // datoteke kroz ISTE puteve kao world-gen. Redoslijed koraka i yieldovi su
    // frame-osjetljivi (PhysX poze, odgođeni Destroy) — ne mijenjati olako.
    public static partial class SaveSystem
    {
        public static IEnumerator LoadRoutine()
        {
            SaveData data = ReadFile();
            if (data == null) yield break;

            var planetCreator = UnityEngine.Object.FindFirstObjectByType<PlanetCreator>();
            var cm = UnityEngine.Object.FindFirstObjectByType<ConnectionManager>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (planetCreator == null || cm == null || player == null)
            {
                Debug.LogError("[SaveSystem] Nedostaju scenski sustavi (PlanetCreator/ConnectionManager/Player) — load prekinut.");
                yield break;
            }

            // Napuni asset registry: itemi + recepti (recepti povlače i svoje
            // result assete — strojevi/alati izvan Resources foldera).
            Resources.LoadAll<ScriptableObject>("");

            Transform hub = FindHub();

            // 1) Sruši proceduralni svijet (Destroy se izvršava na kraju framea).
            cm.ResetForLoad();
            DestroyAll<CollectorMachine>();
            DestroyAll<StorageMachine>();
            DestroyAll<SmelterMachine>();
            DestroyAll<ExtractorMachine>();
            DestroyAll<UplinkMachine>();
            DestroyAll<TeleporterMachine>();
            DestroyAll<ComputerMachine>();
            DestroyAll<EnemyMob>();
            DestroyAll<VolcanicHazardZone>();

            foreach (var t in UnityEngine.Object.FindObjectsByType<RespawnTotem>(FindObjectsSortMode.None))
                if (t != RespawnTotem.HubTotem)
                    UnityEngine.Object.Destroy(t.gameObject);

            // Resursi izvan huba padaju sa svijetom; hub dekor (HubResourceSpawner) ostaje.
            var planetsNow = UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.None);
            foreach (var ii in UnityEngine.Object.FindObjectsByType<ItemInteractable>(FindObjectsSortMode.None))
                if (!IsClosestPlanetHub(ii.transform.position, planetsNow))
                    UnityEngine.Object.Destroy(ii.gameObject);

            foreach (var p in planetsNow)
                if (!p.IsHub)
                    UnityEngine.Object.Destroy(p.gameObject);

            // 2) Planete iz save-a — Planet.Start idući frame raise-a
            //    OnPlanetDiscovered pa se hazardi/mobovi sami spawnaju. Resurse
            //    NE: load-ane planete se odmah označe obrađenima (prije njihovog
            //    Start-a) jer se spremljeni raspored resursa vraća ručno dolje.
            var rsm = UnityEngine.Object.FindFirstObjectByType<ResourceSpawnManager>();
            var byName = new Dictionary<string, Transform>();
            if (hub != null) byName[hub.name] = hub;
            foreach (var ps in data.planets)
            {
                Transform planet = planetCreator.SpawnPlanetFromSave(ps.name, ps.position, ps.scale, ps.gravity, (PlanetType)ps.type);
                // Duplikat imena u save-u bi ovdje tiho pregazio raniji planet i
                // veze/strojeve/igrača zakačio na krivi — imena su ključ formata.
                if (byName.ContainsKey(ps.name))
                    Debug.LogError($"[SaveSystem] Duplikat imena planeta '{ps.name}' u save datoteci — reference na to ime završit će na zadnjem.");
                byName[ps.name] = planet;
                if (rsm != null) rsm.MarkProcessed(planet);
            }

            yield return null; // stari objekti stvarno uništeni; Planet.Start odrađen
            yield return null; // hazardi/mobovi spawnani, PhysX poze sinkane

            // 3) Spremljeni resursi (prije totema — totemi biraju čisto tlo pa se
            //    razmiču od resursa, kao i pri normalnom world-genu).
            if (rsm != null)
                foreach (var rs in data.resources)
                {
                    if (!byName.TryGetValue(rs.planet, out var rp) || rp == null) continue;
                    rsm.SpawnSavedResource(Resolve<Item>(rs.item), rs.pickup, rp, rs.position, rs.rotation);
                }
            else if (data.resources.Count > 0)
                Debug.LogWarning("[SaveSystem] ResourceSpawnManager nije u sceni — spremljeni resursi preskočeni.");

            // 4) Potencijalni totemi pa aktivne veze (veza gasi svoje potencijalne).
            cm.SpawnPotentialMarkersForLoad();
            foreach (var cs in data.connections)
                if (byName.TryGetValue(cs.planetA, out var a) && byName.TryGetValue(cs.planetB, out var b))
                    cm.RestoreConnection(a, b, (ConnectionType)cs.type, cs.health);

            // 5) Strojevi + povezivanja po indeksima.
            var made = new List<Component>();
            foreach (var ms in data.machines)
                made.Add(RebuildMachine(ms, byName, planetCreator));

            for (int i = 0; i < data.machines.Count; i++)
            {
                int link = data.machines[i].linkedIndex;
                if (link < 0 || link >= made.Count || made[i] == null || made[link] == null) continue;

                if (made[i] is CollectorMachine col && made[link] is StorageMachine st)
                    col.SetOutputStorage(st);
                else if (made[i] is TeleporterMachine tel && made[link] is TeleporterMachine other)
                    tel.SetLinkedTeleporter(other);
            }

            // 6) Napredak, skladišta, inventar, hotbar, zdravlje.
            HubProgress.LoadTier(data.hubTier);

            if (HubStorage.Instance != null)
            {
                HubStorage.Instance.ClearForLoad();
                foreach (var ic in data.hubStorage)
                    HubStorage.Instance.LoadItem(Resolve<Item>(ic.item), ic.count);
            }

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.ClearForLoad();
                foreach (var ic in data.inventory)
                    InventorySystem.Instance.LoadItem(Resolve<Item>(ic.item), ic.count);
            }

            var qs = QuickSlotInventory.Instance;
            if (qs != null)
            {
                qs.ClearForLoad();
                foreach (var s in data.quickSlots)
                    qs.LoadSlot(s.index, Resolve<QuickSlotItem>(s.item), s.durability);
                if (data.selectedSlot >= 0)
                    qs.SelectSlot(data.selectedSlot);
            }

            UnityEngine.Object.FindFirstObjectByType<PlayerHealth>()?.LoadHealth(data.playerHealth);

            // 7) Igrač: TeleportToPlanet rješava attractore/kameru/SetPlanet, a
            //    točna poza se vrati preko spremljenog riga.
            Transform target = byName.TryGetValue(data.playerPlanet, out var tp) && tp != null ? tp : hub;
            if (target != null)
                planetCreator.TeleportToPlanet(target);
            player.rig.position = data.playerPosition;
            player.rig.rotation = data.playerRotation;

            Debug.Log("[SaveSystem] Igra učitana.");
        }

        private static SaveData ReadFile()
        {
            if (!SaveExists)
            {
                Debug.LogWarning("[SaveSystem] Nema save datoteke.");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Neispravna save datoteka: {e.Message}");
                return null;
            }
        }

        private static Component RebuildMachine(MachineSave ms, Dictionary<string, Transform> byName, PlanetCreator planetCreator)
        {
            if (!byName.TryGetValue(ms.planet, out Transform planet) || planet == null) return null;

            // Spremljena pozicija je već PRIZEMLJENI pivot; SpawnObject ponovno
            // sjeda dnom na zadanu točku, pa mu dajemo točku POVRŠINE ispod stroja
            // (inače bi stroj potonuo za razmak pivot-dno).
            Vector3 up = (ms.position - planet.position).normalized;
            SurfacePlacement.GetSurfacePoint(planet, up, out Vector3 surfacePos, out _);

            switch (ms.kind)
            {
                case KindCollector:
                {
                    var d = Resolve<MachineData>(ms.data);
                    if (d == null) return null;
                    var go = MachineFactory.SpawnMachine(d.prefab, surfacePos, ms.rotation, d.displayName,
                        MachineFactory.CollectorColor, ms.scale, planet);
                    var c = go.AddComponent<CollectorMachine>();
                    c.Init(d, planet);
                    if (!string.IsNullOrEmpty(ms.linkedPlanet) && byName.TryGetValue(ms.linkedPlanet, out var lp) && lp != null)
                        c.SetLinkedPlanet(lp);
                    foreach (var ic in ms.stored)
                        c.LoadStoredItem(Resolve<Item>(ic.item), ic.count);
                    if (ms.broken) c.LoadBroken();
                    return c;
                }
                case KindStorage:
                {
                    var d = Resolve<StorageMachineData>(ms.data);
                    if (d == null) return null;
                    // Poznata (naslijeđena) razlika: load gradi storage kao standardni
                    // stroj (identity + fitani collider), a MachinePlacer ga postavlja
                    // s default -90° offsetom bez fitanja — vidi PLAN-KOD §1.
                    var go = MachineFactory.SpawnMachine(d.prefab, surfacePos, ms.rotation, d.displayName,
                        MachineFactory.StorageColor, ms.scale, planet);
                    var s = go.AddComponent<StorageMachine>();
                    s.Init(d);
                    foreach (var ic in ms.stored)
                    {
                        var item = Resolve<Item>(ic.item);
                        if (item == null) continue;
                        for (int i = 0; i < ic.count; i++)
                            if (!s.Add(item)) break;
                    }
                    return s;
                }
                case KindSmelter:
                {
                    var d = Resolve<SmelterMachineData>(ms.data);
                    if (d == null) return null;
                    var go = MachineFactory.SpawnMachine(d.prefab, surfacePos, ms.rotation, d.displayName,
                        MachineFactory.SmelterColor, ms.scale, planet);
                    var s = go.AddComponent<SmelterMachine>();
                    s.Init(d, planet);
                    foreach (var ic in ms.stored)
                        s.LoadInputItem(Resolve<Item>(ic.item), ic.count);
                    foreach (var ic in ms.storedB)
                        s.LoadOutputItem(Resolve<Item>(ic.item), ic.count);
                    if (ms.broken) s.LoadBroken();
                    return s;
                }
                case KindExtractor:
                {
                    var d = Resolve<ExtractorMachineData>(ms.data);
                    if (d == null) return null;
                    var go = MachineFactory.SpawnMachine(d.prefab, surfacePos, ms.rotation, d.displayName,
                        MachineFactory.ExtractorColor, ms.scale, planet);
                    var e = go.AddComponent<ExtractorMachine>();
                    e.Init(d, planet);
                    foreach (var ic in ms.stored)
                        e.LoadStoredItem(Resolve<Item>(ic.item), ic.count);
                    if (ms.broken) e.LoadBroken();
                    return e;
                }
                case KindUplink:
                {
                    var d = Resolve<UplinkMachineData>(ms.data);
                    if (d == null) return null;
                    var go = MachineFactory.SpawnMachine(d.prefab, surfacePos, ms.rotation, d.displayName,
                        MachineFactory.UplinkColor, ms.scale, planet);
                    var u = go.AddComponent<UplinkMachine>();
                    u.Init(d, planet);
                    foreach (var ic in ms.stored)
                        u.LoadBufferItem(Resolve<Item>(ic.item), ic.count);
                    if (ms.broken) u.LoadBroken();
                    return u;
                }
                case KindTeleporter:
                {
                    // Resolve pokriva i TwoWayTeleporterMachineData (podklasa);
                    // TeleporterColorFor dvosmjernom vraća gate boju — prije je i
                    // loadani dvosmjerni dobivao boju običnog (defekt-fix, PLAN-KOD §1).
                    var d = Resolve<TeleporterMachineData>(ms.data);
                    if (d == null) return null;
                    var go = MachineFactory.SpawnMachine(d.prefab, surfacePos, ms.rotation, d.displayName,
                        MachineFactory.TeleporterColorFor(d), ms.scale, planet);
                    var t = go.AddComponent<TeleporterMachine>();
                    t.Init(d, planet, planetCreator);
                    return t;
                }
                case KindTotem:
                {
                    var d = Resolve<RespawnTotemMachineData>(ms.data);
                    var t = RespawnTotem.Spawn(d, planet, surfacePos, ms.rotation);
                    if (ms.totemActive) t.Interact();
                    return t;
                }
                case KindComputer:
                {
                    var d = Resolve<ComputerMachineData>(ms.data);
                    return ComputerMachine.Spawn(d, planet, surfacePos, ms.rotation);
                }
            }

            return null;
        }
    }
}
