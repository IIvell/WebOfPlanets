using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class HubStorage : MonoBehaviour
    {
        public static HubStorage current;

        [SerializeField] private int maxCapacity = 100;

        private Dictionary<Item, InventoryItem> m_itemDictionary;
        [SerializeField] private List<InventoryItem> inventory = new List<InventoryItem>();

        public int MaxCapacity => maxCapacity;

        void Awake()
        {
            current = this;
            m_itemDictionary = new Dictionary<Item, InventoryItem>();
        }

        public List<InventoryItem> GetInventory() => inventory;

        public int TotalCount()
        {
            int total = 0;
            foreach (var item in inventory)
                total += item.GetStackSize();
            return total;
        }

        public bool IsFull() => TotalCount() >= maxCapacity;

        public InventoryItem Get(Item referenceData)
        {
            m_itemDictionary.TryGetValue(referenceData, out InventoryItem value);
            return value;
        }

        // Returns true if item was added, false if storage is full.
        public bool Add(Item referenceData)
        {
            if (IsFull())
            {
                GameEventBus.RaiseStorageFull(ResourceType.Ore);
                return false;
            }

            if (m_itemDictionary.TryGetValue(referenceData, out InventoryItem value))
            {
                value.AddToStack();
            }
            else
            {
                InventoryItem newItem = new InventoryItem(referenceData);
                inventory.Add(newItem);
                m_itemDictionary.Add(referenceData, newItem);
            }

            return true;
        }

        public void Remove(Item referenceData)
        {
            if (m_itemDictionary.TryGetValue(referenceData, out InventoryItem value))
            {
                value.RemoveFromStack();

                if (value.GetStackSize() == 0)
                {
                    inventory.Remove(value);
                    m_itemDictionary.Remove(referenceData);
                }
            }
        }
    }
}
