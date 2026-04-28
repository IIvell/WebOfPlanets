using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem current;

        private Dictionary<Item, InventoryItem> m_itemDictionary;

        [SerializeField]
        private List<InventoryItem> inventory = new List<InventoryItem>();

        void Awake()
        {
            current = this;
            m_itemDictionary = new Dictionary<Item, InventoryItem>();
        }

        public List<InventoryItem> GetInventory() => inventory;

        public InventoryItem Get(Item referenceData)
        {
            m_itemDictionary.TryGetValue(referenceData, out InventoryItem value);
            return value;
        }

        public void Add(Item referenceData)
        {
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
