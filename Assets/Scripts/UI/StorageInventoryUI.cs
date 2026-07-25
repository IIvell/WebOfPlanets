using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    // Panel spremnika storage stroja (otvara StorageMachine); lista i
    // input-mode žive u bazi ItemListUI.
    public class StorageInventoryUI : ItemListUI
    {
        public static StorageInventoryUI Instance { get; private set; }

        private StorageMachine _target;

        protected override string PanelName => "StorageInventory_Panel";
        protected override string EmptyText => "Storage is empty.";
        protected override string BottomButtonLabel => "Take All";
        protected override Color BottomButtonColor => new Color(0.1f, 0.45f, 0.2f);

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        public void Show(StorageMachine storage)
        {
            if (storage == null) return;

            _target = storage;
            OpenPanel();
        }

        public override void Close()
        {
            base.Close();
            _target = null;
        }

        protected override void OnBottomButtonClicked()
        {
            if (_target == null) return;
            _target.TakeAll();
            Refresh();
        }

        protected override IReadOnlyList<InventoryItem> GetItems()
        {
            if (_target == null) return null;

            // Naslov nosi i popunjenost — mijenja se nakon Take All, zato u Refresh-u.
            _titleText.text = $"{_target.MachineName} ({_target.TotalStored()}/{_target.Capacity})";
            return _target.Inventory;
        }
    }
}
