using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace WebOfPlanets
{
    // Zajednička baza tri list-panela (InventoryUI / HubStorageUI /
    // StorageInventoryUI) — prije baze su bila tri ~95% identična filea s već
    // prisutnim driftom (Keyboard null-guard samo u jednom, font naslova 18 vs
    // 20). Podklase zadržavaju imena klasa i serijaliziranih polja (scena drži
    // reference), a razlikuju se samo u izvoru redaka, naslovu i donjem
    // gumbu/hintu.
    //
    // Panel se veže na Canvas (Screen Space – Overlay); PlayerController,
    // PlayerCamera i Interactor se dodjeljuju u Inspectoru.
    public abstract class ItemListUI : MonoBehaviour
    {
        [SerializeField] protected PlayerController playerController;
        [SerializeField] protected PlayerCamera playerCamera;
        [SerializeField] protected Interactor interactor;

        protected GameObject _panel;
        protected TextMeshProUGUI _titleText;
        protected TextMeshProUGUI _emptyLabel;

        private RectTransform _listContent;
        private readonly List<GameObject> _rows = new();

        protected bool _isOpen;
        public bool IsOpen => _isOpen;

        // ── Konfiguracija podklase ────────────────────────────────────────────

        protected abstract string PanelName { get; }
        protected abstract string EmptyText { get; }
        // Fiksni naslov postavljen pri izgradnji (null = podklasa naslov piše
        // sama u GetItems, npr. s popunjenošću skladišta).
        protected virtual string InitialTitle => null;
        // Hint na dnu panela (null = bez hinta).
        protected virtual string HintText => null;
        // Donji akcijski gumb (null = bez gumba).
        protected virtual string BottomButtonLabel => null;
        protected virtual Color BottomButtonColor => default;
        protected virtual void OnBottomButtonClicked() { }

        // Opcionalni drugi donji gumb (kolovoz 2026., Withdraw all na hub
        // skladištu). Postoji li, oba gumba dijele donji red popola; null =
        // prvi gumb zadržava punu širinu kao prije.
        protected virtual string SecondButtonLabel => null;
        protected virtual Color SecondButtonColor => default;
        protected virtual void OnSecondButtonClicked() { }

        // Izvor redaka za Refresh; null = nema izvora (lista ostaje prazna, a
        // empty label se ne dira). Podklasa ovdje po potrebi ažurira naslov.
        protected abstract IReadOnlyList<InventoryItem> GetItems();

        // ── Životni ciklus ────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        protected virtual void Update()
        {
            if (_isOpen && GameKeys.WasPressed(GameKeys.Cancel))
                Close();
        }

        // ── Otvaranje/zatvaranje ──────────────────────────────────────────────

        protected void OpenPanel()
        {
            _isOpen = true;
            _panel.SetActive(true);
            UiFocus.Acquire(playerController, playerCamera, interactor);
            Refresh();
        }

        public virtual void Close()
        {
            _isOpen = false;
            _panel.SetActive(false);
            UiFocus.Release(playerController, playerCamera, interactor);
        }

        // ── Lista ─────────────────────────────────────────────────────────────

        protected void Refresh()
        {
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();

            var items = GetItems();
            if (items == null) return;

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

        // ── Izgradnja hijerarhije (jednom) ────────────────────────────────────

        private void BuildUI()
        {
            // Uvlaka mora čistiti rub okvira teme; bez teme ostaje stara 16.
            float pad = UiTheme.WindowPadding;
            // Donji gumb podiže listu i empty label iznad sebe.
            float bottomOffset = BottomButtonLabel != null ? pad + 44f : pad;
            float emptyBottom  = BottomButtonLabel != null ? pad + 44f : 0f;

            _panel = new GameObject(PanelName);
            _panel.transform.SetParent(transform, false);
            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta        = new Vector2(420f, 520f);
            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0f, 0.05f, 0.1f, 0.93f);
            UiTheme.StyleWindow(panelImg); // itch.io pack; bez sprite-a stara ploča

            // Naslov
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_panel.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -pad * 0.6f);
            titleRT.sizeDelta        = new Vector2(0f, 36f);
            _titleText           = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.fontSize  = 20;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color     = Color.white;
            if (InitialTitle != null) _titleText.text = InitialTitle;

            // Gumb za zatvaranje (gore desno)
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(_panel.transform, false);
            var closeBtnRT = closeBtn.AddComponent<RectTransform>();
            closeBtnRT.anchorMin        = new Vector2(1f, 1f);
            closeBtnRT.anchorMax        = new Vector2(1f, 1f);
            closeBtnRT.pivot            = new Vector2(1f, 1f);
            closeBtnRT.anchoredPosition = new Vector2(-pad * 0.6f, -pad * 0.6f);
            closeBtnRT.sizeDelta        = new Vector2(28f, 28f);
            var closeImg = closeBtn.AddComponent<Image>();
            UiTheme.StyleButton(closeImg);
            // S temom: crvenkasti tint preko sprite okvira; bez teme stara puna crvena.
            closeImg.color = UiTheme.Tint(new Color(1f, 0.5f, 0.5f), new Color(0.6f, 0.1f, 0.1f));
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

            // Scrollabilna lista
            var scrollGO = new GameObject("ScrollArea");
            scrollGO.transform.SetParent(_panel.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(pad, bottomOffset);
            scrollRT.offsetMax = new Vector2(-pad, -(pad * 0.6f + 40f));
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

            // Empty-state natpis
            var emptyGO = new GameObject("EmptyLabel");
            emptyGO.transform.SetParent(_panel.transform, false);
            var emptyRT = emptyGO.AddComponent<RectTransform>();
            emptyRT.anchorMin = new Vector2(0f, 0f);
            emptyRT.anchorMax = new Vector2(1f, 1f);
            emptyRT.offsetMin = new Vector2(pad, emptyBottom);
            emptyRT.offsetMax = new Vector2(-pad, -(pad * 0.6f + 40f));
            _emptyLabel           = emptyGO.AddComponent<TextMeshProUGUI>();
            _emptyLabel.text      = EmptyText;
            _emptyLabel.fontSize  = 14;
            _emptyLabel.alignment = TextAlignmentOptions.Center;
            _emptyLabel.color     = new Color(0.6f, 0.6f, 0.6f);

            if (HintText != null)
            {
                var hintGO = new GameObject("Hint");
                hintGO.transform.SetParent(_panel.transform, false);
                var hintRT = hintGO.AddComponent<RectTransform>();
                hintRT.anchorMin        = new Vector2(0f, 0f);
                hintRT.anchorMax        = new Vector2(1f, 0f);
                hintRT.pivot            = new Vector2(0.5f, 0f);
                hintRT.anchoredPosition = new Vector2(0f, pad * 0.4f);
                hintRT.sizeDelta        = new Vector2(0f, 20f);
                var hint           = hintGO.AddComponent<TextMeshProUGUI>();
                hint.text          = HintText;
                hint.fontSize      = 11;
                hint.alignment     = TextAlignmentOptions.Center;
                hint.color         = new Color(0.6f, 0.6f, 0.6f);
            }

            if (BottomButtonLabel != null && SecondButtonLabel != null)
            {
                // Dva gumba: dijele red popola, 3px razmaka od sredine.
                CreateBottomButton(BottomButtonLabel, BottomButtonColor, OnBottomButtonClicked, 0f, 0.5f, pad, 3f);
                CreateBottomButton(SecondButtonLabel, SecondButtonColor, OnSecondButtonClicked, 0.5f, 1f, 3f, pad);
            }
            else if (BottomButtonLabel != null)
            {
                CreateBottomButton(BottomButtonLabel, BottomButtonColor, OnBottomButtonClicked, 0f, 1f, pad, pad);
            }
        }

        // Donji akcijski gumb; anchorMinX/anchorMaxX + uvlake određuju širinu
        // (puni red ili polovica). Izdvojeno iz BuildUI zbog drugog gumba.
        private void CreateBottomButton(string label, Color color, UnityEngine.Events.UnityAction onClick,
            float anchorMinX, float anchorMaxX, float leftInset, float rightInset)
        {
            float pad = UiTheme.WindowPadding;

            var actionBtn = new GameObject($"{label}Button");
            actionBtn.transform.SetParent(_panel.transform, false);
            var actionBtnRT = actionBtn.AddComponent<RectTransform>();
            actionBtnRT.anchorMin = new Vector2(anchorMinX, 0f);
            actionBtnRT.anchorMax = new Vector2(anchorMaxX, 0f);
            actionBtnRT.offsetMin = new Vector2(leftInset, pad);
            actionBtnRT.offsetMax = new Vector2(-rightInset, pad + 36f);
            var actionImg = actionBtn.AddComponent<Image>();
            UiTheme.StyleButton(actionImg);
            actionImg.color = color; // tinta i sprite i fallback ploču
            var actionButton = actionBtn.AddComponent<Button>();
            actionButton.onClick.AddListener(onClick);
            var actionLabelGO = new GameObject("Label");
            actionLabelGO.transform.SetParent(actionBtn.transform, false);
            var actionLabelRT = actionLabelGO.AddComponent<RectTransform>();
            actionLabelRT.anchorMin = Vector2.zero;
            actionLabelRT.anchorMax = Vector2.one;
            actionLabelRT.offsetMin = Vector2.zero;
            actionLabelRT.offsetMax = Vector2.zero;
            var actionTxt = actionLabelGO.AddComponent<TextMeshProUGUI>();
            actionTxt.text      = label;
            actionTxt.fontSize  = 15;
            actionTxt.alignment = TextAlignmentOptions.Center;
            actionTxt.color     = Color.white;
        }
    }
}
