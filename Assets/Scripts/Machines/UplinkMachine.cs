using UnityEngine;
using System.Collections.Generic;

namespace xyz.germanfica.unity.planet.gravity
{
    // Igrač pritisne E da ubaci sve materijale iz inventara; stroj ih zatim postupno
    // šalje u Hub storage — odakle strojevi plaćaju održavanje, a veze svoje zahtjeve.
    public class UplinkMachine : ProductionMachine
    {
        [SerializeField] private UplinkMachineData data;

        private Transform _planet;

        private readonly ItemStackList _buffer = new();

        public UplinkMachineData Data => data;
        public override Transform Planet => _planet;
        public IReadOnlyList<InventoryItem> Buffer => _buffer.Items;

        protected override bool HasData => data != null;
        protected override string DisplayName => data.displayName;
        protected override float CycleInterval => data.transmitInterval;
        protected override float BreakdownChancePerCycle => data.breakdownChancePerCycle;
        protected override ConnectionRequirement[] RepairCost => data.repairCost;

        public void Init(UplinkMachineData machineData, Transform planet)
        {
            data = machineData;
            _planet = planet;
            _state = MachineState.Active;
        }

        protected override void TryCycle()
        {
            if (_buffer.Count == 0 || HubStorage.Instance == null)
            {
                _state = MachineState.Idle;
                return;
            }

            // Kvar samo dok stroj stvarno šalje — prazan uplink se ne troši.
            if (_breakdown.RollBreakdown())
            {
                _state = MachineState.Broken;
                return;
            }

            int sent = 0;
            while (sent < data.itemsPerCycle && _buffer.Count > 0)
            {
                var inv = _buffer.First;
                if (!HubStorage.Instance.Add(inv.data))
                {
                    // Hub je pun — zadrži ostatak i pokušaj ponovo sljedeći ciklus.
                    _state = MachineState.Idle;
                    return;
                }

                _buffer.RemoveOneFromFirst();
                sent++;
            }

            if (sent > 0)
                Debug.Log($"[{data.displayName}] Poslano {sent} resursa u Hub.");
            _state = MachineState.Active;
        }

        // Igrač pritisne E: svi materijali iz inventara idu u buffer za slanje.
        // Na polomljenom stroju E umjesto toga pokušava popravak.
        public override void Interact()
        {
            if (TryRepairInteract()) return;

            if (InventorySystem.Instance == null) return;

            int deposited = DepositAllFromPlayer(_buffer);

            Debug.Log(deposited > 0
                ? $"[{data?.displayName}] Ubačeno {deposited} resursa za slanje u Hub."
                : $"[{data?.displayName}] Inventar je prazan — nema šta poslati.");
        }

        // ── Save/load ─────────────────────────────────────────────────────────

        public void LoadBufferItem(Item item, int count)
        {
            if (item == null) return;
            for (int i = 0; i < count; i++)
                _buffer.Add(item);
        }
    }
}
