using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WebOfPlanets
{
    // Attach to a Canvas (Screen Space – Overlay). Zdravstvena traka gore lijevo + flash na štetu + poruka pri smrti.
    [RequireComponent(typeof(RectTransform))]
    public class HealthUI : MonoBehaviour
    {
        private const float BarWidth = 220f;
        private const float BarHeight = 24f;

        private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color HealthyColor = new Color(0.2f, 0.85f, 0.25f, 0.9f);
        private static readonly Color HurtColor = new Color(0.95f, 0.75f, 0.15f, 0.9f);
        private static readonly Color CriticalColor = new Color(0.9f, 0.15f, 0.15f, 0.9f);

        [SerializeField] private float damageFlashAlpha = 0.35f;
        [SerializeField] private float damageFlashFadeSpeed = 2.5f;

        private Image _fillImage;
        private TextMeshProUGUI _label;
        private Image _damageFlash;
        private GameObject _deathPanel;
        private GameObject _barRoot;

        private float _flashAlpha;

        public static HealthUI Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        // Sakriva SAMO traku zdravlja — damage flash i death overlay moraju ostati
        // aktivni i dok je fullscreen panel (npr. NetworkMapUI) otvoren.
        // Traka je dijete Canvasa, ne ovog objekta, pa gašenje komponente ne pomaže.
        public static void SetBarVisible(bool visible)
        {
            if (Instance != null && Instance._barRoot != null)
                Instance._barRoot.SetActive(visible);
        }

        void Awake()
        {
            Instance = this;
            BuildUI();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEventBus.OnPlayerHealthChanged += OnHealthChanged;
            GameEventBus.OnPlayerDamaged += OnDamaged;
            GameEventBus.OnPlayerDied += OnDied;
        }

        void OnDisable()
        {
            GameEventBus.OnPlayerHealthChanged -= OnHealthChanged;
            GameEventBus.OnPlayerDamaged -= OnDamaged;
            GameEventBus.OnPlayerDied -= OnDied;
        }

        void Update()
        {
            if (_flashAlpha <= 0f) return;

            // Unscaled: na smrt GameManager zamrzne timeScale, a flash mora izblijedjeti.
            _flashAlpha = Mathf.Max(0f, _flashAlpha - damageFlashFadeSpeed * Time.unscaledDeltaTime);
            var c = _damageFlash.color;
            c.a = _flashAlpha;
            _damageFlash.color = c;
        }

        private void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            float ratio = e.Max > 0f ? e.Current / e.Max : 0f;
            _fillImage.fillAmount = ratio;
            _fillImage.color = ratio > 0.5f ? HealthyColor : ratio > 0.2f ? HurtColor : CriticalColor;
            _label.text = $"{Mathf.CeilToInt(e.Current)} / {Mathf.CeilToInt(e.Max)}";

            // Revive (GameManager.Respawn) vraća zdravlje — makni death overlay.
            if (_deathPanel != null && _deathPanel.activeSelf && e.Current > 0f)
                _deathPanel.SetActive(false);
        }

        private void OnDamaged(PlayerDamagedEvent e)
        {
            _flashAlpha = damageFlashAlpha;
        }

        private void OnDied(PlayerDiedEvent e)
        {
            // Isti null-guard kao u OnHealthChanged — event može stići prije BuildUI.
            if (_deathPanel != null) _deathPanel.SetActive(true);
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform uiRoot = canvas != null ? canvas.transform : transform;

            BuildBar(uiRoot);
            BuildDamageFlash(uiRoot);
            BuildDeathPanel(uiRoot);
        }

        private void BuildBar(Transform uiRoot)
        {
            var barGO = new GameObject("HealthBar");
            _barRoot = barGO;
            barGO.transform.SetParent(uiRoot, false);
            var barRT = barGO.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 1f);
            barRT.anchorMax = new Vector2(0f, 1f);
            barRT.pivot = new Vector2(0f, 1f);
            barRT.anchoredPosition = new Vector2(16f, -16f);
            barRT.sizeDelta = new Vector2(BarWidth, BarHeight);

            var bg = barGO.AddComponent<Image>();
            bg.color = BackgroundColor;
            UiTheme.StyleBarFrame(bg); // itch.io pack; bez sprite-a ostaje stara crna ploča

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barGO.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            // S temom veći inset da punjenje ne prekrije rub okvira.
            float inset = UiTheme.HasTheme ? 6f : 2f;
            fillRT.offsetMin = new Vector2(inset, inset);
            fillRT.offsetMax = new Vector2(-inset, -inset);

            _fillImage = fillGO.AddComponent<Image>();
            _fillImage.color = HealthyColor;
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillAmount = 1f;
            // Bijeli segmentirani strip — tintaju ga postojeće boje zdravlja.
            UiTheme.StyleBarFill(_fillImage);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(barGO.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            _label = labelGO.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 13;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.text = "100 / 100";
        }

        private void BuildDamageFlash(Transform uiRoot)
        {
            var flashGO = new GameObject("DamageFlash");
            flashGO.transform.SetParent(uiRoot, false);
            var flashRT = flashGO.AddComponent<RectTransform>();
            flashRT.anchorMin = Vector2.zero;
            flashRT.anchorMax = Vector2.one;
            flashRT.offsetMin = Vector2.zero;
            flashRT.offsetMax = Vector2.zero;

            _damageFlash = flashGO.AddComponent<Image>();
            _damageFlash.color = new Color(0.8f, 0f, 0f, 0f);
            _damageFlash.raycastTarget = false;
        }

        private void BuildDeathPanel(Transform uiRoot)
        {
            _deathPanel = new GameObject("DeathPanel");
            _deathPanel.transform.SetParent(uiRoot, false);
            var panelRT = _deathPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var bg = _deathPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(_deathPanel.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.5f, 0.5f);
            textRT.anchorMax = new Vector2(0.5f, 0.5f);
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.sizeDelta = new Vector2(600f, 100f);

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = $"You died\n<size=20>Press <b>{GameKeys.RespawnName}</b> to return to the respawn totem</size>";

            _deathPanel.SetActive(false);
        }
    }
}
