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

            if (keyboard.qKey.wasPressedThisFrame)
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
                    sb.AppendLine("<color=#aaaaaa>ALAT</color>");
                    sb.AppendLine();
                    sb.AppendLine($"Brzina kopanja: <b>{tool.miningSpeedMultiplier:0.#}x</b>");
                    sb.AppendLine(tool.maxDurability <= 0
                        ? "Trajnost: <b>beskonačna</b>"
                        : currentDurability >= 0
                            ? $"Trajnost: <b>{currentDurability} / {tool.maxDurability}</b>"
                            : $"Trajnost: <b>{tool.maxDurability}</b>");
                    sb.AppendLine();
                    sb.AppendLine("<color=#888888>Odaberi slot (1-9) da ga opremiš.</color>");
                    break;

                case MachineData collector:
                    sb.AppendLine("<color=#aaaaaa>KOLEKTOR</color>");
                    sb.AppendLine();
                    sb.AppendLine($"Skuplja s planete: <b>{ItemList(collector.collectableItems)}</b>");
                    sb.AppendLine($"Ciklus: <b>{collector.collectionInterval:0.#}s</b>, po ciklusu: <b>{collector.amountPerCycle}</b>");
                    AppendMaintenance(sb, collector.maintenanceCost);
                    sb.AppendLine("Pritisni <b>E</b> da preuzmeš skupljeno.");
                    AppendPlaceHint(sb);
                    break;

                case StorageMachineData:
                    sb.AppendLine("<color=#aaaaaa>STORAGE</color>");
                    sb.AppendLine();
                    sb.AppendLine("Prima resurse iz povezanog kolektora.");
                    sb.AppendLine("Pritisni <b>E</b> za pregled sadržaja.");
                    AppendPlaceHint(sb);
                    break;

                case SmelterMachineData smelter:
                    sb.AppendLine("<color=#aaaaaa>TOPIONICA</color>");
                    sb.AppendLine();
                    if (smelter.recipes != null)
                        foreach (var r in smelter.recipes)
                            if (r.input != null && r.output != null)
                                sb.AppendLine($"<b>{r.inputAmount}x {r.input.displayName} -> {r.outputAmount}x {r.output.displayName}</b>");
                    sb.AppendLine($"Ciklus: <b>{smelter.processInterval:0.#}s</b>");
                    sb.AppendLine("Pritisni <b>E</b> da pokupiš gotovo i ubaciš sirovine.");
                    AppendPlaceHint(sb);
                    break;

                case ExtractorMachineData extractor:
                    sb.AppendLine("<color=#aaaaaa>EKSTRAKTOR</color>");
                    sb.AppendLine();
                    if (extractor.outputs != null)
                        foreach (var o in extractor.outputs)
                            if (o.item != null)
                                sb.AppendLine($"Proizvodi: <b>{o.amount}x {o.item.displayName}</b>");
                    sb.AppendLine($"Ciklus: <b>{extractor.extractionInterval:0.#}s</b>, kapacitet: <b>{extractor.maxStored}</b>");
                    AppendMaintenance(sb, extractor.maintenanceCost);
                    sb.AppendLine("Pritisni <b>E</b> da preuzmeš proizvedeno.");
                    AppendPlaceHint(sb);
                    break;

                case UplinkMachineData uplink:
                    sb.AppendLine("<color=#aaaaaa>UPLINK</color>");
                    sb.AppendLine();
                    sb.AppendLine($"Šalje <b>{uplink.itemsPerCycle}</b> resursa svakih <b>{uplink.transmitInterval:0.#}s</b> u Hub storage.");
                    sb.AppendLine("Pritisni <b>E</b> da ubaciš sve materijale iz inventara.");
                    AppendPlaceHint(sb);
                    break;

                // Podklasa mora ići prije TeleporterMachineData case-a.
                case TwoWayTeleporterMachineData:
                    sb.AppendLine("<color=#aaaaaa>DVOSMJERNI TELEPORTER</color>");
                    sb.AppendLine();
                    sb.AppendLine("Prvi <b>P</b> postavlja ulaz na trenutnoj planeti,");
                    sb.AppendLine("drugi <b>P</b> postavlja izlaz na drugoj planeti.");
                    sb.AppendLine("<b>X</b> — odustani (ruši postavljeni ulaz).");
                    sb.AppendLine("Pritisni <b>E</b> za teleport u oba smjera.");
                    AppendPlaceHint(sb);
                    break;

                case TeleporterMachineData:
                    sb.AppendLine("<color=#aaaaaa>TELEPORTER</color>");
                    sb.AppendLine();
                    sb.AppendLine("Postavljanjem se izlazni teleporter automatski gradi na Hubu.");
                    sb.AppendLine("Pritisni <b>E</b> za teleport na povezani teleporter.");
                    AppendPlaceHint(sb);
                    break;

                case RespawnTotemMachineData:
                    sb.AppendLine("<color=#aaaaaa>RESPAWN TOTEM</color>");
                    sb.AppendLine();
                    sb.AppendLine("Postavlja respawn točku na trenutnoj planeti.");
                    sb.AppendLine("Pritisni <b>E</b> na totemu da ga aktiviraš —");
                    sb.AppendLine("smrt te tada vraća na njega umjesto na Hub.");
                    AppendPlaceHint(sb);
                    break;

                case NetworkMapDeviceData:
                    sb.AppendLine("<color=#aaaaaa>UREĐAJ</color>");
                    sb.AppendLine();
                    sb.AppendLine("Prikazuje mapu mreže planeta: sve planete,");
                    sb.AppendLine("veze i njihovo zdravlje, uživo.");
                    sb.AppendLine("Ne postavlja se i ne troši korištenjem.");
                    sb.AppendLine();
                    sb.AppendLine("<color=#888888>P — otvori mapu (dok je slot odabran).</color>");
                    break;

                case GasMaskData:
                    sb.AppendLine("<color=#aaaaaa>OPREMA</color>");
                    sb.AppendLine();
                    sb.AppendLine("Štiti od otrovne atmosfere plinskih planeta —");
                    sb.AppendLine("bez nje ondje postepeno gubiš zdravlje.");
                    sb.AppendLine("Jednom stavljena ostaje na glavi i dok");
                    sb.AppendLine("koristiš druge slotove.");
                    sb.AppendLine("Ne postavlja se i ne troši korištenjem.");
                    sb.AppendLine();
                    sb.AppendLine("<color=#888888>P — stavi/skini masku (dok je slot odabran).</color>");
                    break;
            }

            return sb.ToString().TrimEnd();
        }

        private static string ItemList(List<Item> items)
        {
            if (items == null || items.Count == 0) return "ništa";

            var parts = new List<string>();
            foreach (var item in items)
                if (item != null) parts.Add(item.displayName);
            return parts.Count > 0 ? string.Join(", ", parts) : "ništa";
        }

        private static void AppendMaintenance(StringBuilder sb, ConnectionRequirement[] cost)
        {
            var parts = new List<string>();
            if (cost != null)
                foreach (var req in cost)
                    if (req != null && req.item != null)
                        parts.Add($"{req.amount}x {req.item.displayName}");

            sb.AppendLine(parts.Count > 0
                ? $"Održavanje po ciklusu: <b>{string.Join(", ", parts)}</b> (iz Huba)"
                : "Održavanje: <b>besplatno</b>");
        }

        private static void AppendPlaceHint(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("<color=#888888>P — postavi na planetu (dok je slot odabran).</color>");
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
            hint.text      = "Q / ESC — zatvori";
            hint.fontSize  = 11;
            hint.color     = new Color(0.6f, 0.6f, 0.6f);
            hint.alignment = TextAlignmentOptions.Center;
        }
    }
}
