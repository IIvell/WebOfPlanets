using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WebOfPlanets
{
    // Inventar igrača (I); lista i input-mode žive u bazi ItemListUI.
    public class InventoryUI : ItemListUI
    {
        protected override string PanelName => "Inventory_Panel";
        protected override string EmptyText => "Inventory empty.";
        protected override string InitialTitle => "INVENTORY";
        protected override string HintText => $"{GameKeys.InventoryName} / {GameKeys.CancelName} — close";

        protected override void Update()
        {
            if (Keyboard.current == null) return;

            // Gate kao Interactor/MachinePlacer: bez otvaranja u pauzi/game overu
            // (Close bi zaključao kursor i upalio input usred main menija).
            if (GameManager.IsPlaying && GameKeys.WasPressed(GameKeys.Inventory))
            {
                if (_isOpen) Close(); else Open();
                return;
            }

            base.Update();
        }

        public void Open() => OpenPanel();

        protected override IReadOnlyList<InventoryItem> GetItems()
            => InventorySystem.Instance?.GetInventory();
    }
}
