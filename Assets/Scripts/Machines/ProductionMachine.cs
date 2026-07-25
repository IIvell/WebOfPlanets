using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Zajednički kostur četiri proizvodna stroja (Collector/Smelter/Extractor/
    // Uplink): timer-ciklus u Update-u, vezanje MachineBreakdown komponente,
    // popravak s E, povratak Broken stanja iz save-a, trošak održavanja i
    // dict+lista spremnik. Prije baze je taj kostur bio ručno kopiran u sva
    // četiri stroja i već je počeo divergirati.
    //
    // SO tipovi podataka NEMAJU zajedničku baznu klasu i ne smiju je dobiti
    // (MachinePlacer ih razlikuje pattern-matchingom po tipu), pa baza do
    // vrijednosti dolazi kroz apstraktne accessore, a serijalizirana polja
    // (data, planet) ostaju u podklasama — ništa serijalizirano se ne mijenja.
    //
    // NAMJERNE razlike ostaju u podklasama i baza ih ne unificira: Smelter i
    // Uplink bacaju kocku kvara samo dok stvarno rade (prazan stroj se ne
    // troši), Collector/Extractor svaki ciklus; pauze (pun storage, prazan
    // buffer, pun hub) su semantika svakog stroja u njegovom TryCycle.
    public abstract class ProductionMachine : BaseInteractable
    {
        protected MachineState _state = MachineState.Idle;
        protected MachineBreakdown _breakdown;
        private float _timer;

        public MachineState State => _state;
        public override float HoldTime => 0f;

        // Vrijednosti iz konkretnog SO-a podataka.
        protected abstract bool HasData { get; }
        protected abstract string DisplayName { get; }
        protected abstract float CycleInterval { get; }
        protected abstract float BreakdownChancePerCycle { get; }
        protected abstract ConnectionRequirement[] RepairCost { get; }
        public abstract Transform Planet { get; }

        // Jedan radni ciklus stroja; poziva se svakih CycleInterval sekundi.
        protected abstract void TryCycle();

        // Collector dodatno traži planet != null.
        protected virtual bool ReadyForCycle() => HasData && _state != MachineState.Broken;

        void Update()
        {
            if (!ReadyForCycle()) return;

            EnsureBreakdown();

            _timer += Time.deltaTime;
            if (_timer >= CycleInterval)
            {
                _timer = 0f;
                TryCycle();
            }
        }

        // Lazy umjesto u Init-u da pokrije i eventualne scene-serijalizirane strojeve.
        protected void EnsureBreakdown()
        {
            if (_breakdown == null)
                _breakdown = MachineBreakdown.Attach(gameObject, DisplayName, Planet,
                    BreakdownChancePerCycle, RepairCost);
        }

        // E na polomljenom stroju pokušava popravak umjesto redovne interakcije;
        // true = interakcija time potrošena (stroj je bio polomljen).
        protected bool TryRepairInteract()
        {
            if (_state != MachineState.Broken) return false;
            if (_breakdown != null && _breakdown.TryRepair())
                _state = MachineState.Active;
            return true;
        }

        // Vraća Broken stanje iz save datoteke bez eventa/toasta.
        public void LoadBroken()
        {
            if (!HasData) return;
            EnsureBreakdown();
            _breakdown.LoadBroken();
            _state = MachineState.Broken;
        }

        // Dvofazni trošak održavanja iz Hub storage-a (prvo provjeri sve, pa
        // potroši sve). TestingMode i prazan trošak prolaze besplatno; bez hub
        // skladišta ne prolazi ništa (fail-closed — semantika zadržana iz
        // Collectora/Extractora koji su ovu metodu ranije nosili kao kopije).
        protected static bool TryConsumeMaintenance(ConnectionRequirement[] cost)
        {
            if (GameManager.TestingMode) return true;
            if (cost == null || cost.Length == 0) return true;
            if (HubStorage.Instance == null) return false;

            foreach (var req in cost)
            {
                if (req.item == null) continue;
                var inv = HubStorage.Instance.Get(req.item);
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }

            foreach (var req in cost)
            {
                if (req.item == null) continue;
                for (int i = 0; i < req.amount; i++)
                    HubStorage.Instance.Remove(req.item);
            }

            return true;
        }

        // ── Spremnik stroja ───────────────────────────────────────────────────

        // Par lista+dictionary: lista čuva redoslijed (UI, save, slanje s početka),
        // dictionary daje brzi lookup za stackiranje po Itemu. Prije je isti "add"
        // blok postojao u pet ručnih kopija po strojevima.
        protected sealed class ItemStackList
        {
            private readonly Dictionary<Item, InventoryItem> _dict = new();
            private readonly List<InventoryItem> _list = new();

            public IReadOnlyList<InventoryItem> Items => _list;
            public int Count => _list.Count;
            public InventoryItem First => _list[0];

            public void Add(Item item)
            {
                if (_dict.TryGetValue(item, out var existing))
                    existing.AddToStack();
                else
                {
                    var inv = new InventoryItem(item);
                    _list.Add(inv);
                    _dict[item] = inv;
                }
            }

            // Potroši amount komada ili ništa (smelter recepti).
            public bool TryConsume(Item item, int amount)
            {
                if (!_dict.TryGetValue(item, out var existing) || existing.GetStackSize() < amount)
                    return false;

                for (int i = 0; i < amount; i++)
                    existing.RemoveFromStack();

                if (existing.GetStackSize() == 0)
                {
                    _list.Remove(existing);
                    _dict.Remove(item);
                }

                return true;
            }

            // Skine jedan komad s prvog stacka u listi (uplink šalje redom).
            public void RemoveOneFromFirst()
            {
                var inv = _list[0];
                inv.RemoveFromStack();
                if (inv.GetStackSize() == 0)
                {
                    _dict.Remove(inv.data);
                    _list.RemoveAt(0);
                }
            }

            // Ukupan broj komada preko svih stackova (extractor kapacitet).
            public int TotalStacked()
            {
                int total = 0;
                foreach (var inv in _list)
                    total += inv.GetStackSize();
                return total;
            }

            public void Clear()
            {
                _dict.Clear();
                _list.Clear();
            }
        }

        // "Preuzmi sve": sav sadržaj spremnika u inventar igrača, pa isprazni.
        protected static void TransferAllToPlayer(ItemStackList stacks)
        {
            foreach (var inv in stacks.Items)
                for (int i = 0; i < inv.GetStackSize(); i++)
                    InventorySystem.Instance.Add(inv.data);
            stacks.Clear();
        }

        // "Ubaci sve iz inventara igrača" u spremnik stroja (Smelter filtrira po
        // receptima, Uplink uzima sve); vraća broj ubačenih komada.
        protected static int DepositAllFromPlayer(ItemStackList target, System.Predicate<Item> accepts = null)
        {
            var playerInventory = InventorySystem.Instance;
            if (playerInventory == null) return 0;

            int deposited = 0;
            var items = new List<InventoryItem>(playerInventory.GetInventory());
            foreach (var inventoryItem in items)
            {
                if (accepts != null && !accepts(inventoryItem.data)) continue;

                int stack = inventoryItem.GetStackSize();
                for (int i = 0; i < stack; i++)
                {
                    target.Add(inventoryItem.data);
                    playerInventory.Remove(inventoryItem.data);
                    deposited++;
                }
            }
            return deposited;
        }
    }
}
