using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Attach to a Canvas (Screen Space – Overlay). Uvijek vidljiv, vrh ekrana sredina.
    [RequireComponent(typeof(RectTransform))]
    public class HotbarUI : MonoBehaviour
    {
        private const int SlotCount = QuickSlotInventory.SlotCount;
        private const float SlotSize = 56f;
        private const float Spacing = 6f;

        private static readonly Color NormalColor = new Color(0f, 0.05f, 0.1f, 0.75f);
        private static readonly Color SelectedColor = new Color(0.2f, 0.6f, 1f, 0.85f);

        private static readonly Color DurabilityHighColor = new Color(0.2f, 0.85f, 0.25f, 0.95f);
        private static readonly Color DurabilityMidColor = new Color(0.95f, 0.75f, 0.15f, 0.95f);
        private static readonly Color DurabilityLowColor = new Color(0.9f, 0.15f, 0.15f, 0.95f);

        private readonly Image[] _backgrounds = new Image[SlotCount];
        private readonly Image[] _icons = new Image[SlotCount];
        private readonly TextMeshProUGUI[] _nameLabels = new TextMeshProUGUI[SlotCount];
        private readonly Image[] _durabilityBackgrounds = new Image[SlotCount];
        private readonly Image[] _durabilityFills = new Image[SlotCount];

        void Awake()
        {
            BuildUI();
        }

        void OnEnable()
        {
            GameEventBus.OnQuickSlotsChanged += Refresh;
            GameEventBus.OnToolDurabilityChanged += OnDurabilityChanged;
            Refresh();
        }

        void OnDisable()
        {
            GameEventBus.OnQuickSlotsChanged -= Refresh;
            GameEventBus.OnToolDurabilityChanged -= OnDurabilityChanged;
        }

        private void OnDurabilityChanged(ToolDurabilityEvent e) => Refresh();

        private void Refresh()
        {
            if (QuickSlotInventory.current == null) return;

            for (int i = 0; i < SlotCount; i++)
            {
                QuickSlotItem item = QuickSlotInventory.current.GetSlot(i);
                bool hasItem = item != null;
                bool hasIcon = hasItem && item.icon != null;

                _icons[i].enabled = hasIcon;
                _icons[i].sprite = hasIcon ? item.icon : null;
                _nameLabels[i].text = hasItem && !hasIcon ? item.displayName : "";

                _backgrounds[i].color = i == QuickSlotInventory.current.SelectedIndex
                    ? SelectedColor
                    : NormalColor;

                RefreshDurability(i, item);
            }
        }

        // Traka trajnosti na dnu slota — samo za alate s ograničenom trajnošću.
        private void RefreshDurability(int index, QuickSlotItem item)
        {
            bool show = item is Tool tool && tool.maxDurability > 0;
            _durabilityBackgrounds[index].enabled = show;
            _durabilityFills[index].enabled = show;
            if (!show) return;

            var t = (Tool)item;
            float ratio = Mathf.Clamp01(QuickSlotInventory.current.GetDurability(index) / (float)t.maxDurability);

            // Širina kroz anchorMax.x umjesto Image.Type.Filled — filled bez sprite-a
            // ne poštuje fillAmount (renderira puni quad).
            var fillRT = (RectTransform)_durabilityFills[index].transform;
            fillRT.anchorMax = new Vector2(Mathf.Max(ratio, 0.001f), 1f);

            _durabilityFills[index].color = ratio > 0.5f ? DurabilityHighColor
                : ratio > 0.2f ? DurabilityMidColor
                : DurabilityLowColor;
        }

        private void BuildUI()
        {
            float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * Spacing;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform uiRoot = canvas != null ? canvas.transform : transform;

            var barGO = new GameObject("Hotbar_Bar");
            barGO.transform.SetParent(uiRoot, false);
            var barRT = barGO.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0.5f, 1f);
            barRT.anchorMax = new Vector2(0.5f, 1f);
            barRT.pivot     = new Vector2(0.5f, 1f);
            barRT.anchoredPosition = new Vector2(0f, -16f);
            barRT.sizeDelta = new Vector2(totalWidth, SlotSize);

            for (int i = 0; i < SlotCount; i++)
                CreateSlot(barGO.transform, i);
        }

        private void CreateSlot(Transform parent, int index)
        {
            var slotGO = new GameObject($"Slot_{index + 1}");
            slotGO.transform.SetParent(parent, false);
            var slotRT = slotGO.AddComponent<RectTransform>();
            slotRT.anchorMin = new Vector2(0f, 0f);
            slotRT.anchorMax = new Vector2(0f, 1f);
            slotRT.pivot = new Vector2(0f, 0.5f);
            slotRT.anchoredPosition = new Vector2(index * (SlotSize + Spacing), 0f);
            slotRT.sizeDelta = new Vector2(SlotSize, 0f);

            var bg = slotGO.AddComponent<Image>();
            bg.color = NormalColor;
            _backgrounds[index] = bg;

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(SlotSize - 12f, SlotSize - 12f);
            var icon = iconGO.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;
            _icons[index] = icon;

            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(slotGO.transform, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = Vector2.zero;
            nameRT.anchorMax = Vector2.one;
            nameRT.offsetMin = new Vector2(2f, 2f);
            nameRT.offsetMax = new Vector2(-2f, -2f);
            var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.fontSize = 10;
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.color = Color.white;
            _nameLabels[index] = nameTxt;

            var durBgGO = new GameObject("DurabilityBG");
            durBgGO.transform.SetParent(slotGO.transform, false);
            var durBgRT = durBgGO.AddComponent<RectTransform>();
            durBgRT.anchorMin = new Vector2(0f, 0f);
            durBgRT.anchorMax = new Vector2(1f, 0f);
            durBgRT.pivot = new Vector2(0.5f, 0f);
            durBgRT.anchoredPosition = new Vector2(0f, 3f);
            durBgRT.sizeDelta = new Vector2(-8f, 5f);
            var durBg = durBgGO.AddComponent<Image>();
            durBg.color = new Color(0f, 0f, 0f, 0.6f);
            durBg.enabled = false;
            _durabilityBackgrounds[index] = durBg;

            var durFillGO = new GameObject("DurabilityFill");
            durFillGO.transform.SetParent(durBgGO.transform, false);
            var durFillRT = durFillGO.AddComponent<RectTransform>();
            durFillRT.anchorMin = Vector2.zero;
            durFillRT.anchorMax = Vector2.one;
            durFillRT.offsetMin = new Vector2(1f, 1f);
            durFillRT.offsetMax = new Vector2(-1f, -1f);
            var durFill = durFillGO.AddComponent<Image>();
            durFill.enabled = false;
            _durabilityFills[index] = durFill;

            var numberGO = new GameObject("Number");
            numberGO.transform.SetParent(slotGO.transform, false);
            var numberRT = numberGO.AddComponent<RectTransform>();
            numberRT.anchorMin = new Vector2(0f, 1f);
            numberRT.anchorMax = new Vector2(0f, 1f);
            numberRT.pivot = new Vector2(0f, 1f);
            numberRT.anchoredPosition = new Vector2(2f, -2f);
            numberRT.sizeDelta = new Vector2(14f, 14f);
            var numberTxt = numberGO.AddComponent<TextMeshProUGUI>();
            numberTxt.text = (index + 1).ToString();
            numberTxt.fontSize = 10;
            numberTxt.alignment = TextAlignmentOptions.TopLeft;
            numberTxt.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }
}
