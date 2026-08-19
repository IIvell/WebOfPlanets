using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    public class HubStorageInteractable : BaseInteractable
    {
        public override void Interact()
        {
            if (HubStorageUI.Instance != null)
            {
                HubStorageUI.Instance.Show(this);
                return;
            }

            Debug.LogWarning("HubStorageInteractable: HubStorageUI nije u sceni — deponiram sve odmah.");
            DepositAll();
        }

        public void DepositAll()
        {
            if (HubStorage.Instance == null)
            {
                Debug.LogWarning("HubStorageInteractable: nema HubStorage instance u sceni.");
                return;
            }

            var playerInventory = InventorySystem.Instance;
            if (playerInventory == null) return;

            // Copy list because we modify it during iteration.
            var items = new List<InventoryItem>(playerInventory.GetInventory());
            int deposited = 0;

            foreach (var inventoryItem in items)
            {
                int stack = inventoryItem.GetStackSize();
                for (int i = 0; i < stack; i++)
                {
                    if (!HubStorage.Instance.Add(inventoryItem.data))
                    {
                        Debug.Log($"Hub storage pun ({HubStorage.Instance.MaxCapacity} mjesta). Preneseno {deposited} predmeta.");
                        return;
                    }
                    playerInventory.Remove(inventoryItem.data);
                    deposited++;
                }
            }

            Debug.Log($"Deposited {deposited} item(s) into hub storage. ({HubStorage.Instance.TotalCount()}/{HubStorage.Instance.MaxCapacity})");
        }

        // Vraća SVE iz hub skladišta u inventar igrača (kolovoz 2026.) — zrcalo
        // DepositAll. Inventar igrača nema kapacitet, pa prijenos ne može stati
        // na pola. LoadItem umjesto Add: cijeli stack odjednom, bez pickup zvuka
        // po itemu (isti razlog kao kod save load-a).
        public void WithdrawAll()
        {
            if (HubStorage.Instance == null)
            {
                Debug.LogWarning("HubStorageInteractable: nema HubStorage instance u sceni.");
                return;
            }

            var playerInventory = InventorySystem.Instance;
            if (playerInventory == null) return;

            // Kopija liste jer se mijenja tijekom iteracije.
            var items = new List<InventoryItem>(HubStorage.Instance.GetInventory());
            int withdrawn = 0;

            foreach (var storageItem in items)
            {
                int stack = storageItem.GetStackSize();
                playerInventory.LoadItem(storageItem.data, stack);
                for (int i = 0; i < stack; i++)
                    HubStorage.Instance.Remove(storageItem.data);
                withdrawn += stack;
            }

            Debug.Log($"Withdrawn {withdrawn} item(s) from hub storage. ({HubStorage.Instance.TotalCount()}/{HubStorage.Instance.MaxCapacity})");
        }
    }
}
