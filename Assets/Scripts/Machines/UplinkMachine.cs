using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Igrač pritisne E da ubaci sve materijale iz inventara; stroj ih zatim postupno
    // šalje u Hub storage — odakle strojevi plaćaju održavanje, a veze svoje zahtjeve.
    public class UplinkMachine : BaseInteractable
    {
        [SerializeField] private UplinkMachineData data;

        private MachineState _state = MachineState.Idle;
        private float _timer;

        private readonly Dictionary<Item, InventoryItem> _bufferDict = new();
        private readonly List<InventoryItem> _buffer = new();

        public MachineState State => _state;
        public UplinkMachineData Data => data;
        public IReadOnlyList<InventoryItem> Buffer => _buffer;

        public override float HoldTime => 0f;

        public void Init(UplinkMachineData machineData)
        {
            data = machineData;
            _state = MachineState.Active;
        }

        void Update()
        {
            if (data == null || _state == MachineState.Broken) return;

            _timer += Time.deltaTime;
            if (_timer >= data.transmitInterval)
            {
                _timer = 0f;
                TryTransmit();
            }
        }

        private void TryTransmit()
        {
            if (_buffer.Count == 0 || HubStorage.current == null)
            {
                _state = MachineState.Idle;
                return;
            }

            int sent = 0;
            while (sent < data.itemsPerCycle && _buffer.Count > 0)
            {
                var inv = _buffer[0];
                if (!HubStorage.current.Add(inv.data))
                {
                    // Hub je pun — zadrži ostatak i pokušaj ponovo sljedeći ciklus.
                    _state = MachineState.Idle;
                    return;
                }

                inv.RemoveFromStack();
                if (inv.GetStackSize() == 0)
                {
                    _bufferDict.Remove(inv.data);
                    _buffer.RemoveAt(0);
                }
                sent++;
            }

            if (sent > 0)
                Debug.Log($"[{data.displayName}] Poslano {sent} resursa u Hub.");
            _state = MachineState.Active;
        }

        // Igrač pritisne E: svi materijali iz inventara idu u buffer za slanje.
        public override void Interact()
        {
            var playerInventory = InventorySystem.current;
            if (playerInventory == null) return;

            int deposited = 0;
            var items = new List<InventoryItem>(playerInventory.GetInventory());
            foreach (var inventoryItem in items)
            {
                int stack = inventoryItem.GetStackSize();
                for (int i = 0; i < stack; i++)
                {
                    AddToBuffer(inventoryItem.data);
                    playerInventory.Remove(inventoryItem.data);
                    deposited++;
                }
            }

            Debug.Log(deposited > 0
                ? $"[{data?.displayName}] Ubačeno {deposited} resursa za slanje u Hub."
                : $"[{data?.displayName}] Inventar je prazan — nema šta poslati.");
        }

        private void AddToBuffer(Item item)
        {
            if (_bufferDict.TryGetValue(item, out var existing))
                existing.AddToStack();
            else
            {
                var inv = new InventoryItem(item);
                _buffer.Add(inv);
                _bufferDict[item] = inv;
            }
        }
    }
}
