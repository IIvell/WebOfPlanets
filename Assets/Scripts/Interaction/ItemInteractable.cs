using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ItemInteractable : BaseInteractable
    {
        [SerializeField] private Item referenceItem;
        [SerializeField] private bool destroyAfterPickup = true;

        public override float HoldTime => referenceItem != null ? referenceItem.miningTime : 0f;

        public void Init(Item item, bool destroy = true)
        {
            referenceItem = item;
            destroyAfterPickup = destroy;
        }

        public override void Interact()
        {
            if (referenceItem == null)
            {
                Debug.LogWarning($"{name}: nema dodjeljenog Item asseta.");
                return;
            }

            InventorySystem.current.Add(referenceItem);
            PlayerToolSystem.current?.OnResourceMined();
            Debug.Log($"Picked up: {referenceItem.displayName}");

            if (destroyAfterPickup)
                Destroy(gameObject);
        }
    }
}
