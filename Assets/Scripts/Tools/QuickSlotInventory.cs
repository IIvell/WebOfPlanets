using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    // 9 fiksnih slotova za alate (i buduće strojeve). Rude/materijali idu u InventorySystem, ne ovdje.
    public class QuickSlotInventory : MonoBehaviour
    {
        public const int SlotCount = 9;

        public static QuickSlotInventory current;

        [SerializeField] private Tool[] slots = new Tool[SlotCount];

        private int _selectedIndex = -1;

        public int SelectedIndex => _selectedIndex;
        public bool IsFull => GetFirstEmptySlot() == -1;

        void Awake()
        {
            current = this;
        }

        void Update()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (DigitKeyPressed(i))
                {
                    SelectSlot(i);
                    break;
                }
            }
        }

        public Tool GetSlot(int index) => index >= 0 && index < SlotCount ? slots[index] : null;

        public bool TryAdd(Tool tool, out int slotIndex)
        {
            slotIndex = GetFirstEmptySlot();
            if (slotIndex == -1) return false;

            slots[slotIndex] = tool;
            GameEventBus.RaiseQuickSlotsChanged();
            return true;
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;

            _selectedIndex = index;
            Tool tool = slots[index];

            if (tool != null)
                PlayerToolSystem.current?.EquipTool(tool);
            else
                PlayerToolSystem.current?.UnequipTool();

            GameEventBus.RaiseQuickSlotsChanged();
        }

        private int GetFirstEmptySlot()
        {
            for (int i = 0; i < SlotCount; i++)
                if (slots[i] == null) return i;
            return -1;
        }

        private static bool DigitKeyPressed(int slotIndex)
        {
            var keyboard = Keyboard.current;
            return slotIndex switch
            {
                0 => keyboard.digit1Key.wasPressedThisFrame,
                1 => keyboard.digit2Key.wasPressedThisFrame,
                2 => keyboard.digit3Key.wasPressedThisFrame,
                3 => keyboard.digit4Key.wasPressedThisFrame,
                4 => keyboard.digit5Key.wasPressedThisFrame,
                5 => keyboard.digit6Key.wasPressedThisFrame,
                6 => keyboard.digit7Key.wasPressedThisFrame,
                7 => keyboard.digit8Key.wasPressedThisFrame,
                8 => keyboard.digit9Key.wasPressedThisFrame,
                _ => false
            };
        }
    }
}
