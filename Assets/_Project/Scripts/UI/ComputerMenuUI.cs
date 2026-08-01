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

            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0f, 0.05f, 0.1f, 0.93f);
            UiTheme.StyleWindow(panelImg); // isti čisti okvir kao hub skladište

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
            UiTheme.StyleButton(img);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            // S temom sprite nosi boju pa tranzicije idu svjetlinom; bez teme stare plave.
            colors.normalColor      = UiTheme.Tint(new Color(0.85f, 0.85f, 0.85f), new Color(0.08f, 0.25f, 0.45f));
            colors.highlightedColor = UiTheme.Tint(Color.white,                    new Color(0.15f, 0.40f, 0.65f));
            colors.pressedColor     = UiTheme.Tint(new Color(0.6f, 0.6f, 0.6f),    new Color(0.05f, 0.15f, 0.30f));
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

        // Visine redaka kartice praga — jedino mjesto s tim brojkama, da se
        // izračun visine kartice i pozicije redaka ne mogu razići.
        private const float HeaderHeight  = 22f;
        private const float ReqLineHeight = 16f;
        private const float DescHeight    = 20f;
        private const float UnlockWidth   = 100f;

        private GameObject      _panel;
        private RectTransform   _listContent;
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

            for (int t = 0; t < HubProgress.MaxTier; t++)
                BuildTierSection(t);
        }

        // Gradi karticu jednog praga kao redak vertikalnog layouta u scroll listi.
        // Prije su kartice bile ručno pozicionirane od fiksnog y=248 naniže i šire
        // od panela (420 vs tijelo sprite-a), pa su izlazile izvan okvira, a peti
        // prag je ispadao ispod dna. Sada širinu daje layout, a visina se računa
        // iz istih konstanti kojima se pozicioniraju redci.
        private void BuildTierSection(int tierIndex)
        {
            var reqs = HubProgress.Tiers[tierIndex].Requirements;
            float inner  = UiTheme.HasTheme ? 14f : 8f; // uvlaka do ruba okvira kartice
            float height = inner * 2f + HeaderHeight + reqs.Length * ReqLineHeight + DescHeight;

            bool unlocked = HubProgress.Tier > tierIndex;
            bool isNext   = HubProgress.Tier == tierIndex;
            // Gumb UNLOCK zauzima desni rub kartice — tekst mu ne smije ići ispod.
            float right = isNext ? inner + UnlockWidth + 10f : inner;

            var section = new GameObject("Tier_" + (tierIndex + 1));
            section.transform.SetParent(_listContent, false);
            _sections.Add(section);

            section.AddComponent<RectTransform>();

            var sectionImg = section.AddComponent<Image>();
            sectionImg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
            UiTheme.StyleButton(sectionImg); // blok praga kao uokvirena kartica

            var layoutElement = section.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight       = height;

            string status = unlocked ? "<color=#66ff66>UNLOCKED</color>"
                          : isNext   ? "<color=#ffaa44>NEXT TIER</color>"
                                     : "<color=#888888>LOCKED</color>";

            var header = MakeText(section.transform, $"<b>TIER {tierIndex + 1}</b>   {status}", 13,
                Vector2.zero, Vector2.zero);
            StretchTop(header.rectTransform, inner, right, inner, HeaderHeight);
            header.alignment = TextAlignmentOptions.TopLeft;

            var reqTxt = MakeText(section.transform, BuildReqText(tierIndex, unlocked), 11,
                Vector2.zero, Vector2.zero);
            StretchTop(reqTxt.rectTransform, inner, right, inner + HeaderHeight,
                reqs.Length * ReqLineHeight);
            reqTxt.alignment = TextAlignmentOptions.TopLeft;

            var desc = MakeText(section.transform,
                $"<color=#8899aa>Unlocks: {HubProgress.Tiers[tierIndex].Unlocks}</color>", 11,
                Vector2.zero, Vector2.zero);
            StretchTop(desc.rectTransform, inner, right,
                inner + HeaderHeight + reqs.Length * ReqLineHeight, DescHeight);
            desc.alignment = TextAlignmentOptions.TopLeft;

            if (isNext)
                BuildUnlockButton(section.transform, height, inner);
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

        private void BuildUnlockButton(Transform section, float sectionHeight, float inner)
        {
            bool canUnlock = HubProgress.CanUnlockNext();

            var btnGO = new GameObject("UnlockBtn");
            btnGO.transform.SetParent(section, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin        = new Vector2(1f, 1f);
            btnRT.anchorMax        = new Vector2(1f, 1f);
            btnRT.pivot            = new Vector2(1f, 1f);
            btnRT.anchoredPosition = new Vector2(-inner, -inner);
            btnRT.sizeDelta        = new Vector2(UnlockWidth, sectionHeight - inner * 2f);

            var btnImg = btnGO.AddComponent<Image>();
            UiTheme.StyleButton(btnImg); // boju (zeleno/sivo) postavlja logika ispod
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

            // Panel visok 600 (a ne 660): uz UiScale +20% to je 720px, pa stane i
            // na najmanju podržanu visinu prozora (768). Pragovi se listaju.
            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(480f, 600f);

            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0f, 0.05f, 0.1f, 0.93f);
            UiTheme.StyleWindow(panelImg); // isti čisti okvir kao hub skladište

            float pad = UiTheme.WindowPadding;

            var title = MakeText(_panel.transform, "HUB PROGRESS", 20, Vector2.zero, Vector2.zero);
            StretchTop(title.rectTransform, pad, pad, pad * 0.6f, 32f);
            title.alignment = TextAlignmentOptions.Center;

            _statusLbl = MakeText(_panel.transform, "", 12, Vector2.zero, Vector2.zero);
            StretchTop(_statusLbl.rectTransform, pad, pad, pad * 0.6f + 34f, 20f);
            _statusLbl.alignment = TextAlignmentOptions.Center;
            _statusLbl.color     = new Color(0.65f, 0.75f, 0.85f);

            BuildScrollArea(pad);

            var hint = MakeText(_panel.transform, $"{GameKeys.CancelName} — close", 11, Vector2.zero, Vector2.zero);
            var hintRT = hint.rectTransform;
            hintRT.anchorMin        = new Vector2(0f, 0f);
            hintRT.anchorMax        = new Vector2(1f, 0f);
            hintRT.pivot            = new Vector2(0.5f, 0f);
            hintRT.sizeDelta        = new Vector2(-2f * pad, 22f);
            hintRT.anchoredPosition = new Vector2(0f, pad * 0.4f);
            hint.alignment = TextAlignmentOptions.Center;
            hint.color     = new Color(0.6f, 0.6f, 0.6f);
        }

        private void BuildScrollArea(float pad)
        {
            var scrollGO = new GameObject("ScrollArea");
            scrollGO.transform.SetParent(_panel.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(pad, pad + 26f);              // iznad hinta
            scrollRT.offsetMax = new Vector2(-pad, -(pad * 0.6f + 58f));   // ispod naslova i statusa
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
            _listContent.sizeDelta = Vector2.zero;

            var layout = contentGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing                = 8f;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = _listContent;
        }

        // Vodoravno rastegnut redak sidren na vrh roditelja.
        private static void StretchTop(RectTransform rt, float left, float right, float top, float height)
        {
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(-(left + right), height);
            rt.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
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
