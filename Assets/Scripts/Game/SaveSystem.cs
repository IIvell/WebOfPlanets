using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Save/load u JSON (jedan slot, Application.persistentDataPath). Sprema se:
    // proceduralne planete, aktivne veze (tip + zdravlje), strojevi s vezama
    // (collector→storage, teleporter parovi), respawn totemi, sadržaj storage
    // strojeva, hub skladište, inventar, hotbar (s trajnošću), hub prag i igrač.
    //
    // Load NE reloada scenu (runtime-bootstrap sustavi poput MainMenuUI/VfxManagera
    // bi nestali): proceduralni svijet se sruši u mjestu i ponovno izgradi kroz
    // ISTE puteve kao world-gen — Planet.Start opet raise-a OnPlanetDiscovered pa
    // se vulkanske zone i mobovi sami spawnaju. Resursi su iznimka: spremaju se
    // pojedinačno (item, pozicija, pickup/mining) pa se svježi spawn za load-ane
    // planete preskače (ResourceSpawnManager.MarkProcessed). Sprema se i Broken
    // stanje te interni bufferi svih strojeva.
    //
    // Svjesna pojednostavljenja: mining progress u tijeku i regeneracijski timeri
    // resursa se ne spremaju (resurs u regeneraciji se vrati vidljiv); hub dekor
    // resursi se ne diraju (HubResourceSpawner ih drži).
    public static class SaveSystem
    {
        private const int KindCollector = 0, KindStorage = 1, KindSmelter = 2,
                          KindExtractor = 3, KindUplink = 4, KindTeleporter = 5, KindTotem = 6;

        [Serializable] public class ItemCountSave { public string item; public int count; }

        [Serializable] public class PlanetSave
        {
            public string name;
            public Vector3 position;
            public float scale;
            public float gravity;
            public int type;
        }

        [Serializable] public class ConnectionSave
        {
            public string planetA;
            public string planetB;
            public int type;
            public float health;
        }

        [Serializable] public class SlotSave { public int index; public string item; public int durability; }

        [Serializable] public class ResourceSave
        {
            public string item;
            public string planet;
            public Vector3 position;
            public Quaternion rotation;
            public bool pickup;
        }

        [Serializable] public class MachineSave
        {
            public int kind;
            public string data;
            public string planet;
            public Vector3 position;
            public Quaternion rotation;
            public float scale;
            public string linkedPlanet;   // collector: cilj transporta
            public int linkedIndex = -1;  // collector→storage / teleporter par (indeks u machines listi)
            public bool totemActive;
            public bool broken;
            public List<ItemCountSave> stored = new();  // storage/collector/extractor/uplink buffer; smelter INPUT
            public List<ItemCountSave> storedB = new(); // smelter OUTPUT
        }

        [Serializable] public class SaveData
        {
            public int version = 1;
            public int hubTier;
            public float playerHealth;
            public string playerPlanet;
            public Vector3 playerPosition;
            public Quaternion playerRotation;
            public int selectedSlot = -1;
            public List<PlanetSave> planets = new();
            public List<ConnectionSave> connections = new();
            public List<ItemCountSave> hubStorage = new();
            public List<ItemCountSave> inventory = new();
            public List<SlotSave> quickSlots = new();
            public List<MachineSave> machines = new();
            public List<ResourceSave> resources = new();
        }

        public static string SavePath => Path.Combine(Application.persistentDataPath, "webofplanets_save.json");
        public static bool SaveExists => File.Exists(SavePath);

        // ── Save ──────────────────────────────────────────────────────────────

        public static bool Save()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[SaveSystem] Nema igrača u sceni — spremanje preskočeno.");
                return false;
            }

            var data = new SaveData
            {
                hubTier = HubProgress.Tier,
                playerPlanet = player.currentPlanet != null ? player.currentPlanet.name : "",
                playerPosition = player.rig.position,
                playerRotation = player.rig.rotation
            };

            var health = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            data.playerHealth = health != null ? health.CurrentHealth : 100f;

            foreach (var p in UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.None))
            {
                if (p.IsHub) continue;
                data.planets.Add(new PlanetSave
                {
                    name = p.name,
                    position = p.transform.position,
                    scale = p.transform.localScale.x,
                    gravity = p.Gravity,
                    type = (int)p.Type
                });
            }

            var cm = UnityEngine.Object.FindFirstObjectByType<ConnectionManager>();
            if (cm != null)
                foreach (var c in cm.Connections)
                    if (c != null && c.PlanetA != null && c.PlanetB != null)
                        data.connections.Add(new ConnectionSave
                        {
                            planetA = c.PlanetA.name,
                            planetB = c.PlanetB.name,
                            type = (int)c.Type,
                            health = c.Health
                        });

            if (HubStorage.current != null)
                foreach (var inv in HubStorage.current.GetInventory())
                    if (inv.data != null)
                        data.hubStorage.Add(new ItemCountSave { item = inv.data.name, count = inv.GetStackSize() });

            if (InventorySystem.current != null)
                foreach (var inv in InventorySystem.current.GetInventory())
                    if (inv.data != null)
                        data.inventory.Add(new ItemCountSave { item = inv.data.name, count = inv.GetStackSize() });

            var qs = QuickSlotInventory.current;
            if (qs != null)
            {
                data.selectedSlot = qs.SelectedIndex;
                for (int i = 0; i < QuickSlotInventory.SlotCount; i++)
                {
                    var slot = qs.GetSlot(i);
                    if (slot != null)
                        data.quickSlots.Add(new SlotSave { index = i, item = slot.name, durability = qs.GetDurability(i) });
                }
            }

            GatherMachines(data, out List<Component> comps);
            ResolveMachineLinks(data, comps);
            GatherResources(data);

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Spremanje nije uspjelo: {e.Message}");
                return false;
            }

            Debug.Log($"[SaveSystem] Igra spremljena: {SavePath}");
            return true;
        }

        private static void GatherMachines(SaveData data, out List<Component> comps)
        {
            // Lokalna funkcija ne smije hvatati out parametar (CS1628) — hvata se
            // lokalna lista, a out se dodijeli na kraju.
            var list = new List<Component>();

            void Add(Component c, MachineSave m)
            {
                m.position = c.transform.position;
                m.rotation = c.transform.rotation;
                m.scale = c.transform.localScale.x;
                list.Add(c);
                data.machines.Add(m);
            }

            foreach (var m in UnityEngine.Object.FindObjectsByType<CollectorMachine>(FindObjectsSortMode.None))
                if (m.Data != null && m.Planet != null)
                    Add(m, new MachineSave
                    {
                        kind = KindCollector, data = m.Data.name, planet = m.Planet.name,
                        linkedPlanet = m.LinkedPlanet != null ? m.LinkedPlanet.name : "",
                        broken = m.State == MachineState.Broken,
                        stored = ToItemCounts(m.StoredItems)
                    });

            foreach (var m in UnityEngine.Object.FindObjectsByType<StorageMachine>(FindObjectsSortMode.None))
            {
                if (m.Data == null) continue;
                Transform planet = ClosestPlanet(m.transform.position);
                if (planet == null) continue;

                Add(m, new MachineSave
                {
                    kind = KindStorage, data = m.Data.name, planet = planet.name,
                    stored = ToItemCounts(m.Inventory)
                });
            }

            foreach (var m in UnityEngine.Object.FindObjectsByType<SmelterMachine>(FindObjectsSortMode.None))
                if (m.Data != null && m.Planet != null)
                    Add(m, new MachineSave
                    {
                        kind = KindSmelter, data = m.Data.name, planet = m.Planet.name,
                        broken = m.State == MachineState.Broken,
                        stored = ToItemCounts(m.InputItems),
                        storedB = ToItemCounts(m.OutputItems)
                    });

            foreach (var m in UnityEngine.Object.FindObjectsByType<ExtractorMachine>(FindObjectsSortMode.None))
                if (m.Data != null && m.Planet != null)
                    Add(m, new MachineSave
                    {
                        kind = KindExtractor, data = m.Data.name, planet = m.Planet.name,
                        broken = m.State == MachineState.Broken,
                        stored = ToItemCounts(m.StoredItems)
                    });

            foreach (var m in UnityEngine.Object.FindObjectsByType<UplinkMachine>(FindObjectsSortMode.None))
                if (m.Data != null && m.Planet != null)
                    Add(m, new MachineSave
                    {
                        kind = KindUplink, data = m.Data.name, planet = m.Planet.name,
                        broken = m.State == MachineState.Broken,
                        stored = ToItemCounts(m.Buffer)
                    });

            // Nepovezani (pending dvosmjerni) teleporter se NE sprema — item za
            // njega još nije potrošen iz hotbara, pa je hotbar u save-u istina.
            foreach (var m in UnityEngine.Object.FindObjectsByType<TeleporterMachine>(FindObjectsSortMode.None))
                if (m.Data != null && m.Planet != null && m.Linked != null)
                    Add(m, new MachineSave { kind = KindTeleporter, data = m.Data.name, planet = m.Planet.name });

            foreach (var t in UnityEngine.Object.FindObjectsByType<RespawnTotem>(FindObjectsSortMode.None))
            {
                if (t == RespawnTotem.HubTotem || t.Planet == null) continue; // hub totem spawna GameManager
                Add(t, new MachineSave
                {
                    kind = KindTotem,
                    data = t.Data != null ? t.Data.name : "",
                    planet = t.Planet.name,
                    totemActive = RespawnTotem.Active == t
                });
            }

            comps = list;
        }

        private static List<ItemCountSave> ToItemCounts(IReadOnlyList<InventoryItem> items)
        {
            var result = new List<ItemCountSave>();
            foreach (var inv in items)
                if (inv.data != null)
                    result.Add(new ItemCountSave { item = inv.data.name, count = inv.GetStackSize() });
            return result;
        }

        // Resursi na proceduralnim planetima (hub dekor drži HubResourceSpawner i
        // load ga ne dira, pa se ni ne sprema).
        private static void GatherResources(SaveData data)
        {
            var planets = UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.None);
            foreach (var ii in UnityEngine.Object.FindObjectsByType<ItemInteractable>(FindObjectsSortMode.None))
            {
                if (ii.ReferenceItem == null) continue;
                Planet closest = ClosestPlanetOf(ii.transform.position, planets);
                if (closest == null || closest.IsHub) continue;

                data.resources.Add(new ResourceSave
                {
                    item = ii.ReferenceItem.name,
                    planet = closest.name,
                    position = ii.transform.position,
                    rotation = ii.transform.rotation,
                    pickup = ii.IsPickup
                });
            }
        }

        private static void ResolveMachineLinks(SaveData data, List<Component> comps)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                switch (data.machines[i].kind)
                {
                    case KindCollector:
                        var storage = ((CollectorMachine)comps[i]).OutputStorage;
                        data.machines[i].linkedIndex = storage != null ? comps.IndexOf(storage) : -1;
                        break;
                    case KindTeleporter:
                        var linked = ((TeleporterMachine)comps[i]).Linked;
                        data.machines[i].linkedIndex = linked != null ? comps.IndexOf(linked) : -1;
                        break;
                }
            }
        }

        // ── Load ──────────────────────────────────────────────────────────────

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

            if (HubStorage.current != null)
            {
                HubStorage.current.ClearForLoad();
                foreach (var ic in data.hubStorage)
                    HubStorage.current.LoadItem(Resolve<Item>(ic.item), ic.count);
            }

            if (InventorySystem.current != null)
            {
                InventorySystem.current.ClearForLoad();
                foreach (var ic in data.inventory)
                    InventorySystem.current.LoadItem(Resolve<Item>(ic.item), ic.count);
            }

            var qs = QuickSlotInventory.current;
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
                    var go = MachinePlacer.SpawnObject(d.prefab, surfacePos, ms.rotation, d.displayName,
                        new Color(0.2f, 0.6f, 1f), scale: ms.scale, rotationOffset: Quaternion.identity,
                        fitColliderToRenderer: true, planet: planet);
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
                    var go = MachinePlacer.SpawnObject(d.prefab, surfacePos, ms.rotation, d.displayName,
                        new Color(0.8f, 0.4f, 0f), scale: ms.scale, rotationOffset: Quaternion.identity,
                        fitColliderToRenderer: true, planet: planet);
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
                    var go = MachinePlacer.SpawnObject(d.prefab, surfacePos, ms.rotation, d.displayName,
                        new Color(0.9f, 0.2f, 0.1f), scale: ms.scale, rotationOffset: Quaternion.identity,
                        fitColliderToRenderer: true, planet: planet);
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
                    var go = MachinePlacer.SpawnObject(d.prefab, surfacePos, ms.rotation, d.displayName,
                        new Color(0.1f, 0.8f, 0.5f), scale: ms.scale, rotationOffset: Quaternion.identity,
                        fitColliderToRenderer: true, planet: planet);
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
                    var go = MachinePlacer.SpawnObject(d.prefab, surfacePos, ms.rotation, d.displayName,
                        new Color(0.2f, 0.8f, 0.9f), scale: ms.scale, rotationOffset: Quaternion.identity,
                        fitColliderToRenderer: true, planet: planet);
                    var u = go.AddComponent<UplinkMachine>();
                    u.Init(d, planet);
                    foreach (var ic in ms.stored)
                        u.LoadBufferItem(Resolve<Item>(ic.item), ic.count);
                    if (ms.broken) u.LoadBroken();
                    return u;
                }
                case KindTeleporter:
                {
                    // Resolve pokriva i TwoWayTeleporterMachineData (podklasa).
                    var d = Resolve<TeleporterMachineData>(ms.data);
                    if (d == null) return null;
                    var go = MachinePlacer.SpawnObject(d.prefab, surfacePos, ms.rotation, d.displayName,
                        new Color(0.6f, 0.3f, 0.9f), scale: ms.scale, rotationOffset: Quaternion.identity,
                        fitColliderToRenderer: true, planet: planet);
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
            }

            return null;
        }

        // ── Helperi ───────────────────────────────────────────────────────────

        // Asseti se traže po tipu + imenu među učitanima (Resources.LoadAll gore
        // povuče sve iz Resources foldera, a recepti transitivno i svoje result
        // assete). Tipizirano jer se imena ponavljaju (recept "Teleporter" vs
        // machine data "Teleporter").
        private static T Resolve<T>(string assetName) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            foreach (var o in Resources.FindObjectsOfTypeAll<T>())
                if (o.name == assetName)
                    return o;

            Debug.LogWarning($"[SaveSystem] Asset '{assetName}' ({typeof(T).Name}) nije pronađen — stavka preskočena.");
            return null;
        }

        private static void DestroyAll<T>() where T : Component
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(c.gameObject);
        }

        private static Transform FindHub()
        {
            foreach (var p in UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p.transform;
            return null;
        }

        private static Transform ClosestPlanet(Vector3 pos)
        {
            Planet best = ClosestPlanetOf(pos, UnityEngine.Object.FindObjectsByType<Planet>(FindObjectsSortMode.None));
            return best != null ? best.transform : null;
        }

        private static Planet ClosestPlanetOf(Vector3 pos, Planet[] planets)
        {
            Planet best = null;
            float bestDist = float.MaxValue;
            foreach (var p in planets)
            {
                float d = (p.transform.position - pos).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        private static bool IsClosestPlanetHub(Vector3 pos, Planet[] planets)
        {
            Planet best = ClosestPlanetOf(pos, planets);
            return best != null && best.IsHub;
        }
    }
}
