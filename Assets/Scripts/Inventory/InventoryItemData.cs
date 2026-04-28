using System;

namespace xyz.germanfica.unity.planet.gravity
{
    [Serializable]
    public class InventoryItemData
    {
        public string itemId;
        public int stackSize;

        public InventoryItemData(string itemId, int stackSize)
        {
            this.itemId = itemId;
            this.stackSize = stackSize;
        }
    }
}
