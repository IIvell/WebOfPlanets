using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Pasivno proizvodi resurse (iz atmosfere/tla) — za razliku od CollectorMachine
    // ne treba spawnove na planeti, ali može trošiti održavanje iz Hub storage-a.
    public class ExtractorMachine : BaseInteractable
    {
        [SerializeField] private ExtractorMachineData data;

        private Transform _planet;
        private MachineState _state = MachineState.Idle;
        private MachineBreakdown _breakdown;
        private float _timer;

        private readonly Dictionary<Item, InventoryItem> _dict = new();
        private readonly List<InventoryItem> _storedItems = new();

        public MachineState State => _state;
        public ExtractorMachineData Data => data;
        public Transform Planet => _planet;
        public IReadOnlyList<InventoryItem> StoredItems => _storedItems;

        public override float HoldTime => 0f;

        public void Init(ExtractorMachineData machineData, Transform planet)
        {
            data = machineData;
            _planet = planet;
            _state = MachineState.Active;
        }

        void Update()
        {
            if (data == null || _state == MachineState.Broken) return;

            EnsureBreakdown();

            _timer += Time.deltaTime;
            if (_timer >= data.extractionInterval)
            {
                _timer = 0f;
                TryCycle();
            }
        }

        private void TryCycle()
        {
            if (TotalStored() >= data.maxStored)
            {
                _state = MachineState.Idle;
                return;
            }

            if (_breakdown.RollBreakdown())
            {
                _state = MachineState.Broken;
                return;
            }

            if (!TryConsumeMaintenance())
            {
                _state = MachineState.Idle;
                Debug.Log($"[{data.displayName}] Nema resursa za održavanje — stroj čeka.");
                return;
            }

            Produce();
            _state = MachineState.Active;
        }

        private bool TryConsumeMaintenance()
        {
            if (GameManager.TestingMode) return true;
            if (data.maintenanceCost == null || data.maintenanceCost.Length == 0) return true;
            if (HubStorage.current == null) return false;

            foreach (var req in data.maintenanceCost)
            {
                if (req.item == null) continue;
                var inv = HubStorage.current.Get(req.item);
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }

            foreach (var req in data.maintenanceCost)
            {
                if (req.item == null) continue;
                for (int i = 0; i < req.amount; i++)
                    HubStorage.current.Remove(req.item);
            }

            return true;
        }

        private void Produce()
        {
            if (data.outputs == null) return;

            foreach (var output in data.outputs)
            {
                if (output.item == null) continue;
                for (int i = 0; i < output.amount; i++)
                {
                    if (TotalStored() >= data.maxStored) return;
                    StoreItem(output.item);
                }
            }
        }

        private int TotalStored()
        {
            int total = 0;
            foreach (var inv in _storedItems)
                total += inv.GetStackSize();
            return total;
        }

        // Lazy umjesto u Init-u da pokrije i eventualne scene-serijalizirane strojeve.
        private void EnsureBreakdown()
        {
            if (_breakdown == null)
                _breakdown = MachineBreakdown.Attach(gameObject, data.displayName, _planet,
                    data.breakdownChancePerCycle, data.repairCost);
        }

        // ── Save/load ─────────────────────────────────────────────────────────

        public void LoadStoredItem(Item item, int count)
        {
            if (item == null) return;
            for (int i = 0; i < count; i++)
                StoreItem(item);
        }

        // Vraća Broken stanje bez eventa/toasta.
        public void LoadBroken()
        {
            if (data == null) return;
            EnsureBreakdown();
            _breakdown.LoadBroken();
            _state = MachineState.Broken;
        }

        private void StoreItem(Item item)
        {
            if (_dict.TryGetValue(item, out var existing))
                existing.AddToStack();
            else
            {
                var inv = new InventoryItem(item);
                _storedItems.Add(inv);
                _dict[item] = inv;
            }
        }

        // Igrač pritisne E na stroju da preuzme sve proizvedene resurse;
        // na polomljenom stroju E umjesto toga pokušava popravak.
        public override void Interact()
        {
            if (_state == MachineState.Broken)
            {
                if (_breakdown != null && _breakdown.TryRepair())
                    _state = MachineState.Active;
                return;
            }

            if (InventorySystem.current == null) return;

            if (_storedItems.Count == 0)
            {
                Debug.Log($"[{data?.displayName}] Nema proizvedenih resursa.");
                return;
            }

            foreach (var inv in _storedItems)
                for (int i = 0; i < inv.GetStackSize(); i++)
                    InventorySystem.current.Add(inv.data);

            Debug.Log($"[{data?.displayName}] Preuzeto {_storedItems.Count} vrsta resursa.");
            _dict.Clear();
            _storedItems.Clear();
        }
    }
}
