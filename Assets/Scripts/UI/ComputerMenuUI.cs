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
}
