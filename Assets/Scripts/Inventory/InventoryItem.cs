using System;
using UnityEngine;

namespace WebOfPlanets
{
    [Serializable]
    public class InventoryItem
    {
        public Item data { get; private set; }

        [SerializeField] private int stackSize;

        public InventoryItem(Item source)
        {
            data = source;
            AddToStack();
        }

        public void AddToStack() => stackSize++;
        public void RemoveFromStack() => stackSize--;
        public int GetStackSize() => stackSize;
    }
}
