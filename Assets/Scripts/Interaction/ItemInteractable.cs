using System.Collections;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ItemInteractable : BaseInteractable
    {
        [SerializeField] private Item referenceItem;
        [SerializeField] private bool destroyAfterPickup = true;

        private bool _regenerating;

        public override float HoldTime => referenceItem != null ? referenceItem.miningTime : 0f;
        public Item ReferenceItem => referenceItem;

        public override bool CanInteract
        {
            get
            {
                if (_regenerating) return false;
                if (referenceItem == null || referenceItem.requiredTool == null) return true;
                return PlayerToolSystem.current != null &&
                       PlayerToolSystem.current.EquippedTool == referenceItem.requiredTool;
            }
        }

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
