using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class HubStorageInteractable : BaseInteractable
    {
        public override void Interact()
        {
            if (HubStorage.current == null)
            {
                Debug.LogWarning("HubStorageInteractable: nema HubStorage instance u sceni.");
                return;
            }

            var playerInventory = InventorySystem.current;
            if (playerInventory == null) return;

            // Copy list because we modify it during iteration.
            var items = new List<InventoryItem>(playerInventory.GetInventory());
            int deposited = 0;

            foreach (var inventoryItem in items)
            {
                int stack = inventoryItem.GetStackSize();
                for (int i = 0; i < stack; i++)
                {
                    if (!HubStorage.current.Add(inventoryItem.data))
                    {
                        Debug.Log($"Hub storage pun ({HubStorage.current.MaxCapacity} mjesta). Preneseno {deposited} predmeta.");
                        return;
                    }
                    playerInventory.Remove(inventoryItem.data);
                    deposited++;
                }
            }

            Debug.Log($"Deposited {deposited} item(s) into hub storage. ({HubStorage.current.TotalCount()}/{HubStorage.current.MaxCapacity})");
        }
    }
}
