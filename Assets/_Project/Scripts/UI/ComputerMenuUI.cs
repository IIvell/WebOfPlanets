using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace WebOfPlanets
{
    public class ComputerMenuUI : MonoBehaviour
    {
        public static ComputerMenuUI Instance { get; private set; }

        [SerializeField] private NetworkMapUI networkMapUI;
        [SerializeField] private CraftingUI craftingUI;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Interactor interactor;

        private GameObject _panel;
        private HubProgressUI _hubProgressUI;

        public bool IsOpen => _panel.activeSelf;

        void Awake()
        {
            Instance = this;
            BuildUI();
            _panel.SetActive(false);

            _hubProgressUI = GetComponent<HubProgressUI>();
            if (_hubProgressUI == null)
                _hubProgressUI = gameObject.AddComponent<HubProgressUI>();
            _hubProgressUI.Init(playerController, playerCamera, interactor);
        }

        void Update()
        {
            if (_panel.activeSelf && GameKeys.WasPressed(GameKeys.Cancel))
                Hide();
        }

        public void Show()
        {
            _panel.SetActive(true);
            UiFocus.Acquire(playerController, playerCamera, interactor);
        }

        public void Hide()
        {
            _panel.SetActive(false);
            UiFocus.Release(playerController, playerCamera, interactor);
        }

        private void OpenNetworkMap()
        {
            Hide();
            networkMapUI?.Open();
        }

        private void OpenCrafting()
        {
            Hide();
            craftingUI?.Show();
        }

        private void OpenHubProgress()
        {
            Hide();
            _hubProgressUI?.Show();
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("ComputerMenu_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(380f, 360f);

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            var title = MakeLabel(_panel.transform, "COMPUTER", 22, new Vector2(0f, 140f), new Vector2(330f, 44f));
            title.alignment = TextAlignmentOptions.Center;

            MakeButton(_panel.transform, "Planet Network", new Vector2(0f, 74f),  OpenNetworkMap);
            MakeButton(_panel.transform, "Crafting",       new Vector2(0f, 0f),   OpenCrafting);
            MakeButton(_panel.transform, "Hub Progress",   new Vector2(0f, -74f), OpenHubProgress);

            var hint = MakeLabel(_panel.transform, $"{GameKeys.CancelName} — cancel", 12, new Vector2(0f, -150f), new Vector2(330f, 28f));
            hint.color     = new Color(0.6f, 0.6f, 0.6f);
            hint.alignment = TextAlignmentOptions.Center;
        }

        private void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(280f, 60f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.25f, 0.45f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = new Color(0.08f, 0.25f, 0.45f);
            colors.highlightedColor = new Color(0.15f, 0.40f, 0.65f);
            colors.pressedColor     = new Color(0.05f, 0.15f, 0.30f);
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(8f, 4f);
            lblRT.offsetMax = new Vector2(-8f, -4f);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
        }

        private TextMeshProUGUI MakeLabel(Transform parent, string text, float fontSize, Vector2 pos, Vector2 delta)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = delta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text     = text;
            tmp.fontSize = fontSize;
            tmp.color    = Color.white;
            return tmp;
        }
    }

    // Premješteno iz HubProgressUI.cs (konsolidacija malih datoteka, 31. 7. 2026.).
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

            if (GameKeys.WasPressed(GameKeys.Cancel))
            {
                Hide();
                return;
            }

            // Uplink može dostaviti resurse dok je panel otvoren — osvježi brojeve,
            // ali samo ako se nešto vidljivo promijenilo (rebuild panela nije besplatan).
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= 1f)
            {
                _refreshTimer = 0f;
                if (BuildSignature() != _lastSignature) Refresh();
            }
        }

        public void Show()
        {
            _panel.SetActive(true);
            Refresh();
            UiFocus.Acquire(playerController, playerCamera, interactor);
        }

        public void Hide()
        {
            _panel.SetActive(false);
            UiFocus.Release(playerController, playerCamera, interactor);
        }

        // Potpis vidljivog stanja (prag + stanje skladišta za tražene resurse):
        // periodični refresh ruši i gradi cijeli panel, pa se preskače kad se
        // ništa vidljivo nije promijenilo.
        private string _lastSignature;

        private string BuildSignature()
        {
            var sb = new StringBuilder();
            sb.Append(HubProgress.Tier);
            foreach (var tier in HubProgress.Tiers)
                foreach (var req in tier.Requirements)
                {
                    int have = 0;
                    if (HubStorage.Instance != null && req.Item != null)
                        have = HubStorage.Instance.Get(req.Item)?.GetStackSize() ?? 0;
                    sb.Append('|').Append(have);
                }
            return sb.ToString();
        }

        private void Refresh()
        {
            _refreshTimer = 0f;
            _lastSignature = BuildSignature();

            foreach (var go in _sections)
            {
                go.transform.SetParent(null);
                Destroy(go);
            }
            _sections.Clear();

            _statusLbl.text = HubProgress.Tier >= HubProgress.MaxTier
                ? $"Current tier: {HubProgress.Tier}/{HubProgress.MaxTier} — everything unlocked"
                : $"Current tier: {HubProgress.Tier}/{HubProgress.MaxTier}";

            float yCursor = 248f;
            for (int t = 0; t < HubProgress.MaxTier; t++)
                yCursor = BuildTierSection(t, yCursor) - 8f;
        }

        // Gradi blok jednog praga; vraća y donjeg ruba bloka.
        private float BuildTierSection(int tierIndex, float yTop)
        {
            var reqs = HubProgress.Tiers[tierIndex].Requirements;
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

            string status = unlocked ? "<color=#66ff66>UNLOCKED</color>"
                          : isNext   ? "<color=#ffaa44>NEXT TIER</color>"
                                     : "<color=#888888>LOCKED</color>";

            var header = MakeText(section.transform, $"<b>TIER {tierIndex + 1}</b>   {status}", 13,
                new Vector2(12f, -6f), new Vector2(300f, 20f));
            header.alignment = TextAlignmentOptions.TopLeft;

            var reqTxt = MakeText(section.transform, BuildReqText(tierIndex, unlocked), 11,
                new Vector2(12f, -28f), new Vector2(280f, reqs.Length * 15f + 4f));
            reqTxt.alignment = TextAlignmentOptions.TopLeft;

            var desc = MakeText(section.transform,
                $"<color=#8899aa>Unlocks: {HubProgress.Tiers[tierIndex].Unlocks}</color>", 10,
                new Vector2(12f, -(height - 18f)), new Vector2(396f, 16f));
            desc.alignment = TextAlignmentOptions.TopLeft;

            if (isNext)
                BuildUnlockButton(section.transform, height);

            return yTop - height;
        }

        private string BuildReqText(int tierIndex, bool unlocked)
        {
            var sb = new StringBuilder();
            foreach (var req in HubProgress.Tiers[tierIndex].Requirements)
            {
                if (unlocked)
                {
                    sb.AppendLine($"<color=#88cc88>{req.amount}x {req.DisplayName}</color>");
                    continue;
                }

                int have = 0;
                if (HubStorage.Instance != null && req.Item != null)
                    have = HubStorage.Instance.Get(req.Item)?.GetStackSize() ?? 0;

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

            var lbl = MakeText(btnGO.transform, "UNLOCK", 11, Vector2.zero, Vector2.zero);
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
            panelRT.sizeDelta = new Vector2(460f, 660f);

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            var title = MakeText(_panel.transform, "HUB PROGRESS", 20, Vector2.zero, new Vector2(420f, 32f));
            CenterAnchor(title.rectTransform, new Vector2(0f, 300f));
            title.alignment = TextAlignmentOptions.Center;

            _statusLbl = MakeText(_panel.transform, "", 12, Vector2.zero, new Vector2(420f, 20f));
            CenterAnchor(_statusLbl.rectTransform, new Vector2(0f, 274f));
            _statusLbl.alignment = TextAlignmentOptions.Center;
            _statusLbl.color     = new Color(0.65f, 0.75f, 0.85f);

            var hint = MakeText(_panel.transform, $"{GameKeys.CancelName} — close", 11, Vector2.zero, new Vector2(420f, 24f));
            CenterAnchor(hint.rectTransform, new Vector2(0f, -312f));
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
