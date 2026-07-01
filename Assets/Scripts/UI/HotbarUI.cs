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

        private readonly Image[] _backgrounds = new Image[SlotCount];
        private readonly Image[] _icons = new Image[SlotCount];
        private readonly TextMeshProUGUI[] _nameLabels = new TextMeshProUGUI[SlotCount];

        void Awake()
        {
            var selfRT = (RectTransform)transform;
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;

            BuildUI();
        }

        void OnEnable()
        {
            GameEventBus.OnQuickSlotsChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameEventBus.OnQuickSlotsChanged -= Refresh;
        }

        private void Refresh()
        {
            if (QuickSlotInventory.current == null) return;

            for (int i = 0; i < SlotCount; i++)
            {
                Tool tool = QuickSlotInventory.current.GetSlot(i);
                bool hasTool = tool != null;
                bool hasIcon = hasTool && tool.icon != null;

                _icons[i].enabled = hasIcon;
                _icons[i].sprite = hasIcon ? tool.icon : null;
                _nameLabels[i].text = hasTool && !hasIcon ? tool.displayName : "";

                _backgrounds[i].color = i == QuickSlotInventory.current.SelectedIndex
                    ? SelectedColor
                    : NormalColor;
            }
        }

        private void BuildUI()
        {
            float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * Spacing;

            var barGO = new GameObject("Hotbar_Bar");
            barGO.transform.SetParent(transform, false);
            var barRT = barGO.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0.5f, 1f);
            barRT.anchorMax = new Vector2(0.5f, 1f);
            barRT.pivot = new Vector2(0.5f, 1f);
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
