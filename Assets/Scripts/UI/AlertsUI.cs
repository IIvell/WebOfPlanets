using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace xyz.germanfica.unity.planet.gravity
{
    // Attach to a Canvas (Screen Space – Overlay). Toast upozorenja gore desno:
    // kritična veza (<20% zdravlja) i puno hub skladište — eventi su se do sada
    // raise-ali bez ijednog subscribera pa igrač nije dobivao nikakvo upozorenje.
    [RequireComponent(typeof(RectTransform))]
    public class AlertsUI : MonoBehaviour
    {
        private const float ToastLifetime = 5f;
        private const float ToastFadeTime = 1f;
        private const float ToastWidth = 330f;
        private const float ToastHeight = 46f;
        private const float Spacing = 6f;
        private const int MaxToasts = 5;
        private const float StorageFullCooldown = 8f;

        private static readonly Color CriticalColor = new Color(0.55f, 0.05f, 0.05f, 0.88f);
        private static readonly Color WarningColor = new Color(0.55f, 0.4f, 0f, 0.88f);

        private RectTransform _stack;
        private GameObject _testingWatermark;

        private struct Toast
        {
            public GameObject Root;
            public CanvasGroup Group;
            public float DieAt;
        }

        private readonly List<Toast> _toasts = new();

        // Latch po paru planeta: OnConnectionCritical se raise-a za SVAKU promjenu
        // zdravlja ispod praga, a toast želimo samo pri ulasku u kritično stanje.
        private readonly HashSet<(int, int)> _criticalPairs = new();
        private float _nextStorageToastTime;

        void Awake()
        {
            BuildStack();
            BuildTestingWatermark();
        }

        void OnEnable()
        {
            GameEventBus.OnConnectionCritical += OnConnectionCritical;
            GameEventBus.OnConnectionHealthChanged += OnConnectionHealthChanged;
            GameEventBus.OnConnectionDestroyed += OnConnectionDestroyed;
            GameEventBus.OnStorageFull += OnStorageFull;
        }

        void OnDisable()
        {
            GameEventBus.OnConnectionCritical -= OnConnectionCritical;
            GameEventBus.OnConnectionHealthChanged -= OnConnectionHealthChanged;
            GameEventBus.OnConnectionDestroyed -= OnConnectionDestroyed;
            GameEventBus.OnStorageFull -= OnStorageFull;
        }

        // Unscaled vrijeme da toastovi žive i dok je simulacija pauzirana/game over.
        void Update()
        {
            // Watermark prati Inspector checkbox uživo (može se paliti/gasiti u playu).
            if (_testingWatermark.activeSelf != GameManager.TestingMode)
                _testingWatermark.SetActive(GameManager.TestingMode);

            bool changed = false;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                float remaining = _toasts[i].DieAt - Time.unscaledTime;
                if (remaining <= 0f)
                {
                    Destroy(_toasts[i].Root);
                    _toasts.RemoveAt(i);
                    changed = true;
                    continue;
                }
                _toasts[i].Group.alpha = Mathf.Clamp01(remaining / ToastFadeTime);
            }
            if (changed) RepositionToasts();
        }

        private void OnConnectionCritical(ConnectionHealthChangedEvent e)
        {
            if (e.PlanetA == null || e.PlanetB == null) return;
            if (!_criticalPairs.Add(PairKey(e.PlanetA, e.PlanetB))) return;

            ShowToast($"Connection {e.PlanetA.name} – {e.PlanetB.name} is critical ({e.Health:F0}%)!", CriticalColor);
        }

        private void OnConnectionHealthChanged(ConnectionHealthChangedEvent e)
        {
            if (e.PlanetA == null || e.PlanetB == null) return;
            // Zdravlje natrag iznad praga (budući popravak veza) — dozvoli novi toast.
            if (e.Health > 20f) _criticalPairs.Remove(PairKey(e.PlanetA, e.PlanetB));
        }

        private void OnConnectionDestroyed(ConnectionEvent e)
        {
            if (e.PlanetA == null || e.PlanetB == null) return;
            _criticalPairs.Remove(PairKey(e.PlanetA, e.PlanetB));
        }

        private void OnStorageFull(ResourceType type)
        {
            if (Time.unscaledTime < _nextStorageToastTime) return;
            _nextStorageToastTime = Time.unscaledTime + StorageFullCooldown;

            ShowToast("Hub storage full — incoming resources are discarded!", WarningColor);
        }

        private static (int, int) PairKey(Transform a, Transform b)
        {
            int ia = a.GetInstanceID(), ib = b.GetInstanceID();
            return ia < ib ? (ia, ib) : (ib, ia);
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void ShowToast(string message, Color background)
        {
            AudioManager.PlayAlert();

            while (_toasts.Count >= MaxToasts)
            {
                Destroy(_toasts[0].Root);
                _toasts.RemoveAt(0);
            }

            var toastGO = new GameObject("Toast");
            toastGO.transform.SetParent(_stack, false);
            var rt = toastGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(ToastWidth, ToastHeight);

            var bg = toastGO.AddComponent<Image>();
            bg.color = background;
            bg.raycastTarget = false;

            var group = toastGO.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(toastGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10f, 4f);
            textRT.offsetMax = new Vector2(-10f, -4f);

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 13;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = message;

            _toasts.Add(new Toast { Root = toastGO, Group = group, DieAt = Time.unscaledTime + ToastLifetime });
            RepositionToasts();
        }

        private void RepositionToasts()
        {
            // Najnoviji na vrhu, stariji se spuštaju.
            for (int i = 0; i < _toasts.Count; i++)
            {
                int fromTop = _toasts.Count - 1 - i;
                var rt = (RectTransform)_toasts[i].Root.transform;
                rt.anchoredPosition = new Vector2(0f, -fromTop * (ToastHeight + Spacing));
            }
        }

        private void BuildStack()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform uiRoot = canvas != null ? canvas.transform : transform;

            var stackGO = new GameObject("Alerts_Stack");
            stackGO.transform.SetParent(uiRoot, false);
            _stack = stackGO.AddComponent<RectTransform>();
            _stack.anchorMin = new Vector2(1f, 1f);
            _stack.anchorMax = new Vector2(1f, 1f);
            _stack.pivot = new Vector2(1f, 1f);
            _stack.anchoredPosition = new Vector2(-16f, -16f);
            _stack.sizeDelta = new Vector2(ToastWidth, 0f);
        }

        // Stalni natpis dolje lijevo dok je GameManager.testingMode uključen — stari
        // freeCrafting je znao ostati uključen u sceni jer se nigdje nije vidio.
        private void BuildTestingWatermark()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform uiRoot = canvas != null ? canvas.transform : transform;

            _testingWatermark = new GameObject("TestingModeWatermark");
            _testingWatermark.transform.SetParent(uiRoot, false);
            var rt = _testingWatermark.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(16f, 12f);
            rt.sizeDelta = new Vector2(420f, 22f);

            var text = _testingWatermark.AddComponent<TextMeshProUGUI>();
            text.text = "TESTING MODE — all resource costs disabled";
            text.fontSize = 13;
            text.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.raycastTarget = false;

            _testingWatermark.SetActive(false);
        }
    }
}
