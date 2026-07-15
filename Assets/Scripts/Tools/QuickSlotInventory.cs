using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    // 9 fiksnih slotova za alate (i buduće strojeve). Rude/materijali idu u InventorySystem, ne ovdje.
    public class QuickSlotInventory : MonoBehaviour
    {
        public const int SlotCount = 9;

        public static QuickSlotInventory current;

        [SerializeField] private QuickSlotItem[] slots = new QuickSlotItem[SlotCount];

        // Trajnost po slotu — alat je ScriptableObject asset, pa je slot jedina "instanca"
        private readonly int[] _durabilities = new int[SlotCount];

        private int _selectedIndex = -1;

        public int SelectedIndex => _selectedIndex;
        public bool IsFull => GetFirstEmptySlot() == -1;

        void Awake()
        {
            current = this;

            for (int i = 0; i < SlotCount; i++)
                if (slots[i] is Tool tool)
                    _durabilities[i] = tool.maxDurability;
        }

        void Update()
        {
            if (!GameManager.IsPlaying) return;

            for (int i = 0; i < SlotCount; i++)
            {
                if (DigitKeyPressed(i))
                {
                    SelectSlot(i);
                    break;
                }
            }
        }

        public QuickSlotItem GetSlot(int index) => index >= 0 && index < SlotCount ? slots[index] : null;

        public int GetDurability(int index) => index >= 0 && index < SlotCount ? _durabilities[index] : 0;

        public void SetDurability(int index, int value)
        {
            if (index < 0 || index >= SlotCount) return;
            _durabilities[index] = value;
        }

        public bool TryAdd(QuickSlotItem item, out int slotIndex)
        {
            slotIndex = GetFirstEmptySlot();
            if (slotIndex == -1) return false;

            slots[slotIndex] = item;
            if (item is Tool tool)
                _durabilities[slotIndex] = tool.maxDurability;

            GameEventBus.RaiseQuickSlotsChanged();
            return true;
        }

        // Prazni slot nakon što je stroj iz njega postavljen u svijet (ili se alat polomio).
        public void RemoveSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;

            slots[index] = null;
            _durabilities[index] = 0;
            if (index == _selectedIndex)
                PlayerToolSystem.current?.UnequipTool();

            GameEventBus.RaiseQuickSlotsChanged();
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;

            _selectedIndex = index;
            QuickSlotItem item = slots[index];

            if (item is Tool tool)
                PlayerToolSystem.current?.EquipTool(tool, index, _durabilities[index]);
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
            if (keyboard == null) return false;
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
