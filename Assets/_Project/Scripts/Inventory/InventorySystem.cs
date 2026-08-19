using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        private Dictionary<Item, InventoryItem> _itemDictionary;

        [SerializeField]
        private List<InventoryItem> inventory = new List<InventoryItem>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _itemDictionary = new Dictionary<Item, InventoryItem>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public List<InventoryItem> GetInventory() => inventory;

        public InventoryItem Get(Item referenceData)
        {
            _itemDictionary.TryGetValue(referenceData, out InventoryItem value);
            return value;
        }

        public void Add(Item referenceData)
        {
            if (_itemDictionary.TryGetValue(referenceData, out InventoryItem value))
            {
                value.AddToStack();
            }
            else
            {
                InventoryItem newItem = new InventoryItem(referenceData);
                inventory.Add(newItem);
                _itemDictionary.Add(referenceData, newItem);
            }

            // Zajednička točka svih ulaza resursa u inventar (kopanje, preuzimanje
            // iz strojeva/skladišta). Bus event umjesto ranijeg direktnog poziva
            // AudioManagera — domenska klasa ne smije compile-time ovisiti o audiju.
            GameEventBus.RaiseInventoryItemAdded();
        }

        public void Remove(Item referenceData)
        {
            if (_itemDictionary.TryGetValue(referenceData, out InventoryItem value))
            {
                value.RemoveFromStack();

                if (value.GetStackSize() == 0)
                {
                    inventory.Remove(value);
                    _itemDictionary.Remove(referenceData);
                }
            }
        }

        // Kazna za smrt (kolovoz 2026.): igrač gubi pola svakog stacka, zaokruženo
        // PREMA GORE (7 → gubi 4, ostaje 3; 9 → gubi 5; 1 → gubi 1). Poziva
        // GameManager.HandlePlayerDied; TestingMode se provjerava tamo, ne ovdje.
        public void RemoveHalfOfEachStack()
        {
            for (int i = inventory.Count - 1; i >= 0; i--)
            {
                InventoryItem item = inventory[i];
                int loss = (item.GetStackSize() + 1) / 2;
                for (int j = 0; j < loss; j++)
                    item.RemoveFromStack();

                if (item.GetStackSize() == 0)
                {
                    inventory.RemoveAt(i);
                    _itemDictionary.Remove(item.data);
                }
            }
        }

        // ── Save/load ─────────────────────────────────────────────────────────

        public void ClearForLoad()
        {
            inventory.Clear();
            _itemDictionary.Clear();
        }

        // Kao Add, ali cijeli stack odjednom i bez pickup zvuka po itemu.
        public void LoadItem(Item referenceData, int count)
        {
            if (referenceData == null || count <= 0) return;

            if (!_itemDictionary.TryGetValue(referenceData, out InventoryItem value))
            {
                value = new InventoryItem(referenceData); // konstruktor već broji 1
                inventory.Add(value);
                _itemDictionary.Add(referenceData, value);
                count--;
            }
            for (int i = 0; i < count; i++)
                value.AddToStack();
        }
    }
}
