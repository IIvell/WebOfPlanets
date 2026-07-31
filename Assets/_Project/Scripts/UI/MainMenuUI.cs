using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace WebOfPlanets
{
    // Glavni izbornik + pauza. Samopokretajući: Bootstrap stvara vlastiti canvas
    // runtime, bez izmjena scene (scena se u editoru drži u memoriji pa se disk
    // izmjene gube — isti razlog kao Resources.Load fallbackovi drugdje).
    // Prikazuje se pri pokretanju (Igraj / Kontrole / Izlaz); tijekom igre Esc
    // ga otvara kao pauzu (Nastavi). Gate za Esc: UiFocus kaže da nijedan
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
        private TextMeshProUGUI _saveLabel;
        private TextMeshProUGUI _loadLabel;
        private bool _loading;

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
            if (_loading) return; // tijekom učitavanja Esc ne smije zatvoriti meni

            if (_isOpen)
            {
                if (!GameKeys.WasPressed(GameKeys.Cancel)) return;
                if (_controlsPanel.activeSelf) ShowControls(false);
                else Play();
                return;
            }

            // ReleasedThisFrame: Esc koji je upravo zatvorio panel (npr. kompjuter)
            // ne smije u istom frameu otvoriti pause menu — vidi UiFocus.
            if (GameManager.IsPlaying
                && !UiFocus.IsAnyPanelOpen
                && !UiFocus.ReleasedThisFrame
                && GameKeys.WasPressed(GameKeys.Cancel))
                Open();
        }

        // ── Otvaranje / zatvaranje ────────────────────────────────────────────

        public void Open()
        {
            _isOpen = true;
            _playLabel.text = _startedOnce ? "Resume" : "Play";
            _saveLabel.text = "Save Game";
            _loadLabel.text = SaveSystem.SaveExists ? "Load Game" : "Load Game (none)";
            ShowControls(false);
            _root.SetActive(true);

            if (GameManager.Instance != null) GameManager.Instance.Pause();
            else Time.timeScale = 0f;

            ResolveReferences();
            UiFocus.Acquire(_playerController, _playerCamera, _interactor);
        }

        private void Play()
        {
            _isOpen = false;
            _startedOnce = true;
            _root.SetActive(false);

            if (GameManager.Instance != null) GameManager.Instance.Resume();
            else Time.timeScale = 1f;

            ResolveReferences();
            UiFocus.Release(_playerController, _playerCamera, _interactor);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SaveGame()
        {
            if (_loading) return;
            _saveLabel.text = SaveSystem.Save() ? "Saved" : "Save failed";
        }

        private void LoadGame()
        {
            if (_loading) return;
            if (!SaveSystem.SaveExists)
            {
                _loadLabel.text = "No save found";
                return;
            }
            StartCoroutine(LoadThenPlay());
        }

        // Load vrti par frameova (rušenje + ponovna izgradnja svijeta); meni ostaje
        // otvoren s ugašenim Esc-om dok ne završi, pa se igra sama nastavi.
        private System.Collections.IEnumerator LoadThenPlay()
        {
            _loading = true;
            _loadLabel.text = "Loading...";
            yield return SaveSystem.LoadRoutine();
            _loading = false;
            Play();
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
            rt.sizeDelta = new Vector2(ButtonWidth, 5f * ButtonHeight + 4f * ButtonSpacing);

            float step = ButtonHeight + ButtonSpacing;
            _playLabel = MakeButton(_mainPanel.transform, "Play", 2f * step, Play);
            _saveLabel = MakeButton(_mainPanel.transform, "Save Game", step, SaveGame);
            _loadLabel = MakeButton(_mainPanel.transform, "Load Game", 0f, LoadGame);
            MakeButton(_mainPanel.transform, "Controls", -step, () => ShowControls(true));
            MakeButton(_mainPanel.transform, "Quit", -2f * step, Quit);
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
            // Imena tipki dolaze iz GameKeys — jedini izvor istine (prije je ovaj
            // tekst bio treća ručno sinkronizirana kopija rasporeda tipki).
            text.text =
                "<b>CONTROLS</b>\n\n" +
                "<b>W A S D</b> — move\n" +
                "<b>Mouse</b> — camera\n" +
                "<b>Space</b> — jump\n" +
                $"<b>{GameKeys.InteractName}</b> — interact (mining, pickup, machines, computer)\n" +
                $"<b>{GameKeys.InventoryName}</b> — inventory\n" +
                $"<b>{GameKeys.ItemInfoName}</b> — description of selected item\n" +
                "<b>1–9</b> — select hotbar slot\n" +
                $"<b>{GameKeys.PlaceMachineName}</b> — place machine from selected slot\n" +
                $"<b>{GameKeys.PickupMachineName}</b> — cancel two-way teleporter\n" +
                $"<b>{GameKeys.RespawnName}</b> — respawn after death\n" +
                $"<b>{GameKeys.CancelName}</b> — pause / close window";

            MakeButton(_controlsPanel.transform, "Back",
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
            btn.onClick.AddListener(() => { AudioManager.PlayUiClick(); onClick(); });

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

    // Premješteno iz VictoryUI.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Pobjeda: otključan zadnji hub prag (OnRecipeTierUnlocked == MaxTier) otvara
    // ekran "mreža je potpuna" s izborom nastavi igrati / izađi. Samopokretajući
    // Bootstrap obrazac kao MainMenuUI — vlastiti canvas runtime, bez izmjena scene.
    public class VictoryUI : MonoBehaviour
    {
        private const float ButtonWidth = 280f;
        private const float ButtonHeight = 52f;
        private const float ButtonSpacing = 14f;

        private static readonly Color BackdropColor = new Color(0.02f, 0.05f, 0.03f, 0.9f);
        private static readonly Color ButtonColor = new Color(0.16f, 0.3f, 0.2f, 0.95f);
        private static readonly Color TitleColor = new Color(0.6f, 1f, 0.7f, 1f);

        private GameObject _root;
        private PlayerController _playerController;
        private PlayerCamera _playerCamera;
        private Interactor _interactor;

        // Pobjeda se prikazuje jednom po sesiji — nakon "Keep Playing" se ne vraća
        // (zadnji prag se ionako ne može ponovno otključati).
        private bool _shown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<VictoryUI>() != null) return;
            if (FindFirstObjectByType<PlayerController>() == null) return; // nije gameplay scena

            var go = new GameObject("VictoryCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // iznad HUD-a i panela, ispod pauze (MainMenu = 100)
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<VictoryUI>();

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

        void OnEnable()  => GameEventBus.OnRecipeTierUnlocked += OnTierUnlocked;
        void OnDisable() => GameEventBus.OnRecipeTierUnlocked -= OnTierUnlocked;

        private void OnTierUnlocked(int tier)
        {
            if (_shown || tier < HubProgress.MaxTier) return;
            _shown = true;
            Show();
        }

        private void Show()
        {
            // Zadnji prag se otključava na Hub računalu — njegovi paneli su tada
            // otvoreni ispod pobjedničkog ekrana; zatvori ih da "Keep Playing" ne
            // vrati igrača u napola otvoren UI. (Stanje kursora/inputa sada čuva
            // UiFocus brojač, ali paneli bi bez ovoga ostali vizualno otvoreni.)
            var progress = FindFirstObjectByType<HubProgressUI>();
            if (progress != null && progress.IsOpen) progress.Hide();
            var computer = FindFirstObjectByType<ComputerMenuUI>();
            if (computer != null && computer.IsOpen) computer.Hide();

            _root.SetActive(true);
            AudioManager.PlayAlert();

            if (GameManager.Instance != null) GameManager.Instance.Win();
            else Time.timeScale = 0f;

            ResolveReferences();
            UiFocus.Acquire(_playerController, _playerCamera, _interactor);
        }

        private void KeepPlaying()
        {
            _root.SetActive(false);

            if (GameManager.Instance != null) GameManager.Instance.Resume();
            else Time.timeScale = 1f;

            ResolveReferences();
            UiFocus.Release(_playerController, _playerCamera, _interactor);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
            _root = new GameObject("Victory");
            _root.transform.SetParent(transform, false);
            var rootRT = _root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            var backdrop = _root.AddComponent<Image>();
            backdrop.color = BackdropColor;

            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_root.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = titleRT.anchorMax = titleRT.pivot = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0f, 200f);
            titleRT.sizeDelta = new Vector2(1200f, 120f);

            var title = titleGO.AddComponent<TextMeshProUGUI>();
            title.text = "NETWORK COMPLETE";
            title.fontSize = 72;
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 12f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = TitleColor;
            title.raycastTarget = false;

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(_root.transform, false);
            var bodyRT = bodyGO.AddComponent<RectTransform>();
            bodyRT.anchorMin = bodyRT.anchorMax = bodyRT.pivot = new Vector2(0.5f, 0.5f);
            bodyRT.anchoredPosition = new Vector2(0f, 90f);
            bodyRT.sizeDelta = new Vector2(900f, 120f);

            var body = bodyGO.AddComponent<TextMeshProUGUI>();
            body.text = "You unlocked every hub tier and connected the web of planets.\n" +
                        "You beat the game!";
            body.fontSize = 26;
            body.alignment = TextAlignmentOptions.Center;
            body.color = Color.white;
            body.raycastTarget = false;

            MakeButton(_root.transform, "Keep Playing", -30f, KeepPlaying);
            MakeButton(_root.transform, "Quit", -30f - (ButtonHeight + ButtonSpacing), Quit);

            _root.SetActive(false);
        }

        private void MakeButton(Transform parent, string label, float y, System.Action onClick)
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
            btn.onClick.AddListener(() => { AudioManager.PlayUiClick(); onClick(); });

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
        }
    }
}
