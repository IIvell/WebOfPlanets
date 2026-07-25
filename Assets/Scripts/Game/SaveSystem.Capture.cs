using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WebOfPlanets
{
    // Snimanje stanja igre u SaveData (Save + Gather* + linkovi po indeksima).
    public static partial class SaveSystem
    {
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

            if (HubStorage.Instance != null)
                foreach (var inv in HubStorage.Instance.GetInventory())
                    if (inv.data != null)
                        data.hubStorage.Add(new ItemCountSave { item = inv.data.name, count = inv.GetStackSize() });

            if (InventorySystem.Instance != null)
                foreach (var inv in InventorySystem.Instance.GetInventory())
                    if (inv.data != null)
                        data.inventory.Add(new ItemCountSave { item = inv.data.name, count = inv.GetStackSize() });

            var qs = QuickSlotInventory.Instance;
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

            // Hub Računalo je scenski objekt (NetworkComputerInteractable) — sprema se
            // samo postavljeno ComputerMachine.
            foreach (var c in UnityEngine.Object.FindObjectsByType<ComputerMachine>(FindObjectsSortMode.None))
                if (c.Planet != null)
                    Add(c, new MachineSave
                    {
                        kind = KindComputer,
                        data = c.Data != null ? c.Data.name : "",
                        planet = c.Planet.name
                    });

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
    }
}
