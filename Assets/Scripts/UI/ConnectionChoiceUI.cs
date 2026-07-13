using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ConnectionChoiceUI : MonoBehaviour
    {
        public static ConnectionChoiceUI Instance { get; private set; }

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private Interactor interactor;

        private GameObject _panel;
        private Button[] _buttons;
        private TextMeshProUGUI[] _labels;
        private Button _teleportButton;
        private TextMeshProUGUI _teleportLabel;

        private ConnectionManager _mgr;
        private Transform _source, _target;

        private static readonly ConnectionType[] Types =
            { ConnectionType.Weak, ConnectionType.Mid, ConnectionType.Strong };

        private static readonly string[] TypeNames = { "SLABA", "SREDNJA", "JAKA" };

        private static readonly Color[] TypeColors =
        {
            Color.red,
            new Color(1f, 0.5f, 0f),
            Color.green
        };

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

        public void Show(ConnectionManager mgr, Transform source, Transform target)
        {
            _mgr = mgr;
            _source = source;
            _target = target;
            RefreshButtons();
            _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            playerController?.SetInputEnabled(false);
            playerCamera?.SetInputEnabled(false);
            if (interactor != null) interactor.enabled = false;
        }

        private void Hide()
        {
            _panel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerController?.SetInputEnabled(true);
            playerCamera?.SetInputEnabled(true);
            if (interactor != null) interactor.enabled = true;
        }

        private void Choose(ConnectionType quality)
        {
            if (_mgr == null) return;
            _mgr.TryBuildConnection(_source, _target, quality);
            Hide();
        }

        private void ChooseTeleport()
        {
            if (_mgr == null) return;
            _mgr.TryTeleport(_source, _target);
            Hide();
        }

        private void RefreshButtons()
        {
            if (_mgr == null) return;
            for (int i = 0; i < Types.Length; i++)
            {
                bool canAfford = _mgr.CanAfford(Types[i]);
                _buttons[i].interactable = canAfford;

                var col = TypeColors[i];
                var block = _buttons[i].colors;
                block.normalColor      = canAfford ? col : new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f);
                block.highlightedColor = canAfford ? Color.Lerp(col, Color.white, 0.3f) : block.normalColor;
                block.selectedColor    = block.normalColor;
                block.disabledColor    = block.normalColor;
                _buttons[i].colors = block;

                _labels[i].text = BuildLabel(i);
            }

            bool canTeleport = _mgr.CanAffordTeleport();
            _teleportButton.interactable = canTeleport;
            var tCol = new Color(0f, 0.55f, 1f);
            var tBlock = _teleportButton.colors;
            tBlock.normalColor      = canTeleport ? tCol : new Color(0f, 0.22f, 0.4f);
            tBlock.highlightedColor = canTeleport ? Color.Lerp(tCol, Color.white, 0.3f) : tBlock.normalColor;
            tBlock.selectedColor    = tBlock.normalColor;
            tBlock.disabledColor    = tBlock.normalColor;
            _teleportButton.colors  = tBlock;
            _teleportLabel.text     = BuildTeleportLabel();
        }

        private string BuildLabel(int i)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{TypeNames[i]}</b>");

            var cost = _mgr.GetCost(Types[i]);
            if (cost == null || cost.Length == 0)
            {
                sb.AppendLine("Besplatno");
            }
            else
            {
                foreach (var req in cost)
                    if (req.item != null)
                        sb.AppendLine($"{req.amount}x {req.item.displayName}");
            }

            float lifespan = _mgr.GetLifespan(Types[i], _source, _target);
            sb.Append(lifespan <= 0f ? "∞" : $"{lifespan:F0}s");

            return sb.ToString();
        }

        private string BuildTeleportLabel()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>TELEPORT</b>");

            var cost = _mgr.GetTeleportCost();
            if (cost == null || cost.Length == 0)
            {
                sb.Append("Besplatno");
            }
            else
            {
                foreach (var req in cost)
                    if (req.item != null)
                        sb.Append($"{req.amount}x {req.item.displayName}");
            }

            return sb.ToString();
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("ConnectionChoice_Panel");
            _panel.transform.SetParent(transform, false);

            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(560f, 310f);

            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0f, 0.05f, 0.1f, 0.93f);

            // Title
            var title = MakeText(_panel.transform, "Izaberi akciju", 20,
                new Vector2(0f, 128f), new Vector2(500f, 36f));
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;

            // Connection section label
            var connLabel = MakeText(_panel.transform, "— VEZA —", 11,
                new Vector2(0f, 86f), new Vector2(500f, 22f));
            connLabel.alignment = TextAlignmentOptions.Center;
            connLabel.color = new Color(0.6f, 0.6f, 0.6f);

            // 3 connection choice buttons
            _buttons = new Button[3];
            _labels  = new TextMeshProUGUI[3];

            float[] xPositions = { -180f, 0f, 180f };
            for (int i = 0; i < 3; i++)
            {
                var (btn, lbl) = MakeButton(_panel.transform, xPositions[i]);
                _buttons[i] = btn;
                _labels[i]  = lbl;

                int captured = i;
                btn.onClick.AddListener(() => Choose(Types[captured]));
            }

            // Teleport section label
            var teleLabel = MakeText(_panel.transform, "— TELEPORT —", 11,
                new Vector2(0f, -82f), new Vector2(500f, 22f));
            teleLabel.alignment = TextAlignmentOptions.Center;
            teleLabel.color = new Color(0.6f, 0.6f, 0.6f);

            // Teleport button
            (_teleportButton, _teleportLabel) = MakeTeleportButton(_panel.transform);
            _teleportButton.onClick.AddListener(ChooseTeleport);

            // Hint
            var hint = MakeText(_panel.transform, "ESC — odustani", 11,
                new Vector2(0f, -148f), new Vector2(500f, 24f));
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.6f, 0.6f, 0.6f);
        }

        private (Button btn, TextMeshProUGUI lbl) MakeButton(Transform parent, float x)
        {
            var go = new GameObject("ChoiceBtn");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, -10f);
            rt.sizeDelta        = new Vector2(160f, 140f);

            var img = go.AddComponent<Image>();
            img.color = Color.white;

            var btn = go.AddComponent<Button>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(6f, 6f);
            labelRT.offsetMax = new Vector2(-6f, -6f);

            var lbl = labelGO.AddComponent<TextMeshProUGUI>();
            lbl.fontSize  = 13;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = Color.white;

            return (btn, lbl);
        }

        private (Button btn, TextMeshProUGUI lbl) MakeTeleportButton(Transform parent)
        {
            var go = new GameObject("TeleportBtn");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, -115f);
            rt.sizeDelta        = new Vector2(220f, 50f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0.55f, 1f);

            var btn = go.AddComponent<Button>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(6f, 6f);
            labelRT.offsetMax = new Vector2(-6f, -6f);

            var lbl = labelGO.AddComponent<TextMeshProUGUI>();
            lbl.fontSize  = 13;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = Color.white;

            return (btn, lbl);
        }

        private TextMeshProUGUI MakeText(Transform parent, string text, float size,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text     = text;
            tmp.fontSize = size;
            return tmp;
        }
    }
}
