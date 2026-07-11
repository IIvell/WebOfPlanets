using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlayerToolSystem : MonoBehaviour
    {
        public static PlayerToolSystem current;

        [Tooltip("Transform djeteta na playeru koji označava gdje se alat drži (npr. desna ruka).")]
        [SerializeField] private Transform toolHoldPoint;

        private Tool _equippedTool;
        private int _currentDurability;
        private GameObject _toolVisual;

        public Tool EquippedTool => _equippedTool;
        public int CurrentDurability => _currentDurability;
        public bool HasTool => _equippedTool != null;

        void Awake()
        {
            current = this;
        }

        public static float GetSpeedMultiplier()
        {
            return current != null && current._equippedTool != null
                ? current._equippedTool.miningSpeedMultiplier
                : 1f;
        }

        public void EquipTool(Tool tool)
        {
            DestroyToolVisual();

            _equippedTool = tool;
            _currentDurability = tool.maxDurability;

            SpawnToolVisual(tool);

            GameEventBus.RaiseToolEquipped(new ToolEquippedEvent
            {
                ToolName = tool.displayName,
                SpeedMultiplier = tool.miningSpeedMultiplier,
                CurrentDurability = _currentDurability,
                MaxDurability = tool.maxDurability
            });
        }

        public void UnequipTool()
        {
            DestroyToolVisual();

            _equippedTool = null;
            _currentDurability = 0;
            GameEventBus.RaiseToolEquipped(new ToolEquippedEvent
            {
                ToolName = null,
                SpeedMultiplier = 1f,
                CurrentDurability = 0,
                MaxDurability = 0
            });
        }

        private void SpawnToolVisual(Tool tool)
        {
            if (tool.prefab == null || toolHoldPoint == null) return;

            _toolVisual = Instantiate(tool.prefab, toolHoldPoint);
            _toolVisual.transform.localPosition = tool.holdPositionOffset;
            _toolVisual.transform.localRotation = Quaternion.Euler(tool.holdRotationOffset);
            _toolVisual.transform.localScale = Vector3.one * tool.holdScale;

            // Ukloni sve kolajdere i interactable komponente sa vizuala
            foreach (var col in _toolVisual.GetComponentsInChildren<Collider>())
                col.enabled = false;
            foreach (var interactable in _toolVisual.GetComponentsInChildren<BaseInteractable>())
                Destroy(interactable);
        }

        private void DestroyToolVisual()
        {
            if (_toolVisual != null)
            {
                Destroy(_toolVisual);
                _toolVisual = null;
            }
        }

        // Poziva se svaki put kad igrač skupi resurs
        public void OnResourceMined()
        {
            if (_equippedTool == null || _equippedTool.maxDurability == 0) return;

            _currentDurability--;
            GameEventBus.RaiseToolDurabilityChanged(new ToolDurabilityEvent
            {
                Current = _currentDurability,
                Max = _equippedTool.maxDurability
            });

            if (_currentDurability <= 0)
            {
                Debug.Log($"Alat '{_equippedTool.displayName}' se polomio!");
                UnequipTool();
            }
        }
    }
}
