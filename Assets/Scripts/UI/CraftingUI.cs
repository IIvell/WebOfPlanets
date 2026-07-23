using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private CraftingRecipe[] recipes;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Interactor interactor;

        // Besplatno craftanje ide preko centralnog GameManager.TestingMode (GameState
        // objekt u sceni) — stari lokalni freeCrafting flag je znao ostati uključen
        // u sceni a da se nigdje ne vidi (AUDIT P1 stavka 1).

        private const float RowH       = 72f;
        private const float RowGap     = 6f;
        private const float PadTop     = 8f;
        private const float PadBot     = 8f;
        private const float HeaderH    = 26f;
        private const float SectionGap = 14f;

        private GameObject      _panel;
        private Transform       _contentRoot;
        private RectTransform   _contentRT;
        private ScrollRect      _scrollRect;
        private TextMeshProUGUI _progressLbl;

        public bool IsOpen => _panel.activeSelf;

        void Awake()
        {
            MergeRecipesFromResources();
            BuildUI();
            _panel.SetActive(false);

            if (GetComponent<ItemInfoUI>() == null)
                gameObject.AddComponent<ItemInfoUI>();
        }

        // Recepti se auto-otkrivaju iz Resources/Recipes — novi recept asset ne treba
        // ručno dodavati u scene listu (scene lista ostaje podržana zbog redoslijeda).
        private void MergeRecipesFromResources()
        {
            CraftingRecipe[] loaded = Resources.LoadAll<CraftingRecipe>("Recipes");
            if (loaded == null || loaded.Length == 0) return;

            var merged = new List<CraftingRecipe>();
            if (recipes != null)
                foreach (var r in recipes)
                    if (r != null && !merged.Contains(r))
                        merged.Add(r);
            foreach (var r in loaded)
                if (!merged.Contains(r))
                    merged.Add(r);
            recipes = merged.ToArray();
        }

        void OnEnable()  => GameEventBus.OnRecipeTierUnlocked += HandleTierUnlocked;
        void OnDisable() => GameEventBus.OnRecipeTierUnlocked -= HandleTierUnlocked;

        // Uplink može dostaviti resurse (i otključati prag) dok je panel otvoren.
        private void HandleTierUnlocked(int tier)
        {
            if (IsOpen) Refresh();
        }

        void Update()
        {
            if (_panel.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame)
                Hide();
        }

        public void Show()
        {
            _panel.SetActive(true);
            Refresh();
            _scrollRect.verticalNormalizedPosition = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            playerController?.SetInputEnabled(false);
            playerCamera?.SetInputEnabled(false);
            if (interactor != null) interactor.enabled = false;
        }

        public void Hide()
        {
            _panel.SetActive(false);
            ItemInfoUI.current?.Hide();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            playerController?.SetInputEnabled(true);
            playerCamera?.SetInputEnabled(true);
            if (interactor != null) interactor.enabled = true;
        }

        private enum Category { Tools, Machines, Devices }

        private static Category CategoryOf(CraftingRecipe.ResultType type) => type switch
        {
            CraftingRecipe.ResultType.Tool             => Category.Tools,
            CraftingRecipe.ResultType.NetworkMapDevice => Category.Devices,
            CraftingRecipe.ResultType.GasMask          => Category.Devices,
            _                                          => Category.Machines
        };

        private static string CategoryLabel(Category cat) => cat switch
        {
            Category.Tools    => "TOOLS",
            Category.Machines => "MACHINES",
            Category.Devices  => "DEVICES",
            _                 => ""
        };

        private void Refresh()
        {
            UpdateProgressLabel();

            var old = new List<GameObject>();
            foreach (Transform child in _contentRoot)
                old.Add(child.gameObject);
            foreach (var go in old)
                go.transform.SetParent(null);

            float y   = PadTop;
            bool  any = false;
            foreach (Category cat in (Category[])System.Enum.GetValues(typeof(Category)))
            {
                bool headerBuilt = false;
                if (recipes != null)
                    for (int i = 0; i < recipes.Length; i++)
                    {
                        var recipe = recipes[i];
                        if (recipe == null || CategoryOf(recipe.resultType) != cat) continue;

                        if (!headerBuilt)
                        {
                            if (any) y += SectionGap;
                            BuildSectionHeader(CategoryLabel(cat), -(y + HeaderH * 0.5f));
                            y += HeaderH + RowGap;
                            headerBuilt = true;
                            any = true;
                        }

                        BuildRow(recipe, i, -(y + RowH * 0.5f));
                        y += RowH + RowGap;
                    }
                if (headerBuilt) y -= RowGap;
            }

            float totalH = y + PadBot;
            _contentRT.sizeDelta = new Vector2(_contentRT.sizeDelta.x, totalH);

            foreach (var go in old)
                Destroy(go);
        }

        private void BuildSectionHeader(string label, float yPos)
        {
            var go = new GameObject("Header_" + label);
            go.transform.SetParent(_contentRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta        = new Vector2(-16f, HeaderH);

            go.AddComponent<Image>().color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(10f, 0f);
            txtRT.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text      = label;
            txt.fontSize  = 13;
            txt.fontStyle = FontStyles.Bold;
            txt.color     = new Color(0.65f, 0.78f, 0.95f);
            txt.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private void BuildRow(CraftingRecipe recipe, int index, float yPos)
        {
            var row = new GameObject("Row_" + recipe.displayName);
            row.transform.SetParent(_contentRoot, false);

            // Anchor: stretch horizontally, pin to top
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin        = new Vector2(0f, 1f);
            rowRT.anchorMax        = new Vector2(1f, 1f);
            rowRT.pivot            = new Vector2(0.5f, 0.5f);
            rowRT.anchoredPosition = new Vector2(0f, yPos);
            rowRT.sizeDelta        = new Vector2(-16f, RowH);   // 8px inset each side

            row.AddComponent<Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

            // Klik na red (izvan CRAFT gumba) otvara opis rezultata recepta.
            var rowBtn = row.AddComponent<Button>();
            rowBtn.transition = Selectable.Transition.None;
            rowBtn.onClick.AddListener(() => ItemInfoUI.current?.Toggle(GetResultItem(recipe)));

            bool locked = !GameManager.TestingMode && !recipe.IsUnlocked;

            // Name + type label
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(row.transform, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin        = new Vector2(0f, 0.5f);
            nameRT.anchorMax        = new Vector2(0f, 0.5f);
            nameRT.pivot            = new Vector2(0f, 0.5f);
            nameRT.anchoredPosition = new Vector2(10f, 0f);
            nameRT.sizeDelta        = new Vector2(150f, 60f);
            var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.text     = $"<b>{recipe.displayName}</b>\n<size=10><color=#aaaaaa>{TypeLabel(recipe.resultType)}</color></size>";
            nameTxt.fontSize = 14;
            nameTxt.color    = locked ? new Color(0.55f, 0.55f, 0.55f) : Color.white;

            // Ingredients
            var ingGO = new GameObject("Ingredients");
            ingGO.transform.SetParent(row.transform, false);
            var ingRT = ingGO.AddComponent<RectTransform>();
            ingRT.anchorMin        = new Vector2(0f, 0.5f);
            ingRT.anchorMax        = new Vector2(0f, 0.5f);
            ingRT.pivot            = new Vector2(0f, 0.5f);
            ingRT.anchoredPosition = new Vector2(175f, 0f);
            ingRT.sizeDelta        = new Vector2(245f, 60f);
            var ingTxt = ingGO.AddComponent<TextMeshProUGUI>();
            ingTxt.text     = locked ? BuildLockedText(recipe) : BuildIngredientsText(recipe);
            ingTxt.fontSize = 11;
            ingTxt.color    = Color.white;

            // Craft button
            var btnGO = new GameObject("CraftBtn");
            btnGO.transform.SetParent(row.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(1f, 0.5f);
            btnRT.anchorMax        = new Vector2(1f, 0.5f);
            btnRT.pivot            = new Vector2(1f, 0.5f);
            btnRT.anchoredPosition = new Vector2(-10f, 0f);
            btnRT.sizeDelta        = new Vector2(88f, 44f);
            var btnImg = btnGO.AddComponent<Image>();
            var btn    = btnGO.AddComponent<Button>();

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(4f,  4f);
            lblRT.offsetMax = new Vector2(-4f, -4f);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = "CRAFT";
            lbl.fontSize  = 13;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = Color.white;

            bool hotbarFull = QuickSlotInventory.current != null && QuickSlotInventory.current.IsFull;
            bool canCraft   = !locked && (GameManager.TestingMode || recipe.CanAfford()) && !hotbarFull;
            if (locked)          lbl.text = $"TIER {recipe.unlockTier}";
            else if (hotbarFull) lbl.text = "HOTBAR\nFULL";

            Color btnColor = canCraft ? new Color(0.1f, 0.55f, 0.2f) : new Color(0.22f, 0.22f, 0.22f);
            btnImg.color     = btnColor;
            btn.interactable = canCraft;

            var colors = btn.colors;
            colors.normalColor      = btnColor;
            colors.highlightedColor = canCraft ? Color.Lerp(btnColor, Color.white, 0.25f) : btnColor;
            colors.pressedColor     = canCraft ? Color.Lerp(btnColor, Color.black, 0.2f)  : btnColor;
            colors.disabledColor    = new Color(0.22f, 0.22f, 0.22f);
            btn.colors = colors;

            int captured = index;
            btn.onClick.AddListener(() => OnCraft(captured));
        }

        private void OnCraft(int index)
        {
            if (recipes == null || index >= recipes.Length) return;
            var recipe = recipes[index];
            if (recipe == null || (!GameManager.TestingMode && (!recipe.IsUnlocked || !recipe.CanAfford()))) return;

            // Prvo rezultat u hotbar, pa tek onda potrošnja sastojaka —
            // da se sastojci ne izgube kad je hotbar pun.
            if (!ProduceResult(recipe))
            {
                Debug.Log($"[CraftingUI] Hotbar je pun — '{recipe.displayName}' nije craftan.");
                return;
            }

            if (!GameManager.TestingMode)
                recipe.ConsumeIngredients();
            AudioManager.PlayCraft();
            Refresh();
        }

        private static QuickSlotItem GetResultItem(CraftingRecipe recipe) => recipe.resultType switch
        {
            CraftingRecipe.ResultType.Tool             => recipe.resultTool,
            CraftingRecipe.ResultType.CollectorMachine => recipe.resultMachine,
            CraftingRecipe.ResultType.StorageMachine   => recipe.resultStorageMachine,
            CraftingRecipe.ResultType.SmelterMachine   => recipe.resultSmelterMachine,
            CraftingRecipe.ResultType.ExtractorMachine => recipe.resultExtractorMachine,
            CraftingRecipe.ResultType.UplinkMachine    => recipe.resultUplinkMachine,
            CraftingRecipe.ResultType.TeleporterMachine => recipe.resultTeleporterMachine,
            CraftingRecipe.ResultType.TwoWayTeleporterMachine => recipe.resultTwoWayTeleporterMachine,
            CraftingRecipe.ResultType.NetworkMapDevice => recipe.resultNetworkMapDevice,
            CraftingRecipe.ResultType.RespawnTotem     => recipe.resultRespawnTotem,
            CraftingRecipe.ResultType.GasMask          => recipe.resultGasMask,
            CraftingRecipe.ResultType.Computer         => recipe.resultComputer,
            _                                          => null
        };

        // Sve vrste rezultata završavaju u hotbaru — strojevi se postavljaju u svijet
        // preko MachinePlacer-a. Vraća false ako rezultat nije stao u hotbar.
        private bool ProduceResult(CraftingRecipe recipe)
        {
            QuickSlotItem result = GetResultItem(recipe);
            if (result == null) return false;
            return QuickSlotInventory.current != null && QuickSlotInventory.current.TryAdd(result, out _);
        }

        private string BuildLockedText(CraftingRecipe recipe)
        {
            return $"<color=#ffaa44>LOCKED — tier {recipe.unlockTier}</color>\n" +
                   "<color=#aaaaaa>Unlock at Hub computer</color>";
        }

        private void UpdateProgressLabel()
        {
            if (_progressLbl == null) return;
            _progressLbl.text = HubProgress.Tier >= HubProgress.MaxTier
                ? $"Hub progress: tier {HubProgress.Tier}/{HubProgress.MaxTier} — all recipes unlocked"
                : $"Hub progress: tier {HubProgress.Tier}/{HubProgress.MaxTier} — next tier unlocks at Hub computer";
        }

        private string BuildIngredientsText(CraftingRecipe recipe)
        {
            if (recipe.ingredients == null || recipe.ingredients.Length == 0)
                return "<color=#aaaaaa>Free</color>";

            var sb = new StringBuilder();
            foreach (var ing in recipe.ingredients)
            {
                if (ing.item == null) continue;
                var    inv  = InventorySystem.current?.Get(ing.item);
                int    have = inv?.GetStackSize() ?? 0;
                string col  = have >= ing.amount ? "#88ff88" : "#ff6666";
                sb.AppendLine($"<color={col}>{ing.amount}x {ing.item.displayName} ({have})</color>");
            }
            return sb.ToString().TrimEnd();
        }

        private static string TypeLabel(CraftingRecipe.ResultType type) => type switch
        {
            CraftingRecipe.ResultType.Tool             => "TOOL",
            CraftingRecipe.ResultType.CollectorMachine => "COLLECTOR",
            CraftingRecipe.ResultType.StorageMachine   => "STORAGE",
            CraftingRecipe.ResultType.SmelterMachine   => "SMELTER",
            CraftingRecipe.ResultType.ExtractorMachine => "EXTRACTOR",
            CraftingRecipe.ResultType.UplinkMachine    => "UPLINK",
            CraftingRecipe.ResultType.TeleporterMachine => "TELEPORTER",
            CraftingRecipe.ResultType.TwoWayTeleporterMachine => "TWO-WAY TELEPORTER",
            CraftingRecipe.ResultType.NetworkMapDevice  => "DEVICE",
            CraftingRecipe.ResultType.RespawnTotem      => "RESPAWN TOTEM",
            CraftingRecipe.ResultType.GasMask           => "EQUIPMENT",
            CraftingRecipe.ResultType.Computer          => "COMPUTER",
            _                                           => ""
        };

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("Crafting_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(560f, 430f);

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            MakeLabel(_panel.transform, "CRAFTING", 20, new Vector2(0f, 196f), new Vector2(500f, 36f))
                .alignment = TextAlignmentOptions.Center;

            _progressLbl = MakeLabel(_panel.transform, "", 11, new Vector2(0f, 168f), new Vector2(520f, 20f));
            _progressLbl.color = new Color(0.65f, 0.75f, 0.85f);

            // Scroll view
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(_panel.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchoredPosition = new Vector2(0f, -22f);
            scrollRT.sizeDelta        = new Vector2(540f, 342f);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();

            // Content — no VerticalLayoutGroup or ContentSizeFitter; Refresh() sets size manually
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            scrollRect.viewport = viewportRT;
            scrollRect.content  = contentRT;
            _contentRoot = contentGO.transform;
            _contentRT   = contentRT;
            _scrollRect  = scrollRect;

            BuildScrollbar(scrollGO.transform, scrollRect);

            MakeLabel(_panel.transform, "ESC — close", 11, new Vector2(0f, -200f), new Vector2(500f, 24f))
                .color = new Color(0.6f, 0.6f, 0.6f);
        }

        private void BuildScrollbar(Transform parent, ScrollRect scrollRect)
        {
            var sbGO = new GameObject("Scrollbar");
            sbGO.transform.SetParent(parent, false);
            var sbRT = sbGO.AddComponent<RectTransform>();
            sbRT.anchorMin        = new Vector2(1f, 0f);
            sbRT.anchorMax        = new Vector2(1f, 1f);
            sbRT.pivot            = new Vector2(1f, 0.5f);
            sbRT.anchoredPosition = Vector2.zero;
            sbRT.sizeDelta        = new Vector2(10f, 0f);
            sbGO.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.9f);
            var scrollbar = sbGO.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var areaGO = new GameObject("SlidingArea");
            areaGO.transform.SetParent(sbGO.transform, false);
            var areaRT = areaGO.AddComponent<RectTransform>();
            areaRT.anchorMin = Vector2.zero;
            areaRT.anchorMax = Vector2.one;
            areaRT.offsetMin = new Vector2(2f, 2f);
            areaRT.offsetMax = new Vector2(-2f, -2f);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(areaGO.transform, false);
            var handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.offsetMin = Vector2.zero;
            handleRT.offsetMax = Vector2.zero;
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = new Color(0.3f, 0.42f, 0.55f);

            scrollbar.handleRect    = handleRT;
            scrollbar.targetGraphic = handleImg;

            var colors = scrollbar.colors;
            colors.highlightedColor = new Color(0.4f, 0.55f, 0.7f);
            colors.pressedColor     = new Color(0.5f, 0.65f, 0.8f);
            scrollbar.colors = colors;

            scrollRect.verticalScrollbar           = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing    = 2f;
        }

        private TextMeshProUGUI MakeLabel(Transform parent, string text, float fontSize, Vector2 pos, Vector2 delta)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = delta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
    }
}
