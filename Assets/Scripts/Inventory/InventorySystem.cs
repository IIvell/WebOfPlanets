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

            // Zajednička točka svih ulaza resursa u inventar (kopanje, preuzimanje
            // iz strojeva/skladišta) — direktan poziv umjesto OnResourceCollected
            // eventa jer Item nema ResourceType (mapping po imenu je poznati hack).
            AudioManager.PlayResourcePickup();
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

        // ── Save/load ─────────────────────────────────────────────────────────

        public void ClearForLoad()
        {
            inventory.Clear();
            m_itemDictionary.Clear();
        }

        // Kao Add, ali cijeli stack odjednom i bez pickup zvuka po itemu.
        public void LoadItem(Item referenceData, int count)
        {
            if (referenceData == null || count <= 0) return;

            if (!m_itemDictionary.TryGetValue(referenceData, out InventoryItem value))
            {
                value = new InventoryItem(referenceData); // konstruktor već broji 1
                inventory.Add(value);
                m_itemDictionary.Add(referenceData, value);
                count--;
            }
            for (int i = 0; i < count; i++)
                value.AddToStack();
        }
    }
}
