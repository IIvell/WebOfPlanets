using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
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
            // vrati igrača u napola otvoren UI s krivim stanjem kursora/inputa.
            var progress = FindFirstObjectByType<HubProgressUI>();
            if (progress != null && progress.IsOpen) progress.Hide();
            var computer = FindFirstObjectByType<ComputerMenuUI>();
            if (computer != null && computer.IsOpen) computer.Hide();

            _root.SetActive(true);
            AudioManager.PlayAlert();

            if (GameManager.Instance != null) GameManager.Instance.Win();
            else Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ResolveReferences();
            _playerController?.SetInputEnabled(false);
            _playerCamera?.SetInputEnabled(false);
            if (_interactor != null) _interactor.enabled = false;
        }

        private void KeepPlaying()
        {
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
