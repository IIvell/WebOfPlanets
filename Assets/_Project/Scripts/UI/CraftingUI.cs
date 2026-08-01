using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace WebOfPlanets
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
            recipes = CraftingSystem.MergeWithResources(recipes);
            BuildUI();
            _panel.SetActive(false);

            if (GetComponent<ItemInfoUI>() == null)
                gameObject.AddComponent<ItemInfoUI>();
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
            if (_panel.activeSelf && GameKeys.WasPressed(GameKeys.Cancel))
                Hide();
        }

        public void Show()
        {
            _panel.SetActive(true);
            Refresh();
            _scrollRect.verticalNormalizedPosition = 1f;
            UiFocus.Acquire(playerController, playerCamera, interactor);
        }

        public void Hide()
        {
            _panel.SetActive(false);
            ItemInfoUI.Instance?.Hide();
            UiFocus.Release(playerController, playerCamera, interactor);
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

            var rowImg = row.AddComponent<Image>();
            rowImg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
            UiTheme.StyleButton(rowImg); // red kao uokvirena kartica

            // Klik na red (izvan CRAFT gumba) otvara opis rezultata recepta.
            var rowBtn = row.AddComponent<Button>();
            rowBtn.transition = Selectable.Transition.None;
            rowBtn.onClick.AddListener(() => ItemInfoUI.Instance?.Toggle(CraftingSystem.GetResultItem(recipe)));

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
            UiTheme.StyleButton(btnImg); // boju (zeleno/sivo) postavlja logika ispod
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

            bool hotbarFull = QuickSlotInventory.Instance != null && QuickSlotInventory.Instance.IsFull;
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

        // Sama transakcija (rezultat u hotbar pa potrošnja) živi u CraftingSystemu;
        // UI dodaje samo zvuk, refresh i poruku.
        private void OnCraft(int index)
        {
            if (recipes == null || index >= recipes.Length) return;
            var recipe = recipes[index];
            if (recipe == null || (!GameManager.TestingMode && (!recipe.IsUnlocked || !recipe.CanAfford()))) return;

            if (!CraftingSystem.TryCraft(recipe))
            {
                Debug.Log($"[CraftingUI] Hotbar je pun — '{recipe.displayName}' nije craftan.");
                return;
            }

            AudioManager.PlayCraft();
            Refresh();
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
                var    inv  = InventorySystem.Instance?.Get(ing.item);
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

            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0f, 0.05f, 0.1f, 0.93f);
            UiTheme.StylePanel(panelImg); // itch.io pack; bez sprite-a stara ploča

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

            MakeLabel(_panel.transform, $"{GameKeys.CancelName} — close", 11, new Vector2(0f, -200f), new Vector2(500f, 24f))
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

    // Premješteno iz ItemInfoUI.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Panel s opisom alata/stroja. Otvara se klikom na red recepta u crafting UI-ju
    // ili tipkom Q za item u trenutno odabranom hotbar slotu.
    public class ItemInfoUI : MonoBehaviour
    {
        public static ItemInfoUI Instance { get; private set; }

        private GameObject _panel;
        private TextMeshProUGUI _text;
        private QuickSlotItem _shownItem;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildUI();
            _panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (GameManager.IsPlaying && GameKeys.WasPressed(GameKeys.ItemInfo))
            {
                var slots = QuickSlotInventory.Instance;
                var item = slots != null ? slots.GetSlot(slots.SelectedIndex) : null;
                if (item != null) Toggle(item, slots.GetDurability(slots.SelectedIndex));
                else Hide();
            }

            if (_panel.activeSelf && GameKeys.WasPressed(GameKeys.Cancel))
                Hide();
        }

        // Isti item zatvara panel, drugi item samo mijenja opis.
        // currentDurability < 0 = nepoznata (npr. item iz recepta, ne iz slota).
        public void Toggle(QuickSlotItem item, int currentDurability = -1)
        {
            if (item == null) return;

            if (_panel.activeSelf && _shownItem == item)
                Hide();
            else
                Show(item, currentDurability);
        }

        public void Show(QuickSlotItem item, int currentDurability = -1)
        {
            if (item == null) return;

            _shownItem = item;
            _text.text = BuildDescription(item, currentDurability);
            _panel.SetActive(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
            _shownItem = null;
        }

        public static string BuildDescription(QuickSlotItem item, int currentDurability = -1)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b><size=17>{item.displayName}</size></b>");

            switch (item)
            {
                case Tool tool:
                    sb.AppendLine("<color=#aaaaaa>TOOL</color>");
                    sb.AppendLine();
                    sb.AppendLine($"Mining speed: <b>{tool.miningSpeedMultiplier:0.#}x</b>");
                    sb.AppendLine(tool.maxDurability <= 0
                        ? "Durability: <b>infinite</b>"
                        : currentDurability >= 0
                            ? $"Durability: <b>{currentDurability} / {tool.maxDurability}</b>"
                            : $"Durability: <b>{tool.maxDurability}</b>");
                    sb.AppendLine();
                    sb.AppendLine("<color=#888888>Select slot (1-9) to equip.</color>");
                    break;

                case MachineData collector:
                    sb.AppendLine("<color=#aaaaaa>COLLECTOR</color>");
                    sb.AppendLine();
                    sb.AppendLine($"Collects from planet: <b>{ItemList(collector.collectableItems)}</b>");
                    sb.AppendLine($"Cycle: <b>{collector.collectionInterval:0.#}s</b>, per cycle: <b>{collector.amountPerCycle}</b>");
                    AppendMaintenance(sb, collector.maintenanceCost);
                    sb.AppendLine("Press <b>E</b> to collect the gathered items.");
                    AppendPlaceHint(sb);
                    break;

                case StorageMachineData:
                    sb.AppendLine("<color=#aaaaaa>STORAGE</color>");
                    sb.AppendLine();
                    sb.AppendLine("Receives resources from a connected collector.");
                    sb.AppendLine("Press <b>E</b> to view contents.");
                    AppendPlaceHint(sb);
                    break;

                case SmelterMachineData smelter:
                    sb.AppendLine("<color=#aaaaaa>SMELTER</color>");
                    sb.AppendLine();
                    if (smelter.recipes != null)
                        foreach (var r in smelter.recipes)
                            if (r.input != null && r.output != null)
                                sb.AppendLine($"<b>{r.inputAmount}x {r.input.displayName} -> {r.outputAmount}x {r.output.displayName}</b>");
                    sb.AppendLine($"Cycle: <b>{smelter.processInterval:0.#}s</b>");
                    sb.AppendLine("Press <b>E</b> to collect output and insert raw materials.");
                    AppendPlaceHint(sb);
                    break;

                case ExtractorMachineData extractor:
                    sb.AppendLine("<color=#aaaaaa>EXTRACTOR</color>");
                    sb.AppendLine();
                    if (extractor.outputs != null)
                        foreach (var o in extractor.outputs)
                            if (o.item != null)
                                sb.AppendLine($"Produces: <b>{o.amount}x {o.item.displayName}</b>");
                    sb.AppendLine($"Cycle: <b>{extractor.extractionInterval:0.#}s</b>, capacity: <b>{extractor.maxStored}</b>");
                    AppendMaintenance(sb, extractor.maintenanceCost);
                    sb.AppendLine("Press <b>E</b> to collect the produced items.");
                    AppendPlaceHint(sb);
                    break;

                case UplinkMachineData uplink:
                    sb.AppendLine("<color=#aaaaaa>UPLINK</color>");
                    sb.AppendLine();
                    sb.AppendLine($"Sends <b>{uplink.itemsPerCycle}</b> resources every <b>{uplink.transmitInterval:0.#}s</b> to Hub storage.");
                    sb.AppendLine("Press <b>E</b> to deposit all materials from inventory.");
                    AppendPlaceHint(sb);
                    break;

                // Podklasa mora ići prije TeleporterMachineData case-a.
                case TwoWayTeleporterMachineData:
                    sb.AppendLine("<color=#aaaaaa>TWO-WAY TELEPORTER</color>");
                    sb.AppendLine();
                    sb.AppendLine("First <b>P</b> places the entrance on the current planet,");
                    sb.AppendLine("second <b>P</b> places the exit on another planet.");
                    sb.AppendLine("<b>X</b> — cancel (demolishes the placed entrance).");
                    sb.AppendLine("Press <b>E</b> to teleport in both directions.");
                    AppendPlaceHint(sb);
                    break;

                case TeleporterMachineData:
                    sb.AppendLine("<color=#aaaaaa>TELEPORTER</color>");
                    sb.AppendLine();
                    sb.AppendLine("Placing it automatically builds the exit teleporter on the Hub.");
                    sb.AppendLine("Press <b>E</b> to teleport to the linked teleporter.");
                    AppendPlaceHint(sb);
                    break;

                case RespawnTotemMachineData:
                    sb.AppendLine("<color=#aaaaaa>RESPAWN TOTEM</color>");
                    sb.AppendLine();
                    sb.AppendLine("Sets a respawn point on the current planet.");
                    sb.AppendLine("Press <b>E</b> on the totem to activate it —");
                    sb.AppendLine("death then returns you to it instead of the Hub.");
                    AppendPlaceHint(sb);
                    break;

                case ComputerMachineData:
                    sb.AppendLine("<color=#aaaaaa>COMPUTER</color>");
                    sb.AppendLine();
                    sb.AppendLine("Places a Computer on the current planet with the");
                    sb.AppendLine("same menu as the Hub Computer (network map, hub");
                    sb.AppendLine("tiers). With a Respawn Totem it makes a remote base.");
                    sb.AppendLine("Press <b>E</b> on it to open the menu.");
                    AppendPlaceHint(sb);
                    break;

                case NetworkMapDeviceData:
                    sb.AppendLine("<color=#aaaaaa>DEVICE</color>");
                    sb.AppendLine();
                    sb.AppendLine("Shows the planet network map: all planets,");
                    sb.AppendLine("connections and their health, live.");
                    sb.AppendLine("Not placeable, not consumed by use.");
                    sb.AppendLine();
                    sb.AppendLine("<color=#888888>P — open map (while slot is selected).</color>");
                    break;

                case GasMaskData:
                    sb.AppendLine("<color=#aaaaaa>EQUIPMENT</color>");
                    sb.AppendLine();
                    sb.AppendLine("Protects against the toxic atmosphere of gas planets —");
                    sb.AppendLine("without it you gradually lose health there.");
                    sb.AppendLine("Once put on it stays on your head even while");
                    sb.AppendLine("you use other slots.");
                    sb.AppendLine("Not placeable, not consumed by use.");
                    sb.AppendLine();
                    sb.AppendLine("<color=#888888>P — put on/take off mask (while slot is selected).</color>");
                    break;
            }

            return sb.ToString().TrimEnd();
        }

        private static string ItemList(List<Item> items)
        {
            if (items == null || items.Count == 0) return "nothing";

            var parts = new List<string>();
            foreach (var item in items)
                if (item != null) parts.Add(item.displayName);
            return parts.Count > 0 ? string.Join(", ", parts) : "nothing";
        }

        private static void AppendMaintenance(StringBuilder sb, ConnectionRequirement[] cost)
        {
            var parts = new List<string>();
            if (cost != null)
                foreach (var req in cost)
                    if (req != null && req.item != null)
                        parts.Add($"{req.amount}x {req.item.displayName}");

            sb.AppendLine(parts.Count > 0
                ? $"Maintenance per cycle: <b>{string.Join(", ", parts)}</b> (from Hub)"
                : "Maintenance: <b>free</b>");
        }

        private static void AppendPlaceHint(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("<color=#888888>P — place on planet (while slot is selected).</color>");
            sb.AppendLine("<color=#888888>X — near a placed machine, pick it back up.</color>");
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("ItemInfo_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = new Vector2(445f, 0f);
            panelRT.sizeDelta        = new Vector2(310f, 340f);

            var infoImg = _panel.AddComponent<Image>();
            infoImg.color = new Color(0f, 0.05f, 0.1f, 0.93f);
            UiTheme.StylePanelTall(infoImg); // itch.io pack; bez sprite-a stara ploča

            var textGO = new GameObject("Description");
            textGO.transform.SetParent(_panel.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(14f, 34f);
            textRT.offsetMax = new Vector2(-14f, -12f);
            _text = textGO.AddComponent<TextMeshProUGUI>();
            _text.fontSize  = 13;
            _text.color     = Color.white;
            _text.alignment = TextAlignmentOptions.TopLeft;

            var hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(_panel.transform, false);
            var hintRT = hintGO.AddComponent<RectTransform>();
            hintRT.anchorMin        = new Vector2(0.5f, 0f);
            hintRT.anchorMax        = new Vector2(0.5f, 0f);
            hintRT.pivot            = new Vector2(0.5f, 0f);
            hintRT.anchoredPosition = new Vector2(0f, 8f);
            hintRT.sizeDelta        = new Vector2(280f, 20f);
            var hint = hintGO.AddComponent<TextMeshProUGUI>();
            hint.text      = "Q / ESC — close";
            hint.fontSize  = 11;
            hint.color     = new Color(0.6f, 0.6f, 0.6f);
            hint.alignment = TextAlignmentOptions.Center;
        }
    }
}
