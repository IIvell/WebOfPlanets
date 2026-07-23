using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Panel s opisom alata/stroja. Otvara se klikom na red recepta u crafting UI-ju
    // ili tipkom Q za item u trenutno odabranom hotbar slotu.
    public class ItemInfoUI : MonoBehaviour
    {
        public static ItemInfoUI current;

        private GameObject _panel;
        private TextMeshProUGUI _text;
        private QuickSlotItem _shownItem;

        void Awake()
        {
            current = this;
            BuildUI();
            _panel.SetActive(false);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (GameManager.IsPlaying && keyboard.qKey.wasPressedThisFrame)
            {
                var slots = QuickSlotInventory.current;
                var item = slots != null ? slots.GetSlot(slots.SelectedIndex) : null;
                if (item != null) Toggle(item, slots.GetDurability(slots.SelectedIndex));
                else Hide();
            }

            if (_panel.activeSelf && keyboard.escapeKey.wasPressedThisFrame)
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

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

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
