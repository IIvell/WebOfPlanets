using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    public class HubStorage : MonoBehaviour
    {
        public static HubStorage Instance { get; private set; }

        [SerializeField] private int maxCapacity = 100;

        private Dictionary<Item, InventoryItem> _itemDictionary;
        [SerializeField] private List<InventoryItem> inventory = new List<InventoryItem>();

        // Bazni kapacitet + bonus otključanih hub pragova.
        public int MaxCapacity => maxCapacity + HubProgress.StorageBonus;

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

        public int TotalCount()
        {
            int total = 0;
            foreach (var item in inventory)
                total += item.GetStackSize();
            return total;
        }

        public bool IsFull() => TotalCount() >= MaxCapacity;

        public InventoryItem Get(Item referenceData)
        {
            _itemDictionary.TryGetValue(referenceData, out InventoryItem value);
            return value;
        }

        // Returns true if item was added, false if storage is full.
        public bool Add(Item referenceData)
        {
            if (IsFull())
            {
                GameEventBus.RaiseStorageFull(GetResourceType(referenceData));
                return false;
            }

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

            return true;
        }

        // Eksplicitna vrsta s asseta (Item.resourceKind) ima prednost; Auto
        // zadržava naslijeđeno izvođenje iz naming konvencije asseta
        // ("Kategorija_naziv", npr. Mining_ore, Organic_wood). Fallback je Ore.
        private static ResourceType GetResourceType(Item item)
        {
            if (item == null) return ResourceType.Ore;

            switch (item.resourceKind)
            {
                case ResourceKind.Ore:            return ResourceType.Ore;
                case ResourceKind.Biomass:        return ResourceType.Biomass;
                case ResourceKind.Ice:            return ResourceType.Ice;
                case ResourceKind.Gas:            return ResourceType.Gas;
                case ResourceKind.VolcanicMatter: return ResourceType.VolcanicMatter;
            }

            string assetName = item.name;
            if (assetName.StartsWith("Organic"))  return ResourceType.Biomass;
            if (assetName.StartsWith("Water"))    return ResourceType.Ice;
            if (assetName.StartsWith("Gaseous"))  return ResourceType.Gas;
            if (assetName.StartsWith("Volcanic")) return ResourceType.VolcanicMatter;
            return ResourceType.Ore; // Mining_*, Metal_* i sve ostalo
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

        // ── Save/load ─────────────────────────────────────────────────────────

        public void ClearForLoad()
        {
            inventory.Clear();
            _itemDictionary.Clear();
        }

        // Kao Add, ali cijeli stack odjednom i bez StorageFull eventa — save je
        // kapacitet već poštivao pri spremanju.
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
