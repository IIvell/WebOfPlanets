using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Panel hub skladišta (otvara HubStorageInteractable); lista i input-mode
    // žive u bazi ItemListUI.
    public class HubStorageUI : ItemListUI
    {
        public static HubStorageUI Instance { get; private set; }

        private HubStorageInteractable _target;

        protected override string PanelName => "HubStorage_Panel";
        protected override string EmptyText => "Hub storage empty.";
        protected override string BottomButtonLabel => "Deposit all";
        protected override Color BottomButtonColor => new Color(0.1f, 0.35f, 0.6f);

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        public void Show(HubStorageInteractable target)
        {
            _target = target;
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
            _target.DepositAll();
            Refresh();
        }

        protected override IReadOnlyList<InventoryItem> GetItems()
        {
            var hub = HubStorage.Instance;
            if (hub == null)
            {
                _titleText.text = "HUB STORAGE";
                _emptyLabel.gameObject.SetActive(true);
                return null;
            }

            // Naslov nosi i popunjenost, pa se ažurira pri svakom refreshu.
            _titleText.text = $"HUB STORAGE ({hub.TotalCount()}/{hub.MaxCapacity})";
            return hub.GetInventory();
        }
    }
}
