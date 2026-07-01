using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
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

        public bool IsOpen => _panel.activeSelf;

        void Awake()
        {
            Instance = this;
            BuildUI();
            _panel.SetActive(false);
        }

        void Update()
        {
            if (_panel.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame)
                Hide();
        }

        public void Show()
        {
            _panel.SetActive(true);
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

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("ComputerMenu_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(300f, 220f);

            _panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.1f, 0.93f);

            var title = MakeLabel(_panel.transform, "KOMPJUTER", 18, new Vector2(0f, 80f), new Vector2(260f, 36f));
            title.alignment = TextAlignmentOptions.Center;

            MakeButton(_panel.transform, "Mreža planeta", new Vector2(0f, 18f), OpenNetworkMap);
            MakeButton(_panel.transform, "Crafting",      new Vector2(0f, -42f), OpenCrafting);

            var hint = MakeLabel(_panel.transform, "ESC — odustani", 11, new Vector2(0f, -95f), new Vector2(260f, 24f));
            hint.color     = new Color(0.6f, 0.6f, 0.6f);
            hint.alignment = TextAlignmentOptions.Center;
        }

        private void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(220f, 48f);

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
            tmp.fontSize  = 14;
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
