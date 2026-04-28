using System;

namespace xyz.germanfica.unity.planet.gravity
{
    [Serializable]
    public class InventoryData
    {
        public InventoryItemData[] items;

        public InventoryData(InventoryItemData[] items)
        {
            this.items = items;
        }
    }
}
