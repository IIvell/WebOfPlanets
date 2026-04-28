using System;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [Serializable]
    public class InventoryItem
    {
        public Item data { get; private set; }

        [SerializeField] private int stackSize;
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        public InventoryItem(Item source)
        {
            data = source;
            id = source.id;
            displayName = source.displayName;
            AddToStack();
        }

        public void AddToStack() => stackSize++;
        public void RemoveFromStack() => stackSize--;
        public int GetStackSize() => stackSize;
    }
}
