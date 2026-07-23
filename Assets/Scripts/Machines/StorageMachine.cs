using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class StorageMachine : BaseInteractable
    {
        private StorageMachineData _data;
        private readonly Dictionary<Item, InventoryItem> _dict = new();
        private readonly List<InventoryItem> _inventory = new();

        public override float HoldTime => 0f;
        public IReadOnlyList<InventoryItem> Inventory => _inventory;
        public StorageMachineData Data => _data;
        public string MachineName => _data != null ? _data.displayName : "Storage Machine";
        // Fallback za scene-serijalizirani stroj bez data asseta — isti default kao u StorageMachineData.
        public int Capacity => _data != null ? _data.capacity : 60;
        public bool IsFull => TotalStored() >= Capacity;

        // Vezu collector->storage drži CollectorMachine (SetOutputStorage); storage
        // ne treba referencu natrag.
        public void Init(StorageMachineData data)
        {
            _data = data;
        }

        public int TotalStored()
        {
            int total = 0;
            foreach (var inv in _inventory)
                total += inv.GetStackSize();
            return total;
        }

        // false = kapacitet pun, item nije spremljen (collector tada pauzira).
        public bool Add(Item item)
        {
            if (IsFull) return false;

            if (_dict.TryGetValue(item, out var existing))
                existing.AddToStack();
            else
            {
                var inv = new InventoryItem(item);
                _inventory.Add(inv);
                _dict[item] = inv;
            }
            return true;
        }

        // Igrač pritisne E da otvori UI sa sadržajem storage-a
        public override void Interact()
        {
            if (StorageInventoryUI.Instance != null)
            {
                StorageInventoryUI.Instance.Show(this);
                return;
            }

            Debug.LogWarning($"[{MachineName}] StorageInventoryUI nije u sceni — uzimam sve odmah.");
            TakeAll();
        }

        public void TakeAll()
        {
            if (InventorySystem.current == null) return;

            if (_inventory.Count == 0)
            {
                Debug.Log($"[{MachineName}] Nema skupljenih resursa.");
                return;
            }

            foreach (var inv in _inventory)
                for (int i = 0; i < inv.GetStackSize(); i++)
                    InventorySystem.current.Add(inv.data);

            Debug.Log($"[{MachineName}] Preuzeto {_inventory.Count} vrsta resursa iz storage stroja.");
            _dict.Clear();
            _inventory.Clear();
        }
    }
}
