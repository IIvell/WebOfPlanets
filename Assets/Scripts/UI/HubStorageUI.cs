using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Attach to a Canvas (Screen Space – Overlay).
    // Assign PlayerController, PlayerCamera, Interactor in Inspector.
    public class HubStorageUI : MonoBehaviour
    {
        public static HubStorageUI Instance { get; private set; }

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Interactor interactor;

        private GameObject _panel;
        private RectTransform _listContent;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _emptyLabel;

        private readonly List<GameObject> _rows = new();

        private HubStorageInteractable _target;
        private bool _isOpen;

        void Awake()
        {
            Instance = this;
            BuildUI();
            _panel.SetActive(false);
        }

        void Update()
        {
            if (_isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(HubStorageInteractable target)
        {
            _target = target;
            _isOpen = true;
            _panel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            playerController?.SetInputEnabled(false);
            playerCamera?.SetInputEnabled(false);
            if (interactor != null) interactor.enabled = false;

            Refresh();
        }

        public void Close()
        {
            _isOpen = false;
            _target = null;
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            playerController?.SetInputEnabled(true);
            playerCamera?.SetInputEnabled(true);
            if (interactor != null) interactor.enabled = true;
        }

        public bool IsOpen => _isOpen;

        // ── Actions ──────────────────────────────────────────────────────────

        private void DepositAll()
        {
            if (_target == null) return;
            _target.DepositAll();
            Refresh();
        }

        // ── Refresh list ─────────────────────────────────────────────────────

        private void Refresh()
        {
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();

            var hub = HubStorage.current;
            if (hub == null)
            {
                _titleText.text = "HUB STORAGE";
                _emptyLabel.gameObject.SetActive(true);
                return;
            }

            _titleText.text = $"HUB STORAGE ({hub.TotalCount()}/{hub.MaxCapacity})";

            var items = hub.GetInventory();
            _emptyLabel.gameObject.SetActive(items.Count == 0);

            foreach (var inv in items)
                CreateRow(inv);
        }

        private void CreateRow(InventoryItem inv)
        {
            var row = new GameObject($"Row_{inv.data?.displayName}");
            row.transform.SetParent(_listContent, false);

            var rowRT = row.AddComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 40f);
            row.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 40f;
            rowLayout.minHeight       = 40f;

            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(row.transform, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = new Vector2(12f, 0f);
            nameRT.offsetMax = new Vector2(-60f, 0f);
            var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.text      = inv.data != null ? inv.data.displayName : "???";
            nameTxt.fontSize  = 16;
            nameTxt.alignment = TextAlignmentOptions.MidlineLeft;
            nameTxt.color     = Color.white;

            var countGO = new GameObject("Count");
            countGO.transform.SetParent(row.transform, false);
            var countRT = countGO.AddComponent<RectTransform>();
            countRT.anchorMin        = new Vector2(1f, 0f);
            countRT.anchorMax        = new Vector2(1f, 1f);
            countRT.pivot            = new Vector2(1f, 0.5f);
            countRT.anchoredPosition = new Vector2(-12f, 0f);
            countRT.sizeDelta        = new Vector2(50f, 0f);
            var countTxt = countGO.AddComponent<TextMeshProUGUI>();
            countTxt.text      = $"x{inv.GetStackSize()}";
            countTxt.fontSize  = 16;
            countTxt.alignment = TextAlignmentOptions.MidlineRight;
            countTxt.color     = new Color(0.8f, 0.85f, 1f);

            _rows.Add(row);
        }

        // ── Build UI hierarchy once ───────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("HubStorage_Panel");
            _panel.transform.SetParent(transform, false);
            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta        = new Vector2(420f, 520f);
            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_panel.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -12f);
            titleRT.sizeDelta        = new Vector2(0f, 36f);
            _titleText           = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.fontSize  = 18;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color     = Color.white;

            // Close button (top-right)
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(_panel.transform, false);
            var closeBtnRT = closeBtn.AddComponent<RectTransform>();
            closeBtnRT.anchorMin        = new Vector2(1f, 1f);
            closeBtnRT.anchorMax        = new Vector2(1f, 1f);
            closeBtnRT.pivot            = new Vector2(1f, 1f);
            closeBtnRT.anchoredPosition = new Vector2(-10f, -10f);
            closeBtnRT.sizeDelta        = new Vector2(28f, 28f);
            closeBtn.AddComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f);
            var closeButton = closeBtn.AddComponent<Button>();
            closeButton.onClick.AddListener(Close);
            var closeLabelGO = new GameObject("Label");
            closeLabelGO.transform.SetParent(closeBtn.transform, false);
            var closeLabelRT = closeLabelGO.AddComponent<RectTransform>();
            closeLabelRT.anchorMin = Vector2.zero;
            closeLabelRT.anchorMax = Vector2.one;
            closeLabelRT.offsetMin = Vector2.zero;
            closeLabelRT.offsetMax = Vector2.zero;
            var closeTxt = closeLabelGO.AddComponent<TextMeshProUGUI>();
            closeTxt.text      = "X";
            closeTxt.fontSize  = 16;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color     = Color.white;

            // Scrollable list area
            var scrollGO = new GameObject("ScrollArea");
            scrollGO.transform.SetParent(_panel.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(16f, 60f);
            scrollRT.offsetMax = new Vector2(-16f, -56f);
            scrollGO.AddComponent<RectMask2D>();
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal   = false;
            scrollRect.vertical     = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport     = scrollRT;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            _listContent           = contentGO.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot     = new Vector2(0.5f, 1f);
            _listContent.anchoredPosition = Vector2.zero;
            _listContent.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing                = 6f;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = _listContent;

            // Empty-state label
            var emptyGO = new GameObject("EmptyLabel");
            emptyGO.transform.SetParent(_panel.transform, false);
            var emptyRT = emptyGO.AddComponent<RectTransform>();
            emptyRT.anchorMin = new Vector2(0f, 0f);
            emptyRT.anchorMax = new Vector2(1f, 1f);
            emptyRT.offsetMin = new Vector2(0f, 60f);
            emptyRT.offsetMax = new Vector2(0f, -40f);
            _emptyLabel           = emptyGO.AddComponent<TextMeshProUGUI>();
            _emptyLabel.text      = "Hub storage je prazan.";
            _emptyLabel.fontSize  = 14;
            _emptyLabel.alignment = TextAlignmentOptions.Center;
            _emptyLabel.color     = new Color(0.6f, 0.6f, 0.6f);

            // Deposit-all button (bottom)
            var depositBtn = new GameObject("DepositAllButton");
            depositBtn.transform.SetParent(_panel.transform, false);
            var depositBtnRT = depositBtn.AddComponent<RectTransform>();
            depositBtnRT.anchorMin        = new Vector2(0f, 0f);
            depositBtnRT.anchorMax        = new Vector2(1f, 0f);
            depositBtnRT.pivot            = new Vector2(0.5f, 0f);
            depositBtnRT.anchoredPosition = new Vector2(0f, 12f);
            depositBtnRT.sizeDelta        = new Vector2(-32f, 36f);
            depositBtn.AddComponent<Image>().color = new Color(0.1f, 0.35f, 0.6f);
            var depositButton = depositBtn.AddComponent<Button>();
            depositButton.onClick.AddListener(DepositAll);
            var depositLabelGO = new GameObject("Label");
            depositLabelGO.transform.SetParent(depositBtn.transform, false);
            var depositLabelRT = depositLabelGO.AddComponent<RectTransform>();
            depositLabelRT.anchorMin = Vector2.zero;
            depositLabelRT.anchorMax = Vector2.one;
            depositLabelRT.offsetMin = Vector2.zero;
            depositLabelRT.offsetMax = Vector2.zero;
            var depositTxt = depositLabelGO.AddComponent<TextMeshProUGUI>();
            depositTxt.text      = "Položi sve";
            depositTxt.fontSize  = 15;
            depositTxt.alignment = TextAlignmentOptions.Center;
            depositTxt.color     = Color.white;
        }
    }
}
