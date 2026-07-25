using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    // Pasivno proizvodi resurse (iz atmosfere/tla) — za razliku od CollectorMachine
    // ne treba spawnove na planeti, ali može trošiti održavanje iz Hub storage-a.
    public class ExtractorMachine : ProductionMachine
    {
        [SerializeField] private ExtractorMachineData data;

        private Transform _planet;

        private readonly ItemStackList _stored = new();

        public ExtractorMachineData Data => data;
        public override Transform Planet => _planet;
        public IReadOnlyList<InventoryItem> StoredItems => _stored.Items;

        protected override bool HasData => data != null;
        protected override string DisplayName => data.displayName;
        protected override float CycleInterval => data.extractionInterval;
        protected override float BreakdownChancePerCycle => data.breakdownChancePerCycle;
        protected override ConnectionRequirement[] RepairCost => data.repairCost;

        public void Init(ExtractorMachineData machineData, Transform planet)
        {
            data = machineData;
            _planet = planet;
            _state = MachineState.Active;
        }

        protected override void TryCycle()
        {
            if (_stored.TotalStacked() >= data.maxStored)
            {
                _state = MachineState.Idle;
                return;
            }

            if (_breakdown.RollBreakdown())
            {
                _state = MachineState.Broken;
                return;
            }

            if (!TryConsumeMaintenance(data.maintenanceCost))
            {
                _state = MachineState.Idle;
                Debug.Log($"[{data.displayName}] Nema resursa za održavanje — stroj čeka.");
                return;
            }

            Produce();
            _state = MachineState.Active;
        }

        private void Produce()
        {
            if (data.outputs == null) return;

            foreach (var output in data.outputs)
            {
                if (output.item == null) continue;
                for (int i = 0; i < output.amount; i++)
                {
                    if (_stored.TotalStacked() >= data.maxStored) return;
                    _stored.Add(output.item);
                }
            }
        }

        // ── Save/load ─────────────────────────────────────────────────────────

        public void LoadStoredItem(Item item, int count)
        {
            if (item == null) return;
            for (int i = 0; i < count; i++)
                _stored.Add(item);
        }

        // Igrač pritisne E na stroju da preuzme sve proizvedene resurse;
        // na polomljenom stroju E umjesto toga pokušava popravak.
        public override void Interact()
        {
            if (TryRepairInteract()) return;

            if (InventorySystem.Instance == null) return;

            if (_stored.Count == 0)
            {
                Debug.Log($"[{data?.displayName}] Nema proizvedenih resursa.");
                return;
            }

            int kinds = _stored.Count;
            TransferAllToPlayer(_stored);
            Debug.Log($"[{data?.displayName}] Preuzeto {kinds} vrsta resursa.");
        }
    }
}
