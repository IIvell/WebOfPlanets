using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Panel na Hub računalu: prikazuje pragove hub napretka i troši resurse iz
    // Hub skladišta za njihovo otključavanje (HubProgress). Dodaje ga ComputerMenuUI
    // u Awake — ne treba ručno postavljanje u sceni.
    public class HubProgressUI : MonoBehaviour
    {
        private PlayerController playerController;
        private PlayerCamera     playerCamera;
        private Interactor       interactor;

        private GameObject      _panel;
        private TextMeshProUGUI _statusLbl;
        private readonly List<GameObject> _sections = new();
        private float _refreshTimer;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Init(PlayerController controller, PlayerCamera cam, Interactor inter)
        {
            playerController = controller;
            playerCamera     = cam;
            interactor       = inter;
        }

        void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        void Update()
        {
            if (!_panel.activeSelf) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
                return;
            }

            // Uplink može dostaviti resurse dok je panel otvoren — osvježi brojeve.
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= 1f) Refresh();
        }

        public void Show()
        {
            _panel.SetActive(true);
            Refresh();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            playerController?.SetInputEnabled(false);
            playerCamera?.SetInputEnabled(false);
            if (interactor != null) interactor.enabled = false;
        }

        public void Hide()
        {
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            playerController?.SetInputEnabled(true);
            playerCamera?.SetInputEnabled(true);
            if (interactor != null) interactor.enabled = true;
        }

        private void Refresh()
        {
            _refreshTimer = 0f;

            foreach (var go in _sections)
            {
                go.transform.SetParent(null);
                Destroy(go);
            }
            _sections.Clear();

            _statusLbl.text = HubProgress.Tier >= HubProgress.MaxTier
                ? $"Trenutni prag: {HubProgress.Tier}/{HubProgress.MaxTier} — sve otključano"
                : $"Trenutni prag: {HubProgress.Tier}/{HubProgress.MaxTier}";

            float yCursor = 120f;
            for (int t = 0; t < HubProgress.MaxTier; t++)
                yCursor = BuildTierSection(t, yCursor) - 8f;
        }

        // Gradi blok jednog praga; vraća y donjeg ruba bloka.
        private float BuildTierSection(int tierIndex, float yTop)
        {
            var reqs = HubProgress.TierRequirements[tierIndex];
            float height = 30f + reqs.Length * 15f + 20f;

            var section = new GameObject("Tier_" + (tierIndex + 1));
            section.transform.SetParent(_panel.transform, false);
            _sections.Add(section);

            var rt = section.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, yTop);
            rt.sizeDelta        = new Vector2(420f, height);

            section.AddComponent<Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

            bool unlocked = HubProgress.Tier > tierIndex;
            bool isNext   = HubProgress.Tier == tierIndex;

            string status = unlocked ? "<color=#66ff66>OTKLJUČANO</color>"
                          : isNext   ? "<color=#ffaa44>SLJEDEĆI PRAG</color>"
                                     : "<color=#888888>ZAKLJUČANO</color>";

            var header = MakeText(section.transform, $"<b>PRAG {tierIndex + 1}</b>   {status}", 13,
                new Vector2(12f, -6f), new Vector2(300f, 20f));
            header.alignment = TextAlignmentOptions.TopLeft;

            var reqTxt = MakeText(section.transform, BuildReqText(tierIndex, unlocked), 11,
                new Vector2(12f, -28f), new Vector2(280f, reqs.Length * 15f + 4f));
            reqTxt.alignment = TextAlignmentOptions.TopLeft;

            var desc = MakeText(section.transform,
                $"<color=#8899aa>Otključava: {HubProgress.TierUnlocks[tierIndex]}</color>", 10,
                new Vector2(12f, -(height - 18f)), new Vector2(396f, 16f));
            desc.alignment = TextAlignmentOptions.TopLeft;

            if (isNext)
                BuildUnlockButton(section.transform, height);

            return yTop - height;
        }

        private string BuildReqText(int tierIndex, bool unlocked)
        {
            var sb = new StringBuilder();
            foreach (var req in HubProgress.TierRequirements[tierIndex])
            {
                if (unlocked)
                {
                    sb.AppendLine($"<color=#88cc88>{req.amount}x {req.DisplayName}</color>");
                    continue;
                }

                int have = 0;
                if (HubStorage.current != null && req.Item != null)
                    have = HubStorage.current.Get(req.Item)?.GetStackSize() ?? 0;

                string col = have >= req.amount ? "#88ff88" : "#ff6666";
                sb.AppendLine($"<color={col}>{have}/{req.amount}x {req.DisplayName}</color>");
            }
            return sb.ToString().TrimEnd();
        }

        private void BuildUnlockButton(Transform section, float sectionHeight)
        {
            bool canUnlock = HubProgress.CanUnlockNext();

            var btnGO = new GameObject("UnlockBtn");
            btnGO.transform.SetParent(section, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(1f, 1f);
            btnRT.anchorMax        = new Vector2(1f, 1f);
            btnRT.pivot            = new Vector2(1f, 1f);
            btnRT.anchoredPosition = new Vector2(-10f, -8f);
            btnRT.sizeDelta        = new Vector2(100f, sectionHeight - 30f);

            var btnImg = btnGO.AddComponent<Image>();
            var btn    = btnGO.AddComponent<Button>();

            Color btnColor = canUnlock ? new Color(0.1f, 0.55f, 0.2f) : new Color(0.22f, 0.22f, 0.22f);
            btnImg.color     = btnColor;
            btn.interactable = canUnlock;

            var colors = btn.colors;
            colors.normalColor      = btnColor;
            colors.highlightedColor = canUnlock ? Color.Lerp(btnColor, Color.white, 0.25f) : btnColor;
            colors.pressedColor     = canUnlock ? Color.Lerp(btnColor, Color.black, 0.2f)  : btnColor;
            colors.disabledColor    = new Color(0.22f, 0.22f, 0.22f);
            btn.colors = colors;

            btn.onClick.AddListener(() =>
            {
                if (HubProgress.TryUnlockNext())
                    Refresh();
            });

            var lbl = MakeText(btnGO.transform, "OTKLJUČAJ", 11, Vector2.zero, Vector2.zero);
            var lblRT = lbl.rectTransform;
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(4f, 4f);
            lblRT.offsetMax = new Vector2(-4f, -4f);
            lbl.alignment = TextAlignmentOptions.Center;
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("HubProgress_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(460f, 380f);

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            var title = MakeText(_panel.transform, "HUB NAPREDAK", 20, Vector2.zero, new Vector2(420f, 32f));
            CenterAnchor(title.rectTransform, new Vector2(0f, 168f));
            title.alignment = TextAlignmentOptions.Center;

            _statusLbl = MakeText(_panel.transform, "", 12, Vector2.zero, new Vector2(420f, 20f));
            CenterAnchor(_statusLbl.rectTransform, new Vector2(0f, 142f));
            _statusLbl.alignment = TextAlignmentOptions.Center;
            _statusLbl.color     = new Color(0.65f, 0.75f, 0.85f);

            var hint = MakeText(_panel.transform, "ESC — zatvori", 11, Vector2.zero, new Vector2(420f, 24f));
            CenterAnchor(hint.rectTransform, new Vector2(0f, -172f));
            hint.alignment = TextAlignmentOptions.Center;
            hint.color     = new Color(0.6f, 0.6f, 0.6f);
        }

        private static void CenterAnchor(RectTransform rt, Vector2 pos)
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        private TextMeshProUGUI MakeText(Transform parent, string text, float fontSize, Vector2 pos, Vector2 delta)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = delta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text     = text;
            tmp.fontSize = fontSize;
            tmp.color    = Color.white;
            return tmp;
        }
    }
}
