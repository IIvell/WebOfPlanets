using System.Collections;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ItemInteractable : BaseInteractable
    {
        [SerializeField] private Item referenceItem;
        [SerializeField] private bool isPickup;
        [SerializeField] private bool destroyAfterPickup = true;

        private bool _regenerating;

        public override float HoldTime => !isPickup && referenceItem != null ? referenceItem.miningTime : 0f;
        public Item ReferenceItem => referenceItem;
        public bool IsPickup => isPickup;

        public override bool CanInteract
        {
            get
            {
                if (_regenerating) return false;
                if (isPickup) return true;
                if (referenceItem == null || referenceItem.requiredTool == null) return true;
                if (PlayerToolSystem.current == null) return false;
                var equipped = PlayerToolSystem.current.EquippedTool;
                if (equipped == null) return false;
                // Isti alat ili alat iste klase dovoljnog ranga
                return equipped == referenceItem.requiredTool ||
                       (equipped.toolClass == referenceItem.requiredTool.toolClass &&
                        equipped.miningTier >= referenceItem.requiredTool.miningTier);
            }
        }

        public void Init(Item item, bool pickup = false, bool destroy = true)
        {
            referenceItem = item;
            isPickup = pickup;
            destroyAfterPickup = destroy;
        }

        public override void Interact()
        {
            if (referenceItem == null)
            {
                Debug.LogWarning($"{name}: nema dodjeljenog Item asseta.");
                return;
            }

            int yieldCount = isPickup ? 1 : Random.Range(referenceItem.minMiningYield, referenceItem.maxMiningYield + 1);
            for (int i = 0; i < yieldCount; i++)
                InventorySystem.current.Add(referenceItem);

            if (!isPickup && referenceItem.bonusMiningItem != null && Random.value < referenceItem.bonusMiningChance)
                InventorySystem.current.Add(referenceItem.bonusMiningItem);

            PlayerToolSystem.current?.OnResourceMined();
            Debug.Log($"Picked up: {referenceItem.displayName} x{yieldCount}");

            if (referenceItem.regenerationTime > 0f)
                StartCoroutine(RegenerateAfter(referenceItem.regenerationTime));
            else if (destroyAfterPickup)
                Destroy(gameObject);
        }

        // Koristi stroj umjesto igrača — preskače tool provjeru, ne dodaje u player inventory
        public bool TryCollectByMachine(out Item collected)
        {
            collected = null;
            if (_regenerating || referenceItem == null) return false;

            collected = referenceItem;

            if (referenceItem.regenerationTime > 0f)
                StartCoroutine(RegenerateAfter(referenceItem.regenerationTime));
            else if (destroyAfterPickup)
                Destroy(gameObject);

            return true;
        }

        private IEnumerator RegenerateAfter(float seconds)
        {
            _regenerating = true;
            SetVisible(false);
            yield return new WaitForSeconds(seconds);
            SetVisible(true);
            _regenerating = false;
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = visible;
        }
    }
}
