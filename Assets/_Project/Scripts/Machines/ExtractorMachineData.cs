using System;
using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    [CreateAssetMenu(fileName = "ExtractorMachine", menuName = "Machines/Extractor Machine")]
    public class ExtractorMachineData : QuickSlotItem
    {
        [Serializable]
        public struct OutputYield
        {
            public Item item;
            [Min(1)] public int amount;
        }

        public GameObject prefab;

        [Tooltip("Uniformni world scale vizuala pri postavljanju.")]
        [Min(0.01f)] public float worldScale = MachineFactory.ExtractorScale;

        [Tooltip("Resursi koje stroj proizvodi svaki ciklus — ne trebaju spawnovi na planeti.")]
        public OutputYield[] outputs;

        [Tooltip("Sekunde po ciklusu proizvodnje.")]
        [Min(0.1f)] public float extractionInterval = 15f;

        [Tooltip("Maksimalan broj resursa u internom spremištu; kad je puno, proizvodnja staje.")]
        [Min(1)] public int maxStored = 25;

        [Tooltip("Resursi koji se troše iz Hub storage-a svaki ciklus (prazno = besplatno).")]
        public ConnectionRequirement[] maintenanceCost;

        [Header("Kvar")]
        [Tooltip("Šansa (0–1) da se stroj pokvari po radnom ciklusu; 0 = nikad. Na nestabilnim planetama množi se s 3.")]
        [Range(0f, 1f)] public float breakdownChancePerCycle = 0.03f;
        [Tooltip("Resursi iz inventara igrača potrebni za popravak (E na polomljenom stroju). Prazno = besplatan popravak.")]
        public ConnectionRequirement[] repairCost;
    }

    // Premješteno iz ExtractorMachine.cs (konsolidacija malih datoteka, 31. 7. 2026.).
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
