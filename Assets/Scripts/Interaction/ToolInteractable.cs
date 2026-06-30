using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ToolInteractable : BaseInteractable
    {
        [SerializeField] private Tool tool;

        public override float HoldTime => 0f;

        public void Init(Tool t) => tool = t;

        public override void Interact()
        {
            if (tool == null)
            {
                Debug.LogWarning($"{name}: nema dodjeljenog Tool asseta.");
                return;
            }

            if (QuickSlotInventory.current == null || !QuickSlotInventory.current.TryAdd(tool, out _))
            {
                Debug.Log("Quick slot inventar je pun — alat se ne može pokupiti.");
                return;
            }

            Debug.Log($"Pokupljen alat: {tool.displayName}");
            Destroy(gameObject);
        }
    }
}
