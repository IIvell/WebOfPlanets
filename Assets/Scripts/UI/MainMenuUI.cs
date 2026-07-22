using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Glavni izbornik + pauza. Samopokretajući: Bootstrap stvara vlastiti canvas
    // runtime, bez izmjena scene (scena se u editoru drži u memoriji pa se disk
    // izmjene gube — isti razlog kao Resources.Load fallbackovi drugdje).
    // Prikazuje se pri pokretanju (Igraj / Kontrole / Izlaz); tijekom igre Esc
    // ga otvara kao pauzu (Nastavi). Gate za Esc: kursor zaključan = nijedan
    // drugi panel nije otvoren (isti obrazac kao MachinePlacer).
    public class MainMenuUI : MonoBehaviour
    {
        private const float ButtonWidth = 280f;
        private const float ButtonHeight = 52f;
        private const float ButtonSpacing = 14f;

        private static readonly Color BackdropColor = new Color(0.02f, 0.03f, 0.06f, 0.88f);
        private static readonly Color ButtonColor = new Color(0.16f, 0.22f, 0.34f, 0.95f);
        private static readonly Color TitleColor = new Color(0.75f, 0.85f, 1f, 1f);
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);

        private GameObject _root;
        private GameObject _mainPanel;
        private GameObject _controlsPanel;
        private TextMeshProUGUI _playLabel;

        private PlayerController _playerController;
        private PlayerCamera _playerCamera;
        private Interactor _interactor;

        private bool _isOpen;
        private bool _startedOnce; // prije prvog "Igraj" nema se što nastaviti

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<MainMenuUI>() != null) return;
            if (FindFirstObjectByType<PlayerController>() == null) return; // nije gameplay scena

            var go = new GameObject("MainMenuCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // iznad gameplay canvasa (HUD, paneli, death overlay)
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<MainMenuUI>();

            // Gumbi ne rade bez EventSystema — scena ga ima, fallback za svaki slučaj.
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        void Awake()
        {
            BuildUI();
        }

        void Start()
        {
            Open();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (_isOpen)
            {
                if (!keyboard.escapeKey.wasPressedThisFrame) return;
                if (_controlsPanel.activeSelf) ShowControls(false);
                else Play();
                return;
            }

            if (GameManager.IsPlaying
                && Cursor.lockState == CursorLockMode.Locked
                && keyboard.escapeKey.wasPressedThisFrame)
                Open();
        }

        // ── Otvaranje / zatvaranje ────────────────────────────────────────────

        public void Open()
        {
            _isOpen = true;
            _playLabel.text = _startedOnce ? "Nastavi" : "Igraj";
            ShowControls(false);
            _root.SetActive(true);

            if (GameManager.Instance != null) GameManager.Instance.Pause();
            else Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ResolveReferences();
            _playerController?.SetInputEnabled(false);
            _playerCamera?.SetInputEnabled(false);
            if (_interactor != null) _interactor.enabled = false;
        }

        private void Play()
        {
            _isOpen = false;
            _startedOnce = true;
            _root.SetActive(false);

            if (GameManager.Instance != null) GameManager.Instance.Resume();
            else Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ResolveReferences();
            _playerController?.SetInputEnabled(true);
            _playerCamera?.SetInputEnabled(true);
            if (_interactor != null) _interactor.enabled = true;
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowControls(bool show)
        {
            _mainPanel.SetActive(!show);
            _controlsPanel.SetActive(show);
        }

        private void ResolveReferences()
        {
            if (_playerController == null) _playerController = FindFirstObjectByType<PlayerController>();
            if (_playerCamera == null)     _playerCamera     = FindFirstObjectByType<PlayerCamera>();
            if (_interactor == null)       _interactor       = FindFirstObjectByType<Interactor>();
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            _root = new GameObject("MainMenu");
            _root.transform.SetParent(transform, false);
            var rootRT = _root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            var backdrop = _root.AddComponent<Image>();
            backdrop.color = BackdropColor;

            BuildTitle(_root.transform);
            BuildMainPanel(_root.transform);
            BuildControlsPanel(_root.transform);

            _root.SetActive(false);
        }

        private void BuildTitle(Transform parent)
        {
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(parent, false);
            var rt = titleGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 240f);
            rt.sizeDelta = new Vector2(1000f, 120f);

            var title = titleGO.AddComponent<TextMeshProUGUI>();
            title.text = "WEB OF PLANETS";
            title.fontSize = 72;
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 12f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = TitleColor;
            title.raycastTarget = false;
        }

        private void BuildMainPanel(Transform parent)
        {
            _mainPanel = new GameObject("Buttons");
            _mainPanel.transform.SetParent(parent, false);
            var rt = _mainPanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(ButtonWidth, 3f * ButtonHeight + 2f * ButtonSpacing);

            _playLabel = MakeButton(_mainPanel.transform, "Igraj", ButtonHeight + ButtonSpacing, Play);
            MakeButton(_mainPanel.transform, "Kontrole", 0f, () => ShowControls(true));
            MakeButton(_mainPanel.transform, "Izlaz", -(ButtonHeight + ButtonSpacing), Quit);
        }

        private void BuildControlsPanel(Transform parent)
        {
            _controlsPanel = new GameObject("Controls");
            _controlsPanel.transform.SetParent(parent, false);
            var rt = _controlsPanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -60f);
            rt.sizeDelta = new Vector2(660f, 470f);

            var bg = _controlsPanel.AddComponent<Image>();
            bg.color = PanelColor;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(_controlsPanel.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(32f, 76f);
            textRT.offsetMax = new Vector2(-32f, -20f);

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text =
                "<b>KONTROLE</b>\n\n" +
                "<b>W A S D</b> — kretanje\n" +
                "<b>Miš</b> — kamera\n" +
                "<b>Space</b> — skok\n" +
                "<b>E</b> — interakcija (kopanje, preuzimanje, strojevi, računalo)\n" +
                "<b>I</b> — inventar\n" +
                "<b>Q</b> — opis odabranog predmeta\n" +
                "<b>1–9</b> — odabir hotbar slota\n" +
                "<b>P</b> — postavljanje stroja iz odabranog slota\n" +
                "<b>X</b> — otkazivanje dvosmjernog teleportera\n" +
                "<b>R</b> — oživljavanje nakon smrti\n" +
                "<b>Esc</b> — pauza / zatvaranje prozora";

            MakeButton(_controlsPanel.transform, "Natrag",
                -rt.sizeDelta.y * 0.5f + ButtonHeight * 0.5f + 16f, () => ShowControls(false));
        }

        private TextMeshProUGUI MakeButton(Transform parent, string label, float y, System.Action onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            var img = go.AddComponent<Image>();
            img.color = ButtonColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var text = labelGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;

            return text;
        }
    }
}
